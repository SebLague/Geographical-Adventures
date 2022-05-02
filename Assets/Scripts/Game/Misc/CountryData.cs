using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Data/Country Data")]
public class CountryData : ScriptableObject
{
	[Header("Source")]
	[SerializeField] TextAsset countryFile;
	[SerializeField] TextAsset cityFile;
	[SerializeField] TextAsset capitalsFile;

	[Header("Data")]
	[SerializeField] Country[] countries;


	public Country[] Countries
	{
		get
		{
			return countries;
		}
	}

	public int NumCountries
	{
		get
		{
			return countries.Length;
		}
	}

	public void Load()
	{
		Debug.Log("Load");
		if (countryFile != null)
		{
			CountryReader countryReader = new CountryReader();
			countries = countryReader.ReadCountries(countryFile);
		}

		if (cityFile != null)
		{
			CityReader cityReader = new CityReader();
			City[] allCities = cityReader.ReadCities(cityFile, capitalsFile);
			AddCitiesToCountries(allCities);
		}
	}

	void AddCitiesToCountries(City[] allCities)
	{
		// Can happen due to mismatching country codes in files
		int numCountriesWithoutCity = 0;
		int numCitiesWithoutCountry = 0;

		HashSet<string> legitateCountryCodes = new HashSet<string>(countries.Select(x => x.alpha3Code));

		var citiesByCountry = new Dictionary<string, List<City>>();

		foreach (City city in allCities)
		{
			string countryCode = city.countryAlpha3Code;
			if (legitateCountryCodes.Contains(countryCode))
			{

				if (!citiesByCountry.ContainsKey(countryCode))
				{
					citiesByCountry.Add(countryCode, new List<City>());
				}
				citiesByCountry[countryCode].Add(city);
			}
			else
			{
				numCitiesWithoutCountry++;
			}
		}


		foreach (Country country in countries)
		{
			List<City> citiesInCountry = new List<City>();
			if (citiesByCountry.TryGetValue(country.alpha3Code, out citiesInCountry))
			{
				country.cities = citiesInCountry.ToArray();
			}
			else
			{
				country.cities = new City[0];
				numCountriesWithoutCity++;
			}
		}

		//Debug.Log("Num countries without a city: " + numCountriesWithoutCity + " Num cities without a country: " + numCitiesWithoutCountry);
	}

}
