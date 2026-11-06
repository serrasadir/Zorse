using System;
using System.Collections.Generic;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Entities.Pedestrians;
using BlobSurvivor.Entities.Vehicles;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Blender şehir kiti: Art/City altında mesh başına .fbx + CityPalette.png + level01.json.
    // Tüm kit tek palet materyalini paylaşır (B_Restaurant hariç: Art/Buildings/Restaurant/Mat_Restaurant).
    public class CityKitImportSettings : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(CityKitPipeline.ArtRoot + "/")) return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None; // materyali araç atar
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.importNormals = ModelImporterNormals.Import; // düz gölgeli yüzler korunsun
            importer.meshCompression = ModelImporterMeshCompression.Low;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CityKitPipeline.ArtRoot + "/")) return;
            TextureImporter importer = (TextureImporter)assetImporter;

            // Tile'lanan desen dokuları (kaldırım karosu, su): tekrar eder, mipmap'li, sıkıştırılmış.
            if (assetPath.StartsWith(CityKitPipeline.ArtRoot + "/Textures/"))
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 2;
                importer.maxTextureSize = 256;
                return;
            }

            // Palet hücreleri düz renk: filtre/mipmap/sıkıştırma hücreleri birbirine karıştırır.
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }

    public static class CityKitPipeline
    {
        internal const string ArtRoot = "Assets/_Project/Art/City";
        private const string PrefabRoot = "Assets/_Project/Prefabs/City";
        private const string CarPrefabRoot = PrefabRoot + "/Cars";
        private const string DataRoot = "Assets/_Project/ScriptableObjects/Consumables/City";
        private const string LayoutPath = ArtRoot + "/level01.json";
        private const string PalettePath = ArtRoot + "/CityPalette.png";
        private const string PaletteMaterialPath = ArtRoot + "/Mat_CityPalette.mat";
        // Şehir kiti v2 (zorse-blender/build_kit2.py): kendi paleti (CityPaletteV2, 8x8) ve materyali. Eski kitle yan yana
        // durur; mesh adı Art/City/Kit2'de varsa v2 sayılır (LoadSourceMesh oradan okur, yükseklik sıkıştırması yok).
        internal const string Kit2Root = ArtRoot + "/Kit2";
        private const string PaletteV2Path = Kit2Root + "/CityPaletteV2.png";
        internal const string PaletteV2MaterialPath = Kit2Root + "/Mat_CityPaletteV2.mat";
        private const string RestaurantMaterialPath = "Assets/_Project/Art/Buildings/Restaurant/Mat_Restaurant.mat";
        private const string PedestrianPrefabPath = "Assets/_Project/Prefabs/Consumables/Pedestrian.prefab";
        private const string PedestrianDataPath = "Assets/_Project/ScriptableObjects/Pedestrians/PedestrianData.asset";
        private const string RestaurantMesh = "B_Restaurant";
        private const string RootName = "City_Level01";
        private const float GroundClearance = 0.02f; // 'Ground' plane ile z-fighting olmasın
        private const float PedestrianY = 0.1f;      // yol ve kaldırım (0.14) arası: ayak ikisinde de batmıyor
        private const float PedestrianMapHalfExtent = 19f;
        private const float CarY = 0.06f;            // yol yüzeyinin (0.05) hemen üstü, modellerin pivotu tabanda
        private const float PoliceRootY = 0.6f;      // EnemySpawner.EnemySpawnY (0.65) eksi yol yüksekliği (0.05)
        private const float EliteRootY = 1f;         // NavMeshAgent.baseOffset: pivot NavMesh'in 1 birim üstünde
        private const float CoinModelY = 0.2f;       // CoinSpawner kökü 0.15'te tutar; merkez 0.35'te kaldırımın üstünde kalır

        // 2026-11-02: export Blender +Y'yi Unity -Z'ye çeviriyor (Unity'de ölçüldü). Ön yüzleri +Y'de modellenen
        // insanlar/araçlar hareket yönünün tersine, binalar kameranın tersine bakıyordu. Yerleşim (level01.json)
        // yaw'ları 0 veya hareketle belirlendiği için modele 180° eklemek yeterli. P_ eşyaları ve L_ zemin bilinçli
        // olarak dışarıda: yaw'lı yerleşimleri var, doğrulanmadan çevrilmedi.
        private static readonly Quaternion FacingFix = Quaternion.Euler(0f, 180f, 0f);
        // 2026-11-04 (Serra: "çok uzun binalar, blob o kadar büyümüyor, yemesi garip"): sabit ×0.75 yerine
        // sıkıştırma eğrisi — diz noktasının üstündeki boy TallBuildingExcessScale ile kısalır, uzun bina daha çok kısalır,
        // sıralama korunur. Orijinal boy → yeni: kule 10.9→6.4, ofis 9.1→5.8, otel 7.8→5.3, apartman 6.7→4.9.
        internal const float TallBuildingHeight = 4f;
        internal const float TallBuildingExcessScale = 0.35f;

        internal static float TallBuildingYScale(float height) =>
            height > TallBuildingHeight
                ? (TallBuildingHeight + (height - TallBuildingHeight) * TallBuildingExcessScale) / height
                : 1f;
        // 2026-11-03 (Serra): polis yayalarla (1.16) neredeyse aynı boydaydı (1.20), kalabalıkta tehdit okunmuyordu.
        // ×1.15 ≈ 1.38: yayadan belirgin büyük, elitten (~1.58) belirgin küçük. Sadece görsel; kapsül zaten 2 birim.
        // 2026-11-03 (Serra): polis 1.15 → ×1.15 daha (1.3225), elit ×1.10. Sadece görsel; kapsül/oynanış aynı.
        internal const float PoliceModelScale = 1.3225f;
        internal const float EliteModelScale = 1.1f;
        // 2026-11-04 (Serra): miniboss "çok küçük", en az 2 kat. Değiştirince collider/agent de ölçeklenir.
        internal const float MinibossScale = 2.5f; // 3 olursa tank (5.4 geniş) en dar sokağa (~5.2) sığmaz

        [Serializable]
        internal class CityItem
        {
            public string mesh;
            public float x, y, z, yaw, sx, sy, sz;
        }

        [Serializable]
        private class CityLayout
        {
            public CityItem[] items;
        }

        [MenuItem("Tools/Blob.io/Şehir Kur (Level01)")]
        private static void Build() => BuildCity(false);

        // GDD_v2 Karar 15: Level01 ortada, çevresi mahalle şablonlarıyla dolu sınırlı büyük harita.
        [MenuItem("Tools/Blob.io/Şehir Kur (Büyük)")]
        private static void BuildLarge() => BuildCity(true);

        // 2026-11-04: performans denemesi. Büyük şehri kurar, bina noktalarına kit2 binalarını koyar (restoran kalır).
        // Eski şehre dönmek: Şehir Kur (Büyük). Yerleşim geçici: v2 mahalle sistemi gelince bu menü kalkar.
        // 2026-11-04: şehir v2 (zorse-blender/build_city2.py --export → Kit2/city_v2.json). Mahalle sistemi: yollar, kaldırımlar,
        // yaya/araba/NavMesh büyük şehirle aynı; blokların içi tamamen kit2 (bina, eşya, blok zemini). Eski şehre dönmek: Şehir Kur (Büyük).
        [MenuItem("Tools/Blob.io/Şehir Kur (v2)")]
        private static void BuildV2() => BuildCity(true, v2: true);

        [MenuItem("Tools/Blob.io/Şehir Kur (Kit2 Deneme)")]
        private static void BuildKit2Test() => BuildCity(true, kit2Test: true);

        private static readonly string[] Kit2Small =
        {
            "B_Bakery", "B_Pizzeria", "B_Barber", "B_AptSage", "B_AptBlue", "B_AptBrick", "B_Cafe", "B_Bookstore",
            "B_AptCumba", "B_AptPink",
        };
        private static readonly string[] Kit2Large = { "B_GrandHotel", "B_PoliceStation", "B_Hospital", "B_Cinema", "B_Lokanta" };

        internal static bool IsKit2(string mesh) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Kit2Root}/{mesh}.fbx") != null;

        // Konumdan deterministik seçim (her kurulumda aynı). Geniş eski binaların yerine geniş v2 binası.
        private static CityItem SwapToKit2(CityItem item)
        {
            if (!item.mesh.StartsWith("B_") || item.mesh == RestaurantMesh) return item;
            string[] pool = item.sx >= 4.4f ? Kit2Large : Kit2Small;
            int hash = Mathf.Abs(Mathf.RoundToInt(item.x * 7.31f) * 73856093 ^ Mathf.RoundToInt(item.z * 3.17f) * 19349663);
            return new CityItem { mesh = pool[hash % pool.Length], x = item.x, y = item.y, z = item.z, yaw = 0f };
        }

        private const string V2LayoutPath = Kit2Root + "/city_v2.json";

        private static void BuildCity(bool large, bool kit2Test = false, bool v2 = false)
        {
            AssetDatabase.Refresh();

            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (json == null)
            {
                Debug.LogError($"[CityKitPipeline] {LayoutPath} bulunamadı.");
                return;
            }

            CityLayout layout = JsonUtility.FromJson<CityLayout>(json.text);
            Material palette = GetPaletteMaterial();
            Material restaurant = AssetDatabase.LoadAssetAtPath<Material>(RestaurantMaterialPath);
            if (palette == null || restaurant == null)
            {
                Debug.LogError("[CityKitPipeline] Materyal bulunamadı (URP Simple Lit, palet dokusu veya Mat_Restaurant).");
                return;
            }

            EnsureFolder("Assets/_Project/Prefabs", "City");
            EnsureFolder("Assets/_Project/ScriptableObjects/Consumables", "City");

            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
            List<CarController> cars = new List<CarController>();
            HashSet<string> carMeshes = new HashSet<string>();
            foreach (CityItem item in layout.items)
            {
                if (IsVehicle(item.mesh))
                {
                    if (!carMeshes.Add(item.mesh)) continue;
                    CarController car = BuildCarPrefab(item.mesh, palette);
                    if (car == null) return;
                    cars.Add(car);
                    continue;
                }

                if (prefabs.ContainsKey(item.mesh) || v2) continue; // v2: şehir eşyaları yerleşimde gerektikçe kurulur
                GameObject prefab = BuildPrefab(item, item.mesh == RestaurantMesh ? restaurant : palette);
                if (prefab == null) return;
                prefabs.Add(item.mesh, prefab);
            }

            PedestrianData man = BuildPedestrian("H_Man", "Man", "Erkek Yaya", palette);
            PedestrianData woman = BuildPedestrian("H_Woman", "Woman", "Kadın Yaya", palette);
            AssetDatabase.SaveAssets();

            GameObject existing = GameObject.Find(RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Şehir Kur");
            root.transform.position = new Vector3(0f, GroundClearance, 0f);
            root.AddComponent<CityLevel>();

            IEnumerable<CityItem> placed = large ? CityExpansion.Compose(layout.items) : (IEnumerable<CityItem>)layout.items;
            if (v2)
            {
                TextAsset v2Json = AssetDatabase.LoadAssetAtPath<TextAsset>(V2LayoutPath);
                if (v2Json == null)
                {
                    Debug.LogError($"[CityKitPipeline] {V2LayoutPath} yok: önce build_city2.py --export.");
                    return;
                }
                placed = JsonUtility.FromJson<CityLayout>(v2Json.text).items;
            }
            Material paletteV2 = kit2Test || v2 ? GetPaletteV2Material() : null;
            if ((kit2Test || v2) && paletteV2 == null)
            {
                Debug.LogError($"[CityKitPipeline] {PaletteV2Path} yok: önce build_kit2.py --export.");
                return;
            }
            int swapped = 0;
            foreach (CityItem source in placed)
            {
                if (IsVehicle(source.mesh)) continue; // araçlar sahnede durmaz, CarSpawner havuzundan gelir
                CityItem item = kit2Test ? SwapToKit2(source) : source;
                if (item != source) swapped++;
                if (!prefabs.ContainsKey(item.mesh))
                {
                    GameObject built = BuildPrefab(item, IsKit2(item.mesh) ? paletteV2 : palette);
                    if (built == null) return;
                    prefabs.Add(item.mesh, built);
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[item.mesh], root.transform);
                instance.transform.localPosition = new Vector3(item.x, item.y, item.z);
                instance.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
            }

            float mapHalfExtent = PedestrianMapHalfExtent;
            if (large)
            {
                CityExpansion.BuildGround(root.transform, palette); // L_Ground 40x40'ın yerine
                CityExpansion.BuildBounds(root.transform);
                mapHalfExtent = CityExpansion.HalfExtent - 1f;
            }

            AssignPedestrians(man, woman, mapHalfExtent);
            if (large) AssignEnemyMapExtent(mapHalfExtent);
            AssignCars(cars);
            if (large) RoadNetworkGenerator.Generate(CityExpansion.StreetLines());
            else RoadNetworkGenerator.Generate();
            BakeNavMesh();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log($"[CityKitPipeline] Şehir kuruldu: {root.transform.childCount} obje, {cars.Count} araç tipi" +
                      (kit2Test ? $", {swapped} bina kit2 ile değişti" : "") + ". NavMesh bake bitince sahneyi kaydet (Cmd+S).");
        }

        // Kapsül görselini modelle değiştirir; collider, NavMeshAgent ve EnemyBase ayarlarına dokunmaz.
        [MenuItem("Tools/Blob.io/Polis Modellerini Uygula")]
        internal static void ApplyPoliceModels()
        {
            AssetDatabase.Refresh();

            Material palette = GetPaletteMaterial();
            if (palette == null)
            {
                Debug.LogError("[CityKitPipeline] Palet materyali bulunamadı.");
                return;
            }

            bool police = ApplyEnemyModel("Assets/_Project/Prefabs/Enemies/Enemy_Police.prefab", "H_Police", -PoliceRootY, palette);
            bool elite = ApplyEnemyModel("Assets/_Project/Prefabs/Enemies/Enemy_ElitePolice.prefab", "H_Elite", -EliteRootY, palette);
            bool tank = ApplyMinibossModel(palette);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CityKitPipeline] Polis modelleri: normal={police}, elit={elite}, miniboss tank={tank}.");
        }

        // Miniboss = polis tankı. Eski kapsül (1.5, 1.2, 1) ölçekliydi: kök 1'e çekilir, gövde collider'ı tanka göre büyür.
        // Pivot NavMeshAgent.baseOffset (1) kadar yukarıda olduğu için model elit polis gibi -1'e iner.
        [MenuItem("Tools/Blob.io/Miniboss Tank Modelini Uygula")]
        private static void ApplyMinibossMenu() => ApplyMinibossModel();

        internal static bool ApplyMinibossModel() => ApplyMinibossModel(GetPaletteMaterial());

        private static bool ApplyMinibossModel(Material palette)
        {
            const string prefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Miniboss.prefab";
            if (palette == null)
            {
                Debug.LogError("[CityKitPipeline] Palet materyali bulunamadı.");
                return false;
            }
            if (!ApplyEnemyModel(prefabPath, "V_PoliceTank", -EliteRootY, palette)) return false;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            contents.transform.localScale = Vector3.one;
            Transform model = contents.transform.Find("Model");
            if (model != null) model.localScale *= MinibossScale; // taban -1'de (zemin), ölçek tabandan büyür
            CapsuleCollider capsule = contents.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.radius = 0.95f * MinibossScale;        // tank 1.8 x 2.5 (namlu hariç)
                capsule.height = 2f * MinibossScale;
                capsule.center = new Vector3(0f, MinibossScale - 1f, 0f); // zeminden (-1) kule tepesine
            }
            UnityEngine.AI.NavMeshAgent agent = contents.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.radius = 0.65f * MinibossScale;
                agent.height = 2.6f * MinibossScale;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();
            return true;
        }

        // 2026-11-04 (Serra): final boss = polis helikopteri. Hiyerarşi:
        //   Enemy_FinalBoss (kök: EnemyBase + NavMeshAgent + FinalBossController + kapsül; yerde)
        //     Model (EnemyBase'in atış geri tepmesi bunu oynatır; y = -baseOffset = zemin)
        //       Heli (HelicopterVisual: irtifa/süzülme/eğilme; yön düzeltmesi + HeliScale)
        //         Body, MainRotorPivot/Mesh, TailRotorPivot/Mesh
        // Rotorlar pivot'a takılır: FBX'in kök dönüşü ne olursa olsun pivot'un Y/X ekseni dünyaya göre doğru.
        // Kapsül gövdeye göre irtifa 0'da kurulur; FinalBossController çalışırken irtifa kadar yukarı taşır.
        internal const float HeliScale = 1.5f;
        // build_police_heli.py çıktısı ("Unity local pos"): ana rotor göbeği (0, 1.74, 0.05), kuyruk (0.16, 1.32, -2.6)
        // — burun +Z olacak şekilde. Ham FBX'te yön ters olabilir; kuyruk tarafı gövdenin uzun ucundan bulunur.
        private static readonly Vector3 HeliMainHub = new Vector3(0f, 1.74f, 0.05f);
        private static readonly Vector3 HeliTailHub = new Vector3(0.16f, 1.32f, -2.6f);

        [MenuItem("Tools/Blob.io/Final Boss Helikopterini Uygula")]
        private static void ApplyFinalBossHelicopterMenu()
        {
            AssetDatabase.Refresh();
            Debug.Log($"[CityKitPipeline] Final boss helikopteri uygulandı: {ApplyFinalBossHelicopter()}.");
        }

        internal static bool ApplyFinalBossHelicopter()
        {
            const string prefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_FinalBoss.prefab";
            Material palette = GetPaletteMaterial();
            MeshFilter bodySrc = LoadSourceMesh("V_PoliceHeli");
            MeshFilter mainSrc = LoadSourceMesh("V_PoliceHeliRotor");
            MeshFilter tailSrc = LoadSourceMesh("V_PoliceHeliTailRotor");
            if (palette == null || bodySrc == null || mainSrc == null || tailSrc == null)
            {
                Debug.LogError("[CityKitPipeline] Helikopter: palet ya da V_PoliceHeli*.fbx bulunamadı.");
                return false;
            }

            MeshFilter rocketSrc = LoadSourceMesh("V_HeliRocket");
            MeshFilter flameSrc = LoadSourceMesh("V_HeliRocketFlame");
            if (rocketSrc == null || flameSrc == null)
            {
                Debug.LogError("[CityKitPipeline] Helikopter: V_HeliRocket*.fbx bulunamadı (zorse-blender/build_rocket.py).");
                return false;
            }

            Material shadowBody = EnsureShadowMaterial("HeliShadowBody", 128, ShadowBodyAlpha);
            Material shadowRotor = EnsureShadowMaterial("HeliShadowRotor", 256, ShadowRotorAlpha);
            s_shadowBody = shadowBody;
            s_shadowRotor = shadowRotor;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform oldModel = contents.transform.Find("Model");
                if (oldModel != null) UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
                Transform oldShadow = contents.transform.Find("Shadow");
                if (oldShadow != null) UnityEngine.Object.DestroyImmediate(oldShadow.gameObject);
                Transform oldFx = contents.transform.Find("RocketFX");
                if (oldFx != null) UnityEngine.Object.DestroyImmediate(oldFx.gameObject);
                MeshRenderer oldRenderer = contents.GetComponent<MeshRenderer>();
                if (oldRenderer != null) UnityEngine.Object.DestroyImmediate(oldRenderer);
                MeshFilter oldFilter = contents.GetComponent<MeshFilter>();
                if (oldFilter != null) UnityEngine.Object.DestroyImmediate(oldFilter);
                contents.transform.localScale = Vector3.one; // eski kapsül (1.8, 1.5, 1) ölçekliydi

                UnityEngine.AI.NavMeshAgent agent = contents.GetComponent<UnityEngine.AI.NavMeshAgent>();
                float rootY = agent != null ? agent.baseOffset : EliteRootY;
                int layer = contents.layer;

                Transform model = NewChild(contents.transform, "Model", new Vector3(0f, -rootY, 0f), layer);
                Transform heli = NewChild(model, "Heli", Vector3.zero, layer);

                MeshRenderer body = AddMeshChild(heli, "Body", bodySrc, palette, Vector3.zero, layer);

                // Ham modelde kuyruk +Z'deyse (Blender export'u ön cepheyi -Z'ye çeviriyor, bkz. FacingFix) 180° çevir.
                Bounds raw = body.bounds; // Heli henüz birim/dönüşsüz, kök orijinde: prefab-yerel
                float rel = model.position.z;
                bool tailAtPlusZ = raw.max.z - rel > rel - raw.min.z;
                heli.localRotation = tailAtPlusZ ? FacingFix : Quaternion.identity;
                // Göbek konumları burun +Z çerçevesinde verildi; Heli'nin dönüşü ham çerçeveye taşır.
                Vector3 mainHub = Quaternion.Inverse(heli.localRotation) * HeliMainHub;
                Vector3 tailHub = Quaternion.Inverse(heli.localRotation) * HeliTailHub;

                Transform mainPivot = NewChild(heli, "MainRotorPivot", mainHub, layer);
                AddMeshChild(mainPivot, "Mesh", mainSrc, palette, Vector3.zero, layer);
                Transform tailPivot = NewChild(heli, "TailRotorPivot", tailHub, layer);
                AddMeshChild(tailPivot, "Mesh", tailSrc, palette, Vector3.zero, layer);

                heli.localScale = Vector3.one * HeliScale;

                // Kapsül: gövde boyunca (Z), irtifa 0'da. Rotor diski dahil değil (kalabalıkta çok geniş isabet alanı olurdu).
                Bounds b = body.bounds;
                CapsuleCollider capsule = contents.GetComponent<CapsuleCollider>();
                if (capsule == null) capsule = contents.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = Mathf.Max(b.extents.x, b.extents.y);
                capsule.height = Mathf.Max(b.size.z, capsule.radius * 2f);
                capsule.center = b.center - contents.transform.position;

                // Gerçek gölge kapalı: sahte yer gölgesi (HeliShadow) tam altında, güneşle kayık ikinci gölge olmasın.
                foreach (MeshRenderer r in heli.GetComponentsInChildren<MeshRenderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                MeshRenderer mainRotorRenderer = mainPivot.GetComponentInChildren<MeshRenderer>();
                BuildHeliShadow(contents.transform, body.bounds, mainRotorRenderer != null ? mainRotorRenderer.bounds : body.bounds,
                    mainPivot, rootY, layer);

                HelicopterVisual visual = heli.gameObject.AddComponent<HelicopterVisual>();
                SerializedObject so = new SerializedObject(visual);
                so.FindProperty("_mainRotor").objectReferenceValue = mainPivot;
                so.FindProperty("_tailRotor").objectReferenceValue = tailPivot;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (contents.GetComponent<FinalBossController>() == null)
                    contents.AddComponent<FinalBossController>();
                BuildRocketStrike(contents, palette, rocketSrc, flameSrc, layer);

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log($"[CityKitPipeline] Helikopter: kuyruk ham +Z={tailAtPlusZ}, kapsül r={capsule.radius:F2} h={capsule.height:F2} c={capsule.center}.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
                AssetDatabase.SaveAssets();
            }
        }

        // --- Helikopter yer gölgesi (HeliShadow) ---
        private const string FxArtFolder = "Assets/_Project/Art/FX";
        private const float ShadowGroundY = 0.15f; // kaldırım (0.12) üstü, z-fighting olmasın
        private static Material s_shadowBody;
        private static Material s_shadowRotor;

        private static void BuildHeliShadow(Transform root, Bounds body, Bounds rotor, Transform rotorPivot, float rootY, int layer)
        {
            Transform shadow = NewChild(root, "Shadow", new Vector3(0f, -rootY + ShadowGroundY, 0f), layer);
            Vector3 rootPos = root.position;

            // Gövde lekesi: gövdenin yerdeki izi kadar elips (helikopterle birlikte döner — kökün çocuğu).
            Transform bodyQuad = NewQuad(shadow, "Body", s_shadowBody, layer);
            bodyQuad.localPosition = new Vector3(body.center.x - rootPos.x, 0f, body.center.z - rootPos.z);
            bodyQuad.localScale = new Vector3(body.size.x * 1.4f, body.size.z * 1.05f, 1f);

            // Pervane gölgesi: rotor diski çapında, göbeğin altında; açıyı HeliShadow her kare rotordan kopyalar.
            Transform rotorQuad = NewQuad(shadow, "Rotor", s_shadowRotor, layer);
            Vector3 hub = rotorPivot.position - rootPos;
            rotorQuad.localPosition = new Vector3(hub.x, 0.01f, hub.z);
            float diameter = Mathf.Max(rotor.size.x, rotor.size.z);
            rotorQuad.localScale = new Vector3(diameter, diameter, 1f);

            // Projektör ışığı (#71): burnun altındaki sarı projektörden (build_police_heli.py "searchlight") öne düşen
            // yumuşak elips. Pivot projektörün tam altında; HeliShadow pivot'u yavaşça sağa sola çevirir.
            Transform spotPivot = NewChild(shadow, "SpotPivot", new Vector3(0f, 0.005f, HeliSearchlightZ), layer);
            Transform spotQuad = NewQuad(spotPivot, "Spot", s_shadowBody, layer);
            spotQuad.localPosition = new Vector3(0f, 0f, SpotForward);
            spotQuad.localScale = new Vector3(SpotSize.x, SpotSize.y, 1f);

            HeliShadow component = shadow.gameObject.AddComponent<HeliShadow>();
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("_spot").objectReferenceValue = spotQuad.GetComponent<Renderer>();
            so.FindProperty("_spotPivot").objectReferenceValue = spotPivot;
            so.FindProperty("_body").objectReferenceValue = bodyQuad.GetComponent<Renderer>();
            so.FindProperty("_rotor").objectReferenceValue = rotorQuad.GetComponent<Renderer>();
            so.FindProperty("_rotorSource").objectReferenceValue = rotorPivot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Projektör: build_police_heli.py'de burun altı y=1.16 (Blender) → burun +Z çerçevesinde z = 1.16 × HeliScale.
        private const float HeliSearchlightZ = 1.16f * HeliScale;
        private const float SpotForward = 2.2f;
        private static readonly Vector2 SpotSize = new Vector2(2.6f, 3.6f);

        // --- Helikopter roket alan saldırısı (HeliRocketStrike, #71) ---
        // RocketFX altında üç pasif şablon: Rocket (Body = mesh, Flame = nozuldaki pivot), Marker (Ring + Fill),
        // Blast (Flash + Wave). HeliRocketStrike bunları sahne köküne kopyalayıp kullanır. Quad'lar birim boyda;
        // ölçek runtime'da yarıçapa göre verilir.
        private const float RocketScale = 1.6f;

        private static void BuildRocketStrike(GameObject root, Material palette, MeshFilter rocketSrc, MeshFilter flameSrc, int layer)
        {
            Material ring = EnsureShadowMaterial("StrikeRing", 256, StrikeRingAlpha);
            Material fill = EnsureShadowMaterial("StrikeFill", 128, StrikeFillAlpha);

            Transform fx = NewChild(root.transform, "RocketFX", Vector3.zero, layer);

            Transform rocket = NewChild(fx, "Rocket", Vector3.zero, layer);
            Transform body = NewChild(rocket, "Body", Vector3.zero, layer);
            MeshRenderer rocketRenderer = AddMeshChild(body, "Mesh", rocketSrc, palette, Vector3.zero, layer);
            // Burun +Z olsun: kanatlı (geniş) uç kuyruk. Ham FBX ekseni ters olabilir (bkz. FacingFix).
            Transform mesh = rocketRenderer.transform;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            Vector3[] verts = rocketSrc.sharedMesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = mesh.localRotation * Vector3.Scale(verts[i], mesh.localScale);
                minZ = Mathf.Min(minZ, verts[i].z);
                maxZ = Mathf.Max(maxZ, verts[i].z);
            }
            float span = maxZ - minZ, wideMin = 0f, wideMax = 0f;
            foreach (Vector3 v in verts)
            {
                float radial = new Vector2(v.x, v.y).magnitude;
                if (v.z < minZ + span * 0.25f) wideMin = Mathf.Max(wideMin, radial);
                if (v.z > maxZ - span * 0.25f) wideMax = Mathf.Max(wideMax, radial);
            }
            bool tailAtPlusZ = wideMax > wideMin;
            body.localRotation = tailAtPlusZ ? FacingFix : Quaternion.identity;
            body.localScale = Vector3.one * RocketScale;
            float rearZ = (tailAtPlusZ ? -maxZ : minZ) * RocketScale;

            Transform flame = NewChild(rocket, "Flame", new Vector3(0f, 0f, rearZ), layer);
            Transform flameFix = NewChild(flame, "Fix", Vector3.zero, layer);
            flameFix.localRotation = body.localRotation;
            flameFix.localScale = body.localScale;
            MeshRenderer flameRenderer = AddMeshChild(flameFix, "Mesh", flameSrc, palette, Vector3.zero, layer);
            flameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Transform marker = NewChild(fx, "Marker", Vector3.zero, layer);
            Transform fillQuad = NewQuad(marker, "Fill", fill, layer);
            fillQuad.localPosition = Vector3.zero;
            Transform ringQuad = NewQuad(marker, "Ring", ring, layer);
            ringQuad.localPosition = new Vector3(0f, 0.01f, 0f);

            Transform blast = NewChild(fx, "Blast", Vector3.zero, layer);
            Transform flash = NewQuad(blast, "Flash", fill, layer);
            flash.localPosition = new Vector3(0f, 0.02f, 0f);
            Transform wave = NewQuad(blast, "Wave", ring, layer);
            wave.localPosition = new Vector3(0f, 0.03f, 0f);

            foreach (Transform t in new[] { rocket, marker, blast }) t.gameObject.SetActive(false);

            HeliRocketStrike strike = root.GetComponent<HeliRocketStrike>();
            if (strike == null) strike = root.AddComponent<HeliRocketStrike>();
            SerializedObject so = new SerializedObject(strike);
            so.FindProperty("_rocketTemplate").objectReferenceValue = rocket.gameObject;
            so.FindProperty("_markerTemplate").objectReferenceValue = marker.gameObject;
            so.FindProperty("_blastTemplate").objectReferenceValue = blast.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[CityKitPipeline] Roket: kuyruk ham +Z={tailAtPlusZ}, nozul z={rearZ:F2}.");
        }

        // Kenarda net halka (hedef dairesinin sınırı); içi boş, iç diski Fill çizer.
        private static float StrikeRingAlpha(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return Mathf.SmoothStep(0.82f, 0.88f, r) * (1f - Mathf.SmoothStep(0.95f, 1f, r));
        }

        // Dolu disk, kenarı yumuşak (dolan uyarı + patlama parlaması).
        private static float StrikeFillAlpha(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return 1f - Mathf.SmoothStep(0.88f, 1f, r);
        }

        private static Transform NewQuad(Transform parent, string name, Material material, int layer)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = name;
            quad.layer = layer;
            quad.transform.SetParent(parent, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // yere yatık, yüzü yukarı
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad.transform;
        }

        private delegate float ShadowAlpha(float x, float y); // -1..1 doku koordinatı → 0..1 opaklık

        // Merkezden kenara yumuşak sönen leke.
        private static float ShadowBodyAlpha(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return 1f - Mathf.SmoothStep(0.45f, 1f, r);
        }

        // 4 kanatlı pervane + göbek; kenarlar yumuşak, uçlara doğru hafif incelir.
        private static float ShadowRotorAlpha(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 1f) return 0f;
            float hub = 1f - Mathf.SmoothStep(0.06f, 0.1f, r);

            float angle = Mathf.Atan2(y, x);
            float sector = Mathf.PI * 0.5f;
            float local = Mathf.Repeat(angle + sector * 0.5f, sector) - sector * 0.5f; // en yakın kanada açı
            float distance = Mathf.Abs(Mathf.Sin(local)) * r;                           // kanat eksenine dik mesafe
            float halfWidth = Mathf.Lerp(0.05f, 0.035f, r);
            float blade = (1f - Mathf.SmoothStep(halfWidth * 0.6f, halfWidth, distance))
                        * (1f - Mathf.SmoothStep(0.9f, 1f, r))
                        * (Mathf.Cos(local) > 0f ? 1f : 0f);
            return Mathf.Max(hub, blade);
        }

        // Doku (PNG) + URP Unlit saydam materyal; varsa yeniden üretir. Materyal prefab'da referanslı olduğu için
        // saydam shader varyantı build'de ayıklanmaz (bkz. ShaderStripGuard / ghost materyaller).
        private static Material EnsureShadowMaterial(string name, int size, ShadowAlpha alphaAt)
        {
            if (!AssetDatabase.IsValidFolder(FxArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "FX");

            string texturePath = $"{FxArtFolder}/{name}.png";
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2f - 1f;
                float y = (py + 0.5f) / size * 2f - 1f;
                pixels[py * size + px] = new Color(1f, 1f, 1f, Mathf.Clamp01(alphaAt(x, y)));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            System.IO.File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = size;
            importer.SaveAndReimport();
            Texture2D imported = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            string materialPath = $"{FxArtFolder}/Mat_{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", imported);
            material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.6f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform NewChild(Transform parent, string name, Vector3 localPosition, int layer)
        {
            GameObject go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        // FBX'in kök dönüşü/ölçeği korunur (AddModel ile aynı), yön düzeltmesi üst objede yapılır.
        private static MeshRenderer AddMeshChild(Transform parent, string name, MeshFilter source, Material material,
            Vector3 localPosition, int layer)
        {
            Transform child = NewChild(parent, name, localPosition, layer);
            child.localRotation = source.transform.rotation;
            child.localScale = source.transform.lossyScale;
            child.gameObject.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            MeshRenderer renderer = child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        // Kök ölçeği 1 yapılır: eski küpün 0.2 ölçeği modeli de toplama alanını da küçültürdü.
        [MenuItem("Tools/Blob.io/Coin Modelini Uygula")]
        internal static void ApplyCoinModel()
        {
            AssetDatabase.Refresh();

            Material palette = GetPaletteMaterial();
            if (palette == null)
            {
                Debug.LogError("[CityKitPipeline] Palet materyali bulunamadı.");
                return;
            }

            const string prefabPath = "Assets/_Project/Prefabs/Coins/Coin.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            Transform oldModel = contents.transform.Find("Model");
            if (oldModel != null) UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

            MeshRenderer cubeRenderer = contents.GetComponent<MeshRenderer>();
            if (cubeRenderer != null) UnityEngine.Object.DestroyImmediate(cubeRenderer);
            MeshFilter cubeFilter = contents.GetComponent<MeshFilter>();
            if (cubeFilter != null) UnityEngine.Object.DestroyImmediate(cubeFilter);

            MeshRenderer renderer = AddModel(contents, "C_Coin", palette);
            bool applied = renderer != null; // Unload sonrası renderer yok edilmiş olur
            if (applied)
            {
                contents.transform.localScale = Vector3.one;
                renderer.transform.localPosition = new Vector3(0f, CoinModelY, 0f);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // havuzda 40 coin

                BoxCollider box = contents.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.center = new Vector3(0f, CoinModelY, 0f);
                    box.size = new Vector3(0.55f, 0.55f, 0.55f);
                }
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CityKitPipeline] Coin modeli uygulandı: {applied}.");
        }

        private static bool ApplyEnemyModel(string prefabPath, string mesh, float modelY, Material material)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            Transform oldModel = contents.transform.Find("Model");
            if (oldModel != null) UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

            MeshRenderer capsuleRenderer = contents.GetComponent<MeshRenderer>();
            if (capsuleRenderer != null) UnityEngine.Object.DestroyImmediate(capsuleRenderer);
            MeshFilter capsuleFilter = contents.GetComponent<MeshFilter>();
            if (capsuleFilter != null) UnityEngine.Object.DestroyImmediate(capsuleFilter);

            MeshRenderer renderer = AddModel(contents, mesh, material);
            bool applied = renderer != null; // Unload sonrası renderer yok edilmiş olur
            if (applied)
            {
                renderer.gameObject.layer = contents.layer;
                renderer.transform.localPosition = new Vector3(0f, modelY, 0f);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(contents);
            return applied;
        }

        private static bool IsVehicle(string mesh) => mesh.StartsWith("V_");

        // Sahnedeki V_ prefab'ı yerine hareketli sürüm: eski CarPrefab ile aynı kurulum (Hazard, kinematik).
        private static CarController BuildCarPrefab(string mesh, Material material)
        {
            EnsureFolder(PrefabRoot, "Cars");

            int hazardLayer = LayerMask.NameToLayer("Hazard");
            GameObject root = new GameObject($"Car_{mesh}") { layer = hazardLayer };
            MeshRenderer renderer = AddModel(root, mesh, material);
            if (renderer == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return null;
            }
            renderer.gameObject.layer = hazardLayer;

            Bounds bounds = renderer.bounds;
            BoxCollider body = root.AddComponent<BoxCollider>();
            body.center = bounds.center;
            body.size = bounds.size;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            root.AddComponent<CarController>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{CarPrefabRoot}/{root.name}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<CarController>();
        }

        private static void AssignCars(List<CarController> cars)
        {
            CarSpawner spawner = UnityEngine.Object.FindAnyObjectByType<CarSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[CityKitPipeline] CarSpawner bulunamadı, araç havuzu bağlanmadı.");
                return;
            }

            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty prefabs = so.FindProperty("_carPrefabs");
            prefabs.arraySize = cars.Count;
            for (int i = 0; i < cars.Count; i++)
                prefabs.GetArrayElementAtIndex(i).objectReferenceValue = cars[i];
            so.FindProperty("_poolSize").intValue = 1; // tip başına; havuz gerekirse kendi büyür

            UnityEngine.Object carData = so.FindProperty("_carData").objectReferenceValue;
            so.ApplyModifiedProperties();

            if (carData == null) return;
            SerializedObject data = new SerializedObject(carData);
            data.FindProperty("_spawnYOffset").floatValue = CarY;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        // 2026-11-04: tank NavMesh'i ayrı bir surface — sahnedeki tüm surface'ler birlikte bake edilir.
        internal static void BakeNavMesh()
        {
            NavMeshSurface[] surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>();
            if (surfaces.Length == 0)
            {
                Debug.LogWarning("[CityKitPipeline] NavMeshSurface bulunamadı, bake atlandı.");
                return;
            }

            NavMeshAssetManager.instance.StartBakingSurfaces(surfaces);
        }

        // 2026-11-04 (Serra: "miniboss binaların köşelerine giriyor, çarpmasın"): NavMesh 0.5 yarıçaplı ajana
        // bake'li, tank 2 kat büyüdükten sonra ~1.8 yarı genişlikte — yol bulma onu bina kenarına 0.5'e kadar
        // sokuyordu. Tank için ayrı ajan tipi + aynı ayarlarla ikinci NavMeshSurface. Bina-bina arası en az
        // ~5.2 (4 sokak + iki kaldırım payı), tank 3.6 geniş: tüm sokaklar açık kalır. Tekrar çalıştırmak güvenli.
        internal const string TankAgentName = "Tank";
        internal const float TankAgentRadius = 2.25f; // = 0.9 × MinibossScale
        private const float TankAgentHeight = 2.5f;

        [MenuItem("Tools/Blob.io/Tank NavMesh'ini Kur")]
        private static void SetupTankNavMeshMenu() => Debug.Log(SetupTankNavMesh());

        internal static string SetupTankNavMesh()
        {
            int agentTypeID = EnsureAgentType(TankAgentName, TankAgentRadius, TankAgentHeight);

            NavMeshSurface[] surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>();
            NavMeshSurface main = Array.Find(surfaces, x => x.agentTypeID == 0);
            if (main == null) return "[CityKitPipeline] Tank NavMesh: ana (Humanoid) NavMeshSurface bulunamadı.";

            NavMeshSurface tank = Array.Find(surfaces, x => x.agentTypeID == agentTypeID);
            if (tank == null) tank = Undo.AddComponent<NavMeshSurface>(main.gameObject);
            tank.agentTypeID = agentTypeID;
            tank.collectObjects = main.collectObjects;
            tank.size = main.size;
            tank.center = main.center;
            tank.layerMask = main.layerMask;
            tank.useGeometry = main.useGeometry;
            tank.defaultArea = main.defaultArea;
            tank.ignoreNavMeshAgent = main.ignoreNavMeshAgent;
            tank.ignoreNavMeshObstacle = main.ignoreNavMeshObstacle;
            tank.overrideTileSize = main.overrideTileSize;
            tank.tileSize = main.tileSize;
            tank.overrideVoxelSize = main.overrideVoxelSize;
            tank.voxelSize = main.voxelSize;
            tank.minRegionArea = main.minRegionArea;
            tank.buildHeightMesh = main.buildHeightMesh;
            EditorUtility.SetDirty(tank);

            const string prefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Miniboss.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            UnityEngine.AI.NavMeshAgent agent = contents.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.agentTypeID = agentTypeID;
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();

            EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
            BakeNavMesh();
            return $"[CityKitPipeline] Tank NavMesh kuruldu: ajan tipi {agentTypeID} (r={TankAgentRadius}), miniboss bağlandı={agent != null}. " +
                   "Bake bitince sahneyi kaydet (Cmd+S).";
        }

        // Proje NavMesh ajan tiplerinde adıyla arar, yoksa oluşturur; yarıçap/yükseklik her seferinde ayarlanır.
        private static int EnsureAgentType(string name, float radius, float height)
        {
            UnityEngine.Object projectSettings = Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings");
            SerializedObject so = new SerializedObject(projectSettings);
            SerializedProperty settings = so.FindProperty("m_Settings");
            SerializedProperty names = so.FindProperty("m_SettingNames");

            int index = -1;
            for (int i = 0; i < names.arraySize; i++)
                if (names.GetArrayElementAtIndex(i).stringValue == name) { index = i; break; }

            if (index < 0)
            {
                UnityEngine.AI.NavMesh.CreateSettings();
                so.Update();
                index = settings.arraySize - 1;
                if (names.arraySize < settings.arraySize) names.arraySize = settings.arraySize;
                names.GetArrayElementAtIndex(index).stringValue = name;
            }

            SerializedProperty entry = settings.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("agentRadius").floatValue = radius;
            entry.FindPropertyRelative("agentHeight").floatValue = height;
            so.ApplyModifiedPropertiesWithoutUndo();
            return entry.FindPropertyRelative("agentTypeID").intValue;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }

        internal static Material GetPaletteV2Material()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PaletteV2MaterialPath);
            if (material != null) return material;
            Material city = GetPaletteMaterial();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteV2Path);
            if (city == null || texture == null) return null;
            material = new Material(city) { name = "Mat_CityPaletteV2" }; // aynı shader/ayarlar, sadece doku
            material.SetTexture("_BaseMap", texture);
            AssetDatabase.CreateAsset(material, PaletteV2MaterialPath);
            return material;
        }

        private static Material GetPaletteMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
            if (shader == null || texture == null) return null;

            material = new Material(shader);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, PaletteMaterialPath);
            return material;
        }

        internal static MeshFilter LoadSourceMesh(string mesh)
        {
            string modelPath = IsKit2(mesh) ? $"{Kit2Root}/{mesh}.fbx" : $"{ArtRoot}/{mesh}.fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            MeshFilter source = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            if (source == null)
                Debug.LogError($"[CityKitPipeline] {modelPath} yüklenemedi veya mesh içermiyor.");
            return source;
        }

        // 2026-11-03 (Serra: "ağaçla bina arasında görünmez blok var"): çarpışma kutusu modelin en dış sınırıydı —
        // ağaçta yapraklar (1.78×1.25, gövde 0.24), durakta çatı, lambada kol yerde görünmez duvar oluyordu.
        // Blob yerde yuvarlandığı için XZ ayak izi sadece zemine yakın geometriden (gövde, duvar, direk) alınır;
        // yükseklik tam kalır (yeme tetikleyicisi ve mermi isabeti için). Kök orijinde olmalı (prefab-yerel uzay).
        internal const float GroundSliceHeight = 0.6f;
        private const float MinFootprint = 0.2f;

        internal static Bounds ComputeColliderBounds(MeshRenderer renderer)
        {
            Bounds full = renderer.bounds;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null) return full;

            Matrix4x4 toRoot = renderer.transform.localToWorldMatrix;
            float maxY = full.min.y + GroundSliceHeight;
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector3 v in mesh.vertices)
            {
                Vector3 p = toRoot.MultiplyPoint3x4(v);
                if (p.y > maxY) continue;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }
            if (minX > maxX) return full;

            Vector3 center = new Vector3((minX + maxX) * 0.5f, full.center.y, (minZ + maxZ) * 0.5f);
            Vector3 size = new Vector3(Mathf.Max(maxX - minX, MinFootprint), full.size.y, Mathf.Max(maxZ - minZ, MinFootprint));
            return new Bounds(center, size);
        }

        // 2026-11-03: NavMesh render mesh'ten bake ediliyor; binanın altından devam eden zemin, duvarlarla
        // çatı arasında kaldığı için yürünebilir sayılıyordu (bina içi ~908 birim²), çatılar da (~1616 birim²).
        // Binaya "yürünemez" etiketi yetmez (içerideki zemin binanın değil) — taban izini kaplayan bir
        // NotWalkable hacim içerideki ve üstteki her şeyi siler. Ayak izi yer seviyesinden (tente altı yürünür).
        private const float NavBlockerMargin = 0.5f; // zemin (y=0) ve çatının hacmin içinde kalması için

        // humanoidOnly: 2026-11-04 — sokak eşyaları (P_) sadece normal ajanın NavMesh'inde delik açar; elit polis
        // etrafından dolanır, tank (kendi NavMesh'i) üstünden geçip ezmeye devam eder (TankCrusher). Binalar herkesi engeller.
        internal static void ApplyNavBlocker(GameObject root, Bounds footprint, bool humanoidOnly = false)
        {
            NavMeshModifierVolume volume = root.GetComponent<NavMeshModifierVolume>();
            if (volume == null) volume = root.AddComponent<NavMeshModifierVolume>();
            volume.center = footprint.center;
            volume.size = new Vector3(footprint.size.x, footprint.size.y + NavBlockerMargin * 2f, footprint.size.z);
            volume.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");

            SerializedObject so = new SerializedObject(volume);
            SerializedProperty agents = so.FindProperty("m_AffectedAgents");
            agents.arraySize = 1;
            agents.GetArrayElementAtIndex(0).intValue = humanoidOnly ? 0 : -1; // -1 = tüm ajan tipleri
            so.ApplyModifiedPropertiesWithoutUndo();

            // Hacim tankı etkilemese de eşyanın render mesh'i tank NavMesh'inde hâlâ delik açıyordu (v2'de park
            // arabaları/çalılar sokakları tank için kapattı). Tank bake'inde eşya geometrisi hiç sayılmaz.
            NavMeshModifier ignore = root.GetComponent<NavMeshModifier>();
            // Ajan tipi ID'leri negatif olabilir (Tank = -1372625422): bulunamadı ayrı bayrakla.
            if (!humanoidOnly || !TryFindAgentTypeID(TankAgentName, out int tankID))
            {
                if (ignore != null) UnityEngine.Object.DestroyImmediate(ignore, true);
                return;
            }
            if (ignore == null) ignore = root.AddComponent<NavMeshModifier>();
            ignore.ignoreFromBuild = true;
            ignore.applyToChildren = true;
            SerializedObject mso = new SerializedObject(ignore);
            SerializedProperty mAgents = mso.FindProperty("m_AffectedAgents");
            mAgents.arraySize = 1;
            mAgents.GetArrayElementAtIndex(0).intValue = tankID;
            mso.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static bool TryFindAgentTypeID(string name, out int id)
        {
            for (int i = 0; i < UnityEngine.AI.NavMesh.GetSettingsCount(); i++)
            {
                id = UnityEngine.AI.NavMesh.GetSettingsByIndex(i).agentTypeID;
                if (UnityEngine.AI.NavMesh.GetSettingsNameFromID(id) == name) return true;
            }
            id = 0;
            return false;
        }

        // Model child'ının dönüşü/ölçeği: FBX kökü + yön düzeltmesi + yüksek binalarda basıklık.
        internal static void ApplyModelTransform(Transform model, MeshFilter source, string mesh)
        {
            // Kit2 (B_/P_/L_ hepsi): 180° ile Blender (x, y) = Unity (x, z) olur; city_v2.json yerleşimi buna göre.
            bool facesBackward = mesh.StartsWith("H_") || mesh.StartsWith("V_") || mesh.StartsWith("B_") || IsKit2(mesh);
            model.localRotation = facesBackward ? FacingFix * source.transform.rotation : source.transform.rotation;

            Vector3 scale = source.transform.lossyScale;
            float height = source.sharedMesh.bounds.size.y * scale.y;
            if (mesh.StartsWith("B_") && !IsKit2(mesh)) // v2 binaları oyun boyunda modellendi, ezilmez
                scale.y *= TallBuildingYScale(height);
            if (mesh == "H_Police")
                scale *= PoliceModelScale;
            else if (mesh == "H_Elite")
                scale *= EliteModelScale;
            model.localScale = scale;
        }

        // FBX'in kök dönüşü/ölçeği korunarak "Model" child'ı olarak eklenir.
        private static MeshRenderer AddModel(GameObject root, string mesh, Material material)
        {
            MeshFilter source = LoadSourceMesh(mesh);
            if (source == null) return null;

            GameObject child = new GameObject("Model");
            child.transform.SetParent(root.transform, false);
            ApplyModelTransform(child.transform, source, mesh);
            child.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        // Geçici kök sahnede oluşur; hata olursa sahnede kalıp kaydedilmesin (2026-11-04: GetConsumableData patlayınca
        // haritanın ortasında çarpışmasız bir B_ApartmentRed kalmıştı).
        private static GameObject BuildPrefab(CityItem item, Material material)
        {
            GameObject root = new GameObject(item.mesh);
            try
            {
                return BuildPrefabContents(root, item, material);
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildPrefabContents(GameObject root, CityItem item, Material material)
        {
            MeshRenderer renderer = AddModel(root, item.mesh, material);
            if (renderer == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return null;
            }

            Bounds bounds = renderer.bounds;
            Vector3 expected = new Vector3(item.sx, item.sy, item.sz);
            if (item.mesh.StartsWith("B_"))
                expected.y *= TallBuildingYScale(item.sy);
            if (!IsKit2(item.mesh) && (bounds.size - expected).magnitude > expected.magnitude * 0.1f)
                Debug.LogWarning($"[CityKitPipeline] {item.mesh}: boyut {bounds.size}, beklenen {expected}. Eksen/ölçek hatalı olabilir.");

            const StaticEditorFlags staticFlags = StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(root, staticFlags);
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, staticFlags);

            string prefabPath = $"{PrefabRoot}/{item.mesh}.prefab";
            bool isGround = item.mesh.StartsWith("L_");
            ConsumableData data = isGround ? null : GetConsumableData(item.mesh, root.transform);
            if (!isGround)
            {
                Bounds colliderBounds = ComputeColliderBounds(renderer);
                BoxCollider body = root.AddComponent<BoxCollider>();
                body.center = colliderBounds.center;
                body.size = colliderBounds.size;

                CityConsumable consumable = root.AddComponent<CityConsumable>();
                root.layer = TierLayers.For(data.RequiredTier);

                SerializedObject so = new SerializedObject(consumable);
                so.FindProperty("_data").objectReferenceValue = data;
                if (item.mesh.StartsWith("B_"))
                {
                    so.FindProperty("_obstacle").objectReferenceValue = AddObstacle(root, colliderBounds);
                    ApplyNavBlocker(root, colliderBounds);
                }
                else if (item.mesh.StartsWith("P_"))
                {
                    ApplyNavBlocker(root, colliderBounds, humanoidOnly: true);
                    ApplyPropObstacle(root, colliderBounds);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                if (item.mesh.StartsWith("P_")) ApplyEatZone(root, renderer, colliderBounds);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            if (data != null)
            {
                SerializedObject so = new SerializedObject(data);
                so.FindProperty("_prefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return prefab;
        }

        // 2026-11-04 (Serra: "yayalar bir şeylerin içinden geçiyor"): sokak eşyalarına (P_) yürüyüş engeli.
        // Ayrı "PropObstacle" katmanı (19): sadece sürü polisi ve yayaların yürüyüş sorguları görür — mermi,
        // silah görüşü, kamera saydamlığı Environment'a baktığı için etkilenmez. Katman hiçbir şeyle çarpışmaz
        // (fizik maliyeti yok, sadece sorgu). Eşya yenince child'ıyla birlikte kapanır, engel de kalkar.
        internal const string PropObstacleLayerName = "PropObstacle";
        internal const int PropObstacleLayer = 19;
        private const float PropObstacleMaxHeight = 2f; // sorgular gövde boyunda; kanopiyi saymaya gerek yok

        // Ayak izi görünür boydan çok küçükse (pota: 0.2 direk, 0.9 pano) görünür boy kadar tetikleyici yeme alanı.
        // Yiyebilecek boydaysan panoya/taca değince yenir; küçükken tetikleyici yok sayılır (görünmez duvar yok).
        private const float EatZoneAreaRatio = 1.5f;

        internal static void ApplyEatZone(GameObject root, MeshRenderer renderer, Bounds footprint)
        {
            for (Transform t = root.transform.Find(CityConsumable.EatZoneName); t != null; t = root.transform.Find(CityConsumable.EatZoneName))
                UnityEngine.Object.DestroyImmediate(t.gameObject);

            Bounds full = renderer.bounds;
            Collider zone = null;
            if (full.size.x * full.size.z > footprint.size.x * footprint.size.z * EatZoneAreaRatio)
            {
                GameObject go = new GameObject(CityConsumable.EatZoneName) { layer = root.layer };
                go.transform.SetParent(root.transform, false);
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = full.center;
                box.size = full.size;
                zone = box;
            }
            CityConsumable consumable = root.GetComponent<CityConsumable>();
            if (consumable != null)
            {
                SerializedObject so = new SerializedObject(consumable);
                so.FindProperty("_eatZone").objectReferenceValue = zone;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Tools → Blob.io → Yeme Alanlarını Kur: sadece P_ prefab'larına yeme alanı (Görsel Düzeltmeler de çağırır).
        [MenuItem("Tools/Blob.io/Yeme Alanlarını Kur")]
        private static void ApplyEatZonesMenu()
        {
            int added = 0, total = 0;
            foreach (string guid in AssetDatabase.FindAssets("P_ t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!System.IO.Path.GetFileName(path).StartsWith("P_")) continue;
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Transform model = contents.transform.Find("Model");
                    MeshRenderer renderer = model != null ? model.GetComponent<MeshRenderer>() : null;
                    if (renderer == null || contents.GetComponent<CityConsumable>() == null) continue;
                    total++;
                    ApplyEatZone(contents, renderer, ComputeColliderBounds(renderer));
                    if (contents.transform.Find(CityConsumable.EatZoneName) != null) added++;
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[CityKitPipeline] Yeme alanı: {added}/{total} sokak eşyasına eklendi.");
        }

        internal static void ApplyPropObstacle(GameObject root, Bounds footprint)
        {
            Transform existing = root.transform.Find("PropObstacle");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject obstacle = new GameObject("PropObstacle") { layer = PropObstacleLayer };
            obstacle.transform.SetParent(root.transform, false);
            BoxCollider box = obstacle.AddComponent<BoxCollider>();
            float height = Mathf.Min(footprint.size.y, PropObstacleMaxHeight);
            box.center = new Vector3(footprint.center.x, footprint.min.y + height * 0.5f, footprint.center.z);
            box.size = new Vector3(footprint.size.x, height, footprint.size.z);
        }

        // Katmanı (TagManager) adlandırır ve çarpışma matrisinde her şeyle çarpışmasını kapatır. Tekrar çalıştırmak güvenli.
        internal static void EnsurePropObstacleLayer()
        {
            SerializedObject tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tags.FindProperty("layers");
            SerializedProperty slot = layers.GetArrayElementAtIndex(PropObstacleLayer);
            if (slot.stringValue != PropObstacleLayerName)
            {
                if (!string.IsNullOrEmpty(slot.stringValue))
                    throw new InvalidOperationException($"Katman {PropObstacleLayer} dolu: '{slot.stringValue}'");
                slot.stringValue = PropObstacleLayerName;
                tags.ApplyModifiedPropertiesWithoutUndo();
            }

            for (int i = 0; i < 32; i++)
                Physics.IgnoreLayerCollision(PropObstacleLayer, i, true);
            SerializedObject dynamics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0]);
            dynamics.Update(); // IgnoreLayerCollision'ın yazdığı matrisi diske kaydetmek için
            EditorUtility.SetDirty(dynamics.targetObject);
            AssetDatabase.SaveAssets();
        }

        private static BoxCollider AddObstacle(GameObject root, Bounds bounds)
        {
            GameObject obstacle = new GameObject("EnvironmentObstacle") { layer = LayerMask.NameToLayer("Environment") };
            obstacle.transform.SetParent(root.transform, false);
            BoxCollider box = obstacle.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            return box;
        }

        // Tier ve kütle boydan gelir (TierRules); elle tablo yok, yeni asset kurala kendiliğinden uyar.
        private static ConsumableData GetConsumableData(string mesh, Transform measured)
        {
            string path = $"{DataRoot}/City_{mesh}.asset";
            ConsumableData data = AssetDatabase.LoadAssetAtPath<ConsumableData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ConsumableData>();
                AssetDatabase.CreateAsset(data, path);
            }

            BlobTier tier = TierRules.TryMeasure(measured, out Vector3 size) ? TierRules.TierForSize(TierRules.SizeOf(size)) : BlobTier.Medium;
            int mass = (int)TierRules.MassFor(tier);
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = mesh;
            so.FindProperty("_scoreValue").intValue = mass;
            so.FindProperty("_massValue").floatValue = mass;
            so.FindProperty("_requiredTier").intValue = (int)tier;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (EatEffectSetup.TryCrumbColors(measured, out Color crumbA, out Color crumbB))
                EatEffectSetup.SetColors(so, crumbA, crumbB);
            return data;
        }

        // Mevcut Pedestrian prefab/data kopyalanır: controller ayarları korunur, kapsül yerine model gelir.
        private static PedestrianData BuildPedestrian(string mesh, string suffix, string displayName, Material material)
        {
            string prefabPath = $"{PrefabRoot}/Pedestrian_{suffix}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                AssetDatabase.CopyAsset(PedestrianPrefabPath, prefabPath);

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            Transform oldModel = contents.transform.Find("Model");
            if (oldModel != null) UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

            MeshRenderer capsuleRenderer = contents.GetComponent<MeshRenderer>();
            if (capsuleRenderer != null) UnityEngine.Object.DestroyImmediate(capsuleRenderer);
            MeshFilter capsuleFilter = contents.GetComponent<MeshFilter>();
            if (capsuleFilter != null) UnityEngine.Object.DestroyImmediate(capsuleFilter);

            AddModel(contents, mesh, material);

            CapsuleCollider capsule = contents.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.radius = 0.3f;
                capsule.height = 1.15f;
                capsule.center = new Vector3(0f, 0.575f, 0f);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            string dataPath = $"Assets/_Project/ScriptableObjects/Pedestrians/PedestrianData_{suffix}.asset";
            if (AssetDatabase.LoadAssetAtPath<PedestrianData>(dataPath) == null)
                AssetDatabase.CopyAsset(PedestrianDataPath, dataPath);

            PedestrianData data = AssetDatabase.LoadAssetAtPath<PedestrianData>(dataPath);
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.FindProperty("_spawnYOffset").floatValue = PedestrianY;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void AssignEnemyMapExtent(float mapHalfExtent)
        {
            BlobSurvivor.Entities.Enemies.EnemySpawner spawner =
                UnityEngine.Object.FindAnyObjectByType<BlobSurvivor.Entities.Enemies.EnemySpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[CityKitPipeline] EnemySpawner bulunamadı, harita sınırı bağlanmadı.");
                return;
            }

            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("_mapHalfExtent").floatValue = mapHalfExtent;
            so.ApplyModifiedProperties();
        }

        private static void AssignPedestrians(PedestrianData man, PedestrianData woman, float mapHalfExtent)
        {
            PedestrianSpawner spawner = UnityEngine.Object.FindAnyObjectByType<PedestrianSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[CityKitPipeline] PedestrianSpawner bulunamadı, yeni yayalar bağlanmadı.");
                return;
            }

            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty pool = so.FindProperty("_pedestrianPool");
            pool.arraySize = 2;
            pool.GetArrayElementAtIndex(0).objectReferenceValue = man;
            pool.GetArrayElementAtIndex(1).objectReferenceValue = woman;
            so.FindProperty("_mapHalfExtent").floatValue = mapHalfExtent;
            so.ApplyModifiedProperties();
        }
    }
}
