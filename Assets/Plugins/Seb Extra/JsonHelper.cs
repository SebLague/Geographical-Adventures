using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JsonHelper
{

	public static string ArrayToJson<T>(T[] array)
	{
		var holder = new Holder<T>(array);
		return JsonUtility.ToJson(holder);
	}

	public static T[] ArrayFromJson<T>(string jsonString)
	{
		return JsonUtility.FromJson<Holder<T>>(jsonString).items;
	}

	[System.Serializable]
	public struct Holder<T>
	{
		public T[] items;

		public Holder(T[] items)
		{
			this.items = items;
		}
	}
}
