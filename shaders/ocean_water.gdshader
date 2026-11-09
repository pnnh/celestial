shader_type spatial;
render_mode blend_mix, depth_draw_always, cull_back, diffuse_toon, specular_toon;

uniform vec4 shallow_color : source_color = vec4(0.22, 0.75, 0.82, 0.85);
uniform vec4 deep_color : source_color = vec4(0.08, 0.28, 0.58, 0.95);
uniform vec4 foam_color : source_color = vec4(0.95, 0.98, 1.0, 1.0);
uniform float foam_distance : hint_range(0.1, 3.0, 0.1) = 0.8;
uniform float depth_distance : hint_range(0.5, 30.0, 0.5) = 10.0;
uniform sampler2D depth_texture : hint_depth_texture, filter_linear_mipmap;

uniform vec2 wave_speed = vec2(0.04, 0.02);
uniform float wave_amplitude = 0.12;

varying vec3 v_world_pos;

void vertex() {
	// 赛璐璐手绘微波轻浮动
	float wave = sin(VERTEX.x * 0.5 + TIME * 1.5) * cos(VERTEX.z * 0.5 + TIME * 1.2);
	VERTEX.y += wave * wave_amplitude;
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// 读取背景深度
	float depth = texture(depth_texture, SCREEN_UV).r;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float linear_depth = -view.z;
	float water_depth = linear_depth - VERTEX.z;

	// 深度阶梯渐变 (Toon Depth Stepping)
	float depth_factor = clamp(water_depth / depth_distance, 0.0, 1.0);
	float stepped_depth = floor(depth_factor * 4.0) / 4.0;
	vec4 base_color = mix(shallow_color, deep_color, stepped_depth);

	// 海岸交界白色浪沫 (Shoreline Foam)
	float foam = 0.0;
	if (water_depth < foam_distance && water_depth > 0.0) {
		foam = 1.0 - smoothstep(0.0, foam_distance, water_depth);
	}
	// 关键：浪沫仅在近处海岸呈现，超出 120 米或远景消隐区平滑归零，严防远方地形截断处触发白色泡沫线
	float cam_dist = length(VERTEX);
	foam *= (1.0 - smoothstep(50.0, 120.0, cam_dist));

	vec3 final_color = mix(base_color.rgb, foam_color.rgb, foam * 0.9);

	// 远景地平线与天际线平滑渐变融合 (消除大洋网格拐角、白带与硬切边)
	float island_dist = length(v_world_pos.xz);
	float island_radial_fade = smoothstep(180.0, 340.0, island_dist);
	float cam_fade = smoothstep(140.0, 320.0, cam_dist);
	float horizon_fade = clamp(max(island_radial_fade, cam_fade), 0.0, 1.0);

	// 统一天空与晨雾同款清爽天青蔚蓝中介色 (与 sky_horizon_color 及 fog_light_color 严格同色)
	vec3 horizon_color = vec3(0.46, 0.68, 0.88);

	// 关键光学解耦：
	// 远景海面 ALBEDO 平滑衰减为 0，由 EMISSION 接管精准注入 horizon_color
	// 彻底杜绝主平行光与环境光在远海平面叠加将材质照成高亮白，实现海天一色自然衔接
	ALBEDO = mix(final_color, vec3(0.0), horizon_fade);
	EMISSION = horizon_color * horizon_fade;
	ALPHA = mix(base_color.a, 1.0, clamp(foam + horizon_fade, 0.0, 1.0));
	ROUGHNESS = mix(0.2, 1.0, horizon_fade);
	SPECULAR = mix(0.5, 0.0, horizon_fade);
}

