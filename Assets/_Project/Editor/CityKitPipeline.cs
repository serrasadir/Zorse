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
    // BuildingAssetPipeline'dan ayrı çünkü burada model çevrilmez/ölçeklenmez ve tek materyal paylaşılır.
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

        // Hareketli arabalar dış çevre yolunda, sağ şeritte döner (Waypoint_0..3 sırasıyla).
        private static readonly Vector3[] CarLoop =
        {
            new Vector3(19f, 0f, -19f),
            new Vector3(19f, 0f, 19f),
            new Vector3(-19f, 0f, 19f),
            new Vector3(-19f, 0f, -19f),
        };

        [Serializable]
        private class CityItem
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
                case "P_TreeGreen":
                case "P_TreeYellow":
                    return (BlobTier.Small, 4);
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
        private static void Build()
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

            foreach (CityItem item in layout.items)
            {
                if (IsVehicle(item.mesh)) continue; // araçlar sahnede durmaz, CarSpawner havuzundan gelir

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[item.mesh], root.transform);
                instance.transform.localPosition = new Vector3(item.x, item.y, item.z);
                instance.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
            }

            DisableGreybox();
            MoveCarLoop();
            AssignPedestrians(man, woman);
            AssignCars(cars);
            RoadNetworkGenerator.Generate();
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
            AssetDatabase.SaveAssets();
            Debug.Log($"[CityKitPipeline] Polis modelleri: normal={police}, elit={elite}.");
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

        private static void BakeNavMesh()
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

        // FBX'in kök dönüşü/ölçeği korunarak "Model" child'ı olarak eklenir.
        private static MeshRenderer AddModel(GameObject root, string mesh, Material material)
        {
            string modelPath = $"{ArtRoot}/{mesh}.fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            MeshFilter source = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            if (source == null)
            {
                Debug.LogError($"[CityKitPipeline] {modelPath} yüklenemedi veya mesh içermiyor.");
                return null;
            }

            GameObject child = new GameObject("Model");
            child.transform.SetParent(root.transform, false);
            child.transform.localRotation = source.transform.rotation;
            child.transform.localScale = source.transform.lossyScale;
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
                BoxCollider body = root.AddComponent<BoxCollider>();
                body.center = bounds.center;
                body.size = bounds.size;

                CityConsumable consumable = root.AddComponent<CityConsumable>();
                root.layer = 8 + (int)data.RequiredTier;

                SerializedObject so = new SerializedObject(consumable);
                so.FindProperty("_data").objectReferenceValue = data;
                if (item.mesh.StartsWith("B_"))
                    so.FindProperty("_obstacle").objectReferenceValue = AddObstacle(root, bounds);
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

        private static void AssignPedestrians(PedestrianData man, PedestrianData woman)
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
            so.FindProperty("_mapHalfExtent").floatValue = PedestrianMapHalfExtent;
            so.ApplyModifiedProperties();
        }

        private static void DisableGreybox()
        {
            GameObject greybox = GameObject.Find("GreyboxCity");
            if (greybox == null) return;
            Undo.RecordObject(greybox, "Şehir Kur");
            greybox.SetActive(false);
        }

        // Waypoint'ler ve CarSpawner bağlantısı korunur, yalnızca köşeler yeni yollara taşınır.
        private static void MoveCarLoop()
        {
            GameObject carRoad = GameObject.Find(GreyboxCityGenerator.CarRoadRootName);
            if (carRoad == null)
            {
                Debug.LogWarning("[CityKitPipeline] CarRoad_Waypoints bulunamadı, araba güzergâhı taşınmadı.");
                return;
            }

            for (int i = 0; i < CarLoop.Length; i++)
            {
                Transform waypoint = carRoad.transform.Find($"Waypoint_{i}");
                if (waypoint == null)
                {
                    Debug.LogWarning($"[CityKitPipeline] Waypoint_{i} bulunamadı.");
                    continue;
                }
                Undo.RecordObject(waypoint, "Şehir Kur");
                waypoint.position = CarLoop[i];

                Transform segment = carRoad.transform.Find($"RoadSegment_{i}");
                if (segment == null) continue;
                Undo.RecordObject(segment.gameObject, "Şehir Kur");
                segment.gameObject.SetActive(false); // eski gri şerit yeni şehrin içinden geçerdi
            }
        }
    }
}
