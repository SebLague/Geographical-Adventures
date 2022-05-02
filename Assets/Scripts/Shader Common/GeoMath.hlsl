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


// Test if point p is inside polygon defined by Points buffer
// NOTE: last point in polygon is expected to be a duplicate of the first point
bool pointInPolygon(float2 p, StructuredBuffer<float2> Points, int polygonStartIndex, int numPointsInPolygon) {
	// Thanks to Dan Sunday
	int windingNumber = 0;
	for (int i = polygonStartIndex; i < polygonStartIndex + numPointsInPolygon - 1; i ++) {
		
		float2 a = Points[i];
		float2 b = Points[i + 1];
		bool lineTest = (b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y) > 0;

		if (a.y <= p.y) {
			if (b.y > p.y && lineTest) {
				windingNumber ++;
			}
		}
		else if (b.y <= p.y && !lineTest) {
			windingNumber --;
		}
	}

	return windingNumber != 0;
}


float calculateGeoMipLevel(float2 texCoord, int2 texSize) {
	// * Calculate mip level (doing manually to avoid mipmap seam where texture wraps on x axis -- there's probably a better way?)
	float2 dx, dy;
	if (texCoord.x < 0.75 && texCoord.x > 0.25) {
		dx = ddx(texCoord);
		dy = ddy(texCoord);
	}
	else {
		// Shift texCoord so seam is on other side of world
		dx = ddx((texCoord + float2(0.5, 0)) % 1);
		dy = ddy((texCoord + float2(0.5, 0)) % 1);
	}
	float mipMapWeight = 0.5f;
	dx *= texSize * mipMapWeight;
	dy *= texSize * mipMapWeight;

	// Thanks to https://community.khronos.org/t/mipmap-level-calculation-using-dfdx-dfdy/67480/2
	float maxSqrLength = max(dot(dx, dx), dot(dy, dy));
	float mipLevel = 0.5 * log2(maxSqrLength); // 0.5 * log2(x^2) == log2(x)
	// Clamp mip level to prevent value blowing up at poles
	const int maxMipLevel = 8;
	mipLevel = min(maxMipLevel, mipLevel);
	return mipLevel;
}



#endif