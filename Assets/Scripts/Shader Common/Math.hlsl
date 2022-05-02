#ifndef MATH_INCLUDED
#define MATH_INCLUDED

#include "Assets/Scripts/Shader Common/MathInternal.hlsl"

// Remap a value from the range [minOld, maxOld] to [minNew, maxNew]
float remap(float minOld, float maxOld, float minNew, float maxNew, float val) {
	return saturate(minNew + (val - minOld) * (maxNew - minNew) / (maxOld - minOld));
}

// Remap a value from the range [minOld, maxOld] to [0, 1]
float remap01(float minOld, float maxOld, float val) {
	return saturate((val - minOld) / (maxOld - minOld));
}

// Random value between 0 and 1 (prngState is the 'seed' and will be modified by this function)
float randomValue(inout uint prngState) {
	prngState = hash(prngState);
	return scaleToRange01(prngState);
}

// https://stackoverflow.com/a/6178290
float randomGaussian(float mean, float standardDeviation, inout uint prngState)
{
	float theta = 2 * PI * randomValue(prngState);
	float rho = sqrt(-2 * log(randomValue(prngState)));
	float scale = standardDeviation * rho;
	return mean + scale * cos(theta);
}

// math.stackexchange.com/a/1585996
float3 randomPointOnSphere(inout uint prngState)
{
	float x = randomGaussian(0, 1, prngState);
	float y = randomGaussian(0, 1, prngState);
	float z = randomGaussian(0, 1, prngState);
	return normalize(float3(x, y, z));
}

// math.stackexchange.com/a/87238
float3 randomPointInSphere(inout uint prngState)
{
	float x = randomGaussian(0, 1, prngState);
	float y = randomGaussian(0, 1, prngState);
	float z = randomGaussian(0, 1, prngState);
	float3 p = float3(x, y, z);
	float d = randomValue(prngState);
	return normalize(p) * pow(d, 1/3.0);
}

float3 clampMagnitude(float3 vec, float maxLength) {
	float magnitude = length(vec);
	float3 dir = vec / magnitude;
	return dir * min(magnitude, maxLength);
}

float3 getOrthogonal(float3 axis) {
	float3 a = float3(1, 0, 0);
	float3 b = float3(0, 1, 0);

	float alignA = abs(dot(axis, a));
	float alignB = abs(dot(axis, b));
	float3 leastAligned = (alignA < alignB) ? a : b;
	return normalize(cross(axis, leastAligned));
}

float calculateMipLevel(float2 texCoord, int2 texSize) {
	float2 dx = ddx(texCoord);
	float2 dy = ddy(texCoord);
	
	float mipMapWeight = 1;
	dx *= texSize * mipMapWeight;
	dy *= texSize * mipMapWeight;

	// Thanks to https://community.khronos.org/t/mipmap-level-calculation-using-dfdx-dfdy/67480/2
	float maxSqrLength = max(dot(dx, dx), dot(dy, dy));
	float mipLevel = 0.5 * log2(maxSqrLength); // 0.5 * log2(x^2) == log2(x)
	return mipLevel;
}



#endif