# Implementation Plan: Player Character, Animation & Dual-Perspective Camera System

**Branch**: `002-player-character-and-camera`  
**Spec**: `specs/002-player-character-and-camera/spec.md`  
**Art Guide**: `docs/design/art_style_guide.md`  
**Constitution**: `.specify/memory/constitution.md`  

---

## 1. Technical Context & Subsystems

```
src/
├── Player/
│   ├── PlayerController.cs            # 玩家移动物理、视角输入、状态机协调与 V 键切换
│   ├── PlayerCameraController.cs      # 第一人称/第三人称平滑过渡与 SpringArm3D 避障
│   ├── CharacterAnimationController.cs# AnimationTree 状态机与移动参数混合 (Idle/Walk/Run/Crouch)
│   ├── ToolAttachmentManager.cs       # 左右手 BoneAttachment3D 与 3D 道具/微缩方块挂接
│   └── VoxelInteractor.cs             # 适配第一人称与第三人称准心射线拾取
scenes/
└── prefabs/
    ├── player.tscn                    # 升级后的角色预制体（含 CharacterVisual、SpringArm、武器槽）
    └── tools/
        └── pickaxe_prop.tscn          # 3D 握持水晶镐实体
assets/
└── models/
    └── character/
        ├── celestial_hero.glb         # Ludo AI Hero 模式生成的 3D 角色网格与骨骼 (Git LFS)
        └── animations/
            ├── idle.glb               # Ludo AI idle_neutral 原地循环动画 (Git LFS)
            ├── walk.glb               # Ludo AI walk_forward 原地循环动画 (Git LFS)
            ├── run.glb                # Ludo AI run_forward 原地循环动画 (Git LFS)
            └── crouch.glb             # Ludo AI crouch_walk 原地循环动画 (Git LFS)
```

---

## 2. Technical Approach & Contracts

### 2.1 双视角控制架构 (Dual-Perspective Camera Contract)

```
Player (CharacterBody3D, Layer=2)
├── CollisionShape3D (Capsule, 1.8m x 0.4m)
├── CameraPivot (Node3D, Y=1.55m)
│   ├── SpringArm3D (Length: 0.0m ~ 2.8m, CollisionMask: 1, Margin: 0.15m)
│   │   └── MainCamera (Camera3D, ShoulderOffset: (0.35, 0.15, 0))
│   └── FirstPersonRaycast (RayCast3D, Range: 6.0m)
└── CharacterVisual (Node3D)
    └── celestial_hero (GLB Model Instance)
        └── GeneralSkeleton (Skeleton3D)
            ├── BoneAttachment_RightHand (BoneAttachment3D: Bone="Hand.R")
            │   └── RightHandSlot (Node3D)
            └── BoneAttachment_LeftHand (BoneAttachment3D: Bone="Hand.L")
                └── LeftHandSlot (Node3D)
```

- **视角切换算法**：
  - 维护目标臂长 `TargetArmLength`（第一人称 0.0m，第三人称 2.8m）。
  - 在 `_PhysicsProcess` 中使用 `Mathf.Lerp(SpringArm.SpringLength, TargetArmLength, delta * 12.0f)` 实现平滑拉伸。
  - 第一人称模式下：当 `SpringLength < 0.2m` 时，激活头部 CullMask 隐藏角色头部，防止近裁面穿模；第三人称全显。

### 2.2 动画状态机设计 (Locomotion Animation Tree)

使用 Godot 的 `AnimationTree` 配合 `AnimationNodeStateMachine`：
- **主要状态**：
  - `Idle` (待机状态，速度 $\approx 0$)
  - `Locomotion` (移动混合：根据水平速度在 Walk 与 Run 之间线性插值)
  - `Crouch` (潜行状态：当检测到潜行键按住时触发)
- **过渡设置**：
  - 所有状态间采用 `0.15s` 交叉混合淡入淡出（Cross-Fade），消除动作生硬跳跃。
  - 所有行进动作均要求 Ludo AI 导出时开启 `in_place: true`，由引擎物理严格驱动速度。

### 2.3 手持工具挂载体系 (Tool Attachment Contract)

- 在 `Hand.R` 骨骼上实例化 `BoneAttachment3D`。
- `ToolAttachmentManager` 监听快捷栏切换：
  - 模式 1：手持主工具（水晶镐 `pickaxe_prop.tscn`，根据握柄调整 local transform：`RotationDegrees = (0, 90, -45)`）。
  - 模式 2：手持方块（动态生成 0.25 倍缩放的微型体素立方体，带有与世界相同质感的纹理）。

---

## 3. Constitution Check & Art Governance

- **原则 I & II（模块化与异步）**：相机控制、动画更新与体素世界物理互不污染，Ludo AI 资产拉取在构建期与准备期完成并纳入 LFS，运行时无远程阻塞。
- **原则 V（Git LFS 卫生）**：新建的 `assets/models/character/**/*.glb` 自动匹配现有 `.gitattributes` 中的 `*.glb filter=lfs` 规则。
- **原则 VI（赛璐璐手绘幻想风）**：
  - 提示词严格遵守 `docs/design/art_style_guide.md`；
  - 严禁包含希卡之眼、大师剑等任天堂版权元素。

---

## 4. Verification Plan

### 4.1 自动化与工程检查
- `dotnet build celestial.csproj` 持续 0 警告 0 错误。
- Headless 扫描与自测套件持续全绿。

### 4.2 交互验证
1. **双视角切换体验**：按 `V` 键验证 0.25 秒平滑过渡，在体素狭窄通道内贴墙测试防穿墙。
2. **机动动作连贯性**：WASD 行走、Shift 冲刺、Ctrl 潜行，验证步频与位移无滑步。
3. **装备握持稳定性**：左右手随动作自然挥动，无脱骨脱位现象。
