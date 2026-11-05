using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.EditorTools;

// Yenebilir her şeyin oyundaki boyutunu (genişlik/derinlik/yükseklik, dünya birimi) blob boyuyla
// karşılaştırmak için dosyaya döker. Sahne objeleri yerleşim ölçeğiyle, diğerleri prefab'dan ölçülür.
// Elle: Tools → Blob.io → Yenebilir Boyut Raporu. Claude: Temp/tier_sizes.request oluşturur.
[InitializeOnLoad]
internal static class TierSizeReport
{
    private const string RequestPath = "Temp/tier_sizes.request";
    private const string ResultPath = "Temp/tier_sizes.txt";
    private static double s_nextPoll;

    static TierSizeReport() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < s_nextPoll) return;
        s_nextPoll = EditorApplication.timeSinceStartup + 1.0;
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        Write();
    }

    [MenuItem("Tools/Blob.io/Yenebilir Boyut Raporu")]
    private static void WriteFromMenu()
    {
        Write();
        Debug.Log($"[TierSizeReport] Rapor yazıldı: {Path.GetFullPath(ResultPath)}");
    }

    private class Entry
    {
        public string Kind, Name, Tier;
        public float Mass;
        public readonly List<Vector3> Sizes = new List<Vector3>(); // x = geniş kenar, y = yükseklik, z = dar kenar
    }

    private static void Write()
    {
        var entries = new Dictionary<string, Entry>();
        var measuredData = new HashSet<ConsumableData>();

        // 1) Sahnedeki yerleşik objeler (bina, eşya, park araba...) — kendi ölçekleriyle.
        foreach (var c in Resources.FindObjectsOfTypeAll<ConsumableBase>())
        {
            if (EditorUtility.IsPersistent(c) || !c.gameObject.scene.IsValid()) continue;
            var data = new SerializedObject(c).FindProperty("_data")?.objectReferenceValue as ConsumableData;
            string key = "scene:" + (data != null ? data.name : c.name);
            if (!entries.TryGetValue(key, out var e))
            {
                e = new Entry { Kind = "Sahne", Name = data != null ? data.name : c.name,
                                Tier = data != null ? data.RequiredTier.ToString() : "?", Mass = data != null ? data.MassValue : 0f };
                entries[key] = e;
            }
            if (data != null) measuredData.Add(data);
            if (TierRules.TryMeasure(c.transform, out var s)) e.Sizes.Add(s);
        }

        // 2) Havuzdan gelenler: yerdeki yiyecek, yaya (varyant mesh'leriyle), polis/elit/boss, araba.
        foreach (string guid in AssetDatabase.FindAssets("t:ConsumableData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<ConsumableData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null || data.Prefab == null || measuredData.Contains(data)) continue;
            var e = new Entry { Kind = data is PedestrianData ? "Yaya" : "Havuz", Name = data.name,
                                Tier = data.RequiredTier.ToString(), Mass = data.MassValue };
            if (data is PedestrianData pd && pd.VariantMeshes != null && pd.VariantMeshes.Length > 0)
            {
                foreach (var mesh in pd.VariantMeshes)
                    if (mesh != null) MeasurePrefab(data.Prefab, e, mesh);
            }
            else MeasurePrefab(data.Prefab, e, null);
            entries["data:" + data.name] = e;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:EnemyData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null || data.Prefab == null) continue;
            var e = new Entry { Kind = "Düşman", Name = data.name, Tier = data.RequiredConsumeTier.ToString(), Mass = 0f };
            MeasurePrefab(data.Prefab, e, null);
            entries["enemy:" + data.name] = e;
        }

        var car = AssetDatabase.FindAssets("t:CarData").Select(g => AssetDatabase.LoadAssetAtPath<CarData>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
        string carTier = car != null ? new SerializedObject(car).FindProperty("_consumableFromTier").enumNames[new SerializedObject(car).FindProperty("_consumableFromTier").enumValueIndex] : "?";
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/City/Cars" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            var e = new Entry { Kind = "Araba", Name = prefab.name, Tier = carTier };
            MeasurePrefab(prefab, e, null);
            entries["car:" + prefab.name] = e;
        }

        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("# Boyutlar dünya birimi. boy = TierRules.SizeOf, kural = boya göre tier. W = geniş yatay kenar, D = dar yatay kenar, H = yükseklik. Birden çok örnek varsa ortalama ve [min-max].");
        sb.AppendLine("kind\tname\ttier\tmass\tn\tW\tD\tH\tWmin\tWmax\tHmin\tHmax\tboy\tkural");
        foreach (var e in entries.Values.OrderBy(x => x.Kind).ThenBy(x => x.Name))
        {
            if (e.Sizes.Count == 0) { sb.AppendLine($"{e.Kind}\t{e.Name}\t{e.Tier}\t{e.Mass.ToString(inv)}\t0"); continue; }
            Vector3 avg = Vector3.zero;
            foreach (var s in e.Sizes) avg += s;
            avg /= e.Sizes.Count;
            string F(float v) => v.ToString("0.00", inv);
            sb.AppendLine(string.Join("\t", e.Kind, e.Name, e.Tier, e.Mass.ToString(inv), e.Sizes.Count,
                F(avg.x), F(avg.z), F(avg.y),
                F(e.Sizes.Min(s => s.x)), F(e.Sizes.Max(s => s.x)), F(e.Sizes.Min(s => s.y)), F(e.Sizes.Max(s => s.y)),
                F(TierRules.SizeOf(avg)), TierRules.TierForSize(TierRules.SizeOf(avg))));
        }
        Directory.CreateDirectory("Temp");
        File.WriteAllText(ResultPath, sb.ToString());
    }

    private static void MeasurePrefab(GameObject prefab, Entry e, Mesh variant)
    {
        if (TierRules.TryMeasurePrefab(prefab, variant, out var s)) e.Sizes.Add(s);
    }
}
