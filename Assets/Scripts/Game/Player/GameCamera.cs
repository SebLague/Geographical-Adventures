using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour
{
	public bool topDownMode;
	public Player player;

	[Header("Clipping")]

	public float farClipTopView = 100;
	public float farClipBehindView = 150;
	public int layerExtraTerrestrial;
	public float farClipExtraTerrestrial = 400;

	[Header("Top-Down Settings")]

	public float distanceAbove = 2;
	public float turnSpeed;
	public float startAngle;

	const KeyCode turnLeftKey = KeyCode.LeftArrow;
	const KeyCode turnRightKey = KeyCode.RightArrow;
	const KeyCode switchViewKey = KeyCode.Q;
	float angle;

	[Header("Behind-View Settings")]

	public float rotSmoothSpeed;
	public Vector3 offset;
	public Vector3 lookAheadOffset;


	// Other
	Transform target;
	Camera cam;

	void Start()
	{
		cam = GetComponent<Camera>();
		target = player.transform;

		if (topDownMode)
		{
			transform.position = target.position + target.position.normalized * distanceAbove;
			transform.LookAt(Vector3.zero);
		}
		else
		{
			transform.position = CalculatePos();
		}

		UpdateClippingPlane();
	}


	void LateUpdate()
	{

		if (topDownMode)
		{
			transform.position = target.position.normalized * (player.Height + distanceAbove);

			Vector3 gravityUp = transform.position.normalized;
			transform.LookAt(target.position, target.forward);
			transform.RotateAround(transform.position, gravityUp, -player.totalTurnAngle + angle + startAngle);

			if (Input.GetKey(turnLeftKey))
			{
				angle -= turnSpeed * Time.smoothDeltaTime;
			}
			if (Input.GetKey(turnRightKey))
			{
				angle += turnSpeed * Time.smoothDeltaTime;
			}
		}
		else
		{
			transform.position = CalculatePos();
			Vector3 lookTarget = CalculateLookTarget();

			Vector3 up = transform.position.normalized;
			transform.LookAt(lookTarget, up);
		}

		if (Input.GetKeyDown(switchViewKey))
		{
			topDownMode = !topDownMode;
			UpdateClippingPlane();
		}
	}



	Vector3 CalculatePos()
	{
		Vector3 p = target.position + target.forward * offset.z;
		return p.normalized * (player.Height + offset.y);
	}

	Vector3 CalculateLookTarget()
	{
		Vector3 p = target.position;
		p += target.right * lookAheadOffset.x;
		p += target.up * lookAheadOffset.y;
		p += target.forward * lookAheadOffset.z;
		return p;
	}

	void UpdateClippingPlane()
	{
		float[] layerClipDistances = new float[32];
		for (int i = 0; i < layerClipDistances.Length; i++)
		{
			layerClipDistances[i] = (topDownMode) ? farClipTopView : farClipBehindView;
		}
		layerClipDistances[layerExtraTerrestrial] = farClipExtraTerrestrial;
		cam.farClipPlane = Mathf.Max(Mathf.Max(farClipBehindView, farClipTopView), farClipExtraTerrestrial);
		cam.layerCullDistances = layerClipDistances;
	}

	void OnValidate()
	{
		if (Application.isPlaying && cam != null)
		{
			UpdateClippingPlane();
		}
	}

}