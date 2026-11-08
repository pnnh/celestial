shader_type spatial;
render_mode blend_mix, cull_back, diffuse_toon, specular_toon;

uniform sampler2D grass_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D rock_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D sand_texture : source_color, filter_linear_mipmap, repeat_enable;

uniform vec4 grass_color : source_color = vec4(0.42, 0.68, 0.25, 1.0);
uniform vec4 rock_color : source_color = vec4(0.55, 0.52, 0.48, 1.0);
uniform vec4 sand_color : source_color = vec4(0.86, 0.78, 0.52, 1.0);

uniform float uv_scale = 0.25;      // 水平草地与沙滩 UV 平铺缩放 (每 4 米一个循环)
uniform float rock_uv_scale = 0.20; // 垂直岩石 UV 缩放

uniform float slope_threshold : hint_range(0.1, 0.9, 0.05) = 0.65;
uniform float slope_blend : hint_range(0.01, 0.5, 0.02) = 0.15;
uniform float sand_height_max = 2.2;
uniform float sand_blend = 1.2;

void fragment() {
	// 1. 世界绝对坐标与世界法线
	vec3 world_normal = (INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
	vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

	// 2. 三平面投影权重计算 (Triplanar Blending Weights)
	vec3 blending = abs(world_normal);
	blending = pow(blending, vec3(4.0)); // 提高锐度，防止斜面模糊
	blending = normalize(max(blending, vec3(0.00001)));
	float b_sum = blending.x + blending.y + blending.z;
	blending /= b_sum;

	// 3. 悬崖岩石三平面采样 (彻底消除悬崖拉伸)
	vec3 rock_x = texture(rock_texture, world_pos.zy * rock_uv_scale).rgb;
	vec3 rock_y = texture(rock_texture, world_pos.xz * rock_uv_scale).rgb;
	vec3 rock_z = texture(rock_texture, world_pos.xy * rock_uv_scale).rgb;
	vec3 rock_tex = (rock_x * blending.x + rock_y * blending.y + rock_z * blending.z) * rock_color.rgb;

	// 4. 水平草地与沙滩采样
	vec3 grass_tex = texture(grass_texture, world_pos.xz * uv_scale).rgb * grass_color.rgb;
	vec3 sand_tex = texture(sand_texture, world_pos.xz * uv_scale).rgb * sand_color.rgb;

	// 5. 基于法线坡度混合草地与岩石
	float slope = world_normal.y; // 1.0 表示水平向上，0.0 表示垂直悬崖
	float grass_weight = smoothstep(slope_threshold - slope_blend, slope_threshold + slope_blend, slope);
	vec3 surface_color = mix(rock_tex, grass_tex, grass_weight);

	// 6. 海平面沙滩过渡 (Y 接近或低于 sand_height_max)
	float sand_factor = 1.0 - smoothstep(sand_height_max - sand_blend, sand_height_max + sand_blend, world_pos.y);
	if (sand_factor > 0.0) {
		surface_color = mix(surface_color, sand_tex, clamp(sand_factor * 1.5, 0.0, 1.0) * grass_weight);
	}

	ALBEDO = surface_color;
	ROUGHNESS = 0.85;
	SPECULAR = 0.1;
}
