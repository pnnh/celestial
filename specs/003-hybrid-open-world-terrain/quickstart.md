# Quickstart & Verification Guide: Hybrid Open-World Terrain

**Feature**: `003-hybrid-open-world-terrain`  
**Date**: 2026-11-07  
**Status**: Ready for Verification  

---

## 1. 验证目标 (Objectives)

验证千米级复合海岛高度场、64米瓦片流式加载与三级 LOD、普适物理基岩与 3D 表面放置吸附、以及局部体素卷边界封闭性。

---

## 2. 准备工作 (Prerequisites)

- **环境**: macOS 或 PC 桌面端，.NET 8.0 SDK，Godot Engine 4.7+ Mono。
- **构建工程**:
  ```bash
  DOTNET_CLI_HOME=/tmp/dotnet_home dotnet build celestial.csproj
  ```

---

## 3. 验证场景与执行步骤 (Validation Scenarios)

### Scenario 1: 千米级海岛高度场与海平面采样验证

- **目的**: 验证海岛中心平原、内陆山脊、海滩渐变与海平面的数学连续性与无缝性。
- **运行命令**:
  ```bash
  /Applications/Godot_mono.app/Contents/MacOS/Godot --headless --script tests/test_island_height_sampler.gd
  ```
- **预期结果**:
  - 中心原点 $(0, 0)$ 处标高位于微波平原区间 $Y \in [4\text{m}, 14\text{m}]$。
  - 东北山脊腹地存在顶峰，标高达到 $Y \in [40\text{m}, 80\text{m}]$。
  - 径向距离 $R > 480\text{m}$ 处，标高全部低于海平面（$Y < 0.0\text{m}$），沉入大洋。
  - 10,000 次随机连续采样中，高度变化斜率连续，无 $\text{NaN}$ 或数值突变。

---

### Scenario 2: 64m 瓦片流式加载与动态 LOD 切换

- **目的**: 验证瓦片管理器后台异步计算网格、主线程限流挂载与近/中/远三级 LOD 调度。
- **运行命令**:
  ```bash
  /Applications/Godot_mono.app/Contents/MacOS/Godot --headless --script tests/test_tiled_terrain_streaming.gd
  ```
- **预期结果**:
  - 观察者位于 $(0, 0)$ 时，活动瓦片数量维持在 $25 \sim 49$ 个。
  - 观察者以 $20\text{m/s}$ 速度向山脉方向移动时，新瓦片在后台线程池异步生成，主线程单帧最大消耗 $< 3.0\text{ms}$。
  - 距离观察者 $< 96\text{m}$ 的瓦片自动挂载物理碰撞体；远离的瓦片自动卸载并回收至对象池，无内存泄露。

---

### Scenario 3: 普适物理基岩与 3D 实体放置吸附

- **目的**: 验证对准 3D 静态实体（地面、大树干、石柱）无法破坏，右键可沿表面法线对齐放置方块。
- **运行命令**:
  ```bash
  /Applications/Godot_mono.app/Contents/MacOS/Godot --headless --script tests/test_bedrock_interaction.gd
  ```
- **预期结果**:
  - 射线命中 3D 树木碰撞体并尝试挖掘，返回 `HitType.Bedrock`，无体素被挖除，触发火星反馈。
  - 射线命中 3D 表面并尝试放置 Dirt 方块，成功在命中点外法线方向生成方块，并与整型世界网格对齐。

---

### Scenario 4: 局部体素卷（Voxel Volume）挖掘与封闭边界

- **目的**: 验证在局部体素卷内挖掘至 3D 地形表面时，侧壁与底部正确生成闭合网格，杜绝透光漏缝。
- **运行命令**:
  ```bash
  /Applications/Godot_mono.app/Contents/MacOS/Godot --headless --script tests/test_voxel_volume_meshing.gd
  ```
- **预期结果**:
  - 在泥土地幔层向下挖掘两格至 3D 基岩表面。
  - 检查生成的 Chunk 网格三角形：相邻方块侧面三角形生成正常，空洞数为 0，底层透视现象为 0。

---

## 4. 游戏内端到端交互运行 (In-Game E2E Run)

1. 启动游戏主场景：
   ```bash
   /Applications/Godot_mono.app/Contents/MacOS/Godot --path . scenes/main.tscn
   ```
2. 使用 `WASD` 在大岛上自由移动，按 `空格` 跳跃，观察海岛起伏、远山轮廓、浩瀚大洋与沙滩。
3. 走近采矿区体素层，手持镐子挖掘，观察火星阻隔与方块破坏；手持方块在 3D 石壁上搭梯子攀登。
4. 按 `F3` 打开调试监控，确认 FPS 稳定在 60，Draw Calls 与活动瓦片数处于平稳区间。

