using UnityEngine;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Entities.Enemies
{
    // Silahla ölen düşman: üniforma renklerinde kırıntılara dağılır (yukarı saçılıp düşer), ayağından toz kalkar.
    // Kırıntı parçacığı yemedeki ile aynı (Resources/FX/EatCrumbs); sabit havuz, ilk ölümde bir kez kurulur.
    // Sürü ölümünde çok sayıda ölüm aynı sistemlere eklenir (Emit), yeni nesne açılmaz.
    public static class DeathFx
    {
        private const int Count = 6;
        private static readonly Color DustA = new Color(0.72f, 0.75f, 0.78f);
        private static readonly Color DustB = new Color(0.95f, 0.94f, 0.92f);

        private static EatCrumbs[] s_pool;
        private static int s_next;

        public static void Play(Bounds bounds, Color a, Color b, bool heavy)
        {
            if (!Ensure()) return;
            float size = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z), 0.8f, 3f) * (heavy ? 1.4f : 1f);
            Next().PlayBurst(bounds.center, a, b, size, Mathf.RoundToInt((heavy ? 22f : 12f) * size), 3.2f, -10f);
            Vector3 foot = new Vector3(bounds.center.x, bounds.min.y + 0.1f, bounds.center.z);
            Next().PlayBurst(foot, DustA, DustB, size * 0.8f, heavy ? 10 : 6, 1.1f, 1.5f);
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
            Transform container = new GameObject("DeathFx").transform;
            s_pool = new EatCrumbs[Count];
            for (int i = 0; i < Count; i++) s_pool[i] = Object.Instantiate(prefab, container); // tek seferlik kurulum
            s_next = 0;
            return true;
        }
    }
}
