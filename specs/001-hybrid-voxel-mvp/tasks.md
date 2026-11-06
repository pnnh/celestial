# Tasks: Hybrid 3D Terrain and Voxel World MVP

**Branch**: `001-hybrid-voxel-mvp`  
**Spec**: `specs/001-hybrid-voxel-mvp/spec.md`  
**Plan**: `specs/001-hybrid-voxel-mvp/plan.md`  

---

## Phase 1: Setup & Data Foundation (Sprint 1)

- [x] **TASK-001**: 初始化 Godot 4.7 Mono 项目脚手架与 C# 解决方案配置
  - 文件：`project.godot`, `celestial.csproj`, `celestial.sln`, `nuget.config`
  - 验证：`dotnet build` 编译成功，Godot headless 扫描通过。
- [x] **TASK-002**: 完善 Git LFS 与 `.gitignore` 规则，防止仓库体积膨胀
  - 文件：`.gitignore`, `.gitattributes`
  - 验证：运行 `git lfs install --local`，二进制资源正确匹配 LFS 过滤规则。
- [x] **TASK-003**: 初始化 Spec Kit 工作流与多 Agent (Antigravity & Claude) 协同集成
  - 文件：`.specify/`, `.agents/skills/`, `.claude/skills/`, `AGENTS.md`, `CLAUDE.md`, `.specify/memory/constitution.md`
  - 验证：Spec Kit 命令行工具就绪，项目规则与宪章文件建立。
- [x] **TASK-004**: 实现体素数据模型与坐标系统
  - 文件：`src/Voxel/VoxelType.cs`, `src/Voxel/ChunkCoord.cs`, `src/Core/Constants.cs`
  - 描述：定义体素类型枚举（Air, Dirt, Stone, Bedrock, Wood）及三维坐标索引哈希映射。
  - 验证：编写测试或断言确认局部与世界坐标相互转换正确无溢出。

---

## Phase 2: Terrain & Hybrid Voxel Meshing (Sprint 2)

- [x] **TASK-005**: 实现传统 3D 地形与高度采样器
  - 文件：`src/Terrain/IHeightSampler.cs`, `src/Terrain/StaticTerrain.cs`, `src/Terrain/SimpleProceduralHeightmap.cs`
  - 描述：提供连续函数或高度图采样的测试地面（凹凸泥土地/峡谷坑），支持 `GetHeight(x, z)`。
  - 验证：在场景中生成起伏地形网格与碰撞体，高度查询输出预期数值。
- [x] **TASK-006**: 实现 Chunk 容器与基础体素填充
  - 文件：`src/Voxel/VoxelChunk.cs`, `src/Voxel/VoxelWorld.cs`
  - 描述：实现 16×16×16 连续字节数组存储，结合高度采样器生成局部体素层，体素底部紧贴传统地形。
  - 验证：内存占用符合规范，Chunk 内方块状态正确初始化。
- [x] **TASK-007**: 实现面剔除网格生成器 (Face-Culling Mesher)
  - 文件：`src/Voxel/ChunkMesher.cs`
  - 描述：遍历方块只绘制朝向空气的外表面，生成顶点、法线、UV、索引并赋予 `ArrayMesh`。
  - 验证：渲染出清晰的体素方块外观，内部遮挡面被完全剔除。
- [x] **TASK-008**: 实现体素动态碰撞体同步与高度吸附 (Height Snapping)
  - 文件：`src/Voxel/ChunkMesher.cs`, `src/Voxel/VoxelChunk.cs`
  - 描述：根据渲染网格同步生成 `ConcavePolygonShape3D`，最底层体素顶点向下吸附至传统地形高度，消除黑缝。
  - 验证：角色站立在体素上无穿模，从传统地形走到体素地形无物理障碍。

---

## Phase 3: Player Interaction Loop (Sprint 3)

- [x] **TASK-009**: 实现第一人称角色控制器与视线射线检测
  - 文件：`src/Player/PlayerController.cs`, `src/Player/VoxelInteractor.cs`, `scenes/prefabs/player.tscn`
  - 描述：实现 WASD 移动、跳跃、重力、视角旋转与中心射线检测（获取点击目标坐标及法线）。
  - 验证：角色在测试场景中自由漫游并能精准瞄准方块表面。
- [x] **TASK-010**: 实现挖掘逻辑与触底传统地形阻隔
  - 文件：`src/Player/VoxelInteractor.cs`, `src/Voxel/VoxelWorld.cs`
  - 描述：左键挖掘方块；若目标体素 $Y \le \lfloor H(x, z) \rfloor$，判定为底层传统地形不可破坏，阻断挖掘。
  - 验证：连续下挖方块至传统地形表面时自动停止，无法击穿传统世界。
- [x] **TASK-011**: 实现放置逻辑与重叠防卡
  - 文件：`src/Player/VoxelInteractor.cs`, `src/Voxel/VoxelWorld.cs`
  - 描述：右键沿法线放置新方块；检测新方块位置是否与玩家自身胶囊碰撞体相交，相交则禁止放置。
  - 验证：可以向上或四周搭建方块平台，不会将自己卡进方块内部。
- [x] **TASK-012**: 实现跨 Chunk 边界更新联动
  - 文件：`src/Voxel/VoxelWorld.cs`
  - 描述：当在 Chunk 边缘（0 或 15）挖掘或放置时，自动标记相邻 Chunk 为 Dirty 并协同异步刷新网格。
  - 验证：跨边界挖掘不会在相邻 Chunk 产生漏光或镂空黑洞。

---

## Phase 4: Polish, Debugging & Delta Persistence (Sprint 4)

- [x] **TASK-013**: 实现调试可视化与性能监视器
  - 文件：`src/Core/DebugOverlay.cs`
  - 描述：绘制准心体素方块线框选择框（触底标红/正常标天蓝）、FPS、内存、角色坐标、Chunk 坐标与底层高度指标。按 F3 切换。
  - 验证：按 F3 切换调试信息面板显示，瞄准方块时显示精确的 3D 线框。
- [x] **TASK-014**: 实现轻量级差异存档与读取 (Delta Persistence)
  - 文件：`src/Persistence/ChunkDelta.cs`, `src/Persistence/SimpleSaveManager.cs`
  - 描述：仅保存与默认地形不一致的修改方块，支持 JSON 轻量序列化。支持 F5 快捷保存与 F9 快捷加载。
  - 验证：单元测试通过，挖掘和放置方块后按 F5，重新载入后修改完全保留。
- [x] **TASK-015**: 结合 Blender MCP 与 Ludo AI 丰富测试场景美术资产
  - 文件：`assets/models/ancient_monolith.glb`, `assets/textures/ludo/*.png`
  - 描述：通过在线 Blender MCP 程序化生成远古图腾柱（Ancient Monolith）并导出 GLB 资产，结合 Ludo AI 生成符文石、石砖、草地泥土与水晶镐图标，全部受 Git LFS 纳管。
  - 验证：Godot 正确导入 GLB 模型并将其摆放在传统 3D 地形上，验证混合世界资产共存。
- [x] **TASK-016**: 构建体素纹理图集并实现多面定向 UV 贴图与地形材质统一样式
  - 文件：`assets/textures/voxel_atlas.png`, `src/Voxel/ChunkMesher.cs`, `src/Voxel/VoxelWorld.cs`, `src/Terrain/StaticTerrain.cs`
  - 描述：构建 3×2 纹理图集并集成草皮、泥土、木板、古代石砖与符文基岩；在 `ChunkMesher` 中实现多面定向 UV 映射与简易 AO 明暗；底层传统地形绑定草地泥土贴图消除白模。
  - 验证：方块与地表告别纯色白模，呈现清晰游戏质感，单 Chunk 维持 1 个 Draw Call。
- [x] **TASK-017**: 实现游戏 HUD 操作指引、当前方块切换指示与装备栏图标
  - 文件：`src/Core/HUDOverlay.cs`, `src/Player/PlayerController.cs`
  - 描述：在屏幕展示按键指引，支持数字键 1-5 动态切换手持方块类型并在 HUD 实时响应，右下角渲染手持水晶镐装备图标。
  - 验证：按数字键 1~5 自由切换方块并在放置时生效，右下角水晶镐图标清晰可见。
- [x] **TASK-018**: 实现引擎内启动自测执行器 (In-Engine TestRunner)
  - 文件：`src/Core/TestRunner.cs`, `src/Core/GameManager.cs`
  - 描述：在场景就绪时自动执行核心坐标索引、高度图采样、跨界负数求余及差异存档 JSON 序列化自测。
  - 验证：游戏启动或 Headless 执行时输出全部绿灯自测日志，保障核心契约与底层数学无回归。

