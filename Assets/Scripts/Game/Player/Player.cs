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
    public float maxPitchAngle = 30f;
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

    // Interaction & Hand Socket
    [Header("Interaction & Artifacts")]
    public Transform handSocket;
    public float interactRadius = 10f;
    public Artifact currentArtifact { get; private set; }

    // Private stuff
    GameCamera gameCamera;
    float baseTargetSpeed;
    bool boosting;
    float boostTimeRemaining;
    float boostTimeToAdd;
    float worldRadius;

    public float currentPitchAngle { get; private set; }
    public float currentRollAngle { get; private set; }
    public float turnInput { get; private set; }
    public float pitchInput { get; private set; }

    float recordPositionDstThreshold = 1;
    int maxHistorySize = 10000;
    public Queue<Vector3> positionHistory { get; private set; }
    Vector3 lastRecordedPos;
    Vector3 posLastFrame;

    public float distanceTravelledKM { get; private set; }

    void Awake()
    {
        gameCamera = FindObjectOfType<GameCamera>();
        worldRadius = heightSettings.worldRadius;

        // Hide flight components if they exist
        if (propeller != null) propeller.gameObject.SetActive(false);
        foreach (var a in ailerons) { if (a != null) a.gameObject.SetActive(false); }

        // Setup a simple procedural character visual inside the model container if needed
        SetupProceduralCharacter();

        // Socket setup
        if (handSocket == null)
        {
            GameObject socketObj = new GameObject("HandSocket");
            socketObj.transform.SetParent(model != null ? model : transform);
            socketObj.transform.localPosition = new Vector3(0.5f, 1f, 0.5f);
            socketObj.transform.localRotation = Quaternion.identity;
            handSocket = socketObj.transform;
        }

        SetStartPos(startPoint);

        baseTargetSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.5f);
        currentSpeed = baseTargetSpeed;

        positionHistory = new Queue<Vector3>(maxHistorySize);
        boostTimeRemaining = boostTimeAtStart;
    }

    void SetupProceduralCharacter()
    {
        // Add a capsule and sphere representing a character body inside the model Transform
        if (model != null)
        {
            // Remove existing aircraft meshes if any
            foreach (Transform child in model)
            {
                child.gameObject.SetActive(false);
            }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "CharacterBody";
            body.transform.SetParent(model);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "CharacterHead";
            head.transform.SetParent(model);
            head.transform.localPosition = new Vector3(0, 2.2f, 0);
            head.transform.localRotation = Quaternion.identity;
            head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            // Add simple colorful materials
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.material.color = new Color(0.1f, 0.6f, 0.8f);
            Renderer headRenderer = head.GetComponent<Renderer>();
            headRenderer.material.color = new Color(0.9f, 0.7f, 0.6f);
        }
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

        UpdateDevMode();
    }

    public void UpdateMovementInput(Vector2 moveInput, float accelerateDir, bool boosting)
    {
        turnInput = moveInput.x;
        pitchInput = moveInput.y;

        // In 3rd person, we just move based on input magnitude. No vehicle acceleration.
        float inputMag = Mathf.Clamp01(moveInput.magnitude);
        baseTargetSpeed = inputMag * maxSpeed; 

        this.boosting = boosting && boostTimeRemaining > 0;
    }

    void HandleInput()
    {
        HandleDebugInput();

        // Listen for Interact key (E) or touch equivalent
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (currentArtifact != null)
        {
            // Drop artifact
            DropArtifact();
        }
        else
        {
            // Try to pick up nearest artifact
            Artifact[] artifacts = FindObjectsOfType<Artifact>();
            Artifact nearest = null;
            float minDst = interactRadius;
            foreach (var art in artifacts)
            {
                if (art.IsCarried()) continue;
                float dst = Vector3.Distance(transform.position, art.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    nearest = art;
                }
            }

            if (nearest != null)
            {
                PickUpArtifact(nearest);
            }
        }
    }

    void PickUpArtifact(Artifact artifact)
    {
        currentArtifact = artifact;
        artifact.OnPickedUp(handSocket);
    }

    void DropArtifact()
    {
        if (currentArtifact == null) return;

        Vector3 dropPos = transform.position + transform.forward * 1.5f;
        Vector3 gravityUp = transform.position.normalized;
        
        // Snap to surface height
        Coordinate coord = GeoMaths.PointToCoordinate(dropPos.normalized);
        if (worldLookup != null)
        {
            var info = worldLookup.GetTerrainInfoImmediate(coord);
            dropPos = dropPos.normalized * info.height;
        }

        currentArtifact.OnDropped(dropPos, gravityUp);
        currentArtifact = null;
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
        if (debug_lockMovement) return;

        // Determine input direction relative to the Camera's orientation
        Vector3 gravityUp = transform.position.normalized;
        Vector3 camForward = Vector3.ProjectOnPlane(gameCamera.transform.forward, gravityUp).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(gameCamera.transform.right, gravityUp).normalized;

        Vector3 moveDirection = (camForward * pitchInput + camRight * turnInput).normalized;
        float inputMagnitude = Mathf.Clamp01(new Vector2(turnInput, pitchInput).magnitude);

        // Weight effect: reduce speed based on artifact weight
        float weightFactor = 1.0f;
        if (currentArtifact != null)
        {
            weightFactor = Mathf.Max(0.25f, 1.0f - (currentArtifact.data.Weight / 10.0f));
        }

        // Target speed drops to 0 if there's no input
        float targetSpeed = inputMagnitude * (boosting ? boostSpeed : maxSpeed) * weightFactor;
        
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 1 - Mathf.Pow(speedSmoothing, Time.deltaTime));

        if (currentSpeed > 0.1f && moveDirection.magnitude > 0.05f)
        {
            // Move player
            Vector3 nextPos = transform.position + moveDirection * currentSpeed * Time.deltaTime;
            
            // Snap to planet surface
            Coordinate coord = GeoMaths.PointToCoordinate(nextPos.normalized);
            float terrainHeight = worldRadius;
            if (worldLookup != null)
            {
                terrainHeight = worldLookup.GetTerrainInfoImmediate(coord).height;
            }
            
            transform.position = nextPos.normalized * terrainHeight;
            currentElevation = terrainHeight - worldRadius;

            // Rotate player to face movement direction
            Quaternion targetRot = Quaternion.LookRotation(moveDirection, gravityUp);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }
        else
        {
            // Just align with gravity when idle
            transform.rotation = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;
        }

        // Mock pitch and roll for animations/audio compatibility
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, 0f, Time.deltaTime * 5f);
        currentRollAngle = Mathf.Lerp(currentRollAngle, 0f, Time.deltaTime * 5f);
    }

    void UpdateBoostTimer()
    {
        if (boosting)
        {
            boostTimeRemaining = Mathf.Max(0, boostTimeRemaining - Time.deltaTime);
        }
        if (boostTimeToAdd > 0)
        {
            const float boostAddSpeed = 4;
            float boostTimeToAddThisFrame = Mathf.Min(Time.deltaTime * boostAddSpeed, boostTimeToAdd);
            boostTimeRemaining += boostTimeToAddThisFrame;
            boostTimeToAdd -= boostTimeToAddThisFrame;
            boostTimeRemaining = Mathf.Min(boostTimeRemaining, maxBoostTime);
        }
    }

    public void SetStartPos(PlayerStartPoint startPoint)
    {
        Coordinate coord = startPoint.coordinate.ConvertToRadians();
        
        float terrainHeight = worldRadius;
        if (worldLookup != null)
        {
            terrainHeight = worldLookup.GetTerrainInfoImmediate(coord).height;
        }
        currentElevation = terrainHeight - worldRadius;
        transform.position = GeoMaths.CoordinateToPoint(coord, worldRadius + currentElevation);
        posLastFrame = transform.position;

        Vector3 gravityUp = transform.position.normalized;
        transform.rotation = Quaternion.FromToRotation(transform.up, gravityUp);
        transform.Rotate(transform.position.normalized, startPoint.angle, Space.World);
    }

    void UpdatePositionHistory()
    {
        if ((transform.position - lastRecordedPos).magnitude > recordPositionDstThreshold)
        {
            if (positionHistory.Count == maxHistorySize)
            {
                positionHistory.Dequeue();
            }
            positionHistory.Enqueue(transform.position);
            lastRecordedPos = transform.position;
        }

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
    }

    public Vector3 GravityUp
    {
        get
        {
            return (worldIsSpherical) ? transform.position.normalized : Vector3.up;
        }
    }

    public Vector3 position
    {
        get
        {
            return transform.position;
        }
    }

    public Vector3 forward
    {
        get
        {
            return transform.forward;
        }
    }

    public Vector3 up
    {
        get
        {
            return transform.up;
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
