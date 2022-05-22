using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Some array-related helpers. Sorting, shuffling, etc.

namespace Seb
{

	public static class ArrayHelper
	{

		/// <summary>
		/// Sorts the given array based on their corresponding 'score' values.
		/// Note: the scores array will also be sorted in the process.
		/// </summary>
		public static void SortByScores<ItemType, ScoreType>(ItemType[] items, ScoreType[] scores, bool highToLow = true) where ScoreType : System.IComparable
		{
			Sorting.SortByScores(items, scores, highToLow);
		}

		/// <summary>
		/// Sorts the given array based on a given comparison function.
		/// </summary>
		public static void Sort<T>(T[] items, System.Func<T, T, int> comparisonFunction)
		{
			Sorting.Sort(items, comparisonFunction);
		}

		/// <summary>
		/// Shuffles the elements of the given array
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
		/// Adds all items in the source array onto the end of the target array (resizes the target array to fit the new items).
		/// </summary>
		public static void AppendArray<T>(ref T[] targetArray, T[] sourceArray)
		{
			int originalLength = targetArray.Length;
			System.Array.Resize(ref targetArray, targetArray.Length + sourceArray.Length);
			System.Array.Copy(sourceArray, 0, targetArray, originalLength, sourceArray.Length);
		}

		/// <summary>
		/// Creates an integer array containing values from 0 to length-1 in order.
		/// </summary>
		public static int[] CreateIndexArray(int length)
		{
			int[] array = new int[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = i;
			}

			return array;
		}
	}
}
