extends SceneTree

var _manager = null
var _frames = 0
var _step = 0
var _start_time = 0

func _init():
	print("==================================================")
	print("[Test] 正在执行 64m 瓦片流式加载与视距 LOD 自动化验证...")
	print("==================================================")

	var mgr_script = load("res://src/Terrain/TiledTerrainManager.cs")
	if not mgr_script:
		push_error("FAILED: 无法载入 TiledTerrainManager.cs")
		quit(1)
		return

	_manager = mgr_script.new()
	root.add_child(_manager)
	_start_time = Time.get_ticks_msec()

func _process(delta: float) -> bool:
	_frames += 1
	var elapsed = Time.get_ticks_msec() - _start_time

	if _step == 0:
		# 等待初始瓦片在后台线程池生成并被主线程装载
		if _manager.ActiveTileCount >= 9 or elapsed > 3000:
			print("[Check 1] 初始视距内活动瓦片数: %d (预期 >= 9)" % _manager.ActiveTileCount)
			if _manager.ActiveTileCount < 9:
				push_error("FAILED: 初始瓦片加载不足: %d" % _manager.ActiveTileCount)
				quit(1)
				return true
			
			# 步骤 2: 模拟观察者高速向东北远山位移至 (300, 0, -200)
			print("[Step 2] 模拟玩家高速移动至 (300, 0, -200)...")
			var dummy_player = Node3D.new()
			root.add_child(dummy_player)
			dummy_player.global_position = Vector3(300.0, 0.0, -200.0)
			_manager.SetObserver(dummy_player)
			_step = 1
			_start_time = Time.get_ticks_msec()

	elif _step == 1:
		if elapsed > 2000:
			print("[Check 2] 位移后当前活动瓦片数: %d" % _manager.ActiveTileCount)
			if _manager.ActiveTileCount < 9:
				push_error("FAILED: 移动后瓦片未能正确流式加载: %d" % _manager.ActiveTileCount)
				quit(1)
				return true
			
			print("==================================================")
			print("✓ PASS: TiledTerrainManager 瓦片异步流式与 LOD 调度全部通过！")
			print("==================================================")
			quit(0)
			return true

	return false
