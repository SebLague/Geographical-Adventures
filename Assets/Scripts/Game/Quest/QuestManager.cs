using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class QuestManager : MonoBehaviour
{
	public int numCountries = 5;
	public int countryPopulationBiasIterations = 10;

	public Canvas canvas;
	public GameObject uiHolder;
	public CanvasGroup targetUIHolder;
	public TargetUI targetUIPrefab;
	public TMP_Text resultUI;
	public GameObject cityMarkerPrefab;
	public WorldLookup worldLookup;
	GameObject cityMarker;

	const int maxCountryNameLength = 15;

	public TargetUI[] targetsUI;
	Player player;

	// Countries sorted by population (high to low)
	Country[] orderedCountries;
	Country[] allCountries;
	TargetInfo[] currentTargets;
	int selectedTargetIndex = -1;
	bool readyForTargetSelection;
	bool activated;



	void Start()
	{
		player = FindObjectOfType<Player>();
		allCountries = FindObjectOfType<CountryLoader>().GetCountries();

		var countryList = allCountries.Where(c => c.cities.Length > 0).ToList();
		countryList.Sort((a, b) => b.population.CompareTo(a.population));

		orderedCountries = countryList.ToArray();

		GenerateNewTargets();
		readyForTargetSelection = true;

		if (!activated)
		{
			uiHolder.SetActive(false);
		}
	}

	public void Activate()
	{
		uiHolder.SetActive(true);
		activated = true;
	}


	void Update()
	{
		if (!activated)
		{
			return;
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			//GenerateNewTargets();

		}

		if (readyForTargetSelection)
		{
			int index = -1;
			if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
			{
				index = 0;
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
			{
				index = 1;
			}
			else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
			{
				index = 2;
			}
			if (index >= 0)
			{
				for (int i = 0; i < 3; i++)
				{
					targetsUI[i].ResetColours();
				}
				targetsUI[index].Highlight();
				selectedTargetIndex = index;
			}
		}

		if (readyForTargetSelection && selectedTargetIndex >= 0)
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				StartCoroutine(DropPackage());
			}
		}
	}



	IEnumerator DropPackage()
	{

		Package package = player.DropPackage();
		// Wait until package has data about terrain height and which country it is over (async operation)
		yield return new WaitUntil(() => package.hasTerrainInfo);

		Vector3 packagePoint = package.transform.position.normalized * package.terrainInfo.height;
		int countryIndex = package.terrainInfo.countryIndex;

		readyForTargetSelection = false;

		TargetInfo target = currentTargets[selectedTargetIndex];
		City targetCity = target.country.cities[target.cityIndex];
		Vector3 targetPoint = CoordinateSystem.CoordinateToPoint(targetCity.coordinate, 1);
		//Debug.Log(targetPoint + "  " + targetCity.name);
		const float worldRadiusMetres = 6371 * 1000;
		float dstMetres = CoordinateSystem.DistanceBetweenPointsOnUnitSphere(packagePoint.normalized, targetPoint) * worldRadiusMetres;
		float dstKm = dstMetres / 1000;

		// Fade out
		yield return new WaitForSeconds(0.5f);

		yield return StartCoroutine(Fade(targetUIHolder, 0, 0.5f));

		string resultMessage = "";
		const float deliveryThresholdKm = 50;
		const float correctCountryFailThresholdKm = 600;
		const float wrongCountryFailThresholdKm = 100;
		if (dstKm < deliveryThresholdKm)
		{
			resultMessage = "Perfect delivery!";
		}
		else
		{
			bool inOcean = countryIndex < 0;

			int missDstKm = Mathf.CeilToInt(Mathf.Max(0, dstKm - deliveryThresholdKm));


			if (inOcean)
			{
				if (missDstKm < 30)
				{
					resultMessage = $"So close, only {DistanceString(missDstKm)} off target! Unfortunately the package landed in the ocean, which is not ideal.";
				}
				else
				{
					resultMessage = $"Disastrous delivery! The package was dropped in the ocean, {DistanceString(missDstKm)} away from the target.";
				}
			}
			else
			{
				Country landedCountry = allCountries[countryIndex];
				bool correctCountry = landedCountry == target.country;
				Debug.Log("target: " + target.countryName + " landed: " + landedCountry.nameOfficial + " dst: " + dstKm);

				if (correctCountry)
				{
					if (dstKm > correctCountryFailThresholdKm)
					{
						resultMessage = $"Delivery failed. The package was dropped {DistanceString(missDstKm)} off target.";
					}
					else
					{
						resultMessage = $"Close enough. The package was delivered just {DistanceString(missDstKm)} off target.";
					}

				}
				else
				{

					if (dstKm > wrongCountryFailThresholdKm)
					{
						resultMessage = $"Oh no! The package was intended for {GetCountryUIDisplayName(target.country)}, but was delivered to {GetCountryUIDisplayName(landedCountry)} instead.";
						resultMessage += System.Environment.NewLine;
						resultMessage += $"Your delivery was {DistanceString(missDstKm)} off target.";
					}
					else
					{
						resultMessage = $"Pretty close, but the package landed in {GetCountryUIDisplayName(landedCountry)}, instead of {GetCountryUIDisplayName(target.country)}.";
						resultMessage += System.Environment.NewLine;
						resultMessage += $"Your delivery was {DistanceString(missDstKm)} off target.";
					}
				}
			}

			//TerrainInfo cityMarkTerrainInfo 
			TerrainInfo targetTerrainInfo = worldLookup.GetTerrainInfoImmediate(targetCity.coordinate);
			cityMarker = Instantiate(cityMarkerPrefab);
			cityMarker.GetComponentInChildren<TMP_Text>().text = target.cityName;

			cityMarker.transform.position = targetPoint * targetTerrainInfo.height;
			cityMarker.transform.LookAt(-cityMarker.transform.position.normalized * 1000, player.transform.forward);
			cityMarker.transform.rotation = player.transform.rotation;
			cityMarker.transform.up = cityMarker.transform.position.normalized;
		}

		resultUI.gameObject.SetActive(true);
		resultUI.text = resultMessage;

		float resultDisplayTime = Time.time;
		float maxWaitTime = 8;
		yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Time.time - resultDisplayTime > maxWaitTime);
		resultUI.gameObject.SetActive(false);
		for (int i = 0; i < 3; i++)
		{
			targetsUI[i].ResetColours();
		}
		GenerateNewTargets();
		yield return StartCoroutine(Fade(targetUIHolder, 1, 0.5f));

		// Reset for next:
		if (cityMarker != null)
		{
			GameObject.Destroy(cityMarker);
		}
		readyForTargetSelection = true;
		selectedTargetIndex = -1;

	}


	IEnumerator Fade(CanvasGroup group, float targetAlpha, float duration)
	{
		float t = 0;
		float startAlpha = group.alpha;
		float fadeSpeed = 1 / duration;
		while (t < 1)
		{
			t += Time.deltaTime * fadeSpeed;
			targetUIHolder.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
			yield return null;
		}
	}

	string DistanceString(int dstKm)
	{
		string s = dstKm + " kilometre";
		if (dstKm > 1)
		{
			s += "s";
		}
		return s;
	}

	string GetCountryUIDisplayName(Country country)
	{
		const int maxLength = 14;
		string displayName = (country.nameOfficial.Length > maxLength) ? country.name : country.nameOfficial;
		if (displayName.Length > maxLength)
		{
			displayName = country.abbreviation;
		}
		return displayName;
	}



	void GenerateNewTargets()
	{
		var targets = PickTargets(3);
		for (int i = 0; i < targets.Length; i++)
		{
			string countryName = GetCountryUIDisplayName(targets[i].country);
			targetsUI[i].Set(countryName, targets[i].cityName, (i + 1).ToString());
		}
		currentTargets = targets;
	}


	TargetInfo[] PickTargets(int numCountriesToChoose)
	{
		System.Random prng = new System.Random();
		int[] chosenCountries = ChooseCountries(numCountriesToChoose, prng);
		TargetInfo[] targets = new TargetInfo[numCountriesToChoose];

		for (int i = 0; i < chosenCountries.Length; i++)
		{
			int countryIndex = chosenCountries[i];
			Country country = orderedCountries[countryIndex];
			// Only consider cities with higher population than threshold (unless that reduces number of cities to less than 2)
			const int populationThreshold = 500000;
			City[] filteredCities = country.cities.Where(city => city.population > populationThreshold).ToArray();
			filteredCities = (filteredCities.Length < 2) ? country.cities : filteredCities;
			int cityIndex = ChooseCityIndex(filteredCities, prng);

			string countryName = country.nameOfficial;
			string cityName = country.cities[cityIndex].name;
			targets[i] = new TargetInfo() { country = country, cityIndex = cityIndex, countryName = countryName, cityName = cityName };
		}
		return targets;
	}

	int[] ChooseCountries(int numCountriesToChoose, System.Random prng)
	{
		int[] allIndices = new int[orderedCountries.Length];
		for (int i = 0; i < allIndices.Length; i++)
		{
			allIndices[i] = i;
		}
		ShuffleArray(allIndices, prng);

		// Give adjustable weighting to countries with higher population
		// (countries sorted by pop high to low, so do this by increasing prob of low indices)
		for (int i = 0; i < countryPopulationBiasIterations; i++)
		{
			int countryIndex = prng.Next(0, numCountriesToChoose);
			int swapIndex = prng.Next(numCountriesToChoose, orderedCountries.Length);
			if (allIndices[countryIndex] > allIndices[swapIndex])
			{
				(allIndices[countryIndex], allIndices[swapIndex]) = (allIndices[swapIndex], allIndices[countryIndex]);
			}
		}

		int[] chosenCountries = new int[numCountriesToChoose];
		for (int i = 0; i < numCountriesToChoose; i++)
		{
			chosenCountries[i] = allIndices[i];
		}
		return chosenCountries;
	}

	// Todo: replace this nonsense with something better
	int ChooseCityIndex(City[] cities, System.Random prng)
	{
		double randomVal = prng.NextDouble();
		if (randomVal < 0.1)
		{
			return prng.Next(0, cities.Length);
		}
		else if (randomVal < 0.5f)
		{
			return ChooseCityIndexLinear(cities, prng);
		}
		else
		{
			return ChooseCityIndexSqr(cities, prng);
		}
	}

	int ChooseCityIndexLinear(City[] cities, System.Random prng)
	{
		double randomVal = prng.NextDouble();

		long totalSqrPopulation = cities.Sum(a => a.population);
		double runningSqrPopulationFraction = 0;

		for (int i = 0; i < cities.Length; i++)
		{
			double cityPopulationFraction = (cities[i].population) / (double)totalSqrPopulation;
			runningSqrPopulationFraction += cityPopulationFraction;
			if (randomVal <= runningSqrPopulationFraction)
			{
				return i;
			}
		}

		return 0;
	}

	int ChooseCityIndexSqr(City[] cities, System.Random prng)
	{
		double randomVal = prng.NextDouble();

		long totalSqrPopulation = cities.Sum(a => (long)a.population * (long)a.population);
		double runningSqrPopulationFraction = 0;

		for (int i = 0; i < cities.Length; i++)
		{
			double cityPopulationFraction = ((long)cities[i].population * (long)cities[i].population) / (double)totalSqrPopulation;
			runningSqrPopulationFraction += cityPopulationFraction;
			if (randomVal <= runningSqrPopulationFraction)
			{
				return i;
			}
		}

		return 0;
	}

	public static void ShuffleArray<T>(T[] array, System.Random prng)
	{
		int elementsRemainingToShuffle = array.Length;
		int randomIndex = 0;

		while (elementsRemainingToShuffle > 1)
		{
			// Choose a random element from array
			randomIndex = prng.Next(0, elementsRemainingToShuffle);

			// Swap the randomly chosen element with the last unshuffled element in the array
			elementsRemainingToShuffle--;
			(array[randomIndex], array[elementsRemainingToShuffle]) = (array[elementsRemainingToShuffle], array[randomIndex]);
		}
	}

	public struct TargetInfo
	{
		public Country country;
		public string countryName;
		public int cityIndex;
		public string cityName;
	}
}
