shader_type canvas_item;
render_mode unshaded;
uniform vec4 tint : source_color = vec4(1.0,0.8705882,1.0,1.0);
uniform bool use_red_as_alpha = true;
uniform bool inside_glass = false;
uniform sampler2D glass_mask : filter_linear, repeat_disable;
uniform vec2 mask_center = vec2(0);
uniform vec2 mask_size = vec2(1);
varying vec2 screen_position;
void vertex() {
    screen_position = (MODEL_MATRIX * vec4(VERTEX,0.0,1.0)).xy;
}
void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    float alpha = use_red_as_alpha ? tex.r : tex.a;
    if (inside_glass) {
        vec2 uv = (screen_position-mask_center)/mask_size+0.5;
        float valid = step(0.0, uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
        alpha *= texture(glass_mask, clamp(uv,vec2(0),vec2(1))).a*valid;
    }
    COLOR = vec4(tex.rgb*tint.rgb, alpha);
}
