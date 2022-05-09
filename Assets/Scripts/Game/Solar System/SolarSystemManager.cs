using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SolarSystem
{
	[ExecuteInEditMode]
	public class SolarSystemManager : MonoBehaviour
	{

		public bool animate;

		[Header("Durations")]
		// Allow flexible day/month/year durations since real timescales are a bit slow...
		public float dayDurationMinutes;
		public float monthDurationMinutes;
		public float yearDurationMinutes;

		[Header("References")]
		public Sun sun;
		public EarthOrbit earth;
		public Moon moon;
		public StarRenderer stars;
		public Transform player;

		[Header("Time state")]
		[Range(0, 1)]
		public float dayT;
		[Range(0, 1)]
		public float monthT;
		[Range(0, 1)]
		public float yearT;


		public float fastForwardDayDuration;
		bool fastForwarding;
		float oldPlayerT;
		float fastForwardTargetTime;
		bool fastForwardApproachingTargetTime;

		[Header("Debug")]
		public bool geocentric;


		void Update()
		{

			if (animate && Application.isPlaying && GameController.IsState(GameState.Playing))
			{
				float daySpeed = 1 / (dayDurationMinutes * 60);
				if (fastForwarding)
				{
					HandleFastforwarding(out daySpeed);
				}

				dayT += daySpeed * Time.deltaTime;
				monthT += 1 / (monthDurationMinutes * 60) * Time.deltaTime;
				yearT += 1 / (yearDurationMinutes * 60) * Time.deltaTime;

				dayT %= 1;
				monthT %= 1;
				yearT %= 1;
			}

			earth?.UpdateOrbit(yearT, dayT, geocentric);
			sun?.UpdateOrbit(earth, geocentric);
			moon?.UpdateOrbit(monthT, earth, geocentric);
			stars?.UpdateFixedStars(earth, geocentric);

		}

		public void FastForward(bool toDaytime)
		{
			fastForwardTargetTime = (toDaytime) ? 1 : -1;
			fastForwarding = true;
			fastForwardApproachingTargetTime = false;
			oldPlayerT = CalculatePlayerDayT();
		}

		public void SetTimes(float dayT, float monthT, float yearT)
		{
			this.dayT = dayT;
			this.monthT = monthT;
			this.yearT = yearT;
		}


		void HandleFastforwarding(out float daySpeed)
		{
			daySpeed = 1 / (fastForwardDayDuration * 60);

			float playerT = CalculatePlayerDayT();
			if (DstToTargetTime(playerT, fastForwardTargetTime) < DstToTargetTime(oldPlayerT, fastForwardTargetTime))
			{
				fastForwardApproachingTargetTime = true;
			}
			else
			{
				if (fastForwardApproachingTargetTime)
				{
					fastForwarding = false;
				}
			}
			oldPlayerT = playerT;
		}

		// -1 at midnight to 1 at midday
		float CalculatePlayerDayT()
		{
			return Vector3.Dot(player.position.normalized, -sun.transform.forward);
		}

		// Value between -1 and +1. Can only move forward. Wraps around from +1 to -1.
		float DstToTargetTime(float fromT, float targetT)
		{
			return Mathf.Abs(targetT - fromT);
		}

	}


}