using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonTest : MonoBehaviour
{
	public Transform[] points;
	public bool duplicateLastPoint;

	public int randomSeed;
	public int numRandomPoints;

	public Transform a;
	public Transform b;

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

		Vector2[] randomPoints = Maths.Polygon.RandomPointsInside(polygonPath, numRandomPoints, randomSeed);
		Gizmos.color = Color.cyan;
		for (int i = 0; i < randomPoints.Length; i ++) {
			Gizmos.DrawSphere(randomPoints[i], 0.5f);
		}
		
		//Debug.Log(Maths.Polygon.IsClockwise(polygonPath));

	}
}
