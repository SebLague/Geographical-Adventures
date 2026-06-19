using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(200)]
public class PlanetaryHUD : MonoBehaviour
{
    public CountryData countryData;
    
    private Player player;
    private WorldLookup worldLookup;
    private GeoGame.Quest.QuestSystem questSystem;

    // UI elements created dynamically
    private GameObject hudContainer;
    private TMP_Text regionText;
    private TMP_Text coordsText;
    private TMP_Text compassText;
    private TMP_Text objectiveText;
    private TMP_Text artifactText;
    private Image artifactIconImage;
    
    private Image hudBackground;
    private CanvasGroup canvasGroup;

    void Start()
    {
        player = FindObjectOfType<Player>();
        worldLookup = FindObjectOfType<WorldLookup>();
        questSystem = FindObjectOfType<GeoGame.Quest.QuestSystem>();

        // Keep old overlay UI enabled so Quest UI functions properly
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && uiManager.hudGroup != null)
        {
            // uiManager.hudGroup.gameObject.SetActive(false); // DO NOT DISABLE
        }

        CreateHUDLayout();
    }

    void CreateHUDLayout()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Container
        hudContainer = new GameObject("PlanetaryHUD_Container");
        hudContainer.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = hudContainer.AddComponent<RectTransform>();
        // Top-left anchor with some padding
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(350, 240);

        canvasGroup = hudContainer.AddComponent<CanvasGroup>();

        // Background Image
        hudBackground = hudContainer.AddComponent<Image>();
        hudBackground.color = new Color(0.04f, 0.08f, 0.12f, 0.85f); // Sleek dark blue
        
        // Add a subtle border using outline or another panel
        Outline outline = hudContainer.AddComponent<Outline>();
        outline.effectColor = new Color(0.0f, 0.8f, 1.0f, 0.6f); // Cyan glow border
        outline.effectDistance = new Vector2(2, 2);

        // Vertial Layout Group for padding and stacking
        VerticalLayoutGroup layout = hudContainer.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        // Fonts and Text creation helper
        GameObject CreateText(string name, string initText, int fontSize = 14, bool isBold = false)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(hudContainer.transform, false);
            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = initText;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            if (isBold)
            {
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.0f, 0.9f, 1.0f, 1.0f); // Bright Cyan
            }
            return textObj;
        }

        CreateText("TitleText", "PLANETARY SURVEYOR HUD", 16, true);
        regionText = CreateText("RegionText", "Region: Scanning...", 14).GetComponent<TMP_Text>();
        coordsText = CreateText("CoordsText", "Coordinates: Lat: 0.00 Lon: 0.00", 12).GetComponent<TMP_Text>();
        compassText = CreateText("CompassText", "Heading: 0° N", 13).GetComponent<TMP_Text>();
        artifactText = CreateText("ArtifactText", "Artifact: None", 13).GetComponent<TMP_Text>();
        
        // Artifact Icon Image
        GameObject iconObj = new GameObject("ArtifactIcon");
        iconObj.transform.SetParent(hudContainer.transform, false);
        artifactIconImage = iconObj.AddComponent<Image>();
        artifactIconImage.rectTransform.sizeDelta = new Vector2(32, 32);
        artifactIconImage.color = Color.clear; // Hide by default
        
        // Orange glow accent for objectives
        GameObject objHeader = CreateText("ObjectiveHeader", "ACTIVE OBJECTIVES:", 12, true);
        objHeader.GetComponent<TMP_Text>().color = new Color(1.0f, 0.5f, 0.0f, 1.0f); 
        
        objectiveText = CreateText("ObjectiveText", "None", 12).GetComponent<TMP_Text>();
        objectiveText.color = new Color(0.9f, 0.9f, 0.9f, 1.0f);

        // Fade-in animation
        canvasGroup.alpha = 0f;
        StartCoroutine(FadeInHUD());
    }

    IEnumerator FadeInHUD()
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    void Update()
    {
        if (hudContainer == null)
        {
            CreateHUDLayout();
            if (hudContainer == null) return;
        }

        if (player == null || worldLookup == null) return;

        bool active = GameController.IsState(GameState.Playing);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, active ? 1 : 0, Time.deltaTime * 3f);
        }

        if (!active) return;

        UpdateRegionAndCoords();
        UpdateCompass();
        UpdateArtifactStatus();
        UpdateObjectives();
    }

    void UpdateRegionAndCoords()
    {
        Vector3 pos = player.position;
        Coordinate coord = GeoMaths.PointToCoordinate(pos.normalized);
        CoordinateDegrees deg = coord.ConvertToDegrees();

        string latSuffix = deg.latitude >= 0 ? "N" : "S";
        string lonSuffix = deg.longitude >= 0 ? "E" : "W";
        coordsText.text = $"Coordinates: {Mathf.Abs(deg.latitude):F2}° {latSuffix}, {Mathf.Abs(deg.longitude):F2}° {lonSuffix}";

        // Get Region Info
        var info = worldLookup.GetTerrainInfoImmediate(coord);
        if (info.inOcean)
        {
            regionText.text = "Region: International Waters (Ocean)";
        }
        else if (countryData != null && info.countryIndex >= 0 && info.countryIndex < countryData.Countries.Length)
        {
            var country = countryData.Countries[info.countryIndex];
            regionText.text = $"Region: {country.name} ({country.continent})";
        }
        else
        {
            regionText.text = "Region: Landmass";
        }
    }

    void UpdateCompass()
    {
        Vector3 pos = player.position;
        Vector3 forward = player.forward;

        // Calculate simplified angle relative to North
        Vector3 northDir = new Vector3(-pos.x * pos.y, 1 - pos.y * pos.y, -pos.z * pos.y).normalized;
        float angle = Vector3.SignedAngle(forward, northDir, -pos.normalized);
        if (angle < 0) angle += 360f;

        string dirStr = "N";
        if (angle >= 337.5f || angle < 22.5f) dirStr = "N";
        else if (angle >= 22.5f && angle < 67.5f) dirStr = "NE";
        else if (angle >= 67.5f && angle < 112.5f) dirStr = "E";
        else if (angle >= 112.5f && angle < 157.5f) dirStr = "SE";
        else if (angle >= 157.5f && angle < 202.5f) dirStr = "S";
        else if (angle >= 202.5f && angle < 247.5f) dirStr = "SW";
        else if (angle >= 247.5f && angle < 292.5f) dirStr = "W";
        else if (angle >= 292.5f && angle < 337.5f) dirStr = "NW";

        compassText.text = $"Compass: {dirStr} ({Mathf.RoundToInt(angle)}°)";
    }

    void UpdateArtifactStatus()
    {
        if (player.currentArtifact != null)
        {
            artifactText.text = $"Carried Item: <color=#FFA500>{player.currentArtifact.data.Name}</color> ({player.currentArtifact.data.Weight} kg)";
            if (player.currentArtifact.data.Icon != null)
            {
                artifactIconImage.sprite = player.currentArtifact.data.Icon;
                artifactIconImage.color = Color.white;
            }
            else
            {
                artifactIconImage.color = new Color(1, 0.5f, 0, 0.8f); // Placeholder orange block
            }
        }
        else
        {
            artifactText.text = "Carried Item: None <size=10>(Press E to interact)</size>";
            artifactIconImage.color = Color.clear;
        }
    }

    void UpdateObjectives()
    {
        if (questSystem == null) return;
        
        string quests = "";
        // Read active quests from QuestSystem dynamically if possible
        // Let's assume we can fetch them or show generic status
        objectiveText.text = "Deliver packages to target regions.";
    }
}
