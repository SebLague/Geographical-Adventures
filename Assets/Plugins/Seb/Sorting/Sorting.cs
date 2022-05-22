using UnityEngine;

namespace Seb
{
	public static class Sorting
	{

		/// <summary>
		/// Sorts the given array based on their corresponding 'score' values.
		/// Note: the scores array will also be sorted in the process.
		/// </summary>
		public static void SortByScores<ItemType, ScoreType>(ItemType[] items, ScoreType[] scores, bool highToLow = true) where ScoreType : System.IComparable
		{
			Debug.Assert(items.Length == scores.Length, "Cannot sort if array length does not match score length");

			for (int i = 0; i < items.Length - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					int comparison = scores[swapIndex].CompareTo(scores[j]);
					bool swap = (highToLow) ? comparison < 0 : comparison > 0;

					if (swap)
					{
						(items[j], items[swapIndex]) = (items[swapIndex], items[j]);
						(scores[j], scores[swapIndex]) = (scores[swapIndex], scores[j]);
					}
				}
			}
		}

		/// <summary>
		/// Sorts the given array based on a given comparison function.
		/// </summary>
		public static void Sort<T>(T[] items, System.Func<T, T, int> comparisonFunction)
		{
			for (int i = 0; i < items.Length - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					int relativeScore = comparisonFunction.Invoke(items[swapIndex], items[j]);
					if (relativeScore < 0)
					{
						(items[j], items[swapIndex]) = (items[swapIndex], items[j]);
					}
				}
			}
		}

		/// <summary>
		/// Randomly shuffles the elements of the given array
		/// </summary>
		public static void ShuffleArray<T>(T[] array, System.Random rng)
		{
			// wikipedia.org/wiki/Fisher–Yates_shuffle#The_modern_algorithm
			for (int i = 0; i < array.Length - 1; i++)
			{
				int randomIndex = rng.Next(i, array.Length);
				(array[randomIndex], array[i]) = (array[i], array[randomIndex]);
			}
		}

		/// <summary>
		/// Sorts the given array of points based on their angle from their centre (centre calculated as average position)
		/// </summary>
		public static void SortPointsByAngle(Vector2[] points, bool clockwise = true)
		{
			if (points.Length == 0)
			{
				return;
			}

			Vector2 pointSum = Vector2.zero;
			for (int i = 0; i < points.Length; i++)
			{
				pointSum += points[i];
			}
			Vector2 centre = pointSum / points.Length;

			SortPointsByAngle(points, centre, clockwise);
		}

		/// <summary>
		/// Sorts the given array of points based on their angle from a given origin point
		/// </summary>
		public static void SortPointsByAngle(Vector2[] points, Vector2 origin, bool clockwise = true)
		{

			if (clockwise)
			{
				ArrayHelper.Sort(points, (a, b) => Compare(b, a, origin));
			}
			else
			{
				ArrayHelper.Sort(points, (a, b) => Compare(a, b, origin));
			}

			// Thanks to https://stackoverflow.com/a/6989383
			int Compare(Vector2 a, Vector2 b, Vector2 centre)
			{
				if (a.x - centre.x >= 0 && b.x - centre.x < 0)
					return 1;
				if (a.x - centre.x < 0 && b.x - centre.x >= 0)
					return -1;
				if (a.x - centre.x == 0 && b.x - centre.x == 0)
				{
					if (a.y - centre.y >= 0 || b.y - centre.y >= 0)
					{
						return (a.y > b.y) ? 1 : -1;
					}
					return (b.y > a.y) ? 1 : -1;
				}

				// Compute the cross product of vectors (centre -> a) x (centre -> b)
				float det = (a.x - centre.x) * (b.y - centre.y) - (b.x - centre.x) * (a.y - centre.y);
				if (det < 0)
				{
					return 1;
				}
				if (det > 0)
				{
					return -1;
				}

				// Points a and b are on the same line from the centre, so check which is closer to the centre
				float sqrDstA = (a.x - centre.x) * (a.x - centre.x) + (a.y - centre.y) * (a.y - centre.y);
				float sqrDstB = (b.x - centre.x) * (b.x - centre.x) + (b.y - centre.y) * (b.y - centre.y);
				return (sqrDstA > sqrDstB) ? 1 : -1;
			}
		}
	}
}