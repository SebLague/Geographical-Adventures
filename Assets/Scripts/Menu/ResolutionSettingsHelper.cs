using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResolutionSettingsHelper
{

	public static Vector2Int GetRatio(int width, int height)
	{
		int gcd = Seb.Maths.GreatestCommonDivisor(width, height);
		int aspectW = width / gcd;
		int aspectH = height / gcd;
		Vector2Int aspect = new Vector2Int(aspectW, aspectH);
		return aspect;
	}

	public static Vector2Int GetRatio(Resolution res)
	{
		return GetRatio(res.width, res.height);
	}

	public static string ResolutionToName(Resolution res)
	{
		Vector2Int aspectRatio = GetRatio(res);
		// Change 8:5 to 16:10 since that's typically used (presumably to compare easier to 16:9)
		if (aspectRatio.x == 8 && aspectRatio.y == 5)
		{
			aspectRatio *= 2;
		}
		return $"{res.width} x {res.height} ({aspectRatio.x}:{aspectRatio.y})";
	}

}
