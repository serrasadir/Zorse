using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Orta oyun düşmanları (OYUN.md §6): Çevik kuvvet (ön kalkan) ve Motorlu polis (dalış).
    // Polis prefab'ı ve verisinden kopyalanır; modeller Art/City/H_RiotPolice / H_MotorPolice
    // (Blender: build_police_variants.py, şimdilik tools/zorse-blender-pending/). Polisle aynı palet ve ölçek.
    // Dalgalar (5. ve 7. dk) oluşturulup açık sahnedeki WaveController'a eklenir. Tekrar çalıştırmak güvenli.
    internal static class EnemyVariantSetup
    {
        private const string EnemyFolder = "Assets/_Project/Prefabs/Enemies";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Enemies";
        private const string WaveFolder = "Assets/_Project/ScriptableObjects/Waves";

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

            // Çevik kuvvet: yavaş, kalın, önden %85 az hasar; antrasit zırh, beyaz kask, açık mavi kalkan. Large'da yenir.
            EnemyData riot = MakeEnemy("RiotPolice", d =>
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
                d.FindProperty("_crumbColorA").colorValue = new Color(0.25f, 0.31f, 0.37f); // antrasit zırh
                d.FindProperty("_crumbColorB").colorValue = new Color(0.62f, 0.77f, 0.85f); // kalkan
                d.FindProperty("_dashRange").floatValue = 0f;
            });

            // Motorlu polis: hızlı, menzilde durup nişan alır ve dalar; beyaz motor, mavi üniforma. Large'da yenir.
            EnemyData motor = MakeEnemy("MotorPolice", d =>
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
                d.FindProperty("_crumbColorA").colorValue = new Color(0.95f, 0.94f, 0.92f); // beyaz motor
                d.FindProperty("_crumbColorB").colorValue = new Color(0.23f, 0.34f, 0.66f); // polis mavisi
            });

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

        private static EnemyData MakeEnemy(string name, System.Action<SerializedObject> configure)
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

            // Model: polisle aynı yükseklik (kök yol + 0.6) ve ölçek (ApplyModelTransform).
            if (!CityKitPipeline.ApplyEnemyModel(prefabPath, $"H_{name}", -CityKitPipeline.PoliceRootY, CityKitPipeline.GetPaletteMaterial()))
            {
                Debug.LogError($"[EnemyVariantSetup] H_{name} modeli takılamadı (Art/City/H_{name}.fbx var mı?).");
                return null;
            }

            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            contents.name = $"Enemy_{name}";
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
