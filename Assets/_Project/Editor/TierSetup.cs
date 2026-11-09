using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Vehicles;

namespace BlobSurvivor.EditorTools
{
    // 7 tier'lık sistemi kurar: katman adları + Little (21) / Colossal (20) çarpışma satırları, sahnedeki tier eşikleri,
    // tüm yenebilirlerin tier'ı boydan (TierRules). Polis/elit tasarım gereği EnemyData'da kalır.
    // GameScene açıkken çalıştır, sonra Cmd+S. Tekrar çalıştırmak güvenli.
    [InitializeOnLoad]
    internal static class TierSetup
    {
        private const string RequestPath = "Temp/tier_setup.request";
        private const string ResultPath = "Temp/tier_setup.txt";
        private static double s_nextPoll;

        static TierSetup() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_nextPoll) return;
            s_nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            Run();
        }

        [MenuItem("Tools/Blob.io/Tier'ları Boya Göre Kur")]
        private static void Run()
        {
            var log = new StringBuilder();
            SetupLayer(log);
            SetupThresholds(log);
            AssignSceneObjects(log);
            AssignPooledData(log);
            AssignCars(log);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Temp");
            File.WriteAllText(ResultPath, log.ToString());
            Debug.Log($"[TierSetup] Bitti, sahneyi kaydet (Cmd+S). Ayrıntı: {Path.GetFullPath(ResultPath)}");
        }

        // Katman adları tier adıyla; sonradan eklenen katmanların çarpışma satırı komşu tier'dan kopyalanır.
        private static readonly (int layer, string name)[] LayerNames =
        {
            (9, "ConsumableTiny"), (TierLayers.LittleLayer, "ConsumableLittle"), (10, "ConsumableSmall"), (11, "ConsumableMedium"),
            (12, "ConsumableLarge"), (13, "ConsumableGiant"), (TierLayers.ColossalLayer, "ConsumableColossal"),
        };
        private static readonly Dictionary<int, int> CopyRowFrom = new Dictionary<int, int>
        {
            { TierLayers.ColossalLayer, 13 }, { TierLayers.LittleLayer, 10 },
        };

        private static void SetupLayer(StringBuilder log)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (var (layer, name) in LayerNames)
                layers.GetArrayElementAtIndex(layer).stringValue = name;
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            foreach (var kv in CopyRowFrom)
                for (int j = 0; j < 32; j++)
                {
                    int src = CopyRowFrom.TryGetValue(j, out int s) ? s : j;
                    Physics.IgnoreLayerCollision(kv.Key, j, Physics.GetIgnoreLayerCollision(kv.Value, src));
                }
            log.AppendLine("Katmanlar: " + string.Join(", ", LayerNames.Select(l => $"{l.layer}={l.name}")));
        }

        private static void SetupThresholds(StringBuilder log)
        {
            var growth = Object.FindAnyObjectByType<BlobGrowth>(FindObjectsInactive.Include);
            if (growth == null) { log.AppendLine("UYARI: sahnede BlobGrowth yok (GameScene açık mı?)"); return; }
            var so = new SerializedObject(growth);
            var arr = so.FindProperty("_tierThresholds");
            arr.arraySize = TierRules.Thresholds.Length;
            for (int i = 0; i < TierRules.Thresholds.Length; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("Tier").intValue = i + 1;
                el.FindPropertyRelative("MassRequired").floatValue = TierRules.Thresholds[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(growth.gameObject.scene);
            log.AppendLine("Eşikler: " + string.Join(" / ", TierRules.Thresholds));
        }

        // Şehir objeleri: sahnedeki yerleşik hâlleriyle ölçülür; tier + kütle (TierRules.MassByTier) + katman.
        private static void AssignSceneObjects(StringBuilder log)
        {
            var groups = new Dictionary<ConsumableData, List<ConsumableBase>>();
            foreach (var c in Resources.FindObjectsOfTypeAll<ConsumableBase>())
            {
                if (EditorUtility.IsPersistent(c) || !c.gameObject.scene.IsValid()) continue;
                var data = new SerializedObject(c).FindProperty("_data")?.objectReferenceValue as ConsumableData;
                if (data == null || data is PedestrianData) continue;
                if (!groups.TryGetValue(data, out var list)) groups[data] = list = new List<ConsumableBase>();
                list.Add(c);
            }

            log.AppendLine("\n# Şehir objeleri (sahne)");
            foreach (var kv in groups.OrderBy(k => k.Key.name))
            {
                float size = 0f; int n = 0;
                foreach (var c in kv.Value)
                    if (TierRules.TryMeasure(c.transform, out var s)) { size += TierRules.SizeOf(s); n++; }
                if (n == 0) continue;
                size /= n;
                BlobTier tier = TierRules.TierFor(kv.Key.name, size);
                bool isCity = kv.Key.name.StartsWith("City_");
                SetData(kv.Key, tier, isCity ? TierRules.MassFor(tier) : (float?)null, log, size);

                int layer = TierLayers.For(tier);
                var prefabs = new HashSet<GameObject>();
                foreach (var c in kv.Value)
                {
                    if (c.gameObject.layer != layer) { c.gameObject.layer = layer; EditorSceneManager.MarkSceneDirty(c.gameObject.scene); }
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject);
                    if (src != null && src.transform.parent == null) prefabs.Add(src);
                }
                foreach (var p in prefabs)
                    if (p.layer != layer) { p.layer = layer; EditorUtility.SetDirty(p); }
            }
        }

        // Havuzdan gelenler (yerdeki yiyecek, yaya): sadece tier; kütle tasarım değeri olarak kalır.
        private static void AssignPooledData(StringBuilder log)
        {
            log.AppendLine("\n# Havuz (prefab)");
            foreach (string guid in AssetDatabase.FindAssets("t:ConsumableData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<ConsumableData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null || data.Prefab == null || data.name.StartsWith("City_")) continue;
                float size = 0f; bool any = false;
                var variants = (data as PedestrianData)?.VariantMeshes;
                if (variants != null && variants.Length > 0)
                {
                    foreach (var m in variants)
                        if (m != null && TierRules.TryMeasurePrefab(data.Prefab, m, out var s)) { size = Mathf.Max(size, TierRules.SizeOf(s)); any = true; }
                }
                else if (TierRules.TryMeasurePrefab(data.Prefab, null, out var s)) { size = TierRules.SizeOf(s); any = true; }
                if (any) SetData(data, TierRules.TierForSize(size), null, log, size);
            }
        }

        private static void AssignCars(StringBuilder log)
        {
            log.AppendLine("\n# Hareketli arabalar");
            var spawner = Object.FindAnyObjectByType<CarSpawner>(FindObjectsInactive.Include);
            if (spawner == null) { log.AppendLine("UYARI: sahnede CarSpawner yok"); return; }
            var sso = new SerializedObject(spawner);
            var carData = sso.FindProperty("_carData").objectReferenceValue as CarData;
            if (carData == null) { log.AppendLine("UYARI: CarData yok"); return; }

            var cso = new SerializedObject(carData);
            cso.FindProperty("_consumableFromTier").intValue = (int)BlobTier.Medium;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var prefabs = sso.FindProperty("_carPrefabs");
            for (int i = 0; i < prefabs.arraySize; i++)
            {
                var car = prefabs.GetArrayElementAtIndex(i).objectReferenceValue as CarController;
                if (car == null || !TierRules.TryMeasurePrefab(car.gameObject, null, out var s)) continue;
                float size = TierRules.SizeOf(s);
                BlobTier tier = TierRules.TierForSize(size);
                bool isDefault = tier == BlobTier.Medium;
                var so = new SerializedObject(car);
                so.FindProperty("_tierOverride").intValue = isDefault ? 0 : (int)tier;
                so.FindProperty("_massOverride").floatValue = isDefault ? 0f : TierRules.MassFor(tier);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(car);
                log.AppendLine($"{car.name}\tboy {size:0.00}\t{tier}{(isDefault ? $" (CarData, kütle {carData.MassValue})" : $", kütle {TierRules.MassFor(tier)}")}");
            }
            EditorUtility.SetDirty(carData);
        }

        private static void SetData(ConsumableData data, BlobTier tier, float? mass, StringBuilder log, float size)
        {
            var so = new SerializedObject(data);
            var tierProp = so.FindProperty("_requiredTier");
            BlobTier old = (BlobTier)tierProp.intValue;
            tierProp.intValue = (int)tier;
            if (mass.HasValue)
            {
                so.FindProperty("_massValue").floatValue = mass.Value;
                so.FindProperty("_scoreValue").intValue = (int)mass.Value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            log.AppendLine($"{data.name}\tboy {size:0.00}\t{old} → {tier}{(old != tier ? "  *" : "")}");
        }
    }
}
