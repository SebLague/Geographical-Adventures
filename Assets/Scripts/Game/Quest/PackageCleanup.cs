using UnityEngine;

namespace GeoGame.Quest
{
    public class PackageCleanup : MonoBehaviour
    {
        QuestSystem questSystem;
        PackageDropInfo lastPackageDropInfo;

        void Awake() => questSystem = GetComponent<QuestSystem>();

        void OnEnable() => questSystem.packageDropped += OnPackageDropped;

        void OnDisable() => questSystem.packageDropped -= OnPackageDropped;

        void OnPackageDropped(PackageDropInfo packageDropInfo)
        {
            lastPackageDropInfo?.Destroy();
            lastPackageDropInfo = packageDropInfo;
        }
    }
}
