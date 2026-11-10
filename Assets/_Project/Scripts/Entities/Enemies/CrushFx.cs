using UnityEngine;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Entities.Enemies
{
    // Tankın ezdiği şehir eşyası: objenin palet renklerinde kırıntılar saçılıp yere düşer, altından gri toz kalkar.
    // Paylaşılan sabit havuz (Resources/FX/EatCrumbs, ilk kullanımda bir kez kurulur); hepsi meşgulse en eskisine eklenir.
    public static class CrushFx
    {
        private const int Count = 4;
        private static readonly Color DustA = new Color(0.72f, 0.75f, 0.78f); // palet "grey"
        private static readonly Color DustB = new Color(0.95f, 0.94f, 0.92f); // palet "white"

        private static EatCrumbs[] s_pool;
        private static int s_next;

        public static void Play(Bounds bounds, Color a, Color b)
        {
            if (!Ensure()) return;
            float size = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z), 1f, 4f);
            Next().PlayBurst(bounds.center, a, b, size, Mathf.RoundToInt(16f * size), 3.5f, -9f);
            Vector3 foot = new Vector3(bounds.center.x, bounds.min.y + 0.1f, bounds.center.z);
            Next().PlayBurst(foot, DustA, DustB, size * 0.8f, 8, 1.2f, 1.5f);
        }

        private static EatCrumbs Next()
        {
            EatCrumbs fx = s_pool[s_next];
            s_next = (s_next + 1) % s_pool.Length;
            return fx;
        }

        private static bool Ensure()
        {
            if (s_pool != null && s_pool[0] != null) return true;
            EatCrumbs prefab = Resources.Load<EatCrumbs>("FX/EatCrumbs");
            if (prefab == null) return false;
            Transform container = new GameObject("CrushFx").transform;
            s_pool = new EatCrumbs[Count];
            for (int i = 0; i < Count; i++) s_pool[i] = Object.Instantiate(prefab, container); // tek seferlik kurulum
            s_next = 0;
            return true;
        }
    }
}
