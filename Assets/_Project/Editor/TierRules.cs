using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.EditorTools
{
    // Tier'ı boydan hesaplayan tek kural. Obje, blob o tier'a yeni girdiğindeki çapından en fazla
    // Tolerance kadar büyükse o tier'da yenir. Boy = kenarların geometrik ortalaması; uzun ince
    // objelerde (direk, lamba) en uzun kenarın yarısı daha büyükse o.
    internal static class TierRules
    {
        public const float Tolerance = 1.05f;
        public static readonly float[] Thresholds = { 0f, 15f, 50f, 200f, 650f, 1100f, 1650f }; // Tiny..Colossal
        public static readonly float[] MassByTier = { 2f, 3f, 4f, 10f, 25f, 50f, 80f };        // şehir objeleri

        // BlobGrowth: ölçek = 0.5 × (1 + kütle × 0.1) ^ 0.45; gövde çapı = ölçek.
        private const float BaseScale = 0.5f, GrowthFactor = 0.1f, GrowthExponent = 0.45f;

        public static float BlobDiameter(float mass) => BaseScale * Mathf.Pow(1f + mass * GrowthFactor, GrowthExponent);

        public static float SizeOf(Vector3 wdh) // x = geniş kenar, y = yükseklik, z = dar kenar
        {
            float gm = Mathf.Pow(Mathf.Max(1e-4f, wdh.x * wdh.y * wdh.z), 1f / 3f);
            return Mathf.Max(gm, Mathf.Max(wdh.x, wdh.y) * 0.5f);
        }

        public static BlobTier TierForSize(float size)
        {
            for (int i = 0; i < Thresholds.Length; i++)
                if (size <= BlobDiameter(Thresholds[i]) * Tolerance) return (BlobTier)(i + 1);
            return TierLayers.MaxTier;
        }

        public static float MassFor(BlobTier tier) => MassByTier[Mathf.Clamp((int)tier - 1, 0, MassByTier.Length - 1)];

        // Kökün kendi yönünde (yaw'dan bağımsız) tüm görünür mesh'leri saran kutu. Pasif objelerde de çalışır.
        public static bool TryMeasure(Transform root, out Vector3 wdh)
        {
            wdh = Vector3.zero;
            bool any = false;
            Bounds b = default;
            Quaternion inv = Quaternion.Inverse(root.rotation);
            void Add(Mesh mesh, Matrix4x4 m)
            {
                Bounds mb = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = inv * (m.MultiplyPoint3x4(corner) - root.position);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null) continue;
                string n = mf.name.ToLowerInvariant();
                if (n.Contains("shadow") || n.Contains("gölge") || n.Contains("ghost")) continue;
                Add(mf.sharedMesh, mf.transform.localToWorldMatrix);
            }
            foreach (var sk in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sk.sharedMesh != null) Add(sk.sharedMesh, sk.transform.localToWorldMatrix);
            if (!any) return false;
            wdh = new Vector3(Mathf.Max(b.size.x, b.size.z), b.size.y, Mathf.Min(b.size.x, b.size.z));
            return true;
        }

        public static bool TryMeasurePrefab(GameObject prefab, Mesh variant, out Vector3 wdh)
        {
            var go = Object.Instantiate(prefab);
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (variant != null)
                {
                    var mf = go.GetComponentInChildren<MeshFilter>(true);
                    if (mf != null) mf.sharedMesh = variant;
                }
                return TryMeasure(go.transform, out wdh);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
