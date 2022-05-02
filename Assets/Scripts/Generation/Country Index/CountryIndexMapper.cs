using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class CountryIndexMapper : MonoBehaviour
{
	public ComputeShader compute;
	public int width = 8192;
	const int countryFillKernel = 0;

	public RenderTexture CreateCountryIndexMap()
	{
		return CreateCountryIndexMap(width, width / 2);
	}

	public RenderTexture CreateCountryIndexMap(int width, int height)
	{
		var format = GraphicsFormat.R8_UNorm;
		RenderTexture indexMap = ComputeHelper.CreateRenderTexture(width, height, FilterMode.Point, format, "Country Index Map");
		WriteCountryIndices(indexMap);
		return indexMap;

	}

	void WriteCountryIndices(RenderTexture texture)
	{
		Country[] countries = FindObjectOfType<CountryLoader>().GetCountries();

		var metaData = new List<PolygonMetaData>();
		var points = new List<Vector2>();

		for (int i = 0; i < countries.Length; i++)
		{
			Country country = countries[i];
			foreach (Polygon polygon in country.shape.polygons)
			{
				PolygonMetaData meta = new PolygonMetaData();

				meta.countryIndex = i;
				meta.bufferOffset = points.Count;
				Coordinate[] coordPath = polygon.paths[0].points;
				Vector2[] path = new Vector2[coordPath.Length];
				for (int j = 0; j < path.Length; j++)
				{
					path[j] = new Vector2(coordPath[j].longitude * Mathf.Rad2Deg, coordPath[j].latitude * Mathf.Rad2Deg);
				}
				Bounds2D bounds = new Bounds2D(path);
				meta.boundsMax = bounds.Max;
				meta.boundsMin = bounds.Min;

				points.AddRange(path);
				meta.length = path.Length;
				metaData.Add(meta);
			}
		}

		ComputeBuffer pointBuffer = new ComputeBuffer(points.Count, sizeof(float) * 2);
		pointBuffer.SetData(points);


		ComputeBuffer metadataBuffer = null;
		ComputeHelper.CreateStructuredBuffer(ref metadataBuffer, metaData.ToArray());


		compute.SetInt("width", texture.width);
		compute.SetInt("height", texture.height);
		compute.SetInt("numPoints", pointBuffer.count);
		compute.SetInt("numMeta", metadataBuffer.count);

		// Fill
		compute.SetBuffer(countryFillKernel, "Points", pointBuffer);
		compute.SetTexture(countryFillKernel, "CountryData", texture);
		compute.SetBuffer(countryFillKernel, "Meta", metadataBuffer);
		compute.SetInt("numCountries", countries.Length);
		ComputeHelper.Dispatch(compute, texture.width, texture.height, 1, countryFillKernel);

		pointBuffer.Release();
		metadataBuffer.Release();
	}

	struct PolygonMetaData
	{
		public int countryIndex;
		public Vector2 boundsMin;
		public Vector2 boundsMax;
		public int bufferOffset;
		public int length;
	}
}
