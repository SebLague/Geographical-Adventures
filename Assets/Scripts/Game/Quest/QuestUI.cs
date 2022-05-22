using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoGame.Quest
{
	public class QuestUI : MonoBehaviour
	{

		public int maxCountryNameLength = 18;

		[Header("References")]
		public RectTransform countryTargetsRect;
		public TargetUI countryTargetPrefab;
		public TMPro.TMP_Text timer;
		public QuestSystem questSystem;

		TargetUI[] countryTargets;
		Coroutine activeAnimation;

		int timerSecondsOld = int.MaxValue;
		int timerMinutesOld = int.MaxValue;

		void Awake()
		{
			Seb.TransformHelper.DestroyAllChildren(countryTargetsRect.transform);
			countryTargets = new TargetUI[QuestSystem.numActiveQuests];

			for (int i = 0; i < countryTargets.Length; i++)
			{
				countryTargets[i] = Instantiate(countryTargetPrefab, parent: countryTargetsRect.transform);
			}
		}

		void Update()
		{

			UpdateTimer();
		}

		void UpdateTimer()
		{
			timer.gameObject.SetActive(!questSystem.InEndlessMode);
			if (!questSystem.InEndlessMode)
			{
				int seconds = (int)(questSystem.TimeRemaining % 60);
				int minutes = (int)(questSystem.TimeRemaining / 60) % 60;
				// Update timer (but avoid allocating every frame)
				if (seconds != timerSecondsOld || minutes != timerMinutesOld)
				{
					timer.text = $"{minutes:00}:{seconds:00}";
					timerSecondsOld = seconds;
					timerMinutesOld = minutes;
				}
			}
		}


		public void MarkCompleted(int index)
		{
			countryTargets[index].MarkCompleted();
		}

		public void SetTarget(int index, Location location, bool isPickup, bool animate = false)
		{
			string countryCode = location.country.alpha3Code;
			//string countryName = location.GetCountryDisplayName(maxCountryNameLength);
			string countryName = Localization.LocalizationManager.Localize($"countryCode3.{countryCode}");
			if (countryName.Length > maxCountryNameLength)
			{
				countryName = location.country.abbreviation;
			}
			string cityName = location.GetCityDisplayName();

			if (animate)
			{
				Coroutine prevAnimation = activeAnimation;
				activeAnimation = StartCoroutine(Animate(index, countryName, cityName, isPickup, prevAnimation));
			}
			else
			{
				countryTargets[index].Set(countryName, cityName, isPickup);
				UpdateTargetsLayout();
			}
		}

		// Animate the first targets 
		public void AnimateFirstSet()
		{
			activeAnimation = StartCoroutine(AnimateFirst());
		}

		IEnumerator AnimateFirst()
		{
			RectTransform[] targets = new RectTransform[countryTargets.Length];
			Vector2[] startPoints = new Vector2[targets.Length];
			Vector2[] endPoints = new Vector2[targets.Length];
			float[] animTimes = new float[targets.Length];

			for (int i = 0; i < targets.Length; i++)
			{
				targets[i] = countryTargets[i].GetComponent<RectTransform>();
				startPoints[i] = (Vector2)targets[i].localPosition + Vector2.up * targets[i].sizeDelta.y;
				endPoints[i] = targets[i].localPosition;
				targets[i].localPosition = startPoints[i];
				animTimes[i] = 0 - i * 0.2f;
			}
			//float t = 0;

			while (true)
			{
				bool done = true;
				for (int i = 0; i < targets.Length; i++)
				{

					//Debug.Log(t);
					animTimes[i] += Time.deltaTime;
					done &= animTimes[i] >= 1;

					targets[i].localPosition = Vector3.Lerp(startPoints[i], endPoints[i], Seb.Ease.Cubic.Out(animTimes[i]));

				}
				if (GameController.IsState(GameState.ViewingMap))
				{
					yield return new WaitWhile(() => GameController.IsState(GameState.ViewingMap));
				}
				yield return null;
				if (done)
				{
					break;
				}

			}
		}

		// What a mess! Please clean this up future-me
		IEnumerator Animate(int index, string countryName, string cityName, bool isPickup, Coroutine oldAnim)
		{
			if (oldAnim != null)
			{
				yield return oldAnim;
			}
			yield return new WaitWhile(() => GameController.IsState(GameState.ViewingMap));

			RectTransform oldRect = countryTargets[index].RectTransform;

			// Record starting positions of all targets
			Vector2[] originalPos = new Vector2[countryTargets.Length];
			for (int i = 0; i < countryTargets.Length; i++)
			{
				originalPos[i] = countryTargets[i].RectTransform.localPosition;
			}

			// Create new target
			countryTargets[index] = Instantiate(countryTargetPrefab, parent: countryTargetsRect.transform);
			countryTargets[index].Set(countryName, cityName, isPickup);

			// Calculate layout with new target
			Vector2[] targetPos = CalculateTargetLayout();
			// Move new target off screen
			Vector2 newTargetStartPos = targetPos[index] + Vector2.up * countryTargets[index].RectTransform.sizeDelta.y;
			countryTargets[index].RectTransform.localPosition = newTargetStartPos;

			const float durationDisappear = 0.5f;
			const float durationPause = 0.25f;
			const float durationAppear = 0.75f;
			const float totalDuration = durationDisappear + durationPause + durationAppear;

			StartCoroutine(AnimateLayout(index, targetPos, totalDuration));
			float t = 0;
			// Animate old target disappearing off screen
			while (t < 1)
			{
				t += Time.deltaTime / durationDisappear;
				oldRect.localPosition = new Vector2(oldRect.localPosition.x, 0 + Seb.Ease.Cubic.In(t) * oldRect.sizeDelta.y);
				if (GameController.IsState(GameState.ViewingMap))
				{
					yield return new WaitWhile(() => GameController.IsState(GameState.ViewingMap));
				}
				yield return null;
			}
			yield return new WaitForSeconds(durationPause);
			t = 0;

			// Animate new target appearing on screen, and other targets moving to fit
			while (t < 1)
			{
				t += Time.deltaTime / durationAppear;
				for (int i = 0; i < countryTargets.Length; i++)
				{
					if (i != index)
					{
						//countryTargets[i].RectTransform.localPosition = Vector2.Lerp(originalPos[i], targetPos[i], Maths.Ease.Cubic.InOut(t));
					}
				}
				countryTargets[index].RectTransform.localPosition = Vector2.Lerp(newTargetStartPos, targetPos[index], Seb.Ease.Cubic.Out(t));
				if (GameController.IsState(GameState.ViewingMap))
				{
					yield return new WaitWhile(() => GameController.IsState(GameState.ViewingMap));
				}
				yield return null;
			}

			Destroy(oldRect.gameObject);

			//oldRect.gameObject.SetActive(false);
		}

		IEnumerator AnimateLayout(int dontAnimateIndex, Vector2[] targetLocalPositions, float duration)
		{
			Vector2[] originalPos = new Vector2[countryTargets.Length];
			for (int i = 0; i < countryTargets.Length; i++)
			{
				originalPos[i] = countryTargets[i].RectTransform.localPosition;
			}

			float t = 0;

			while (t < 1)
			{
				t += Time.deltaTime / duration;
				for (int i = 0; i < countryTargets.Length; i++)
				{
					if (i != dontAnimateIndex)
					{
						countryTargets[i].RectTransform.localPosition = Vector2.Lerp(originalPos[i], targetLocalPositions[i], Seb.Ease.Cubic.InOut(t));
					}
				}
				yield return null;
			}
		}


		void UpdateTargetsLayout()
		{
			float containerWidth = RectTransformSize(countryTargetsRect).x;
			float freeSpace = containerWidth;
			for (int i = 0; i < countryTargets.Length; i++)
			{
				freeSpace -= countryTargets[i].RectTransform.sizeDelta.x;
			}
			//Debug.Log("free space " + freeSpace + "   " + containerWidth);
			float spacing = freeSpace / (countryTargets.Length + 1);
			float x = -containerWidth / 2;
			for (int i = 0; i < countryTargets.Length; i++)
			{
				float rectWidth = countryTargets[i].RectTransform.sizeDelta.x;
				x += rectWidth / 2;
				x += spacing;
				//Debug.Log(x);
				countryTargets[i].RectTransform.localPosition = new Vector2(x, 0);
				x += rectWidth / 2;
			}
		}


		Vector2[] CalculateTargetLayout()
		{
			Vector2[] localPositions = new Vector2[countryTargets.Length];

			float containerWidth = RectTransformSize(countryTargetsRect).x;
			float freeSpace = containerWidth;
			for (int i = 0; i < countryTargets.Length; i++)
			{
				freeSpace -= countryTargets[i].RectTransform.sizeDelta.x;
			}
			//Debug.Log("free space " + freeSpace + "   " + containerWidth);
			float spacing = freeSpace / (countryTargets.Length + 1);
			float x = -containerWidth / 2;
			for (int i = 0; i < countryTargets.Length; i++)
			{
				float rectWidth = countryTargets[i].RectTransform.sizeDelta.x;
				x += rectWidth / 2;
				x += spacing;
				//Debug.Log(x);
				//countryTargets[i].RectTransform.localPosition = new Vector2(x, 0);
				localPositions[i] = new Vector2(x, 0);
				x += rectWidth / 2;
			}
			return localPositions;
		}

		Vector2 RectTransformSize(RectTransform rect)
		{
			Vector3[] corners = new Vector3[4];
			rect.GetLocalCorners(corners);
			return new Vector2(corners[2].x - corners[1].x, corners[1].y - corners[0].y);

		}

	}
}