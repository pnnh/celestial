extends SceneTree

var _volume = null
var _done = false

func _init():
	print("==================================================")
	print("[Test] 正在执行局部体素卷 (VoxelVolume) 边界与挖掘验证...")
	print("==================================================")

	var volume_script = load("res://src/Voxel/VoxelVolume.cs")
	if not volume_script:
		push_error("FAILED: 无法载入 VoxelVolume.cs")
		quit(1)
		return

	_volume = volume_script.new()
	_volume.Extents = Vector3(32.0, 16.0, 32.0)
	_volume.position = Vector3(0.0, 14.0, 0.0) # 中心平原标高约 14.6m
	root.add_child(_volume)

func _process(delta: float) -> bool:
	if _done:
		return true
	_done = true

	# 1. 验证空间包围盒包含性 (ContainsWorldPosition)
	var inside_pt = Vector3(0.0, 14.0, 0.0)
	var outside_pt = Vector3(100.0, 14.0, 0.0)
	print("[Check 1] 内部点判定: %s (预期 true), 外部点判定: %s (预期 false)" % [
		_volume.ContainsWorldPosition(inside_pt),
		_volume.ContainsWorldPosition(outside_pt)
	])
	if not _volume.ContainsWorldPosition(inside_pt) or _volume.ContainsWorldPosition(outside_pt):
		push_error("FAILED: 空间包围盒包含性判定错误！")
		quit(1)
		return true

	# 2. 验证地表以下实心基岩查询 (wy < 14m 时返回 Bedrock = 4)
	var subsurface_pt = Vector3(0.0, 10.0, 0.0)
	var subsurface_voxel = _volume.GetVoxelAtWorld(subsurface_pt)
	print("[Check 2] 地表以下查询体素类型: %d (预期 Bedrock = 4)" % subsurface_voxel)
	if subsurface_voxel != 4:
		push_error("FAILED: 地表以下未返回 Bedrock！当前类型: %d" % subsurface_voxel)
		quit(1)
		return true

	# 3. 验证地表泥土挖掘尝试 (基岩不可挖掘)
	var can_dig_subsurface = _volume.TryDigAtWorld(subsurface_pt)
	print("[Check 3] 地表下基岩尝试挖掘结果: %s (预期 false)" % can_dig_subsurface)
	if can_dig_subsurface:
		push_error("FAILED: 地表以下基岩被错误允许挖掘！")
		quit(1)
		return true

	print("==================================================")
	print("✓ PASS: VoxelVolume 局部体素卷边界与基岩封闭验证全部通过！")
	print("==================================================")
	quit(0)
	return true
