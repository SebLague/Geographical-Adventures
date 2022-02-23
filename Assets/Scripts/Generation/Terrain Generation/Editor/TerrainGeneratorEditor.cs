using UnityEngine;
using UnityEditor;

namespace TerrainGeneration
{
	[CustomEditor(typeof(TerrainGenerator))]
	public class TerrainGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			TerrainGenerator generator = target as TerrainGenerator;

			using (new EditorGUI.DisabledScope(!Application.isPlaying || generator.isGenerating))
			{
				if (GUILayout.Button("Save to File"))
				{

					TerrainWriter.WriteToFile(generator.allFaceData, generator.saveFileName);
				}
			}

			if (GUILayout.Button("Load A"))
			{
				generator.LoadA();
			}
			if (GUILayout.Button("Load B"))
			{
				generator.LoadB();
			}

			if (GUILayout.Button("Regenerate Normals"))
			{
				generator.RegenerateNormals();
			}
				if (GUILayout.Button("Recalculate Normals"))
			{
				generator.CalculateAllNormals();
			}

		}
	}
}
