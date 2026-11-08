extends SceneTree

var _terrain_mgr = null
var _frames = 0

func _init():
	print("==================================================")
	print("[Test] 正在执行三平面贴图着色器与材质绑定自动化验证...")
	print("==================================================")

	# 1. 验证 Shader 资源加载与有效性
	var shader = load("res://shaders/terrain_triplanar.gdshader")
	if not shader or not (shader is Shader):
		push_error("FAILED: 无法载入 res://shaders/terrain_triplanar.gdshader")
		quit(1)
		return

	print("[Check 1] 成功载入 terrain_triplanar.gdshader")

	# 2. 验证 TiledTerrainManager 挂载
	var mgr_script = load("res://src/Terrain/TiledTerrainManager.cs")
	if not mgr_script:
		push_error("FAILED: 无法载入 TiledTerrainManager.cs")
		quit(1)
		return

	_terrain_mgr = mgr_script.new()
	root.add_child(_terrain_mgr)

func _process(delta: float) -> bool:
	_frames += 1
	if _frames < 3:
		return false

	var mat = _terrain_mgr.TerrainMaterial
	if not mat or not (mat is ShaderMaterial):
		push_error("FAILED: TiledTerrainManager.TerrainMaterial 为空或非 ShaderMaterial")
		_terrain_mgr.queue_free()
		quit(1)
		return true

	var smat = mat as ShaderMaterial
	print("[Check 2] 成功创建并绑定 ShaderMaterial")

	# 3. 验证三贴图绑定 (草地、岩石、沙滩)
	var grass_tex = smat.get_shader_parameter("grass_texture")
	var rock_tex = smat.get_shader_parameter("rock_texture")
	var sand_tex = smat.get_shader_parameter("sand_texture")

	print("[Check 3] 草地贴图: %s, 岩石贴图: %s, 沙滩贴图: %s" % [grass_tex, rock_tex, sand_tex])
	if not grass_tex or not rock_tex or not sand_tex:
		push_error("FAILED: 贴图采样器未完全绑定 (grass, rock, sand 必须全为有效 Texture)")
		_terrain_mgr.queue_free()
		quit(1)
		return true

	# 4. 验证三平面缩放与坡度参数
	var uv_scale = smat.get_shader_parameter("uv_scale")
	var slope_threshold = smat.get_shader_parameter("slope_threshold")
	print("[Check 4] UV 缩放: %s, 坡度阈值: %s" % [uv_scale, slope_threshold])

	_terrain_mgr.queue_free()

	print("==================================================")
	print("✓ PASS: 三平面贴图着色器与材质绑定验证全部通过！")
	print("==================================================")
	quit(0)
	return true
