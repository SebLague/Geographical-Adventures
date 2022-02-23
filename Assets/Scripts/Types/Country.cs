using UnityEngine;

[System.Serializable]
public class Country
{
	public string nameOfficial;
	public string name;
	public string abbreviation;

	public string continent;
	public string alpha3Code;
	public int population;

	// sorted by population (highest to lowest)
	public City[] cities;
	public Shape shape;
}

[System.Serializable]
public class City
{
	public string name;
	public int population;
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
	// All paths are closed loops: first and last point are the same
	public Path[] paths;
}
[System.Serializable]
public struct Path
{
	public Coordinate[] points;
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

