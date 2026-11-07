# Implementation Plan: Hybrid Open-World Terrain, Tiled Streaming & Voxel Volumes

**Branch**: `003-hybrid-open-world-terrain` | **Date**: 2026-11-07 | **Spec**: [Feature Specification](file:///Users/Larry/Projects/github/celestial/specs/003-hybrid-open-world-terrain/spec.md)

**Input**: Feature specification from `/specs/003-hybrid-open-world-terrain/spec.md`

---

## Summary

实现 Celestial 混合开放世界架构的核心底座：
1. **千米级复合海岛高度场与全局大洋系统**：以数学分形构建 $1024\text{m} \times 1024\text{m}$ 的有机海岛地貌（中心平原微波、东北山脊拔高 $50\text{m} \sim 75\text{m}$、海滩缓坡、海平面 $Y=0.0\text{m}$ 下沉入浩瀚大洋）；
2. **64米瓦片异步流式加载与三级动态 LOD 调度**：构建 `TiledTerrainManager`，后台多线程异步计算几何数据，主线程限流挂载（$\le 3\text{ms}$/帧），对象池回收复用瓦片，维持稳定 60 FPS；
3. **普适物理基岩与表面附着搭建体系**：将底层 3D 瓦片与预制体统一置于 `Collision Layer 1: Environment`，挖掘左键判定为基岩阻隔并触发火星，放置右键支持沿 3D 表面法线对齐附着；
4. **局部体素卷 (VoxelVolume) 空间隔离与边界密封**：体素地幔层仅在指定区域（采矿区、田地、塌方山洞）激活，查询到底层 3D 地形高度时填充为实体基岩，彻底根治边界漏面透视问题。

---

## Technical Context

**Language/Version**: C# 12 / .NET 8.0, Godot Engine 4.7+ Mono (.NET 8.0)  
**Primary Dependencies**: Godot 4.7 Mono Engine API, `System.Threading.Tasks`, `System.Collections.Concurrent`, `System.Buffers` (`ArrayPool<T>`)  
**Storage**: 增量差异序列化 (`WorldSaveData`)，保存为本地 JSON / 二进制存档文件  
**Testing**: Godot Headless 脚本自动化测试套件 (`/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --script tests/...`)  
**Target Platform**: macOS (Apple Silicon Metal) & PC (Windows/Linux Vulkan/Forward+) 桌面端  
**Project Type**: 3D 混合开放世界动作冒险沙盒游戏  
**Performance Goals**: 稳定 60 FPS，主线程瓦片网格提交时间 $\le 3\text{ms}$/帧，活动瓦片数量恒定在 $25 \sim 49$ 个，内存占用平稳零泄漏  
**Constraints**: 瓦片尺寸严格 $64\text{m} \times 64\text{m}$，体素网格步长 $1\text{m}$，海平面严格锚定 $Y=0.0\text{m}$，无缝拼缝  
**Scale/Scope**: 宏观全岛 $1024\text{m} \times 1024\text{m}$（共 256 瓦片网格），视距半径 $640\text{m}$，支持多个独立 `VoxelVolume` 实例  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 宪章准则 | 评估要点 | 状态 | 实施措施与设计保障 |
|---|---|:---:|---|
| **I. 模块化与清晰边界** | 3D 地形、体素卷、交互物理、数据持久化四者解耦 | **PASS** | `IHeightSampler` 作为高度唯一真理源；`ITiledTerrainManager` 负责 3D 地形流式；`IVoxelVolume` 负责局部体素；`IBedrockPhysicsDetector` 统一处理交互射线。 |
| **II. 性能与异步优先** | 杜绝主线程阻塞掉帧，控制 GC 分配 | **PASS** | 瓦片顶点与法线计算全部移交 C# `Task.Run` 后台线程池，主线程每帧仅限流挂载 $\le 2$ 个瓦片（$\le 3\text{ms}$ 预算）；采用对象池复用瓦片节点。 |
| **III. 混合衔接与物理确定性** | 3D 地形与体素无空隙拼接，碰撞一致 | **PASS** | `VoxelVolume` 下边界与 `IHeightSampler` 紧密缝合，低于地表判定为实心基岩杜绝漏缝；3D 地表与体素统一位于 `Collision Layer 1: Environment`。 |
| **IV. 渐进式算法演进** | 原型阶段不引入过度复杂算法 | **PASS** | 瓦片生成采用规则网格（Regular Grid）+ 三级视距 LOD（1m / 4m / 16m 步长）；体素层采用经过验证的立方体面剔除，不超前引入 Marching Cubes。 |
| **V. 资产规范与轻量化** | 避免仓库膨胀，LFS 纳管 | **PASS** | 海岛高度完全基于分形数学参数生成，无需外部庞大贴图资产；无临时文件进库。 |
| **VI. 赛璐璐手绘幻想风** | 美术基调统一，严格版权规避 | **PASS** | 海水采用手绘卡通着色器（碧蓝海面、海岸浪沫白边、阶梯高光）；地表采用三平面卡通着色；坚决不借用 Zelda 专有侵权符号。 |

*结论：全项通过宪章门禁，无违规豁免项。*

---

## Project Structure

### Documentation (this feature)

```text
specs/003-hybrid-open-world-terrain/
├── spec.md              # 特性规范定义
├── plan.md              # 架构实施计划 (当前文件)
├── research.md          # Phase 0: 核心技术决策与备选评估
├── data-model.md        # Phase 1: 领域模型与数据结构契约
├── quickstart.md        # Phase 1: 可运行端到端验证指南
├── contracts/           # Phase 1: 公开接口与系统边界契约
│   ├── IHeightSampler.md
│   ├── ITiledTerrainManager.md
│   └── IVoxelVolume.md
└── tasks.md             # Phase 2: 任务分解 (将在后续 speckit-tasks 命令中生成)
```

### Source Code (repository root)

```text
src/
├── Terrain/                                 # 3D 开放世界大地形系统
│   ├── IHeightSampler.cs                   # 高度场采样契约
│   ├── IslandHeightSampler.cs              # [新增] 千米海岛复合分形采样器
│   ├── TerrainTileCoord.cs                 # [新增] 瓦片空间坐标与距离计算
│   ├── TerrainTile.cs                      # [新增] 64m 单体瓦片节点 (包含 LOD 网格与物理碰撞体)
│   ├── RawTileMeshData.cs                  # [新增] 后台异步网格数值传输对象
│   ├── TiledTerrainManager.cs              # [新增] 瓦片流式调度器与视距管理器
│   └── OceanWaterPlane.cs                  # [新增] 全局海平面与大洋水体渲染节点
├── Voxel/                                   # 体素引擎子系统
│   ├── VoxelType.cs                        # 体素类型定义 (含 Bedrock)
│   ├── VoxelChunk.cs                       # 单个 16x16x16 体素块
│   ├── ChunkMesher.cs                      # 体素网格化与面剔除 (支持基岩侧面封闭)
│   ├── VoxelVolume.cs                      # [新增] 局部体素卷节点 (农田、采矿区、山洞腔体)
│   ├── IVoxelVolume.cs                     # [新增] 局部体素卷接口
│   └── VoxelWorld.cs                       # 全局体素管理器兼容适配器
├── Player/                                  # 玩家交互与拾取
│   ├── VoxelInteractor.cs                  # [升级] 挖掘与放置控制器 (基岩与 3D 吸附)
│   ├── BedrockPhysicsDetector.cs           # [新增] 普适物理基岩与 3D 实体检测器
│   └── InteractionRaycastResult.cs         # [新增] 拾取结果结构体
└── Core/
    └── GameManager.cs                      # 游戏主管理器 (驱动地形与角色挂载)

shaders/
├── ocean_water.gdshader                    # [新增] 赛璐璐手绘大洋与海岸浪花着色器
└── terrain_triplanar.gdshader              # [新增] 赛璐璐三平面草地/岩石过渡着色器

scenes/
├── main.tscn                               # 主场景 (挂载 TiledTerrainManager, OceanWaterPlane, VoxelVolume)
└── prefabs/
    └── player.tscn                         # 角色预制体

tests/                                      # 自动化验证套件
├── test_island_height_sampler.gd           # 海岛采样连续性与标高测试
├── test_tiled_terrain_streaming.gd         # 64m 瓦片流式加载与 LOD 调度测试
├── test_bedrock_interaction.gd             # 普适物理基岩与放置吸附测试
└── test_voxel_volume_meshing.gd            # 局部体素卷边界封闭性测试
```

**Structure Decision**: 
采用模块化分层架构（`scripts/terrain/`, `scripts/voxel/`, `scripts/interaction/`），各子系统职责专一。通过接口契约（`IHeightSampler`, `IVoxelVolume`）进行通信解耦，杜绝循环依赖。

---

## Complexity Tracking

*Constitution Check 无违规项，设计满足极简渐进演进原则，无需豁免追踪。*
