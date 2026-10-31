using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Şehir binası asset boru hattı. Yeni bina eklemek için Assets/_Project/Art/Buildings/<Ad>/
    // klasörüne Meshy'den tek .fbx + base color .png koy, sonra "Greybox Şehir Oluştur" menüsünü
    // çalıştır. Metallic/roughness haritaları gerekmez (Meshy'de düz geliyor).
    public class BuildingImportSettings : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!BuildingAssetPipeline.IsBuildingAsset(assetPath)) return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None; // materyali pipeline üretir
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
        }

        private void OnPreprocessTexture()
        {
            bool isRoadAtlas = assetPath.StartsWith("Assets/_Project/Art/Roads/");
            if (!isRoadAtlas && !BuildingAssetPipeline.IsBuildingAsset(assetPath)) return;

            int maxSize = isRoadAtlas ? 2048 : 1024; // yol atlası iki 1024'lük hücre taşıyor

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = maxSize;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;

            foreach (string platform in new[] { "iOS", "Android" })
            {
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
                settings.overridden = true;
                settings.maxTextureSize = maxSize;
                settings.format = TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }

    public static class BuildingAssetPipeline
    {
        private const string ArtRoot = "Assets/_Project/Art/Buildings";
        private const string PrefabRoot = "Assets/_Project/Prefabs/Buildings";

        // Şehir ızgarasındaki bina payı: BlockSpacing(12) - StreetWidth(6).
        private const float MaxFootprint = 6f;

        // Meshy modelleri ön cepheyi +Z'ye veriyor; kamera +Z'ye baktığı için (CameraController,
        // tilt 60°) 180° çevrilmezse oyuncu boş arka duvarı görür.
        private const float ModelYaw = 180f;

        public static bool IsBuildingAsset(string assetPath) => assetPath.StartsWith(ArtRoot + "/");

        // Art/Buildings altındaki her klasör için prefab üretir/günceller. Bina yoksa boş liste döner.
        public static List<GameObject> BuildPrefabs()
        {
            List<GameObject> prefabs = new List<GameObject>();
            if (!AssetDatabase.IsValidFolder(ArtRoot)) return prefabs;

            if (!AssetDatabase.IsValidFolder(PrefabRoot))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Buildings");

            int environmentLayer = LayerMask.NameToLayer("Environment");
            foreach (string folder in AssetDatabase.GetSubFolders(ArtRoot))
            {
                GameObject prefab = BuildPrefab(folder, environmentLayer);
                if (prefab != null) prefabs.Add(prefab);
            }

            AssetDatabase.SaveAssets();
            return prefabs;
        }

        private static GameObject BuildPrefab(string folder, int environmentLayer)
        {
            string buildingName = Path.GetFileName(folder);
            GameObject model = LoadFirstWithExtension<GameObject>(folder, "t:Model", ".fbx");
            Texture2D albedo = LoadFirstWithExtension<Texture2D>(folder, "t:Texture2D", ".png");
            if (model == null || albedo == null)
            {
                Debug.LogWarning($"[BuildingAssetPipeline] {folder}: .fbx veya .png bulunamadı, atlandı.");
                return null;
            }

            GameObject root = new GameObject($"Building_{buildingName}") { layer = environmentLayer };
            GameObject instance = Object.Instantiate(model, root.transform);
            instance.name = "Model";

            Renderer renderer = instance.GetComponentInChildren<Renderer>();
            renderer.sharedMaterial = GetOrCreateMaterial(folder, buildingName, albedo);

            FitToGrid(instance.transform, renderer);

            // Mesh yerine kutu: düşman/yaya SphereCast'leri her frame buna çarpıyor, MeshCollider mobilde pahalı.
            Bounds bounds = renderer.bounds;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;

            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = environmentLayer;
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.NavigationStatic);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{root.name}.prefab");
            Object.DestroyImmediate(root);

            Debug.Log($"[BuildingAssetPipeline] {prefab.name} hazır, boyut {bounds.size}.");
            return prefab;
        }

        // Modeli kameraya çevirir, ızgara payına sığdırır, tabanını y=0'a ve merkezini pivota oturtur.
        private static void FitToGrid(Transform model, Renderer renderer)
        {
            model.rotation = Quaternion.Euler(0f, ModelYaw, 0f) * model.rotation;

            Vector3 size = renderer.bounds.size;
            model.localScale *= MaxFootprint / Mathf.Max(size.x, size.z);

            Bounds bounds = renderer.bounds;
            model.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static Material GetOrCreateMaterial(string folder, string buildingName, Texture2D albedo)
        {
            string path = $"{folder}/Mat_{buildingName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", albedo);
            material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T LoadFirstWithExtension<T>(string folder, string filter, string extension) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }
    }
}
