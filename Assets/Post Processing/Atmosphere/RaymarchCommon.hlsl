float3 topLeftDir;
float3 topRightDir;
float3 bottomLeftDir;
float3 bottomRightDir;

float3 camPos;

float3 calculateViewDir(float2 texCoord) {
	float3 a = lerp(topLeftDir, topRightDir, texCoord.x);
	float3 b = lerp(bottomLeftDir, bottomRightDir, texCoord.x);
	float3 viewVector = lerp(a, b, texCoord.y);
	return normalize(viewVector);
}