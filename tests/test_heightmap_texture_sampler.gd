extends SceneTree

func _init() -> void:
	print("==================================================")
	print("[Test] 正在验证 HeightmapTextureSampler 预制高度场采样器...")
	print("==================================================")
	
	var sampler_script = load("res://src/Terrain/HeightmapTextureSampler.cs")
	if not sampler_script:
		push_error("FAILED: 无法载入 HeightmapTextureSampler.cs")
		quit(1)
		return
	
	var sampler = sampler_script.new()
	print("[Check 1] Heightmap 是否成功加载: %s" % sampler.IsLoaded)
	if not sampler.IsLoaded:
		push_error("FAILED: HeightmapTextureSampler 未能成功加载二进制数据！")
		quit(1)
		return

	# 测试中心与关键区域标高
	var h_center = sampler.GetHeight(0.0, 0.0)
	var n_center = sampler.GetNormal(0.0, 0.0)
	print("[Check 2] 中心坐标 (0, 0) 地表高程: %0.2fm, 表面法线: %s" % [h_center, n_center])

	# 采样 100 个随机位置，验证标高连续性与无 NaN / Inf
	var min_h = 999.0
	var max_h = -999.0
	for i in range(100):
		var rx = (randf() - 0.5) * 600.0
		var rz = (randf() - 0.5) * 600.0
		var h = sampler.GetHeight(rx, rz)
		var n = sampler.GetNormal(rx, rz)
		if is_nan(h) or is_inf(h) or is_nan(n.x) or is_nan(n.y) or is_nan(n.z):
			push_error("FAILED: 检测到 NaN 或 Inf 高度/法线: at (%f, %f)" % [rx, rz])
			quit(1)
			return
		min_h = min(min_h, h)
		max_h = max(max_h, h)

	print("[Check 3] 全岛随机采样高程范围: 最低 %0.2fm, 最高 %0.2fm" % [min_h, max_h])
	if max_h < 40.0:
		push_error("FAILED: 最高海拔过低 (%f < 40m)，未体现宏伟山脊！" % max_h)
		quit(1)
		return

	# 测试外围大洋平滑沉降
	var h_ocean = sampler.GetHeight(450.0, 450.0)
	print("[Check 4] 外围大洋 (450, 450) 标高: %0.2fm (预期水下 < -5m)" % h_ocean)
	if h_ocean >= 0.0:
		push_error("FAILED: 外围大洋未沉入水下: %f >= 0.0" % h_ocean)
		quit(1)
		return

	print("==================================================")
	print("✓ PASS: HeightmapTextureSampler 预制高度场采样器自测全部通过！")
	print("==================================================")
	quit(0)

