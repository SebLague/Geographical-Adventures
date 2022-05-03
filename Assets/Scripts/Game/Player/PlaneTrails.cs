using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneTrails : MonoBehaviour
{

	public GameObject trailHolder;
	public Material trailMaterial;
	public Color trailCol = Color.white;
	public TrailRenderer[] trails;
	public Player player;

	public float alphaMin = 0;
	public float alphaMax = 0.5f;

	void Awake()
	{
		trailHolder.SetActive(false);
	}

	void Start()
	{
		trailHolder.gameObject.SetActive(true);

		for (int i = 0; i < trails.Length; i++)
		{
			trails[i].material = new Material(trailMaterial);
		}
	}

	void Update()
	{
		float alpha = Mathf.Lerp(alphaMin, alphaMax, player.SpeedT);

		for (int i = 0; i < trails.Length; i++)
		{
			trails[i].sharedMaterial.color = new Color(trailCol.r, trailCol.g, trailCol.b, alpha);
		}

	}
}
