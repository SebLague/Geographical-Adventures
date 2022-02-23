using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	[Header("Time state")]
	[Range(0, 1)]
	public float dayT;
	[Range(0, 1)]
	public float monthT;
	[Range(0, 1)]
	public float yearT;

	[Header("Debug")]
	public bool geocentric;


	void Update()
	{
		if (animate && Application.isPlaying)
		{
			dayT += 1 / (dayDurationMinutes * 60) * Time.deltaTime;
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


}
