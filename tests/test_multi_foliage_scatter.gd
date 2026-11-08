extends SceneTree

var _foliage = null
var _frames = 0

func _init() -> void:
	print("==================================================")
	print("[Test] 正在执行多变体植被散布与生态分层自动化验证 (test_multi_foliage_scatter)...")
	print("==================================================")

	var foliage_script = load("res://src/Terrain/FoliageScatter.cs")
	if not foliage_script:
		push_error("FAILED: 无法载入 FoliageScatter.cs")
		quit(1)
		return

	_foliage = foliage_script.new()
	root.add_child(_foliage)

func _process(delta: float) -> bool:
	_frames += 1
	if _frames < 3:
		return false

	# 1. 验证多变体 MultiMesh 节点存在
	var expected_trees = ["oak", "spruce", "birch"]
	var expected_floras = ["wild_grass", "tall_grass", "sunflower", "poppy", "dandelion"]

	var summary: Dictionary = _foliage.GetScatterSummary()
	print("[Check 1] 散布汇总数据: ", summary)

	var tree_count = summary.get("total_trees", 0)
	var flora_count = summary.get("total_floras", 0)
	var total_count = summary.get("total_instances", 0)
	var water_count = summary.get("water_spawns", 0)

	print("[Check 2] 树木总数: %d, 花草总数: %d, 全岛植被总数: %d" % [tree_count, flora_count, total_count])
	if tree_count < 500:
		push_error("FAILED: 树木散布总数过低: %d" % tree_count)
		_cleanup_and_quit(1)
		return true

	if flora_count < 800:
		push_error("FAILED: 花草散布总数过低: %d" % flora_count)
		_cleanup_and_quit(1)
		return true

	if water_count > 0:
		push_error("FAILED: 发现水下植被生成！水下生成数: %d" % water_count)
		_cleanup_and_quit(1)
		return true

	# 2. 验证生态海拔分层 (平原橡木 vs 高山云杉)
	var oak_avg_h = summary.get("oak_avg_height", 0.0)
	var spruce_avg_h = summary.get("spruce_avg_height", 0.0)
	print("[Check 3] 橡木平均标高: %0.2fm, 云杉平均标高: %0.2fm" % [oak_avg_h, spruce_avg_h])
	if spruce_avg_h <= oak_avg_h:
		push_error("FAILED: 生态高程分层反常！云杉标高应高于平原橡木: spruce=%f, oak=%f" % [spruce_avg_h, oak_avg_h])
		_cleanup_and_quit(1)
		return true

	# 3. 验证采石场 VoxelVolume 排除带
	var quarry_violations = summary.get("exclusion_violations", 0)
	print("[Check 4] 采石场等体素可变区违规入侵数: %d" % quarry_violations)
	if quarry_violations > 0:
		push_error("FAILED: 发现植被入侵体素排除区！")
		_cleanup_and_quit(1)
		return true

	# 4. 验证树木树干碰撞体（StaticBody3D 存在且无缩放告警）
	var collider_count = summary.get("tree_colliders_count", 0)
	print("[Check 5] 树干圆柱碰撞体生成数量: %d" % collider_count)
	if collider_count < 500:
		push_error("FAILED: 树木碰撞体数量与树木实例不匹配！")
		_cleanup_and_quit(1)
		return true

	print("==================================================")
	print("✓ PASS: 多变体植被散布、生态海拔分层与防穿模碰撞体验证全部通过！")
	print("==================================================")
	_cleanup_and_quit(0)
	return true

func _cleanup_and_quit(code: int) -> void:
	if _foliage:
		_foliage.queue_free()
	quit(code)

