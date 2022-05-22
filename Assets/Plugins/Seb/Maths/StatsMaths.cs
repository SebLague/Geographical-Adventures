using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Some statistical maths

namespace Seb
{
	public static partial class Maths
	{
		/// <summary>
		/// Returns a random value with normal distribution.
		/// The mean determines the 'centre' of the distribution. 
		/// The standardDeviation controls the spread of the distribution (i.e. how likely it is to get values that are far from the mean).
		/// See https://www.desmos.com/calculator/0dnzmd0x0h for example.
		/// </summary>
		public static float RandomNormal(System.Random rng, float mean = 0, float standardDeviation = 1)
		{
			// Thanks to https://stackoverflow.com/a/6178290
			float theta = 2 * Mathf.PI * (float)rng.NextDouble();
			float rho = Mathf.Sqrt(-2 * Mathf.Log((float)rng.NextDouble()));
			float scale = standardDeviation * rho;
			return mean + scale * Mathf.Cos(theta);
		}


		/// <summary>
		/// Pick random index, weighted by the weights array.
		/// For example, if the array contains {1, 6, 3}...
		/// The possible indices would be (0, 1, 2)
		/// and the probabilities for these would be (1/10, 6/10, 3/10)
		/// </summary>
		public static int WeightedRandomIndex(System.Random prng, float[] weights)
		{
			float weightSum = 0;
			for (int i = 0; i < weights.Length; i++)
			{
				weightSum += weights[i];
			}

			float randomValue = (float)prng.NextDouble() * weightSum;
			float cumul = 0;

			for (int i = 0; i < weights.Length; i++)
			{
				cumul += weights[i];
				if (randomValue < cumul)
				{
					return i;
				}
			}

			return weights.Length - 1;
		}
	}
}
