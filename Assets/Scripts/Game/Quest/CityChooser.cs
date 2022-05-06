using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class CityChooser : MonoBehaviour
{

	public TextAsset capitalsFile;
	public CountryData countryData;

	public Region region;

	void Start()
	{
		ReadCapitals();
	}


	Capital[] ReadCapitals()
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

	[System.Serializable]
	public struct Region
	{
		public string regionName;
		public float regionWeight;
		public CuratedCountry[] countries;

	}

	[System.Serializable]
	public struct CuratedCountry
	{
		public string countryName;
		public CuratedCity[] curatedCities;
	}

	[System.Serializable]
	public struct CuratedCity
	{
		public string cityName;
		public string overrideCountryName;
		public string overrideCountryCode;
	}
}
