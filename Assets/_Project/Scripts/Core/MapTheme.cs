using UnityEngine;

namespace BlobSurvivor.Core
{
    // Haritaya özel görünüm (kale: ortaçağ). Şimdilik görünür silah: WeaponVisual "G_<Önek><Model>_<form>" mesh'ini ve
    // bu materyali kullanır (yoksa şehir mesh'i). CastleMap Awake'te kurar; şehirde boş.
    public static class MapTheme
    {
        public static string WeaponPrefix = "";
        public static Material WeaponMaterial;

        public static void Set(string weaponPrefix, Material weaponMaterial)
        {
            WeaponPrefix = weaponPrefix ?? "";
            WeaponMaterial = weaponMaterial;
        }

        public static void Clear() => Set("", null);
    }
}
