using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GeoGame.Quest;

public class StatsMenu : Menu
{
	public Color extraInfoCol;
	public Color perfectCol;
	public Color goodCol;
	public Color okCol;
	public Color badCol;

	public Player player;
	public QuestSystem questSystem;
	public TMP_Text labels;
	public TMP_Text values;

	public GameObject gameOverHolder;
	public GameObject pausedHolder;

	public Button quitToMainMenuButton;
	public Button continueInEndlessModeButton;

	protected override void Awake()
	{
		base.Awake();
		quitToMainMenuButton.onClick.AddListener(GameController.ExitToMainMenu);
		continueInEndlessModeButton.onClick.AddListener(SwitchToEndlessMode);
	}

	void SwitchToEndlessMode()
	{
		questSystem.ContinueInEndlessMode();
		CloseMenu();
	}

	void Update()
	{
		//OnMenuOpened();
	}

	protected override void OnMenuOpened()
	{
		Refresh();
	}

	[NaughtyAttributes.Button()]
	void Refresh()
	{
		gameOverHolder.SetActive(GameController.IsState(GameState.GameOver));
		pausedHolder.SetActive(!GameController.IsState(GameState.GameOver));

		labels.text = "";
		values.text = "";

		// Travel distance
		float numTimesAroundGlobe = player.distanceTravelledKM / GeoMaths.EarthCircumferenceKM;
		string dstTravelledString = DistanceString((int)player.distanceTravelledKM);
		string timesAroundGlobeString = $"({numTimesAroundGlobe:0.0} times around the globe)";

		//distanceTravelled.text = dstTravelledString;
		Add("Distance travelled", CreateString(dstTravelledString, timesAroundGlobeString));
		Add("Timer", GetTimeString(questSystem.TimeSinceGameStart));
		AddSpace();
		//timer.text = GetTimeString(Time.timeSinceLevelLoad);

		var results = questSystem.GetResults();

		int numTotal = results.Length;
		int numPerfect = 0;
		int numGood = 0;
		int numOk = 0;
		int numBad = 0;
		float totalDistanceError = 0;
		QuestSystem.DeliveryResult bestResult = default;
		bestResult.distanceKM = float.MaxValue;
		QuestSystem.DeliveryResult worstResult = default;
		worstResult.distanceKM = float.MinValue;

		foreach (var result in results)
		{
			if (result.distanceKM <= QuestSystem.perfectRadius)
			{
				numPerfect++;
			}
			else if (result.distanceKM < QuestSystem.goodRadius)
			{
				numGood++;
			}
			else if (result.distanceKM < QuestSystem.okRadius)
			{
				numOk++;
			}
			else
			{
				numBad++;
			}

			if (result.distanceKM < bestResult.distanceKM)
			{
				bestResult = result;
			}
			if (result.distanceKM > worstResult.distanceKM)
			{
				worstResult = result;
			}
			totalDistanceError += result.distanceKM;
			//if (result.distanceKM < GeoGame.Quest.QuestSystem.p)
		}

		Add("Packages delivered", results.Length.ToString());
		Add(CreateString(SetColour("Perfect deliveries", perfectCol), $"(< {QuestSystem.perfectRadius} km)"), DeliveryResultString(numPerfect, numTotal));
		Add(CreateString(SetColour("Good deliveries", goodCol), $"(< {QuestSystem.goodRadius} km)"), DeliveryResultString(numGood, numTotal));
		Add(CreateString(SetColour("OK deliveries", okCol), $"(< {QuestSystem.okRadius} km)"), DeliveryResultString(numOk, numTotal));
		Add(SetColour("Bad deliveries", badCol), DeliveryResultString(numBad, numTotal));

		string bestDeliveryString = DistanceString(0);
		string worstDeliveryString = DistanceString(0);
		if (numTotal > 0)
		{
			bestDeliveryString = ResultInfoString(bestResult);
			worstDeliveryString = ResultInfoString(worstResult);
		}
		AddSpace();
		Add("Best delivery", bestDeliveryString);
		Add("Worst delivery", worstDeliveryString);

		string averageErrorString = (numTotal == 0) ? DistanceString(0) : DistanceString(totalDistanceError / numTotal);
		Add("Average error", averageErrorString);

		// Score
		if (!questSystem.InEndlessMode)
		{
			AddSpace();
			int score = questSystem.CalculateScore();
			int prevPersonalBest = questSystem.PreviousPersonalBestTimedScore;
			bool isNewBestScore = score > prevPersonalBest;
			Add(MakeBold(GameController.IsState(GameState.GameOver) ? "Final score" : "Current score"), score.ToString());
			Add(MakeBold(isNewBestScore ? "Previous personal best" : "Personal best"), prevPersonalBest.ToString());
		}
	}

	string ResultInfoString(QuestSystem.DeliveryResult result)
	{
		string resultString = DistanceString(result.distanceKM);
		resultString += FormatExtraInfo($" ({result.targetCity.name}, {result.targetCountry.GetPreferredDisplayName(15)})");
		return resultString;
	}

	string DeliveryResultString(int numInCategory, int total)
	{
		string resultString = numInCategory.ToString();
		if (total > 0)
		{
			int percent = Mathf.RoundToInt(numInCategory / (float)total * 100);
			resultString = CreateString(resultString, $"({percent}%)");
		}
		return resultString;
	}

	string SetColour(string text, Color colour)
	{
		string colHex = ColorUtility.ToHtmlStringRGB(colour);
		return $"<color=#{colHex}>{text}</color>";
	}

	string MakeBold(string text)
	{
		return $"<b>{text}</b>";
	}

	//string Combine(params string[] strings) {

	//}

	string CreateString(string mainString, string infoString)
	{
		return $"{mainString} {FormatExtraInfo(infoString)}";
	}

	string FormatExtraInfo(string extraInfo)
	{
		string colHex = ColorUtility.ToHtmlStringRGB(extraInfoCol);
		return $"<size={75}%><color=#{colHex}>{extraInfo}</color></size>";
	}

	string AddRichTextToString(string original, string stringToAdd, int sizePercent, Color col)
	{
		string colHex = ColorUtility.ToHtmlStringRGB(col);
		return $"{original}<size={sizePercent}%><color=#{colHex}>{stringToAdd}</color></size>";
	}

	void AddSpace(int sizePercent = 100)
	{
		string lineBreak = $"<line-height={sizePercent}%>\n</line-height>";
		labels.text += lineBreak;
		values.text += lineBreak;
	}

	void Add(string label, string value)
	{
		if (!string.IsNullOrEmpty(labels.text))
		{
			labels.text += "\n";
		}
		if (!string.IsNullOrEmpty(values.text))
		{
			values.text += "\n";
		}

		labels.text += label;
		values.text += value;
	}

	public static string DistanceString(float dstKm)
	{
		return (int)dstKm + " km";
	}

	public static string GetTimeString(float time)
	{
		//float time = Time.timeSinceLevelLoad;
		int seconds = (int)(time % 60);
		int minutes = (int)(time / 60) % 60;
		int hours = (int)(time / 60 / 60);
		string timeString = "";
		if (hours > 0)
		{
			timeString += hours + ((hours == 1) ? " hour, " : " hours, ");
		}
		if (minutes > 0 || hours > 0)
		{
			timeString += minutes + ((minutes == 1) ? " minute, " : " minutes, ");
		}
		timeString += seconds + ((seconds == 1) ? " second" : " seconds");
		return timeString;

	}
}
