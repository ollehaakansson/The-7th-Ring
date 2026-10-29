shader_type canvas_item;
uniform sampler2D screen_texture : hint_screen_texture, filter_linear;
void fragment() {
	vec2 step_uv = SCREEN_PIXEL_SIZE * 2.0;
	vec3 scene = texture(screen_texture, SCREEN_UV).rgb * 0.25;
	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			if (x == 0 && y == 0) continue;
			float weight = (x == 0 || y == 0) ? 0.125 : 0.0625;
			scene += texture(screen_texture, SCREEN_UV + vec2(float(x), float(y)) * step_uv).rgb * weight;
		}
	}
	COLOR = vec4(scene * vec3(0.62, 0.55, 0.55), 1.0);
}
