# Implementation Plan: 多样化赛璐璐植被资产与树木物理阻挡 (005-stylized-flora-and-foliage-models)

**Branch**: `main` | **Date**: 2026-11-08 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-stylized-flora-and-foliage-models/spec.md`

---

## 1. Summary

利用 Ludo AI MCP 资产管线生成符合日系清爽手绘赛璐璐美术规范（`docs/design/art_style_guide.md`）的 3 种典型树木模型（橡木、云杉、白桦）与 5 种微观花草资产（野草、高草、向日葵、虞美人、蒲公英）。建立规范的带物理树干预制体体系（使用 `StaticBody3D` + 无告警等比 `CylinderShape3D`），升级大世界多网格植被散布器（`FoliageScatter.cs`），实现多物种依高程与生态自然分布，并保持对体素可挖掘区（`VoxelVolume`）的严格排斥。

---

## 2. Technical Context

- **Language/Version**: C# 12 / .NET 8.0, Godot Engine 4.7+ Mono (Forward+ 渲染器)
- **Primary Dependencies**: Godot Engine API, Ludo AI MCP (`createImage`, `create3DModel`, `getApiJob`)
- **Storage**: 本地文件系统（`assets/models/trees/`, `assets/models/flora/`），通过 Git LFS 追踪管理
- **Testing**: Godot GDScript Headless 自动化测试套件 (`tests/test_tree_prefabs.gd`, `tests/test_multi_foliage_scatter.gd`)
- **Target Platform**: macOS / Desktop
- **Project Type**: 3D 开放世界动作沙盒游戏
- **Performance Goals**: 全岛单批次 GPU 实例化渲染 1500+ 株植被，稳定保持 60 FPS
- **Constraints**: 严禁临时链接硬编码；`CollisionShape3D` 缩放比例严格保持 `(1, 1, 1)`；体素区缓冲带内植被散布为 0

---

## 3. Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 宪章条目 | 要求与约束 | 评估结果 | 落实方案 |
| :--- | :--- | :--- | :--- |
| **I. 模块化与清晰边界** | 地形散布器与体素引擎解耦，通过标准 Bounds 交互 | **PASS** | 散布器通过 `VoxelVolume.WorldBounds` 进行排斥判定，不依赖体素内部数据 |
| **II. 性能与异步优先** | 植被渲染必须高效，无卡顿 | **PASS** | 大量树木花草基于 `MultiMeshInstance3D` 实现单批次 GPU 绘制 |
| **III. 混合衔接与物理确定性**| 碰撞体规范，防止卡人与穿模 | **PASS** | 树干专属圆柱碰撞体，精准阻挡玩家行走并拦截方块穿模放置 |
| **V. 资产规范与轻量化** | 大体积模型与贴图纳入 Git LFS | **PASS** | `.gitattributes` 已配置 `*.glb` 与 `*.png` LFS 追踪，产物立即落地本地 |
| **VI. 视觉与艺术风格宪章** | 赛璐璐手绘动漫质感，原创防侵权 | **PASS** | 提示词严格对齐 `docs/design/art_style_guide.md`，使用吉卜力手绘正负词典 |

---

## 4. Project Structure

### Documentation (this feature)
```text
specs/005-stylized-flora-and-foliage-models/
├── plan.md              # 本计划文档
├── research.md          # Phase 0: Ludo AI 提示词、生成管线与物理架构调研
├── data-model.md        # Phase 1: 变体配置结构体与预制体节点模型
├── quickstart.md        # Phase 1: 自动化与手动走查验证指南
├── checklists/
│   └── requirements.md  # 规格质量核验清单
└── tasks.md             # Phase 2: 后续任务清单 (/speckit-tasks 输出)
```

### Source Code & Assets
```text
celestial/
├── assets/
│   └── models/
│       ├── trees/
│       │   ├── oak/        # 橡木 .glb 模型与手绘贴图
│       │   ├── spruce/     # 云杉 .glb 模型与手绘贴图
│       │   └── birch/      # 白桦 .glb 模型与手绘贴图
│       └── flora/
│           ├── wild_grass.glb
│           ├── tall_grass.glb
│           ├── sunflower.glb
│           ├── poppy.glb
│           └── dandelion.glb
├── scenes/
│   ├── main.tscn           # 主场景 (挂载多变体散布与标准预制体)
│   └── prefabs/
│       └── trees/          # 标准化树木预制体
│           ├── oak_tree.tscn
│           ├── spruce_tree.tscn
│           └── birch_tree.tscn
├── src/
│   └── Terrain/
│       ├── FoliageScatter.cs   # 升级支持多网格生态分层散布
│       └── TreeVariantConfig.cs# 变体配置实体
└── tests/
    ├── test_tree_prefabs.gd           # 预制体层级与碰撞体自动化测试
    └── test_multi_foliage_scatter.gd  # 多网格全岛生态散布测试
```

---

## 5. Execution Phases

### Phase 0: Outline & Research (Completed)
- 调研 Ludo AI MCP 工具集调用细节与轮询下载模式；
- 梳理橡木、云杉、白桦以及 5 种花草植物的标准英文正负提示词；
- 确立 `StaticBody3D` + 等比 `CylinderShape3D` 的预制体物理规范。

### Phase 1: Design & Contracts (Completed)
- 编写 `data-model.md` 确定 `TreeVariantConfig`、`FloraVariantConfig` 与预制体节点树结构；
- 编写 `quickstart.md` 明确自动化与手动测试指标。

### Phase 2: Asset Generation (Tasks)
- 调用 Ludo AI MCP 生成 3 种树木正视图并转换为 `.glb` 3D 模型；
- 调用 Ludo AI MCP 生成 5 种地表花草植物模型与贴图；
- 编写脚本批量下载临时 GCS 资产至本地相应目录，触发 Git LFS 托管。

### Phase 3: Prefab & Physics Authoring (Tasks)
- 在 Godot 中构建 `oak_tree.tscn`、`spruce_tree.tscn`、`birch_tree.tscn` 预制体；
- 为树干配置规范的 `StaticBody3D`（Layer 1）与 `CylinderShape3D`（Scale 1:1:1，0 警告）；
- 验证角色阻挡与方块放置防穿模拦截。

### Phase 4: MultiMesh Scatter Engine Upgrade (Tasks)
- 升级 `FoliageScatter.cs`，实现多网格变体阵列与依海拔生态分配；
- 确保采石场与体素挖掘区排斥机制持续生效；
- 编写测试脚本并在无头模式下通过所有自动化测试。
