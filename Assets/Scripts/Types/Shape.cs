using UnityEngine;

[System.Serializable]
public struct Shape
{
	public Polygon[] polygons;
}

[System.Serializable]
public struct Polygon
{
	// First path is the outline of the polygon, any subsequent paths are holes to be cut out
	public Path[] paths;

	public Polygon(Path[] paths)
	{
		this.paths = paths;
	}

	public int NumHoles
	{
		get
		{
			return paths.Length - 1;
		}
	}

	public Path Outline
	{
		get
		{
			return paths[0];
		}
	}

	public Path[] Holes
	{
		get
		{
			Path[] holes = new Path[NumHoles];
			for (int i = 0; i < holes.Length; i++)
			{
				holes[i] = paths[i + 1];
			}
			return holes;
		}
	}
}

[System.Serializable]
public struct Path
{
	public Coordinate[] points;

	public Path(Coordinate[] points)
	{
		this.points = points;
	}

	public int NumPoints
	{
		get
		{
			return points.Length;
		}
	}

	// Convert coordinates to Vector2s.
	// Optionally don't include last point (for cases where first and last points have been defined as the same)
	public Vector2[] GetPointsAsVector2(bool includeLastPoint = true)
	{
		int numPoints = (includeLastPoint) ? points.Length : points.Length - 1;
		Vector2[] pointsVec = new Vector2[numPoints];
		for (int i = 0; i < numPoints; i++)
		{
			pointsVec[i] = points[i].ToVector2();
		}
		return pointsVec;
	}

	public static Vector2[] GetPointsAsVector2(Coordinate[] coords)
	{
		Vector2[] pointsVec = new Vector2[coords.Length];
		for (int i = 0; i < pointsVec.Length; i++)
		{
			pointsVec[i] = coords[i].ToVector2();
		}
		return pointsVec;
	}
}