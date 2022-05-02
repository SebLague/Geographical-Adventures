using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainGeneration
{
	public abstract class Generator : MonoBehaviour
	{
		public enum StartupMode { DoNothing, Generate, Load }
		public StartupMode startupMode;

		// Generation state
		public bool isGenerating { get; private set; }
		public bool generationComplete { get; private set; }

		protected virtual void Start()
		{
			switch (startupMode)
			{
				case StartupMode.DoNothing:
					break;
				case StartupMode.Generate:
					StartGenerating();
					break;
				case StartupMode.Load:
					Load();
					break;
			}
		}

		protected void NotifyGenerationStarted()
		{
			isGenerating = true;
			generationComplete = false;
		}

		protected void NotifyGenerationComplete()
		{
			isGenerating = false;
			generationComplete = true;
		}

		public abstract void StartGenerating();

		public abstract void Save();

		public abstract void Load();

		protected virtual string SavePath
		{
			get
			{
				return FileHelper.MakePath("Assets", "Data", "Terrain");
			}
		}
	}
}