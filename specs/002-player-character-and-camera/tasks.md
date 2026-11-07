# Tasks: Player Character, Animation & Dual-Perspective Camera System

**Branch**: `002-player-character-and-camera`  
**Spec**: `specs/002-player-character-and-camera/spec.md`  
**Plan**: `specs/002-player-character-and-camera/plan.md`  

---

## Phase 1: Setup & Asset Generation Pipeline (Sprint 1)

- [x] **TASK-001**: 准备美术提示词并调用 Ludo AI 生成角色正面概念立绘
  - 依赖：`docs/design/art_style_guide.md`
  - 接口：`call_mcp_tool(ludo-ai, create3DModelImage)`
  - 产物：`assets/textures/character_concept.webp`
- [x] **TASK-002**: 调用 Ludo AI Hero 模式生成带骨骼绑定的 3D 模型并本地落地
  - 依赖：TASK-001
  - 接口：`call_mcp_tool(ludo-ai, create3DModel)` (model="hero", rig=true, joint_naming="godot", target_num_faces=30000)
  - 产物：`assets/models/character/celestial_hero.glb` (受 Git LFS 纳管，Godot 导入校验)
- [x] **TASK-003**: 调用 Ludo AI 提取 4 组基础机动原地动画
  - 依赖：TASK-002
  - 接口：`call_mcp_tool(ludo-ai, animate3DModelPreset)` (in_place=true, crop_loop=true)
  - 预设：`idle_neutral`, `walk_forward`, `run_forward`, `crouch_walk`
  - 产物：`assets/models/character/animations/*.glb` (受 Git LFS 纳管)

---

## Phase 2: Dual-Perspective Camera System (Sprint 2)

- [x] **TASK-004**: 实现双视角弹簧臂与相机平滑控制器
  - 文件：`src/Player/PlayerCameraController.cs`
  - 描述：管理第一人称与第三人称平滑插值（Lerp），配置 `SpringArm3D` 半径 0.15m、长度 2.8m 及过肩偏移 `(0.35, 0.15, 0)`，实现环境防穿墙。
- [x] **TASK-005**: 在角色控制器中集成视角按键与第一人称头部防穿模遮罩
  - 文件：`src/Player/PlayerController.cs`
  - 描述：监听按键 `V` 与滚轮切换视角；当相机处于第一人称极近距离（$<0.2m$）时，自动剔除角色头部网格显示（CullMask），保留手部与身体。
- [x] **TASK-006**: 重构玩家预制体场景结构
  - 文件：`scenes/prefabs/player.tscn`
  - 描述：重构节点树，引入 `CameraPivot`、`SpringArm3D`、`Camera3D` 及 `CharacterVisual` 节点槽位。

---

## Phase 3: Character Model & Locomotion Animation Tree (Sprint 3)

- [x] **TASK-007**: 将 3D 角色网格与骨骼装配至角色场景
  - 文件：`scenes/prefabs/player.tscn`, `assets/models/character/celestial_hero.glb`
  - 描述：将 Hero 模式模型作为 `CharacterVisual` 挂入，调整轴向与胶囊碰撞体高度对齐，设置赛璐璐卡通材质。
- [x] **TASK-008**: 组装 `AnimationPlayer` 与 `CharacterAnimationController` 状态机
  - 文件：`src/Player/CharacterAnimationController.cs`, `scenes/prefabs/player.tscn`
  - 描述：载入 4 组动画片段，构建包含 Idle、Walk、Run、Crouch 的状态机混合树，配置 0.15s 交叉淡入淡出。
- [x] **TASK-009**: 联动角色物理移动速度与潜行状态
  - 文件：`src/Player/PlayerController.cs`, `src/Player/CharacterAnimationController.cs`
  - 描述：根据角色实际位移速度驱动动画混合参数，按住 Ctrl 切换潜行姿态，验证移动步频与物理位移无滑步。

---

## Phase 4: Dynamic Tool Attachment & Interaction Polish (Sprint 4)

- [x] **TASK-010**: 实现手持装备槽骨骼挂载管理器
  - 文件：`src/Player/ToolAttachmentManager.cs`
  - 描述：在右手骨骼 `Hand.R` 与左手骨骼 `Hand.L` 上配置 `BoneAttachment3D`，实现工具动态插拔与变换定位。
- [x] **TASK-011**: 制作 3D 水晶镐握持预制体与微型体素手持渲染
  - 文件：`scenes/prefabs/tools/pickaxe_prop.tscn`, `src/Player/ToolAttachmentManager.cs`
  - 描述：基于既有资产制作右手握持的 3D 水晶镐网格，并支持在切换 1~5 键时在掌心渲染迷你体素方块。
- [x] **TASK-012**: 统一双视角下的准心射线拾取与挖掘/放置交互
  - 文件：`src/Player/VoxelInteractor.cs`
  - 描述：确保在第一人称与第三人称过肩视角下，屏幕准心射线均能精准拾取体素方块表面与法线，挖掘与放置手感一致。
- [x] **TASK-013**: 全流程验证、编译与自测确认
  - 验证：`dotnet build` 0 警告 0 错误，Headless 场景加载通过，手动运行验证第三人称奔跑、潜行与手持武器效果。

---

## Phase 5: Gameplay Polish & In-Place Defect Convergence (Sprint 5)

- [x] **TASK-014**: 第一人称视锥与第三人称全身模型完全解耦
  - 文件：`src/Player/PlayerCameraController.cs`, `src/Player/ToolAttachmentManager.cs`
  - 描述：第一人称下将全身模型设为 `ShadowsOnly`（仅投射地面阴影，摄像机不渲染空心颈部与身体穿模），隐藏第三人称道具；并在摄像机上引入第一人称专属风格化冒险者手臂与手持物，彻底消除双重方块与惊悚颈部断口。
- [x] **TASK-015**: 第三人称道具挂载点精准校准
  - 文件：`src/Player/ToolAttachmentManager.cs`
  - 描述：挂载点优先锁定至手掌心中心骨骼 `bone_17`；校准迷你方块（0.11m）与水晶镐的握持旋转与局部偏移，彻底消除嵌入小臂与髋部穿模。
- [x] **TASK-016**: 跑步动画根骨骼位移清洗与移动物理加速度重构
  - 文件：`src/Player/CharacterAnimationController.cs`, `src/Player/PlayerController.cs`
  - 描述：在动画加载重定向时，过滤 `run.glb` 与 `crouch.glb` 中 Hips 根骨骼的水平（X/Z）位移 track，仅保留自然垂直 Y 轴颠簸；修正跑步触发条件（仅在产生移动输入时方可加速，静止按 Shift 维持 Idle），重构速度平滑逼近计算，彻底解决按下 Shift 瞬间向前瞬移穿入体素泥土的物理 Bug。
- [x] **TASK-017**: 自动化回归自测与构建验证
  - 验证：执行 `dotnet build` 0 错误，运行 Headless 回归测试校验双视角隔离、骨骼挂载与根骨骼位移清洗全部通过。
- [x] **TASK-018**: 角色足底接地贴合微调
  - 文件：`scenes/prefabs/player.tscn`, `src/Player/PlayerController.cs`
  - 描述：微调模型局部 Y 轴高度从 0.925m 至 0.865m，消除胶囊体球形底部在地面上导致的足底悬空缝隙，使角色双脚稳固踩在草地表面。
- [x] **TASK-019**: 第一人称手臂流线一体化重构与混淆色消除
  - 文件：`src/Player/ToolAttachmentManager.cs`
  - 描述：将第一人称手臂从散落的阶梯方块重构为同轴一体化微锥形小臂模型；腕部护腕改用深黑色皮革细环，彻底消除其与泥土方块视觉相似导致的“两个方块”误解；小臂与手掌统一定义日系二次元动漫肤色，呈现平滑自然的完整手臂握持外观。
- [x] **TASK-020**: 第一人称与第三人称手持方块尺寸分化与穿模覆盖
  - 文件：`src/Player/ToolAttachmentManager.cs`
  - 描述：第一人称手持方块适度放大至 0.25m，既完全覆盖掌心微小穿模，又大幅缩小与放置后 1.0m 世界体素的尺寸骤变感；第三人称设定为 0.14m，稳固托于右手侧，杜绝碰触腰跨裙摆。
- [x] **TASK-021**: 待机动画（Idle）骨盆摆钟与双脚悬空抬起修复
  - 文件：`src/Player/CharacterAnimationController.cs`
  - 描述：清洗 `idle.glb` 中 Hips 根骨骼左右 13cm 的横向位移与侧倾（Roll/Yaw）旋转摇摆，锁定下身双腿与双脚至水平平稳着地姿态；100% 保留胸腔（Spine/Chest）、肩部、头颈部的自然呼吸与生机微动，彻底消除“腰部被吊起如钟摆般晃动轮流离地”的不真实感。
- [x] **TASK-022**: 第一人称手持方块写实托持姿态与图集纹理升级
  - 文件：`src/Player/ToolAttachmentManager.cs`
  - 描述：解耦道具与小臂轴线，在手掌掌心与托底四指前端设立专属挂载锚点 `FP_ItemMount`；重构手部构造（手掌掌心 + 微屈托底四指 + 侧后方大拇指支撑），手腕向身体内侧翻掌内旋 34°；第一人称方块设定为 0.20m 等轴测展示角，引入与场景体素世界一致的 Texture Atlas 6 面贴图与简易 AO 阴影，手掌与手指紧密贴合方块右下后侧，彻底消除“小臂插在方块内部”的违和感。
- [x] **TASK-023**: 体素地质层实心化填充与边界级联重构（彻底根治挖掘后侧面透视 3D 地形）
  - 文件：`src/Voxel/VoxelWorld.cs`, `src/Voxel/VoxelChunk.cs`
  - 描述：根治挖掘后侧面缺失的两大深层缺陷：
    1. 修正越界方块判定：将超出体素 Chunk 范围的 `GetVoxel` 从错误的伪造 Bedrock 修正为标准的 Air，消除了原本因误判邻居实心而错误剔除的整整 564 处体素边界外表面，使体素世界边界拥有完整坚固垂直封闭墙面；
    2. 修复跨 Chunk 边界更新失效：重构 `MarkBorderNeighborsDirty` 为 `MarkChunkDirty` 并显式置位 `neighborChunk.IsDirty = true`，移除 `RebuildMesh` 中阻断重建的多余拦截，确保边界挖掘后相邻 Chunk 立即级联刷新露出新侧面（diff 由 0 升至正确生成的面数）；
    3. 实心填充地下基岩：将 `wy < baseTerrainY` 填充为不可破坏的 `Bedrock`，彻底根除地下空气空洞与触底穿帮。
