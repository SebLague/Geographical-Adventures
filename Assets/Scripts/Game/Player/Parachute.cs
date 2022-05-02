using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parachute : MonoBehaviour
{
	public AudioSource openAudio;

	[Header("Geometry")]
	public Vector2Int numPoints;
	public int numPointsPerCircle;
	public int numCircles;
	public bool useDiagonals;
	public bool useLoad;
	public float ropeThickness = 0.1f;
	public Transform ropeAttachPoint;

	[Header("Canopy Shape")]
	public float canopyScale = 1;
	public float inflatedHeight;
	public float indentRadiusMultiplier;

	[Header("Canopy Animation")]
	[Range(0, 1)]
	public float animWidth;
	[Range(0, 1)]
	public float animHeight;
	[Range(0, 1)]
	public float cuspT;
	public bool setToFirstFrameAtStart = true;
	float crumpleT;
	float crumpleSpeed = 0.5f;
	bool crumpling;


	[Header("Display")]
	public MeshFilter filter;
	public Material ropeMat;

	Mesh mesh;
	Animation anim;
	Transform[] ropes;
	List<int> tris;
	List<Vector3> verts = new List<Vector3>();
	List<Vector2> uvs = new List<Vector2>();

	public bool IsOpen { get; private set; }

	protected void Start()
	{
		mesh = new Mesh();
		filter.mesh = mesh;

		anim = GetComponent<Animation>();

		// Set params to start of animation
		if (setToFirstFrameAtStart)
		{
			anim.Play();
			anim.Sample();
			anim.Stop();
		}


		CreateRopes();
		CreateTriangles();
		GenerateMesh();
	}

	public float CalculateRadius(float t)
	{
		float y = (t - cuspT / 2) / (1 - cuspT / 2);
		return animWidth * Mathf.Sqrt(Mathf.Abs(1 - y * y));
	}

	void Update()
	{
		if (crumpling)
		{
			crumpleT += Time.deltaTime * crumpleSpeed;
			if (crumpleT > 1)
			{
				crumpleT = 1;
				crumpling = false;
			}
		}

		if (anim.isPlaying || crumpling)
		{
			GenerateMesh();
		}

		UpdateRopes();

	}

	void GenerateMesh()
	{
		float crumpleScale = Mathf.Lerp(1, 0.5f, crumpleT);

		mesh.Clear();
		verts.Clear();
		uvs.Clear();


		Vector3 centrePoint = Vector3.up * inflatedHeight * animHeight * canopyScale * crumpleScale;
		verts.Add(centrePoint);
		uvs.Add(Vector2.one * 0.5f);

		float maxRadius = CalculateRadius(0) * canopyScale;

		for (int circleIndex = 0; circleIndex < numCircles; circleIndex++)
		{
			float t = 1 - (circleIndex + 1f) / numCircles;
			t = -t * t + 2 * t;

			float height = t * inflatedHeight * animHeight * canopyScale * crumpleScale;
			float circleRadius = CalculateRadius(t);

			for (int pointIndex = 0; pointIndex < numPointsPerCircle; pointIndex++)
			{
				float m = (pointIndex % 2 == 0) ? 1 : indentRadiusMultiplier;
				float radius = circleRadius * m;

				float angle = pointIndex / (float)(numPointsPerCircle) * Mathf.PI * 2;
				Vector2 pos2D = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius * canopyScale;
				Vector3 pos = new Vector3(pos2D.x, height, pos2D.y);
				verts.Add(pos);

				float uvAngle = angle * 3 + Mathf.PI / 2;
				Vector2 uvSNorm = new Vector2(Mathf.Sin(uvAngle), Mathf.Cos(uvAngle));
				Vector2 uv = (uvSNorm + Vector2.one) * 0.5f;
				uvs.Add(uv);
			}
		}

		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0, true);
		mesh.SetUVs(0, uvs);
		mesh.RecalculateNormals();

	}

	void CreateRopes()
	{
		ropes = new Transform[numPointsPerCircle / 2];
		for (int i = 0; i < ropes.Length; i++)
		{
			ropes[i] = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			ropes[i].GetComponent<MeshRenderer>().material = ropeMat;
			ropes[i].SetParent(transform);
		}
	}

	void UpdateRopes()
	{
		// Ropes
		Vector3 loadPos = transform.InverseTransformPoint(ropeAttachPoint.position);
		int spacing = 2;
		int ropeIndex = 0;

		for (int i = 1; i < numPointsPerCircle; i += spacing)
		{
			Vector3 start = verts[GetPointIndex(numCircles - 1, i)];
			Vector3 end = loadPos;
			Vector3 centre = (start + end) / 2;
			var rot = Quaternion.FromToRotation(Vector3.up, (start - end).normalized);
			Vector3 scale = new Vector3(ropeThickness, (start - end).magnitude, ropeThickness);

			ropes[ropeIndex].localPosition = centre;
			ropes[ropeIndex].localRotation = rot;
			ropes[ropeIndex].localScale = scale;
			ropeIndex++;

		}
	}

	void CreateTriangles()
	{
		tris = new List<int>();
		// Create triangles
		for (int circleIndex = 0; circleIndex < numCircles; circleIndex++)
		{
			for (int pointIndex = 0; pointIndex < numPointsPerCircle; pointIndex++)
			{
				tris.Add(GetPointIndex(circleIndex - 1, pointIndex));
				tris.Add(GetPointIndex(circleIndex, pointIndex));
				tris.Add(GetPointIndex(circleIndex, pointIndex + 1));
				if (circleIndex > 0)
				{
					tris.Add(GetPointIndex(circleIndex - 1, pointIndex));
					tris.Add(GetPointIndex(circleIndex, pointIndex + 1));
					tris.Add(GetPointIndex(circleIndex - 1, pointIndex + 1));
				}
			}
		}
	}

	int GetPointIndex(int circleIndex, int pointIndex = 0)
	{
		if (circleIndex == -1)
		{
			return 0;
		}
		return circleIndex * numPointsPerCircle + 1 + (pointIndex % numPointsPerCircle);
	}

	public void Open()
	{
		IsOpen = true;
		openAudio.Play();
		anim.Play();
	}

	public void StartCrumple()
	{
		crumpling = true;
	}

}
