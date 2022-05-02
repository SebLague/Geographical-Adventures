using UnityEngine;

namespace Seb.Meshing
{
	public struct RenderObject
	{
		public readonly GameObject gameObject;
		public readonly MeshRenderer renderer;
		public readonly MeshFilter filter;
		public readonly Material material;

		public RenderObject(GameObject gameObject, MeshRenderer renderer, MeshFilter filter, Material material)
		{
			this.gameObject = gameObject;
			this.renderer = renderer;
			this.filter = filter;
			this.material = material;
		}
	}
}