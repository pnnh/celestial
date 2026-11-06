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

- [ ] **TASK-013**: 实现调试可视化与性能监视器
  - 文件：`src/Core/DebugOverlay.cs`
  - 描述：绘制 Chunk AABB 线框、当前指针坐标、FPS 及网格重建耗时指标。
  - 验证：按 F3 切换调试信息面板显示。
- [ ] **TASK-014**: 实现轻量级差异存档与读取 (Delta Persistence)
  - 文件：`src/Persistence/ChunkDelta.cs`, `src/Persistence/SimpleSaveManager.cs`
  - 描述：仅保存与默认地形不一致的修改方块，支持 JSON 或二进制轻量存取。
  - 验证：挖掘和放置后按保存键，重启游戏后修改完全保留。
- [ ] **TASK-015**: 结合 Blender MCP 与 Ludo AI 丰富测试场景美术资产
  - 描述：使用 Blender MCP 生成精细的传统岩石和峡谷地形网格，使用 Ludo AI MCP 补充体素材质纹理并挂接测试。
  - 验证：传统地形与体素区域视觉过渡自然，资产经由 Git LFS 规范管理。

