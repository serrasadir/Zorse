using System.Collections.Generic;
using System.Linq;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Yerdeki yenebilir küçük objeler (Blender: zorse-blender/build_litter.py → Art/City/Litter/J_*.fbx).
    // Her obje için prefab + ConsumableData kurar, ConsumableSpawner havuzunu bunlarla değiştirir
    // (eski gri küp Consumable_Trash havuzdan çıkar, dosyası durur). Tekrar çalıştırmak güvenli; sonra Cmd+S.
    public static class LitterSetup
    {
        private const string ArtFolder = CityKitPipeline.ArtRoot + "/Litter";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Consumables/Litter";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Consumables/Litter";
        private const string CityMaterialPath = CityKitPipeline.ArtRoot + "/Mat_CityPalette.mat";
        // Kendi paleti: şehir renkleri + pembe/kabuk. CityPalette'e renk eklenmez (40 = tam 8x5; 41. renk
        // satır sayısını değiştirip bütün şehrin UV'lerini kaydırıyor).
        private const string LitterPalettePath = ArtFolder + "/LitterPalette.png";
        private const string LitterMaterialPath = ArtFolder + "/Mat_LitterPalette.mat";

        // Ortalama kütle 2 = eski Consumable_Trash; ilerleme dengesi değişmesin.
        private static readonly (string mesh, string display, float mass)[] Items =
        {
            ("J_PaperBall", "Paper Ball", 1f),
            ("J_SodaCan", "Soda Can", 1.5f),
            ("J_Bottle", "Bottle", 1.5f),
            ("J_AppleCore", "Apple Core", 1.5f),
            ("J_BananaPeel", "Banana Peel", 2f),
            ("J_CoffeeCup", "Coffee Cup", 2f),
            ("J_Donut", "Donut", 2f),
            ("J_PizzaSlice", "Pizza Slice", 2f),
            ("J_Simit", "Simit", 2.5f),
            ("J_FriesBox", "Fries", 2.5f),
            ("J_Watermelon", "Watermelon", 2.5f),
            ("J_Burger", "Burger", 3f),
        };

        // Tiny blob 0.5 çap: çok ince objeler (pizza 0.06) de rahat yensin diye tetik kutusu en az bu kadar.
        private const float MinColliderSize = 0.25f;

        [MenuItem("Tools/Blob.io/Yerdeki Yiyecekleri Kur")]
        private static void Run() => Debug.Log("[LitterSetup]\n" + Apply());

        private static string Apply()
        {
            AssetDatabase.Refresh();
            var log = new System.Text.StringBuilder();
            Material palette = GetLitterMaterial();
            if (palette == null) return $"HATA: {CityMaterialPath} ya da {LitterPalettePath} yok";

            EnsureFolder(PrefabFolder);
            EnsureFolder(DataFolder);

            var allData = new List<ConsumableData>();
            foreach (var (mesh, display, mass) in Items)
            {
                Mesh source = LoadMesh(mesh, out Transform sourceTransform);
                if (source == null)
                {
                    log.AppendLine($"EKSİK {ArtFolder}/{mesh}.fbx");
                    continue;
                }

                string name = mesh.Substring(2); // J_Donut → Donut
                ConsumableData data = LoadOrCreateData($"{DataFolder}/Litter_{name}.asset");
                GameObject prefab = BuildPrefab($"{PrefabFolder}/Consumable_{name}.prefab", "Consumable_" + name,
                    source, sourceTransform, palette, data);

                SerializedObject so = new SerializedObject(data);
                so.FindProperty("_displayName").stringValue = display;
                so.FindProperty("_prefab").objectReferenceValue = prefab;
                so.FindProperty("_scoreValue").intValue = 1;
                so.FindProperty("_massValue").floatValue = mass;
                so.FindProperty("_requiredTier").intValue = (int)BlobTier.Tiny;
                so.FindProperty("_isHazard").boolValue = false;
                so.FindProperty("_hazardAmount").floatValue = 0f;
                so.FindProperty("_spawnYOffset").floatValue = 0f; // model pivotu tabanda
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                allData.Add(data);
                log.AppendLine($"OK {name}: kütle {mass}");
            }
            AssetDatabase.SaveAssets();

            ConsumableSpawner spawner = Object.FindAnyObjectByType<ConsumableSpawner>();
            if (spawner == null)
            {
                log.AppendLine("ATLANDI ConsumableSpawner: açık sahnede yok (GameScene'i açıp tekrar çalıştır)");
                return log.ToString();
            }

            SerializedObject spawnerSo = new SerializedObject(spawner);
            SerializedProperty pool = spawnerSo.FindProperty("_consumablePool");
            Undo.RecordObject(spawner, "Yerdeki yiyecek havuzu");
            pool.arraySize = allData.Count;
            for (int i = 0; i < allData.Count; i++)
                pool.GetArrayElementAtIndex(i).objectReferenceValue = allData[i];
            spawnerSo.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            log.AppendLine($"OK ConsumableSpawner havuzu: {string.Join(", ", allData.Select(d => d.name))}. Sahneyi kaydet (Cmd+S).");
            return log.ToString();
        }

        private static GameObject BuildPrefab(string path, string name, Mesh mesh, Transform sourceTransform,
            Material palette, ConsumableData data)
        {
            GameObject root = new GameObject(name);
            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = sourceTransform.rotation;
            model.transform.localScale = sourceTransform.lossyScale;
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = palette;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // havuzda 80 tane, çok alçak
            renderer.receiveShadows = true;

            Bounds b = ModelBounds(mesh, model.transform); // root orijinde, dönüşsüz
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 size = new Vector3(Mathf.Max(b.size.x, MinColliderSize), Mathf.Max(b.size.y, MinColliderSize),
                Mathf.Max(b.size.z, MinColliderSize));
            box.size = size;
            box.center = new Vector3(b.center.x, size.y * 0.5f, b.center.z);

            ConsumableBase consumable = root.AddComponent<ConsumableBase>();
            SerializedObject so = new SerializedObject(consumable);
            so.FindProperty("_data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static Material GetLitterMaterial()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LitterPalettePath);
            Material city = AssetDatabase.LoadAssetAtPath<Material>(CityMaterialPath);
            if (texture == null || city == null) return null;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(LitterMaterialPath);
            if (material == null)
            {
                material = new Material(city); // aynı shader/ayarlar, sadece doku farklı
                AssetDatabase.CreateAsset(material, LitterMaterialPath);
            }
            material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Bounds ModelBounds(Mesh mesh, Transform model)
        {
            Bounds local = mesh.bounds;
            Bounds result = new Bounds(model.TransformPoint(local.min), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x,
                    (i & 2) == 0 ? local.min.y : local.max.y, (i & 4) == 0 ? local.min.z : local.max.z);
                result.Encapsulate(model.TransformPoint(corner));
            }
            return result;
        }

        private static Mesh LoadMesh(string mesh, out Transform sourceTransform)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtFolder}/{mesh}.fbx");
            MeshFilter filter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            sourceTransform = filter != null ? filter.transform : null;
            return filter != null ? filter.sharedMesh : null;
        }

        private static ConsumableData LoadOrCreateData(string path)
        {
            ConsumableData data = AssetDatabase.LoadAssetAtPath<ConsumableData>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<ConsumableData>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
