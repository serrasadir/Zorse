using UnityEngine;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    public abstract class UpgradeEffect : ScriptableObject
    {
        public abstract void Apply(GameObject blobRoot, UpgradeData data);

        // UpgradeSystem seviyeyi Apply'dan önce artırır: bu, az önce ulaşılan seviye (1..5).
        protected static int LevelOf(UpgradeData data) =>
            UpgradeSystem.Instance != null ? Mathf.Max(1, UpgradeSystem.Instance.GetLevel(data)) : 1;

        // Seviye tablosundan değer (dizi kısaysa son eleman).
        protected static float At(float[] table, int level) =>
            table == null || table.Length == 0 ? 0f : table[Mathf.Clamp(level - 1, 0, table.Length - 1)];

        protected static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T c = root.GetComponent<T>();
            return c != null ? c : root.AddComponent<T>();
        }
    }
}
