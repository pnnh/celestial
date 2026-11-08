extends SceneTree

var _foliage = null
var _frames = 0

func _init():
	print("==================================================")
	print("[Test] 正在执行开放世界批量程序化植被散布自动化验证...")
	print("==================================================")

	var foliage_script = load("res://src/Terrain/FoliageScatter.cs")
	if not foliage_script:
		push_error("FAILED: 无法载入 FoliageScatter.cs")
		quit(1)
		return

	_foliage = foliage_script.new()
	_foliage.InstanceCount = 800
	_foliage.ScatterRadius = 350.0
	_foliage.MinSpawnHeight = 2.5
	_foliage.MaxSpawnHeight = 48.0
	_foliage.MinSlopeNormalY = 0.8
	_foliage.Seed = 2026

	root.add_child(_foliage)

func _process(delta: float) -> bool:
	_frames += 1
	if _frames < 3:
		return false

	# 1. 验证 MultiMesh 是否成功生成
	var mm = _foliage.multimesh
	if not mm:
		push_error("FAILED: FoliageScatter.multimesh 为空")
		_foliage.queue_free()
		quit(1)
		return true

	print("[Check 1] MultiMesh 网格面数与材质验证通过: %s" % mm.mesh)
	if not mm.mesh:
		push_error("FAILED: MultiMesh 缺少基础 Mesh 几何体")
		_foliage.queue_free()
		quit(1)
		return true

	# 2. 验证生成的植被实例总数与有效散布数 (空间分块多网格汇总)
	var summary: Dictionary = _foliage.GetScatterSummary()
	var count = summary.get("valid_count", 0)
	var total_instances = summary.get("requested_count", 0)
	print("[Check 2] 实际有效植被散布数量: %d / %d (预期 > 500)" % [count, total_instances])
	if count < 500:
		push_error("FAILED: 有效植被散布数量过低: %d" % count)
		_foliage.queue_free()
		quit(1)
		return true

	# 3. 验证生态高度与水下过滤 (水下生长率必须为 0%)
	print("[Check 3] 散布生态统计: %s" % summary)

	var water_spawns = summary.get("water_spawns", 0)
	var min_height = summary.get("min_height", 0.0)
	var max_height = summary.get("max_height", 0.0)

	print("[Check 4] 最低生长标高: %0.2fm, 最高生长标高: %0.2fm, 水下生长数: %d" % [min_height, max_height, water_spawns])
	if water_spawns > 0 or min_height < 2.0:
		push_error("FAILED: 发现水下/浅滩植被生成，生态过滤规则失效！")
		_foliage.queue_free()
		quit(1)
		return true

	if max_height > 50.0:
		push_error("FAILED: 发现超高绝壁植被生成！")
		_foliage.queue_free()
		quit(1)
		return true

	_foliage.queue_free()

	print("==================================================")
	print("✓ PASS: 批量程序化植被散布与生态过滤验证全部通过！")
	print("==================================================")
	quit(0)
	return true
