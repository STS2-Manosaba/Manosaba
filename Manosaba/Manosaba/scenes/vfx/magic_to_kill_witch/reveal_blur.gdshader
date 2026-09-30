shader_type canvas_item;
render_mode unshaded;
uniform float blur_pixels = 0.0;
varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec2 step_uv = TEXTURE_PIXEL_SIZE * blur_pixels;
	vec4 color = texture(TEXTURE, UV) * 4.0;
	color += texture(TEXTURE, UV + vec2(step_uv.x, 0.0)) * 2.0;
	color += texture(TEXTURE, UV - vec2(step_uv.x, 0.0)) * 2.0;
	color += texture(TEXTURE, UV + vec2(0.0, step_uv.y)) * 2.0;
	color += texture(TEXTURE, UV - vec2(0.0, step_uv.y)) * 2.0;
	color += texture(TEXTURE, UV + step_uv);
	color += texture(TEXTURE, UV - step_uv);
	color += texture(TEXTURE, UV + vec2(step_uv.x, -step_uv.y));
	color += texture(TEXTURE, UV + vec2(-step_uv.x, step_uv.y));
	COLOR = color * (1.0 / 16.0) * vertex_color;
}
