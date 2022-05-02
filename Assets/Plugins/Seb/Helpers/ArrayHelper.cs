using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.Helpers
{

	public static class ArrayHelper
	{
		public static void AppendArray<T>(ref T[] array, T[] arrayToAppend)
		{
			int originalLength = array.Length;
			System.Array.Resize(ref array, array.Length + arrayToAppend.Length);
			System.Array.Copy(arrayToAppend, 0, array, originalLength, arrayToAppend.Length);
		}

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
