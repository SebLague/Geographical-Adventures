using UnityEngine;

public class CountryIndexDisplay : MonoBehaviour
{
	public FilterMode filterMode;
	public MeshRenderer display;
	public int textureWidth;
	RenderTexture texture;


	void Start()
	{
		texture = FindObjectOfType<CountryIndexMapper>().CreateCountryIndexMap(textureWidth, textureWidth / 2);
		texture.filterMode = filterMode;
		display.material.mainTexture = texture;
	}

	void OnDestroy()
	{
		ComputeHelper.Release(texture);
	}

}
