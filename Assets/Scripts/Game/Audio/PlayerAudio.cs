using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{


	[Range(0, 0.5f)] public float pitchChangeStrength;
	public float fadeInDuration;
	public Player player;
	public AudioSource audioSource;

	float targetVolume;

	void Start()
	{
		targetVolume = audioSource.volume;
	}


	void Update()
	{
		// Fade in
		float fadeInT = (fadeInDuration > 0) ? Time.timeSinceLevelLoad / fadeInDuration : 1;
		audioSource.volume = Mathf.Lerp(0, targetVolume, fadeInT);

		// Change pitch of engine sfx slightly when descending/ascending to try make it seem a bit more dynamic...
		// TODO: make better
		float planeClimbT = Mathf.InverseLerp(player.maxPitchAngle, -player.maxPitchAngle, player.currentPitchAngle);
		audioSource.pitch = Mathf.Lerp(1 - pitchChangeStrength, 1 + pitchChangeStrength, planeClimbT);
	}
}
