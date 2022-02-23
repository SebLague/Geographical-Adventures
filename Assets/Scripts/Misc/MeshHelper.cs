using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshHelper
{

	public static RenderObject CreateRendererObject(string name, Mesh mesh = null, Material material = null, Transform parent = null)
	{
		GameObject meshHolder = new GameObject(name);
		MeshFilter meshFilter = meshHolder.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = meshHolder.AddComponent<MeshRenderer>();

		RenderObject renderObject = new RenderObject(meshHolder, meshRenderer, meshFilter);

		meshFilter.mesh = mesh;
		meshRenderer.material = material;
		meshHolder.transform.parent = parent;

		return renderObject;
	}

	public struct RenderObject
	{
		public readonly GameObject gameObject;
		public readonly MeshRenderer renderer;
		public readonly MeshFilter filter;

		public RenderObject(GameObject gameObject, MeshRenderer renderer, MeshFilter filter)
		{
			this.gameObject = gameObject;
			this.renderer = renderer;
			this.filter = filter;
		}
	}
}
