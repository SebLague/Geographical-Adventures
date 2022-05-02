using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceDisplay : MonoBehaviour
{
	public enum Mode { Distance, Direction };

	[Header("Settings")]
	public Mode displayMode;
	public bool highlightLand;
	public float dstMultiplier = 0.75f;

	[Header("References")]
	public JumpFloodTest jumpFlood;
	public MeshRenderer display;

	void Start()
	{
		display.material.mainTexture = jumpFlood.result;
	}

	void Update()
	{
		display.material.SetInt("displayMode", (int)displayMode);
		display.material.SetInt("highlightLand", (highlightLand) ? 1 : 0);
		display.material.SetFloat("dstMultiplier", dstMultiplier);
	}
}
