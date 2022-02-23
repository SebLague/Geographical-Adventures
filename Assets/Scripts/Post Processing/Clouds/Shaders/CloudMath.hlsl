#ifndef CLOUD_MATH_INCLUDED
#define CLOUD_MATH_INCLUDED

//#include "Assets/Scripts/Shader Common/MathInternal.hlsl"//

// Returns vector (dstToSphere, dstThroughSphere)
// If ray origin is inside sphere, dstToSphere = 0
// If ray misses sphere, dstToSphere = infinity; dstThroughSphere = 0
float2 raySphere(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir) {
	float3 offset = rayOrigin - sphereCentre;
	float a = 1; // Set to dot(rayDir, rayDir) if rayDir might not be normalized
	float b = 2 * dot(offset, rayDir);
	float c = dot (offset, offset) - sphereRadius * sphereRadius;
	float d = b * b - 4 * a * c; // Discriminant from quadratic formula

	// Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
	if (d > 0) {
		float s = sqrt(d);
		float dstToSphereNear = max(0, (-b - s) / (2 * a));
		float dstToSphereFar = (-b + s) / (2 * a);

		// Ignore intersections that occur behind the ray
		if (dstToSphereFar >= 0) {
			return float2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
		}
	}
	// Ray did not intersect sphere
	return float2(1.#INF, 0);
}

float2 rayShellSingle(float3 rayOrigin, float3 rayDir, float innerRadius, float outerRadius, float sceneDepth)
{
	// Distances to, and through, the inner and outer spheres that define the shell where clouds can appear
	float2 innerSphereHitInfo = raySphere(0, innerRadius, rayOrigin, rayDir);
	float dstToInnerSphere = innerSphereHitInfo.x;
	float dstThroughInnerSphere = innerSphereHitInfo.y;

	float2 outerSphereHitInfo = raySphere(0, outerRadius, rayOrigin, rayDir);
	float dstToOuterSphere = outerSphereHitInfo.x;
	float dstThroughOuterSphere = outerSphereHitInfo.y;

	// Outputs:
	float dstToEntry = 1.#INF;
	float pathLength = 0;

	float dstFromCentre = length(rayOrigin);

	// Inside inner sphere (i.e. below the cloud shell)
	if (dstFromCentre < innerRadius)
	{
		dstToEntry = dstThroughInnerSphere;
		pathLength = dstThroughOuterSphere - dstThroughInnerSphere;
	}
	// Outside the inner sphere, but inside the outer sphere (i.e. inside the cloud shell)
	else if (dstFromCentre < outerRadius)
	{
		dstToEntry = 0;
		pathLength = min(dstThroughOuterSphere, dstToInnerSphere);
	}
	// Outside of both spheres (i.e. above the cloud shell)
	else
	{
		dstToEntry = dstToOuterSphere;
		pathLength = min(dstThroughOuterSphere, dstToInnerSphere - dstToOuterSphere);
	}

	pathLength = min(pathLength, sceneDepth - dstToEntry);
	return float2(dstToEntry, pathLength);
}

// Returns vector: dstToEntryA, dstThroughShellA, dstToEntryB, dstThroughShellB
float4 rayShell(float3 rayOrigin, float3 rayDir, float innerRadius, float outerRadius, float sceneDepth)
{
	float2 a = rayShellSingle(rayOrigin, rayDir, innerRadius, outerRadius, sceneDepth);
	const float bias = 0.01f;
	float nextRayDst = a.x + a.y + bias;
	float2 b = rayShellSingle(rayOrigin + rayDir * nextRayDst, rayDir, innerRadius, outerRadius, sceneDepth - nextRayDst);
	return float4(a.x, a.y, a.x + a.y + b.x, b.y);
}

// Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 invRaydir) {
	// Adapted from: http://jcgt.org/published/0007/03/04/
	float3 t0 = (boundsMin - rayOrigin) * invRaydir;
	float3 t1 = (boundsMax - rayOrigin) * invRaydir;
	float3 tmin = min(t0, t1);
	float3 tmax = max(t0, t1);
	
	float dstA = max(max(tmin.x, tmin.y), tmin.z);
	float dstB = min(tmax.x, min(tmax.y, tmax.z));

	// CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
	// dstA is dst to nearest intersection, dstB dst to far intersection

	// CASE 2: ray intersects box from inside (dstA < 0 < dstB)
	// dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

	// CASE 3: ray misses box (dstA > dstB)

	float dstToBox = max(0, dstA);
	float dstInsideBox = max(0, dstB - dstToBox);
	return float2(dstToBox, dstInsideBox);
}

// Remap a value from the range [minOld, maxOld] to [0, 1]
float remap01(float minOld, float maxOld, float val) {
	return saturate((val - minOld) / (maxOld - minOld));
}

#endif