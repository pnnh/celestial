# Tasks: Open-World Atmosphere, Stylized Texturing, Editor Preview & Foliage

**Feature**: `004-environment-and-foliage`  
**Input**: Design documents from `/specs/004-environment-and-foliage/` (`spec.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md`)  
**Prerequisites**: `plan.md` (completed), `spec.md` (completed)  

---

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: 可并行执行（不同文件，无未完成任务依赖）
- **[Story]**: 所属用户故事标签（`[US1]`, `[US2]`, `[US3]`, `[US4]`）
- 包含精确的文件路径

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: 准备基础资源与目录

- [x] T001 [P] 验证并准备植被与贴图资源路径在 `assets/textures/` 与 `assets/models/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 核心数据模型与配置结构体

- [x] T002 [P] 实现植被散布配置结构体 `FoliageScatterConfig` 在 `src/Terrain/FoliageScatter.cs`

---

## Phase 3: User Story 1 - 晨曦与远景大气透视雾 (Priority: P1) 🎯 MVP

**Goal**: 消除远海灰色硬切边与方形网格截断，实现天海一体的赛璐璐明媚氛围与大气距离透视蓝雾。

**Independent Test**: 运行自动化测试 `tests/test_environment_atmosphere.gd`，验证天空盒下半球颜色与海平面匹配无灰色带，大气雾距离与密度设置生效。

- [x] T003 [P] [US1] 编写大气环境与天空盒色彩验证脚本在 `tests/test_environment_atmosphere.gd`
- [x] T004 [US1] 校准 `scenes/main.tscn` 中 `WorldEnvironment` 的 `ProceduralSkyMaterial`，消除深灰下半球底色并对齐深海蓝与晨曦青白天际线
- [x] T005 [US1] 配置 `WorldEnvironment` 的大气距离透视雾（`fog_enabled`、`fog_depth_begin = 180.0m`、`fog_depth_end = 520.0m`），柔化遮罩瓦片边缘与方形转角在 `scenes/main.tscn`

---

## Phase 4: User Story 2 - 三平面赛璐璐手绘无缝贴图升级 (Priority: P1)

**Goal**: 升级 `terrain_triplanar.gdshader`，混合草甸、沙滩、风化崖壁贴图，世界坐标三平面投影彻底消除悬崖拉伸。

**Independent Test**: 运行 `tests/test_triplanar_shader.gd`，验证着色器成功绑定贴图采样器，草地、沙滩与悬崖在各高度与法线坡度下平滑过渡。

- [x] T006 [P] [US2] 编写三平面贴图着色器自动化验证脚本在 `tests/test_triplanar_shader.gd`
- [x] T007 [US2] 升级 `shaders/terrain_triplanar.gdshader`，实现草甸、沙滩、风化岩壁无缝贴图三平面投影混合与坡度过渡
- [x] T008 [US2] 在 `src/Terrain/TiledTerrainManager.cs` 中绑定高精度草地、泥沙与岩石贴图资源至三平面着色材质

---

## Phase 5: User Story 3 - Godot 编辑器内实时地形预览与 3D 资产地面吸附 (Priority: P2)

**Goal**: 为 `TiledTerrainManager` 与 `OceanWaterPlane` 增加 `[Tool]` 支持与编辑器轻量预览瓦片，打通拖拽 3D 模型一键地面吸附工作流。

**Independent Test**: 在 Godot 场景编辑器中打开 `scenes/main.tscn`，视口内即时呈现海岛与海水；拖拽 3D 模型点击地面吸附工具可精准贴合斜坡。

- [x] T009 [US3] 为 `src/Terrain/TiledTerrainManager.cs` 增加 `[Tool]` 特性与编辑器轻量级 3x3 瓦片生成模式，使地形在 Godot 编辑器视口即时可见
- [x] T010 [US3] 为 `src/Terrain/OceanWaterPlane.cs` 增加 `[Tool]` 特性，使大洋水面在 Godot 编辑器视口即时可见并支持吸附
- [x] T011 [US3] 验证在 Godot 编辑器中直接使用顶部“地面吸附”（Snap to Floor）工具将外部 3D 模型吸附至地形表面

---

## Phase 6: User Story 4 - 批量程序化植被散布 (Foliage MultiMesh) (Priority: P2)

**Goal**: 基于 `MultiMeshInstance3D` 实现千株卡通树木、野花簇单批次高效渲染（60 FPS），并依据高度与坡度生态自然分布。

**Independent Test**: 运行 `tests/test_foliage_scatter.gd`，验证 800+ 株植被实例全部生长于陆地地表，水下生成率为 0%，单批次 Draw Call 极小。

- [x] T012 [P] [US4] 编写植被散布高度贴合与生态过滤自动化测试脚本在 `tests/test_foliage_scatter.gd`
- [x] T013 [US4] 实现轻量级批量植被散布节点 `FoliageScatter`（基于 `MultiMeshInstance3D` 与 `IHeightSampler`）在 `src/Terrain/FoliageScatter.cs`
- [x] T014 [US4] 在主场景 `scenes/main.tscn` 挂载 `FoliageScatter`，散布千株卡通树木与草丛，验证单批次 60 FPS 渲染

---

## Phase 7: Polish & Verification

**Purpose**: 全量构建与回归自测

- [x] T015 [P] 执行工程 C# 全量编译构建验证在终端运行 `DOTNET_CLI_HOME=/tmp/dotnet_home dotnet build celestial.csproj`
- [x] T016 [P] 批量运行所有自动化测试套件在 `tests/`
- [x] T017 归档大地形环境与植被设计细节至 `docs/plan/open_world_terrain_and_streaming_architecture.md`
