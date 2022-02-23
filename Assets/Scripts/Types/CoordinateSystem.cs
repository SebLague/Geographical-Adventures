using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public static class CoordinateSystem
{


	public static Coordinate PointToCoordinate(Vector3 pointOnUnitSphere)
	{
		float latitude = Mathf.Asin(pointOnUnitSphere.y);
		float a = pointOnUnitSphere.x;
		float b = -pointOnUnitSphere.z;

		float longitude = Mathf.Atan2(a, b);
		return new Coordinate(longitude, latitude);
	}

	// Calculate point on sphere given longitude and latitude (in radians), and the radius of the sphere
	public static Vector3 CoordinateToPoint(Coordinate coordinate, float radius)
	{
		float y = Mathf.Sin(coordinate.latitude);
		float r = Mathf.Cos(coordinate.latitude); // radius of 2d circle cut through sphere at 'y'
		float x = Mathf.Sin(coordinate.longitude) * r;
		float z = -Mathf.Cos(coordinate.longitude) * r;

		return new Vector3(x, y, z) * radius;
	}

	public static float DistanceBetweenPointsOnSphere(Vector3 a, Vector3 b, float radius)
	{
		return radius * DistanceBetweenPointsOnUnitSphere(a / radius, b / radius);
	}

	public static float DistanceBetweenPointsOnUnitSphere(Vector3 a, Vector3 b)
	{
		return Mathf.Acos(Vector3.Dot(a, b));
	}

}

[System.Serializable]
public struct Coordinate
{
	// Longitude/latitude in radians
	[Range(-Mathf.PI, Mathf.PI)]
	public float longitude;
	[Range(-Mathf.PI / 2, Mathf.PI / 2)]
	public float latitude;

	public Coordinate(float longitude, float latitude)
	{
		this.longitude = longitude;
		this.latitude = latitude;
	}

	public Vector2 ToVector2()
	{
		return new Vector2(longitude, latitude);
	}

	public Vector2 ToUV()
	{
		return new Vector2((longitude + PI) / (2 * PI), (latitude + PI / 2) / PI);
	}
}
