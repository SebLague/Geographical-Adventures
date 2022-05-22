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

		Seb.Sorting.SortByScores(rankedNames, scores);
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
