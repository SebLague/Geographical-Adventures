using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class CountryLoader : MonoBehaviour
{
	public bool autoRunOnAwake = true;
	public TextAsset countryFile;
	public TextAsset cityFile;
	public TextAsset capitalsFile;

	[SerializeField] Country[] countries;
	bool loaded;

	[ContextMenu("Run")]
	void Awake()
	{
		if (autoRunOnAwake)
		{
			Load();
		}
	}

	public Country[] GetCountries()
	{
		Load();
		return countries;
	}

	public int NumCountries
	{
		get
		{
			Load();
			return countries.Length;
		}
	}

	public void Load()
	{
		if (!loaded || !Application.isPlaying)
		{
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
			loaded = true;
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

	/*
	[ContextMenu("Create Country Info Template")]
	void CreateCountryInfoJsonTemplate()
	{
		Load();

		AllCountryInfo allCountriesInfo = new AllCountryInfo();
		allCountriesInfo.countryInfo = new CountryInfo[countries.Length];
		for (int i = 0; i < countries.Length; i++)
		{
			CountryInfo info = new CountryInfo() { countryName = countries[i].name, countryCode = countries[i].alpha3Code };
			allCountriesInfo.countryInfo[i] = info;
		}

		string jsonText = JsonUtility.ToJson(allCountriesInfo, prettyPrint: true);

		System.IO.StreamWriter writer = new System.IO.StreamWriter("./Assets/CountryInfoTemplate.json");
		writer.Write(jsonText);
		Debug.Log(jsonText);
		writer.Dispose();
		Debug.Log("Template file saved to Assets folder");
	}
	*/
}
