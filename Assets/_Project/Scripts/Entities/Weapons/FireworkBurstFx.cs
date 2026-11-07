using UnityEngine;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Entities.Weapons
{
    // Havai fişek efektleri: isabette renkli kıvılcım patlaması, atarın arkasından gri duman. Paylaşılan sabit havuz
    // (Resources/FX/EatCrumbs, ilk kullanımda bir kez kurulur); hepsi meşgulse en eskisine Emit eklenir.
    public static class FireworkBurstFx
    {
        private const int Count = 6;
        private static readonly Color SmokeA = new Color(0.72f, 0.75f, 0.78f); // palet "grey"
        private static readonly Color SmokeB = new Color(0.95f, 0.94f, 0.92f); // palet "white"

        private static EatCrumbs[] s_pool;
        private static int s_next;

        // size ≈ blob ölçeğine göre mermi boyu (WeaponBase BulletScale ile aynı mertebe).
        public static void Burst(Vector3 point, Color color, float size)
        {
            if (!Ensure()) return;
            Color light = Color.Lerp(color, Color.white, 0.55f);
            Next().PlayBurst(point, color, light, size, 26, 5f, -7f);
        }

        public static void Smoke(Vector3 point, float blobScale)
        {
            if (!Ensure()) return;
            float size = Mathf.Clamp(Mathf.Sqrt(blobScale / 0.5f), 1f, 3f);
            Next().PlayBurst(point, SmokeA, SmokeB, size * 0.9f, 7, 1.2f, 2.5f);
        }

        // Kurma bitti: fitilden küçük sarı-turuncu kıvılcım.
        public static void Spark(Vector3 point, float blobScale)
        {
            if (!Ensure()) return;
            float size = Mathf.Clamp(Mathf.Sqrt(blobScale / 0.5f), 1f, 3f);
            Next().PlayBurst(point, Spark1, Spark2, size * 0.6f, 6, 1.8f, -3f);
        }

        private static readonly Color Spark1 = new Color(0.96f, 0.77f, 0.27f); // palet "yellow"
        private static readonly Color Spark2 = new Color(0.96f, 0.64f, 0.36f); // palet "orange"

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
            Transform container = new GameObject("FireworkFx").transform;
            s_pool = new EatCrumbs[Count];
            for (int i = 0; i < Count; i++) s_pool[i] = Object.Instantiate(prefab, container); // tek seferlik kurulum
            s_next = 0;
            return true;
        }
    }
}
