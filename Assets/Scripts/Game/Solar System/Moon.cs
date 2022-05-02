using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SolarSystem
{
	public class Moon : MonoBehaviour
	{
		public float moonOrbitAngle;
		public float moonTilt;
		public float periapsis;
		public float apoapsis;
		public float dstMultiplier;
		public float size;
		public int resolution;



		public Renderer moonRenderer;

		Material material;
		[Header("Debug")]
		public Camera camTest;
		public float debug_dst;
		public bool freezeOrbit;




		void Start()
		{

			///moonMesh = sphere.GetMesh();
			//GetComponentInChildren<MeshFilter>().mesh = moonMesh;
			//material = GetComponentInChildren<MeshRenderer>().material;
		}

		public void UpdateOrbit(float monthT, EarthOrbit earth, bool geocentric)
		{

			transform.localScale = Vector3.one * size * ((Application.isPlaying) ? 1 : 2);

			if (freezeOrbit)
			{
				return;
			}

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


			//Graphics.DrawMesh(moonMesh, Matrix4x4.TRS(transform.position, transform.GetChild(0).rotation, transform.localScale * scaleMul), moonMat, LayerMask.NameToLayer("ExtraTerrestrial"));
		}

		public void Setup(CommandBuffer cmd)
		{
			//cmd.DrawMesh(mesh, Matrix4x4.TRS(new Vector3(70, 134, -80), Quaternion.identity, Vector3.one * 30), mat);
			moonRenderer.gameObject.GetComponent<MeshFilter>().mesh = Seb.Meshing.IcoSphere.Generate(resolution).ToMesh();
			Material mat = moonRenderer.sharedMaterial;

			cmd.DrawRenderer(moonRenderer, mat);
			//cmd.DrawMesh(moonMesh, Matrix4x4.TRS(Camera.main.transform.position + Camera.main.transform.forward * (10 + scaleMul), transform.GetChild(0).rotation, transform.localScale), moonMat);
		}

	}

}