using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMenu : MonoBehaviour
{
	public GameObject globeHolder;
	public GlobePlayerTrail globePlayerTrail;
	public GlobeController globeController;
	public GlobeDeliveryDisplay deliveryDisplay;

	bool active;


	public bool ToggleActive(Player player)
	{
		active = !active;

		if (active)
		{
			globePlayerTrail.ShowPlayer(player);
			globeController.FramePlayer(player.transform.position);

			globeHolder.gameObject.SetActive(true);
			deliveryDisplay.UpdateDisplay();
			globeController.Open();
		}
		else
		{
			globeController.Close();
			globeHolder.gameObject.SetActive(false);
		}
		return active;
	}
}
