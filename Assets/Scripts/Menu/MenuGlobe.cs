using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuGlobe : MonoBehaviour
{
	public Vector3 rot;
	public Vector3[] rotations;
	public Transform globe;

    void Start()
    {
        
    }

    void Update()
    {
        globe.transform.eulerAngles = rot;
    }
}
