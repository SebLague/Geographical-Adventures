#ifndef GEOMATH_INCLUDED
#define GEOMATH_INCLUDED

#include "Assets/Scripts/Shader Common/Math.hlsl"

// Get point on sphere from long/lat (given in radians)
float3 longitudeLatitudeToPoint(float2 longLat) {
	float longitude = longLat[0];
	float latitude = longLat[1];

	float y = sin(latitude);
	float r = cos(latitude); // radius of 2d circle cut through sphere at 'y'
	float x = sin(longitude) * r;
	float z = -cos(longitude) * r;
	return float3(x,y,z);
}

// Scale longitude and latitude (given in radians) to range [0, 1]
float2 longitudeLatitudeToUV(float2 longLat) {
	float longitude = longLat[0]; // range [-PI, PI]
	float latitude = longLat[1]; // range [-PI/2, PI/2]
	
	float u = (longitude / PI + 1) * 0.5;
	float v = latitude / PI + 0.5;
	return float2(u,v);
}

float2 uvToLongitudeLatitude(float2 uv) {
	float longitude = (uv.x - 0.5) * 2 * PI;
	float latitude = (uv.y - 0.5) * PI;
	return float2(longitude, latitude);
}

// Convert point on unit sphere to longitude and latitude
float2 pointToLongitudeLatitude(float3 p) {
	float longitude = atan2(p.x, -p.z);
	float latitude = asin(p.y);
	return float2(longitude, latitude);
}

// Convert point on unit sphere to uv texCoord
float2 pointToUV(float3 p) {
	float2 longLat = pointToLongitudeLatitude(p);
	return longitudeLatitudeToUV(longLat);
}

float3 uvToPointOnSphere(float2 uv) {
	return longitudeLatitudeToPoint(uvToLongitudeLatitude(uv));
}

float distanceBetweenPointsOnUnitSphere(float3 a, float3 b)
{
	return acos(saturate(dot(a, b)));
}

// Wrap index around equirectangular (plate carrée) projection
uint2 WrapIndex(int2 index, uint2 size) {
	// TODO: test if this wrapping is working properly
	// Wrap y index (latitude)
	if (index.y >= (int)size.y) {
		index.y = size.y - (index.y - size.y) - 1;
		index.x = (index.x + size.x / 2) % size.x;
	}
	else if (index.y < 0) {
		index.y = abs(index.y);
		index.x = (index.x + size.x / 2) % size.x;
	}
	// Wrap x index (longitude)
	index.x = (index.x + size.x) % size.x;
	return index;
}


#endif