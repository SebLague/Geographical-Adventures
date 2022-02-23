using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moon : MonoBehaviour
{
	public float moonOrbitAngle;
	public float moonTilt;
	public float periapsis;
	public float apoapsis;
	public float dstMultiplier;
	public float size;
	public int resolution;


	Material material;
	[Header("Debug")]
	public Camera camTest;
	public float debug_dst;


	void Start()
	{
		IcoSphere sphere = new IcoSphere(resolution);
		GetComponentInChildren<MeshFilter>().mesh = sphere.GetMesh();
		//material = GetComponentInChildren<MeshRenderer>().material;
	}

	public void UpdateOrbit(float monthT, EarthOrbit earth, bool geocentric)
	{

		transform.localScale = Vector3.one * size * ((Application.isPlaying) ? 1 : 2);

		Vector3 xAxis = new Vector3(Mathf.Cos(moonOrbitAngle * Mathf.Deg2Rad), Mathf.Sin(moonOrbitAngle * Mathf.Deg2Rad), 0);
		Vector3 yAxis = Vector3.forward;

		Vector2 orbitPos = Orbit.CalculatePointOnOrbit(periapsis, apoapsis, monthT);
		debug_dst = orbitPos.magnitude;
		Vector3 moonPos = (xAxis * orbitPos.x + yAxis * orbitPos.y) * dstMultiplier;
		Quaternion moonRot = Quaternion.Euler(0, 0, -moonTilt) * Quaternion.Euler(0, -monthT * 360, 0);

		// Earth object doesn't actually move/rotate, so have to move moon to account for that
		if (geocentric)
		{
			transform.position = Quaternion.Inverse(earth.earthRot) * moonPos;
			transform.rotation = Quaternion.Inverse(earth.earthRot) * moonRot;
		}
		else
		{
			transform.position = earth.earthPos + moonPos;
			transform.rotation = moonRot;
		}

		if (camTest)
		{
			camTest.transform.position = (geocentric) ? Vector3.zero : earth.earthPos;
			camTest.transform.LookAt(transform);
		}

	}
}
