using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SolarSystem
{
	[ExecuteInEditMode]
	public class Sun : MonoBehaviour
	{

		public bool animateSunColour;
		public Gradient sunColGradient;
		public float dayStartOffset;
		Light lightSource;
		Camera cam;

		[Header("Debug")]
		public Color sunColour;
		public float timeOfDayT;
		public int maxSize;

		void Start()
		{
			lightSource = GetComponent<Light>();
			cam = Camera.main;
		}

		void Update()
		{
			//lightSource.shadowCustomResolution = maxSize;
		}


		// For simplicity, earth is stationary and doesn't rotate.
		// Instead the sun calculates the earth's spin and orbit, and positions itself relative to that
		public void UpdateOrbit(EarthOrbit earth, bool geocentric)
		{

			if (geocentric)
			{
				transform.position = Quaternion.Inverse(earth.earthRot) * -earth.earthPos;
				transform.LookAt(Vector3.zero);

				UpdateColourApprox(Vector3.zero);
			}
			else
			{
				transform.position = Vector3.zero;
				transform.LookAt(earth.earthPos);

				UpdateColourApprox(earth.earthPos);
			}



		}

		// Estimate sunlight colour based on angle from viewer to sun.
		// Alternative would be reading data from atmosphere system, but that's on gpu so would rather avoid
		void UpdateColourApprox(Vector3 earthPos)
		{

			Vector3 dirToCam = (cam.transform.position - earthPos).normalized;
			Vector3 dirToSun = -transform.forward;
			timeOfDayT = Mathf.Max(0, (Vector3.Dot(dirToCam, dirToSun) + dayStartOffset) / (1 + dayStartOffset));
			sunColour = sunColGradient.Evaluate(timeOfDayT);

			if (animateSunColour)
			{
				lightSource.color = sunColour;
			}
		}
	}
}