using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{
	public Player player;
	public RectTransform needle;
	public Image boostIndicator;
	public Color boostInactiveCol;
	public Color boostActiveCol;
	public Color boostChargingCol;
	public float smoothTime;

	//
	float smoothV;
	float needleValue;
	RectTransform boostIndicatorTransform;

	void Start()
	{
		boostIndicatorTransform = boostIndicator.GetComponent<RectTransform>();
	}

	void LateUpdate()
	{
		needleValue = Mathf.SmoothDamp(needleValue, player.TargetSpeedT, ref smoothV, smoothTime);
		needle.eulerAngles = new Vector3(0, 0, Mathf.Lerp(90, -90, needleValue));

		if (player.BoostRemainingT > 0)
		{
			boostIndicatorTransform.gameObject.SetActive(true);
			Color targetCol = player.BoosterIsCharging ? boostChargingCol : (player.IsBoosting ? boostActiveCol : boostInactiveCol);

			boostIndicator.color = Color.Lerp(boostIndicator.color, targetCol, Time.deltaTime * 3);
			boostIndicatorTransform.eulerAngles = Vector3.forward * player.BoostRemainingT * -180;
		}
		else
		{
			boostIndicatorTransform.gameObject.SetActive(false);
		}
	}
}
