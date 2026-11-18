using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Bekçi (ilk dakikaların zayıf, copsuz düşmanı; model H_Guard ← build_guard.py) ve orta oyun düşmanları
    // (OYUN.md §6): Çevik kuvvet (ön kalkan) ve Motorlu polis (dalış). Dalga tablosu baştan yazılır.
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

            // Bekçi: copsuz, yumrukla durdurmaya çalışır; yavaş, az canlı, Little'da yenir (ilk dakikada yutulur).
            EnemyData guard = MakeEnemy("Guard", d =>
            {
                d.FindProperty("_maxHealth").floatValue = 10f; // temel sopa (10) tek vuruşta
                d.FindProperty("_damage").floatValue = 3f;
                d.FindProperty("_moveSpeed").floatValue = 2.0f;
                d.FindProperty("_scoreValue").intValue = 5;
                d.FindProperty("_massReward").floatValue = 1.5f;
                d.FindProperty("_killXp").floatValue = 0.5f; // polis 2
                d.FindProperty("_requiredConsumeTier").intValue = (int)BlobTier.Little;
                d.FindProperty("_consumeDamage").floatValue = 1f;
                d.FindProperty("_coinOverride").intValue = 4;      // %25 olasılıkla 4 coin (ortalama 1)
                d.FindProperty("_coinDropChance").floatValue = 0.25f;
                d.FindProperty("_frontShieldHalfAngle").floatValue = 0f;
                d.FindProperty("_dashRange").floatValue = 0f;
                d.FindProperty("_crumbColorA").colorValue = new Color(0.96f, 0.77f, 0.27f); // reflektörlü yelek
                d.FindProperty("_crumbColorB").colorValue = new Color(0.91f, 0.79f, 0.56f); // bej gömlek
                // 2. dk'dan 10. dk'ya bekçinin yerini giderek polis alır (3. dk'da polis görünür).
                d.FindProperty("_replacedBy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>(PoliceData);
                d.FindProperty("_replaceStart").floatValue = 120f;
                d.FindProperty("_replaceEnd").floatValue = 600f;
            });

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

            if (guard == null || riot == null || motor == null) return;

            var police = AssetDatabase.LoadAssetAtPath<EnemyData>(PoliceData);
            var elite = AssetDatabase.LoadAssetAtPath<EnemyData>(EliteData);
            var dog = AssetDatabase.LoadAssetAtPath<EnemyData>(DogData);

            // Dalgalar "bekçi" yazar; bekçi 3. dk'dan 12. dk'ya giderek polise döner (EnemyData._replacedBy).
            AssetDatabase.DeleteAsset(WaveFolder + "/Wave_PoliceMix.asset");
            AssetDatabase.DeleteAsset(WaveFolder + "/Wave_PoliceMain.asset");
            WaveData guardWave = MakeWave("Wave_Guard", "Guard", 20f, 2.0f, (guard, 1f));
            WaveData policeWave = MakeWave("Wave_Police", "Police", 60f, 2.2f, (guard, 1f), (dog, 0.35f));
            WaveData eliteWave = MakeWave("Wave_PoliceElite", "PoliceElite", 240f, 2.3f,
                (guard, 0.85f), (elite, 0.15f), (dog, 0.25f));
            WaveData riotWave = MakeWave("Wave_Riot", "Riot", 300f, 2.3f,
                (guard, 0.7f), (elite, 0.15f), (dog, 0.15f), (riot, 0.5f));
            WaveData motorWave = MakeWave("Wave_Motor", "Motor", 420f, 2.1f,
                (guard, 0.55f), (elite, 0.15f), (dog, 0.1f), (riot, 0.4f), (motor, 0.4f));

            AddAlertMark(EnemyFolder + "/Enemy_Guard.prefab");
            AddAlertMark(PolicePrefab);
            SetSceneWaves(guardWave, policeWave, eliteWave, riotWave, motorWave);
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyVariantSetup] Bekçi + Çevik kuvvet + Motorlu polis kuruldu, dalga tablosu yazıldı, sahne kaydedildi.");
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
            bool created = wave == null;
            if (created)
            {
                wave = ScriptableObject.CreateInstance<WaveData>();
                AssetDatabase.CreateAsset(wave, path);
            }
            var so = new SerializedObject(wave);
            so.FindProperty("_timeThreshold").floatValue = time;
            so.FindProperty("_spawnRate").floatValue = spawnRate;
            so.FindProperty("_waveName").stringValue = name;
            if (created) so.FindProperty("_unlockByBlobTier").boolValue = false; // var olanın tier kilidi korunur (elit: Medium)
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

        // "!" işareti: kırmızı ünlem, beyaz kenarlı (palet dokusu, Unlit). Başın üstünde child + AlertMark.
        private const string AlertMeshPath = "Assets/_Project/Art/City/EnemyRigs/AlertMark.asset";
        private const string AlertMatPath = "Assets/_Project/Materials/Mat_AlertMark.mat";
        private const string PaletteTex = "Assets/_Project/Art/City/CityPalette.png";

        private static void AddAlertMark(string prefabPath)
        {
            Mesh mesh = AlertMesh();
            Material material = AlertMaterial();
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform old = contents.transform.Find("AlertMark");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                float top = 1.5f;
                Transform model = contents.transform.Find("Model");
                if (model != null)
                {
                    bool any = false;
                    Bounds b = default;
                    foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!any) { b = r.bounds; any = true; }
                        else b.Encapsulate(r.bounds);
                    }
                    if (any) top = b.max.y - contents.transform.position.y;
                }

                var mark = new GameObject("AlertMark") { layer = contents.layer };
                mark.transform.SetParent(contents.transform, false);
                mark.transform.localPosition = new Vector3(0f, top + 0.45f, 0f);
                mark.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = mark.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                mark.SetActive(false);

                AlertMark alert = contents.GetComponent<AlertMark>();
                if (alert == null) alert = contents.AddComponent<AlertMark>();
                var so = new SerializedObject(alert);
                so.FindProperty("_mark").objectReferenceValue = mark.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static Material AlertMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AlertMatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, AlertMatPath);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteTex));
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Vector2 PaletteUv(int row, int col) => new Vector2((col + 0.5f) / 8f, 1f - (row + 0.5f) / 5f);

        // Pivot alt orta, boy ~1. Beyaz kenar arkada ve biraz büyük, kırmızı ünlem önde (-Z kameraya bakar).
        private static Mesh AlertMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AlertMeshPath);
            bool created = mesh == null;
            if (created) mesh = new Mesh { name = "AlertMark" };
            mesh.Clear();
            var v = new System.Collections.Generic.List<Vector3>();
            var uv = new System.Collections.Generic.List<Vector2>();
            var t = new System.Collections.Generic.List<int>();
            Vector2 red = PaletteUv(1, 6), white = PaletteUv(3, 7);
            const float o = 0.07f;
            AddBox(v, uv, t, new Vector3(-0.12f - o, 0.32f - o, 0.02f), new Vector3(0.12f + o, 0.9f + o, 0.08f), white);
            AddBox(v, uv, t, new Vector3(-0.12f - o, -o, 0.02f), new Vector3(0.12f + o, 0.22f + o, 0.08f), white);
            AddBox(v, uv, t, new Vector3(-0.12f, 0.32f, -0.04f), new Vector3(0.12f, 0.9f, 0.02f), red);
            AddBox(v, uv, t, new Vector3(-0.12f, 0f, -0.04f), new Vector3(0.12f, 0.22f, 0.02f), red);
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, AlertMeshPath);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void AddBox(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Vector2> uv,
            System.Collections.Generic.List<int> t, Vector3 min, Vector3 max, Vector2 color)
        {
            Vector3[] c =
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
            };
            int[][] faces =
            {
                new[] { 0, 3, 2, 1 }, new[] { 5, 6, 7, 4 }, new[] { 4, 7, 3, 0 },
                new[] { 1, 2, 6, 5 }, new[] { 3, 7, 6, 2 }, new[] { 4, 0, 1, 5 },
            };
            foreach (int[] f in faces)
            {
                int start = v.Count;
                foreach (int i in f) { v.Add(c[i]); uv.Add(color); }
                t.Add(start); t.Add(start + 1); t.Add(start + 2);
                t.Add(start); t.Add(start + 2); t.Add(start + 3);
            }
        }

        // WaveController dalgaları dizideki sırayla açar (indeks sadece ileri gider): sahnedeki diğer dalgalar korunur,
        // hepsi zamana göre sıralanıp yazılır.
        private static void SetSceneWaves(params WaveData[] waves)
        {
            var controller = Object.FindAnyObjectByType<WaveController>(FindObjectsInactive.Include);
            var so = new SerializedObject(controller);
            SerializedProperty list = so.FindProperty("_waves");
            var all = new System.Collections.Generic.List<WaveData>(waves);
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue is WaveData w && w != null && !all.Contains(w)) all.Add(w);
            all.Sort((a, b) => a.TimeThreshold.CompareTo(b.TimeThreshold));
            list.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }
    }
}
