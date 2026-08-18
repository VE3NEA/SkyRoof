#version 330 core

#define Pi 3.1415926538
#define TwoPi (2 * Pi)
#define HalfPi (Pi / 2)

uniform float in_zoom;
uniform float in_homeLat;
uniform float in_homeLon;
uniform float in_footprint;
uniform float in_brightness;

uniform sampler2D worldMapTexture;

in vec2 pass_RoPhi;
out vec4 out_Color;

void main(void) 
{
	float ro =  pass_RoPhi[0] * Pi;
  float phi = pass_RoPhi[1];

	// lat
	float a = sin(in_homeLat) * cos(ro) + cos(in_homeLat) * sin(ro) * sin(phi);
	float lat = asin(a);

	// lon
	float num = cos(phi) * sin(ro) * cos(in_homeLat);
	float den = cos(ro) - sin(in_homeLat) * a;
	float lon = in_homeLon + atan(num, den);

	// if lat = +/- 90
	if (HalfPi - in_homeLat < 0.001) lon = in_homeLon + HalfPi + phi;
	else if (HalfPi + in_homeLat < 0.001) lon = in_homeLon + HalfPi -phi;
	

	// lat, lon -> s, t
	float t = 0.5 - lat / Pi;
	float s = 0.5 + lon / TwoPi;
  out_Color = texture(worldMapTexture, vec2(s, t));

	// the map bitmap is a light image: dim it without touching alpha
  out_Color.rgb *= in_brightness;

	// shadow
  //if (ro > Pi / in_zoom) out_Color *= 0; else
  if (ro > in_footprint) out_Color *= 0.8;
}