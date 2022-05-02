using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountryOutlines : MonoBehaviour
{
	public Color colour;
	public bool useTransformSpace;
	public bool projectToSphere;
	public float radius = 1;
	OutlineRenderer outlineRenderer;

	void Start()
	{
		CountryLoader countryLoader = FindObjectOfType<CountryLoader>();
		Country[] countries = countryLoader.GetCountries();

		outlineRenderer = GetComponent<OutlineRenderer>();

		for (int i = 0; i < countries.Length; i++)
		{
			CreateOutlinePathOnSphere(countries[i].shape.polygons);
		}
	}


	void CreateOutline(Country country)
	{
		//LineSegment[] path = CreateOutlinePathOnSphere(country.shape.polygons);
		//GetComponent<OutlineRenderer>().Add(path);
	}

	void CreateOutlinePathOnSphere(Polygon[] polygons)
	{
		List<LineSegment> lineSegments = new List<LineSegment>();

		foreach (Polygon polygon in polygons)
		{
			Coordinate[] path2D = polygon.paths[0].points;

			for (int i = 0; i < path2D.Length - 1; i++)
			{
				LineSegment lineSegment = new LineSegment();
				Vector3 a = path2D[i].ToVector2();
				Vector3 b = path2D[i + 1].ToVector2();
				if (projectToSphere)
				{
					a = GeoMaths.CoordinateToPoint(path2D[i], radius);
					b = GeoMaths.CoordinateToPoint(path2D[i + 1], radius);
				}
				if (useTransformSpace)
				{
					a = transform.TransformPoint(a);
					b = transform.TransformPoint(b);
				}
				lineSegment.pointA = a;
				lineSegment.pointB = b;
				lineSegments.Add(lineSegment);
			}

		}

		outlineRenderer.Add(lineSegments.ToArray(), colour);

	}

}
