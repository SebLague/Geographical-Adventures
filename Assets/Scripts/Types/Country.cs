using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Country
{
	public string name;
	public string name_long; // alternate
	public string name_sort; // alternate 2 (not sure what the _sort attribute means?)

	public string nameOfficial;

	public string abbreviation;

	public string continent;
	public string alpha2Code;
	public string alpha3Code;
	public int population;

	// sorted by population (highest to lowest, with caveat: capital cities appear first)
	public City[] cities;
	public Shape shape;

	public string GetPreferredDisplayName(int maxDesiredLength = int.MaxValue, bool debug = false)
	{
		const int abbreviatedIndex = 4;
		string[] rankedNames = { name, name_long, nameOfficial, name_sort, abbreviation };
		int[] scores = new int[rankedNames.Length];
		for (int i = 0; i < scores.Length; i++)
		{
			string currentName = rankedNames[i];

			// If all else is equal, take the first element from the array
			int penalty = i;
			if (currentName.Length > 0)
			{
				// Prefer shorter names
				penalty += currentName.Length;
				// Penalty if over max desired length
				if (currentName.Length > maxDesiredLength)
				{
					penalty += 1000;
				}

				// Penalty if name is abbreviated
				// Note: most names are appreviated with a '.', but there are some exceptions like 'Isle of Man" becomes 'IoMan'.
				// Also note: in many cases the abbreviated form is just the same as the normal name, hence why using index instead of string comparison
				if (i == abbreviatedIndex || currentName.Contains("."))
				{
					penalty += 100;
					// If abbreviated, prefer longer abbreviations (for example, "Bosnia and Herz." is clearer than "B.H.")
					penalty -= currentName.Length * 2;
				}
			}
			else
			{
				// Name is empty
				penalty = int.MaxValue;
			}
			scores[i] = -penalty;
		}

		Maths.Sorting.SortByScores(rankedNames, scores);
		if (debug)
		{
			for (int i = 0; i < rankedNames.Length; i++)
			{
				Debug.Log($"{rankedNames[i]}  (score = {scores[i]})");
			}
		}
		return rankedNames[0];
	}
}

[System.Serializable]
public class City
{
	public string name;
	public bool isCapital;
	public int populationMetro;
	public Coordinate coordinate;

	public string countryName;
	public string countryAlpha3Code;
	public bool inAmericanState;
	public string americanStateName;
}

[System.Serializable]
public struct Shape
{
	public Polygon[] polygons;
}

[System.Serializable]
public struct Polygon
{
	// First path is the outline of the polygon, any subsequent paths are holes to be cut out
	public Path[] paths;

	public Polygon(Path[] paths)
	{
		this.paths = paths;
	}

	public int NumHoles
	{
		get
		{
			return paths.Length - 1;
		}
	}

	public Path Outline
	{
		get
		{
			return paths[0];
		}
	}

	public Path[] Holes
	{
		get
		{
			Path[] holes = new Path[NumHoles];
			for (int i = 0; i < holes.Length; i++)
			{
				holes[i] = paths[i + 1];
			}
			return holes;
		}
	}
}

[System.Serializable]
public struct Path
{
	public Coordinate[] points;

	public Path(Coordinate[] points)
	{
		this.points = points;
	}

	public int NumPoints
	{
		get
		{
			return points.Length;
		}
	}

	// Convert coordinates to Vector2s.
	// Optionally don't include last point (for cases where first and last points have been defined as the same)
	public Vector2[] GetPointsAsVector2(bool includeLastPoint = true)
	{
		int numPoints = (includeLastPoint) ? points.Length : points.Length - 1;
		Vector2[] pointsVec = new Vector2[numPoints];
		for (int i = 0; i < numPoints; i++)
		{
			pointsVec[i] = points[i].ToVector2();
		}
		return pointsVec;
	}

	public static Vector2[] GetPointsAsVector2(Coordinate[] coords)
	{
		Vector2[] pointsVec = new Vector2[coords.Length];
		for (int i = 0; i < pointsVec.Length; i++)
		{
			pointsVec[i] = coords[i].ToVector2();
		}
		return pointsVec;
	}
}

[System.Serializable]
public class AllCountryInfo
{
	public CountryInfo[] countryInfo;
}


[System.Serializable]
public struct CountryInfo
{
	public string countryName;
	public string countryCode;
	public string[] exports;
	public string[] facts;
}

