using UnityEngine;

[System.Serializable]
public struct CityLight
{
	public Vector3 pointOnSphere;
	public float height;
	public float intensity;
	public float randomT;
}

[System.Serializable]
public struct CityLightGroup
{
	public Bounds bounds;
	public CityLight[] cityLights;
}