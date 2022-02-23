using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

public class CityReader
{

	JsonTextReader reader;

	// Get all cities, sorted by population (highest to lowest)
	public City[] ReadCities(TextAsset cityFile)
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

		cities.Sort((a, b) => b.population.CompareTo(a.population));


		return cities.ToArray();

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
						cityInfo.population = (int)reader.ReadAsInt32();
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

	string[] GetCSV(string line)
	{
		List<string> values = new List<string>();

		string current = "";
		char quoteMark = '\"';
		char comma = ',';

		bool insideQuote = false;

		for (int i = 0; i < line.Length; i++)
		{
			char currentChar = line[i];


			if (line[i] == comma && !insideQuote)
			{
				values.Add(current);
				current = "";
			}
			else if (currentChar == quoteMark)
			{
				insideQuote = !insideQuote;
			}
			else
			{
				current += currentChar;
			}
		}

		return values.ToArray();
	}
}