using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeoGame.Quest;

public class GlobeDeliveryDisplay : MonoBehaviour
{

	public GeoGame.Quest.QuestSystem questSystem;
	public GlobeController globeController;

	public float arcHeightMin = 0.5f;
	public float arcHeightMax = 4;
	public float minAngle = 10;
	public float maxAngle = 30;
	public int maxLineResolution;
	public float dash = 10;
	public LineRenderer lineRenderer;
	public Transform packagePointVis;
	public Transform targetPointVis;
	public GameObject holder;

	public void UpdateDisplay()
	{
		holder.SetActive(false);
		if (questSystem != null)
		{
			var deliveryResults = questSystem.GetResults();
			if (deliveryResults.Length > 0)
			{
				holder.SetActive(true);
				var lastResult = deliveryResults[deliveryResults.Length - 1];
				DisplayResult(lastResult);
			}
		}
	}



	void DisplayResult(QuestSystem.DeliveryResult result)
	{
		targetPointVis.position = GetPointOnMap(result.targetCityPoint.normalized);
		packagePointVis.position = GetPointOnMap(result.packagedLandedPoint.normalized);
		targetPointVis.gameObject.name = $"{result.targetCity.name}, {result.targetCountry.GetPreferredDisplayName(18)}";

		if (result.distanceKM > QuestSystem.perfectRadius)
		{
			DrawArc(targetPointVis.position, packagePointVis.position);
		}
		else
		{
			lineRenderer.gameObject.SetActive(false);
		}
	}

	void DrawArc(Vector3 pointA, Vector3 pointB)
	{

		float heightA = pointA.magnitude;
		float heightB = pointB.magnitude;


		Vector3 dirA = pointA.normalized;
		Vector3 dirB = pointB.normalized;

		float angleDegrees = Vector3.Angle(dirA, dirB);
		float arcHeight = Mathf.Lerp(arcHeightMin, arcHeightMax, Mathf.InverseLerp(minAngle, maxAngle, angleDegrees));

		// Calculate two quaternions rotated towards the two sphere sphere points
		Vector3 greatCircleNormal = Vector3.Cross(dirA, dirB).normalized;
		Quaternion rotationA = Quaternion.LookRotation(dirA, greatCircleNormal);
		Quaternion rotationB = Quaternion.LookRotation(dirB, greatCircleNormal);

		int res = (int)Mathf.Max(10, Mathf.Clamp01(angleDegrees / 180) * maxLineResolution);

		Vector3[] localPathPoints = new Vector3[res];
		Vector3 pOld = pointA;
		for (int i = 0; i < res; i++)
		{
			float pathT = i / (res - 1f);
			// Spherically interpolate between the start and end of the path
			Vector3 unitPathPoint = Quaternion.Slerp(rotationA, rotationB, pathT) * Vector3.forward;
			float pathCentreT = 1 - Mathf.Abs(pathT - 0.5f) * 2; // 0 at start, 1 at midpoint, 0 at end
			float height = Mathf.Lerp(heightA, heightB, pathT) + arcHeight * (1 - (1 - pathCentreT) * (1 - pathCentreT));

			Vector3 pathPointWorld = unitPathPoint * height;
			//Debug.DrawLine(pathPoint, pOld, Color.red);
			pOld = pathPointWorld;
			localPathPoints[i] = transform.InverseTransformPoint(pathPointWorld);
		}
		lineRenderer.gameObject.SetActive(true);
		lineRenderer.positionCount = localPathPoints.Length;
		lineRenderer.SetPositions(localPathPoints);
		lineRenderer.material.SetFloat("_DashCount", Mathf.Max(4, angleDegrees / 180f * dash));
	}

	Vector3 GetPointOnMap(Vector3 pointOnUnitSphere)
	{
		Ray ray = new Ray(pointOnUnitSphere * 100, -pointOnUnitSphere);
		RaycastHit hit;


		if (Physics.Raycast(ray, out hit, Mathf.Infinity))
		{
			return hit.point;
		}

		// This should never happen
		Debug.Log("Failed to find point on globe!");
		return pointOnUnitSphere * 20;
	}
}
