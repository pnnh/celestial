extends SceneTree

func _init():
	print("==================================================")
	print("[Test] 正在验证峰顶放置方块的再次挖掘与基岩阻隔精准性...")
	print("==================================================")

	var world_script = load("res://src/Voxel/VoxelWorld.cs")
	var world = world_script.new()
	root.add_child(world)

	var sampler_script = load("res://src/Terrain/IslandHeightSampler.cs")
	var sampler = sampler_script.new()
	world.SetHeightSampler(sampler)

	# 坐标 (105, 67, -80) 是山顶地表附近
	var h = sampler.GetHeight(105.5, -79.5)
	var floor_h = int(floor(h))
	print("[Check 1] 山顶 (105, -80) 地表连续高度: %0.2fm, 地表基准整数 Y: %d" % [h, floor_h])

	# 1. 在地表以下 (floor_h - 1) 必须不可挖掘
	var can_dig_underground = world.CanDig(105, floor_h - 1, -80)
	print("[Check 2] 地表以下 Y=%d 是否可挖: %s (预期 false)" % [floor_h - 1, can_dig_underground])
	if can_dig_underground:
		push_error("FAILED: 地下基岩未被正确阻隔！")
		quit(1)
		return

	# 2. 在地表表面 (floor_h) 玩家放置一个 Dirt 方块
	world.SetVoxel(105, floor_h, -80, 1) # VoxelType.Dirt = 1
	var can_dig_placed = world.CanDig(105, floor_h, -80)
	print("[Check 3] 地表放置方块 Y=%d 是否可挖: %s (预期 true)" % [floor_h, can_dig_placed])
	if not can_dig_placed:
		push_error("FAILED: 玩家刚放置在山顶地表的方块被误判为基岩无法挖掘！")
		quit(1)
		return

	# 3. 执行挖掘移除方块
	world.SetVoxel(105, floor_h, -80, 0) # VoxelType.Air = 0
	print("[Check 4] 移除方块后该位置方块类型: %d (预期 Air=0)" % world.GetVoxel(105, floor_h, -80))

	world.queue_free()
	print("==================================================")
	print("✓ PASS: 山顶地表放置与二次挖掘 Bug 修复验证通过！")
	print("==================================================")
	quit(0)
