using UnityEngine;

public class Bounds2D
{

	Vector2 min;
	Vector2 max;

	public Bounds2D()
	{
		min = Vector2.one * float.MaxValue;
		max = Vector2.one * float.MinValue;
	}

	public Bounds2D(Vector2[] points)
	{
		min = Vector2.one * float.MaxValue;
		max = Vector2.one * float.MinValue;
		GrowToInclude(points);
	}

	public void GrowToInclude(Vector2[] points)
	{
		for (int i = 0; i < points.Length; i++)
		{
			GrowToInclude(points[i]);
		}
	}

	public void GrowToInclude(Vector2 point)
	{
		min = Vector2.Min(min, point);
		max = Vector2.Max(max, point);
	}

	public void GrowToInclude(Bounds2D other)
	{
		min = Vector2.Min(min, other.min);
		max = Vector2.Max(max, other.max);
	}

	public Vector2 Min
	{
		get
		{
			return min;
		}
	}

	public Vector2 Max
	{
		get
		{
			return max;
		}
	}

	public Vector2 Centre
	{
		get
		{
			return (min + max) / 2;
		}
	}

	public Vector2 Size
	{
		get
		{
			return max - min;
		}
	}

	public Vector2 HalfSize
	{
		get
		{
			return Size / 2;
		}
	}

	public float Area
	{
		get
		{
			return Size.x * Size.y;
		}
	}

	public bool Contains(Vector2 point)
	{
		Vector2 halfSize = HalfSize;
		Vector2 centre = Centre;

		float ox = Mathf.Abs(centre.x - point.x);
		float oy = Mathf.Abs(centre.y - point.y);

		return ox < halfSize.x && oy < halfSize.y;
	}
}