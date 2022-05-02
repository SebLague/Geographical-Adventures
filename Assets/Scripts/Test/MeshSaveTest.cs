using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Meshing;

public class MeshSaveTest : MonoBehaviour
{
	public byte[] bytes;
	public MeshFilter testMesh;
	public MeshFilter testMesh2;
	public Material testMat;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.S))
		{
			Debug.Log("Save");


			SimpleMeshData d = new SimpleMeshData(testMesh.sharedMesh.vertices, testMesh.sharedMesh.triangles, testMesh.sharedMesh.normals);
			d.name = "Test Mesh Name";
			SimpleMeshData d2 = new SimpleMeshData(testMesh2.sharedMesh.vertices, testMesh2.sharedMesh.triangles, testMesh2.sharedMesh.normals);
			d2.name = "";
			bytes = MeshSerializer.MeshesToBytes(new SimpleMeshData[]{d,d2});
			//bytes = MeshSerializer.MeshToBytes(d);
		}

		if (Input.GetKeyDown(KeyCode.L))
		{
			Debug.Log("Load");

			SimpleMeshData[] d = MeshSerializer.BytesToMeshes(bytes);
			SimpleMeshData combined = SimpleMeshData.Combine(d[0],d[1]);
			//MeshHelper.CreateRendererObject("Test", MeshHelper.CreateMesh(d[0], false), testMat);
			//MeshHelper.CreateRendererObject("Test2", MeshHelper.CreateMesh(d[1], false), testMat);
			MeshHelper.CreateRendererObject("Combined", MeshHelper.CreateMesh(combined, false), testMat);

		//	SimpleMeshData d = MeshSerializer.BytesToMesh(bytes);
			//MeshHelper.CreateRendererObject("Test", MeshHelper.CreateMesh(d, false), testMat);
		}
	}


}
