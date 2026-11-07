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

void vertex() {
	// 赛璐璐手绘微波轻浮动
	float wave = sin(VERTEX.x * 0.5 + TIME * 1.5) * cos(VERTEX.z * 0.5 + TIME * 1.2);
	VERTEX.y += wave * wave_amplitude;
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

	vec3 final_color = mix(base_color.rgb, foam_color.rgb, foam * 0.9);
	ALBEDO = final_color;
	ALPHA = mix(base_color.a, 1.0, foam);
	ROUGHNESS = 0.2;
	SPECULAR = 0.5;
}

