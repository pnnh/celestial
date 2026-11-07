extends SceneTree

func _init():
	print("==================================================")
	print("[Test] 正在执行普适物理基岩与 3D 实体拾取自动化验证...")
	print("==================================================")

	var detector_script = load("res://src/Player/BedrockPhysicsDetector.cs")
	if not detector_script:
		push_error("FAILED: 无法载入 BedrockPhysicsDetector.cs")
		quit(1)
		return

	var detector = detector_script.new()
	var sampler = load("res://src/Terrain/IslandHeightSampler.cs").new()

	# 1. 模拟命中 3D 静态石柱 (StaticBody3D，非体素 Chunk)
	var static_body = StaticBody3D.new()
	static_body.name = "AncientPillar"

	var ray_hit_3d = {
		"position": Vector3(12.2, 5.0, -12.4),
		"normal": Vector3(0.0, 1.0, 0.0),
		"collider": static_body
	}

	var res_3d = detector.AnalyzeHitDictionary(ray_hit_3d, sampler)
	print("[Check 1] 命中 3D 静态石柱类型: %d (预期 Bedrock = 2)" % res_3d.Type)
	if res_3d.Type != 2:
		push_error("FAILED: 传统 3D 实体未被识别为基岩！类型: %d" % res_3d.Type)
		quit(1)
		return

	# 检查放置对齐吸附坐标
	print("[Check 1b] 3D 石柱表面放置坐标: %s (预期 (12, 5, -13))" % res_3d.PlaceVoxelCoord)
	if res_3d.PlaceVoxelCoord.y != 5:
		push_error("FAILED: 3D 实体表面吸附高度不符: %s" % res_3d.PlaceVoxelCoord)
		quit(1)
		return

	# 2. 模拟命中体素 Chunk 中低于地表高度的方块
	var chunk = Node3D.new()
	chunk.set_script(load("res://src/Voxel/VoxelChunk.cs"))
	var ray_hit_subsurface = {
		"position": Vector3(0.5, 2.0, 0.5), # 中心平原高度约 14.6m，Y=2.0 位于地表以下
		"normal": Vector3(0.0, 1.0, 0.0),
		"collider": chunk
	}

	var res_sub = detector.AnalyzeHitDictionary(ray_hit_subsurface, sampler)
	print("[Check 2] 命中地表以下体素类型: %d (预期 Bedrock = 2)" % res_sub.Type)
	if res_sub.Type != 2:
		push_error("FAILED: 地表以下体素未判定为基岩！")
		quit(1)
		return

	# 3. 模拟命中体素 Chunk 中高于地表的方块
	var ray_hit_air_voxel = {
		"position": Vector3(0.5, 20.0, 0.5), # Y=20.0 处于地表上方
		"normal": Vector3(0.0, 1.0, 0.0),
		"collider": chunk
	}

	var res_air = detector.AnalyzeHitDictionary(ray_hit_air_voxel, sampler)
	print("[Check 3] 命中地上可破坏体素类型: %d (预期 Voxel = 1)" % res_air.Type)
	if res_air.Type != 1:
		push_error("FAILED: 地表上方体素未判定为可破坏 Voxel！")
		quit(1)
		return

	print("==================================================")
	print("✓ PASS: BedrockPhysicsDetector 普适物理基岩与拾取判定全部通过！")
	print("==================================================")
	quit(0)
