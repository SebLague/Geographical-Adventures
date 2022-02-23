#ifndef TRIPLANAR_INCLUDED
#define TRIPLANAR_INCLUDED

float4 triplanar(float3 vertPos, float3 normal, float3 scale, sampler2D tex, float2 offset = 0) {
	float3 scaledPos = vertPos / scale;
	float4 colX = tex2D (tex, scaledPos.zy + offset);
	float4 colY = tex2D(tex, scaledPos.xz + offset);
	float4 colZ = tex2D (tex,scaledPos.xy + offset);
	
	// Square normal to make all values positive + increase blend sharpness
	float3 blendWeight = normal * normal;
	// Divide blend weight by the sum of its components. This will make x + y + z = 1
	blendWeight /= dot(blendWeight, 1);
	return colX * blendWeight.x + colY * blendWeight.y + colZ * blendWeight.z;
}

// Reoriented Normal Mapping
// http://blog.selfshadow.com/publications/blending-in-detail/
// Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
float3 blend_rnm(float3 n1, float3 n2)
{
	n1.z += 1;
	n2.xy = -n2.xy;

	return n1 * dot(n1, n2) / n1.z - n2;
}

float3 unpackScaleNormal (float4 packednormal, float normalScale) {
	float3 normal;
	normal.xy = (packednormal.wy * 2 - 1);
	normal.xy *= normalScale;
	normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
	return normal;
}

// Sample normal map with triplanar coordinates
// Returned normal will be in obj/world space (depending whether pos/normal are given in obj or world space)
// Based on: medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
float3 triplanarNormal(sampler2D normalMap, float3 pos, float3 normal, float3 scale, float2 offset, float strength = 1) {
	float3 absNormal = abs(normal);

	// Calculate triplanar blend
	float3 blendWeight = saturate(pow(normal, 4));
	// Divide blend weight by the sum of its components. This will make x + y + z = 1
	blendWeight /= dot(blendWeight, 1);

	// Calculate triplanar coordinates
	float2 uvX = pos.zy * scale + offset;
	float2 uvY = pos.xz * scale + offset;
	float2 uvZ = pos.xy * scale + offset;

	// Sample tangent space normal maps
	// UnpackNormal puts values in range [-1, 1]
	float3 tangentNormalX = unpackScaleNormal(tex2D(normalMap, uvX), strength);
	float3 tangentNormalY = unpackScaleNormal(tex2D(normalMap, uvY), strength);
	float3 tangentNormalZ = unpackScaleNormal(tex2D(normalMap, uvZ), strength);

	// Swizzle normals to match the tangent space of the projection planes
	// and apply reoriented normal mapping blend
	tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
	tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
	tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

	// Apply input normal sign to tangent space Z
	float3 axisSign = sign(normal);
	tangentNormalX.z *= axisSign.x;
	tangentNormalY.z *= axisSign.y;
	tangentNormalZ.z *= axisSign.z;

	// Swizzle tangent normals to match input normal and blend together.
	// For example, if blendWeight.x == 1, that means we're projecting purely along the x axis,
	// in which case the up direction is along the x axis. Since z is up in tangent space,
	// we convert to object/world space by swizzling it to zyx.
	float3 outputNormal = normalize(
		tangentNormalX.zyx * blendWeight.x +
		tangentNormalY.xzy * blendWeight.y +
		tangentNormalZ.xyz * blendWeight.z
	);

	return outputNormal;
}

// Sample normal map with triplanar coordinates
// Returned normal will be in obj/world space (depending whether pos/normal are given in obj or world space)
// Based on: medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
float3 triplanarNormal(sampler2D normalMap, float3 pos, float3 normal, float3 scale, float2 offset, float strength, out float3 tangentNormal) {
	float3 absNormal = abs(normal);

	// Calculate triplanar blend
	float3 blendWeight = saturate(pow(normal, 4));
	// Divide blend weight by the sum of its components. This will make x + y + z = 1
	blendWeight /= dot(blendWeight, 1);

	// Calculate triplanar coordinates
	float2 uvX = pos.zy * scale + offset;
	float2 uvY = pos.xz * scale + offset;
	float2 uvZ = pos.xy * scale + offset;

	// Sample tangent space normal maps
	// UnpackNormal puts values in range [-1, 1]
	float3 tangentNormalX = unpackScaleNormal(tex2D(normalMap, uvX), strength);
	float3 tangentNormalY = unpackScaleNormal(tex2D(normalMap, uvY), strength);
	float3 tangentNormalZ = unpackScaleNormal(tex2D(normalMap, uvZ), strength);

	tangentNormal = tangentNormalX * blendWeight.x + tangentNormalY * blendWeight.y + tangentNormalZ * blendWeight.z;

	// Swizzle normals to match the tangent space of the projection planes
	// and apply reoriented normal mapping blend
	tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
	tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
	tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

	// Apply input normal sign to tangent space Z
	float3 axisSign = sign(normal);
	tangentNormalX.z *= axisSign.x;
	tangentNormalY.z *= axisSign.y;
	tangentNormalZ.z *= axisSign.z;

	// Swizzle tangent normals to match input normal and blend together.
	// For example, if blendWeight.x == 1, that means we're projecting purely along the x axis,
	// in which case the up direction is along the x axis. Since z is up in tangent space,
	// we convert to object/world space by swizzling it to zyx.
	float3 outputNormal = normalize(
		tangentNormalX.zyx * blendWeight.x +
		tangentNormalY.xzy * blendWeight.y +
		tangentNormalZ.xyz * blendWeight.z
	);

	return outputNormal;
}

#endif