shader_type canvas_item;

uniform vec4 key_color : source_color = vec4(0.0, 1.0, 0.0, 1.0);
uniform float threshold : hint_range(0.0, 1.0) = 0.35;
uniform float softness : hint_range(0.0, 0.5) = 0.08;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    float distance_to_key = distance(tex.rgb, key_color.rgb);
    float alpha = smoothstep(threshold, threshold + softness, distance_to_key) * tex.a;
    COLOR = vec4(tex.rgb, alpha);
}
