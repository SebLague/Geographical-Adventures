using UnityEngine;

namespace GeoGame.Quest
{
    public class PackageDropInfo
    {
        private readonly Package package;
        private readonly CityMarker cityMarker;

        public PackageDropInfo(Package package, CityMarker cityMarker)
        {
            this.package = package;
            this.cityMarker = cityMarker;
        }

        public void Destroy()
        {
            Object.Destroy(package.gameObject);
            Object.Destroy(cityMarker.gameObject);
        }
    }
}