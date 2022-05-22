using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GeoGame.Quest
{
	public class QuestCreator : MonoBehaviour
	{
		[Header("Settings")]
		public bool randomizeSeed;
		public int seed;
		public bool tryHaveFirstBalloonInView;

		[Header("References")]
		public Locations curatedLocations;
		public Player player;
		public Camera gameCamera;

		// ---- Private variables ----
		bool isInitialized;
		System.Random prng;

		float[] allLocationWeights;
		Location[] allLocations;

		List<Quest> questHistory;




		public Quest CreateQuest()
		{
			Init();
			Quest quest = new Quest();

			// Ensure not picking a location that's too close to active/recent quests, just to prevent things getting too clustered
			List<Vector3> pointsToAvoid = new List<Vector3>();
			pointsToAvoid.Add(player.transform.position.normalized);
			for (int i = 0; i < questHistory.Count; i++)
			{
				Quest oldQuest = questHistory[i];
				bool isRecentQuest = (questHistory.Count - i) <= 5;
				if (isRecentQuest || !oldQuest.hasPickedUp)
				{
					pointsToAvoid.Add(oldQuest.pickupLocation.cityPointUnitSphere);
				}
				if (isRecentQuest || !oldQuest.completed)
				{
					pointsToAvoid.Add(oldQuest.deliverLocation.cityPointUnitSphere);
				}

			}

			quest.pickupLocation = PickRandomLocation(pointsToAvoid, avoidDegrees: 10);
			pointsToAvoid.Add(quest.pickupLocation.cityPointUnitSphere);
			quest.deliverLocation = PickRandomLocation(pointsToAvoid, avoidDegrees: 10);

			if (questHistory.Count == 0 && tryHaveFirstBalloonInView)
			{

				var test = GetLocationsInPlayerView();
				int index = new System.Random(prng.Next()).Next(0, test.Length);
				Debug.Log(test.Length);
				if (test.Length > 0)
				{
					quest.pickupLocation = test[index];
				}
			}


			questHistory.Add(quest);
			return quest;
		}


		Location[] GetLocationsInPlayerView()
		{
			Debug.DrawRay(player.transform.position, player.transform.forward * 10, Color.green, 100);
			List<Location> candidates = new List<Location>();
			var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(gameCamera);

			foreach (var location in allLocations)
			{
				float angleBetween = Vector3.Angle(player.transform.position.normalized, location.cityPointUnitSphere);
				if (angleBetween > 10 && angleBetween < 30)
				{
					if (GeometryUtility.TestPlanesAABB(frustumPlanes, new Bounds(location.cityPointUnitSphere * player.transform.position.magnitude, Vector3.one)))
					{
						Debug.DrawRay(location.cityPointUnitSphere * 150, location.cityPointUnitSphere * 152, Color.red, 100);
						candidates.Add(location);
					}
				}
			}
			return candidates.ToArray();
		}

		Location PickRandomLocation(List<Vector3> avoidPoints, float avoidDegrees)
		{
			int safety = 0;
			while (true)
			{
				Location randomLocation = PickRandomLocation();
				if (ValidateLocation(randomLocation, avoidPoints, avoidDegrees))
				{
					return randomLocation;
				}

				safety++;
				if (safety > 100)
				{
					Debug.LogError("Couldn't find location within reasonable number of iterations. Something has gone horribly wrong!");
					break;
				}
			}
			// This should never happen
			return PickRandomLocation();
		}

		bool ValidateLocation(Location location, List<Vector3> avoidPoints, float avoidDegrees)
		{
			float closestAngleDegrees = 180;
			foreach (Vector3 avoid in avoidPoints)
			{
				float angle = Vector3.Angle(location.cityPointUnitSphere, avoid.normalized);
				closestAngleDegrees = Mathf.Min(angle, closestAngleDegrees);
			}

			return closestAngleDegrees > avoidDegrees;
		}

		Location PickRandomLocation()
		{
			int index = Seb.Maths.WeightedRandomIndex(prng, allLocationWeights);
			for (int i = 0; i < allLocationWeights.Length; i++)
			{
				//Debug.Log(allLocationWeights[i]);
			}
			return allLocations[index];
		}


		void Init()
		{
			if (!isInitialized)
			{
				if (randomizeSeed)
				{
					seed = new System.Random().Next();
				}
				prng = new System.Random(seed);
				questHistory = new List<Quest>();
				isInitialized = true;

				allLocations = curatedLocations.CreateLocations();
				allLocationWeights = new float[allLocations.Length];
				for (int i = 0; i < allLocations.Length; i++)
				{
					allLocationWeights[i] = allLocations[i].weight;
				}
			}
		}

	}



	[System.Serializable]
	public class WeightDebug
	{
		public string name;
		public float weight;
		public string percent;
	}


}