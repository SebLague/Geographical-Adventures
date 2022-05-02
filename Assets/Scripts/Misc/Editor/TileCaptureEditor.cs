using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileCaptureTest))]
public class TileCaptureEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		TileCaptureTest capture = target as TileCaptureTest;

		using (new EditorGUI.DisabledScope(!Application.isPlaying))
		{
			if (!Application.isPlaying)
			{
				GUILayout.Box("Must be in play mode to capture");
			}
			if (GUILayout.Button("Capture tiles 4x2"))
			{
				capture.StartCapture4x2();
			}

		}

	}
}
