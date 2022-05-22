using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GeoGame.Quest
{

	public class CityMarker : MonoBehaviour
	{
		public float textHeight;
		public bool updateRotationContinuously;

		public Transform cityNameHolder;
		public TMPro.TMP_Text cityNameUI;
		public Transform graphic;
		float t;


		public void Init(Vector3 position, Vector3 cameraPos)
		{
			transform.position = position;
			cityNameUI.gameObject.SetActive(false);
			//cityNameUI.text = cityName;
			//UpdateRotation(cameraPos);
			transform.localScale = Vector3.zero;

		}

		void UpdateRotation(Vector3 camPos)
		{
			// Rotate city name to be aligned with camera, but still facing upwards
			Vector3 gravityUp = transform.position.normalized;
			cityNameHolder.transform.rotation = Camera.main.transform.rotation;
			cityNameHolder.transform.rotation = Quaternion.FromToRotation(cityNameHolder.transform.up, gravityUp) * cityNameHolder.transform.rotation;
		}

		void Update()
		{
			t+=Time.deltaTime;
			transform.localScale = Vector3.one * Seb.Ease.Cubic.InOut(t);
			if (updateRotationContinuously)
			{
				//UpdateRotation(Camera.main.transform.position);
			}
			//cityNameHolder.position = transform.position + transform.position.normalized * textHeight;
		}
	}
}