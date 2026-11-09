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
	if not sky or not (sky.sky_material is ProceduralSkyMaterial or sky.sky_material is ShaderMaterial):
		push_error("FAILED: 缺少有效天空材质 (ProceduralSkyMaterial 或 ShaderMaterial)")
		quit(1)
		return

	var sky_horizon_c: Color
	var ground_horizon_c: Color
	var ground_bottom_c: Color

	if sky.sky_material is ProceduralSkyMaterial:
		var p_mat = sky.sky_material as ProceduralSkyMaterial
		sky_horizon_c = p_mat.sky_horizon_color
		ground_horizon_c = p_mat.ground_horizon_color
		ground_bottom_c = p_mat.ground_bottom_color
	elif sky.sky_material is ShaderMaterial:
		var s_mat = sky.sky_material as ShaderMaterial
		sky_horizon_c = s_mat.get_shader_parameter("sky_horizon_color")
		ground_horizon_c = s_mat.get_shader_parameter("ground_horizon_color")
		ground_bottom_c = s_mat.get_shader_parameter("ground_bottom_color")

	print("[Check 1] 天空地平线颜色: %s, 地面地平线颜色: %s" % [sky_horizon_c, ground_horizon_c])
	if sky_horizon_c != ground_horizon_c:
		push_error("FAILED: 天海地平线颜色不一致，存在生硬灰色断层！")
		quit(1)
		return

	# 2. 验证深海底色与水体统一 (非灰色)
	print("[Check 2] 天空盒下底面底色: %s (预期为深海深蓝 B > 0.4)" % ground_bottom_c)
	if ground_bottom_c.b < 0.4 or ground_bottom_c.r > 0.2:
		push_error("FAILED: 天空盒下半球仍为灰暗色调: %s" % ground_bottom_c)
		quit(1)
		return

	# 3. 验证大气距离透视雾已启用 (深度模式 + 天空盒手绘保护)
	print("[Check 3] 大气雾开启: %s, 模式: %s (预期 1:Depth), 雾起始: %0.1fm, 雾截止: %0.1fm, 雾对天空影响: %0.2f" % [
		env.fog_enabled, env.fog_mode, env.fog_depth_begin, env.fog_depth_end, env.fog_sky_affect
	])
	if not env.fog_enabled or env.fog_mode != 1 or env.fog_depth_end > 520.0:
		push_error("FAILED: 大气深度透视雾未开启(必须为 Depth 模式)或截止距离过远！")
		quit(1)
		return

	# 若使用手绘 Sky Shader，fog_sky_affect 必须为 0 或极小(<=0.15)，防止深度雾将太阳与积云抹消
	if sky.sky_material is ShaderMaterial:
		if env.fog_sky_affect > 0.15:
			push_error("FAILED: 手绘 Sky Shader 下 fog_sky_affect 过高(%0.2f > 0.15)，会导致雾效遮蔽太阳与云朵！" % env.fog_sky_affect)
			quit(1)
			return
		var s_mat = sky.sky_material as ShaderMaterial
		var sun_sz = s_mat.get_shader_parameter("sun_size")
		var cloud_cov = s_mat.get_shader_parameter("cloud_coverage")
		print("[Check 4] 手绘天空参数: sun_size=%s, cloud_coverage=%s" % [sun_sz, cloud_cov])
		if sun_sz == null or cloud_cov == null or float(sun_sz) <= 0.0 or float(cloud_cov) <= 0.0:
			push_error("FAILED: 手绘 Sky Shader 缺少太阳或云朵关键着色参数！")
			quit(1)
			return

		# 5. 验证地平线雾气与天空盒中介色对齐 (消除深海-白雾-蓝天的三色断层带)
		print("[Check 5] 验证地平线色彩对齐: 雾颜色=%s, 地平线颜色=%s" % [env.fog_light_color, sky_horizon_c])
		if env.fog_light_color != sky_horizon_c:
			push_error("FAILED: 雾效颜色与天空盒地平线颜色未对齐，存在色彩脱节！")
			quit(1)
			return
		if sky_horizon_c.r > 0.55 or sky_horizon_c.g > 0.75:
			push_error("FAILED: 地平线雾效依然偏向刺眼高亮白(R=%0.2f, G=%0.2f)，未能形成柔和海雾中介色！" % [sky_horizon_c.r, sky_horizon_c.g])
			quit(1)
			return

	print("==================================================")
	print("✓ PASS: 开放世界大气环境与天空盒色彩校准验证全部通过！")
	print("==================================================")
	quit(0)

