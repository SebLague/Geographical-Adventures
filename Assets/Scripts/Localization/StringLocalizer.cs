using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoGame.Localization
{
	public class StringLocalizer : MonoBehaviour
	{
		public string id;
		public TMPro.TMP_Text textElement;

		public string currentValue { get; private set; }
		public bool controlRectTransformWidth;
		public float padding = 50;

		void Start()
		{
			Localize();
			LocalizationManager.onLanguageChanged += Localize;
		}

		void Localize()
		{
			currentValue = LocalizationManager.Localize(id);
			textElement.text = currentValue;
			textElement.isRightToLeftText = LocalizationManager.IsRightToLeftWritingSystem;

			if (controlRectTransformWidth)
			{
				textElement.ForceMeshUpdate();
				RectTransform rectTransform = GetComponent<RectTransform>();
				if (rectTransform != null)
				{
					GetComponent<RectTransform>().sizeDelta = new Vector2(textElement.bounds.size.x + padding, rectTransform.sizeDelta.y);
				}
			}
		}

#if UNITY_EDITOR
		void OnValidate()
		{
			if (textElement == null)
			{
				textElement = GetComponent<TMPro.TMP_Text>();
				if (textElement == null)
				{
					textElement = GetComponentInChildren<TMPro.TMP_Text>();
				}
			}
		}
#endif
	}
}