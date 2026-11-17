using BlobSurvivor.Data;
using BlobSurvivor.Entities.Enemies;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace BlobSurvivor.EditorTools
{
    // Robot polis köpeği: erken oyunun sürü düşmanı (VS'deki yarasa rolü). Polis prefab'ından kopyalanır,
    // modeli Art/City/H_RobotDog.fbx (zorse-blender/build_robodog.py). Tekrar çalıştırmak güvenli: değerleri yeniden yazar.
    internal static class RobotDogSetup
    {
        private const string EnemyFolder = "Assets/_Project/Prefabs/Enemies";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Enemies";
        private const string PolicePrefab = EnemyFolder + "/Enemy_Police.prefab";
        private const string DogPrefab = EnemyFolder + "/Enemy_RobotDog.prefab";
        private const string PoliceData = DataFolder + "/EnemyData_Police.asset";
        private const string DogData = DataFolder + "/EnemyData_RobotDog.asset";
        private const string WaveFolder = "Assets/_Project/ScriptableObjects/Waves";

        [MenuItem("Tools/Blob.io/Robot Köpeği Kur")]
        private static void Setup()
        {
            AssetDatabase.Refresh();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(DogPrefab) == null && !AssetDatabase.CopyAsset(PolicePrefab, DogPrefab))
            {
                Debug.LogError("[RobotDogSetup] Prefab kopyalanamadı.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(DogData) == null && !AssetDatabase.CopyAsset(PoliceData, DogData))
            {
                Debug.LogError("[RobotDogSetup] EnemyData kopyalanamadı.");
                return;
            }
            AssetDatabase.SaveAssets();

            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(DogData);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DogPrefab);

            // Model: polisle aynı yükseklik (kök yol + 0.6).
            if (!CityKitPipeline.ApplyEnemyModel(DogPrefab, "H_RobotDog", -CityKitPipeline.PoliceRootY, GetDogMaterial()))
            {
                Debug.LogError("[RobotDogSetup] H_RobotDog modeli takılamadı (Art/City/H_RobotDog.fbx var mı?).");
                return;
            }

            // Gövde: polis kapsülü (r 0.5, boy 2) köpeğe göre küçülür; kök yerden 0.6 yukarıda.
            GameObject contents = PrefabUtility.LoadPrefabContents(DogPrefab);
            contents.name = "Enemy_RobotDog";
            if (contents.TryGetComponent(out CapsuleCollider capsule))
            {
                capsule.radius = 0.3f;
                capsule.height = 0.75f;
                capsule.center = new Vector3(0f, -0.25f, 0f);
            }
            if (contents.TryGetComponent(out NavMeshAgent agent))
            {
                agent.radius = 0.3f;
                agent.height = 0.7f;
            }
            Transform bar = contents.transform.Find("HealthBar");
            if (bar != null)
            {
                bar.localPosition = new Vector3(0f, 0.3f, 0f);
                bar.localScale = Vector3.one * 0.7f;
            }
            var enemy = contents.GetComponent<EnemyBase>();
            var so = new SerializedObject(enemy);
            so.FindProperty("_data").objectReferenceValue = data;
            so.FindProperty("_separationRadius").floatValue = 1f;
            so.FindProperty("_detectionRange").floatValue = 40f; // yarasa rolü: doğduğu andan sana koşar (doğma penceresi ±30)
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, DogPrefab);
            PrefabUtility.UnloadPrefabContents(contents);

            // Değerler: az can (sopanın ilk vuruşu öldürür), hızlı ama blob'dan yavaş, Little'da yenir, 3'lü sürü.
            var d = new SerializedObject(data);
            d.FindProperty("_displayName").stringValue = "RobotDog";
            d.FindProperty("_prefab").objectReferenceValue = prefab;
            d.FindProperty("_maxHealth").floatValue = 10f;
            d.FindProperty("_damage").floatValue = 4f;
            d.FindProperty("_moveSpeed").floatValue = 3.4f;
            d.FindProperty("_attackRange").floatValue = 1.2f;
            d.FindProperty("_attackCooldown").floatValue = 1f;
            d.FindProperty("_scoreValue").intValue = 5;
            d.FindProperty("_massReward").floatValue = 1.5f;
            d.FindProperty("_killXp").floatValue = 0.5f;
            d.FindProperty("_requiredConsumeTier").intValue = (int)Core.BlobTier.Little;
            d.FindProperty("_consumeDamage").floatValue = 2f;
            d.FindProperty("_heartDropChance").floatValue = 0.02f;
            d.FindProperty("_packSize").intValue = 3;
            d.FindProperty("_coinOverride").intValue = 1;
            d.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);

            // Dalgalar: Police (1. dk) ve PoliceElite. Köpek sürüyle geldiği için ağırlığı düşük tutulur.
            AddToWave($"{WaveFolder}/Wave_Police.asset", data, 0.35f);
            AddToWave($"{WaveFolder}/Wave_PoliceElite.asset", data, 0.25f);

            AssetDatabase.SaveAssets();
            Debug.Log("[RobotDogSetup] Robot köpek kuruldu: prefab, EnemyData, dalgalar.");
        }

        // Köpek polis paletini kullanıyordu: polisle aynı mavi. Paletin köpeğe özel kopyasında maviler açık
        // metalik griye döner (koyu asfalttan da ayrılır); diğer renkler (ışıklar vb.) aynı kalır.
        private const string PalettePath = "Assets/_Project/Art/City/CityPalette.png";
        private const string DogPalettePath = "Assets/_Project/Art/City/RobotDogPalette.png";
        private const string DogMaterialPath = "Assets/_Project/Art/City/Mat_RobotDog.mat";

        [MenuItem("Tools/Blob.io/Robot Köpek Rengini Uygula")]
        private static void ApplyColor()
        {
            Material material = GetDogMaterial(true);
            if (material == null) return;
            GameObject contents = PrefabUtility.LoadPrefabContents(DogPrefab);
            int count = 0;
            foreach (Renderer r in contents.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].mainTexture == null || !mats[i].mainTexture.name.StartsWith("CityPalette")) continue;
                    mats[i] = material;
                    changed = true;
                }
                if (changed) { r.sharedMaterials = mats; count++; }
            }
            PrefabUtility.SaveAsPrefabAsset(contents, DogPrefab);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log($"[RobotDogSetup] Köpek rengi uygulandı ({count} renderer).");
        }

        private static Material GetDogMaterial() => GetDogMaterial(false);

        private static Material GetDogMaterial(bool rebuildTexture)
        {
            if (rebuildTexture || !File.Exists(DogPalettePath)) BuildDogPalette();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DogPalettePath);
            Material palette = CityKitPipeline.GetPaletteMaterial();
            if (texture == null || palette == null)
            {
                Debug.LogError("[RobotDogSetup] Köpek paleti ya da şehir palet materyali yok.");
                return null;
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DogMaterialPath);
            if (material == null)
            {
                material = new Material(palette);
                AssetDatabase.CreateAsset(material, DogMaterialPath);
            }
            material.CopyPropertiesFromMaterial(palette);
            material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void BuildDogPalette()
        {
            var source = new Texture2D(2, 2);
            if (!source.LoadImage(File.ReadAllBytes(PalettePath)))
            {
                Debug.LogError("[RobotDogSetup] CityPalette.png okunamadı.");
                return;
            }
            Color[] pixels = source.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                Color.RGBToHSV(c, out float h, out float sat, out float v);
                float hue = h * 360f;
                if (hue < 190f || hue > 235f || sat < 0.45f) continue; // sadece doygun maviler
                // Parlaklık sırası korunur: açık mavi -> açık gümüş, koyu mavi -> orta gri. Hafif soğuk ton.
                float grey = Mathf.Lerp(0.5f, 0.86f, Mathf.InverseLerp(0.5f, 0.85f, v));
                pixels[i] = new Color(grey * 0.97f, grey * 0.99f, grey * 1.03f, c.a);
            }
            source.SetPixels(pixels);
            File.WriteAllBytes(DogPalettePath, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(DogPalettePath); // Art/City ön işlemcisi: Point, Clamp, mipmap yok
        }

        private static void AddToWave(string path, EnemyData data, float weight)
        {
            var wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wave == null) { Debug.LogWarning($"[RobotDogSetup] {path} yok."); return; }
            var so = new SerializedObject(wave);
            SerializedProperty list = so.FindProperty("_enemyTypes");
            int index = -1;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("EnemyData").objectReferenceValue == data) index = i;
            if (index < 0)
            {
                index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
            }
            SerializedProperty entry = list.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("EnemyData").objectReferenceValue = data;
            entry.FindPropertyRelative("SpawnWeight").floatValue = weight;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
        }
    }
}
