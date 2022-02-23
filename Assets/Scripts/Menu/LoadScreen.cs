using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadScreen : MonoBehaviour
{

	public TMPro.TMP_Text log;

	public void Init()
	{
		log.text = "";
	}

	public void Log(string info)
	{
		log.text += info;
		log.text += "\n";
	}
}
