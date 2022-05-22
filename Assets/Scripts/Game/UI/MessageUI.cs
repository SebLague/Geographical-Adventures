using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class MessageUI : MonoBehaviour
{

	public float spacingBottom = 1.5f;
	public RectTransform messageHolder;
	public TMP_Text messagePrefab;
	public TMP_Text test;

	Message activeMessage;

	public void ShowMessage(string messageText, float duration)
	{
		// Deactivate previous active message
		if (activeMessage != null)
		{
			activeMessage.active = false;
		}

		var messageTextInstance = Instantiate(messagePrefab, parent: messageHolder);
		messageTextInstance.text = messageText;
		Message message = new Message() { text = messageTextInstance, active = true, visibleDuration = duration };
		activeMessage = message;
		StartCoroutine(AnimateMessage(activeMessage));
	}

	IEnumerator AnimateMessage(Message message)
	{
		message.text.ForceMeshUpdate();
		float boundsHeight = message.text.bounds.size.y;
		float lineSpacing = boundsHeight / Mathf.Max(1, message.text.textInfo.lineCount);

		float startY = -boundsHeight / 2;
		float endY = boundsHeight / 2 + lineSpacing * spacingBottom;
		//activeMessage.rectTransform.localPosition = Vector3.up * (boundsHeight / 2 + lineSpacing);

		float t = 0;
		const float appearDuration = 0.5f;
		Color textCol = message.text.color;

		// Appear
		while (t < 1)
		{
			t += Time.deltaTime / appearDuration;
			float easedT = Seb.Ease.Cubic.InOut(t);
			message.text.rectTransform.localPosition = Vector3.up * Mathf.Lerp(startY, endY, easedT);
			message.text.color = new Color(textCol.r, textCol.g, textCol.b, easedT);
			yield return null;
		}

		// Wait
		t = 0;
		while (t < message.visibleDuration)
		{
			t += Time.deltaTime;
			if (!message.active)
			{
				break;
			}
			yield return null;
		}

		// Fade out
		t = 0;
		const float fadeOutDuration = 0.5f;
		while (t < 1)
		{
			t += Time.deltaTime / fadeOutDuration;
			message.text.color = new Color(textCol.r, textCol.g, textCol.b, Mathf.Clamp01(1 - t));
			yield return null;
		}

		// Destroy
		Destroy(message.text.gameObject);
		message = null;
	}

	IEnumerator AnimateDisappear(TMP_Text text)
	{
		float t = 0;
		const float duration = 0.5f;
		while (t < 1)
		{
			t += Time.deltaTime / duration;
			text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Clamp01(1 - t));
			yield return null;
		}
	}

	void UpdateAA()
	{
		test.ForceMeshUpdate();
		//Debug.Log(test.bounds.size);
		//return;
		Vector3[] corners = new Vector3[4];
		messageHolder.GetWorldCorners(corners);
		float holderBottomY = corners[0].y;
		float middleX = (corners[0].x + corners[3].x) / 2;
		//test.ForceMeshUpdate();
		Debug.Log(test.bounds.size);
		test.rectTransform.position = new Vector3(middleX, holderBottomY + test.bounds.size.y / 2, 0);

		//
		//Debug.Log(test.rectTransform.localPosition + "   " + test.rectTransform.anchoredPosition);

	}

	class Message
	{
		public TMP_Text text;
		public bool active;
		public float visibleDuration;
	}
}
