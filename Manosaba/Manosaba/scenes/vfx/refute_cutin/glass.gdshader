shader_type canvas_item;
render_mode unshaded;
#include "res://Manosaba/scenes/vfx/refute_cutin/original_noise.gdshaderinc"
uniform sampler2D kirakira : filter_linear, repeat_disable;
uniform sampler2D kirakira2 : filter_linear, repeat_disable;
uniform float tick = 1.0;
uniform float fader = 0.0;
uniform float fader2 = 0.0;
uniform float animation_time = 0.0;
uniform bool silhouette_only = false;
uniform vec3 glow_tint : source_color = vec3(1.0,0.8705882,1.0);
uniform vec3 edge_tint : source_color = vec3(1.0,0.0,0.9404259);

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec2 unity_uv = vec2(UV.x, 1.0-UV.y);
    float threshold = original_noise(unity_uv, 55.9)*(1.0-unity_uv.x);
    float alive = step(threshold, tick);
    float edge = alive-step(threshold, tick-0.02);
    if (silhouette_only) {
        COLOR = vec4(mix(glow_tint, edge_tint, edge), tex.r*alive);
    } else {
        float wave = sin(unity_uv.x*6.0 + animation_time*3.0);
        vec3 a = texture(kirakira, UV).rgb;
        vec3 b = texture(kirakira2, UV).rgb;
        vec3 tint = vec3(0.992487,1.0,0.851577);
        vec3 color = tex.rgb + (a*(wave*fader + 2.0*fader2)-0.1)*tint
                   + (b*wave*tint-0.1)*fader;
        color = mix(color, edge_tint, edge);
        COLOR = vec4(color, tex.a*alive);
    }
}
