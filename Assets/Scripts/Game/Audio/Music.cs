using System.Collections;
using UnityEngine;
using System.Linq;

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
		tracks = tracks.Where(track => track != null).ToArray();
		if (shuffleTracksOnStart)
		{
			Seb.ArrayHelper.ShuffleArray(tracks, new System.Random());
		}
		nextTrackIndex = 0;
		if (tracks.Length == 0) {
			Destroy(gameObject);
		}
	}

	void Update()
	{
		if (Time.time > nextTrackStartTime)
		{
			StartCoroutine(PlayNextTrack());
		}
	}

	IEnumerator PlayNextTrack()
	{
		if (tracks[nextTrackIndex] != null)
		{
			source.Stop();
			source.clip = tracks[nextTrackIndex];
			source.Play();
			nextTrackStartTime = Time.time + source.clip.length;
			nextTrackIndex = (nextTrackIndex + 1) % tracks.Length;
		}
		yield return null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	static void ResetStaticValues()
	{
		// Handle if domain reloading is disabled in player settings
		instance = null;
	}

}
