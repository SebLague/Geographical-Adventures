using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using GeoJSON.Net;

public class MarineLoad : MonoBehaviour
{

	public TextAsset marineFile;

	void Start()
	{
		var polygons = ReadPolygons(marineFile.text);

		foreach (var poly in polygons)
		{
			var col = Color.green;
			foreach (var path in poly.paths)
			{
				DebugExtra.DrawPath(path.GetPointsAsVector2(), true, col, 100);
				col = Color.red;
			}
		}
	}

	public static Polygon[] ReadPolygons(string geoJsonString)
	{
		List<Polygon> polygons = new List<Polygon>();
		GeoJSON.Net.Feature.FeatureCollection collection = new GeoJSON.Net.Feature.FeatureCollection();
		collection = JsonConvert.DeserializeObject<GeoJSON.Net.Feature.FeatureCollection>(geoJsonString);

		for (int i = 0; i < collection.Features.Count; i++)
		{
			var feature = collection.Features[i];
			Debug.Log(feature.Properties["name"]);

			if (feature.Geometry.Type == GeoJSONObjectType.Polygon)
			{
				var polygon = feature.Geometry as GeoJSON.Net.Geometry.Polygon;
				polygons.Add(ReadPolygon(polygon));
			}
			else if (feature.Geometry.Type == GeoJSONObjectType.MultiPolygon)
			{
				var multiPolygon = feature.Geometry as GeoJSON.Net.Geometry.MultiPolygon;
				//Debug.Log(feature.Properties["name"]);
				foreach (var polygon in multiPolygon.Coordinates)
				{
					polygons.Add(ReadPolygon(polygon));
				}
			}
		}

		return polygons.ToArray();
	}

	public Path[] Read()
	{

		List<Path> paths = new List<Path>();

		GeoJSON.Net.Feature.FeatureCollection collection = new GeoJSON.Net.Feature.FeatureCollection();
		collection = JsonConvert.DeserializeObject<GeoJSON.Net.Feature.FeatureCollection>(marineFile.text);
		for (int i = 0; i < collection.Features.Count; i++)
		{
			var feature = collection.Features[i];
			if (feature.Geometry.Type == GeoJSONObjectType.Polygon)
			{
				var polygon = feature.Geometry as GeoJSON.Net.Geometry.Polygon;
				var linestrings = polygon.Coordinates;
				foreach (var lineString in linestrings)
				{
					paths.Add(new Path(GetCoordinates(lineString)));
				}
			}
			if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.LineString)
			{
				var lineString = feature.Geometry as GeoJSON.Net.Geometry.LineString;
				paths.Add(new Path(GetCoordinates(lineString)));
			}
			if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.MultiLineString)
			{
				var multilineString = feature.Geometry as GeoJSON.Net.Geometry.MultiLineString;
				foreach (var lineString in multilineString.Coordinates)
				{
					paths.Add(new Path(GetCoordinates(lineString)));
				}
			}
		}

		return paths.ToArray();
	}

	public static Polygon ReadPolygon(GeoJSON.Net.Geometry.Polygon geoPolygon)
	{
		List<Path> paths = new List<Path>();
		var linestrings = geoPolygon.Coordinates;
		foreach (var lineString in linestrings)
		{
			paths.Add(new Path(GetCoordinates(lineString)));
		}

		return new Polygon() { paths = paths.ToArray() };
	}

	public static Coordinate[] GetCoordinates(GeoJSON.Net.Geometry.LineString lineString)
	{
		Coordinate[] coordinates = new Coordinate[lineString.Coordinates.Count];
		for (int j = 0; j < coordinates.Length; j++)
		{
			float lat = (float)lineString.Coordinates[j].Latitude * Mathf.Deg2Rad;
			float longitude = (float)lineString.Coordinates[j].Longitude * Mathf.Deg2Rad;
			Coordinate coord = new Coordinate(longitude, lat);
			coordinates[j] = coord;
		}
		return coordinates;
	}

}
