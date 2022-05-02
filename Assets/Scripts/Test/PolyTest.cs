using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyTest : MonoBehaviour
{
	public Transform[] points;
	public bool duplicateLastPoint;

	void OnDrawGizmos()
	{
		Vector2[] polygonPath = new Vector2[(duplicateLastPoint) ? points.Length + 1 : points.Length];
		for (int i = 0; i < points.Length; i++)
		{
			polygonPath[i] = points[i].position;
		}
		if (duplicateLastPoint)
		{
			polygonPath[polygonPath.Length - 1] = polygonPath[0];
		}

		Gizmos.color = Color.green;
		for (int i = 0; i < points.Length; i++)
		{
			Gizmos.DrawLine(points[i].position, points[(i + 1) % points.Length].position);
		}

		bool inPoly = Maths.Polygon.ContainsPoint(transform.position, polygonPath);
		Gizmos.color = (inPoly) ? Color.white : Color.red;
		Gizmos.DrawSphere(transform.position, 0.5f);

		Debug.Log(Maths.Polygon.IsClockwise(polygonPath));
	}
}
