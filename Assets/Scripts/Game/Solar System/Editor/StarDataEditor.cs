using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SolarSystem.CustomEditors
{
	[CustomEditor(typeof(StarData))]
	public class StarDataEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			StarData starData = target as StarData;

			if (GUILayout.Button("Generate"))
			{
				starData.CreateStarData();
			}

		}
	}
}