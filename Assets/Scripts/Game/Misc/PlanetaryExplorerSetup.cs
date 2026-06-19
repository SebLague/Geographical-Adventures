using UnityEngine;

public class PlanetaryExplorerSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        // Only run in Game scene (where Player exists)
        Player player = FindObjectOfType<Player>();
        if (player == null) return;

        // Instantiate HUD if not present
        if (FindObjectOfType<PlanetaryHUD>() == null)
        {
            GameObject hudObj = new GameObject("PlanetaryHUD");
            var hud = hudObj.AddComponent<PlanetaryHUD>();
            hud.countryData = FindObjectOfType<GeoGame.Quest.QuestSystem>()?.countryData;
        }

        // Spawn artifacts using a delayed component to ensure world is loaded
        GameObject spawner = new GameObject("ArtifactSpawnerDelay");
        spawner.AddComponent<ArtifactSpawnerDelay>();
    }
}

public class ArtifactSpawnerDelay : MonoBehaviour
{
    void Update()
    {
        if (GameController.IsState(GameState.Playing))
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                SpawnTestArtifacts(player);
            }
            Destroy(gameObject);
        }
    }

    void SpawnTestArtifacts(Player player)
    {
        Vector3 playerPos = player.transform.position;
        Vector3 worldCenter = Vector3.zero;
        Vector3 gravityUp = playerPos.normalized;
        Vector3 forward = player.transform.forward;
        Vector3 right = player.transform.right;

        float worldRadius = player.heightSettings != null ? player.heightSettings.worldRadius : 1000f;

        // Offset coordinates on sphere surface
        Vector3[] spawnOffsets = new Vector3[] {
            forward * 4f + right * 2f,
            -forward * 5f + right * 4f,
            forward * 2f - right * 5f
        };

        string[] names = { "Plasma Gun", "Ancient Sword", "Quantum Scanner" };
        float[] weights = { 4.5f, 2.0f, 1.2f };
        Color[] colors = { Color.red, Color.yellow, Color.cyan };

        for (int i = 0; i < 3; i++)
        {
            Vector3 targetPos = (playerPos + spawnOffsets[i]).normalized * worldRadius;
            
            // Query exact terrain height at target
            if (player.worldLookup != null)
            {
                Coordinate coord = GeoMaths.PointToCoordinate(targetPos.normalized);
                float h = player.worldLookup.GetTerrainInfoImmediate(coord).height;
                targetPos = targetPos.normalized * (h + 1.5f); // Spawn slightly above ground
            }

            GameObject artObj = new GameObject(names[i]);
            artObj.transform.position = targetPos;
            artObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, targetPos.normalized);

            // Add simple visuals
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(artObj.transform, false);
            visual.transform.localScale = new Vector3(1.5f, 3f, 1.5f);
            visual.GetComponent<Renderer>().material.color = colors[i];

            // Add Collider & Trigger
            var col = artObj.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = new Vector3(1f, 2f, 1f);

            // ScriptableObject data
            ArtifactData data = ScriptableObject.CreateInstance<ArtifactData>();
            data.Name = names[i];
            data.Description = "A futuristic artifact scattered across the globe.";
            data.Weight = weights[i];
            data.HoldOffsetPosition = new Vector3(0.3f, 0.5f, 0.5f);
            data.HoldOffsetRotation = new Vector3(45f, 0f, 0f);

            var artifact = artObj.AddComponent<Artifact>();
            artifact.data = data;
        }

        Debug.Log("Successfully spawned 3 test artifacts near the player!");
    }
}
