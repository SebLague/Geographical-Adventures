using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

	public event System.Action<Package> packageDropped;

	[Header("Startup Settings")]
	public bool worldIsSpherical = true;
	public PlayerStartPoint startPoint;

	[Header("Elevation")]
	public float minElevation;
	public float maxElevation;
	public float currentElevation;

	[Header("Movement")]
	public float turnSpeedInTopDownView;
	public float turnSpeedInBehindView;
	public float minSpeed = 4;
	public float maxSpeed = 12;
	public float accelerateDuration = 2;
	public float boostSpeed = 25;
	public float speedSmoothing = 0.1f;
	public float maxBoostTime = 30;
	public float boostTimeAtStart = 5;

	[HideInInspector]
	public float totalTurnAngle;
	public float smoothRollTime;
	public float rollAngle;
	public float smoothPitchTime;
	public float maxPitchAngle;
	public float changeElevationSpeed;
	public float turnSmoothTime;

	[Header("Graphics")]
	public Transform model;
	public Transform[] navigationLights;
	public Transform[] ailerons;
	public float aileronAngle = 20;
	public Transform propeller;
	public float propellerSpeed;

	[Header("Package Delivery")]
	public Package packagePrefab;
	public Transform packageDropPoint;

	[Header("Other References")]
	public WorldLookup worldLookup;
	public Transform sunLight;
	public TerrainGeneration.TerrainHeightSettings heightSettings;

	[Header("Debug")]
	public float currentSpeed;
	public bool debug_TestInitPos;
	public bool debug_lockMovement;

	// Private stuff


	GameCamera gameCamera;
	float smoothedTurnSpeed;
	float turnSmoothV;
	float pitchSmoothV;
	float rollSmoothV;

	public float currentPitchAngle { get; private set; }
	public float currentRollAngle { get; private set; }
	public float turnInput { get; private set; }
	bool boosting;
	float boostTimeRemaining;
	float boostTimeToAdd;
	float baseTargetSpeed;

	float worldRadius;


	float pitchInput;


	float nextNavigationLightUpdateTime;
	bool navigationLightsOn;

	float recordPositionDstThreshold = 1;
	int maxHistorySize = 10000;
	public Queue<Vector3> positionHistory { get; private set; }
	Vector3 lastRecordedPos;
	Vector3 posLastFrame;


	// Note: this is calculated as dst on surface of earth (meaning elevation has no effect)
	public float distanceTravelledKM { get; private set; }


	void Awake()
	{
		gameCamera = FindObjectOfType<GameCamera>();

		worldRadius = heightSettings.worldRadius;

		SetStartPos(startPoint);

		baseTargetSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.35f);
		currentSpeed = baseTargetSpeed;
		SetNavigationLightScale(0);

		positionHistory = new Queue<Vector3>(maxHistorySize);
		boostTimeRemaining = boostTimeAtStart;
	}

	void Update()
	{
		if (GameController.IsState(GameState.Playing))
		{
			HandleInput();
			HandleMovement();
			UpdatePositionHistory();
			UpdateBoostTimer();
		}

		UpdateGraphics();
		UpdateDevMode();

	}

	public void UpdateMovementInput(Vector2 moveInput, float accelerateDir, bool boosting)
	{
		// Turning
		turnInput = moveInput.x;
		pitchInput = moveInput.y;

		// Speed
		baseTargetSpeed += (maxSpeed - minSpeed) / accelerateDuration * accelerateDir * Time.deltaTime;
		baseTargetSpeed = Mathf.Clamp(baseTargetSpeed, minSpeed, maxSpeed);

		this.boosting = boosting && boostTimeRemaining > 0;
	}


	void HandleInput()
	{
		HandleDebugInput();
	}

	void HandleDebugInput()
	{
		bool devMode = Input.GetKey(KeyCode.LeftBracket) && Input.GetKey(KeyCode.RightBracket);
		if (Application.isEditor || devMode)
		{
			if (Input.GetKeyDown(KeyBindings.Debug_ToggleLockPlayer))
			{
				debug_lockMovement = !debug_lockMovement;
			}

			if (Input.GetKeyDown(KeyCode.B))
			{
				AddBoost(15);
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


		// Update speed
		float targetSpeed = (boosting) ? boostSpeed : baseTargetSpeed;
		currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 1 - Mathf.Pow(speedSmoothing, Time.deltaTime));


		// Calculate forward and vertical components of the speed based on pitch angle of the plane
		float forwardSpeed = Mathf.Cos(Mathf.Abs(currentPitchAngle) * Mathf.Deg2Rad) * currentSpeed;
		float verticalVelocity = -Mathf.Sin(currentPitchAngle * Mathf.Deg2Rad) * currentSpeed;

		// Update elevation
		currentElevation += verticalVelocity * Time.deltaTime;
		currentElevation = Mathf.Clamp(currentElevation, minElevation, maxElevation);

		// Debug/test stuff
		if (GameController.InDevMode)
		{
			forwardSpeed *= (debug_lockMovement) ? 0 : 1;
			forwardSpeed = Mathf.Abs(forwardSpeed) * ((Input.GetKey(KeyCode.F)) ? -1 : 1);
		}




		UpdatePosition(forwardSpeed);
		UpdateRotation(turnAmount);

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

	void UpdateBoostTimer()
	{
		// Decrease boost timer when boosting
		if (boosting)
		{
			boostTimeRemaining = Mathf.Max(0, boostTimeRemaining - Time.deltaTime);
		}
		// Increase boost timer gradually when time has been gained
		if (boostTimeToAdd > 0)
		{
			const float boostAddSpeed = 4;
			float boostTimeToAddThisFrame = Mathf.Min(Time.deltaTime * boostAddSpeed, boostTimeToAdd);
			boostTimeRemaining += boostTimeToAddThisFrame;
			boostTimeToAdd -= boostTimeToAddThisFrame;
			boostTimeRemaining = Mathf.Min(boostTimeRemaining, maxBoostTime);
		}
	}

	void UpdatePosition(float forwardSpeed)
	{
		// Update position
		Vector3 newPos = transform.position + transform.forward * forwardSpeed * Time.deltaTime;
		if (worldIsSpherical)
		{
			newPos = newPos.normalized * (worldRadius + currentElevation);
		}
		else
		{
			newPos = new Vector3(newPos.x, currentElevation, newPos.z);
		}
		transform.position = newPos;
	}

	void UpdateRotation(float turnAmount)
	{
		if (worldIsSpherical)
		{
			Vector3 gravityUp = transform.position.normalized;
			transform.RotateAround(transform.position, gravityUp, turnAmount);
			transform.LookAt((transform.position + transform.forward * 10).normalized * (worldRadius + currentElevation), gravityUp);
			transform.rotation = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;
		}
		else
		{
			transform.RotateAround(transform.position, Vector3.up, turnAmount);
		}
	}


	void UpdateGraphics()
	{
		// Rotate ailerons when turning
		UpdateAileronGraphic(ailerons[0], -turnInput * aileronAngle);
		UpdateAileronGraphic(ailerons[1], turnInput * aileronAngle);

		// Set pitch/roll rotation
		SetPlaneRotation();

		// Rotate propeller
		propeller.localEulerAngles += Vector3.forward * propellerSpeed * Time.deltaTime;

		// Turn on navigation lights at night (even if should technically be on always I think...)
		if (Time.time > nextNavigationLightUpdateTime && sunLight != null)
		{
			bool isDark = Vector3.Dot(transform.up, -sunLight.forward) < 0.25;
			navigationLightsOn = isDark;
			nextNavigationLightUpdateTime = Time.time + 3;
		}
		SetNavigationLightScale((navigationLightsOn) ? 1 : 0, true);

		void UpdateAileronGraphic(Transform aileron, float targetAngle)
		{

			Vector3 rot = aileron.localEulerAngles;
			float smoothAngle = Mathf.LerpAngle(rot.x, targetAngle, Time.deltaTime * 5);
			aileron.localEulerAngles = new Vector3(smoothAngle, rot.y, rot.z);
		}
	}

	void SetPlaneRotation()
	{
		model.localEulerAngles = new Vector3(currentPitchAngle, 0, currentRollAngle);
	}



	public void SetStartPos(PlayerStartPoint startPoint)
	{

		Coordinate coord = startPoint.coordinate.ConvertToRadians();
		currentElevation = Mathf.Lerp(minElevation, maxElevation, startPoint.elevationT);
		transform.position = GeoMaths.CoordinateToPoint(coord, worldRadius + currentElevation);
		posLastFrame = transform.position;

		// Needs to be called twice to settle (Todo: fix this nonsense)
		SetStartRot();
		SetStartRot();


		void SetStartRot()
		{
			Vector3 gravityUp = transform.position.normalized;
			transform.rotation = Quaternion.FromToRotation(transform.up, gravityUp);
			transform.LookAt((transform.position + transform.forward * 10).normalized * (worldRadius + currentElevation), gravityUp);
			transform.Rotate(transform.position.normalized, startPoint.angle, Space.World);

			UpdateRotation(0);
		}

	}

	void UpdatePositionHistory()
	{
		//Debug.Log((transform.position - lastRecordedPos).magnitude);
		if ((transform.position - lastRecordedPos).magnitude > recordPositionDstThreshold)
		{
			if (positionHistory.Count == maxHistorySize)
			{
				positionHistory.Dequeue();
			}
			positionHistory.Enqueue(transform.position);
			lastRecordedPos = transform.position;
		}

		// Update distance (on surface)
		float dstOnUnitSphere = GeoMaths.DistanceBetweenPointsOnUnitSphere(posLastFrame.normalized, transform.position.normalized);
		distanceTravelledKM += dstOnUnitSphere * GeoMaths.EarthRadiusKM;
		posLastFrame = transform.position;
	}


	void UpdateDevMode()
	{
		if (GameController.InDevMode)
		{
			if (debug_TestInitPos)
			{
				SetStartPos(startPoint);
			}
		}
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

	// ---- Public functions ----

	public void AddBoost(float time)
	{
		boostTimeToAdd += time;
	}

	public Package DropPackage()
	{
		Package package = Instantiate(packagePrefab, packageDropPoint.position, packageDropPoint.rotation);
		package.Init(worldLookup);
		packageDropped?.Invoke(package);
		return package;
	}

	public void SetPitch(float pitch)
	{
		currentPitchAngle = pitch;
		SetPlaneRotation();
	}

	// ---- Properties ----
	public Vector3 GravityUp
	{
		get
		{
			return (worldIsSpherical) ? transform.position.normalized : Vector3.up;
		}
	}

	public float Height
	{
		get
		{
			return worldRadius + currentElevation;
		}
	}

	public bool IsBoosting
	{
		get
		{
			return boosting;
		}
	}

	public bool BoosterIsCharging
	{
		get
		{
			return boostTimeToAdd > 0;
		}
	}

	// Get speed value remapped to [0,1] (0 at min speed, 1 at max speed. Note: still 1 when boosting).
	public float SpeedT
	{
		get
		{
			return Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
		}
	}

	public float TargetSpeedT
	{
		get
		{
			if (IsBoosting)
			{
				return 1;
			}
			return Mathf.InverseLerp(minSpeed, maxSpeed, baseTargetSpeed);
		}
	}

	public float BoostRemainingT
	{
		get
		{
			return Mathf.Clamp01(boostTimeRemaining / maxBoostTime);
		}
	}

	[System.Serializable]
	public struct PlayerStartPoint
	{
		public CoordinateDegrees coordinate;
		public float angle;
		[Range(0, 1)] public float elevationT;
	}
}
