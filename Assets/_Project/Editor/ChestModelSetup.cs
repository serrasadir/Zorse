using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Sandık modeli (Blender: zorse-blender/build_chest.py → Art/City/C_Chest.fbx, şehir paleti).
    // Chest.prefab'daki küpü kaldırır, "Model" child'ı ekler. Kök y=0.35'te döner (ChestPickup), pivot model ortası.
    // Tetik kutusunun dünya boyutu eskisiyle aynı kalır (0.6 ölçek × 1.35). Tekrar çalıştırmak güvenli.
    public static class ChestModelSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Chest.prefab";
        private const string MaterialPath = CityKitPipeline.ArtRoot + "/Mat_CityPalette.mat";
        private const float TriggerSize = 0.81f;

        [MenuItem("Tools/Blob.io/Sandık Modelini Uygula")]
        private static void Run()
        {
            AssetDatabase.Refresh();
            Material palette = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            MeshFilter source = CityKitPipeline.LoadSourceMesh("C_Chest");
            if (palette == null || source == null)
            {
                Debug.LogError("[ChestModelSetup] Mat_CityPalette ya da C_Chest.fbx yok.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            Transform oldModel = contents.transform.Find("Model");
            if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);
            MeshRenderer cubeRenderer = contents.GetComponent<MeshRenderer>();
            if (cubeRenderer != null) Object.DestroyImmediate(cubeRenderer);
            MeshFilter cubeFilter = contents.GetComponent<MeshFilter>();
            if (cubeFilter != null) Object.DestroyImmediate(cubeFilter);

            contents.transform.localScale = Vector3.one;
            GameObject child = new GameObject("Model");
            child.transform.SetParent(contents.transform, false);
            CityKitPipeline.ApplyModelTransform(child.transform, source, "C_Chest");
            child.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = palette;

            BoxCollider box = contents.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = Vector3.zero;
                box.size = Vector3.one * TriggerSize;
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();
            Debug.Log("[ChestModelSetup] Sandık modeli uygulandı.");
        }
    }
}
