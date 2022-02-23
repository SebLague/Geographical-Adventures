#ifndef SHADING_INCLUDED
#define SHADING_INCLUDED

#include "Assets/Scripts/Shader Common/Math.hlsl"

float calculateSpecular(float3 normal, float3 viewDir, float3 dirToSun, float smoothness) {
	float specularAngle = acos(dot(normalize(dirToSun - viewDir), normal));
	float specularExponent = specularAngle / smoothness;
	float specularHighlight = exp(-max(0,specularExponent) * specularExponent);
	return specularHighlight;
}

#endif