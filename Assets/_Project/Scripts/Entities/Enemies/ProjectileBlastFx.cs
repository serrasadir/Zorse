using UnityEngine;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Entities.Enemies
{
    // Patlayan mermilerin (helikopter mini roketi) küçük ateş patlaması: paylaşılan sabit havuz, ilk kullanımda
    // bir kez kurulur (Resources/FX/EatCrumbs). Hepsi meşgulse en eskisi yeniden kullanılır (Emit eklenir).
    public static class ProjectileBlastFx
    {
        private const int Count = 6;
        private static readonly Color Fire = new Color(0.96f, 0.64f, 0.36f);  // palet "orange"
        private static readonly Color Spark = new Color(0.96f, 0.77f, 0.27f); // palet "yellow"

        private static EatCrumbs[] s_pool;
        private static int s_next;

        public static void Play(Vector3 point, float radius)
        {
            if (!Ensure()) return;
            var bounds = new Bounds(point + Vector3.up * radius * 0.3f, new Vector3(radius, radius * 0.6f, radius));
            s_pool[s_next].Play(bounds, Fire, Spark, point + Vector3.up * 4f, radius * 1.4f);
            s_next = (s_next + 1) % s_pool.Length;
        }

        private static bool Ensure()
        {
            if (s_pool != null && s_pool[0] != null) return true;
            EatCrumbs prefab = Resources.Load<EatCrumbs>("FX/EatCrumbs");
            if (prefab == null) return false;
            Transform container = new GameObject("ProjectileBlasts").transform;
            s_pool = new EatCrumbs[Count];
            for (int i = 0; i < Count; i++) s_pool[i] = Object.Instantiate(prefab, container); // tek seferlik kurulum
            s_next = 0;
            return true;
        }
    }
}
