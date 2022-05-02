using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class GlobePlayerTrail : MonoBehaviour
{

	public Transform globe;
	public Transform playerGraphic;
	public float playerScale;
	public float playerHeight;

	public float trailDrawLength;
	public float dstBetweenTrailPoints;
	public MeshRenderer trailPointPrefab;
	public float trailPointSize = 0.1f;

	GameObject trailHolder;

	void Update()
	{
		//	if (Input.GetKeyDown(KeyCode.U))
		{
			//Debug.Log("Update Trail");
			//CreateTrail();
		}
	}

	public void ShowPlayer(Player player)
	{
		playerGraphic.localScale = Vector3.one * playerScale;
		playerGraphic.localPosition = player.transform.position.normalized * playerHeight;
		playerGraphic.localRotation = player.transform.rotation;

		CreateTrail(player);
	}

	void CreateTrail(Player player)
	{
		if (trailHolder != null)
		{
			Destroy(trailHolder);
		}
		trailHolder = new GameObject("Trail Holder");
		trailHolder.transform.parent = globe;
		trailHolder.transform.localPosition = Vector3.zero;
		trailHolder.transform.localRotation = Quaternion.identity;
		trailHolder.transform.localScale = Vector3.one;

		Vector3[] playerPositionHistory = player.positionHistory.ToArray();
		List<Vector3> trailPoints = new List<Vector3>();
		float currentTrailLength = 0;
		trailPoints.Add(player.transform.position);

		for (int i = playerPositionHistory.Length - 1; i >= 0; i--)
		{
			Vector3 point = playerPositionHistory[i];
			currentTrailLength += (point - trailPoints[trailPoints.Count - 1]).magnitude;
			//Debug.Log(i + "  " + currentTrailLength + "  " + point + "  " + trailPoints[trailPoints.Count - 1]);
			if (currentTrailLength < trailDrawLength)
			{
				trailPoints.Add(point);
			}
			else
			{
				break;
			}
		}


		// Scale points for display on globe
		for (int i = 0; i < trailPoints.Count; i++)
		{
			trailPoints[i] = trailPoints[i].normalized * playerHeight;
		}

		if (trailPoints.Count > 1)
		{

			BezierPath bezierPath = new BezierPath(trailPoints, isClosed: false, PathCreation.PathSpace.xyz);
			PathCreation.VertexPath path = new PathCreation.VertexPath(bezierPath, transform: transform);

			float startDst = 0.75f; // (don't want to start in the middle of the plane)
			for (float dst = startDst; dst < path.length; dst += dstBetweenTrailPoints)
			{
				float t = dst / path.length;
				float oneMinusT = Mathf.Clamp01(1 - t);
				Vector3 pathPoint = path.GetPointAtDistance(dst);
				MeshRenderer trailPointRenderer = Instantiate(trailPointPrefab, pathPoint, Quaternion.identity);

				trailPointRenderer.transform.SetParent(trailHolder.transform, worldPositionStays: true);
				trailPointRenderer.transform.localPosition = trailPointRenderer.transform.localPosition.normalized * playerHeight;
				trailPointRenderer.transform.localScale = Vector3.one * trailPointSize * oneMinusT;
				trailPointRenderer.material.color = new Color(1, 1, 1, oneMinusT);
				trailPointRenderer.gameObject.layer = gameObject.layer;

			}
		}

	}

}
