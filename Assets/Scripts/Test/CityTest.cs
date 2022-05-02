using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityTest : MonoBehaviour
{
	public Locations curatedLocations;
	public Transform testEarth;
	public float cityScale;
	public Material cityMat;

	public float sizeMin;
	public float sizeMax;

	int citySpawnCount;

	void Start()
	{

		Location[] locations = curatedLocations.CreateLocations();
		foreach (var location in locations)
		{
			SpawnCity(location.city);
		}

		Debug.Log("Num Cities Spawned: " + citySpawnCount);

	}


	void SpawnCity(City city)
	{
		GameObject cityDisplay = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		cityDisplay.transform.localScale = Vector3.one * cityScale * Mathf.Lerp(sizeMin, sizeMax, Mathf.InverseLerp(500000, 5000000, city.populationMetro));
		cityDisplay.transform.position = GeoMaths.CoordinateToPoint(city.coordinate) * testEarth.localScale.x * 0.5f;
		cityDisplay.transform.SetParent(testEarth, true);
		cityDisplay.name = city.name + ", " + city.countryName + " " + city.americanStateName + " pop = " + city.populationMetro;
		cityDisplay.GetComponent<MeshRenderer>().sharedMaterial = cityMat;
		citySpawnCount++;
	}

}
