shader_type canvas_item;
render_mode unshaded;
#include "res://Manosaba/scenes/vfx/refute_cutin/original_noise.gdshaderinc"
uniform float threshold = 0.3;
uniform float animation_time = 0.0;
uniform vec3 background_a : source_color = vec3(1.0,0.0,0.6876597);
uniform vec3 background_b : source_color = vec3(0);
void fragment() {
    vec2 uv = vec2(UV.x, 1.0-UV.y);
    float distortion = original_noise(uv-animation_time*0.1, 10.0)
                     + original_noise(uv+animation_time*0.1, 10.0)
                     + original_noise(uv, 30.0);
    float noise_alpha = original_noise(uv+vec2(distortion), 30.0);
    if (noise_alpha < threshold) discard;
    COLOR = vec4(mix(background_a, background_b, distortion*noise_alpha), noise_alpha);
}
