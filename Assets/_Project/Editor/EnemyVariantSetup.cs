using System.IO;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Orta oyun düşmanları (OYUN.md §6) — Çevik kuvvet (ön kalkan) ve Motorlu polis (dalış).
    // Polis prefab'ı ve verisinden kopyalanır; şimdilik polis modeli + kendi palet rengi (model sonra Blender'dan).
    // Dalgalar (5. ve 7. dk) oluşturulup açık sahnedeki WaveController'a eklenir. Tekrar çalıştırmak güvenli.
    internal static class EnemyVariantSetup
    {
        private const string EnemyFolder = "Assets/_Project/Prefabs/Enemies";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Enemies";
        private const string WaveFolder = "Assets/_Project/ScriptableObjects/Waves";
        private const string ArtFolder = "Assets/_Project/Art/City";
        private const string PalettePath = ArtFolder + "/CityPalette.png";

        private const string PolicePrefab = EnemyFolder + "/Enemy_Police.prefab";
        private const string PoliceData = DataFolder + "/EnemyData_Police.asset";
        private const string EliteData = DataFolder + "/EnemyData_ElitePolis.asset";
        private const string DogData = DataFolder + "/EnemyData_RobotDog.asset";

        [MenuItem("Tools/Blob.io/Yeni Polis Türlerini Kur")]
        private static void Setup()
        {
            if (Object.FindAnyObjectByType<WaveController>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("[EnemyVariantSetup] GameScene açık olmalı (WaveController yok).");
                return;
            }
            AssetDatabase.Refresh();

            // Çevik kuvvet: yavaş, kalın, önden %85 az hasar; zeytin yeşili + kalkan. Large'da yenir.
            EnemyData riot = MakeEnemy("RiotPolice", new Color(0.30f, 0.34f, 0.18f), new Color(0.55f, 0.60f, 0.33f), d =>
            {
                d.FindProperty("_maxHealth").floatValue = 120f;
                d.FindProperty("_damage").floatValue = 12f;
                d.FindProperty("_moveSpeed").floatValue = 1.8f;
                d.FindProperty("_scoreValue").intValue = 25;
                d.FindProperty("_massReward").floatValue = 6f;
                d.FindProperty("_killXp").floatValue = 3f;
                d.FindProperty("_requiredConsumeTier").intValue = (int)BlobTier.Large;
                d.FindProperty("_consumeDamage").floatValue = 8f;
                d.FindProperty("_coinOverride").intValue = 3;
                d.FindProperty("_frontShieldHalfAngle").floatValue = 60f;
                d.FindProperty("_frontShieldBlock").floatValue = 0.85f;
                d.FindProperty("_crumbColorA").colorValue = new Color(0.55f, 0.60f, 0.33f);
                d.FindProperty("_crumbColorB").colorValue = new Color(0.30f, 0.34f, 0.18f);
                d.FindProperty("_dashRange").floatValue = 0f;
            }, AddShield);

            // Motorlu polis: hızlı, menzilde durup nişan alır ve dalar; fosforlu sarı-yeşil. Large'da yenir.
            EnemyData motor = MakeEnemy("MotorPolice", new Color(0.62f, 0.70f, 0.12f), new Color(0.88f, 0.95f, 0.25f), d =>
            {
                d.FindProperty("_maxHealth").floatValue = 70f;
                d.FindProperty("_damage").floatValue = 14f;
                d.FindProperty("_moveSpeed").floatValue = 2.6f;
                d.FindProperty("_scoreValue").intValue = 20;
                d.FindProperty("_massReward").floatValue = 5f;
                d.FindProperty("_killXp").floatValue = 2f;
                d.FindProperty("_requiredConsumeTier").intValue = (int)BlobTier.Large;
                d.FindProperty("_consumeDamage").floatValue = 6f;
                d.FindProperty("_coinOverride").intValue = 3;
                d.FindProperty("_frontShieldHalfAngle").floatValue = 0f;
                d.FindProperty("_dashRange").floatValue = 9f;
                d.FindProperty("_dashSpeed").floatValue = 8.5f;
                d.FindProperty("_dashWindup").floatValue = 0.6f;
                d.FindProperty("_dashDuration").floatValue = 0.9f;
                d.FindProperty("_dashCooldown").floatValue = 3f;
                d.FindProperty("_crumbColorA").colorValue = new Color(0.88f, 0.95f, 0.25f);
                d.FindProperty("_crumbColorB").colorValue = new Color(0.62f, 0.70f, 0.12f);
            }, null);

            if (riot == null || motor == null) return;

            var police = AssetDatabase.LoadAssetAtPath<EnemyData>(PoliceData);
            var elite = AssetDatabase.LoadAssetAtPath<EnemyData>(EliteData);
            var dog = AssetDatabase.LoadAssetAtPath<EnemyData>(DogData);

            WaveData riotWave = MakeWave("Wave_Riot", "Riot", 300f, 2.3f,
                (police, 0.7f), (elite, 0.15f), (dog, 0.15f), (riot, 0.5f));
            WaveData motorWave = MakeWave("Wave_Motor", "Motor", 420f, 2.1f,
                (police, 0.55f), (elite, 0.15f), (dog, 0.1f), (riot, 0.4f), (motor, 0.4f));

            AddWavesToScene(riotWave, motorWave);
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyVariantSetup] Çevik kuvvet + Motorlu polis kuruldu, dalgalar (5. / 7. dk) eklendi, sahne kaydedildi.");
        }

        private static EnemyData MakeEnemy(string name, Color dark, Color light,
            System.Action<SerializedObject> configure, System.Action<GameObject> decorate)
        {
            string prefabPath = $"{EnemyFolder}/Enemy_{name}.prefab";
            string dataPath = $"{DataFolder}/EnemyData_{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null && !AssetDatabase.CopyAsset(PolicePrefab, prefabPath))
            {
                Debug.LogError($"[EnemyVariantSetup] {prefabPath} kopyalanamadı.");
                return null;
            }
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath) == null && !AssetDatabase.CopyAsset(PoliceData, dataPath))
            {
                Debug.LogError($"[EnemyVariantSetup] {dataPath} kopyalanamadı.");
                return null;
            }
            AssetDatabase.SaveAssets();

            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            Material material = GetVariantMaterial(name, dark, light);
            if (material == null) return null;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            contents.name = $"Enemy_{name}";
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
                if (changed) r.sharedMaterials = mats;
            }
            decorate?.Invoke(contents);
            var so = new SerializedObject(contents.GetComponent<EnemyBase>());
            so.FindProperty("_data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            var d = new SerializedObject(data);
            d.FindProperty("_displayName").stringValue = name;
            d.FindProperty("_prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            configure(d);
            d.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        // Kalkan: modelin önünde, gövde genişliğinde dikey levha. Model child'ı: hamle pozlarıyla birlikte oynar.
        private static void AddShield(GameObject root)
        {
            Transform model = root.transform.Find("Model");
            Renderer body = model != null ? model.GetComponentInChildren<Renderer>() : null;
            if (body == null) { Debug.LogWarning("[EnemyVariantSetup] Kalkan için Model bulunamadı."); return; }
            Transform old = model.Find("Shield");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Bounds b = body.bounds;
            Vector3 local = root.transform.InverseTransformPoint(b.center);
            Vector3 size = new Vector3(b.size.x * 1.05f, b.size.y * 0.62f, 0.08f);

            var shield = new GameObject("Shield");
            shield.layer = root.layer;
            shield.transform.SetParent(root.transform, false);
            shield.transform.localPosition = new Vector3(0f, local.y - b.size.y * 0.06f, local.z + b.extents.z + 0.1f);
            shield.transform.localRotation = Quaternion.identity;
            shield.transform.localScale = size;
            shield.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            shield.AddComponent<MeshRenderer>().sharedMaterial = GetShieldMaterial();
            shield.transform.SetParent(model, true);
        }

        private static Material GetShieldMaterial()
        {
            string path = $"{ArtFolder}/Mat_RiotShield.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(CityKitPipeline.GetPaletteMaterial());
                AssetDatabase.CreateAsset(material, path);
            }
            material.CopyPropertiesFromMaterial(CityKitPipeline.GetPaletteMaterial());
            material.SetTexture("_BaseMap", null);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", null);
            material.SetColor("_BaseColor", new Color(0.78f, 0.84f, 0.9f));
            EditorUtility.SetDirty(material);
            return material;
        }

        // Paletin türe özel kopyası: doygun maviler (polis üniforması) dark..light arasına, parlaklık sırası korunur.
        private static Material GetVariantMaterial(string name, Color dark, Color light)
        {
            string texPath = $"{ArtFolder}/{name}Palette.png";
            string matPath = $"{ArtFolder}/Mat_{name}.mat";

            var source = new Texture2D(2, 2);
            if (!source.LoadImage(File.ReadAllBytes(PalettePath)))
            {
                Debug.LogError("[EnemyVariantSetup] CityPalette.png okunamadı.");
                return null;
            }
            Color[] pixels = source.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color.RGBToHSV(pixels[i], out float h, out float s, out float v);
                float hue = h * 360f;
                if (hue < 190f || hue > 235f || s < 0.45f) continue;
                Color c = Color.Lerp(dark, light, Mathf.InverseLerp(0.5f, 0.85f, v));
                c.a = pixels[i].a;
                pixels[i] = c;
            }
            source.SetPixels(pixels);
            File.WriteAllBytes(texPath, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(texPath); // Art/City ön işlemcisi: Point, Clamp, mipmap yok

            Material palette = CityKitPipeline.GetPaletteMaterial();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(palette);
                AssetDatabase.CreateAsset(material, matPath);
            }
            material.CopyPropertiesFromMaterial(palette);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static WaveData MakeWave(string file, string name, float time, float spawnRate, params (EnemyData data, float weight)[] entries)
        {
            string path = $"{WaveFolder}/{file}.asset";
            var wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveData>();
                AssetDatabase.CreateAsset(wave, path);
            }
            var so = new SerializedObject(wave);
            so.FindProperty("_timeThreshold").floatValue = time;
            so.FindProperty("_spawnRate").floatValue = spawnRate;
            so.FindProperty("_waveName").stringValue = name;
            so.FindProperty("_unlockByBlobTier").boolValue = false;
            SerializedProperty list = so.FindProperty("_enemyTypes");
            list.ClearArray();
            foreach (var (data, weight) in entries)
            {
                if (data == null) continue;
                int i = list.arraySize;
                list.InsertArrayElementAtIndex(i);
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("EnemyData").objectReferenceValue = data;
                e.FindPropertyRelative("SpawnWeight").floatValue = weight;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
            return wave;
        }

        // WaveController dalgaları dizideki sırayla açar (indeks sadece ileri gider): zamana göre sıralı eklenir.
        private static void AddWavesToScene(params WaveData[] waves)
        {
            var controller = Object.FindAnyObjectByType<WaveController>(FindObjectsInactive.Include);
            var so = new SerializedObject(controller);
            SerializedProperty list = so.FindProperty("_waves");
            foreach (WaveData wave in waves)
            {
                bool found = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == wave) found = true;
                if (found) continue;
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = wave;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }
    }
}
