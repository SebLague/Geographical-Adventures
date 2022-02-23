using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

	public event System.Action<Package> packageDropped;

	public Vector2 startCoordinate;
	public float startAngle;
	public float minElevation;
	public float maxElevation;
	public float currentElevation;

	[Header("Movement")]
	public bool pauseMovement;
	public float turnSpeedInTopDownView;
	public float turnSpeedInBehindView;
	public float normalSpeed;
	public float fastSpeed;
	public float speedSmoothing = 0.1f;

	[HideInInspector]
	public float totalTurnAngle;

	const KeyCode turnLeftKey = KeyCode.A;
	const KeyCode turnRightKey = KeyCode.D;
	const KeyCode pitchUpKey = KeyCode.W;
	const KeyCode pitchDownKey = KeyCode.S;
	const KeyCode debug_toggleMovementKey = KeyCode.Tab;

	public float smoothRollTime;
	public float rollAngle;
	public float smoothPitchTime;
	public float maxPitchAngle;
	public float changeElevationSpeed;
	public float turnSmoothTime;
	public GameCamera gameCamera;

	[Header("Graphics")]
	public Transform model;
	public Transform[] navigationLights;
	public Transform trailHolder;
	public Transform[] ailerons;
	public float aileronAngle = 20;
	public Transform propeller;
	public float propellerSpeed;

	[Header("Package Delivery")]
	public Package packagePrefab;
	public Transform packageDropPoint;

	[Header("Debug")]
	public float currentSpeed;
	public bool debug_TestInitPos;

	// Private stuff
	WorldLookup worldLookup;
	float smoothedTurnSpeed;
	float turnSmoothV;
	float pitchSmoothV;
	float rollSmoothV;

	public float currentPitchAngle { get; private set; }
	public float currentRollAngle { get; private set; }
	public int turnInput { get; private set; }

	float worldRadius;


	float pitchInput;
	Transform sunLight;
	float nextNavigationLightUpdateTime;
	bool navigationLightsOn;
	bool playerIsActive;

	void Awake()
	{
		trailHolder.gameObject.SetActive(false);

		var worldManager = FindObjectOfType<WorldManager>();
		worldRadius = WorldManager.worldRadius;
		sunLight = worldManager.sunLight.transform;
		worldLookup = worldManager.worldLookup;

		SetStartPos();

		currentSpeed = normalSpeed;
		SetNavigationLightScale(0);
	}

	void Start()
	{
		trailHolder.gameObject.SetActive(true);
	}

	public void Activate()
	{
		playerIsActive = true;
	}

	void Update()
	{
		if (playerIsActive)
		{
			HandleInput();
			HandleMovement();
			UpdateGraphics();
		}
	}

	void HandleInput()
	{
		turnInput = 0;
		if (Input.GetKey(turnLeftKey))
		{
			turnInput -= 1;
		}
		if (Input.GetKey(turnRightKey))
		{
			turnInput += 1;
		}

		pitchInput = 0;
		if (Input.GetKey(pitchUpKey))
		{
			pitchInput += 1;
		}
		if (Input.GetKey(pitchDownKey))
		{
			pitchInput -= 1;
		}


		HandleDebugInput();
	}

	void HandleDebugInput()
	{
		if (Application.isEditor)
		{
			if (Input.GetKeyDown(debug_toggleMovementKey))
			{
				pauseMovement = !pauseMovement;
			}
		}
	}
	void HandleMovement()
	{
		// Turn
		float turnSpeed = (gameCamera.topDownMode) ? turnSpeedInTopDownView : turnSpeedInBehindView;
		smoothedTurnSpeed = Mathf.SmoothDamp(smoothedTurnSpeed, turnInput * turnSpeed, ref turnSmoothV, turnSmoothTime);
		float turnAmount = smoothedTurnSpeed * Time.deltaTime;
		totalTurnAngle += turnAmount;
		transform.RotateAround(transform.position, transform.up, turnAmount);

		// Update speed
		float targetSpeed = (Input.GetKey(KeyCode.LeftShift)) ? fastSpeed : normalSpeed;
		currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 1 - Mathf.Pow(speedSmoothing, Time.deltaTime));


		// Calculate forward and vertical components of the speed based on pitch angle of the plane
		float forwardSpeed = Mathf.Cos(Mathf.Abs(currentPitchAngle) * Mathf.Deg2Rad) * currentSpeed;
		float verticalVelocity = -Mathf.Sin(currentPitchAngle * Mathf.Deg2Rad) * currentSpeed;

		// Update elevation
		currentElevation += verticalVelocity * Time.deltaTime;
		currentElevation = Mathf.Clamp(currentElevation, minElevation, maxElevation);

		// Debug/test stuff
		if (Application.isEditor)
		{
			forwardSpeed *= (pauseMovement) ? 0 : 1;
			forwardSpeed = Mathf.Abs(forwardSpeed) * ((Input.GetKey(KeyCode.F)) ? -1 : 1);
		}



		// Update position
		Vector3 newPos = transform.position + transform.forward * forwardSpeed * Time.deltaTime;
		newPos = newPos.normalized * (worldRadius + currentElevation);
		transform.position = newPos;
		if (debug_TestInitPos)
		{
			SetStartPos();
		}

		UpdateRotation();

		// --- Update pitch and roll ---
		float targetPitch = pitchInput * maxPitchAngle;
		// Automatically stop pitching the plane when reaching min/max elevation
		float dstToPitchLimit = (targetPitch > 0) ? currentElevation - minElevation : maxElevation - currentElevation;
		float pitchLimitSmoothDst = 3;
		targetPitch *= Mathf.Clamp01(dstToPitchLimit / pitchLimitSmoothDst);

		currentPitchAngle = Mathf.SmoothDampAngle(currentPitchAngle, targetPitch, ref pitchSmoothV, smoothPitchTime);


		float targetRoll = turnInput * rollAngle;
		currentRollAngle = Mathf.SmoothDampAngle(currentRollAngle, targetRoll, ref rollSmoothV, smoothRollTime);
	}

	void UpdateRotation()
	{
		Vector3 gravityUp = transform.position.normalized;
		transform.rotation = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;
		transform.LookAt((transform.position + transform.forward * 10).normalized * (worldRadius + currentElevation), gravityUp);
	}

	void UpdateGraphics()
	{
		// Rotate ailerons when turning
		UpdateAileronGraphic(ailerons[0], -turnInput * aileronAngle);
		UpdateAileronGraphic(ailerons[1], turnInput * aileronAngle);

		// Set pitch/roll rotation
		model.localEulerAngles = new Vector3(currentPitchAngle, 0, currentRollAngle);

		// Rotate propeller
		propeller.localEulerAngles += Vector3.forward * propellerSpeed * Time.deltaTime;

		// Turn on navigation lights at night (even if should technically be on always I think...)
		if (Time.time > nextNavigationLightUpdateTime)
		{
			bool isDark = Vector3.Dot(transform.up, -sunLight.forward) < 0.25;
			navigationLightsOn = isDark;
			nextNavigationLightUpdateTime = Time.time + 3;
		}
		SetNavigationLightScale((navigationLightsOn) ? 1 : 0, true);
	}

	void UpdateAileronGraphic(Transform aileron, float targetAngle)
	{

		Vector3 rot = aileron.localEulerAngles;
		float smoothAngle = Mathf.LerpAngle(rot.x, targetAngle, Time.deltaTime * 5);
		aileron.localEulerAngles = new Vector3(smoothAngle, rot.y, rot.z);
	}

	void SetStartPos()
	{
		Coordinate coord = new Coordinate(startCoordinate.x * Mathf.Deg2Rad, startCoordinate.y * Mathf.Deg2Rad);
		transform.position = CoordinateSystem.CoordinateToPoint(coord, worldRadius + currentElevation);
		Vector3 gravityUp = transform.position.normalized;
		transform.rotation = Quaternion.FromToRotation(transform.up, gravityUp);
		transform.LookAt((transform.position + transform.forward * 10).normalized * (worldRadius + currentElevation), gravityUp);
		transform.Rotate(transform.position.normalized, startAngle, Space.World);
	}

	public Package DropPackage()
	{
		Package package = Instantiate(packagePrefab, packageDropPoint.position, packageDropPoint.rotation);
		package.Init(worldLookup);
		packageDropped?.Invoke(package);
		return package;
	}

	// Allow navigation lights to be turned on and off by scaling them (crude way to allow brightness to fade in/out)
	void SetNavigationLightScale(float scale, bool smooth = false)
	{
		for (int i = 0; i < navigationLights.Length; i++)
		{
			if (smooth)
			{
				float currentScale = navigationLights[i].localScale.x;
				navigationLights[i].localScale = Vector3.one * Mathf.Lerp(currentScale, scale, Time.deltaTime);
			}
			else
			{
				navigationLights[i].localScale = Vector3.one * scale;
			}
		}
	}

	public float Height
	{
		get
		{
			return worldRadius + currentElevation;
		}
	}
}
