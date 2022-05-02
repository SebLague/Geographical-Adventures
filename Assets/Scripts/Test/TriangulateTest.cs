using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;
using Seb.Helpers;

public class TriangulateTest : MonoBehaviour
{
	public Transform[] points;
	public Transform[] contour;
	public Transform[] hole;
	public Material mat;

	public int testIndex;

	void Update()
	{

		Vector3[] outline = TransformHelper.GetTransformPositions(contour);
		Vector3[] innerPoints = TransformHelper.GetTransformPositions(points);
		Vector3[] holePoints = TransformHelper.GetTransformPositions(hole);

		int[] t = TerrainGeneration.Triangulator.Triangulate(VectorHelper.To2DArray(outline), VectorHelper.To2DArray(innerPoints), VectorHelper.To2DArray(holePoints));
		List<Vector3> verts = new List<Vector3>();
		verts.AddRange(outline);
		verts.AddRange(innerPoints);
		verts.AddRange(holePoints);
		Mesh mesh = MeshHelper.CreateMesh(verts.ToArray(), t, true);

		Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, mat, 0);
	}

	void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			Gizmos.color = Color.red;
		//	Gizmos.DrawSphere(TerrainGeneration.Triangulator.testVerts[testIndex], 1.2f);
		}
	}
}
