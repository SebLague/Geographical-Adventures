using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Music : MonoBehaviour
{

	public AudioClip[] tracks;
	public AudioSource source;
	public bool shuffleTracksOnStart;
	int[] playOrder;
	int nextTrackIndex;
	float nextTrackStartTime;

	static Music instance;


	void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
			Init();
		}
		// Force single instance
		else
		{
			Destroy(gameObject);
		}
	}


	void Init()
	{
		playOrder = Seb.ArrayHelper.CreateIndexArray(tracks.Length);
		if (shuffleTracksOnStart)
		{
			Seb.ArrayHelper.ShuffleArray(playOrder, new System.Random());
		}
		nextTrackIndex = 0;
	}

	void Update()
	{
		if (Time.time > nextTrackStartTime)
		{
			if (tracks[nextTrackIndex] != null)
			{
				source.Stop();
				source.clip = tracks[nextTrackIndex];
				source.Play();
				nextTrackStartTime = Time.time + source.clip.length;
				nextTrackIndex = (nextTrackIndex + 1) % tracks.Length;
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	static void ResetStaticValues()
	{
		// Handle if domain reloading is disabled in player settings
		instance = null;
	}

}
