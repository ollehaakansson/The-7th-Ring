shader_type canvas_item;

uniform float flash_strength : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 sprite = COLOR;
    sprite.rgb = mix(sprite.rgb, vec3(1.0, 0.85, 0.55), flash_strength);
    COLOR = sprite;
}
