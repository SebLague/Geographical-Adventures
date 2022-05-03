using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLodSystem : MonoBehaviour
{
	public enum Mode { Auto, ForceHighRes, ForceLowRes }

	[Header("Settings")]
	public Mode mode;
	public float highResDistanceThreshold = 50;
	[Min(1)] public int numFramesPerUpdate = 1; // spread update over multiple frames
	public Camera cam;

	[Header("Debug")]
	public bool useDebugMaterial;
	public Color highResDebugCol = Color.red;
	public Color lowResDebugCol = Color.green;


	Material lowResDebugMat;
	Material highResDebugMat;


	Transform camT;
	List<RenderGroup> renderers;
	Plane[] frustumPlanes;
	int lastUpdatedIndex;

	Vector3 camPosOld;
	Vector3 camDirOld;

	void Start()
	{
		camT = cam.transform;
		var debugShader = Shader.Find("Standard");
		lowResDebugMat = new Material(debugShader);
		highResDebugMat = new Material(debugShader);
		frustumPlanes = new Plane[6];

		Camera.onPreCull += UpdateLODs;
	}

	public void AddLOD(MeshRenderer highRes, MeshRenderer lowRes)
	{
		if (renderers == null)
		{
			renderers = new List<RenderGroup>();
		}

		renderers.Add(new RenderGroup(highRes, lowRes));
	}


	// Called on camera pre-cull
	void UpdateLODs(Camera camera)
	{
		if (renderers != null && camera == cam)
		{
			highResDebugMat.color = highResDebugCol;
			lowResDebugMat.color = lowResDebugCol;

			GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);

			int numToUpdate = Mathf.CeilToInt(renderers.Count / Mathf.Max(1f, numFramesPerUpdate));

			// Cam pos/dir changed drastically since last frame, so update all renderers immediately
			if ((camT.position - camPosOld).sqrMagnitude > 1 || Vector3.Dot(camT.forward, camDirOld) < 0.9f)
			{
				lastUpdatedIndex = 0;
				numToUpdate = renderers.Count;
			}

			for (int i = 0; i < numToUpdate; i++)
			{
				var renderer = renderers[lastUpdatedIndex];
				Process(renderer);
				lastUpdatedIndex = (lastUpdatedIndex + 1) % renderers.Count;
			}

			camPosOld = camT.position;
			camDirOld = camT.forward;
		}
	}

	void Process(RenderGroup renderer)
	{
		bool showHighRes = false;
		switch (mode)
		{
			case Mode.Auto:
				// Show high res mesh if within distance threshold
				bool showHighResDst = renderer.highRes.bounds.SqrDistance(camT.position) < highResDistanceThreshold * highResDistanceThreshold;
				// Show high res mesh if in view frustum (low res version is fine if only being rendered for shadows)
				bool showHighResFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.highRes.bounds);
				showHighRes = showHighResDst && showHighResFrustum;
				break;
			case Mode.ForceHighRes:
				showHighRes = true;
				break;
			case Mode.ForceLowRes:
				showHighRes = false;
				break;
		}

		renderer.Set(showHighRes);
		renderer.Debug(useDebugMaterial, highResDebugMat, lowResDebugMat);
	}

	void OnDestroy()
	{
		Camera.onPreCull -= UpdateLODs;
	}

	public class RenderGroup
	{
		public MeshRenderer highRes;
		public MeshRenderer lowRes;
		Material highResMat;
		Material lowResMat;

		bool usingDebugMat;
		bool showingHighRes;

		public RenderGroup(MeshRenderer highRes, MeshRenderer lowRes)
		{
			this.highRes = highRes;
			this.lowRes = lowRes;
			highResMat = highRes.sharedMaterial;
			lowResMat = lowRes.sharedMaterial;

			usingDebugMat = false;
			showingHighRes = false;
			highRes.gameObject.SetActive(false);
			lowRes.gameObject.SetActive(true);
		}

		public void Set(bool showHighRes)
		{
			if (showingHighRes != showHighRes)
			{
				showingHighRes = showHighRes;
				highRes.gameObject.SetActive(showHighRes);
				lowRes.gameObject.SetActive(!showHighRes);
			}
		}

		public void Debug(bool useDebugMat, Material highResDebugMat, Material lowResDebugMat)
		{
			if (usingDebugMat != useDebugMat)
			{
				usingDebugMat = useDebugMat;
				highRes.sharedMaterial = (useDebugMat) ? highResDebugMat : highResMat;
				lowRes.sharedMaterial = (useDebugMat) ? lowResDebugMat : lowResMat;
			}
		}


	}
}
