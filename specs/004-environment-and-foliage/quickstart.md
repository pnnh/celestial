# Quickstart & Verification Guide: Atmosphere, Triplanar & Foliage

**Feature**: `004-environment-and-foliage`  
**Date**: 2026-11-07  
**Status**: Ready for Verification  

---

## 1. 自动化验证场景

### Scenario 1: 大气环境与天空盒色彩校准验证
- **脚本**: `tests/test_environment_atmosphere.gd`
- **预期**:
  - `ProceduralSkyMaterial` 的 `ground_horizon_color` 与 `sky_horizon_color` 颜色严格一致，消除天海灰色断层；
  - `WorldEnvironment` 的 `fog_enabled == true`，`fog_depth_end <= 550.0`。

### Scenario 2: 三平面手绘贴图着色器编译与参数验证
- **脚本**: `tests/test_triplanar_shader.gd`
- **预期**:
  - `shaders/terrain_triplanar.gdshader` 成功载入，包含 grass_texture, sand_texture, rock_texture 采样槽；
  - 材质参数在平原、沙滩、山峰悬崖正常计算。

### Scenario 3: 程序化植被散布 (Foliage MultiMesh) 验证
- **脚本**: `tests/test_foliage_scatter.gd`
- **预期**:
  - 散布 500+ 株实例，所有实例的世界高度 $Y$ 均高于海平面且处于地表表面（误差 $< 0.1\text{m}$）；
  - 水下与陡峭悬崖实例生成率为 0。

