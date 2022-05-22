using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

[ExecuteInEditMode]
public class MTest : MonoBehaviour
{

	public int resolution = 3;
	public int endCapResolution = 2;
	public float radius = 0.5f;

	public Transform test;

	void Update()
	{

		MeshFilter filter = GetComponent<MeshFilter>();
		Mesh mesh = filter.sharedMesh;
		mesh.Clear();

		var meshData = CreateLineSegmentMesh(Vector3.zero, test.position, resolution, endCapResolution, radius);

		MeshHelper.CreateMesh(ref mesh, meshData, true);


	}

	public static SimpleMeshData CreateLineSegmentMesh(Vector3 pointA, Vector3 pointB, int resolution, int endCapResolution, float radius)
	{
		int numPointsPerCircle = 3 + Mathf.Max(0, resolution);
		endCapResolution = Mathf.Max(1, endCapResolution);

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();

		Vector3 dir = (pointB - pointA).normalized;
		(Vector3 axisA, Vector3 axisB) = Seb.Maths.CreateOrthonormalVectors(dir);

		Vector3[] centres = new Vector3[2 + endCapResolution];
		centres[0] = pointA;
		centres[1] = pointB;

		float[] radii = new float[centres.Length];
		radii[0] = radius;
		radii[1] = radius;

		for (int i = 0; i < endCapResolution; i++)
		{
			float t = (i + 1f) / (endCapResolution);
			float dst = Mathf.Sin(t * Mathf.PI / 2) * radius;
			Vector3 p = pointB + dir * dst;
			centres[i + 2] = p;
			radii[i + 2] = radius * Mathf.Cos(t * Mathf.PI / 2);
		}

		for (int circleIndex = 0; circleIndex < centres.Length - 1; circleIndex++)
		{
			for (int i = 0; i < numPointsPerCircle; i++)
			{
				var angle = ((float)i / numPointsPerCircle) * (Mathf.PI * 2.0f);

				var x = Mathf.Sin(angle) * radii[circleIndex];
				var y = Mathf.Cos(angle) * radii[circleIndex];

				var point = (axisB * x) + (axisA * y) + centres[circleIndex];
				verts.Add(point);

				// Adding the triangles
				if (circleIndex < centres.Length - 2)
				{
					int startIndex = numPointsPerCircle * circleIndex;
					tris.Add(startIndex + i);
					tris.Add(startIndex + (i + 1) % numPointsPerCircle);
					tris.Add(startIndex + i + numPointsPerCircle);

					tris.Add(startIndex + (i + 1) % numPointsPerCircle);
					tris.Add(startIndex + (i + 1) % numPointsPerCircle + numPointsPerCircle);
					tris.Add(startIndex + i + numPointsPerCircle);
				}
			}
		}

		verts.Add(centres[centres.Length - 1]); // Apex

		for (int i = 0; i < numPointsPerCircle; i++)
		{
			int startIndex = numPointsPerCircle * (centres.Length - 2);
			tris.Add(startIndex + i);
			tris.Add(startIndex + (i + 1) % numPointsPerCircle);
			tris.Add(verts.Count - 1);
		}

		SimpleMeshData meshData = new SimpleMeshData(verts.ToArray(), tris.ToArray());
		return meshData;
	}
}
