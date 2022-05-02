using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration;

public class NewOutlineTest : MonoBehaviour
{
	public TextAsset outlineFile;
	bool init;
	public float thickness = 1;
	public Material mat;


	LineRenderer[] lineR;
	public float offset;

	public void Load()
	{
		TerrainGenerator.AllOutlines allOutlines = JsonUtility.FromJson<TerrainGenerator.AllOutlines>(outlineFile.text);
		lineR = new LineRenderer[allOutlines.paths.Length];
		for (int i = 0; i < lineR.Length; i++)
		{
			//var p = new PolylinePath();
			//p.AddPoints(allOutlines.paths[i].path);
			//lines[i] = p;

			for (int j = 0; j < allOutlines.paths[i].path.Length; j++)
			{
				var v = allOutlines.paths[i].path[j];
				allOutlines.paths[i].path[j] = v + v.normalized * offset;
			}

			GameObject outline = new GameObject("Outline");
			outline.transform.parent = transform;
			var l = outline.AddComponent<LineRenderer>();
			l.widthMultiplier = thickness;
			l.positionCount = allOutlines.paths[i].path.Length;
			l.SetPositions(allOutlines.paths[i].path);
			l.sharedMaterial = mat;
			l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			l.receiveShadows = false;
			l.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
			l.allowOcclusionWhenDynamic = false;
			l.loop = true;
			lineR[i] = l;
		}

	}

	void Update()
	{
		//foreach (var l in lineR) {
		//l.widthMultiplier = thickness;
		//}
	}

}
