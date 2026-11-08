extends SceneTree

func _init():
	print("==================================================")
	print("[Test] 正在执行开放世界大气环境与天空盒色彩验证...")
	print("==================================================")

	var scene = load("res://scenes/main.tscn")
	if not scene:
		push_error("FAILED: 无法载入 main.tscn")
		quit(1)
		return

	var main_node = scene.instantiate()
	var env_node = main_node.get_node_or_null("WorldEnvironment")
	if not env_node or not env_node.environment:
		push_error("FAILED: main.tscn 缺少 WorldEnvironment 或 Environment 资源")
		quit(1)
		return

	var env = env_node.environment
	# 1. 验证天空盒地平线上下半球色彩对齐 (消除灰色断层)
	var sky = env.sky
	if not sky or not sky.sky_material is ProceduralSkyMaterial:
		push_error("FAILED: 缺少 ProceduralSkyMaterial")
		quit(1)
		return

	var sky_mat = sky.sky_material as ProceduralSkyMaterial
	print("[Check 1] 天空地平线颜色: %s, 地面地平线颜色: %s" % [sky_mat.sky_horizon_color, sky_mat.ground_horizon_color])
	if sky_mat.sky_horizon_color != sky_mat.ground_horizon_color:
		push_error("FAILED: 天海地平线颜色不一致，存在生硬灰色断层！")
		quit(1)
		return

	# 2. 验证深海底色与水体统一 (非灰色)
	print("[Check 2] 天空盒下底面底色: %s (预期为深海深蓝 B > 0.4)" % sky_mat.ground_bottom_color)
	if sky_mat.ground_bottom_color.b < 0.4 or sky_mat.ground_bottom_color.r > 0.2:
		push_error("FAILED: 天空盒下半球仍为灰暗色调: %s" % sky_mat.ground_bottom_color)
		quit(1)
		return

	# 3. 验证大气距离透视雾已启用 (深度模式 + 天空盒融合)
	print("[Check 3] 大气雾开启: %s, 模式: %s (预期 1:Depth), 雾起始: %0.1fm, 雾截止: %0.1fm, 天空盒融合度: %0.2f" % [
		env.fog_enabled, env.fog_mode, env.fog_depth_begin, env.fog_depth_end, env.fog_sky_affect
	])
	if not env.fog_enabled or env.fog_mode != 1 or env.fog_depth_end > 520.0 or env.fog_sky_affect < 0.9:
		push_error("FAILED: 大气深度透视雾未开启(必须为 Depth 模式)、截止距离过远或天空盒融合度不足！")
		quit(1)
		return

	print("==================================================")
	print("✓ PASS: 开放世界大气环境与天空盒色彩校准验证全部通过！")
	print("==================================================")
	quit(0)

