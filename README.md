# Celestial: Hybrid 3D Terrain + Voxel Engine
# Celestial：传统3D地形与局部体素混合世界引擎

[English](#english) | [中文说明](#chinese)

---

<a name="english"></a>
## English

### 1. Overview
**Celestial** is an experimental open-world game prototype built with **Godot 4.7 Mono (C# / .NET 8)**. It pioneers a **hybrid world architecture**:
- **Macro Static 3D World**: High-fidelity, fixed 3D terrain meshes (similar to *Conan Exiles* or Souls-like environments) that offer rich artistic detail, atmospheric storytelling, and stable performance.
- **Micro Editable Voxel Layer**: In specific gameplay zones (such as canyons, mud basins, sand pits, or mining sites), dynamic voxel chunks (16×16×16) are overlaid on top of the traditional terrain. Players can freely excavate, build bases, and reshape the landscape.
- **Bedrock Boundary Locking**: Digging downwards naturally stops when reaching the surface of the underlying traditional 3D terrain, preserving the macroscopic level structure and preventing players from falling through the world.

---

### 2. Key Features

- **Hybrid Layer Alignment & Snapping**:
  - The underlying traditional 3D terrain exposes a continuous height sampler interface (`IHeightSampler`).
  - Voxel chunk columns dynamically sample terrain heights to align their bases seamlessly to the underlying surface, preventing visual gaps or floating geometry.
- **Bedrock Collision & Dig Interruption**:
  - Whenever a player attempts to dig a block whose height is less than or equal to the traditional terrain elevation, the voxel engine marks it as indestructible bedrock and blocks the destruction.
- **High-Performance C# Voxel Pipeline**:
  - 16×16×16 chunk volume (4,096 voxels per chunk) stored in contiguous byte arrays (4KB ~ 8KB RAM per chunk).
  - Face-culling mesher generates clean `ArrayMesh` buffers (vertices, normals, UVs, ambient occlusion shading).
  - Dynamic `ConcavePolygonShape3D` collision synchronization sharing Collision Layer 1 (Environment) with traditional terrain.
- **First-Person Sandbox Interaction**:
  - Full first-person controller with physics-based gravity, jumping, and mouse look.
  - Precision center raycast picker.
  - Placement protection prevents players from embedding blocks inside their own collision capsules.
  - Cascade dirty-chunk updates ensure no hollow mesh artifacts when digging across chunk borders.
- **Delta Persistence (Lightweight Save System)**:
  - Only records diffs (modifications made by the player) instead of storing entire chunks.
  - Fast JSON serialization (`user://celestial_delta_save.json`) with `F5` Quick Save and `F9` Quick Load.
- **Real-Time Debug & Selection Box**:
  - Press `F3` to toggle the performance monitor (FPS, process time, memory, player coordinates, chunk indices).
  - 3D bounding wireframe snaps to targeted blocks in real time (cyan for diggable blocks, red warning for bedrock boundaries).
- **Asset Pipeline & Git LFS Hygiene**:
  - Integrated with **Blender MCP** to procedurally model 3D props (e.g. `ancient_monolith.glb`) directly into the traditional terrain.
  - Binary assets (`.glb`, `.png`, `.wav`, etc.) strictly tracked by Git LFS to prevent repository bloat.
- **Spec-Kit Workflow**:
  - Fully scaffolded with GitHub Spec Kit, incorporating project constitutions, specifications, and multi-agent synergy for both **Antigravity** and **Claude Code**.

---

### 3. Controls & Gameplay Guide

| Key / Input | Action | Description |
|:---|:---|:---|
| **W, A, S, D** | Move | Move character relative to camera orientation |
| **Space** | Jump | Jump when grounded (`OnFloor`) |
| **Mouse Motion** | Look | Rotate camera view pitch and yaw |
| **Escape (ESC)** | Toggle Mouse | Release or re-capture mouse cursor |
| **Left Mouse Button** | Mine / Dig | Break targeted voxel block (stops at traditional terrain) |
| **Right Mouse Button** | Place | Place selected voxel block on adjacent surface |
| **1 ~ 5 Keys** | Select Block | `1`: Dirt, `2`: Stone, `3`: Wood, `4`: Planks, `5`: Sand |
| **F3** | Debug Monitor | Toggle diagnostic overlay and 3D selection wireframe |
| **F5** | Quick Save | Save world modifications to JSON delta save file |
| **F9** | Quick Load | Restore world modifications from JSON delta save file |

---

### 4. Project Structure

```
celestial/
├── project.godot                  # Godot 4.7 engine configuration
├── celestial.csproj               # C# project file (.NET 8 / Godot.NET.Sdk 4.7.2)
├── celestial.sln                  # Classic Visual Studio solution
├── nuget.config                   # Local bundled Godot NuGet package source
├── .gitattributes                 # Git LFS rules for 3D models and textures
├── .gitignore                     # Strict rules excluding engine cache and build outputs
├── AGENTS.md                      # AI Agent behavioral guidelines (Antigravity)
├── CLAUDE.md                      # AI Agent behavioral guidelines (Claude Code)
├── scenes/
│   ├── main.tscn                  # Assembled main testing world scene
│   └── prefabs/
│       └── player.tscn            # First-person character prefab
├── src/
│   ├── Core/
│   │   ├── Constants.cs           # Chunk dimensions and direction constants
│   │   ├── GameManager.cs         # Entry coordinator and scene bootstrapping
│   │   ├── HUDOverlay.cs          # Reticle and on-screen control HUD
│   │   ├── DebugOverlay.cs        # F3 diagnostic panel & 3D wireframe highlighter
│   │   └── TestRunner.cs          # Automated math and coordinate unit test suite
│   ├── Terrain/
│   │   ├── IHeightSampler.cs      # Terrain height & normal sampling interface
│   │   ├── SimpleProceduralHeightmap.cs # Procedural canyon & hill terrain generator
│   │   └── StaticTerrain.cs       # 3D static terrain mesh and collision node
│   ├── Voxel/
│   │   ├── VoxelType.cs           # Voxel type enumeration (byte) & attributes
│   │   ├── ChunkCoord.cs          # 3D chunk coordinates & indexing math
│   │   ├── VoxelChunk.cs          # 16x16x16 chunk data and mesh instance node
│   │   ├── ChunkMesher.cs         # Face-culling ArrayMesh & collision generator
│   │   └── VoxelWorld.cs          # Global voxel coordinate & border updater
│   ├── Player/
│   │   ├── PlayerController.cs    # Movement, jumping, gravity, and mouse look
│   │   └── VoxelInteractor.cs     # Raycast picking, dig blocking & block placement
│   └── Persistence/
│       ├── ChunkDelta.cs          # Chunk modification diff data model
│       └── SimpleSaveManager.cs   # JSON delta save/load manager
├── assets/
│   ├── models/                    # 3D meshes (e.g. ancient_monolith.glb via Blender MCP)
│   ├── textures/                  # 16x16 pixel textures (dirt, stone, grass, etc.)
│   ├── materials/                 # Shader and spatial material resources
│   └── audio/                     # Sound effects and background music
├── docs/
│   ├── hybrid_voxel_game_requirements.md # Original design document
│   └── plan/                      # Feasibility study and technical blueprints
└── specs/
    └── 001-hybrid-voxel-mvp/      # Spec Kit specifications, plans, and tasks
```

---

### 5. Build and Run

#### Prerequisites
- [Godot Engine 4.7+ Mono Edition](https://godotengine.org) (macOS / Windows / Linux)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Git LFS (`git lfs install`)

#### Building the C# Assembly
```bash
# Set DOTNET_CLI_HOME if needed
export DOTNET_CLI_HOME=/tmp/dotnet_home

# Restore dependencies
dotnet restore celestial.csproj

# Build solution
dotnet build celestial.csproj
```

#### Running the Game
Open the project directory in **Godot 4.7 Mono** and press **F5** (or run `scenes/main.tscn`), or run via CLI:
```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot scenes/main.tscn
```

---
---

<a name="chinese"></a>
## 中文说明

### 1. 项目概述
**Celestial** 是一个基于 **Godot 4.7 Mono (C# / .NET 8)** 构建的实验性开放世界游戏原型，核心探索一种**“混合世界架构”（Hybrid World Architecture）**：
- **宏观传统 3D 世界**：采用传统固定 3D 地形与高精度场景模型（类似《流放者柯南》或魂类游戏关卡），承载丰富的叙事氛围、精美的美术细节与极佳的宏观渲染性能。
- **微观局部体素编辑层**：在特定区域（如大峡谷、泥泞沼泽、沙丘盆地、采矿场等）动态叠加一层体素网格（Voxel Layer）。玩家可以在体素层内自由挖掘、采集矿物与建造基地。
- **基岩边界阻隔机制**：当玩家在体素层垂直向下挖掘至传统 3D 地形表面时，系统自动判定为不可破坏的“基岩”，强制终止下挖，从而有效保护底层固定关卡结构，彻底杜绝穿模掉出世界的异常。

---

### 2. 核心特性

- **高度采样与平滑贴合 (Height Snapping)**：
  - 底层传统地形提供连续高度采样契约 (`IHeightSampler`)。
  - 体素分块在初始化时根据传统地形的高度函数自动贴合表面生成，消除悬空断层与镂空漏缝。
- **触底基岩物理与挖掘阻隔**：
  - 挖掘判定严格比对目标体素坐标与底层地形高度：若 $Y \le \lfloor H(x, z) \rfloor$，则触发基岩判定阻断破坏，并给予视觉/操作反馈。
- **高性能 C# 体素运算管线**：
  - 标准 16×16×16 分块（单 Chunk 包含 4,096 个体素），连续 `byte` 数组紧凑表示（单 Chunk 数据量仅 4KB~8KB）。
  - 面剔除（Face Culling）网格生成器：仅渲染暴露于空气中的可见面，自动计算环境遮挡（AO）明暗。
  - 物理多边形同步：网格生成后即时同步更新 `ConcavePolygonShape3D`，与传统地形共享环境碰撞层（Layer 1）。
- **第一人称沙盒核心循环**：
  - 完整的第一人称物理控制器（WASD 移动、跳跃、重力、平滑视角旋转与 ESC 鼠标捕获）。
  - 中心射线精确拾取。
  - 建造防卡模检测：放置新方块前进行 AABB 包围盒相交测试，防止将角色卡入方块体内。
  - 跨 Chunk 边界更新广播：在边缘（0 或 15）操作时自动级联触发相邻 Chunk 重新网格化，杜绝漏光镂空。
- **轻量级差异存档 (Delta Persistence)**：
  - 仅存储玩家实际改动过的方块差异（Delta Diff），不持久化庞大的未修改世界数据。
  - 支持快捷键：`F5` 快速保存世界改动，`F9` 快速载入世界改动。
- **实时调试与 3D 线框选择框**：
  - 按 `F3` 随时开启/关闭性能诊断面板（查看实时 FPS、帧耗时、静态内存占用、角色坐标与 Chunk 索引）。
  - 准心体素方块线框选择框：对准方块时以 3D 边框实时吸附（触底不可挖掘呈现**红色线框**，正常可破坏方块呈现**天蓝色线框**）。
- **外部工具集成 (MCP) 与 Git LFS 治理**：
  - 联动本地 **Blender MCP** 程序化生成远古图腾柱雕塑（`ancient_monolith.glb`）并实装在传统地形上，验证 3D 道具与体素层共存。
  - 所有 3D 模型（`.glb`）、纹理贴图（`.png`）严格纳入 **Git LFS** 托管，杜绝仓库体积膨胀。
- **规范驱动开发 (Spec-Kit)**：
  - 集成 GitHub Spec Kit，配备项目宪章 (`constitution.md`)、需求规格说明 (`spec.md`)、技术规划 (`plan.md`) 与任务清单 (`tasks.md`)，原生支持 **Antigravity** 与 **Claude Code** 双 Agent 协作开发。

---

### 3. 操作与玩法指南

| 按键 / 操作 | 功能名称 | 说明 |
|:---|:---|:---|
| **W, A, S, D** | 角色移动 | 沿摄像机朝向水平前后左右移动 |
| **空格 (Space)** | 角色跳跃 | 仅在着地状态（`OnFloor`）下允许跳跃 |
| **鼠标移动** | 视角旋转 | 控制摄像机水平偏航（Yaw）与俯仰（Pitch） |
| **ESC 键** | 鼠标锁定切换 | 释放或重新捕获鼠标指针 |
| **鼠标左键** | 挖掘体素方块 | 破坏瞄准的方块（挖到底层传统地形时自动被基岩阻隔） |
| **鼠标右键** | 放置方块 | 沿瞄准面的法线方向生成新方块（具备角色防卡模检测） |
| **数字键 1 ~ 5** | 切换方块类型 | `1`: 泥土, `2`: 石头, `3`: 原木, `4`: 木板, `5`: 沙子 |
| **F3 键** | 调试监控面板 | 切换诊断监控覆盖层显示，准心自动高亮目标方块 3D 线框 |
| **F5 键** | 快速保存世界 | 将世界差异修改保存至本地 JSON 文件 |
| **F9 键** | 快速读取世界 | 重新读取存档并还原被修改的体素分块 |

---

### 4. 构建与运行说明

#### 环境要求
- [Godot Engine 4.7+ Mono 版本](https://godotengine.org) (macOS / Windows / Linux)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Git LFS (`git lfs install`)

#### 依赖还原与编译
```bash
# 如在沙箱或隔离环境下可指定 DOTNET_CLI_HOME
export DOTNET_CLI_HOME=/tmp/dotnet_home

# 还原依赖项（内置包源已在 nuget.config 配置）
dotnet restore celestial.csproj

# 编译 C# 解决方案
dotnet build celestial.csproj
```

#### 启动游戏
使用 **Godot 4.7 Mono** 打开当前项目根目录，点击运行或运行 `scenes/main.tscn`；或通过命令行直接拉起：
```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot scenes/main.tscn
```

---

### 5. 许可证与开发规范

- 本项目采用 Spec-Driven Development 流程进行演进。
- 遵循严谨的 Git 提交审查规则，AI Agent 每次任务后由开发者检验确认后再进行提交。

