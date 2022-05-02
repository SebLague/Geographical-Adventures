using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Locations")]
public class Locations : ScriptableObject
{
	[Header("Curated")]
	public List<CuratedCountry> countries;
	[Header("Settings")]
	public float cityWeight;
	public float populationWeight;
	public int minorCityPopulationThreshold;
	public float minorCityWeight;
	[Header("Data")]
	public CountryData countryData;
	public Texture2D[] flags;
	//[Header("Result")]
	//public Location[] debugLocations;

	[NaughtyAttributes.Button()]
	void DebugLocations()
	{
		CreateLocations();
	}

	public Location[] CreateLocations()
	{
		List<Location> locations = new List<Location>();

		foreach (var curatedCountry in countries)
		{
			Country country = FindCountry(curatedCountry.countryCode3);
			City[] cities = new City[curatedCountry.curatedCities.Count];
			for (int i = 0; i < cities.Length; i++)
			{
				City city = FindCity(country, curatedCountry.curatedCities[i].cityName);
				cities[i] = city;
				if (city == null)
				{
					Debug.LogError("Failed finding city: " + curatedCountry.curatedCities[i].cityName + ", " + country.name);
				}
			}

			int numMajorCities = 0;
			foreach (City city in cities)
			{
				if (city.populationMetro > minorCityPopulationThreshold)
				{
					numMajorCities++;
				}
			}


			for (int i = 0; i < cities.Length; i++)
			{
				City city = cities[i];
				CuratedCity curatedCity = curatedCountry.curatedCities[i];

				Location location = new Location();
				location.country = country;
				location.city = city;
				location.overrideCountryName = !string.IsNullOrEmpty(curatedCity.overrideCountryName);
				location.overridenCountryName = curatedCity.overrideCountryName;
				bool overrideFlag = !string.IsNullOrEmpty(curatedCity.overrideCountryFlagCode);

				location.flag = FindFlag(overrideFlag ? curatedCity.overrideCountryFlagCode : country.alpha2Code);
				if (city == null)
				{
					Debug.Log(curatedCity.cityName);
				}
				location.cityPointUnitSphere = GeoMaths.CoordinateToPoint(city.coordinate);

				if (country == null || city == null || location.flag == null)
				{
					Debug.Log("Skip: " + curatedCity.cityName + ", " + curatedCountry.countryName + " " + country.alpha2Code);
				}
				else
				{
					location.testName = country.name + ", " + city.name;

					double weight = (1.0 / Mathf.Max(1, numMajorCities));
					weight += numMajorCities * cityWeight + city.populationMetro / 5000000 * populationWeight;
					if (country.name != "Antarctica")
					{
						weight *= Mathf.Lerp(minorCityWeight, 1, Mathf.Max(1, city.populationMetro / (float)minorCityPopulationThreshold));
					}
					location.weight = (float)weight;
					locations.Add(location);
				}
			}
		}

		return locations.ToArray();

		// ---- Local functions ----
		Texture2D FindFlag(string code2)
		{
			for (int i = 0; i < flags.Length; i++)
			{
				if (flags[i].name.ToLower() == code2.ToLower())
				{
					return flags[i];
				}

			}
			return null;
		}

		City FindCity(Country country, string cityName)
		{
			if (country != null)
			{
				foreach (City city in country.cities)
				{
					if (city.name == cityName)
					{
						return city;
					}
				}
			}
			return null;
		}

		Country FindCountry(string code3)
		{
			for (int i = 0; i < countryData.Countries.Length; i++)
			{
				if (code3 == countryData.Countries[i].alpha3Code)
				{
					return countryData.Countries[i];
				}
			}
			Debug.Log("Could not find country: " + code3);
			return null;
		}
	}

}



[System.Serializable]
public struct CuratedCountry
{
	public string countryName;
	public string countryCode3;
	public List<CuratedCity> curatedCities;

	public CuratedCountry(string countryName, string countryCode3)
	{
		this.countryName = countryName;
		this.countryCode3 = countryCode3;
		curatedCities = new List<CuratedCity>();
	}
}

[System.Serializable]
public struct CuratedCity
{
	public string cityName;
	public string overrideCountryName;
	public string overrideCountryFlagCode;
}



[System.Serializable]
public struct Location
{
	public string testName;
	public Country country;
	public City city;
	public Texture2D flag;
	public bool overrideCountryName;
	public string overridenCountryName;
	public Vector3 cityPointUnitSphere;
	public float weight;

	public string GetCountryDisplayName(int preferredMaxLength = int.MaxValue)
	{
		if (overrideCountryName)
		{
			return overridenCountryName;
		}
		return country.GetPreferredDisplayName(preferredMaxLength);
	}

	public string GetCityDisplayName()
	{
		string cityName = city.name;
		// Add state to american cities
		if (city.inAmericanState)
		{
			if (cityName == "Washington, D.C.")
			{
				return cityName;
			}
			return cityName + ", " + city.americanStateName;
		}
		return cityName;
	}
}