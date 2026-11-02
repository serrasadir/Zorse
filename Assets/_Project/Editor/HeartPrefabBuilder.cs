using System.IO;
using BlobSurvivor.Entities.Loot;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.EditorTools
{
    // 2026-11-02: Kalp toplanabilirinin prefab'ı. Görsel Art/City/C_Heart.fbx (low poly, şehir paleti).
    // Prefab'ı üretip polis (%5) ve elit (%100) EnemyData'larına bağlar.
    // Tekrar çalıştırmak prefab'ı yeniden üretir. Temp/heart_prefab.request varsa kendiliğinden çalışır.
    public static class HeartPrefabBuilder
    {
        private const string RequestPath = "Temp/heart_prefab.request";
        private const string ResultPath = "Temp/heart_prefab.result";
        private const string PrefabPath = "Assets/_Project/Prefabs/Heart.prefab";
        private const string PaletteMaterialPath = CityKitPipeline.ArtRoot + "/Mat_CityPalette.mat";
        private const string HeartMeshPath = CityKitPipeline.ArtRoot + "/C_Heart.fbx";
        private const string EnemyDataRoot = "Assets/_Project/ScriptableObjects/Enemies";

        [InitializeOnLoadMethod]
        private static void RunPendingRequest()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                File.Delete(RequestPath);
                File.WriteAllText(ResultPath, EditorApplication.isPlayingOrWillChangePlaymode ? "ABORTED: Play Mode açık" : Build());
            };
        }

        [MenuItem("Tools/Blob.io/Kalp Prefab'ını Kur")]
        private static void BuildFromMenu() => Debug.Log("[HeartPrefabBuilder] " + Build());

        private static string Build()
        {
            Material material = GetMaterial();
            if (material == null) return $"HATA: {PaletteMaterialPath} bulunamadı";

            GameObject root = new GameObject("Heart");
            try
            {
                BoxCollider trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(0.8f, 0.8f, 0.8f);

                // Kalp kameraya doğru dik durur (kamera 60° eğik, +Z'ye bakar); sivri ucu aşağıda.
                Transform model = new GameObject("Model").transform;
                model.SetParent(root.transform, false);
                model.localRotation = Quaternion.Euler(-30f, 0f, 0f);

                if (!AddHeartMesh(model, material)) return $"HATA: {HeartMeshPath} yüklenemedi";

                HeartPickup pickup = root.AddComponent<HeartPickup>();
                SerializedObject so = new SerializedObject(pickup);
                so.FindProperty("_model").objectReferenceValue = model;
                so.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                HeartPickup prefabPickup = prefab.GetComponent<HeartPickup>();

                string police = AssignDrop("EnemyData_Police", prefabPickup, 0.05f);
                string elite = AssignDrop("EnemyData_ElitePolis", prefabPickup, 1f);
                AssetDatabase.SaveAssets();
                return $"OK: {PrefabPath}; {police}; {elite}";
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Blender'daki C_Heart (build_city.py heart()): dik, pivot ortada. "Model" objesinin -30° eğimi korunur
        // çünkü HeartPickup yanıp sönmeyi o objeyle yapar.
        private static bool AddHeartMesh(Transform model, Material material)
        {
            MeshFilter source = CityKitPipeline.LoadSourceMesh("C_Heart");
            if (source == null) return false;

            GameObject mesh = new GameObject("Mesh");
            mesh.transform.SetParent(model, false);
            CityKitPipeline.ApplyModelTransform(mesh.transform, source, "C_Heart");
            mesh.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;

            MeshRenderer renderer = mesh.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; // havuzda birden fazla kalp, coin ile aynı
            return true;
        }

        // Şehir paleti: kalp polis/coin ile aynı materyali paylaşır (ek draw call / doku yok).
        private static Material GetMaterial() => AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);

        private static string AssignDrop(string dataName, HeartPickup prefab, float chance)
        {
            string path = $"{EnemyDataRoot}/{dataName}.asset";
            Object data = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (data == null) return $"{dataName} bulunamadı";

            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_heartPrefab").objectReferenceValue = prefab;
            so.FindProperty("_heartDropChance").floatValue = chance;
            so.ApplyModifiedPropertiesWithoutUndo();
            return $"{dataName}=%{chance * 100f:F0}";
        }
    }
}
