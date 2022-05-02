using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipTest : MonoBehaviour
{

	public float speed;
	public bool norm;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.right * Time.deltaTime * -speed;
		if (norm) {
			transform.position = transform.position.normalized * 150;
			transform.up = transform.position.normalized;
		}
    }
}
