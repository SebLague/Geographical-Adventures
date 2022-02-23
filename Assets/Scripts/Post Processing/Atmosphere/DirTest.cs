using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirTest : MonoBehaviour
{

	public int sqrtSamples = 8;
	public float displaySize = 1;
	public bool useFibSphere;
	public bool drawSunDir;



	Vector3 GetSphericalDir(float theta, float phi)
	{
		float cosPhi = Mathf.Cos(phi);
		float sinPhi = Mathf.Sin(phi);
		float cosTheta = Mathf.Cos(theta);
		float sinTheta = Mathf.Sin(theta);
		return new Vector3(sinPhi * sinTheta, cosPhi, sinPhi * cosTheta);
	}

	void OnDrawGizmos()
	{
		if (drawSunDir)
		{
			for (int y = 0; y < sqrtSamples; y++)
			{
				for (int x = 0; x < sqrtSamples; x++)
				{
					Vector2 uv = new Vector2(x, y) / (sqrtSamples - 1f);

					float theta = uv.x * Mathf.PI;
					Vector3 dir = new Vector3(Mathf.Sin(theta), Mathf.Cos(theta), 0);
					//float sunCosTheta = uv.x * 2 - 1;
					//float sunTheta = Mathf.Acos(sunCosTheta);
					//Vector3 dir = new Vector3(Mathf.Sin(sunTheta), sunCosTheta, 0);

					Gizmos.DrawSphere(dir, 0.01f * displaySize);
				}
			}
		}
		else
		{


			if (useFibSphere)
			{
				float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
				float angleIncrement = Mathf.PI * 2 * goldenRatio;

				for (int i = 0; i < sqrtSamples * sqrtSamples; i++)
				{
					float t = (float)i / (sqrtSamples * sqrtSamples);
					float inclination = Mathf.Acos(1 - 2 *  t);
					float azimuth = angleIncrement * i;

					float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
					float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
					float z = Mathf.Cos(inclination);
					Vector3 dir = new Vector3(x, y, z);
					Gizmos.DrawSphere(dir, 0.01f * displaySize);
				}

			}
			else
			{
				for (int x = 0; x < sqrtSamples; x++)
				{
					for (int y = 0; y < sqrtSamples; y++)
					{
						float theta = ((x + 0.5f) / sqrtSamples) * Mathf.PI;
						float phi = Mathf.Acos(1 - 2 * (y + 0.5f) / sqrtSamples);
						Vector3 dir = GetSphericalDir(theta, phi);
						Gizmos.DrawSphere(dir, 0.01f * displaySize);

					}
				}
			}
		}
	}
}