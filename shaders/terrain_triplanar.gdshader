shader_type spatial;
render_mode blend_mix, cull_back, diffuse_toon, specular_toon;

uniform vec4 grass_color : source_color = vec4(0.38, 0.65, 0.22, 1.0);
uniform vec4 rock_color : source_color = vec4(0.48, 0.46, 0.43, 1.0);
uniform vec4 sand_color : source_color = vec4(0.86, 0.78, 0.54, 1.0);

uniform float slope_threshold : hint_range(0.1, 0.9, 0.05) = 0.65;
uniform float slope_blend : hint_range(0.01, 0.5, 0.02) = 0.15;
uniform float sand_height_max = 2.0;
uniform float sand_blend = 1.5;

void fragment() {
	// 世界法线与高度
	vec3 world_normal = (INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
	vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float slope = world_normal.y; // 1.0 表示水平向上，0.0 表示垂直悬崖

	// 坡度权重：平缓为草地，陡峭为岩石
	float grass_weight = smoothstep(slope_threshold - slope_blend, slope_threshold + slope_blend, slope);
	vec3 surface_color = mix(rock_color.rgb, grass_color.rgb, grass_weight);

	// 海平面沙滩过渡 (Y 接近或低于 sand_height_max)
	float sand_factor = 1.0 - smoothstep(sand_height_max - sand_blend, sand_height_max + sand_blend, world_pos.y);
	if (sand_factor > 0.0) {
		surface_color = mix(surface_color, sand_color.rgb, sand_factor * grass_weight);
	}

	ALBEDO = surface_color;
	ROUGHNESS = 0.85;
	SPECULAR = 0.1;
}

