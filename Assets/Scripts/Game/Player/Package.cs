using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Package : MonoBehaviour
{

	public Parachute parachute;
	public float gravity;
	public float parachuteAirResistance;
	public float parachuteOpenTime = 1.25f;
	public Transform parachuteAttachPoint;

	float timeSinceDrop;
	Vector3 velocity;
	Vector3 parachutePoint;

	bool packageHasLanded;
	bool parachuteHasLanded;

	public bool hasTerrainInfo { get; private set; }
	public TerrainInfo terrainInfo { get; private set; }


	public void Init(WorldLookup worldLookup)
	{
		parachutePoint = parachuteAttachPoint.position;

		worldLookup.GetTerrainInfoAsync(transform.position, OnTerrainInfoReceived);
	}

	void OnTerrainInfoReceived(TerrainInfo info)
	{
		this.terrainInfo = info;
		hasTerrainInfo = true;
	}

	void Update()
	{
		timeSinceDrop += Time.deltaTime;

		velocity -= transform.up * gravity * Time.deltaTime;

		if (parachute.IsOpen)
		{
			velocity -= velocity * velocity.magnitude * parachuteAirResistance * Time.deltaTime;
		}
		else
		{
			parachutePoint += velocity * Time.deltaTime / 2;
			parachute.transform.position = parachutePoint;

			if (timeSinceDrop > parachuteOpenTime)
			{
				parachute.Open();
			}
		}

		if (packageHasLanded)
		{
			if (!parachuteHasLanded)
			{
				LandParachute();
			}
		}
		else
		{
			transform.position += velocity * Time.deltaTime;

			if (hasTerrainInfo)
			{
				float currentHeight = transform.position.magnitude;
				if (currentHeight < terrainInfo.height)
				{
					transform.position = transform.position.normalized * terrainInfo.height;
					parachute.StartCrumple();
					packageHasLanded = true;
				}
			}
		}
	}

	void LandParachute()
	{
		parachute.transform.position += velocity * Time.deltaTime / 2;

		if (parachute.transform.position.magnitude < parachuteAttachPoint.position.magnitude)
		{
			parachute.transform.position = Vector3.ClampMagnitude(parachute.transform.position, parachuteAttachPoint.position.magnitude);
			parachuteHasLanded = true;
		}
	}
}
