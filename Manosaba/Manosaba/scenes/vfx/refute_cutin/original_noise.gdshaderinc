// Unity Shader Graph deterministic value-noise hash, recovered from the DXBC.
float original_hash(vec2 p) {
    uint n = uint(int(p.y)) ^ uint(0x41c64e6d);
    n *= n + uint(int(p.x));
    n ^= n >> uint(5);
    n *= uint(0x27d4eb2d);
    return float(n >> uint(8)) / 16777215.0;
}
float original_value_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f*f*(3.0-2.0*f);
    return mix(mix(original_hash(i), original_hash(i+vec2(1,0)), f.x),
               mix(original_hash(i+vec2(0,1)), original_hash(i+vec2(1,1)), f.x), f.y);
}
float original_noise(vec2 uv, float scale) {
    return original_value_noise(uv*scale)*0.125
         + original_value_noise(uv*scale*0.5)*0.25
         + original_value_noise(uv*scale*0.25)*0.5;
}
