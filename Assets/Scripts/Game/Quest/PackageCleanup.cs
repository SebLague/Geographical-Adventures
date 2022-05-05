using UnityEngine;

namespace GeoGame.Quest
{
    public class PackageCleanup : MonoBehaviour
    {
        private QuestSystem questSystem;
        private PackageDropInfo lastPackageDropInfo;

        private void Awake() => questSystem = GetComponent<QuestSystem>();

        private void OnEnable() => questSystem.packageDropped += OnPackageDropped;

        private void OnDisable() => questSystem.packageDropped -= OnPackageDropped;

        private void OnPackageDropped(PackageDropInfo packageDropInfo)
        {
            lastPackageDropInfo?.Destroy();
            lastPackageDropInfo = packageDropInfo;
        }
    }
}