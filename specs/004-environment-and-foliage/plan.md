# Implementation Plan: Open-World Atmosphere, Stylized Texturing, Editor Preview & Foliage

**Branch**: `004-environment-and-foliage` | **Date**: 2026-11-07 | **Spec**: [Feature Specification](file:///Users/Larry/Projects/github/celestial/specs/004-environment-and-foliage/spec.md)

**Input**: Feature specification from `/specs/004-environment-and-foliage/spec.md`

---

## Summary

实现开放世界环境与画质四大演进路线：
1. **大气距离雾与天空盒色彩校准**：消除远海灰色硬切边与方形网格截断，实现天海一体的赛璐璐明媚氛围；
2. **三平面赛璐璐无缝手绘贴图升级**：升级 `terrain_triplanar.gdshader`，混合草甸、沙滩、风化崖壁贴图，世界坐标三平面投影彻底消除悬崖拉伸；
3. **Godot 编辑器内实时地形预览与地面吸附**：为 `TiledTerrainManager` 与 `OceanWaterPlane` 增加 `[Tool]` 支持与编辑器轻量预览瓦片，打通拖拽 3D 模型一键地面吸附工作流；
4. **批量程序化植被散布 (Foliage MultiMesh)**：基于 `MultiMeshInstance3D` 实现千株卡通树木、野花簇单批次高效渲染（60 FPS）。

---

## Technical Context

**Language/Version**: C# 12 / .NET 8.0, Godot Engine 4.7+ Mono  
**Primary Dependencies**: Godot 4.7 Mono Engine API (`MultiMeshInstance3D`, `MultiMesh`, `WorldEnvironment`, `ShaderMaterial`)  
**Target Platform**: macOS (Apple Silicon Metal) & PC (Windows/Linux) Desktop  
**Performance Goals**: 稳定 60 FPS，植被 Draw Call 增量 $\le 5$，编辑器预览加载 $< 1.5\text{s}$  
**Scale/Scope**: 千株植被散布，涵盖 $1024\text{m} \times 1024\text{m}$ 海岛腹地  

---

## Constitution Check

| 宪章准则 | 评估要点 | 状态 | 实施措施与设计保障 |
|---|---|:---:|---|
| **I. 模块化与清晰边界** | 植被、地形材质、大气环境解耦 | **PASS** | `FoliageScatter` 独立依赖 `IHeightSampler`，不与角色或存储耦合。 |
| **II. 性能与异步优先** | 植被散布不增加 Draw Call | **PASS** | 采用 GPU 单批次实例化（`MultiMesh`），1,000+ 株树木花草仅消耗 1~2 个 Draw Call。 |
| **III. 混合衔接与物理确定性** | 植被高度严格贴合地形 | **PASS** | 植被变换矩阵依据 `IHeightSampler.GetHeight()` 与法线旋转贴合。 |
| **IV. 渐进式算法演进** | 原型到美化循序渐进 | **PASS** | 遵循第二阶段“优化阶段：高度插值与边缘平滑材质过渡”，不超前引入过度复杂系统。 |
| **V. 资产规范与轻量化** | 避免仓库膨胀 | **PASS** | 复用工程既有贴图与轻量低模，受 Git LFS 规范管理。 |
| **VI. 赛璐璐手绘幻想风** | 美术语言高度统一，版权规避 | **PASS** | 统一蓝白晴空、碧蓝大洋与赛璐璐手绘草甸，绝不借用塞尔达侵权专有符号。 |

---

## Project Structure

### Source Code

```text
shaders/
├── ocean_water.gdshader                    # 赛璐璐大洋水体着色器
└── terrain_triplanar.gdshader              # [升级] 支持草地/沙滩/岩石三平面贴图混合

src/Terrain/
├── TiledTerrainManager.cs                  # [升级] 增加 [Tool] 编辑器预览模式
├── OceanWaterPlane.cs                      # [升级] 增加 [Tool] 编辑器预览模式
└── FoliageScatter.cs                       # [新增] 批量程序化植被散布管理器 (MultiMesh)

scenes/
└── main.tscn                               # [升级] 配置大气距离雾、天空盒调色、挂载 FoliageScatter

tests/
├── test_environment_atmosphere.gd         # [新增] 大气环境与天空盒色彩验证
├── test_triplanar_shader.gd                # [新增] 三平面贴图着色器参数验证
└── test_foliage_scatter.gd                 # [新增] 程序化植被高度与生态过滤验证
```
