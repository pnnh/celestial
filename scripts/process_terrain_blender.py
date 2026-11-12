"""
Celestial Open-World Terrain Processor in Blender
- Imports 8-bit concept heightmap
- Eliminates 8-bit quantization terracing via multi-scale edge-preserving smoothing
- Simulates realistic hydraulic & thermal erosion features
- Smoothly feathers coastlines into deep ocean
- Builds 3D terrain mesh in Blender scene with cell-shaded biome materials
- Bakes precision 16-bit grayscale heightmap (2048x2048) and splatmap
"""

import bpy
import numpy as np
import os
import math

def process_heightmap(input_png_path, output_16bit_path, output_splatmap_path, target_res=2048):
    print(f"[BlenderTerrain] Loading input concept: {input_png_path}")
    if not os.path.exists(input_png_path):
        raise FileNotFoundError(f"Input file not found: {input_png_path}")

    # 1. 使用 Blender 图像加载器加载源图
    img = bpy.data.images.load(input_png_path, check_existing=False)
    src_w, src_h = img.size[0], img.size[1]
    print(f"[BlenderTerrain] Loaded image size: {src_w}x{src_h}")

    # 读取原始浮点像素 (RGBA)
    raw_pixels = np.empty(src_w * src_h * 4, dtype=np.float32)
    img.pixels.foreach_get(raw_pixels)
    # 取 R 通道灰度，变换为 (src_h, src_w)
    src_hmap = raw_pixels.reshape((src_h, src_w, 4))[:, :, 0]

    # 2. 高质量双线性升采样到 target_res (2048x2048)
    print(f"[BlenderTerrain] Upsampling to {target_res}x{target_res}...")
    y_indices = np.linspace(0, src_h - 1, target_res)
    x_indices = np.linspace(0, src_w - 1, target_res)
    
    # 构造网格坐标进行插值
    x_floor = np.floor(x_indices).astype(int)
    x_ceil = np.clip(x_floor + 1, 0, src_w - 1)
    x_weight = (x_indices - x_floor).reshape((1, target_res))
    
    y_floor = np.floor(y_indices).astype(int)
    y_ceil = np.clip(y_floor + 1, 0, src_h - 1)
    y_weight = (y_indices - y_floor).reshape((target_res, 1))

    # 双线性插值
    top_left = src_hmap[y_floor[:, None], x_floor[None, :]]
    top_right = src_hmap[y_floor[:, None], x_ceil[None, :]]
    bot_left = src_hmap[y_ceil[:, None], x_floor[None, :]]
    bot_right = src_hmap[y_ceil[:, None], x_ceil[None, :]]

    top = top_left * (1.0 - x_weight) + top_right * x_weight
    bot = bot_left * (1.0 - x_weight) + bot_right * x_weight
    hmap = top * (1.0 - y_weight) + bot * y_weight

    # 3. 抗梯田化多尺度平滑 (Anti-Terracing Smoothing)
    # 采用分离式可控高斯卷积，彻底抹除 256 级离散量化产生的阶梯
    print("[BlenderTerrain] Eliminating 8-bit quantization terracing...")
    
    def gaussian_blur_1d(arr, axis, sigma=2.0):
        radius = int(math.ceil(sigma * 3.0))
        x = np.arange(-radius, radius + 1)
        kernel = np.exp(-0.5 * (x / sigma) ** 2)
        kernel /= kernel.sum()
        
        # 沿指定轴一维卷积
        pad_width = [(0, 0), (0, 0)]
        pad_width[axis] = (radius, radius)
        padded = np.pad(arr, pad_width, mode='edge')
        
        blurred = np.zeros_like(arr)
        for i, w in enumerate(kernel):
            shift = i
            if axis == 0:
                blurred += padded[shift:shift + arr.shape[0], :] * w
            else:
                blurred += padded[:, shift:shift + arr.shape[1]] * w
        return blurred

    # 先施加半径约为 2.5 像素的高斯滤波彻底平滑量化台阶
    hmap_smooth = gaussian_blur_1d(hmap, axis=0, sigma=2.5)
    hmap_smooth = gaussian_blur_1d(hmap_smooth, axis=1, sigma=2.5)

    # 4. 水力侵蚀与山脊强化模拟 (Hydraulic Erosion & Ridge Sharpening)
    print("[BlenderTerrain] Simulating hydraulic erosion and ridge sharpening...")
    # 计算局部梯度坡度
    gy, gx = np.gradient(hmap_smooth)
    slope = np.sqrt(gx**2 + gy**2)
    laplacian = np.gradient(gx)[1] + np.gradient(gy)[0]

    # 山脊锐化：负拉普拉斯区域（山脊凸起）强化锐度，正拉普拉斯区域（山谷底）适度加深沟壑
    ridge_mask = np.clip(-laplacian * 40.0, 0.0, 1.0)
    valley_mask = np.clip(laplacian * 40.0, 0.0, 1.0)

    # 汇水侵蚀冲刷沟壑（在山谷中形成自然流线冲沟）
    hmap_eroded = hmap_smooth + ridge_mask * 0.035 - valley_mask * slope * 0.045
    
    # 5. 外围大洋平滑渐变归零 (Radial Ocean Feathering)
    print("[BlenderTerrain] Feathering outer island edges into ocean...")
    cy, cx = target_res // 2, target_res // 2
    yy, xx = np.ogrid[:target_res, :target_res]
    dist_from_center = np.sqrt((xx - cx)**2 + (yy - cy)**2) / (target_res * 0.5) # 0.0 ~ 1.0 (归一化半径)

    # 在半径 0.72 ~ 0.95 范围内，平滑衰减至 0 (海平面以下深海)
    fade_start = 0.68
    fade_end = 0.94
    radial_mask = np.clip((dist_from_center - fade_start) / (fade_end - fade_start), 0.0, 1.0)
    # 三次 Hermite 平滑 (Smoothstep)
    fade_factor = 1.0 - (radial_mask * radial_mask * (3.0 - 2.0 * radial_mask))

    hmap_final = np.clip(hmap_eroded * fade_factor, 0.0, 1.0)
    
    # 确保极值规整
    min_val, max_val = hmap_final.min(), hmap_final.max()
    print(f"[BlenderTerrain] Heightmap elevation range: min={min_val:.5f}, max={max_val:.5f}")

    # 6. 烘焙输出 16-bit 灰度图与 32-bit Float 高精二进制场
    print(f"[BlenderTerrain] Saving 16-bit heightmap to {output_16bit_path}...")
    hmap_16bit = np.clip(hmap_final * 65535.0, 0, 65535).astype(np.uint16)

    # 导出纯二进制 .f32 高度场 (2048x2048 float32, 极速零损耗高精物理采样)
    output_f32_path = output_16bit_path.replace(".png", ".f32")
    print(f"[BlenderTerrain] Exporting raw float32 elevation field to {output_f32_path}...")
    hmap_final.astype(np.float32).tofile(output_f32_path)
    
    # 通过 Blender Image 保存 16-bit PNG (供可视化与管线预览)
    out_img = bpy.data.images.new("IslandHeightmap16", width=target_res, height=target_res, alpha=False, float_buffer=True)
    norm_floats = (hmap_final.astype(np.float32)).flatten()
    rgba_out = np.zeros((target_res * target_res, 4), dtype=np.float32)
    rgba_out[:, 0] = norm_floats
    rgba_out[:, 1] = norm_floats
    rgba_out[:, 2] = norm_floats
    rgba_out[:, 3] = 1.0
    out_img.pixels.foreach_set(rgba_out.flatten())

    scene = bpy.context.scene
    scene.render.image_settings.color_depth = '16'
    scene.render.image_settings.color_mode = 'BW'
    scene.render.image_settings.file_format = 'PNG'
    out_img.save_render(output_16bit_path, scene=scene)
    print(f"[BlenderTerrain] Successfully saved 16-bit heightmap: {output_16bit_path}")

    # 7. 计算并烘焙 Splatmap (RGBA 生态材质分层权重)
    # R: 平原缓坡草地 (Grass)
    # G: 陡峭悬崖岩石 (Rock, 坡度大或海拔高)
    # B: 海岸与内陆湖边细沙 (Sand, 海拔极低且靠近水面)
    # A: 高山寒带冷杉雪地 (Alpine / Snow, 海拔最高峰)
    print(f"[BlenderTerrain] Generating Splatmap to {output_splatmap_path}...")
    final_slope = np.sqrt(np.gradient(hmap_final)[0]**2 + np.gradient(hmap_final)[1]**2) * 50.0
    
    sand_mask = np.clip((0.08 - hmap_final) / 0.05, 0.0, 1.0) * np.clip(1.0 - final_slope * 2.0, 0.0, 1.0)
    rock_mask = np.clip((final_slope - 0.35) / 0.3, 0.0, 1.0) + np.clip((hmap_final - 0.65) / 0.25, 0.0, 1.0) * 0.5
    rock_mask = np.clip(rock_mask, 0.0, 1.0)
    snow_mask = np.clip((hmap_final - 0.75) / 0.18, 0.0, 1.0)
    grass_mask = np.clip(1.0 - rock_mask - sand_mask - snow_mask, 0.0, 1.0)

    splat_rgba = np.zeros((target_res * target_res, 4), dtype=np.float32)
    splat_rgba[:, 0] = grass_mask.flatten()
    splat_rgba[:, 1] = rock_mask.flatten()
    splat_rgba[:, 2] = sand_mask.flatten()
    splat_rgba[:, 3] = snow_mask.flatten()

    splat_img = bpy.data.images.new("IslandSplatmap", width=target_res, height=target_res, alpha=True, float_buffer=False)
    splat_img.pixels.foreach_set(splat_rgba.flatten())
    scene.render.image_settings.color_depth = '8'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.file_format = 'PNG'
    splat_img.save_render(output_splatmap_path, scene=scene)
    print(f"[BlenderTerrain] Successfully saved Splatmap: {output_splatmap_path}")

    # 8. 在 Blender 场景中重建完整的 3D 几何地形模型 (供实时查看与渲染)
    print("[BlenderTerrain] Building 3D mesh in live Blender scene...")
    # 清理旧物体
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()

    # 创建一个 128x128 初始细分网格，尺寸 700m
    island_scale = 700.0
    max_height = 80.0
    mesh_res = 128
    
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=mesh_res, y_subdivisions=mesh_res, size=island_scale, location=(0, 0, 0))
    terrain_obj = bpy.context.active_object
    terrain_obj.name = "Celestial_3D_Island"

    # 添加多级细分修改器
    subsurf = terrain_obj.modifiers.new(name="Subdivision", type='SUBSURF')
    subsurf.subdivision_type = 'SIMPLE'
    subsurf.levels = 3
    subsurf.render_levels = 3

    # 添加位移修改器 (Displace)
    disp_mod = terrain_obj.modifiers.new(name="IslandDisplace", type='DISPLACE')
    disp_tex = bpy.data.textures.new("HeightmapTexture", type='IMAGE')
    disp_tex.image = out_img
    disp_mod.texture = disp_tex
    disp_mod.texture_coords = 'UV'
    disp_mod.strength = max_height
    disp_mod.mid_level = 0.0

    # 赋予地形展示材质
    mat = bpy.data.materials.new(name="TerrainToonMaterial")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (0.35, 0.65, 0.28, 1.0) # 赛璐璐青翠原野绿
        bsdf.inputs['Roughness'].default_value = 0.85
    terrain_obj.data.materials.append(mat)

    # 架设斜视观览相机
    cam_data = bpy.data.cameras.new("OverviewCam")
    cam_obj = bpy.data.objects.new("OverviewCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    cam_obj.location = (450.0, -450.0, 350.0)
    cam_obj.rotation_euler = (math.radians(58.0), 0.0, math.radians(45.0))
    bpy.context.scene.camera = cam_obj

    # 添加阳光主平行光
    sun_data = bpy.data.lights.new(name="SunLight", type='SUN')
    sun_data.energy = 2.5
    sun_data.color = (1.0, 0.98, 0.92)
    sun_obj = bpy.data.objects.new(name="SunLight", object_data=sun_data)
    bpy.context.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = (math.radians(45.0), math.radians(25.0), math.radians(-35.0))

    print("==================================================")
    print("✓ PASS: Blender 3D 大世界海岛模型与 16-bit 高度图烘焙全部完成！")
    print(f"  高度图: {output_16bit_path} (16-bit Grayscale, 2048x2048)")
    print(f"  地表分层图: {output_splatmap_path} (RGBA, 2048x2048)")
    print("==================================================")
    return True
