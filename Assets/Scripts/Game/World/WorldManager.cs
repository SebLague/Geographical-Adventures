using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration;

public class WorldManager : MonoBehaviour
{

	public enum LoadMode { Quick, PrintProgress }

	[Header("Settings")]
	public LoadMode editorLoadMode;
	public LoadMode buildLoadMode;

	[Header("References")]
	public Player player;
	public QuestManager questManager;
	public LoadScreen loadScreen;
	public TerrainHeightSettings heightSettings;
	public TerrainHeightProcessor heightProcessor;
	public WorldMeshGenerator meshGenerator;
	public CountryLoader countryLoader;
	public CountryHighlighting countryHighlighter;
	public CityLights cityLights;
	public WorldLookup worldLookup;
	public Light sunLight;
	public AtmosphereEffect atmosphereEffect;
	public GameObject placeholder;

	static WorldManager instance;


	void Start()
	{
		LoadMode loadMode = (Application.isEditor) ? editorLoadMode : buildLoadMode;

		if (loadMode == LoadMode.Quick)
		{
			LoadQuick();
		}
		else
		{
			StartCoroutine(LoadRoutine());
		}
	}

	public LoadTask[] GetTasks()
	{
		List<LoadTask> tasks = new List<LoadTask>();

		AddTask(() => heightProcessor.ProcessHeightMap(), "Processing Height Map");
		AddTask(() => countryLoader.Load(), "Loading Country Data");
		AddTask(() => countryHighlighter.Init(countryLoader.NumCountries), "Initializing Country Highlighter");
		AddTask(() => meshGenerator.Init(), "Loading Terrain mesh");
		AddTask(() => cityLights.Init(heightProcessor.processedHeightMap, meshGenerator.GetAllBounds(), sunLight), "Creating City Lights");
		AddTask(() => worldLookup.Init(heightProcessor.processedHeightMap), "Initializing World Lookup");

		void AddTask(System.Action task, string name)
		{
			tasks.Add(new LoadTask(task, name));
		}

		return tasks.ToArray();
	}



	void LoadQuick()
	{
		var loadTimer = System.Diagnostics.Stopwatch.StartNew();
		OnLoadStart();

		LoadTask[] tasks = GetTasks();

		foreach (LoadTask task in tasks)
		{
			task.Execute(null, false);
		}

		OnLoadFinish();
		Debug.Log("Load duration: " + loadTimer.ElapsedMilliseconds);
	}



	IEnumerator LoadRoutine()
	{
		var loadTimer = System.Diagnostics.Stopwatch.StartNew();
		OnLoadStart();

		LoadTask[] tasks = GetTasks();

		foreach (LoadTask task in tasks)
		{
			task.Execute(loadScreen, true);
			yield return null;
		}

		loadScreen.Log("Loading Complete");
		yield return new WaitForSeconds(0.5f);
		OnLoadFinish();

		Debug.Log("Load duration: " + loadTimer.ElapsedMilliseconds);
	}


	void OnLoadStart()
	{
		loadScreen.gameObject.SetActive(true);
		loadScreen.Init();
		placeholder.SetActive(false);
	}

	void OnLoadFinish()
	{
		// Release any memory from stuff no longer needed after all generation is finished
		meshGenerator.OnGenerationStageFinished();
		heightProcessor.Release();
		Resources.UnloadUnusedAssets();

		// Start game
		player.Activate();
		questManager.Activate();
		loadScreen.gameObject.SetActive(false);
	}

	public class LoadTask
	{
		public System.Action task;
		public string taskName;

		public LoadTask(System.Action task, string name)
		{
			this.task = task;
			this.taskName = name;
		}

		public void Execute(LoadScreen loadScreen, bool log)
		{
			if (log)
			{
				loadScreen.Log(taskName);
			}
			task.Invoke();
		}
	}


	void OnDestroy()
	{
	}

	public static float worldRadius
	{
		get
		{
			return Instance.heightSettings.worldRadius;
		}
	}

	static WorldManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<WorldManager>();
			}
			return instance;
		}
	}

}
