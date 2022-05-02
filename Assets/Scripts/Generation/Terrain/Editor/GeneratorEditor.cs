using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TerrainGeneration.CustomEditors
{
	[CustomEditor(typeof(Generator), editorForChildClasses: true)]
	public class GeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			Generator generator = target as Generator;

			using (new EditorGUI.DisabledScope(!Application.isPlaying))
			{
				DrawButtons(generator);
			}
		}

		void DrawButtons(Generator generator)
		{
			using (new EditorGUI.DisabledScope(generator.isGenerating || generator.generationComplete))
			{
				if (GUILayout.Button("Generate"))
				{
					generator.StartGenerating();
				}
			}

			using (new EditorGUI.DisabledScope(!generator.generationComplete))
			{
				if (GUILayout.Button("Save"))
				{
					generator.Save();
					UnityEditor.AssetDatabase.Refresh();
				}
			}

			//using (new EditorGUI.DisabledScope(generator.loadFile == null))
			{
				if (GUILayout.Button("Load"))
				{
					generator.Load();
				}
			}
		}

	}
}