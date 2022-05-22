using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobeController : MonoBehaviour
{
	public Camera cam;
	public float camDst = -55;

	public LayerMask globeMask;
	public float poleAngleLimit = 15;
	public float rotateSensitivity;
	public float zoomSensitivity = 4;
	public float fadeSpeed = 2;

	public Color highlightCol;

	public TMPro.TMP_Text countryNameDisplay;
	public Vector2 zoomMinMax;
	public float cursorHighlightRadius;

	[Header("References")]
	public Transform globe;
	public GlobeMapLoader mapLoader;
	public UnityEngine.UI.CanvasScaler canvasScaler;

	// Private stuff
	float angleX;
	float angleY;

	Color originalCountryCol;
	GameObject lastHighlightedCountry;
	GameObject oceanObject;

	string[] countryNames;
	Material[] countryMaterials;
	float[] countryHighlightStates;

	Dictionary<GameObject, int> countryIndexLookup;

	bool overrideTextDisplay;
	string overridenText;
	bool isZoomed;
	float targetZoom;
	float smoothZoomV;

	PlayerAction playerActions;

	void Awake()
	{
		playerActions = new PlayerAction();
	}

	void Start()
	{
		if (mapLoader.hasLoaded)
		{

			countryIndexLookup = new Dictionary<GameObject, int>();

			oceanObject = mapLoader.oceanObject;
			int numCountries = mapLoader.countryObjects.Length;
			countryNames = new string[numCountries];
			countryMaterials = new Material[numCountries];
			countryHighlightStates = new float[numCountries];

			for (int i = 0; i < numCountries; i++)
			{
				MeshRenderer renderer = mapLoader.countryObjects[i].renderer;
				countryMaterials[i] = renderer.sharedMaterial;
				countryNames[i] = renderer.gameObject.name;
				countryIndexLookup.Add(renderer.gameObject, i);
			}

			targetZoom = zoomMinMax.x;
			cam.fieldOfView = targetZoom;
		}
		else
		{
			Debug.LogError("Map loader has not yet loaded map");
		}
	}


	void Update()
	{

		if (GameController.IsState(GameState.ViewingMap))
		{
			HandleInput();
			Vector2 mousePos = Input.mousePosition;

			if (!Input.GetMouseButton(0))
			{
				HandleSelection();
			}
			UpdateRotation();


			cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetZoom, ref smoothZoomV, 0.2f);
		}

		UpdateHighlightState();

	}

	void HandleInput()
	{
		// Zoom
		float zoomInput = (playerActions.MapControls.MapZoom.ReadValue<float>());
		float newZoom = targetZoom - zoomInput * zoomSensitivity;
		// weird that min/max is inverted (todo: fix)
		targetZoom = Mathf.Clamp(newZoom, zoomMinMax.y, zoomMinMax.x);

		// Rotation
		Vector2 delta = playerActions.MapControls.Turn.ReadValue<Vector2>();
		angleX -= delta.x * rotateSensitivity;
		angleY += delta.y * rotateSensitivity;
		ClampAngleY();

	}



	void ClampAngleY()
	{
		angleY = Mathf.Clamp(angleY, -90 + poleAngleLimit, 90 - poleAngleLimit);
	}

	public void FramePlayer(Vector3 playerPos)
	{
		playerPos = playerPos.normalized;
		angleX = -Mathf.Atan2(playerPos.x, playerPos.z) * Mathf.Rad2Deg + 180;
		angleY = -Mathf.Asin(playerPos.y) * Mathf.Rad2Deg;
		ClampAngleY();
		UpdateRotation();
	}

	void UpdateRotation()
	{
		Quaternion globeRot = Quaternion.Euler(angleY, 0, 0) * Quaternion.Euler(0, angleX, 0);
		cam.transform.position = Quaternion.Inverse(globeRot) * Vector3.forward * camDst;
		cam.transform.LookAt(Vector3.zero);
	}

	void HandleSelection()
	{
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		GameObject selection = Raycast(ray, globeMask);
		overrideTextDisplay = false;

		if (selection != null && countryIndexLookup.ContainsKey(selection))
		{
			lastHighlightedCountry = selection;
			int countryIndex = countryIndexLookup[selection];
			countryHighlightStates[countryIndex] = 1;
		}
		else
		{
			if (selection != null)
			{
				// Used for package and target city markers
				overridenText = selection.name;
				overrideTextDisplay = true;

				if (lastHighlightedCountry != null)
				{
					int countryIndex = countryIndexLookup[lastHighlightedCountry];
					countryHighlightStates[countryIndex] = 1;
				}
			}
			else
			{
				lastHighlightedCountry = null;
			}
		}
	}

	Vector2 GetUIPos(Vector2 screenPos)
	{
		float x = (screenPos.x / Screen.width - 0.5f) * canvasScaler.referenceResolution.x;
		float y = (screenPos.y / Screen.height - 0.5f) * canvasScaler.referenceResolution.y;
		return new Vector2(x, y);
	}

	GameObject Raycast(Ray ray, LayerMask mask)
	{
		Vector2 mousePos = Input.mousePosition;
		RaycastHit hitInfo;

		if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, mask))
		{
			if (hitInfo.collider.gameObject == oceanObject)
			{
				// If hit the ocean, then check if is country nearby that can be selected (to make selection of small islands more forgiving)
				RaycastHit[] allHitInfo = Physics.SphereCastAll(ray, cursorHighlightRadius, hitInfo.distance, mask);
				GameObject bestHit = null;
				float closestDstToMouse = float.MaxValue;

				for (int i = 0; i < allHitInfo.Length; i++)
				{
					if (allHitInfo[i].collider.gameObject != oceanObject)
					{
						Vector2 screenSpaceHitPoint = cam.WorldToScreenPoint(allHitInfo[i].point);
						float dstFromCursor = (mousePos - screenSpaceHitPoint).magnitude;
						if (dstFromCursor < closestDstToMouse)
						{
							closestDstToMouse = dstFromCursor;
							bestHit = allHitInfo[i].collider.gameObject;
						}
					}
				}
				return bestHit;
			}
			else
			{
				return hitInfo.collider.gameObject;
			}
		}

		return null;
	}

	void UpdateHighlightState()
	{
		// Update highlight cols and highlighted country name
		int mostHighlightedIndex = 0;
		float mostHighlightedValue = 0;

		for (int i = 0; i < countryHighlightStates.Length; i++)
		{
			float easedT = Seb.Ease.Quadratic.In(countryHighlightStates[i]);
			countryMaterials[i].color = Color.Lerp(mapLoader.countryColours[i].colour, highlightCol, easedT);
			if (easedT > mostHighlightedValue)
			{
				mostHighlightedValue = easedT;
				mostHighlightedIndex = i;
			}

			countryHighlightStates[i] = Mathf.Clamp01(countryHighlightStates[i] - Time.unscaledDeltaTime * fadeSpeed);
		}

		countryNameDisplay.rectTransform.localPosition = GetUIPos(Input.mousePosition);
		countryNameDisplay.color = new Color(1, 1, 1, Mathf.InverseLerp(0.5f, 1, mostHighlightedValue));
		countryNameDisplay.text = countryNames[mostHighlightedIndex];

		if (overrideTextDisplay)
		{
			float textAlpha = Input.GetMouseButton(0) ? 0 : 1;
			countryNameDisplay.color = new Color(1, 1, 1, textAlpha);
			countryNameDisplay.text = overridenText;
		}
	}

	public void Open()
	{
		playerActions.MapControls.Enable();
	}

	public void Close()
	{
		playerActions.MapControls.Disable();
		for (int i = 0; i < countryHighlightStates.Length; i++)
		{
			countryHighlightStates[i] = 0;
		}
		UpdateHighlightState();
	}
}
