// Tone mapping
float intensity;
float contrast;
float whitePoint;

// Dithering
float ditherStrength;
sampler2D BlueNoise;
float4 BlueNoise_TexelSize;


// https://64.github.io/tonemapping/
float3 reinhard_extended(float3 v, float max_white)
{
	float3 numerator = v * (1.0f + (v / (max_white * max_white)));
	return numerator / (1.0f + v);
}

float3 smoothMax (float3 a, float3 b, float k) {
	k = -abs (k);
	float h = saturate((b - a + k) / (2 * k));
	return a * h + b * (1 - h) - k * h * (1 - h);//
}

float3 toneMap(float3 lum) {
	lum *= intensity;
	lum = lerp(0.5, lum, contrast);
	lum = reinhard_extended(lum, whitePoint);

	lum = smoothMax(lum, -0.05, 0.05);
	
	return lum;
}

float3 toneMapA(float3 lum) {
	lum *= intensity;
	lum = lerp(0.5, lum, contrast);
	return lum;
}

float3 toneMapB(float3 lum) {
	lum = reinhard_extended(lum, whitePoint);
	lum = smoothMax(lum, -0.05, 0.05);
	return lum;
}

// Remap noise to triangular distribution
// See pg. 45 to 57 of www.gdcvault.com/play/1023002/Low-Complexity-High-Fidelity-INSIDE
// Thanks to https://www.shadertoy.com/view/MslGR8 See also https://www.shadertoy.com/view/4t2SDh
float remap_noise_tri_erp(float v)
{
	float r2 = 0.5 * v;
	float f1 = sqrt(r2);
	float f2 = 1.0 - sqrt(r2 - 0.25);
	return (v < 0.5) ? f1 : f2;
}

float3 getBlueNoise(float2 uv) {
	float2 screenSize = _ScreenParams.xy;
	
	uv = (uv * screenSize) * BlueNoise_TexelSize.xy;

	float3 blueNoise = tex2D(BlueNoise, uv).rgb;
	float3 m = 0;
	m.r = remap_noise_tri_erp(blueNoise.r);
	m.g = remap_noise_tri_erp(blueNoise.g);	
	m.b = remap_noise_tri_erp(blueNoise.b);

	float3 weightedNoise = (m * 2.0 - 0.5);
	return weightedNoise;
}

float3 blueNoiseDither(float3 col, float2 uv, float strength) {
	float3 weightedNoise = getBlueNoise(uv) / 255.0 * strength;

	return col + weightedNoise;
}