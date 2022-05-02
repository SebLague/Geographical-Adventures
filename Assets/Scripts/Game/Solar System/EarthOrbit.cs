using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SolarSystem
{
	public class EarthOrbit : MonoBehaviour
	{

		public Quaternion earthRot { get; private set; }
		public Vector3 earthPos { get; private set; }
		public float currentAxisAngle { get; private set; }

		public float periapis = 147.2f;
		public float apoapsis = 152.1f;
		public float tilt = 23.4f;

		public float distanceScale = 1;

		[Header("Debug")]
		public float debug_dst;

		public void UpdateOrbit(float yearT, float dayT, bool geocentric)
		{
			Vector2 orbitEllipse = Orbit.CalculatePointOnOrbit(periapis, apoapsis, yearT);
			earthPos = new Vector3(orbitEllipse.x, 0, orbitEllipse.y) * distanceScale;
			debug_dst = orbitEllipse.magnitude;

			float siderealDayAngle = -dayT * 360;
			float solarDayAngle = siderealDayAngle - yearT * 360;
			currentAxisAngle = solarDayAngle;

			earthRot = Quaternion.Euler(0, 0, -tilt) * Quaternion.Euler(0, currentAxisAngle, 0);

			if (geocentric)
			{
				transform.position = Vector3.zero;
				transform.rotation = Quaternion.identity;
			}
			else
			{
				transform.position = earthPos;
				transform.rotation = earthRot;
			}

		}
	}
}