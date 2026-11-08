# Quickstart Validation Guide: 多样化赛璐璐植被资产与树木物理阻挡

**Feature**: `005-stylized-flora-and-foliage-models`  
**Date**: 2026-11-08  
**Status**: Completed

---

## 1. 验证目标
本指南用于验证通过 Ludo AI 生成的 3 种树木（橡木、云杉、白桦）和 5 种地表花草资产，在导入 Godot 后能够：
1. 具备符合《Celestial》美术规范的日系手绘赛璐璐视觉质感；
2. 树木预制体拥有规范的 `StaticBody3D` 与 `CylinderShape3D`，且在 Godot 编辑器中 **0 告警**；
3. 玩家角色被树干阻挡，且无法在树干内放置方块（防穿模检测）；
4. `FoliageScatter.cs` 多变体批量散布器在大地图上按海拔自然呈现，且严格排除采石场体素区。

---

## 2. 自动化验证指令

### 2.1 C# 编译构建验证
确保所有新增模型配置结构体与散布类编译无误：
```bash
DOTNET_CLI_HOME=/tmp/dotnet_home dotnet build celestial.csproj
```
*预期输出*：`Build succeeded. 0 Error(s)`。

### 2.2 树木预制体与碰撞体规范自动化测试
运行无头模式验证树木预制体的节点层级与碰撞参数：
```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless -s tests/test_tree_prefabs.gd
```
*验证指标*:
- `oak_tree.tscn`、`spruce_tree.tscn`、`birch_tree.tscn` 均包含有效 `MeshInstance3D` 与 `StaticBody3D`；
- 所有 `CollisionShape3D` 的 `Scale` 严格等于 `(1, 1, 1)`；
- 碰撞体形状均为 `CylinderShape3D`，半径位于 $0.15\text{m} \sim 0.35\text{m}$，高度位于 $1.8\text{m} \sim 3.2\text{m}$。

### 2.3 多网格大世界生态散布自动化测试
运行多变体散布测试：
```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless -s tests/test_multi_foliage_scatter.gd
```
*验证指标*:
- 橡木主要分布于平原低地（5m ~ 28m）；
- 云杉主要分布于高山山脊（24m ~ 68m）；
- 白桦分布于过渡林缘（12m ~ 38m）；
- 采石场（`QuarryVolume`）及其 4m 缓冲带内植被数量为 0。

---

## 3. 编辑器与游戏内手动走查步骤

1. **Godot 编辑器视口走查**:
   - 打开 `scenes/main.tscn`；
   - 检查场景树中展开的树木节点，确认无黄色感叹号警告（⚠️）；
   - 在 3D 视口中查看平原上的橡木与向日葵、山坡上的白桦与山峰上的云杉，色彩过渡明快自然。
2. **游戏内交互走查 (F5 运行)**:
   - 角色走到树木旁边，身体紧贴树干，确认被坚固阻挡；
   - 抬头穿行于树荫枝叶下方，确认树冠不产生穿模碰撞卡顿；
   - 右键尝试在树干中心放置方块，观察控制台是否输出 `[放置受阻] 目标位置存在 3D 实体障碍物`。

