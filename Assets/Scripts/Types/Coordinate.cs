using UnityEngine;
using static UnityEngine.Mathf;

// Structure for storing latitude and longitude (in radians)
[System.Serializable]
public struct Coordinate
{
	[Range(-PI, PI)]
	public float longitude;
	[Range(-PI / 2, PI / 2)]
	public float latitude;

	public Coordinate(float longitude, float latitude)
	{
		this.longitude = longitude;
		this.latitude = latitude;
	}

	// Return vector2 containing long/lat remapped to range [0,1]
	public Vector2 ToUV()
	{
		return new Vector2((longitude + PI) / (2 * PI), (latitude + PI / 2) / PI);
	}

	public Vector2 ToVector2()
	{
		return new Vector2(longitude, latitude);
	}

	public static Coordinate FromVector2(Vector2 vec2D)
	{
		return new Coordinate(vec2D.x, vec2D.y);
	}

	public CoordinateDegrees ConvertToDegrees()
	{
		return new CoordinateDegrees(longitude * Rad2Deg, latitude * Rad2Deg);
	}

	public override string ToString()
	{
		return $"Coordinate (radians): (longitude = {longitude}, latitude = {latitude})";
	}
}

// Structure for storing latitude and longitude (in degrees)
[System.Serializable]
public struct CoordinateDegrees
{
	[Range(-180, 180)]
	public float longitude;
	[Range(-90, 90)]
	public float latitude;

	public CoordinateDegrees(float longitude, float latitude)
	{
		this.longitude = longitude;
		this.latitude = latitude;
	}

	public Coordinate ConvertToRadians()
	{
		return new Coordinate(longitude * Deg2Rad, latitude * Deg2Rad);
	}

	public override string ToString()
	{
		return $"Coordinate (degrees): (longitude = {longitude}, latitude = {latitude})";
	}
}

