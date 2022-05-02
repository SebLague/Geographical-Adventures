#include "Assets/Scripts/Shader Common/GeoMath.hlsl"
#include "Assets/Scripts/Shader Common/SimplexNoise.hlsl"

sampler2D _WaveDstMap;
float WaveNoiseScale;
float WaveNoiseSpeed;
float WaveNoiseAmplitude;
float WaveNoiseStretch;
float WaveShoreFalloff;

float clmp(float x, float m) {
	return max(m, abs(x)) * ((x>=0)?1:-1);
}

float4 waveCalc(float3 worldPos, inout float3 normal) {
	//return float4(worldPos,0);
	float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos);
	float d = dot(viewDir, normal);
	//d = smoothstep(-0.1,0,d);
	//return worldPos;
	float3 spherePos = normalize(worldPos);
	float2 texCoord = pointToUV(spherePos);
	float shoreDst = tex2Dlod(_WaveDstMap, float4(texCoord.xy, 0, 0));

	// Vertex wave anim
	float waveWeight = smoothstep(0.01, WaveShoreFalloff * 0.01 + 0.01, shoreDst);
	//waveWeight *= d;
	if (waveWeight > 0) {
		float startAngle_xz = atan2(spherePos.z, clmp(spherePos.x,0.001));
		float angle_xz = _Time.y * WaveNoiseSpeed * 0.01 + startAngle_xz;
		float3 pos_xz = float3(cos(angle_xz), spherePos.y, sin(angle_xz));
	
		float startAngle_xy = atan2(spherePos.y, clmp(spherePos.x,0.001));
		float angle_xy = _Time.y * WaveNoiseSpeed * 0.01 + startAngle_xy;
		float3 pos_xy = float3(cos(angle_xy), sin(angle_xy), spherePos.z);

		float w = smoothstep(0.4, 0.8, abs(spherePos.y));
		w = w * w;

		
		float4 noise_xz = SimplexNoiseGrad(pos_xz * WaveNoiseScale / float3(1, WaveNoiseStretch, 1));
		float4 noise_xy = SimplexNoiseGrad(pos_xy * WaveNoiseScale / float3(1, 1, WaveNoiseStretch));
	
		float4 noise = lerp(noise_xz, noise_xy, w);
		float h = noise.w;

		float s = WaveNoiseAmplitude * 0.1 * waveWeight;
		float radius = length(worldPos);

		float3 newPos = spherePos * (radius + h * s);

		// Gradient test: https://math.stackexchange.com/questions/1071662/surface-normal-to-point-on-displaced-sphere
		//h = noise.w * noise.w;
		//float3 grad = 2 * ((noise1.xyz * noise1.w) + (noise2.xyz * noise2.w * 0.5) + (noise3.xyz * noise3.w * 0.25));
		float3 grad = noise.xyz;
		float waveNormalInverseStrength = 20;
		grad = grad / (waveNormalInverseStrength + s * h);
		float3 proj = grad - dot (grad, spherePos) * spherePos;
		float3 n = spherePos - s * proj;
		normal = normalize(n);
		worldPos = newPos;
	}

	return float4(worldPos,d);
}