using System;
using System.Collections.Generic;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
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

            // Palet hücreleri düz renk: filtre/mipmap/sıkıştırma hücreleri birbirine karıştırır.
            TextureImporter importer = (TextureImporter)assetImporter;
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
        internal const float TallBuildingHeight = 5f;
        internal const float TallBuildingHeightScale = 0.75f;
        // 2026-11-03 (Serra): polis yayalarla (1.16) neredeyse aynı boydaydı (1.20), kalabalıkta tehdit okunmuyordu.
        // ×1.15 ≈ 1.38: yayadan belirgin büyük, elitten (~1.58) belirgin küçük. Sadece görsel; kapsül zaten 2 birim.
        // 2026-11-03 (Serra): polis 1.15 → ×1.15 daha (1.3225), elit ×1.10. Sadece görsel; kapsül/oynanış aynı.
        internal const float PoliceModelScale = 1.3225f;
        internal const float EliteModelScale = 1.1f;

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

        // GDD §9 ölçeği: çöp → eşya → araç → yapı. Eşikler: Small 10, Medium 30, Large 60, Giant 100 mass.
        private static (BlobTier tier, int mass) Balance(string mesh)
        {
            switch (mesh)
            {
                case "P_Trash":
                case "P_Hydrant":
                    return (BlobTier.Tiny, 2);
                case "P_Bench":
                case "P_Lamp":
                case "P_TrafficLight":
                    return (BlobTier.Small, 4);
                case "P_TreeGreen":   // 2026-11-02 (Serra): ağaçlar Medium
                case "P_TreeYellow":
                case "P_BusStop":
                case "V_CarRed":
                case "V_CarBlue":
                case "V_CarGreen":
                case "V_Taxi":
                case "V_Police":
                    return (BlobTier.Medium, 10);
                case "V_VanWhite":
                case "V_Truck":
                case "V_Bus":
                case "B_ShopBlue":
                case "B_ShopRed":
                case "B_Coffee":
                case "B_Burger":
                case "B_Restaurant":
                case "B_Police":
                    return (BlobTier.Large, 25);
                default:
                    return (BlobTier.Giant, 50); // apartman, kule, ofis, otel, banka, süpermarket
            }
        }

        [MenuItem("Tools/Blob.io/Şehir Kur (Level01)")]
        private static void Build() => BuildCity(false);

        // GDD_v2 Karar 15: Level01 ortada, çevresi mahalle şablonlarıyla dolu sınırlı büyük harita.
        [MenuItem("Tools/Blob.io/Şehir Kur (Büyük)")]
        private static void BuildLarge() => BuildCity(true);

        private static void BuildCity(bool large)
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

                if (prefabs.ContainsKey(item.mesh)) continue;
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
            foreach (CityItem item in placed)
            {
                if (IsVehicle(item.mesh)) continue; // araçlar sahnede durmaz, CarSpawner havuzundan gelir

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
            Debug.Log($"[CityKitPipeline] Şehir kuruldu: {root.transform.childCount} obje, {cars.Count} araç tipi. NavMesh bake bitince sahneyi kaydet (Cmd+S).");
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
            CapsuleCollider capsule = contents.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.radius = 0.95f;        // tank 1.8 x 2.5 (namlu hariç)
                capsule.height = 2f;
                capsule.center = Vector3.zero; // -1..1: zeminden kule tepesine
            }
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();
            return true;
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

        internal static void BakeNavMesh()
        {
            NavMeshSurface surface = UnityEngine.Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                Debug.LogWarning("[CityKitPipeline] NavMeshSurface bulunamadı, bake atlandı.");
                return;
            }

            NavMeshAssetManager.instance.StartBakingSurfaces(new UnityEngine.Object[] { surface });
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
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
            string modelPath = $"{ArtRoot}/{mesh}.fbx";
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

        internal static void ApplyNavBlocker(GameObject root, Bounds footprint)
        {
            NavMeshModifierVolume volume = root.GetComponent<NavMeshModifierVolume>();
            if (volume == null) volume = root.AddComponent<NavMeshModifierVolume>();
            volume.center = footprint.center;
            volume.size = new Vector3(footprint.size.x, footprint.size.y + NavBlockerMargin * 2f, footprint.size.z);
            volume.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
        }

        // Model child'ının dönüşü/ölçeği: FBX kökü + yön düzeltmesi + yüksek binalarda basıklık.
        internal static void ApplyModelTransform(Transform model, MeshFilter source, string mesh)
        {
            bool facesBackward = mesh.StartsWith("H_") || mesh.StartsWith("V_") || mesh.StartsWith("B_");
            model.localRotation = facesBackward ? FacingFix * source.transform.rotation : source.transform.rotation;

            Vector3 scale = source.transform.lossyScale;
            float height = source.sharedMesh.bounds.size.y * scale.y;
            if (mesh.StartsWith("B_") && height > TallBuildingHeight)
                scale.y *= TallBuildingHeightScale;
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

        private static GameObject BuildPrefab(CityItem item, Material material)
        {
            GameObject root = new GameObject(item.mesh);
            MeshRenderer renderer = AddModel(root, item.mesh, material);
            if (renderer == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return null;
            }

            Bounds bounds = renderer.bounds;
            Vector3 expected = new Vector3(item.sx, item.sy, item.sz);
            if (item.mesh.StartsWith("B_") && item.sy > TallBuildingHeight)
                expected.y *= TallBuildingHeightScale;
            if ((bounds.size - expected).magnitude > expected.magnitude * 0.1f)
                Debug.LogWarning($"[CityKitPipeline] {item.mesh}: boyut {bounds.size}, beklenen {expected}. Eksen/ölçek hatalı olabilir.");

            const StaticEditorFlags staticFlags = StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(root, staticFlags);
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, staticFlags);

            string prefabPath = $"{PrefabRoot}/{item.mesh}.prefab";
            bool isGround = item.mesh.StartsWith("L_");
            ConsumableData data = isGround ? null : GetConsumableData(item.mesh);
            if (!isGround)
            {
                Bounds colliderBounds = ComputeColliderBounds(renderer);
                BoxCollider body = root.AddComponent<BoxCollider>();
                body.center = colliderBounds.center;
                body.size = colliderBounds.size;

                CityConsumable consumable = root.AddComponent<CityConsumable>();
                root.layer = 8 + (int)data.RequiredTier;

                SerializedObject so = new SerializedObject(consumable);
                so.FindProperty("_data").objectReferenceValue = data;
                if (item.mesh.StartsWith("B_"))
                {
                    so.FindProperty("_obstacle").objectReferenceValue = AddObstacle(root, colliderBounds);
                    ApplyNavBlocker(root, colliderBounds);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
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

        private static BoxCollider AddObstacle(GameObject root, Bounds bounds)
        {
            GameObject obstacle = new GameObject("EnvironmentObstacle") { layer = LayerMask.NameToLayer("Environment") };
            obstacle.transform.SetParent(root.transform, false);
            BoxCollider box = obstacle.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            return box;
        }

        private static ConsumableData GetConsumableData(string mesh)
        {
            string path = $"{DataRoot}/City_{mesh}.asset";
            ConsumableData data = AssetDatabase.LoadAssetAtPath<ConsumableData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ConsumableData>();
                AssetDatabase.CreateAsset(data, path);
            }

            (BlobTier tier, int mass) = Balance(mesh);
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = mesh;
            so.FindProperty("_scoreValue").intValue = mass;
            so.FindProperty("_massValue").floatValue = mass;
            so.FindProperty("_objectSize").floatValue = (int)tier;
            so.FindProperty("_requiredTier").intValue = (int)tier;
            so.ApplyModifiedPropertiesWithoutUndo();
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
