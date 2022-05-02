using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortPointsByAngleTest : MonoBehaviour
{
	public bool sort;
	public bool clockwise;

#if UNITY_EDITOR

	void OnDrawGizmos()
	{
		Vector2[] points = Seb.Helpers.TransformHelper.GetAllChildPositions2D(transform);

		Gizmos.color = (sort) ? Color.green : Color.red;
		if (sort)
		{
			Maths.Sorting.SortPointsByAngle(points, clockwise);
		}

		for (int i = 0; i < points.Length; i++)
		{
			UnityEditor.Handles.Label(points[i] + Vector2.up, i.ToString());
			Gizmos.DrawLine(points[i], points[(i + 1) % points.Length]);
		}
	}

#endif


}
