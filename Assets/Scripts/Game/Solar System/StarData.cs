using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SolarSystem
{
	[CreateAssetMenu(menuName = "Data/Star Data")]
	public class StarData : ScriptableObject
	{

		[SerializeField] TextAsset starFile;
		[SerializeField] float magnitudeThreshold = 6.5f;
		[SerializeField] Gradient gradient;

		[Header("Data")]
		[SerializeField] Star[] stars;

		public int NumStars
		{
			get
			{
				return stars.Length;
			}
		}

		public Star[] Stars
		{
			get
			{
				return stars;
			}
		}

		public void CreateStarData()
		{
			List<Star> starList = new List<Star>();

			MinMax magnitudeRange = new MinMax();
			MinMax temperatureRange = new MinMax();

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
						float colorIndex = 0;
						if (float.TryParse(values[16], out colorIndex))
						{
							temperatureRange.AddValue(colorIndex);
						}
						magnitudeRange.AddValue(magnitude);
						//temperatureRange.AddValue(colorIndex);
						Coordinate coord = new Coordinate((rightAscension * 360f / 24 - 180) * Mathf.Deg2Rad, declination * Mathf.Deg2Rad);

						Vector3 dir = GeoMaths.CoordinateToPoint(coord, 1);

						Star star = new Star();
						star.direction = dir;
						star.brightnessT = magnitude;
						//https://en.wikipedia.org/wiki/Color_index
						star.colour = gradient.Evaluate(Mathf.InverseLerp(-0.33f, 1.40f, colorIndex));
						starList.Add(star);
					}
				}
			}

			// Scale magnitude between 0 and 1
			// (with 1 being brightest, i.e the one with the lowest magnitude since lower is brighter for whatever reason!)
			for (int i = 0; i < starList.Count; i++)
			{
				Star star = starList[i];
				star.brightnessT = 1 - Mathf.InverseLerp(magnitudeRange.minValue, magnitudeRange.maxValue, starList[i].brightnessT);
				starList[i] = star;
			}
			stars = starList.ToArray();
		}

		[System.Serializable]
		public struct Star
		{
			public Vector3 direction;
			public float brightnessT;
			public Color colour;
		}
	}
}