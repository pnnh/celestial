# Tasks: 多样化赛璐璐植被资产与树木物理阻挡 (005-stylized-flora-and-foliage-models)

**Feature**: `005-stylized-flora-and-foliage-models`  
**Input**: Design documents from `specs/005-stylized-flora-and-foliage-models/` (`spec.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md`)  
**Prerequisites**: `plan.md` (completed), `spec.md` (completed)

---

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: 可并行执行（不同文件，无未完成任务依赖）
- **[Story]**: 所属用户故事标签（`[US1]`, `[US2]`, `[US3]`, `[US4]`）
- 包含精确的文件路径

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: 准备本地资产存储目录与生成管线

- [X] T001 创建本地资产目录结构在 `assets/models/trees/oak/`, `assets/models/trees/spruce/`, `assets/models/trees/birch/` 与 `assets/models/flora/`
- [X] T002 [P] 编写 Ludo AI 资产下载与本地缓存转存脚本在 `scripts/tools/fetch_ludo_asset.py`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 核心数据结构体与自动化测试基准

- [X] T003 [P] 实现植被变体配置结构体 `TreeVariantConfig.cs` 与 `FloraVariantConfig.cs` 在 `src/Terrain/TreeVariantConfig.cs`
- [X] T004 编写树木预制体层级与无告警碰撞体验证脚本在 `tests/test_tree_prefabs.gd`

---

## Phase 3: User Story 1 - 三种核心树木（橡木、云杉、白桦）赛璐璐模型生成与预制体搭建 (Priority: P1) 🎯 MVP

**Goal**: 生成符合日系清爽手绘赛璐璐美术风格的橡木、云杉、白桦 3D 资产，并封装为标准 Godot 预制体。

**Independent Test**: 在 Godot 中实例化各树木预制体，验证模型网格面数合理（< 25,000 面）、材质无噪点、贴图手绘感良好。

- [X] T005 [P] [US1] 调用 Ludo AI MCP 生成手绘赛璐璐风格橡木 (Oak) 模型与贴图并存入 `assets/models/trees/oak/oak.glb`
- [X] T006 [P] [US1] 调用 Ludo AI MCP 生成手绘赛璐璐风格云杉 (Spruce) 模型与贴图并存入 `assets/models/trees/spruce/spruce.glb`
- [X] T007 [P] [US1] 调用 Ludo AI MCP 生成手绘赛璐璐风格白桦 (Birch) 模型与贴图并存入 `assets/models/trees/birch/birch.glb`
- [X] T008 [US1] 在 Godot 中封装三款标准树木预制体在 `scenes/prefabs/trees/oak_tree.tscn`、`spruce_tree.tscn`、`birch_tree.tscn`

---

## Phase 4: User Story 2 - 树干专用圆柱碰撞体与体素防穿模放置联动 (Priority: P1)

**Goal**: 为各树木预制体树干配置规范的 `StaticBody3D` 与 `CylinderShape3D`，实现 100% 阻挡与防穿模放置，且编辑器 0 缩放告警。

**Independent Test**: 运行 `tests/test_tree_prefabs.gd` 验证各预制体碰撞体为 `Scale: (1, 1, 1)`，树干阻挡有效且方块放置受阻。

- [X] T009 [US2] 为各树木预制体挂载 `StaticBody3D`（Layer 1: Environment）并在树干对齐 `CylinderShape3D`（保持 Scale 1:1:1 无告警）
- [X] T010 [US2] 验证并测试玩家对树干的移动物理碰撞阻挡，以及通过 `VoxelInteractor.cs` 放置方块时的穿模拦截反馈

---

## Phase 5: User Story 3 - 经典野花与草丛（野草、高草、向日葵、虞美人、蒲公英）资产生成 (Priority: P2)

**Goal**: 使用 Ludo AI 生成 5 种地表野花与草丛模型/贴图资产，为海岛增添生动的微观生态层次。

**Independent Test**: 在场景中查看花草资产，验证色彩明快、与三平面草地高度贴合且不产生物理穿行阻碍。

- [X] T011 [P] [US3] 调用 Ludo AI MCP 生成低多边形野草 (Wild Grass) 与高草 (Tall Grass) 资产在 `assets/models/flora/wild_grass.glb` 与 `tall_grass.glb`
- [X] T012 [P] [US3] 调用 Ludo AI MCP 生成向日葵 (Sunflower)、虞美人 (Poppy) 与蒲公英 (Dandelion) 资产在 `assets/models/flora/`
- [X] T013 [US3] 为花草资产配置赛璐璐手绘双面渲染无碰撞材质

---

## Phase 6: User Story 4 - 批量植被散布器（FoliageScatter）多变体生态分层升级 (Priority: P2)

**Goal**: 升级 `FoliageScatter.cs`，实现多网格 GPU 实例化并行渲染，根据高程与坡度自然分布不同树种与花草，并维持 60 FPS 与采石场避让。

**Independent Test**: 运行自动化测试 `tests/test_multi_foliage_scatter.gd`，验证平原生成橡木、山脊生成云杉、采石场 0 生成。

- [X] T014 [P] [US4] 编写多变体植被散布与生态分层自动化测试在 `tests/test_multi_foliage_scatter.gd`
- [X] T015 [US4] 升级 `src/Terrain/FoliageScatter.cs`，由单网格重构为支持 `TreeVariants` 与 `FloraVariants` 阵列的生态分层散布
- [X] T016 [US4] 确保采石场等体素可变区（`VoxelVolume`）的排除遮罩与安全缓冲带在多网格模式下持续生效
- [X] T017 [US4] 在主场景 `scenes/main.tscn` 挂载多变体散布器，验证 1500+ 株混合植被同屏 60 FPS 渲染

---

## Phase 7: Polish & Verification

**Purpose**: 全量构建与回归自测

- [X] T018 [P] 执行工程 C# 全量编译构建验证在终端运行 `DOTNET_CLI_HOME=/tmp/dotnet_home dotnet build celestial.csproj`
- [X] T019 [P] 批量运行所有自动化测试套件在 `tests/`
- [X] T020 归档多样化植被资产与碰撞配置细节至 `docs/plan/open_world_terrain_and_streaming_architecture.md`

