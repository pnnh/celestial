# Feature Specification: 多样化赛璐璐植被资产与树木物理阻挡 (005-stylized-flora-and-foliage-models)

**Feature Directory**: `specs/005-stylized-flora-and-foliage-models`  
**Created**: 2026-11-08  
**Status**: Draft  
**Input**: 用户需求："对于程序化生成的树木目前还没有配置碰撞体积，并且生成的树木是一种简化的树木形状。现在尝试使用ludo ai mcp来生成一批真正的树木模型。提示词中要写明符合目前项目的美术风格（参考docs/design目录中的art_style_guide.md），忽略我自己放置的那棵树。不要放置单一树木，而是放置几种变体。先放置我的世界游戏中常见的橡木、云杉、白桦三种吧，需要使用ludo生成3种树木的模型和贴图，并为树干设置圆柱形碰撞体积。另外同样使用ludo mcp，生成一些我的世界游戏中常见的花草与灌木丛，比如野草、高草、向日葵、虞美人、蒲公英等等，使我们在制作的游戏场景内容更丰富生动。"

---

## 1. User Scenarios & Testing *(mandatory)*

### User Story 1 - 三种核心树木（橡木、云杉、白桦）赛璐璐模型生成与预制体搭建 (Priority: P1) 🎯 MVP

作为探索海岛的玩家，我希望在大地图上看到造型各异且符合《Celestial》日系清爽手绘赛璐璐美术风格的真实树木（包括阔叶橡木、挺拔云杉、白干白桦），而不是原先单一简陋的几何圆锥树。

**Why this priority**: 树木是开放世界大自然生态的骨干视觉资产，直接决定了海岛的生机与视觉品质。通过 Ludo AI 生成高品质 3D 模型并构建标准预制体，是构建真实开放世界森林的基础。

**Independent Test**: 在 Godot 中实例化橡木、云杉、白桦预制体，验证模型网格面数合理（< 25,000 面）、材质符合动漫手绘无噪点基调、贴图无拉伸与接缝。

**Acceptance Scenarios**:
1. **Given** 调用 Ludo AI MCP 工具及美术风格词典（`docs/design/art_style_guide.md`），**When** 生成正视图渲染并转为 3D 模型，**Then** 输出带有 PBR 手绘贴图的完整 `.glb` 资产并存入 `assets/models/trees/`。
2. **Given** 导入的 3D 树木资产，**When** 构建为 Godot 预制体，**Then** 分别输出 `oak_tree.tscn`（橡木）、`spruce_tree.tscn`（云杉）与 `birch_tree.tscn`（白桦）。

---

### User Story 2 - 树干专用圆柱碰撞体与体素防穿模放置联动 (Priority: P1)

作为伐木与建造玩家，我希望树木拥有真实的物理阻挡，当我走近树木时被树干挡住，同时无法把方块塞入树干内部；但同时树叶与树枝保持轻盈通透，允许玩家穿过树下枝叶。

**Why this priority**: 解决之前用户遇到的“碰撞体包裹整个树叶像大帐篷”以及“缺少 StaticBody3D 导致物理失效”的交互痛点。实现精准、无警告、物理高效的树干圆柱碰撞体。

**Independent Test**: 玩家走向树木，树叶部分允许穿行，树干部分牢固阻挡角色；用准星对准树干右键放置方块，触发拦截提示 `[放置受阻] 目标位置存在 3D 实体障碍物`。

**Acceptance Scenarios**:
1. **Given** 树木预制体节点树，**When** 挂载 `StaticBody3D`（Layer 1）与 `CollisionShape3D`，**Then** 其 `Transform.Scale` 严格保持 `(1, 1, 1)` 无缩放警告，内部 `CylinderShape3D` 的半径与高度精准贴合树干外轮廓。
2. **Given** 玩家在树干碰撞体所占据的 $1\text{m}^3$ 空间内尝试右键放置方块，**When** 触发射线与盒检测，**Then** 放置动作被立即拦截并不产生任何穿模。

---

### User Story 3 - 经典野花与草丛（野草、高草、向日葵、虞美人、蒲公英）资产生成 (Priority: P2)

作为漫游海岛的玩家，我希望在起伏的草地上看到点缀其间的斑斓花草灌木，包括低矮野草、茂密高草、明艳向日葵、绯红虞美人与点点蒲公英，营造类似吉卜力草原的明媚生动感。

**Why this priority**: 地表仅有绿草贴图会显得空旷单调，丰富的地表花草层级能大幅增强大世界沉浸感与微观生态丰富度。

**Independent Test**: 在不同高程与缓坡上查看散布的花草资产，验证花草与三平面地表自然贴合，无漂浮或破面。

**Acceptance Scenarios**:
1. **Given** 调用 Ludo AI MCP，**When** 依照吉卜力赛璐璐清爽调色板生成野草、高草、向日葵、虞美人、蒲公英纹理与十字面片/低多边形 3D 模型，**Then** 资源规范存储于 `assets/models/flora/`。
2. **Given** 花草植物通常为非阻挡穿行层，**When** 角色走过花草，**Then** 不会阻挡角色行进，且支持玩家在其位置正常插桩放置方块。

---

### User Story 4 - 批量植被散布器（FoliageScatter）多变体生态分层升级 (Priority: P2)

作为游戏运行时系统，程序化散布器需要支持多网格混合（MultiMesh / Multi-Mesh Array），根据高程、坡度与生态规则智能分布不同树种与花草（如低地平原生长橡木与向日葵，内陆山脊生长云杉与耐寒野草，海滩边缘过滤树木），并确保单批次渲染帧率保持 60 FPS。

**Why this priority**: 替代原先单一云杉树的全岛无差别撒点，实现多样性生物群落分布，且不牺牲大规模渲染性能。

**Independent Test**: 运行全岛植被散布自动化测试，验证 3 种树木与 5 种花草在全岛按规则均匀自然分布，采石场等体素可变区保持严格排除。

**Acceptance Scenarios**:
1. **Given** `FoliageScatter.cs` 升级为支持多类型资产配置（`TreeVariants` 与 `FloraVariants`），**When** 生成大世界植被，**Then** 各变体按生态权重与高度自然过渡。
2. **Given** 视距内渲染 2000+ 株混合植被实例，**When** 监控 Draw Calls 与帧率，**Then** 借助 GPU 实例化渲染保持流畅 60 FPS。

---

### Edge Cases
- **树木生成在极陡峭峭壁边缘**：法线倾角大于 45° 的悬崖表面禁止生成大型树木，避免树木横向生长违背重力。
- **浅滩与海水交界处**：绝对高程低于海平面 $Y \le 0.5\text{m}$ 的区域禁止生成任何陆生花草与树木。
- **体素挖掘区动态避让**：所有树木与花草散布点必须严格排除在 `VoxelVolume`（采石场、矿区）的包围盒外延 $3\text{m}$ 缓冲带之外。

---

## 2. Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: 必须使用 Ludo AI MCP 工具及符合 `docs/design/art_style_guide.md` 的规范提示词，生成 3 种树木模型（橡木、云杉、白桦）与 5 种花草资产（野草、高草、向日葵、虞美人、蒲公英）。
- **FR-002**: 生成的所有资产文件（`.glb`, `.png`）必须下载并持久化保存在本地 `assets/models/trees/` 与 `assets/models/flora/`，严禁直接依赖会过期的 GCS 临时链接。
- **FR-003**: 3 种树木必须制作规范的 Godot 预制体（`.tscn`），树干部位必须配备 `StaticBody3D` 与 `CollisionShape3D`（采用 `CylinderShape3D`，且 `Transform.Scale` 严格保持 `(1, 1, 1)`）。
- **FR-004**: 树木树干碰撞体必须设置在 Collision Layer 1（Environment），支持被 `BedrockPhysicsDetector` 识别为不可破坏基岩，并被 `VoxelInteractor` 识别为放置障碍物。
- **FR-005**: 升级 `src/Terrain/FoliageScatter.cs`，从单网格改为支持多网格变体阵列（橡木、云杉、白桦按高度与生态群落比例混合散布，花草灌木簇紧密贴地散布）。
- **FR-006**: 散布算法必须保持对 `VoxelVolume` 的排斥区检测，确保体素可挖掘区内绝对无 3D 植被。

### Key Entities
- **TreeVariant**: 树木变体实体，包含模型网格、树干圆柱碰撞体半径与高度、生长适宜高程区间（如平原橡木 5~30m，高山云杉 25~70m，林缘白桦 10~40m）及生成权重。
- **FloraVariant**: 地表花草实体，包含花草贴图/网格、散布密度因子、群落集群聚类半径。

---

## 3. Success Criteria *(mandatory)*

### Measurable Outcomes
- **SC-001**: 成功产出 3 种标准 3D 树木模型与 5 种花草资产，美术风格符合赛璐璐暖色调与吉卜力手绘质感。
- **SC-002**: 玩家角色走向任意程序化树木树干时，阻挡成功率 100%，无穿模；且 Godot 编辑器内 0 条关于 `CollisionShape3D` 缩放的警告。
- **SC-003**: 玩家尝试在树干中心放置方块时，防穿模拦截率 100%，并在控制台清晰输出阻挡原因。
- **SC-004**: 全岛大世界散布 1500+ 株多样化植被与花草，同屏单批次渲染帧率稳定维持在 60 FPS 以上。

---

## 4. Assumptions
- Ludo AI MCP 保持可用且 API 账户额度充裕（当前实测有效，额度充沛）。
- 树冠枝叶部分遵循现代开放世界通行方案，保持无物理阻挡以优化穿行体验。
- 用户手动放置的测试树（`tree.glb`）保持独立保留，不影响程序化生态系统的自动运作。

