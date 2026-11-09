shader_type sky;

uniform vec3 sky_top_color : source_color = vec3(0.18, 0.52, 0.92);
uniform vec3 sky_horizon_color : source_color = vec3(0.46, 0.68, 0.88);
uniform vec3 ground_bottom_color : source_color = vec3(0.08, 0.28, 0.58);
uniform vec3 ground_horizon_color : source_color = vec3(0.46, 0.68, 0.88);

uniform float sky_curve : hint_range(0.1, 2.0, 0.05) = 0.55;

// 手绘二次元赛璐璐太阳配置
uniform float sun_size : hint_range(0.01, 0.15, 0.005) = 0.045;
uniform float sun_halo_size : hint_range(0.05, 0.45, 0.01) = 0.18;
uniform vec3 sun_color : source_color = vec3(1.0, 0.99, 0.95);
uniform vec3 sun_halo_color : source_color = vec3(1.0, 0.88, 0.65);

// 动态手绘积云配置
uniform float cloud_scale : hint_range(0.02, 0.5, 0.01) = 0.065;
uniform vec2 cloud_speed = vec2(0.003, 0.0015);
uniform float cloud_coverage : hint_range(0.2, 0.8, 0.02) = 0.42;
uniform float cloud_softness : hint_range(0.01, 0.3, 0.01) = 0.06;
uniform float cloud_opacity : hint_range(0.0, 1.0, 0.05) = 0.95;
uniform vec3 cloud_color_light : source_color = vec3(1.0, 0.99, 0.96);
uniform vec3 cloud_color_shadow : source_color = vec3(0.70, 0.80, 0.93);

vec2 hash2(vec2 p) {
	p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
	return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

float gnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(dot(hash2(i + vec2(0.0, 0.0)), f - vec2(0.0, 0.0)),
	               dot(hash2(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0)), u.x),
	           mix(dot(hash2(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0)),
	               dot(hash2(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0)), u.x), u.y);
}

float fbm(vec2 p) {
	float f = 0.0;
	f += 0.5000 * gnoise(p); p *= 2.02;
	f += 0.2500 * gnoise(p); p *= 2.03;
	f += 0.1250 * gnoise(p); p *= 2.01;
	return f * 0.5 + 0.5;
}

void sky() {
	float y = EYEDIR.y;
	vec3 col;

	// 渐变天空底色
	if (y >= 0.0) {
		float factor = pow(clamp(y, 0.0, 1.0), sky_curve);
		col = mix(sky_horizon_color, sky_top_color, factor);
	} else {
		float factor = pow(clamp(-y, 0.0, 1.0), 0.8);
		col = mix(ground_horizon_color, ground_bottom_color, factor);
	}

	// 1. 手绘赛璐璐太阳 (跟随主定向光朝向，支持兜底朝向)
	vec3 sun_dir = LIGHT0_ENABLED ? LIGHT0_DIRECTION : normalize(vec3(0.452, 0.616, -0.646));
	if (y > -0.05) {
		float sun_cos = dot(EYEDIR, sun_dir);
		float disc_threshold = 1.0 - (sun_size * sun_size * 0.5);
		float halo_threshold = 1.0 - (sun_halo_size * sun_halo_size * 0.5);

		float halo = smoothstep(halo_threshold, disc_threshold, sun_cos) * 0.55;
		float disc = smoothstep(disc_threshold - 0.001, disc_threshold + 0.001, sun_cos);

		col = mix(col, sun_halo_color, halo);
		col = mix(col, sun_color, disc);
	}

	// 2. 动态手绘积云 (在上半球投影计算，结合域扭曲营造棉花糖蓬松积云)
	if (y > 0.01) {
		vec2 sky_uv = EYEDIR.xz / max(y + 0.18, 0.12);
		vec2 uv = sky_uv * cloud_scale + TIME * cloud_speed;

		// 域扭曲增强云朵流线边缘
		vec2 warp = vec2(fbm(uv), fbm(uv + vec2(4.3, 1.7))) * 0.35;
		float n = fbm(uv + warp);
		float billow = fbm((uv + warp) * 1.8);
		float density = n * 0.65 + billow * 0.35;

		float cloud_mask = smoothstep(cloud_coverage, cloud_coverage + cloud_softness, density);

		// 向光/背光二分赛璐璐光照
		float sun_dot = dot(EYEDIR, sun_dir);
		float light_factor = clamp(sun_dot * 0.4 + 0.6, 0.0, 1.0);
		float shadow_t = smoothstep(cloud_coverage, cloud_coverage + cloud_softness * 2.5, density);
		vec3 cloud_col = mix(cloud_color_shadow, cloud_color_light, shadow_t * light_factor);

		// 地平线柔和消隐 (平滑融入晨雾与地平线色彩)
		float horizon_fade = smoothstep(0.01, 0.10, y);
		cloud_mask *= horizon_fade;

		col = mix(col, cloud_col, cloud_mask * cloud_opacity);
	}

	COLOR = col;
}
