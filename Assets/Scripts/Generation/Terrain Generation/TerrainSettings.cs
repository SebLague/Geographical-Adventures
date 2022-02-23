using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Settings")]
public class TerrainSettings : ScriptableObject
{
	public int numSubdivisions = 4;
	public int resolution = 601;
	public int gridCellSize = 10;
	public float errorThreshold = 0.025f;
	public float normalsStepSize = 0.0018f;
}
