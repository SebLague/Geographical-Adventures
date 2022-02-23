using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellTest : MonoBehaviour
{

	public float innerRadius;
	public float outerRadius;
	public float collisionRadius;

	public Transform innerShellObject;
	public Transform outerShellObject;
	public Transform collisionObject;
	public Vector4 result;
	// Returns vector (dstToSphere, dstThroughSphere)
	// If ray origin is inside sphere, dstToSphere = 0
	// If ray misses sphere, dstToSphere = maxValue; dstThroughSphere = 0
	Vector2 RaySphere(Vector3 sphereCentre, float sphereRadius, Vector3 rayOrigin, Vector3 rayDir)
	{
		Vector3 offset = rayOrigin - sphereCentre;
		float a = 1; // Set to dot(rayDir, rayDir) if rayDir might not be normalized
		float b = 2 * Vector3.Dot(offset, rayDir);
		float c = Vector3.Dot(offset, offset) - sphereRadius * sphereRadius;
		float d = b * b - 4 * a * c; // Discriminant from quadratic formula

		// Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
		if (d > 0)
		{
			float s = Mathf.Sqrt(d);
			float dstToSphereNear = Mathf.Max(0, (-b - s) / (2 * a));
			float dstToSphereFar = (-b + s) / (2 * a);

			// Ignore intersections that occur behind the ray
			if (dstToSphereFar >= 0)
			{
				return new Vector2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
			}
		}
		// Ray did not intersect sphere
		return new Vector2(Mathf.Infinity, 0);
	}

	Vector2 RayShellSingle(Vector3 rayOrigin, Vector3 rayDir, float sceneDepth)
	{
		// Distances to, and through, the inner and outer spheres that define the shell where clouds can appear
		Vector2 innerSphereHitInfo = RaySphere(Vector3.zero, innerRadius, rayOrigin, rayDir);
		float dstToInnerSphere = innerSphereHitInfo.x;
		float dstThroughInnerSphere = innerSphereHitInfo.y;

		Vector2 outerSphereHitInfo = RaySphere(Vector3.zero, outerRadius, rayOrigin, rayDir);
		float dstToOuterSphere = outerSphereHitInfo.x;
		float dstThroughOuterSphere = outerSphereHitInfo.y;

		// Outputs:
		float dstToEntry = Mathf.Infinity;
		float length = 0;

		float dstFromCentre = (rayOrigin - Vector3.zero).magnitude;

		// Inside inner sphere (i.e. below the cloud shell)
		if (dstFromCentre < innerRadius)
		{
			dstToEntry = dstThroughInnerSphere;
			length = dstThroughOuterSphere - dstThroughInnerSphere;
		}
		// Outside the inner sphere, but inside the outer sphere (i.e. inside the cloud shell)
		else if (dstFromCentre < outerRadius)
		{
			dstToEntry = 0;
			length = Mathf.Min(dstThroughOuterSphere, dstToInnerSphere);
		}
		// Outside of both spheres (i.e. above the cloud shell)
		else
		{
			dstToEntry = dstToOuterSphere;
			length = Mathf.Min(dstThroughOuterSphere, dstToInnerSphere - dstToOuterSphere);
		}

		length = Mathf.Min(length, sceneDepth - dstToEntry);
		return new Vector2(dstToEntry, length);
	}

	// Returns vector: dstToEntryA, dstThroughShellA, dstToEntryB, dstThroughShellB
	Vector4 RayShell(Vector3 rayOrigin, Vector3 rayDir, float sceneDepth)
	{
		Vector2 a = RayShellSingle(rayOrigin, rayDir, sceneDepth);
		const float bias = 0.01f;
		float nextRayDst = a.x + a.y + bias;
		Vector2 b = RayShellSingle(rayOrigin + rayDir * nextRayDst, rayDir, sceneDepth - nextRayDst);
		return new Vector4(a.x, a.y, a.x + a.y + b.x, b.y);
	}

	void OnDrawGizmos()
	{
		innerShellObject.localScale = Vector3.one * innerRadius * 2;
		outerShellObject.localScale = Vector3.one * outerRadius * 2;
		collisionObject.localScale = Vector3.one * collisionRadius * 2;

		Vector3 viewPos = transform.position;
		Vector3 viewDir = transform.forward;

		float sceneDepth = RaySphere(collisionObject.position, collisionRadius, viewPos, viewDir).x;

		Gizmos.color = new Color(1, 1, 1, 0.3f);
		Gizmos.DrawRay(viewPos, viewDir * 100);
		Gizmos.color = Color.white;
		Gizmos.DrawSphere(viewPos, 0.25f);

		result = RayShell(viewPos, viewDir, sceneDepth);
		Gizmos.color = Color.green;
		if (result.y > 0)
		{
			Vector3 entryPointA = viewPos + viewDir * result.x;
			Vector3 exitPointA = entryPointA + viewDir * result.y;
			Gizmos.DrawSphere(entryPointA, 0.25f);
			Gizmos.DrawSphere(exitPointA, 0.25f);
			Gizmos.DrawLine(entryPointA, exitPointA);
			if (result.w > 0)
			{
				Gizmos.color = Color.red;
				Vector3 entryPointB = viewPos + viewDir * result.z;
				Vector3 exitPointB = entryPointB + viewDir * result.w;
				Gizmos.DrawLine(entryPointB, exitPointB);
				Gizmos.DrawSphere(entryPointB, 0.25f);
				Gizmos.DrawSphere(exitPointB, 0.25f);
			}
		}
	}
}
