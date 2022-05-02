using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCountryPolygons : MonoBehaviour
{

	public CountryLoader countryLoader;

	void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			Country[] countries = countryLoader.GetCountries();

			foreach (var country in countries)
			{
				foreach (var polygon in country.shape.polygons)
				{


					for (int i = 0; i < polygon.paths.Length; i++)
					{
						bool isHole = i > 0;
						float z = (isHole) ? -0.01f : 0;
						Gizmos.color = (isHole) ? Color.red : Color.green;
						DrawPathGizmo(polygon.paths[i], z);

					}
				}
			}
		}
	}

	void DrawPathGizmo(Path path, float z = 0)
	{
		for (int i = 0; i < path.NumPoints - 1; i++)
		{
			Vector2 a = path.points[i].ToVector2();
			Vector2 b = path.points[i + 1].ToVector2();
			Gizmos.DrawLine(new Vector3(a.x, a.y, z), new Vector3(b.x, b.y, z));
		}
	}
}
