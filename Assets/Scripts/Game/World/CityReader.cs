using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

public class CityReader
{

	JsonTextReader reader;

	// Get all cities, sorted by population (highest to lowest)
	public City[] ReadCities(TextAsset cityFile, TextAsset capitalsFile)
	{
		// Create lookup of whether city is a capital city or not.
		Capital[] capitals = ReadCapitals(capitalsFile);
		Dictionary<string, HashSet<string>> capitalCitiesByCountryCode = new Dictionary<string, HashSet<string>>();
		foreach (Capital capital in capitals)
		{
			if (!capitalCitiesByCountryCode.ContainsKey(capital.countryCode3))
			{
				capitalCitiesByCountryCode.Add(capital.countryCode3, new HashSet<string>());
			}
			foreach (string capitalCityName in capital.capitalNames)
			{
				capitalCitiesByCountryCode[capital.countryCode3].Add(capitalCityName);
			}
		}

		// Read cities
		List<City> cities = Read(cityFile);

		// Set capitals
		foreach (City city in cities)
		{
			if (capitalCitiesByCountryCode.ContainsKey(city.countryAlpha3Code))
			{
				if (capitalCitiesByCountryCode[city.countryAlpha3Code].Contains(city.name))
				{
					city.isCapital = true;
					//Debug.Log("Capital of " + city.countryName + " : " + city.name);
				}
			}
			else
			{
				//Debug.Log("No capital listed for: " + city.countryName + " " + city.countryAlpha3Code);
			}
		}
		cities.Sort((a, b) => CityCompare(a, b));

		return cities.ToArray();

		int CityCompare(City a, City b)
		{
			int popCompare = (a.populationMetro > b.populationMetro) ? -1 : 1;
			int capitalCompare = 0;
			if (a.isCapital)
			{
				capitalCompare -= 2;
			}
			if (b.isCapital)
			{
				capitalCompare += 2;
			}
			return popCompare + capitalCompare;
		}

	}

	List<City> Read(TextAsset cityFile)
	{
		reader = new JsonTextReader(new System.IO.StringReader(cityFile.text));
		reader.Read();

		List<City> cities = new List<City>();
		while (reader.Read())
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				cities.Add(ReadCity());
			}

		}

		return cities;
	}

	City ReadCity()
	{
		City cityInfo = new City();

		int startDepth = reader.Depth;

		while (reader.Read() && reader.Depth > startDepth)
		{
			if (reader.TokenType == JsonToken.PropertyName)
			{

				switch ((string)reader.Value)
				{
					case "name":
						cityInfo.name = reader.ReadAsString();
						break;
					case "adm0name":
						cityInfo.countryName = reader.ReadAsString();
						break;
					case "adm0_a3":
						cityInfo.countryAlpha3Code = reader.ReadAsString();
						if (cityInfo.countryAlpha3Code == "USA")
						{
							cityInfo.inAmericanState = true;
						}
						break;
					case "adm1name":
						if (cityInfo.inAmericanState)
						{
							cityInfo.americanStateName = reader.ReadAsString();
						}
						break;
					case "pop_max":
						cityInfo.populationMetro = (int)reader.ReadAsInt32();
						break;
				}
			}

			if (reader.TokenType == JsonToken.StartArray)
			{
				float longitude = (float)reader.ReadAsDouble() * Mathf.Deg2Rad;
				float latitude = (float)reader.ReadAsDouble() * Mathf.Deg2Rad;
				cityInfo.coordinate = new Coordinate(longitude, latitude);
			}
		}

		return cityInfo;
	}


	Capital[] ReadCapitals(TextAsset capitalsFile)
	{
		var reader = new JsonTextReader(new System.IO.StringReader(capitalsFile.text));

		List<Capital> capitals = new List<Capital>();
		while (reader.Read())
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				Capital capital = ReadCapital();
				capitals.Add(capital);
			}
		}
		return capitals.ToArray();

		Capital ReadCapital()
		{
			Capital capital = new Capital();

			int startDepth = reader.Depth;

			while (reader.Read() && reader.Depth > startDepth)
			{

				if (reader.TokenType == JsonToken.PropertyName)
				{

					switch ((string)reader.Value)
					{
						case "common":
							if (string.IsNullOrEmpty(capital.countryName))
							{
								capital.countryName = reader.ReadAsString();
							}
							break;
						case "cca3":
							capital.countryCode3 = reader.ReadAsString();
							break;
						case "capital":
							List<string> capitalCityNames = new List<string>();
							while (reader.TokenType != JsonToken.EndArray)
							{
								if (reader.ValueType == typeof(string))
								{
									string capitalName = reader.Value as string;
									capitalCityNames.Add(capitalName);
								}
								reader.Read();
							}
							capital.capitalNames = capitalCityNames.ToArray();
							break;
					}

				}
			}
			return capital;
		}
	}

	struct Capital
	{
		public string countryName;
		public string countryCode3;
		public string[] capitalNames;
	}

}