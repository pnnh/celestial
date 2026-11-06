# Feature Specification: Player Character, Animation & Dual-Perspective Camera System

**Feature Branch**: `002-player-character-and-camera`  
**Created**: 2026-11-06  
**Status**: Ready for Planning  
**Input**: 用户需求 - 调用 Ludo AI MCP 生成 Hero 模式 3D 角色模型，实现自动骨骼与预设动画（待机、走路、跑步、潜行），支持第一人称与第三人称双视角切换，实现左右手持工具效果，遵循日系赛璐璐幻想美术风格规范。  

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 第一人称与第三人称（后背过肩）双视角无缝切换 (Priority: P1)

作为一名玩家，我在游戏中能够按 `V` 键或滑动鼠标滚轮，在第一人称沉浸视角（FPS）与第三人称过肩后背视角（TPS）之间平滑过渡。在第三人称下可以全方位观察角色身姿、动作与周围环境；在第一人称下可以精准瞄准方块进行挖掘与建造。

**Why this priority**: 视角是连接玩家与角色模型的前提桥梁。没有完善的双视角与防穿墙弹簧臂系统，后续的角色渲染与动作表现将无从体现。

**Independent Test**:
- 启动场景，默认处于第一人称视角；按 `V` 键平滑拉远至第三人称过肩视角，角色的背影与侧面清晰可见。
- 在狭小通道或紧贴墙体时切换第三人称，弹簧臂（`SpringArm3D`）自动缩短，摄像机绝不穿透方块或地形进入虚空。
- 再次按 `V` 键，视角瞬间拉回第一人称头部位置，头部模型自动剔除，无近距离穿模。

**Acceptance Scenarios**:
1. **Given** 玩家处于测试场景中，**When** 按下 `V` 键，**Then** 相机位置在 0.25 秒内平滑过渡至角色右肩后方 2.8 米处，视野（FOV）保持稳定。
2. **Given** 玩家背靠高耸的体素墙壁，**When** 切换至第三人称，**Then** 弹簧臂碰撞检测触发，相机停留在方块外缘，避免穿墙黑屏。
3. **Given** 玩家切换回第一人称，**When** 低头观察脚下，**Then** 能清晰看到角色身躯与腿部，但摄像机近平面不被头部遮挡。

---

### User Story 2 - 赛璐璐风 3D 角色模型与人形骨骼装配 (Priority: P1)

作为一名玩家，我在第三人称下看到的主角是一名身着亚麻探险风衣、皮革腰带的年轻冒险者，美术呈现为日系手绘赛璐璐幻想风格（Ghibli / 旷野之息启发），具有清晰的剪影和鲜明色彩，与当前的体素与传统 3D 地形高度融合。

**Why this priority**: 验证 Ludo AI Hero 模式高精度建模质量，以及基于 Godot 命名的人形骨骼能否在引擎内稳定运转。

**Independent Test**:
- 导入生成的 `assets/models/character/celestial_hero.glb`，在 Godot 编辑器与运行时检查材质渲染，无贴图错误与面法线反转。
- 检查其 `Skeleton3D` 节点，确认包含标准骨骼链（`Hips`、`Spine`、`Chest`、`Neck`、`Head`、`Hand.R`、`Hand.L`、双腿骨骼）。

**Acceptance Scenarios**:
1. **Given** 场景加载完成，**When** 摄像机环绕角色观察，**Then** 角色材质呈现二阶柔和卡通着色，服装与饰物具备清晰手绘质感，符合 `docs/design/art_style_guide.md` 规范。
2. **Given** 角色模型挂载至 `PlayerController` 下，**When** 角色在复杂体素地形上跳跃或下落，**Then** 整体包围盒胶囊体碰撞准确，角色模型紧贴地面。

---

### User Story 3 - 基础机动动作与动画状态机 (Priority: P2)

作为一名玩家，我控制角色移动时，角色能根据运动状态自动播放自然流畅的动作：静止时平稳待机（Idle）、行走时稳健踏步（Walk）、按住 Shift 疾跑时倾身冲刺（Run）、按住 Ctrl 时压低身姿轻声潜行（Crouch/Sneak）。

**Why this priority**: 动画赋予角色生命力，且 In-place 动画能保证角色在沙盒复杂地形移动时绝不出现滑步。

**Independent Test**:
- 角色静止不动，播放呼吸起伏的待机循环（Idle）。
- 按下 WASD 键盘移动，角色双腿交替迈步进入 Walking 动作。
- 同时按住 Shift，动作平滑过渡为快速摆臂的 Running 动作。
- 按住 Ctrl 键移动，角色身体下蹲，进入潜行挪步状态。

**Acceptance Scenarios**:
1. **Given** 角色从静止开始奔跑，**When** 速度达到阈值，**Then** `AnimationTree` 状态机在 0.15 秒内完成从 Idle 到 Run 的交叉淡入淡出（Cross-fade），无动作瞬移卡顿。
2. **Given** 角色在原地待机或移动，**When** 观察脚底与地面的接触点，**Then** 角色物理速度与动作步频保持一致，无肉眼可见的滑步。

---

### User Story 4 - 左右手持工具动态挂载 (Priority: P2)

作为一名玩家，当我装备水晶镐或切换不同方块时，角色的右手能紧握对应的 3D 工具/道具，并随着挥臂、奔跑或待机动作自然摆动；副手（左手）预留标准挂载槽，便于未来持握火把或盾牌。

**Why this priority**: 打通角色骨骼系统与游戏核心玩法道具库（挖掘工具、建造方块、照明火把）之间的物理与视觉连接。

**Independent Test**:
- 角色右手骨骼节点下生成 `BoneAttachment3D`，挂载当前手持物模型。
- 当角色播放待机、奔跑或挖掘动作时，手持水晶镐随手掌骨骼一同晃动，空间位置不发生分离漂移。

**Acceptance Scenarios**:
1. **Given** 玩家手持水晶镐，**When** 切换至第三人称并移动，**Then** 水晶镐握柄精准固定在角色右掌心，随手臂前后摆动。
2. **Given** 玩家在第一人称视角下，**When** 点击左键挖掘，**Then** 能在屏幕右侧看到手持工具做出下砸挖掘挥击的视觉反馈。

---

## Edge Cases

- **狭小空间相机穿透**：玩家在仅有 1 格宽高的体素洞穴中切换第三人称 -> `SpringArm3D` 半径碰撞自动将相机推至极近距离（甚至退化为特写），避免穿入周围体素内部。
- **高速切换移动方向（左右乱晃）**：快速交替按 A/D 键导致动画频繁切换撕裂 -> 在动画混合树（BlendTree）中加入平滑时间常数（Damping / Blend Time 0.15s）。
- **工具挂载点偏移**：不同模型的握柄轴心不一致导致穿透手掌 -> 在 `ToolSlot` 下提供局部偏移调整变换（`PositionOffset` 与 `RotationOffset`），确保各类道具握持自然。

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系统必须提供第一人称与第三人称双视角支持，按 `V` 键或滚轮可平滑过渡，第三人称配备防穿墙弹簧臂（`SpringArm3D`）。
- **FR-002**: 角色 3D 模型必须采用 Ludo AI Hero 模式生成，严格遵循 `docs/design/art_style_guide.md` 中的赛璐璐手绘幻想风与防侵权规避清单。
- **FR-003**: 角色必须具备绑定完整的通用人形骨骼（`Skeleton3D`），且遵循 Godot 标准关节命名。
- **FR-004**: 系统必须接入 4 组基础循环骨骼动画：待机（Idle）、行走（Walk）、跑步（Run）、潜行（Crouch/Sneak），且所有行进动作必须为原地动画（`in_place: true`）。
- **FR-005**: 必须使用 Godot 的 `AnimationTree` 状态机实现动作平滑过渡，支持行走与跑步的速度自适应混合。
- **FR-006**: 角色右手骨骼必须配置 `BoneAttachment3D`（右手装备槽），支持动态实例化当前选定的 3D 工具或微缩方块。
- **FR-007**: 第一人称模式下必须通过相机 `CullMask` 或材质通道遮罩角色头部网格，杜绝面部内侧穿模破面。

### Key Entities

- **PlayerCharacterModel**: 角色视觉模型容器，包含从 GLB 导入的网格、骨骼与材质。
- **SpringArmPivot**: 第三人称过肩相机的弹性支架，负责视线防遮挡与距离缩放。
- **CharacterAnimationManager**: 负责驱动 `AnimationPlayer` 与 `AnimationTree`，根据 `PlayerController` 的移动速度和姿态切换动作状态。
- **ToolSlotAttachment**: 绑定在左右手骨骼（`Hand.R` / `Hand.L`）上的挂载节点，负责响应玩家背包/快捷栏的工具切换并同步变换。

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 双视角（FPS/TPS）切换动画平滑时间控制在 0.2 ~ 0.3 秒，过程掉帧为 0。
- **SC-002**: 角色模型在各种移动状态下，脚步与地面的同步无肉眼可见滑步（Foot Sliding）。
- **SC-003**: 角色在 100 次狭窄方块通道进出测试中，第三人称相机穿入方块内部黑屏率为 0%。
- **SC-004**: 左右手持工具在 50 次跑动、跳跃、挥动测试中，工具模型与手骨脱节或偏移率为 0%。
- **SC-005**: 角色模型与其动画资源通过 Git LFS 纳管，Git 仓库本体增量小于 50 KB。

---

## Assumptions

- 角色生成使用本地配置的 Ludo AI MCP 服务完成，生成的 GLB 资产立即落盘并在本地通过 Git LFS 纳管。
- 角色控制保持原有的第一人称核心物理参数（重力、速度、跳跃高度），第三人称视角不破坏原有的体素挖掘与放置射线交互。

