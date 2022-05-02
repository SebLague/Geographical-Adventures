using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadScreen : MonoBehaviour
{

	public TMPro.TMP_Text log;
	public Canvas canvas;

	public void Init()
	{
		log.text = "";
		canvas.gameObject.SetActive(true);
	}


	public void Log(string info, bool newLine = true)
	{
		if (newLine && !string.IsNullOrEmpty(log.text))
		{
			log.text += "\n";
		}
		log.text += info;

	}

	public void Close()
	{
		canvas.gameObject.SetActive(false);
	}
}
