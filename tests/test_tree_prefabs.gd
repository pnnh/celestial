extends SceneTree

func _init():
	print("==================================================")
	print("[Test] 正在验证 3 种核心树木预制体（橡木、云杉、白桦）规范与碰撞体...")
	print("==================================================")

	var tree_prefabs = [
		{"name": "橡木", "path": "res://scenes/prefabs/trees/oak_tree.tscn"},
		{"name": "云杉", "path": "res://scenes/prefabs/trees/spruce_tree.tscn"},
		{"name": "白桦", "path": "res://scenes/prefabs/trees/birch_tree.tscn"}
	]

	for item in tree_prefabs:
		var name = item["name"]
		var path = item["path"]
		if not ResourceLoader.exists(path):
			push_error("FAILED: 预制体未找到: %s" % path)
			quit(1)
			return

		var scene = load(path)
		var instance = scene.instantiate()
		root.add_child(instance)

		# 1. 检查 StaticBody3D
		var sb: StaticBody3D = instance.find_child("StaticBody3D", true, false)
		if sb == null:
			push_error("FAILED: %s 预制体内部缺少 StaticBody3D 物理体！" % name)
			quit(1)
			return
		print("[Check 1] %s 包含 StaticBody3D (Layer: %d)" % [name, sb.collision_layer])
		if sb.collision_layer != 1:
			push_error("FAILED: %s StaticBody3D 碰撞层不是 Layer 1！" % name)
			quit(1)
			return

		# 2. 检查 CollisionShape3D
		var col: CollisionShape3D = sb.find_child("CollisionShape3D", true, false)
		if col == null:
			push_error("FAILED: %s 缺少 CollisionShape3D！" % name)
			quit(1)
			return

		# 3. 严格检查 Scale == Vector3.ONE (杜绝 Godot 缩放警告)
		var scale_diff = (col.scale - Vector3.ONE).length()
		print("[Check 2] %s CollisionShape3D 缩放值: %s (误差: %0.4f)" % [name, col.scale, scale_diff])
		if scale_diff > 0.001:
			push_error("FAILED: %s CollisionShape3D 存在非统一缩放！必须保持 (1, 1, 1)！" % name)
			quit(1)
			return

		# 4. 检查圆柱形状参数
		if not col.shape is CylinderShape3D:
			push_error("FAILED: %s 树干碰撞体不是 CylinderShape3D！" % name)
			quit(1)
			return

		var cyl: CylinderShape3D = col.shape
		print("[Check 3] %s 树干圆柱碰撞参数: 半径=%0.2fm, 高度=%0.2fm" % [name, cyl.radius, cyl.height])
		if cyl.radius < 0.1 or cyl.radius > 0.5:
			push_error("FAILED: %s 半径超出合理范围 [0.1m, 0.5m]" % name)
			quit(1)
			return

		instance.queue_free()

	print("==================================================")
	print("✓ PASS: 核心树木预制体与无告警树干碰撞体验证全部通过！")
	print("==================================================")
	quit(0)

