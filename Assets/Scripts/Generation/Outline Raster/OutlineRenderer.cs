using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineRenderer : MonoBehaviour
{
	public float width;

	public Shader lineShader;
	public Shader lineJoinsShader;
	public int circleJoinResolution;

	Mesh lineSegmentMesh;
	Mesh circleJoinMesh;

	List<LineMesh> lines;


	public void Add(LineSegment[] lineSegments, Color colour)
	{
		LineMesh line = new LineMesh(lineSegments, colour);
		line.Prepare(lineShader, lineJoinsShader, lineSegmentMesh, circleJoinMesh);
		lines.Add(line);
	}


	void Awake()
	{
		CreateLineMesh();
		CreateCircleJoinMesh();
		lines = new List<LineMesh>();
	}


	void Update()
	{
		for (int i = 0; i < lines.Count; i++)
		{
			lines[i].Draw(width);
		}
	}

	void CreateLineMesh()
	{
		lineSegmentMesh = new Mesh();

		Vector3[] vertices = {
			new Vector3(0,-0.5f), // bottom left
			new Vector3(1,-0.5f), // bottom right
			new Vector3(1,0.5f), // top right
			new Vector3(0, 0.5f) // top left
		};

		int[] triangles = { 0, 2, 1, 0, 3, 2 };

		lineSegmentMesh.SetVertices(vertices);
		lineSegmentMesh.SetTriangles(triangles, 0, true);
	}

	void CreateCircleJoinMesh()
	{
		int numIncrements = (int)Mathf.Max(3, circleJoinResolution);

		float angleIncrement = (2 * Mathf.PI) / (numIncrements - 1f);
		var verts = new Vector3[numIncrements + 1];
		var tris = new int[(numIncrements - 1) * 3];
		verts[0] = Vector3.zero;

		for (int i = 0; i < numIncrements; i++)
		{
			float currAngle = angleIncrement * i;
			Vector3 pos = new Vector3(Mathf.Sin(currAngle), Mathf.Cos(currAngle), 0);
			verts[i + 1] = pos;

			if (i < numIncrements - 1)
			{
				tris[i * 3] = 0;
				tris[i * 3 + 1] = i + 1;
				tris[i * 3 + 2] = i + 2;
			}
		}
		circleJoinMesh = new Mesh();
		circleJoinMesh.SetVertices(verts);
		circleJoinMesh.SetTriangles(tris, 0, true);
	}

	public class LineMesh
	{
		LineSegment[] lineSegments;
		Bounds bounds;
		ComputeBuffer lineSegmentsBuffer;
		ComputeBuffer lineArgsBuffer;
		ComputeBuffer joinsArgsBuffer;
		Material lineMat;
		Material joinsMat;
		Mesh lineSegmentMesh;
		Mesh circleJoinMesh;
		Color colour;

		public LineMesh(LineSegment[] lineSegments, Color colour)
		{
			this.lineSegments = lineSegments;
			this.colour = colour;
		}

		public void Prepare(Shader lineShader, Shader joinsShader, Mesh lineSegmentMesh, Mesh circleJoinMesh)
		{
			this.lineSegmentMesh = lineSegmentMesh;
			this.circleJoinMesh = circleJoinMesh;

			// Create buffers
			ComputeHelper.CreateStructuredBuffer<LineSegment>(ref lineSegmentsBuffer, lineSegments.Length);
			lineSegmentsBuffer.SetData(lineSegments);

			lineArgsBuffer = ComputeHelper.CreateArgsBuffer(lineSegmentMesh, lineSegments.Length);
			joinsArgsBuffer = ComputeHelper.CreateArgsBuffer(circleJoinMesh, lineSegments.Length);

			// Calculate bounds
			bounds = new Bounds(lineSegments[0].pointA, Vector3.zero);
			for (int i = 1; i < lineSegments.Length; i++)
			{
				bounds.Encapsulate(lineSegments[i].pointB);
			}

			// Create materials
			lineMat = new Material(lineShader);
			joinsMat = new Material(joinsShader);

			lineMat.SetBuffer("lineSegments", lineSegmentsBuffer);
			joinsMat.SetBuffer("lineSegments", lineSegmentsBuffer);
		}

		public void Draw(float width)
		{
			lineMat.SetColor("colour", colour);
			lineMat.SetFloat("width", width);
			Graphics.DrawMeshInstancedIndirect(lineSegmentMesh, 0, lineMat, bounds, lineArgsBuffer);

			joinsMat.SetColor("colour", colour);
			joinsMat.SetFloat("width", width);
			Graphics.DrawMeshInstancedIndirect(circleJoinMesh, 0, joinsMat, bounds, joinsArgsBuffer);
		}

		public void Release()
		{
			ComputeHelper.Release(lineSegmentsBuffer, joinsArgsBuffer, lineArgsBuffer);
		}
	}


	void OnDestroy()
	{
		for (int i = 0; i < lines.Count; i++)
		{
			lines[i].Release();
		}
	}
}
public struct LineSegment
{
	public Vector3 pointA;
	public Vector3 pointB;
}
