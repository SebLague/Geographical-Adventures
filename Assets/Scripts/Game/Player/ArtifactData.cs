using UnityEngine;

[CreateAssetMenu(fileName = "NewArtifact", menuName = "PlanetaryExplorer/Artifact Data")]
public class ArtifactData : ScriptableObject
{
    public string Name;
    public string Description;
    public float Weight;
    public Sprite Icon;
    public Vector3 HoldOffsetPosition;
    public Vector3 HoldOffsetRotation;
}
