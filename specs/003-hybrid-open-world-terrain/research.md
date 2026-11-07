# Phase 0: Research & Technical Architecture Decisions

**Feature**: `003-hybrid-open-world-terrain`  
**Date**: 2026-11-07  
**Status**: Completed  

---

## 1. 千米级海岛高度场复合分形模型 (Island Heightfield Fractal Architecture)

### 决策内容 (Decision)
采用**复合分形数学采样器 (`IslandHeightSampler`)**，实现 $O(1)$ 复杂度的确定性高度查询：
1. **宏观岛屿径向衰减 (Radial Island Mask)**:
   - 以世界原点 $(0, 0)$ 为海岛中心。
   - 核心陆地半径 $R_{\text{core}} = 350\text{m}$（保证大面积开阔陆地与丘陵）。
   - 陆缘过渡带 $R_{\text{fade}} = 450\text{m} \sim 500\text{m}$，高度从海滩平滑跌落至海平面以下（$Y = -15\text{m}$ 海床）。
   - 外围大洋半径 $R > 500\text{m}$ 统一沉浸在大洋中。
2. **地形层次叠加 (Multi-layer Noise Octaves)**:
   - **宏观平原 (Macro Plains)**: 低频平滑噪声（波长 $200\text{m} \sim 300\text{m}$），振幅 $6\text{m} \sim 12\text{m}$，营造平缓微波丘陵。
   - **内陆山脊 (Inland Mountain Spine)**: 偏置绝对值山脊噪声（Rigid Multi-fractal / Billow Noise），偏向岛屿东北/西北内陆腹地，主峰拔高至 $50\text{m} \sim 75\text{m}$。
   - **地表微起伏 (Micro Detail)**: 高频微噪声（波长 $15\text{m} \sim 25\text{m}$），振幅 $1.5\text{m} \sim 2.5\text{m}$，赋予草甸自然的凹凸质感。
3. **海平面锚定 (Sea Level Anchor)**:
   - 海平面严格定义在世界绝对高度 $Y = 0.0\text{m}$。
   - 海滩海岸线过渡区在 $Y \in [-1.0\text{m}, 2.5\text{m}]$。

### 决策理由 (Rationale)
- **零内存占用与极速查询**：纯数学公式计算，无需加载上百兆的高精度灰度图或外部高度场资产，支持多线程任意坐标无锁并行采样。
- **可控可复现**：通过单一整型随机种子（`Seed`）即可完全确定全岛形态，天然契合开放世界动态流式加载。
- **与体素层完美对齐**：任何体素方块或角色移动逻辑，只需调用 `sampler.GetHeight(x, z)` 即可在几纳秒内获得精准基准面高度。

### 备选方案评估 (Alternatives Considered)
- **静态 16-bit PNG 高度图导入**：
  - 缺点：分辨率固定（如 $1024 \times 1024$ 贴图单个像素代表 1 米），缩放容易产生锯齿梯田感；需要打包几十兆的纹理资源；修改岛屿地貌需要反复导出资产。
- **纯粹均匀噪声（无径向遮罩）**：
  - 缺点：无法形成孤岛形态，四周无法自然呈现被大洋环绕的世界观，玩家走远后会出现无限大陆与山脉。

---

## 2. 64米宏观瓦片流式加载与视距 LOD 调度 (Tiled Streaming & LOD Grid)

### 决策内容 (Decision)
构建 **`TiledTerrainManager`**，以 $64\text{m} \times 64\text{m}$ 为空间切片粒度管理大世界：
1. **网格剖分与坐标映射**:
   - 世界坐标 $(X, Z)$ 映射至瓦片坐标：$\text{TileX} = \lfloor X / 64 \rfloor, \text{TileZ} = \lfloor Z / 64 \rfloor$。
   - 全岛 $1024\text{m} \times 1024\text{m}$ 划分为 $16 \times 16 = 256$ 个潜在瓦片。
2. **三级动态视距 (LOD Grid)**:
   - **近景 LOD 0 ($0 \sim 96\text{m}$，切比雪夫距离 $\le 1$)**: 
     - 顶点步长 $1\text{m}$（$64 \times 64 = 4096$ 四边形，约 8192 三角面）。
     - 附带物理碰撞体（`HeightMapShape3D` 或 `ConcavePolygonShape3D` on `Collision Layer 1`）。
     - 支持完整法线与材质细节。
   - **中景 LOD 1 ($96 \sim 320\text{m}$，切比雪夫距离 $2 \sim 4$)**:
     - 顶点步长 $4\text{m}$（$16 \times 16 = 256$ 四边形，约 512 三角面）。
     - 纯渲染网格，无物理碰撞体，减少物理引擎计算负担。
   - **远景 LOD 2 ($320 \sim 640\text{m}$，切比雪夫距离 $5 \sim 8$)**:
     - 顶点步长 $16\text{m}$（$4 \times 4 = 16$ 四边形，约 32 三角面）。
     - 极低消耗的地平线天际轮廓，配合大气深度雾呈现层次感。
   - **视距外 ($> 640\text{m}$)**: 卸载并存入对象池。
3. **后台线程异步生成与主线程节流挂载**:
   - 瓦片顶点数据（`Vector3[]`, `Vector3[] normals`, `int[] indices`）在 C# `Task.Run` 后台线程池中生成。
   - 生成完成后送入 `ConcurrentQueue<ReadyTileData>`。
   - 主线程在 `_Process` 中每帧最多挂载 2 个就绪瓦片（耗时控制在 $\le 3\text{ms}$），彻底杜绝掉帧。
4. **对象池化 (Object Pooling)**:
   - 瓦片被卸载时，不销毁 Godot 节点，而是隐藏并放入 `Queue<TerrainTile>` 回收池；新瓦片直接从池中复用，杜绝 GC 震荡。

### 决策理由 (Rationale)
- 遵循宪章原则 II（性能与异步优先）：主线程只负责不可避免的 GPU 资源提交，所有数学运算与几何剖分全在后台完成。
- $64\text{m}$ 瓦片尺寸适中：既不会像 $16\text{m}$ 那样产生数千个 Node 节点的树状管理开销，也不会像 $256\text{m}$ 那样导致单次网格提交突发卡顿。

### 备选方案评估 (Alternatives Considered)
- **单体 $1024\text{m}$ 大网格一次性加载**：
  - 缺点：顶点数达到百万级别，内存占用暴增，且无法进行视距剔除，在低端设备直接显存溢出。
- **Godot 社区 Terrain3D 插件**：
  - 缺点：该插件基于外部 C++ GDExtension，无法灵活深度挂接本项目的 C# 体素边界检测、统一基岩物理层与增量序列化系统。

---

## 3. 普适物理基岩与射线拾取法则 (Universal Physical Bedrock & Placement Snapping)

### 决策内容 (Decision)
统一整个开放世界的物理法则，将所有传统 3D 实体定义为**天然物理基岩**：
1. **物理碰撞层级规范**:
   - `Collision Layer 1`: **Environment (环境层)**。包括：3D 地形瓦片、3D 预制体（树干、巨石、石桥、神庙结构）以及体素块碰撞体。
   - 角色移动与摄像机检测均与 Layer 1 发生碰撞。
2. **射线检测识别与基岩拦截 (`BedrockPhysicsDetector`)**:
   - 当玩家准星发起射线检测时，命中碰撞体后通过元数据/节点类型识别目标：
     - 若命中节点为 `VoxelChunk`，且命中的体素不是底层基岩 $\implies$ 判定为**可破坏体素**。
     - 若命中节点为 `TerrainTile` 或普通 3D 静态实体（`StaticBody3D`），或命中体素高度 $\le H_{\text{terrain}}(x, z)$ $\implies$ 判定为**不可破坏基岩 (`HitType.Bedrock`)**。
3. **交互反馈**:
   - **鼠标左键（挖掘）**：命中基岩时，禁止修改任何体素，在碰撞点触发坚固火星粒子（Spark Particles）与低沉击石音效（Dull Hit Sound），给出清晰的物理不可破坏反馈。
   - **鼠标右键（放置）**：命中 3D 表面时，沿命中法线偏移：$P_{\text{place}} = P_{\text{hit}} + \vec{n} \times 0.5\text{m}$，将其对齐到最接近的整数网格 $\lfloor P_{\text{place}} \rceil$，并在该位置附着生成体素方块。

### 决策理由 (Rationale)
- 完美契合用户的核心世界观诉求：“精美不可变的 3D 骨架 + 自由搭建的沙盒体素”。
- 避免传统 3D 模型的布尔剔除（CSG）性能灾难，让写实建模的参天大树、古老废墟与天然岩壁成为玩家搭建树屋、哨塔或沿崖铺路的天然地基。

---

## 4. 局部体素卷 (VoxelVolume) 空间隔离与边界封闭 (Localized Voxel Volumes & Boundary Sealing)

### 决策内容 (Decision)
将体素系统从全局大世界解耦，升级为**局部体素卷 (`VoxelVolume`) 架构**：
1. **体素卷组件化**:
   - `VoxelVolume` 为独立的 Godot 3D 节点，拥有局部的 `AABB` 范围定义（例如采矿区 $64 \times 32 \times 64$ 或塌方山洞 $32 \times 16 \times 32$）。
   - 体素卷仅在玩家接近（如距离 $< 64\text{m}$）时激活挂载，远离时自动休眠。
2. **与 3D 地形高度场的垂直锚定与实心基岩填充**:
   - 局部体素卷内部的坐标系与世界网格对齐。
   - 在查询邻居体素以进行面剔除时：
     - 若查询的邻居坐标落在 3D 地形表面高度 $H(x, z)$ 之下，返回 `VoxelType.Bedrock`（实心不可破坏石头）。
     - 若查询的邻居坐标超出体素卷的 AABB 外边界，返回 `VoxelType.Air`。
   - **核心效果**：当玩家在体素卷内挖开泥土，露出下方的 3D 地形时，四周侧壁的体素块会检测到底下是实体，从而**正确生成侧壁与底部的闭合面**，彻底杜绝之前出现的“侧壁透视看到底层草地”的穿透镂空问题！

### 决策理由 (Rationale)
- **按需分配内存**：无需在千米大岛上为不需要挖掘的区域分配体素数据，内存占用下降 90% 以上。
- **彻底根治边界漏面**：通过显式的高度场基岩几何感知，使 `ChunkMesher` 的面剔除算法具有完备的边界封闭性。

---

## 5. 赛璐璐卡通水体与海陆交界面 (Stylized Ocean Water Shader)

### 决策内容 (Decision)
1. **海平面全局单体网格 (`OceanWaterPlane`)**:
   - 尺寸 $2048\text{m} \times 2048\text{m}$ 的水平 Quad 网格，锚定于 $Y = 0.0\text{m}$，随玩家摄像机在 X/Z 平面上以 16 米步长吸附移动（实现无限海平视感）。
2. **赛璐璐手绘水体 Shader**:
   - **颜色层次**：基于水深（利用 Scene Depth 缓冲区）实现浅滩碧绿海蓝到深海靛蓝的阶梯过渡。
   - **海岸浪花白边 (Shore Foam)**：当水体与 3D 地形相交（深度差 $< 0.8\text{m}$）时，生成手绘风格的白色浪沫边缘。
   - **高光与波光**：采用阶梯式卡通高光（Toon Specular）与轻微顶点波浪起伏。

---

## 结论与对齐
本研究方案完全解决用户提出的千米级宏观岛屿、大洋环绕、64m 异步流式加载、普适物理基岩与局部体素卷无缝嵌合等所有核心架构痛点。所有技术选型均满足项目宪章原则，准备进入 Phase 1 设计阶段。

