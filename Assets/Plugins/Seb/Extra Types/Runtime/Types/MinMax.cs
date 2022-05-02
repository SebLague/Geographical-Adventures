using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MinMax
{
	public float minValue;
	public float maxValue;

	public MinMax()
	{
		minValue = float.MaxValue;
		maxValue = float.MinValue;
	}

	public void AddValue(float value)
	{
		minValue = Mathf.Min(minValue, value);
		maxValue = Mathf.Max(maxValue, value);
	}

	public override string ToString()
	{
		return $"Min: {minValue} Max: {maxValue}";
	}
}
