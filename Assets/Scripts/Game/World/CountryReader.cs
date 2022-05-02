using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class CountryReader
{
	JsonTextReader reader;

	public Country[] ReadCountries(TextAsset countryFile)
	{

		reader = new JsonTextReader(new System.IO.StringReader(countryFile.text));

		List<Country> countryList = new List<Country>();


		ReadAhead(5);

		while (reader.Read())
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				countryList.Add(ReadCountry());
			}
			else
			{
				break;
			}

		}

		reader.Close();

		//countryList.Sort((a, b) => (a.name.CompareTo(b.name)));
		return countryList.ToArray();
	}

	Country ReadCountry()
	{
		Country country = new Country();

		int startDepth = reader.Depth;

		// Read country name and code from properties array
		while (reader.TokenType != JsonToken.EndArray)
		{
			reader.Read();
			if (reader.TokenType == JsonToken.PropertyName)
			{
				switch ((string)reader.Value)
				{
					case "ADMIN":
						country.nameOfficial = reader.ReadAsString();
						break;
					case "NAME":
						country.name = reader.ReadAsString();
						break;
					case "NAME_LONG":
						country.name_long = reader.ReadAsString();
						break;
					case "NAME_SORT":
						country.name_sort = reader.ReadAsString();
						break;
					case "2_LETTER_CODE":
						country.alpha2Code = reader.ReadAsString();
						break;
					case "ADM0_A3":
						country.alpha3Code = reader.ReadAsString();
						break;
					case "ABBREV":
						country.abbreviation = reader.ReadAsString();
						break;
					case "CONTINENT":
						country.continent = reader.ReadAsString();
						break;
					case "POP_EST":
						country.population = (int)reader.ReadAsDouble();
						break;
				}
			}
		}

		List<Polygon> polygons = new List<Polygon>();
		List<Path> pathsInCurrentPolygon = new List<Path>();
		List<Coordinate> coordList = new List<Coordinate>();

		// Read shape data from geometry array
		while (reader.Read() && reader.Depth > startDepth)
		{
			if (reader.TokenType == JsonToken.Float)
			{
				double x = (double)reader.Value;
				double y = (double)reader.ReadAsDouble();
				Coordinate coord = new Coordinate((float)x * Mathf.Deg2Rad, (float)y * Mathf.Deg2Rad);
				coordList.Add(coord);
				ReadAhead(2);
			}

			if (reader.TokenType == JsonToken.EndArray)
			{
				//	Debug.Log("Finished path " + pointsList[0] + "  -> " + pointsList[pointsList.Count - 1]);
				coordList.Add(coordList[0]); // duplicate start point at end for conveniece in some other code
				Path path = new Path() { points = coordList.ToArray() };
				pathsInCurrentPolygon.Add(path);

				coordList.Clear();
				ReadAhead(1);
			}
			if (reader.TokenType == JsonToken.EndArray)
			{
				//Debug.Log("Finished polygon (" + pathsInCurrentPolygon.Count + " paths)");
				Polygon polygon = new Polygon() { paths = pathsInCurrentPolygon.ToArray() };
				polygons.Add(polygon);

				pathsInCurrentPolygon.Clear();
				ReadAhead(1);
			}
		}

		country.shape = new Shape() { polygons = polygons.ToArray() };

		return country;
	}

	void ReadAhead(int n)
	{
		for (int i = 0; i < n; i++)
		{
			reader.Read();
		}
	}

}
