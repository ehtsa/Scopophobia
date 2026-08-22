shader_type spatial;
render_mode unshaded, cull_back;

// The developed snapshot, set from C# via ShaderMaterial.SetShaderParameter.
uniform sampler2D photo_texture : source_color;

// 0 = just ejected (blank), 1 = fully developed.
// Driven from Polaroid.DevelopProgress every frame while it's changing.
uniform float develop_progress : hint_range(0.0, 1.0) = 0.0;

// Fraction of the quad, measured from the top, that the photo occupies.
// 0.8 = top 80%, leaving the bottom 20% as the white polaroid border.
uniform float photo_area_height : hint_range(0.0, 1.0) = 0.8;

uniform vec4 border_color : source_color = vec4(0.94, 0.93, 0.89, 1.0);
uniform vec4 undeveloped_color : source_color = vec4(0.82, 0.80, 0.76, 1.0);

void fragment() {
	// Assumes UV.y == 0 at the top of the quad. If the photo shows up in
	// the BOTTOM of the frame instead of the top when you test this,
	// your mesh's V is flipped — change the line below to:
	//   if (1.0 - UV.y < photo_area_height) { ... UV.y / photo_area_height ...
	// and remap accordingly.
	if (UV.y < photo_area_height) {
		vec2 photo_uv = vec2(UV.x, UV.y / photo_area_height);
		vec3 photo_color = texture(photo_texture, photo_uv).rgb;
		ALBEDO = mix(undeveloped_color.rgb, photo_color, develop_progress);
	} else {
		ALBEDO = border_color.rgb;
	}
}
