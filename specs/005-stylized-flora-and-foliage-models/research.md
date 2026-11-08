# Phase 0: Research & Technical Feasibility

**Feature**: `005-stylized-flora-and-foliage-models`  
**Date**: 2026-11-08  
**Status**: Completed

---

## 1. Ludo AI MCP 资产生成管线设计 (Asset Generation Pipeline)

### 1.1 业务背景与问题
原有程序化生成的植被（`FoliageScatter.cs`）仅使用极简的圆锥和圆柱体几何体作为占位符，缺乏生机与手绘质感；用户手动导入的外置 `.glb` 资产未经标准化处理，缺少物理碰撞体且层级不规范。

### 1.2 调研与技术选型

#### A. 提示词约束规范（严格符合 `docs/design/art_style_guide.md`）
- **通用正向词 (Positive Modifiers)**:
  `stylized 3d game asset, anime cel-shaded aesthetic, vibrant warm colors, ghibli inspired fantasy, clean silhouette, hand-painted texture, soft ambient occlusion, isolated on solid white background`
- **负向过滤词 (Negative Modifiers)**:
  `photorealistic, noisy textures, dirty realism, dark gritty, blurry details, complex modern military gear, deformed mesh`
- **具体物种正向定制**:
  - **橡木 (Oak)**: `broadleaf oak tree, lush vibrant green canopy, round fluffy foliage clusters, sturdy warm brown bark trunk, stylized hand-painted anime fantasy tree`
  - **云杉 (Spruce)**: `coniferous evergreen spruce pine tree, tiered conical foliage layers, deep teal forest green needles, straight sturdy dark wood trunk, stylized anime mountain tree`
  - **白桦 (Birch)**: `slender birch tree, distinctive ivory white bark with subtle dark horizontal lenticels, delicate golden green autumn foliage clumps, elegant silhouette, anime cel-shaded style`
  - **花草 (Flora)**: `vibrant field wildflowers clump, wild grass, tall grass, sunflowers, red poppies, yellow dandelions, hand-painted botanical game prop, clean alpha cutout, anime garden aesthetic`

#### B. Ludo AI 工具调用序列
1. **生成 2D 手绘正视图**: 调用 `createImage`（`image_type: "3d"` / `"asset"`, `perspective: "Front view"`, `art_style: "Cel-Shaded"`）；
2. **2D 转高质量 3D 模型**: 调用 `create3DModel`（`model: "prop"`, `target_num_faces: 15000`, `texture_size: 2048`, `texture_type: "pbr"`）；
3. **轮询任务状态**: 循环调用 `getApiJob`（`wait: 30`），直到状态为 `succeeded` 并提取 `model_url`；
4. **持久化与 LFS 下载**:
   - Ludo GCS 临时链接 7 天过期，**必须立即下载**至本地 `assets/models/trees/` 与 `assets/models/flora/`；
   - 依赖 `.gitattributes` 中配置的 `*.glb filter=lfs` 与 `*.png filter=lfs` 自动追踪。

---

## 2. 树木预制体标准化与物理碰撞架构 (Tree Prefabs & Collision)

### 2.1 碰撞体核心规范
- **痛点排查**:
  - `CollisionShape3D` 直接作为 `Node3D` 子节点时，物理引擎不分配 Body，导致穿模且射线/盒探测失效；
  - `Transform.Scale` 非等比缩放（如 `(0.5, 0.8, 0.5)`）会使物理引擎抛出警告，并在运行时导致接触面失真。
- **解决方案**:
  - 每种树木建立统一的标准场景预制体（如 `oak_tree.tscn`、`spruce_tree.tscn`、`birch_tree.tscn`）；
  - 树干专属物理节点：
    ```
    TreePrefab (Node3D, Tag: "Tree")
     ├── VisualMesh (MeshInstance3D)
     └── StaticBody3D (CollisionLayer: 1)
           └── CollisionShape3D (Transform.Scale: Vector3.One)
                 └── shape: CylinderShape3D (radius: 0.2~0.3m, height: 2.2~3.0m)
    ```
  - **树冠穿行与树干阻挡解耦**: 树叶部分保留无碰撞或只接收视觉光影，玩家走近树木可自然穿入树荫，但撞到树干被牢固阻挡；
  - **放置拦截集成**: 当玩家在树干处右键放置方块时，`VoxelInteractor.cs` 的 `shapeQuery.IntersectShape()` 命中树干的 `StaticBody3D`，触发 `[放置受阻] 目标位置存在 3D 实体障碍物` 拦截。

---

## 3. 多网格生态分层散布系统 (MultiMesh Foliage System)

### 3.1 渲染性能与批次（Batching）
- 开放世界成百上千株树木不能直接实例化数百个带有脚本的场景节点，否则 Draw Call 和场景树遍历开销过大；
- 采用 **混合双层架构**：
  1. **宏观环境层（大量远近风景）**：通过 `MultiMeshInstance3D` 进行 GPU 实例化批量渲染，单批次绘制 1000+ 株树木与花草，Draw Call 控制在 5 次以内；
  2. **物理交互层（玩家周边互动）**：
     - 在大世界中，大面积树木作为远景观赏与生态覆盖；
     - 玩家核心活动区（如大本营周边、可伐林区）实例化具备真实 `StaticBody3D` 碰撞体的实体树预制体；
     - 或者在 `FoliageScatter.cs` 内部为主要树干自动维护低成本的静态圆柱碰撞体网格。

### 3.2 生态群落分布规则 (Biomes & Altitudes)
| 植被类型 | 适宜高程区间 (Height) | 坡度限制 (Slope) | 生态分布逻辑 |
| :--- | :--- | :--- | :--- |
| **橡木 (Oak)** | $4.0\text{m} \sim 28.0\text{m}$ | $< 25^\circ$ | 分布于低地平原、缓坡草甸及淡水湖泊周边 |
| **白桦 (Birch)** | $12.0\text{m} \sim 38.0\text{m}$ | $< 32^\circ$ | 分布于平原过渡至山地的林缘地带 |
| **云杉 (Spruce)** | $24.0\text{m} \sim 68.0\text{m}$ | $< 42^\circ$ | 集中于东北内陆山脊腹地、高海拔山峰斜坡 |
| **野花簇 (Wildflowers)** | $3.0\text{m} \sim 35.0\text{m}$ | $< 20^\circ$ | 成片聚类生长于阳光充足的平原草甸 |
| **高草/野草 (Grass)** | $2.5\text{m} \sim 50.0\text{m}$ | $< 35^\circ$ | 广泛铺设于所有陆地草甸表面 |

- **排斥区（Exclusion Zones）**:
  动态检测场景内所有 `VoxelVolume`（如采石场、矿洞、玩家自建区），在包围盒外延 $4\text{m}$ 范围严禁散布任何 3D 树木与花草。

