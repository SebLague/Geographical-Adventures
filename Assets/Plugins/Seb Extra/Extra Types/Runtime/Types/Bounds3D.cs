using UnityEngine;

public class Bounds3D
{

	Vector3 min;
	Vector3 max;

	public Bounds3D()
	{
		min = Vector3.one * float.MaxValue;
		max = Vector3.one * float.MinValue;
	}

	public Bounds3D(Vector3 centre, Vector3 size)
	{
		min = centre - size;
		max = centre + size;
	}

	public Bounds3D(Vector3[] points)
	{
		min = Vector3.one * float.MaxValue;
		max = Vector3.one * float.MinValue;
		GrowToInclude(points);
	}

	public void GrowToInclude(Vector3[] points)
	{
		for (int i = 0; i < points.Length; i++)
		{
			GrowToInclude(points[i]);
		}
	}

	public void GrowToInclude(Vector3 point)
	{
		min = Vector3.Min(min, point);
		max = Vector3.Max(max, point);
	}

	public void GrowToInclude(Bounds3D other)
	{
		min = Vector3.Min(min, other.min);
		max = Vector3.Max(max, other.max);
	}

	public static Bounds3D Combine(Bounds3D a, Bounds3D b)
	{
		Bounds3D combinedBounds = new Bounds3D();
		combinedBounds.GrowToInclude(a);
		combinedBounds.GrowToInclude(b);
		return combinedBounds;
	}

	public Vector3 Min
	{
		get
		{
			return min;
		}
	}

	public Vector3 Max
	{
		get
		{
			return max;
		}
	}

	public Vector3 Centre
	{
		get
		{
			return (min + max) / 2;
		}
	}

	public Vector3 Size
	{
		get
		{
			return max - min;
		}
	}

	public Vector3 HalfSize
	{
		get
		{
			return Size / 2;
		}
	}

	public float Volume
	{
		get
		{
			return Size.x * Size.y * Size.z;
		}
	}

	public bool Contains(Vector3 point)
	{
		Vector3 halfSize = HalfSize;
		Vector3 centre = Centre;

		float ox = Mathf.Abs(centre.x - point.x);
		float oy = Mathf.Abs(centre.y - point.y);
		float oz = Mathf.Abs(centre.z - point.z);

		return ox < halfSize.x && oy < halfSize.y && oz < halfSize.z;
	}

	public bool Overlaps(Bounds3D other)
	{
		bool overlaps = true;
		overlaps &= IntervalOverlaps(min.x, max.x, other.min.x, other.max.x);
		overlaps &= IntervalOverlaps(min.y, max.y, other.min.y, other.max.y);
		overlaps &= IntervalOverlaps(min.z, max.z, other.min.z, other.max.z);
		return overlaps;

		bool IntervalOverlaps(float minA, float maxA, float minB, float maxB)
		{
			return maxA >= minB && maxB >= minA;
		}

	}
}