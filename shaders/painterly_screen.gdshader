shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;
uniform float color_steps : hint_range(8.0, 64.0) = 30.0;
uniform float grain_strength : hint_range(0.0, 0.12) = 0.027;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.42;


float random_noise(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}


void fragment() {
	vec2 uv = SCREEN_UV;
	vec3 color = texture(screen_texture, uv).rgb;
	color = floor(color * color_steps + 0.5) / color_steps;

	float luminance = dot(color, vec3(0.299, 0.587, 0.114));
	color += vec3(0.025, 0.010, -0.012) * smoothstep(0.45, 1.0, luminance);
	color += vec3(-0.012, 0.002, 0.022) * (1.0 - smoothstep(0.0, 0.55, luminance));

	float grain = random_noise(FRAGCOORD.xy) - 0.5;
	color += grain * grain_strength;

	vec2 centered = uv * 2.0 - 1.0;
	float vignette = smoothstep(0.42, 1.38, dot(centered, centered));
	color *= 1.0 - vignette * vignette_strength;

	COLOR = vec4(color, 1.0);
}
