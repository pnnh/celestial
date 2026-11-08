# Phase 0: Research & Technical Architecture Decisions

**Feature**: `004-environment-and-foliage`  
**Date**: 2026-11-07  
**Status**: Completed  

---

## 1. 大气距离透视雾与天空盒色彩校准 (Atmospheric Fog & Skybox Alignment)

### 决策内容 (Decision)
1. **ProceduralSkyMaterial 色彩缝合**:
   - `sky_top_color`: `Color(0.35, 0.62, 0.96)` (清爽二次元暖天蓝)
   - `sky_horizon_color`: `Color(0.68, 0.82, 0.95)` (晨曦天际线浅青白)
   - `ground_bottom_color`: `Color(0.08, 0.28, 0.58)` (深海靛蓝，与 `ocean_water.gdshader` 的深水色完全一致，彻底消除死板灰色！)
   - `ground_horizon_color`: `Color(0.68, 0.82, 0.95)` (与天际线浅青白严格一致，天海交界无缝闭合)
2. **大气距离雾 (Distance Fog)**:
   - `fog_enabled = true`
   - `fog_light_color = Color(0.68, 0.82, 0.95)` (与地平线雾色对齐)
   - `fog_density = 0.002`
   - `fog_depth_begin = 180.0m` (近中景草甸与山体清晰通透)
   - `fog_depth_end = 520.0m` (远景海床尽头与 64m 瓦片最外缘截断边界完全平滑淡出在雾气中，彻底遮罩方形转角)

### 决策理由 (Rationale)
- 遵循开放世界标准做法：利用大气深度雾自然隐藏视距尽头，消除突兀的网格截断，营造壮阔的千米空间透视感。

---

## 2. 三平面赛璐璐手绘无缝贴图材质 (Triplanar Stylized Texturing)

### 决策内容 (Decision)
升级 `shaders/terrain_triplanar.gdshader`：
1. **三组无缝手绘贴图支持**:
   - 草甸平铺：`assets/textures/ludo/lush_grass_soil.png` 或 `grass_top.png`
   - 沙滩平铺：`assets/textures/sand.png`
   - 峭壁山石：`assets/textures/stone.png`
2. **世界空间三平面投影 (World-Space Triplanar)**:
   - 依据世界坐标进行三向投影：$UV_x = (pos.z, pos.y) \times \text{scale}$, $UV_y = (pos.x, pos.z) \times \text{scale}$, $UV_z = (pos.x, pos.y) \times \text{scale}$。
   - 依据世界法线绝对值进行三向归一化混合，悬崖岩壁 100% 杜绝纹理拉伸。
3. **二阶赛璐璐卡通光影**:
   - `diffuse_toon`, `specular_toon`，与角色赛璐璐风格高度一致。

---

## 3. Godot 编辑器内实时地形预览与地面吸附 (`[Tool]` Support)

### 决策内容 (Decision)
1. 为 `TiledTerrainManager` 与 `OceanWaterPlane` 增加 Godot C# `[Tool]` 特性；
2. 在 `Engine.IsEditorHint()` 为 true 时：
   - 生成以世界原点 $(0, 0)$ 为中心、半径为 3 块（覆盖 $448\text{m}$ 核心海岛与中心平原）的编辑器预览瓦片；
   - 生成物理碰撞面，使得开发者在 Godot 编辑器中直接按 `Ctrl+Alt+S` 或点击顶部“地面吸附”按钮时，射线能精准拾取并对齐地面。

---

## 4. 批量程序化植被散布 (Foliage MultiMesh Scatter)

### 决策内容 (Decision)
构建 `FoliageScatter.cs`（基于 `MultiMeshInstance3D`）：
1. **GPU 单批次实例化 (Single Draw Call)**:
   - 单个 MultiMesh 管理上千株植被，零 CPU Draw Call 负担；
2. **高度场生态过滤规则**:
   - 水下与海滩（$Y \le 1.2\text{m}$）：不生长陆生大树；
   - 陡峭岩壁（$\text{Normal.Y} < 0.75$）：不生长草簇；
   - 平原微波草甸（$Y > 2.0\text{m}$ 且坡度平缓）：错落散布卡通树木、野花簇与草丛。

