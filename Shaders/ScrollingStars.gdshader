shader_type canvas_item;

uniform vec2 Direction = vec2(1.0, 1.0);
uniform float Speed = 0.08;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;


void fragment() {
	// Place fragment code here.
	// vec2 DistortedUV = SCREEN_UV + texture(TEXTURE, UV);
	
	COLOR = texture(TEXTURE, UV + (Direction * TIME * Speed) * vec2(.0002 * abs(cos(TIME/4.0)), .98));
}
