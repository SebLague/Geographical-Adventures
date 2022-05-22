using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Some miscellaneous maths. Maybe one day these functions will find a proper home?

namespace Seb
{
	public static partial class Maths
	{

		/// <summary>
		/// Returns the greatest common divisor of A and B
		/// </summary>
		public static int GreatestCommonDivisor(int a, int b)
		{
			// Thanks to https://stackoverflow.com/a/41766138
			while (a != 0 && b != 0)
			{
				if (a > b)
					a %= b;
				else
					b %= a;
			}

			return a | b;
		}
	}
}
