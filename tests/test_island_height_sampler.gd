extends SceneTree

func _init():
	print("==================================================")
	print("[Test] 正在执行海岛高度场采样器自动化验证...")
	print("==================================================")

	var sampler_script = load("res://src/Terrain/IslandHeightSampler.cs")
	if not sampler_script:
		push_error("FAILED: 无法载入 IslandHeightSampler.cs")
		quit(1)
		return

	var sampler = sampler_script.new()
	if not sampler:
		push_error("FAILED: 无法实例化 IslandHeightSampler")
		quit(1)
		return

	# 1. 验证中心微波平原 (0, 0)
	var center_h = sampler.GetHeight(0.0, 0.0)
	print("[Check 1] 中心原点 (0, 0) 高度: %0.2f m (预期 5.0 ~ 16.0m)" % center_h)
	if center_h < 5.0 or center_h > 16.0:
		push_error("FAILED: 中心平原高度超出正常范围: %0.2f" % center_h)
		quit(1)
		return

	# 2. 验证内陆山脊顶峰 (110, -80)
	var peak_h = sampler.GetHeight(110.0, -80.0)
	print("[Check 2] 东北山脊腹地 (110, -80) 顶峰高度: %0.2f m (预期 40.0 ~ 75.0m)" % peak_h)
	if peak_h < 40.0 or peak_h > 75.0:
		push_error("FAILED: 山脊顶峰高度超出正常范围: %0.2f" % peak_h)
		quit(1)
		return

	# 3. 验证外围大洋 (R > 480m) 全部沉入海平面以下
	var outer_points = [
		Vector2(500.0, 0.0),
		Vector2(-500.0, 0.0),
		Vector2(0.0, 500.0),
		Vector2(0.0, -500.0),
		Vector2(400.0, 400.0) # R ~ 565m
	]
	for pt in outer_points:
		var ocean_h = sampler.GetHeight(pt.x, pt.y)
		print("[Check 3] 外围海域 (%0.0f, %0.0f) 高度: %0.2f m (预期 < 0.0m)" % [pt.x, pt.y, ocean_h])
		if ocean_h >= 0.0:
			push_error("FAILED: 外围海域未沉入海平面: %0.2f" % ocean_h)
			quit(1)
			return

	# 4. 验证 2,000 次随机连续采样 (连续性与数值稳定性)
	var rng = RandomNumberGenerator.new()
	rng.seed = 42
	for i in range(2000):
		var rx = rng.randf_range(-600.0, 600.0)
		var rz = rng.randf_range(-600.0, 600.0)
		var h = sampler.GetHeight(rx, rz)
		if is_nan(h) or is_inf(h):
			push_error("FAILED: 采样出现 NaN 或 Inf! 坐标: (%0.2f, %0.2f)" % [rx, rz])
			quit(1)
			return
		
		# 相邻微步采样斜率不可突变 (连续性)
		var h_step = sampler.GetHeight(rx + 0.5, rz)
		var slope = abs(h_step - h) / 0.5
		if slope > 8.0: # 允许最大约 80 度峭壁，不允许断崖瞬变无穷大
			push_error("FAILED: 连续采样斜率突变异常: %0.2f 在 (%0.2f, %0.2f)" % [slope, rx, rz])
			quit(1)
			return

	# 5. 验证地表法线向量计算
	var norm = sampler.GetNormal(0.0, 0.0)
	var norm_len = norm.length()
	print("[Check 5] 中心点法线: %s, 模长: %0.4f (预期 ~ 1.0, Y > 0.8)" % [norm, norm_len])
	if abs(norm_len - 1.0) > 0.01 or norm.y < 0.8:
		push_error("FAILED: 中心平原法线向量异常: %s" % norm)
		quit(1)
		return

	print("==================================================")
	print("✓ PASS: IslandHeightSampler 海岛高度场采样器验证全部通过！")
	print("==================================================")
	quit(0)

