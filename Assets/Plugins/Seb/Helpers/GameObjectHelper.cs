using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.Helpers
{
	public static class GameObjectHelper
	{
		public static void SetActiveAll(bool active, params GameObject[] gameObjects)
		{
			foreach (var g in gameObjects)
			{
				g.SetActive(active);
			}
		}
	}
}
