using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;


public static class DebugExtra
{

	static SimpleMeshData sphere;

	static DebugExtra()
	{
		sphere = IcoSphere.Generate(1);
	}

	public static void DrawPath(Vector2[] points, bool closed, Color color, float duration)
	{
		for (int i = 0; i < points.Length - 1; i++)
		{
			Debug.DrawLine(points[i], points[i + 1], color, duration);
		}
		if (closed)
		{

			Debug.DrawLine(points[0], points[points.Length - 1], color, duration);
		}
	}

	public static void DrawSphere(Vector3 centre, float radius, Color color, float duration = 0)
	{
		for (int i = 0; i < sphere.triangles.Length; i += 3)
		{
			Vector3 a = centre + sphere.vertices[sphere.triangles[i]] * radius;
			Vector3 b = centre + sphere.vertices[sphere.triangles[i + 1]] * radius;
			Vector3 c = centre + sphere.vertices[sphere.triangles[i + 2]] * radius;
			Debug.DrawLine(a, b, color, duration);
			Debug.DrawLine(b, c, color, duration);
			Debug.DrawLine(c, a, color, duration);
		}
	}

	public static void DrawBox(Vector3 centre, Vector3 halfSize, Color col, float duration = 0)
	{
		Vector3 frontBottomLeft = new Vector3(centre.x - halfSize.x, centre.y - halfSize.y, centre.z - halfSize.z);
		Vector3 frontTopLeft = new Vector3(centre.x - halfSize.x, centre.y + halfSize.y, centre.z - halfSize.z);
		Vector3 frontTopRight = new Vector3(centre.x + halfSize.x, centre.y + halfSize.y, centre.z - halfSize.z);
		Vector3 frontBottomRight = new Vector3(centre.x + halfSize.x, centre.y - halfSize.y, centre.z - halfSize.z);

		Vector3 backBottomLeft = new Vector3(centre.x - halfSize.x, centre.y - halfSize.y, centre.z + halfSize.z);
		Vector3 backTopLeft = new Vector3(centre.x - halfSize.x, centre.y + halfSize.y, centre.z + halfSize.z);
		Vector3 backTopRight = new Vector3(centre.x + halfSize.x, centre.y + halfSize.y, centre.z + halfSize.z);
		Vector3 backBottomRight = new Vector3(centre.x + halfSize.x, centre.y - halfSize.y, centre.z + halfSize.z);

		// Draw front
		Debug.DrawLine(frontBottomLeft, frontTopLeft, col, duration);
		Debug.DrawLine(frontTopLeft, frontTopRight, col, duration);
		Debug.DrawLine(frontTopRight, frontBottomRight, col, duration);
		Debug.DrawLine(frontBottomRight, frontBottomLeft, col, duration);

		// Draw back
		Debug.DrawLine(backBottomLeft, backTopLeft, col, duration);
		Debug.DrawLine(backTopLeft, backTopRight, col, duration);
		Debug.DrawLine(backTopRight, backBottomRight, col, duration);
		Debug.DrawLine(backBottomRight, backBottomLeft, col, duration);

		// Draw connecting edges
		Debug.DrawLine(frontBottomLeft, backBottomLeft, col, duration);
		Debug.DrawLine(frontTopLeft, backTopLeft, col, duration);
		Debug.DrawLine(frontTopRight, backTopRight, col, duration);
		Debug.DrawLine(frontBottomRight, backBottomRight, col, duration);

	}
}
