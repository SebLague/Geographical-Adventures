using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.Meshing
{
	public static class PipeMeshGenerator
	{

		public static SimpleMeshData GenerateMesh(Vector3[] path, bool closed, float radius = 1, int resolution = 8)
		{
			int numPointsPerCircle = 3 + Mathf.Max(0, resolution);
			int numVertices = path.Length * numPointsPerCircle;
			int numTriangles = (path.Length + ((closed) ? 0 : -1)) * numPointsPerCircle * 2;

			Vector3[] vertices = new Vector3[numVertices];
			int[] triangles = new int[numTriangles * 3];
			Axes[] allAxes = CalculateAxes(path, closed);

			int numCircles = path.Length;
			int triIndex = 0;

			for (int circleIndex = 0; circleIndex < numCircles; circleIndex++)
			{
				float segmentPercent = circleIndex / (numCircles - 1f);
				Vector3 centerPos = path[circleIndex];
				Axes axes = allAxes[circleIndex];

				// Add each vertex in the circle, and triangulate the cylinder formed by this circle and the next one
				for (int i = 0; i < numPointsPerCircle; i++)
				{
					var angle = ((float)i / numPointsPerCircle) * (Mathf.PI * 2.0f);

					var x = Mathf.Sin(angle) * radius;
					var y = Mathf.Cos(angle) * radius;

					var point = (axes.normal * x) + (axes.binormal * y) + centerPos;
					int vertexIndex = circleIndex * numPointsPerCircle + i;
					vertices[vertexIndex] = point;

					// Adding the triangles
					if (circleIndex < numCircles - 1 || closed)
					{
						int startIndex = numPointsPerCircle * circleIndex;
						triangles[triIndex + 0] = (startIndex + i) % vertices.Length;
						triangles[triIndex + 1] = (startIndex + (i + 1) % numPointsPerCircle) % vertices.Length;
						triangles[triIndex + 2] = (startIndex + i + numPointsPerCircle) % vertices.Length;

						triangles[triIndex + 3] = (startIndex + (i + 1) % numPointsPerCircle) % vertices.Length;
						triangles[triIndex + 4] = (startIndex + (i + 1) % numPointsPerCircle + numPointsPerCircle) % vertices.Length;
						triangles[triIndex + 5] = (startIndex + i + numPointsPerCircle) % vertices.Length;
						triIndex += 6;
					}

				}
			}
			SimpleMeshData meshData = new SimpleMeshData(vertices, triangles);
			return meshData;

		}

		// Calculate tangents, normals, and binormals for each point on the path
		public static Axes[] CalculateAxes(Vector3[] path, bool closed)
		{
			// Calculate path tangents
			Vector3[] tangents = new Vector3[path.Length];
			for (int i = 0; i < path.Length; i++)
			{
				int nextIndex = i + 1;
				if (i == path.Length - 1)
				{
					nextIndex = (closed) ? 0 : path.Length - 1;
				}
				int prevIndex = i - 1;
				if (i == 0)
				{
					prevIndex = (closed) ? path.Length - 1 : 0;
				}

				Vector3 dirToNext = (path[nextIndex] - path[i]).normalized;
				Vector3 dirFromPrev = (path[i] - path[prevIndex]).normalized;
				Vector3 tangent = (dirToNext + dirFromPrev).normalized;
				tangents[i] = tangent;
			}


			// Calculate normals
			// (using the "Rotation Minimising Frames" technique described here: https://pomax.github.io/bezierinfo/#pointvectors3d)
			Vector3 lastRotationAxis = Vector3.Cross(tangents[0], tangents[1]).normalized;
			if (lastRotationAxis == Vector3.zero)
			{
				lastRotationAxis = Vector3.Cross(tangents[0], (tangents[0] == Vector3.forward) ? Vector3.up : Vector3.forward).normalized;
			}
			Vector3 firstNormal = Vector3.Cross(lastRotationAxis, tangents[0]).normalized;

			Vector3[] normals = new Vector3[path.Length];
			normals[0] = firstNormal;

			for (int i = 1; i < path.Length; i++)
			{
				// First reflection
				Vector3 offset = (path[i] - path[i - 1]);
				float sqrDst = offset.sqrMagnitude;
				Vector3 r = lastRotationAxis - offset * 2 / sqrDst * Vector3.Dot(offset, lastRotationAxis);
				Vector3 t = tangents[i - 1] - offset * 2 / sqrDst * Vector3.Dot(offset, tangents[i - 1]);

				// Second reflection
				Vector3 v2 = tangents[i] - t;
				float c2 = Vector3.Dot(v2, v2);

				Vector3 finalRot = r - v2 * 2 / c2 * Vector3.Dot(v2, r);
				Vector3 n = Vector3.Cross(finalRot, tangents[i]).normalized;
				normals[i] = n;
				lastRotationAxis = finalRot;
			}

			// Apply correction for 3d normals along a closed path (TODO: not sure how well this works, might be worth rethinking...)
			if (closed)
			{
				// Get angle between first and last normal (if zero, they're already lined up, otherwise we need to correct)
				float normalsAngleErrorAcrossJoin = Vector3.SignedAngle(normals[path.Length - 1], normals[0], tangents[0]);
				// Gradually rotate the normals along the path to ensure start and end normals line up correctly
				if (Mathf.Abs(normalsAngleErrorAcrossJoin) > 0.01f) // don't bother correcting if very nearly correct
				{
					for (int i = 1; i < normals.Length; i++)
					{
						float t = (i / (normals.Length - 1f));
						float angle = normalsAngleErrorAcrossJoin * t;
						Quaternion rot = Quaternion.AngleAxis(angle, tangents[i]);
						normals[i] = rot * normals[i];
					}
				}
			}

			// Store results in axes structure and return
			Axes[] allAxes = new Axes[path.Length];
			for (int i = 0; i < path.Length; i++)
			{
				Axes axes = new Axes();
				axes.tangent = tangents[i];
				axes.normal = normals[i];
				axes.binormal = Vector3.Cross(normals[i], tangents[i]).normalized;
				allAxes[i] = axes;
			}

			return allAxes;
		}


		public struct Axes
		{
			public Vector3 tangent;
			public Vector3 normal;
			public Vector3 binormal;
		}
	}
}