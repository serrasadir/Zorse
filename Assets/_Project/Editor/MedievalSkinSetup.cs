using System.Collections.Generic;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Entities.Pedestrians;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → Ortaçağ Düşmanlarını ve Yayalarını Kur (CrownreachScene açıkken).
    // Şehrin düşman/boss/dalga/yaya/yerdeki yiyecek asset'lerinin ortaçağ kopyaları ("Med"): davranış ve denge aynı,
    // model ve isim ortaçağ. Sahnedeki bütün referanslar kopyalara çevrilir; GameScene ve şehir asset'leri değişmez.
    public static class MedievalSkinSetup
    {
        private const string EnemyFolder = "Assets/_Project/Prefabs/Enemies";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Enemies";
        private const string WaveFolder = "Assets/_Project/ScriptableObjects/Waves";
        private const string PedFolder = "Assets/_Project/ScriptableObjects/Pedestrians";
        private const string PedPrefabFolder = "Assets/_Project/Prefabs/City";
        private const string LitterPrefabFolder = "Assets/_Project/Prefabs/Consumables/LitterMedieval";
        private const string LitterDataFolder = "Assets/_Project/ScriptableObjects/Consumables/LitterMedieval";

        private struct EnemySkin
        {
            public string Data, Prefab, Med, Mesh, Title, Sub;
            public float ModelScale;
        }

        private static readonly EnemySkin[] Enemies =
        {
            new EnemySkin { Data = "EnemyData_Guard", Prefab = "Enemy_Guard", Med = "Militia", Mesh = "H_MedMilitia", ModelScale = 1f },
            new EnemySkin { Data = "EnemyData_Police", Prefab = "Enemy_Police", Med = "TownGuard", Mesh = "H_MedTownGuard", ModelScale = 1f },
            new EnemySkin { Data = "EnemyData_ElitePolis", Prefab = "Enemy_ElitePolice", Med = "Archer", Mesh = "H_MedArcher", ModelScale = 1f },
            new EnemySkin { Data = "EnemyData_RiotPolice", Prefab = "Enemy_RiotPolice", Med = "ShieldKnight", Mesh = "H_MedShieldKnight", ModelScale = 1f,
                            Title = "KALKANLI ŞÖVALYE!", Sub = "Önden vurma · arkasına geç!" },
            new EnemySkin { Data = "EnemyData_MotorPolice", Prefab = "Enemy_MotorPolice", Med = "MountedKnight", Mesh = "H_MedMountedKnight", ModelScale = 1f,
                            Title = "ATLI ŞÖVALYE!", Sub = "Mızrağı indirince yana kaç!" },
            new EnemySkin { Data = "EnemyData_RobotDog", Prefab = "Enemy_RobotDog", Med = "Hound", Mesh = "H_MedHuntingHound", ModelScale = 1.2f },
            new EnemySkin { Data = "EnemyData_Miniboss_Stage1", Prefab = "Enemy_Miniboss", Med = "Catapult", Mesh = "X_MedSiegeCatapult", ModelScale = 1.9f },
            new EnemySkin { Data = "EnemyData_Miniboss_Stage2", Prefab = "Enemy_Miniboss", Med = "SiegeTower", Mesh = "X_MedSiegeTower", ModelScale = 1.7f },
        };

        private const string FinalData = "EnemyData_FinalBoss";
        private const string FinalPrefab = "Enemy_FinalBoss";
        private const string DragonMesh = "X_MedDragonFlying";

        // Yayalar: şehir verisinin kopyası, varyantları ortaçağ köylüleri.
        private static readonly (string city, string med, string[] meshes)[] Pedestrians =
        {
            ("PedestrianData_Man", "PedestrianData_MedMale", new[] { "H_MedPeasantMale", "H_MedFarmer", "H_MedMinstrel" }),
            ("PedestrianData_Woman", "PedestrianData_MedFemale", new[] { "H_MedPeasantFemale", "H_MedPeasantHat", "H_MedPeasantBasket" }),
            ("PedestrianData_Big", "PedestrianData_MedMerchant", new[] { "H_MedMerchant" }),
        };

        private static readonly (string mesh, string display, float mass)[] Litter =
        {
            ("P_MedBreadCrust", "Ekmek Kabuğu", 1f), ("P_MedAppleCore", "Elma Koçanı", 1.5f), ("P_MedSmallBone", "Kemik", 1.5f),
            ("P_MedCandleStub", "Mum", 1.5f), ("P_MedTurnip", "Şalgam", 2f), ("P_MedCheeseWedge", "Peynir", 2f),
            ("P_MedWoodenMug", "Kupa", 2.5f), ("P_MedDrumstick", "Tavuk Budu", 3f),
        };

        // Ortaçağ modelleri şehir yiyeceklerinden (J_*, en uzun kenar ~0,55) 2-3 kat küçük çıkıyor; şehirle aynı boy olsun.
        // Tek ortak çarpan: aralarındaki boy farkı korunur.
        private const float CityLitterSize = 0.55f;

        // Sadece yerdeki yiyecek prefab'ları (sahneye dokunmaz; data asset'leri ve GUID'ler aynı kalır).
        [MenuItem("Tools/Blob.io/Ortaçağ Yerdeki Yiyecekleri Yenile")]
        private static void LitterMenu()
        {
            Material med = CityKitPipeline.GetMedievalMaterial();
            if (med == null) { Debug.LogError("[MedievalSkin] ortaçağ paleti yok."); return; }
            var log = new System.Text.StringBuilder();
            BuildLitter(med, log);
            AssetDatabase.SaveAssets();
            Debug.Log("[MedievalSkin]\n" + log);
        }

        [MenuItem("Tools/Blob.io/Ortaçağ Düşmanlarını ve Yayalarını Kur")]
        private static void Menu() => Debug.Log("[MedievalSkin]\n" + Apply());

        internal static string Apply()
        {
            var log = new System.Text.StringBuilder();
            if (SceneManager.GetActiveScene().path != CrownreachMapBuilder.ScenePath)
                return "HATA: CrownreachScene açık değil.";
            Material med = CityKitPipeline.GetMedievalMaterial();
            if (med == null) return "HATA: ortaçağ paleti yok.";

            var map = new Dictionary<Object, Object>(); // şehir asset'i → ortaçağ kopyası

            foreach (EnemySkin e in Enemies) SkinEnemy(e, med, map, log);
            SkinDragon(med, map, log);

            // Dalgalar ve boss verisi: kopyala, içlerindeki düşman referanslarını çevir.
            foreach (string guid in AssetDatabase.FindAssets("t:WaveData", new[] { WaveFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileName(path).StartsWith("Med_")) continue;
                CopyAndMap(path, map);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:BossData", new[] { DataFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileName(path).StartsWith("Med_")) continue;
                CopyAndMap(path, map);
            }
            foreach (Object copy in new List<Object>(map.Values)) Remap(copy, map); // _replacedBy, dalga içerikleri, boss aşamaları

            foreach (var p in Pedestrians) SkinPedestrian(p.city, p.med, p.meshes, med, map, log);
            List<ConsumableData> litter = BuildLitter(med, log);
            BakeWeapons(med, log);
            FerrisWheelFormSetup.SetupWindmill(med); // yel değirmeni dönüşümü

            // Sahne: bütün bileşenlerdeki referanslar kopyalara.
            int changed = 0;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (mb != null && mb.gameObject.scene == SceneManager.GetActiveScene()) changed += Remap(mb, map);
            ConsumableSpawner food = Object.FindAnyObjectByType<ConsumableSpawner>(FindObjectsInactive.Include);
            if (food != null && litter.Count > 0)
            {
                var so = new SerializedObject(food);
                SerializedProperty pool = so.FindProperty("_consumablePool");
                pool.arraySize = litter.Count;
                for (int i = 0; i < litter.Count; i++) pool.GetArrayElementAtIndex(i).objectReferenceValue = litter[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            log.AppendLine($"OK sahnede {changed} referans ortaçağ kopyasına çevrildi, {litter.Count} yerdeki yiyecek.");
            return log.ToString();
        }

        // ---------- düşmanlar ----------

        private static void SkinEnemy(EnemySkin e, Material med, Dictionary<Object, Object> map, System.Text.StringBuilder log)
        {
            string cityPrefabPath = $"{EnemyFolder}/{e.Prefab}.prefab";
            string medPrefabPath = $"{EnemyFolder}/Enemy_Med{e.Med}.prefab";
            string cityDataPath = $"{DataFolder}/{e.Data}.asset";
            string medDataPath = $"{DataFolder}/EnemyData_Med{e.Med}.asset";
            var cityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cityPrefabPath);
            var cityData = AssetDatabase.LoadAssetAtPath<EnemyData>(cityDataPath);
            if (cityPrefab == null || cityData == null) { log.AppendLine($"ATLANDI {e.Med}: {cityPrefabPath} / {cityDataPath} yok"); return; }

            float modelY = ModelY(cityPrefab);
            // Prefab her seferinde şehirden taze kopyalanır (eski model parçaları/rig kalmasın).
            AssetDatabase.DeleteAsset(medPrefabPath);
            AssetDatabase.CopyAsset(cityPrefabPath, medPrefabPath);
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(medDataPath) == null) AssetDatabase.CopyAsset(cityDataPath, medDataPath);
            AssetDatabase.SaveAssets();

            if (!CityKitPipeline.ApplyEnemyModel(medPrefabPath, e.Mesh, modelY, med)) { log.AppendLine($"HATA {e.Med}: {e.Mesh} takılamadı"); return; }
            var medData = AssetDatabase.LoadAssetAtPath<EnemyData>(medDataPath);

            GameObject contents = PrefabUtility.LoadPrefabContents(medPrefabPath);
            contents.name = $"Enemy_Med{e.Med}";
            Transform model = contents.transform.Find("Model");
            if (model != null && !Mathf.Approximately(e.ModelScale, 1f)) model.localScale *= e.ModelScale;
            var eb = contents.GetComponent<EnemyBase>();
            if (eb != null)
            {
                var so = new SerializedObject(eb);
                so.FindProperty("_data").objectReferenceValue = medData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject medPrefab = PrefabUtility.SaveAsPrefabAsset(contents, medPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            var d = new SerializedObject(medData);
            d.FindProperty("_displayName").stringValue = e.Med;
            d.FindProperty("_prefab").objectReferenceValue = medPrefab;
            if (e.Title != null)
            {
                d.FindProperty("_introTitle").stringValue = e.Title;
                d.FindProperty("_introSub").stringValue = e.Sub;
            }
            d.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(medData);

            map[cityData] = medData;
            if (!map.ContainsKey(cityPrefab)) map[cityPrefab] = medPrefab; // tank prefab'ı iki aşamada ortak: ilki
            log.AppendLine($"OK {e.Med} ← {e.Data} ({e.Mesh})");
        }

        private static bool HasAncestor(Transform t, string name)
        {
            for (; t != null; t = t.parent) if (t.name == name) return true;
            return false;
        }

        private static float ModelY(GameObject prefab)
        {
            Transform model = prefab.transform.Find("Model");
            return model != null ? model.localPosition.y : -CityKitPipeline.PoliceRootY;
        }

        // Ejderha: helikopter prefab'ının kopyası; gövde mesh'i ejderha, rotorlar gizli. Uçuş/alçalma/yutulma helikopterle aynı.
        private static void SkinDragon(Material med, Dictionary<Object, Object> map, System.Text.StringBuilder log)
        {
            string cityPrefabPath = $"{EnemyFolder}/{FinalPrefab}.prefab";
            string medPrefabPath = $"{EnemyFolder}/Enemy_MedDragon.prefab";
            string cityDataPath = $"{DataFolder}/{FinalData}.asset";
            string medDataPath = $"{DataFolder}/EnemyData_MedDragon.asset";
            var cityData = AssetDatabase.LoadAssetAtPath<EnemyData>(cityDataPath);
            MeshFilter dragon = CityKitPipeline.LoadSourceMesh(DragonMesh);
            if (cityData == null || dragon == null) { log.AppendLine("ATLANDI ejderha"); return; }

            AssetDatabase.DeleteAsset(medPrefabPath);
            AssetDatabase.CopyAsset(cityPrefabPath, medPrefabPath);
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(medDataPath) == null) AssetDatabase.CopyAsset(cityDataPath, medDataPath);
            AssetDatabase.SaveAssets();
            var medData = AssetDatabase.LoadAssetAtPath<EnemyData>(medDataPath);

            GameObject contents = PrefabUtility.LoadPrefabContents(medPrefabPath);
            contents.name = "Enemy_MedDragon";
            Transform body = null;
            Transform heli = null;
            foreach (Transform t in contents.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Heli") heli = t;
                if (t.name == "MainRotorPivot" || t.name == "TailRotorPivot")
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            }
            // Helikopterin gövdesi: Heli altında, rotor pivotlarında olmayan en büyük mesh.
            float best = 0f;
            if (heli != null)
                foreach (MeshFilter f in heli.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (f.GetComponentInParent<Transform>() != null && (HasAncestor(f.transform, "MainRotorPivot") || HasAncestor(f.transform, "TailRotorPivot"))) continue;
                    float size = f.sharedMesh != null ? Vector3.Scale(f.sharedMesh.bounds.size, f.transform.lossyScale).magnitude : 0f;
                    if (size > best) { best = size; body = f.transform; }
                }
            if (body != null)
            {
                var mr = body.GetComponent<MeshRenderer>();
                var mf = body.GetComponent<MeshFilter>();
                Vector3 oldExt = Vector3.Scale(mf.sharedMesh.bounds.size, body.lossyScale);
                float oldSize = Mathf.Max(oldExt.x, oldExt.z, oldExt.y);
                CityKitPipeline.ApplyModelTransform(body, dragon, DragonMesh);
                mf.sharedMesh = dragon.sharedMesh;
                mr.sharedMaterial = med;
                Vector3 newExt = Vector3.Scale(dragon.sharedMesh.bounds.size, body.lossyScale);
                float newSize = Mathf.Max(newExt.x, newExt.z, newExt.y);
                if (newSize > 0.01f) body.localScale *= oldSize * 1.15f / newSize; // kanat açıklığı helikopterden biraz büyük
                var eb = contents.GetComponent<EnemyBase>();
                if (eb != null)
                {
                    var so = new SerializedObject(eb);
                    so.FindProperty("_data").objectReferenceValue = medData;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            bool dragonOk = body != null; // Unload sonrası body yok edilmiş görünür
            GameObject medPrefab = PrefabUtility.SaveAsPrefabAsset(contents, medPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            var d = new SerializedObject(medData);
            d.FindProperty("_displayName").stringValue = "Ejderha";
            d.FindProperty("_prefab").objectReferenceValue = medPrefab;
            d.ApplyModifiedPropertiesWithoutUndo();
            map[cityData] = medData;
            map[AssetDatabase.LoadAssetAtPath<GameObject>(cityPrefabPath)] = medPrefab;
            log.AppendLine(dragonOk ? "OK Ejderha ← helikopter" : "UYARI ejderha: helikopter gövdesi (Heli altında) bulunamadı");
        }

        // ---------- yayalar ----------

        private static void SkinPedestrian(string city, string medName, string[] meshes, Material med,
            Dictionary<Object, Object> map, System.Text.StringBuilder log)
        {
            string cityPath = $"{PedFolder}/{city}.asset";
            string medPath = $"{PedFolder}/{medName}.asset";
            var cityData = AssetDatabase.LoadAssetAtPath<PedestrianData>(cityPath);
            if (cityData == null) { log.AppendLine($"ATLANDI {city}"); return; }
            if (AssetDatabase.LoadAssetAtPath<PedestrianData>(medPath) == null) AssetDatabase.CopyAsset(cityPath, medPath);
            var medData = AssetDatabase.LoadAssetAtPath<PedestrianData>(medPath);

            var so = new SerializedObject(medData);
            var cityPrefab = new SerializedObject(cityData).FindProperty("_prefab").objectReferenceValue as GameObject; // şehrinki (kopya kendini göstermesin)
            string prefabPath = $"{PedPrefabFolder}/Pedestrian_{medName.Replace("PedestrianData_", "")}.prefab";
            if (cityPrefab != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(cityPrefab), prefabPath);
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                foreach (MeshRenderer r in contents.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = med;
                MeshFilter first = CityKitPipeline.LoadSourceMesh(meshes[0]);
                Transform model = contents.transform.Find("Model");
                if (model != null && first != null)
                {
                    CityKitPipeline.ApplyModelTransform(model, first, meshes[0]);
                    var mf = model.GetComponent<MeshFilter>();
                    if (mf != null) mf.sharedMesh = first.sharedMesh;
                }
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                PrefabUtility.UnloadPrefabContents(contents);
                so.FindProperty("_prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            SerializedProperty variants = so.FindProperty("_variantMeshes");
            variants.arraySize = meshes.Length;
            for (int i = 0; i < meshes.Length; i++)
            {
                MeshFilter src = CityKitPipeline.LoadSourceMesh(meshes[i]);
                variants.GetArrayElementAtIndex(i).objectReferenceValue = src != null ? src.sharedMesh : null;
            }
            so.FindProperty("_rigs").ClearArray(); // Yaya Yürüyüş Parçalarını Kur yeniden böler
            so.FindProperty("_displayName").stringValue = medName.Replace("PedestrianData_Med", "Köylü ");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(medData);
            map[cityData] = medData;
            log.AppendLine($"OK yaya {medName}: {string.Join(", ", meshes)}");
        }

        // ---------- yerdeki yiyecekler ----------

        private static List<ConsumableData> BuildLitter(Material med, System.Text.StringBuilder log)
        {
            EnsureFolder(LitterPrefabFolder);
            EnsureFolder(LitterDataFolder);
            var all = new List<ConsumableData>();
            float litterScale = LitterScale();
            foreach (var (mesh, display, mass) in Litter)
            {
                MeshFilter src = CityKitPipeline.LoadSourceMesh(mesh);
                if (src == null) continue;
                string name = mesh.Substring(5); // P_MedTurnip → Turnip
                string dataPath = $"{LitterDataFolder}/LitterMed_{name}.asset";
                var data = AssetDatabase.LoadAssetAtPath<ConsumableData>(dataPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<ConsumableData>();
                    AssetDatabase.CreateAsset(data, dataPath);
                }

                var root = new GameObject("Consumable_Med" + name);
                var model = new GameObject("Model");
                model.transform.SetParent(root.transform, false);
                CityKitPipeline.ApplyModelTransform(model.transform, src, mesh);
                model.transform.localScale *= litterScale;
                model.AddComponent<MeshFilter>().sharedMesh = src.sharedMesh;
                var mr = model.AddComponent<MeshRenderer>();
                mr.sharedMaterial = med;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // Taban köke otursun: model ortadan pivotlu, büyütünce yarısı yerin altında kalıyordu.
                model.transform.localPosition += Vector3.up * -mr.bounds.min.y;
                Bounds b = mr.bounds;
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                Vector3 size = Vector3.Max(b.size, Vector3.one * 0.25f);
                box.size = size;
                box.center = new Vector3(b.center.x, size.y * 0.5f, b.center.z);
                var consumable = root.AddComponent<ConsumableBase>();
                var cso = new SerializedObject(consumable);
                cso.FindProperty("_data").objectReferenceValue = data;
                cso.ApplyModifiedPropertiesWithoutUndo();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{LitterPrefabFolder}/Consumable_Med{name}.prefab");
                Object.DestroyImmediate(root);

                var so = new SerializedObject(data);
                so.FindProperty("_displayName").stringValue = display;
                so.FindProperty("_prefab").objectReferenceValue = prefab;
                so.FindProperty("_scoreValue").intValue = 1;
                so.FindProperty("_massValue").floatValue = mass;
                so.FindProperty("_requiredTier").intValue = (int)BlobTier.Tiny;
                so.FindProperty("_isHazard").boolValue = false;
                so.FindProperty("_spawnYOffset").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                all.Add(data);
            }
            log.AppendLine($"OK yerdeki yiyecek: {all.Count} (×{litterScale:0.00}, şehir boyu)");
            return all;
        }

        // Ortaçağ yiyeceklerinin ortalama en uzun kenarını şehirdekine (CityLitterSize) getiren çarpan.
        private static float LitterScale()
        {
            float sum = 0f; int n = 0;
            foreach (var (mesh, _, _) in Litter)
            {
                MeshFilter src = CityKitPipeline.LoadSourceMesh(mesh);
                if (src == null) continue;
                var probe = new GameObject("LitterProbe");
                CityKitPipeline.ApplyModelTransform(probe.transform, src, mesh);
                Vector3 size = Vector3.Scale(src.sharedMesh.bounds.size, probe.transform.lossyScale);
                Object.DestroyImmediate(probe);
                sum += Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                n++;
            }
            return n > 0 && sum > 0f ? CityLitterSize / (sum / n) : 1f;
        }

        // ---------- görünür silahlar ----------
        // Şehir silah mesh'leri blob görünüm uzayında (pivot = blob merkezi, dal gidiş yönüne uzanır). Ortaçağ kılıcı/arbaleti
        // aynı uzaya pişirilir: kılıç dalın uzun ekseni boyunca (kabza dalın blob'a yakın ucunda), boyu dalınki kadar;
        // arbalet atarın kutusuna ortalanıp ölçeklenir. Resources'a mesh + materyal (WeaponVisual, MapTheme "Med").
        private const string WeaponFolder = "Assets/_Project/Resources/FX/Weapons";

        private static void BakeWeapons(Material med, System.Text.StringBuilder log)
        {
            string matPath = WeaponFolder + "/Mat_MedWeapon.mat";
            var weaponMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (weaponMat == null) AssetDatabase.CreateAsset(new Material(med) { name = "Mat_MedWeapon" }, matPath);
            else { weaponMat.shader = med.shader; weaponMat.CopyPropertiesFromMaterial(med); EditorUtility.SetDirty(weaponMat); }
            int n = 0;
            for (int f = 1; f <= 5; f++)
                if (BakeAlongAxis($"{WeaponFolder}/G_Stick_{f}.fbx", $"G_MedSword_{f}", $"{WeaponFolder}/G_MedStick_{f}.asset")) n++;
            for (int f = 1; f <= 4; f++)
                if (BakeIntoBox($"{WeaponFolder}/G_Firework_{f}.fbx", $"G_MedCrossbow_{f}", $"{WeaponFolder}/G_MedFirework_{f}.asset")) n++;
            log.AppendLine($"OK görünür silah: {n}/9 pişirildi");
        }

        private static Mesh LoadFbxMesh(string path)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Mesh m) return m;
            return null;
        }

        // FBX'in kök dönüşü/ölçeği uygulanmış köşeler (Unity model uzayı).
        private static Vector3[] SourceVerts(MeshFilter src, out Vector3[] normals)
        {
            Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, src.transform.rotation, src.transform.lossyScale);
            Vector3[] v = src.sharedMesh.vertices, nr = src.sharedMesh.normals;
            normals = new Vector3[nr.Length];
            for (int i = 0; i < v.Length; i++) v[i] = m.MultiplyPoint3x4(v[i]);
            for (int i = 0; i < nr.Length; i++) normals[i] = (src.transform.rotation * nr[i]).normalized;
            return v;
        }

        private static bool BakeAlongAxis(string targetPath, string sourceMesh, string outPath)
        {
            Mesh target = LoadFbxMesh(targetPath);
            MeshFilter src = CityKitPipeline.LoadSourceMesh(sourceMesh);
            if (target == null || src == null) return false;
            Bounds tb = target.bounds;
            int axis = tb.size.x >= tb.size.y && tb.size.x >= tb.size.z ? 0 : tb.size.y >= tb.size.z ? 1 : 2;
            float lo = tb.min[axis], hi = tb.max[axis];
            bool gripAtLo = Mathf.Abs(lo) <= Mathf.Abs(hi); // blob merkezine yakın uç kabza
            Vector3 dirT = Vector3.zero; dirT[axis] = gripAtLo ? 1f : -1f;
            Vector3 grip = tb.center; grip[axis] = gripAtLo ? lo : hi;

            Vector3[] v = SourceVerts(src, out Vector3[] nr);
            var sb = new Bounds(v[0], Vector3.zero);
            foreach (Vector3 p in v) sb.Encapsulate(p);
            int sAxis = sb.size.x >= sb.size.y && sb.size.x >= sb.size.z ? 0 : sb.size.y >= sb.size.z ? 1 : 2;
            bool tipAtHi = Mathf.Abs(sb.max[sAxis]) >= Mathf.Abs(sb.min[sAxis]); // kabza orijinde, uç uzakta
            Vector3 dirS = Vector3.zero; dirS[sAxis] = tipAtHi ? 1f : -1f;
            Vector3 sGrip = sb.center; sGrip[sAxis] = tipAtHi ? sb.min[sAxis] : sb.max[sAxis];

            float scale = (hi - lo) / Mathf.Max(0.001f, sb.size[sAxis]);
            Quaternion rot = Quaternion.FromToRotation(dirS, dirT);
            for (int i = 0; i < v.Length; i++) v[i] = grip + rot * ((v[i] - sGrip) * scale);
            for (int i = 0; i < nr.Length; i++) nr[i] = rot * nr[i];
            SaveMesh(src.sharedMesh, v, nr, outPath);
            return true;
        }

        private static bool BakeIntoBox(string targetPath, string sourceMesh, string outPath)
        {
            Mesh target = LoadFbxMesh(targetPath);
            MeshFilter src = CityKitPipeline.LoadSourceMesh(sourceMesh);
            if (target == null || src == null) return false;
            Bounds tb = target.bounds;
            Vector3[] v = SourceVerts(src, out Vector3[] nr);
            var sb = new Bounds(v[0], Vector3.zero);
            foreach (Vector3 p in v) sb.Encapsulate(p);
            float scale = Mathf.Max(tb.size.x, tb.size.y, tb.size.z) / Mathf.Max(0.001f, Mathf.Max(sb.size.x, sb.size.y, sb.size.z));
            for (int i = 0; i < v.Length; i++) v[i] = tb.center + (v[i] - sb.center) * scale;
            SaveMesh(src.sharedMesh, v, nr, outPath);
            return true;
        }

        private static void SaveMesh(Mesh source, Vector3[] v, Vector3[] nr, string path)
        {
            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            mesh.vertices = v;
            if (nr.Length == v.Length) mesh.normals = nr;
            mesh.uv = source.uv;
            if (source.colors != null && source.colors.Length == v.Length) mesh.colors = source.colors; // köşe karartması
            mesh.subMeshCount = 1;
            mesh.triangles = source.triangles;
            if (nr.Length != v.Length) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        // ---------- yardımcılar ----------

        private static void CopyAndMap(string path, Dictionary<Object, Object> map)
        {
            string dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string medPath = $"{dir}/Med_{System.IO.Path.GetFileName(path)}";
            if (AssetDatabase.LoadAssetAtPath<Object>(medPath) == null) AssetDatabase.CopyAsset(path, medPath);
            map[AssetDatabase.LoadAssetAtPath<Object>(path)] = AssetDatabase.LoadAssetAtPath<Object>(medPath);
        }

        // Nesnedeki bütün obje referanslarını haritaya göre çevirir; değişen alan sayısı.
        private static int Remap(Object target, Dictionary<Object, Object> map)
        {
            var so = new SerializedObject(target);
            SerializedProperty it = so.GetIterator();
            int n = 0;
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                Object v = it.objectReferenceValue;
                if (v != null && map.TryGetValue(v, out Object to) && to != null && to != v)
                {
                    it.objectReferenceValue = to;
                    n++;
                }
            }
            if (n > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
            return n;
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
