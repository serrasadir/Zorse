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
        private const int ProjectileLayer = 18;
        private const string NewsPath = "Assets/_Project/ScriptableObjects/UI/NewsHeadlines.asset";
        private const string MedNewsPath = "Assets/_Project/ScriptableObjects/UI/Med_NewsHeadlines.asset";

        private struct EnemySkin
        {
            public string Data, Prefab, Med, Mesh, Title, Sub, Hud; // Hud: boss uyarısındaki ad ("X GELDİ!")
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
            new EnemySkin { Data = "EnemyData_Miniboss_Stage1", Prefab = "Enemy_Miniboss", Med = "Catapult", Mesh = "X_MedSiegeCatapult", ModelScale = 2.3f, Hud = "MANCINIK" },
            new EnemySkin { Data = "EnemyData_Miniboss_Stage2", Prefab = "Enemy_Miniboss", Med = "SiegeTower", Mesh = "X_MedSiegeTower", ModelScale = 2.05f, Hud = "KUŞATMA KULESİ" },
        };

        private const string FinalData = "EnemyData_FinalBoss";
        private const string FinalPrefab = "Enemy_FinalBoss";
        private const string DragonMesh = "X_MedDragonFlying";

        // Yayalar: şehir verisinin kopyası, varyantları ortaçağ köylüleri.
        private static readonly (string city, string med, string[] meshes)[] Pedestrians =
        {
            ("PedestrianData_Man", "PedestrianData_MedMale", new[] { "H_MedPeasantMale", "H_MedFarmer", "H_MedMinstrel", "H_MedMonk", "H_MedFisherman", "H_MedBlacksmith" }),
            ("PedestrianData_Woman", "PedestrianData_MedFemale", new[] { "H_MedPeasantFemale", "H_MedPeasantHat", "H_MedPeasantBasket", "H_MedOldWoman", "H_MedMilkmaid" }),
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

            // Haber bandı: ortaçağ manşetleri (metinler asset'te, elle yazılır).
            Object cityNews = AssetDatabase.LoadAssetAtPath<Object>(NewsPath);
            Object medNews = AssetDatabase.LoadAssetAtPath<Object>(MedNewsPath);
            if (cityNews != null && medNews != null) map[cityNews] = medNews;
            else log.AppendLine("UYARI: " + MedNewsPath + " yok, haber bandı şehir manşetlerinde kalır.");

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
            if (e.Hud != null) d.FindProperty("_hudName").stringValue = e.Hud;
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
                Bounds span = BuildWings(contents.transform, body, mr, med, log); // gövde + kanatlar (gövde uzayı)
                Vector3 newExt = Vector3.Scale(span.size, body.lossyScale);
                float newSize = Mathf.Max(newExt.x, newExt.z, newExt.y);
                if (newSize > 0.01f) body.localScale *= oldSize * DragonSpan / newSize;
                BuildFireAttack(contents, body, med, medData, log);
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
            d.FindProperty("_hudName").stringValue = "EJDERHA";
            d.FindProperty("_prefab").objectReferenceValue = medPrefab;
            d.ApplyModifiedPropertiesWithoutUndo();
            map[cityData] = medData;
            map[AssetDatabase.LoadAssetAtPath<GameObject>(cityPrefabPath)] = medPrefab;
            log.AppendLine(dragonOk ? "OK Ejderha ← helikopter" : "UYARI ejderha: helikopter gövdesi (Heli altında) bulunamadı");
        }

        // ---------- ejderha: kanat çırpma + ateş topu (#91) ----------
        private const float DragonSpan = 1.265f; // kanat açıklığı / helikopter boyu (sadece görsel; kapsül helikopterinki)
        // Blender (tools/medieval/kit_bosses.py): gövde, iki kanat ve ateş topu ayrı FBX; hepsi gövdeyle aynı uzayda.
        private const string WingMeshL = "X_MedDragonWingL";
        private const string WingMeshR = "X_MedDragonWingR";
        private const string FireballMesh = "X_MedFireball";
        private const string DragonRigFolder = "Assets/_Project/Art/City/Medieval/Rigs";
        private const string FireballBulletPath = EnemyFolder + "/MedFireballBullet.prefab";
        private const string TrailMaterialPath = "Assets/_Project/Materials/Mat_BulletTrail.mat";
        private const float FireballBulletSize = 0.8f;  // helikopter roket mermisi ~0.6
        private const float FireballStrikeSize = 1.4f;  // alan saldırısı
        private static readonly Color FireTrailHead = new Color(1f, 0.85f, 0.3f);
        private static readonly Color FireTrailTail = new Color(0.9f, 0.25f, 0.08f);

        // Kanatları gövdenin child'ı yapar (pivot omuzda) + WingFlap. Dönüş: gövde + kanatların gövde uzayı sınırı.
        private static Bounds BuildWings(Transform root, Transform body, MeshRenderer bodyRenderer, Material med, System.Text.StringBuilder log)
        {
            Mesh bodyMesh = body.GetComponent<MeshFilter>().sharedMesh;
            Bounds span = bodyMesh.bounds;
            MeshFilter l = CityKitPipeline.LoadSourceMesh(WingMeshL), r = CityKitPipeline.LoadSourceMesh(WingMeshR);
            if (l == null || r == null) { log.AppendLine("UYARI ejderha: kanat FBX'leri yok, kanatsız"); return span; }
            span.Encapsulate(l.sharedMesh.bounds);
            span.Encapsulate(r.sharedMesh.bounds);

            Vector3 up = body.InverseTransformDirection(Vector3.up).normalized;
            Vector3 lateral = DominantAxis(r.sharedMesh.bounds.center - bodyMesh.bounds.center);
            Transform wl = BuildWing(body, l.sharedMesh, -lateral, med, "WingL", $"{DragonRigFolder}/{WingMeshL}_Pivot.asset");
            Transform wr = BuildWing(body, r.sharedMesh, lateral, med, "WingR", $"{DragonRigFolder}/{WingMeshR}_Pivot.asset");

            var flap = body.gameObject.AddComponent<WingFlap>();
            var so = new SerializedObject(flap);
            so.FindProperty("_left").objectReferenceValue = wl;
            so.FindProperty("_right").objectReferenceValue = wr;
            // +açı kanadı kaldırsın: yan eksen × yukarı (sol kanat için yan eksen ters).
            so.FindProperty("_leftAxis").vector3Value = Vector3.Cross(-lateral, up).normalized;
            so.FindProperty("_rightAxis").vector3Value = Vector3.Cross(lateral, up).normalized;
            so.FindProperty("_body").objectReferenceValue = bodyRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            return span;
        }

        // Kanat mesh'ini omuza (gövdeye en yakın köşelerin sınır kutusu ortası) göre yeniden merkezler; child'ı omuza koyar.
        private static Transform BuildWing(Transform body, Mesh src, Vector3 lateral, Material med, string name, string path)
        {
            Vector3[] v = src.vertices;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (Vector3 p in v) { float d = Vector3.Dot(p, lateral); lo = Mathf.Min(lo, d); hi = Mathf.Max(hi, d); }
            float cut = lo + (hi - lo) * 0.06f;
            Bounds root = default;
            bool any = false;
            foreach (Vector3 p in v)
            {
                if (Vector3.Dot(p, lateral) > cut) continue;
                if (!any) { root = new Bounds(p, Vector3.zero); any = true; }
                else root.Encapsulate(p);
            }
            Vector3 pivot = root.center; // iki kanatta simetrik kalsın (köşe ortalaması yoğunluğa göre kayar)
            for (int i = 0; i < v.Length; i++) v[i] -= pivot;
            EnsureFolder(DragonRigFolder);
            SaveMesh(src, v, src.normals, path);

            var go = new GameObject(name) { layer = body.gameObject.layer };
            go.transform.SetParent(body, false);
            go.transform.localPosition = pivot;
            go.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = med;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // helikopter gibi: sahte yer gölgesi var
            return go.transform;
        }

        private static Vector3 DominantAxis(Vector3 d)
        {
            Vector3 a = new Vector3(Mathf.Abs(d.x), Mathf.Abs(d.y), Mathf.Abs(d.z));
            if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(d.x), 0f, 0f);
            if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(d.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(d.z));
        }

        // Lazer + roket yerine: ağızda biriken ateş topu (MouthFire), ateş topu mermisi, alan saldırısında ateş topu.
        // Helikopterden kalan pervane gölgesi ve projektör lekesi kapanır.
        private static void BuildFireAttack(GameObject root, Transform body, Material med, EnemyData medData, System.Text.StringBuilder log)
        {
            MeshFilter fb = CityKitPipeline.LoadSourceMesh(FireballMesh);
            if (fb == null) { log.AppendLine("UYARI ejderha: X_MedFireball yok, helikopter saldırısı kaldı"); return; }
            var trailMat = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);

            // Ağız: burnun ucu (gövdenin ileri yöndeki en uç köşesi), top yarı yarıya önünde.
            Mesh bodyMesh = body.GetComponent<MeshFilter>().sharedMesh;
            Vector3 fwd = body.InverseTransformDirection(root.transform.forward).normalized;
            Vector3 nose = Vector3.zero;
            float best = float.MinValue;
            foreach (Vector3 p in bodyMesh.vertices) { float d = Vector3.Dot(p, fwd); if (d > best) { best = d; nose = p; } }
            Vector3 side = DominantAxis(Vector3.Cross(fwd, body.InverseTransformDirection(Vector3.up)));
            nose -= side * Vector3.Dot(nose, side); // burun deliği değil, ağzın ortası
            float ballRadius = fb.sharedMesh.bounds.extents.magnitude * 0.5f;
            var mouth = new GameObject("MouthFire") { layer = root.layer };
            mouth.transform.SetParent(body, false);
            mouth.transform.localPosition = nose + fwd * ballRadius * 0.6f;
            var ball = new GameObject("Ball") { layer = root.layer };
            ball.transform.SetParent(mouth.transform, false); // gövdeyle aynı uzay: topun önü ejderhanın önü
            ball.AddComponent<MeshFilter>().sharedMesh = fb.sharedMesh;
            var bmr = ball.AddComponent<MeshRenderer>();
            bmr.sharedMaterial = med;
            bmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ball.SetActive(false);
            var mf = mouth.AddComponent<MouthFire>();
            var mso = new SerializedObject(mf);
            mso.FindProperty("_ball").objectReferenceValue = ball.transform;
            mso.ApplyModifiedPropertiesWithoutUndo();

            // Makineli seri → ateş topu mermisi (değince patlar, roket mermisiyle aynı alan hasarı).
            var bullet = new GameObject("MedFireballBullet") { layer = ProjectileLayer };
            try
            {
                var proj = bullet.AddComponent<EnemyProjectile>();
                var pso = new SerializedObject(proj);
                pso.FindProperty("_hitRadius").floatValue = 0.3f;
                pso.FindProperty("_splashRadius").floatValue = 1.2f;
                pso.ApplyModifiedPropertiesWithoutUndo();
                FireballModel(bullet.transform, fb, med, FireballBulletSize);
                AddFireTrail(bullet, trailMat, 0.5f, 0.22f);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(bullet, FireballBulletPath);
                var d = new SerializedObject(medData);
                d.FindProperty("_projectilePrefab").objectReferenceValue = saved.GetComponent<EnemyProjectile>();
                d.FindProperty("_aimLineMaterial").objectReferenceValue = null; // lazer yok, ağız ateşi var
                d.ApplyModifiedPropertiesWithoutUndo();
            }
            finally { Object.DestroyImmediate(bullet); }

            // Alan saldırısı: roket şablonu ateş topu olur, ağızdan çıkar (yerdeki kırmızı daire aynı).
            Transform rocket = root.transform.Find("RocketFX/Rocket");
            if (rocket != null)
            {
                foreach (string child in new[] { "Body", "Flame" })
                {
                    Transform t = rocket.Find(child);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
                FireballModel(rocket, fb, med, FireballStrikeSize);
                AddFireTrail(rocket.gameObject, trailMat, 0.9f, 0.3f);
            }
            var strike = root.GetComponent<HeliRocketStrike>();
            if (strike != null)
            {
                var sso = new SerializedObject(strike);
                sso.FindProperty("_launchPoint").objectReferenceValue = mouth.transform;
                sso.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (string path in new[] { "Shadow/Rotor", "Shadow/SpotPivot/Spot" })
            {
                Transform t = root.transform.Find(path);
                if (t != null && t.TryGetComponent(out Renderer rr)) rr.enabled = false;
            }
            log.AppendLine("OK ejderha: kanat çırpma, ağız ateşi, ateş topu mermisi + alan saldırısı");
        }

        // Ateş topu modeli: alevli kuyruk (merkezden en uzak köşe) −Z'ye, en uzun kenar `size` birim.
        private static void FireballModel(Transform parent, MeshFilter fb, Material med, float size)
        {
            var go = new GameObject("Model") { layer = parent.gameObject.layer };
            Transform t = go.transform;
            t.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = fb.sharedMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = med;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Quaternion rot = fb.transform.rotation;
            Vector3 scale = fb.transform.lossyScale;
            Vector3 far = Vector3.back;
            float farSqr = 0f;
            foreach (Vector3 v in fb.sharedMesh.vertices)
            {
                Vector3 p = rot * Vector3.Scale(v, scale);
                if (p.sqrMagnitude > farSqr) { farSqr = p.sqrMagnitude; far = p; }
            }
            t.localRotation = Quaternion.FromToRotation(far.normalized, Vector3.back) * rot;
            Vector3 ext = Vector3.Scale(fb.sharedMesh.bounds.size, scale);
            t.localScale = scale * (size / Mathf.Max(0.001f, Mathf.Max(ext.x, ext.y, ext.z)));
        }

        private static void AddFireTrail(GameObject go, Material mat, float width, float time)
        {
            var trail = go.GetComponent<TrailRenderer>();
            if (trail == null) trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = mat;
            trail.time = time;
            trail.minVertexDistance = 0.15f;
            trail.widthMultiplier = 1f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, width), new Keyframe(1f, 0f));
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(FireTrailHead, 0f), new GradientColorKey(FireTrailTail, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            trail.colorGradient = grad;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.autodestruct = false;
            trail.emitting = true;
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
                // Dik şalgamın sivri kökü 60° kameradan gövdenin arkasında kalıyor, yarısı gömülü gibi okunuyordu: yan yatar.
                if (mesh == "P_MedTurnip") model.transform.localRotation = Quaternion.Euler(0f, 0f, 75f) * model.transform.localRotation;
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

        // Kılıcın kabzası blob merkezinden bu kadar ileride (görünüm mesh'i uzayı, gövde yarıçapı ~0,5).
        private const float SwordGripOffset = 0.42f;

        private static void BakeWeapons(Material med, System.Text.StringBuilder log)
        {
            string matPath = WeaponFolder + "/Mat_MedWeapon.mat";
            var weaponMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (weaponMat == null) AssetDatabase.CreateAsset(new Material(med) { name = "Mat_MedWeapon" }, matPath);
            else { weaponMat.shader = med.shader; weaponMat.CopyPropertiesFromMaterial(med); EditorUtility.SetDirty(weaponMat); }
            int n = 0;
            for (int f = 1; f <= 5; f++)
                if (BakeAlongAxis($"{WeaponFolder}/G_Stick_{f}.fbx", $"G_MedSword_{f}", $"{WeaponFolder}/G_MedStick_{f}.asset", SwordGripOffset)) n++;
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

        private static bool BakeAlongAxis(string targetPath, string sourceMesh, string outPath, float gripOffset)
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
            grip += dirT * gripOffset; // kabza gövde yüzeyinde: kılıç blob'un içinden geçmesin
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
