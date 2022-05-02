using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialInspector : MonoBehaviour
{

	public event System.Action<Material> materialUpdated;
	public Material material;

	public void NotifyMaterialUpdate()
	{
		materialUpdated?.Invoke(material);
	}

}
