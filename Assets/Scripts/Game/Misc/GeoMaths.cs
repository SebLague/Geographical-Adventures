using UnityEngine;

public static class GeoMaths
{

	public const float EarthRadiusKM = 6371;
	public const float EarthCircumferenceKM = EarthRadiusKM * Mathf.PI * 2;

	public static Coordinate PointToCoordinate(Vector3 pointOnUnitSphere)
	{
		float latitude = Mathf.Asin(pointOnUnitSphere.y);
		float a = pointOnUnitSphere.x;
		float b = -pointOnUnitSphere.z;

		float longitude = Mathf.Atan2(a, b);
		return new Coordinate(longitude, latitude);
	}

	// Calculate point on sphere given longitude and latitude (in radians), and the radius of the sphere
	public static Vector3 CoordinateToPoint(Coordinate coordinate, float radius = 1)
	{
		float y = Mathf.Sin(coordinate.latitude);
		float r = Mathf.Cos(coordinate.latitude); // radius of 2d circle cut through sphere at 'y'
		float x = Mathf.Sin(coordinate.longitude) * r;
		float z = -Mathf.Cos(coordinate.longitude) * r;

		return new Vector3(x, y, z) * radius;
	}

	public static float DistanceBetweenPointsOnUnitSphere(Vector3 a, Vector3 b)
	{
		// Thanks to https://www.movable-type.co.uk/scripts/latlong-vectors.html
		return Mathf.Atan2(Vector3.Cross(a, b).magnitude, Vector3.Dot(a, b));
		// This simpler approach works as well, but is less precise for small
		//return Mathf.Acos(Vector3.Dot(a, b));
	}

}