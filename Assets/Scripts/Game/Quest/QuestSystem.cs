using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoGame.Quest
{
	public class QuestSystem : MonoBehaviour
	{
		public float timedModeDurationSeconds = 15 * 60;

		public const int numActiveQuests = 3;
		public const float perfectRadius = 75;
		public const float goodRadius = 300;
		public const float okRadius = 1000;



		public bool useStartSettings;
		public bool cycleThroughStartSettings;
		public int startSettingsIndex;
		public StartSettings[] allStartSettings;


		public int perfectBoostTimeAdd = 15;
		public float resultMessageDuration = 6.5f;
		public float pickupMessageDuration = 5;



		// ---- Inspector variables ----
		[Header("References")]
		public CountryData countryData;
		public Player player;
		public GameCamera gameCamera;
		public HotAirBalloon hotAirBalloonPrefab;
		public TerrainGeneration.TerrainHeightSettings heightSettings;
		public QuestUI questUI;
		public QuestCreator questCreator;
		public WorldLookup worldLookup;
		public MessageUI messageUI;
		public CityMarker cityMarkerPrefab;
		public SolarSystem.SolarSystemManager solarSystem;

		// ---- Private variables -----
		Quest[] activeQuests;
		bool displayingDeliveryResults;
		List<DeliveryResult> deliveryResults;
		float timeSinceGameStart;

		const string timedScoreSaveKey = "timedModeBestScore";
		int previousPersonalBestTimedScore;
		bool inEndlessMode;

		void Awake()
		{
		}

		void Start()
		{
			if (useStartSettings)
			{
				const string startSettingsIndexSaveKey = "startSettingsIndex";
				if (cycleThroughStartSettings)
				{
					int lastIndex = PlayerPrefs.GetInt(startSettingsIndexSaveKey, defaultValue: -1);
					startSettingsIndex = (lastIndex + 1) % allStartSettings.Length;
					PlayerPrefs.SetInt(startSettingsIndexSaveKey, startSettingsIndex);
					PlayerPrefs.Save();

				}
				StartSettings startSettings = allStartSettings[startSettingsIndex];
				player.SetStartPos(startSettings.playerStartPoint);
				gameCamera.InitView();
				solarSystem.SetTimes(startSettings.dayT, startSettings.monthT, startSettings.yearT);
			}

			previousPersonalBestTimedScore = GetPersonalBestTimedScore();

			deliveryResults = new List<DeliveryResult>();
			activeQuests = new Quest[numActiveQuests];

			GameController.Instance.onGameStarted -= OnGameStarted;
			GameController.Instance.onGameStarted += OnGameStarted;



		}

		void OnGameStarted()
		{
			for (int i = 0; i < activeQuests.Length; i++)
			{
				SetNewActiveQuest(i);
			}

			questUI.AnimateFirstSet();
		}


		void Update()
		{

			if (GameController.IsState(GameState.Playing) || GameController.IsState(GameState.ViewingMap))
			{
				timeSinceGameStart += Time.deltaTime;
				if (!inEndlessMode)
				{
					if (TimeRemaining <= 0)
					{
						GameOver();
					}
				}
				HandleDebugInput();
			}
		}

		void GameOver()
		{
			int score = CalculateScore();
			int personalBest = GetPersonalBestTimedScore();
			if (score > personalBest)
			{
				personalBest = score;
				SavePersonalBestTimedScore(personalBest);
			}


			GameController.GameOver();
		}

		public void ContinueInEndlessMode()
		{
			GameController.SwitchToEndlessMode();
			inEndlessMode = true;
		}

		public bool InEndlessMode
		{
			get
			{
				return inEndlessMode;
			}
		}

		public void SavePersonalBestTimedScore(int score)
		{
			PlayerPrefs.SetInt(timedScoreSaveKey, score);
			PlayerPrefs.Save();
		}

		public int GetPersonalBestTimedScore()
		{
			int personalBest = PlayerPrefs.GetInt(timedScoreSaveKey, 0);
			return personalBest;
		}

		public int PreviousPersonalBestTimedScore
		{
			get
			{
				return previousPersonalBestTimedScore;
			}
		}

		void HandleDebugInput()
		{

			// Debug
			if (GameController.InDevMode)
			{
				if (Input.GetKeyDown(KeyCode.Alpha8))
				{
					OnPickup(0);
				}
				if (Input.GetKeyDown(KeyCode.Alpha9))
				{
					OnPickup(1);
				}
				if (Input.GetKeyDown(KeyCode.Alpha0))
				{
					OnPickup(2);
				}
			}
		}

		public void TryDropPackage()
		{
			if (GameController.IsState(GameState.Playing))
			{
				if (displayingDeliveryResults)
				{
					return;
				}

				// Choose nearest delivery target to player's current position
				int activeDeliverQuestIndex = -1;
				float bestDst = float.MaxValue;
				for (int i = 0; i < activeQuests.Length; i++)
				{
					if (activeQuests[i].hasPickedUp && !activeQuests[i].completed)
					{
						Vector3 cityPosUnitSphere = GeoMaths.CoordinateToPoint(activeQuests[i].deliverLocation.city.coordinate);
						float dst = GeoMaths.DistanceBetweenPointsOnUnitSphere(player.transform.position.normalized, cityPosUnitSphere);
						if (dst < bestDst)
						{
							bestDst = dst;
							activeDeliverQuestIndex = i;
						}
					}
				}

				// Drop package
				if (activeDeliverQuestIndex >= 0)
				{
					Package package = player.DropPackage();
					StartCoroutine(HandleDelivery(package, activeDeliverQuestIndex));
				}
				else
				{
					string message = "You don't have any packages to deliver!";
					//message = $"The package will land in Denmark instead of France, but it was still pretty close. Only 3000 kilometers off target.\nSome more text";
					messageUI.ShowMessage(message, 1);
					Debug.Log("You don't have any packages to deliver!");
				}
			}
		}

		IEnumerator HandleDelivery(Package package, int questIndex)
		{
			displayingDeliveryResults = true;
			float startTime = Time.time;

			// Mark quest as completed
			Quest completedQuest = activeQuests[questIndex];
			completedQuest.completed = true;
			questUI.MarkCompleted(questIndex);

			// Lookup height of terrain at target city so can display marker at that point
			float targetCityTerrainHeight = -1;
			worldLookup.GetTerrainInfoAsync(completedQuest.deliverLocation.city.coordinate, (TerrainInfo info) => targetCityTerrainHeight = info.height);

			// Wait until package has data about terrain height and which country it is over (async operation)
			// Also wait until city height lookup is complete
			yield return new WaitUntil(() => package.hasTerrainInfo && targetCityTerrainHeight >= 0);
			// Wait a little bit before revealing the result
			const float delayBeforeShowingResults = 1;
			yield return new WaitForSeconds(delayBeforeShowingResults - (Time.time - startTime));

			// Spawn city marker
			Vector3 cityPointUnitSphere = completedQuest.deliverLocation.cityPointUnitSphere;
			Vector3 cityPoint = cityPointUnitSphere * targetCityTerrainHeight;
			CityMarker cityMarker = Instantiate(cityMarkerPrefab, parent: transform);
			cityMarker.Init(cityPoint, gameCamera.transform.position);


			// Calculate results
			DeliveryResult result = new DeliveryResult();
			result.targetCountry = completedQuest.deliverLocation.country;
			result.targetCity = completedQuest.deliverLocation.city;
			result.targetCityPoint = cityPoint;
			result.packagedLandedPoint = package.transform.position.normalized * package.terrainInfo.height;
			result.landedInOcean = package.terrainInfo.inOcean;
			result.countryPackageLandedIn = (package.terrainInfo.inOcean) ? null : countryData.Countries[package.terrainInfo.countryIndex];
			float unitSphereDistance = GeoMaths.DistanceBetweenPointsOnUnitSphere(result.packagedLandedPoint.normalized, cityPointUnitSphere);
			result.distanceKM = unitSphereDistance * GeoMaths.EarthRadiusKM;
			result.landedInCorrectCountry = result.countryPackageLandedIn == completedQuest.deliverLocation.country;
			deliveryResults.Add(result);

			string resultMessage = CreateResultMessage(result);
			messageUI.ShowMessage(resultMessage, resultMessageDuration);
			Debug.Log(resultMessage);

			AddBoost(result);

			yield return new WaitForSeconds(1);

			// Start new quest
			SetNewActiveQuest(questIndex, animate: true);
			displayingDeliveryResults = false;
		}

		void SetNewActiveQuest(int index, bool animate = false)
		{
			// Create quest and setup UI
			Quest quest = questCreator.CreateQuest();
			activeQuests[index] = quest;
			questUI.SetTarget(index, quest.pickupLocation, isPickup: true, animate: animate);

			// Spawn hot air ballooon
			Vector3 pickupPos = quest.pickupLocation.cityPointUnitSphere * heightSettings.worldRadius;
			HotAirBalloon hotAirBalloon = Instantiate(hotAirBalloonPrefab, parent: transform);
			hotAirBalloon.Init(player.transform, pickupPos, () => OnPickup(index), quest.pickupLocation.flag);
		}

		// Called when player picks up package from a hot air balloon
		void OnPickup(int questIndex)
		{
			Quest quest = activeQuests[questIndex];
			quest.hasPickedUp = true;
			questUI.SetTarget(questIndex, quest.deliverLocation, isPickup: false, animate: true);

			string countryName = quest.deliverLocation.GetCountryDisplayName();
			string message = $"You collected a package! It's marked for delivery to {quest.deliverLocation.city.name}, {countryName}";
			messageUI.ShowMessage(message, pickupMessageDuration);
			Debug.Log(message);
			//questUI.SetTarget(questIndex, quest.deliverCountry, quest.deliverCity, isPickup: false);
		}

		void AddBoost(DeliveryResult result)
		{
			float boostAdd = 0;
			if (result.distanceKM <= perfectRadius)
			{
				boostAdd = perfectBoostTimeAdd;
			}
			else
			{
				float t = Mathf.InverseLerp(perfectRadius, okRadius, result.distanceKM);
				float maxNonPerfectBoostIncrease = perfectBoostTimeAdd * 0.75f;
				boostAdd = Mathf.Lerp(maxNonPerfectBoostIncrease, 0, 1 - Mathf.Pow(1 - t, 3));
				if (result.landedInCorrectCountry)
				{
					boostAdd += 2;
					boostAdd = Mathf.Min(boostAdd, maxNonPerfectBoostIncrease);
				}
			}
			player.AddBoost(boostAdd);
		}

		string CreateResultMessage(DeliveryResult result)
		{
			string dstString = DistanceString(result.distanceKM);
			string cityName = result.targetCity.name;
			string targetCountryName = result.targetCountry.GetPreferredDisplayName(15);
			string landedInCountryName = "ocean";
			if (result.countryPackageLandedIn != null)
			{
				landedInCountryName = result.countryPackageLandedIn.GetPreferredDisplayName(15);
			}
			// Create result message

			// Perfect delivery
			if (result.distanceKM <= perfectRadius)
			{
				if (result.distanceKM > 20)
				{
					if (result.landedInOcean)
					{
						return $"Perfect delivery! The package will land just {dstString} from the city centre (although in the water, unfortunately).";
					}
					else if (!result.landedInCorrectCountry)
					{
						return $"Perfect delivery! The package will land just {dstString} from the city centre (but will have to be brought across from {landedInCountryName} to {targetCountryName}).";
					}
				}

				return $"Perfect delivery! The package will land just {dstString} from the city centre.";
			}
			// Good delivery
			else if (result.distanceKM <= goodRadius)
			{
				if (result.landedInCorrectCountry)
				{
					return $"Good enough! The package will land {dstString} from the city.";
				}
				else if (result.landedInOcean)
				{
					return $"Good enough! The package will land {dstString} from the city, but will have to be fished out of the water.";
				}
				else
				{
					return $"Good enough! The package will land {dstString} from the city (but will have to be transported from {landedInCountryName} to {targetCountryName}).";
				}
			}
			else if (result.distanceKM <= okRadius)
			{
				if (result.landedInCorrectCountry)
				{
					return $"An okay attempt. The package will land {dstString} from the city.";
				}
				else if (result.landedInOcean)
				{
					return $"An okay attempt. The package will land {dstString} from the city, and will have to be fished out of the water.";
				}
				else
				{
					return $"An okay attempt. The package will land {dstString} from the city (and will have to be transported from {landedInCountryName} to {targetCountryName}).";
				}
			}
			else
			{
				if (result.landedInCorrectCountry)
				{
					return $"Oh no! The package will land {dstString} away from the city. On the bright side, it's in the right country at least!";
				}
				else if (result.landedInOcean)
				{
					return $"Oh no! The package will land in the water, {dstString} away from the city.";
				}
				else
				{
					return $"Oh no! The package will land {dstString} away from the city, and in {landedInCountryName} instead of {targetCountryName}.";
				}
			}

			string DistanceString(float dstKm)
			{
				int dstRounded = Mathf.CeilToInt(dstKm);
				string s = dstRounded + " kilometre";
				if (dstRounded != 1)
				{
					s += "s";
				}
				return s;
			}
		}

		public float TimeSinceGameStart
		{
			get
			{
				return timeSinceGameStart;
			}
		}

		public float TimeRemaining
		{
			get
			{
				if (inEndlessMode)
				{
					return float.PositiveInfinity;
				}
				return Mathf.Max(0, timedModeDurationSeconds - TimeSinceGameStart);

			}
		}

		public int CalculateScore()
		{
			float distanceScore = 0;
			float streakScore = 0;
			float bonusScore = 0;
			if (deliveryResults != null)
			{
				int perfectStreakLength = 0;
				int goodStreakLength = 0;
				foreach (var result in deliveryResults)
				{
					// Distance score
					distanceScore += Mathf.Pow(1 - Mathf.Min(1, result.distanceKM / 3000), 5) * 100;

					// Bonus score
					if (result.landedInCorrectCountry)
					{
						bonusScore += 10;
					}

					// Streak score
					if (result.distanceKM <= perfectRadius)
					{
						perfectStreakLength++;
					}
					else
					{
						perfectStreakLength = 0;
						if (result.distanceKM <= goodRadius)
						{
							goodStreakLength++;
						}
						else
						{
							goodStreakLength = 0;
						}
					}


					if (perfectStreakLength > 1)
					{
						streakScore += 30 + (perfectStreakLength - 1) * 20;
					}
					else if (goodStreakLength > 1)
					{
						streakScore += (goodStreakLength - 1) * 10;
					}
				}
			}
			float score = distanceScore + streakScore + bonusScore;
			return Mathf.CeilToInt(score);
		}


		public DeliveryResult[] GetResults()
		{
			if (deliveryResults == null)
			{
				return new DeliveryResult[0];
			}
			return deliveryResults.ToArray();
		}


		[System.Serializable]
		public struct DeliveryResult
		{
			//public enum ResultType {Perfect, Good, }
			public Country targetCountry;
			public City targetCity;
			public Country countryPackageLandedIn;
			public float distanceKM;
			public Vector3 packagedLandedPoint;
			public Vector3 targetCityPoint;
			public bool landedInOcean;
			public bool landedInCorrectCountry;
		}

		[System.Serializable]
		public struct StartSettings
		{
			public Player.PlayerStartPoint playerStartPoint;
			[Header("Time Settings")]
			[Range(0, 1)] public float dayT;
			[Range(0, 1)] public float monthT;
			[Range(0, 1)] public float yearT;
		}

	}
}