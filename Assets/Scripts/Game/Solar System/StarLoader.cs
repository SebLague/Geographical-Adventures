using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarLoader : MonoBehaviour
{

	public TextAsset starFile;
	public float magnitudeThreshold = 6.5f;

	public Star[] LoadStars()
	{
		List<Star> stars = new List<Star>();

		float magnitudeMin = float.MaxValue;
		float magnitudeMax = float.MinValue;

		using (System.IO.StringReader reader = new System.IO.StringReader(starFile.text))
		{
			string header = reader.ReadLine();
			string sol = reader.ReadLine(); // skip the sun since this is handled differently

			while (true)
			{
				string line = reader.ReadLine();
				if (string.IsNullOrEmpty(line))
				{
					break;
				}

				string[] values = line.Split(',');
				string starName = values[6];
				float magnitude = float.Parse(values[13]);

				// Corresponds to longitude. Measured in hours [0, 24)
				float rightAscension = float.Parse(values[7]);
				// Corresponds to latitude. Measured in degrees [-90, 90]
				float declination = float.Parse(values[8]);

				if (magnitude <= magnitudeThreshold)
				{
					magnitudeMin = Mathf.Min(magnitude, magnitudeMin);
					magnitudeMax = Mathf.Max(magnitude, magnitudeMax);
					Coordinate coord = new Coordinate((rightAscension * 360f / 24 - 180) * Mathf.Deg2Rad, declination * Mathf.Deg2Rad);

					Vector3 dir = CoordinateSystem.CoordinateToPoint(coord, 1);
					stars.Add(new Star() { direction = dir, brightnessT = magnitude });
				}
			}
		}

		// Scale magnitude between 0 and 1
		// (with 1 being brightest, i.e the one with the lowest magnitude since lower is brighter for whatever reason!)
		for (int i = 0; i < stars.Count; i++)
		{
			Star star = stars[i];
			star.brightnessT = 1 - Mathf.InverseLerp(magnitudeMin, magnitudeMax, stars[i].brightnessT);
			stars[i] = star;
		}
		return stars.ToArray();
	}

	public struct Star
	{
		public Vector3 direction;
		public float brightnessT;
	}

}
