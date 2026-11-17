using System.IO;
using BlobSurvivor.Entities.Blob;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Dönme dolap dönüşümünün görseli (FerrisWheelForm): Resources/Transformations/FerrisWheelForm.prefab.
    // Model: Art/City/X_FerrisWheelRoll.fbx (ayaksız çark; Blender: build_ferris_roll.py), yoksa P_FerrisWheel
    // (geçici, ayaklarıyla). Aks yerel X'e çevrilir (X ekseni etrafında dönünce +Z'ye yuvarlanır), pivot çark merkezi.
    internal static class FerrisWheelFormSetup
    {
        private const string RollModel = "Assets/_Project/Art/City/X_FerrisWheelRoll.fbx";
        private const string FallbackModel = "Assets/_Project/Art/City/Kit2/P_FerrisWheel.fbx";
        private const string CityPrefab = "Assets/_Project/Prefabs/City/P_FerrisWheel.prefab";
        private const string OutFolder = "Assets/_Project/Resources/Transformations";
        private const string OutPath = OutFolder + "/FerrisWheelForm.prefab";

        [MenuItem("Tools/Blob.io/Dönme Dolap Formunu Kur")]
        private static void Setup()
        {
            string modelPath = File.Exists(RollModel) ? RollModel : FallbackModel;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            MeshFilter source = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            if (source == null) { Debug.LogError($"[FerrisWheelFormSetup] {modelPath} yüklenemedi."); return; }

            // Şehirdeki dönme dolabın materyali (aynı palet).
            var cityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CityPrefab);
            Renderer cityRenderer = cityPrefab != null ? cityPrefab.GetComponentInChildren<MeshRenderer>(true) : null;
            Material material = cityRenderer != null ? cityRenderer.sharedMaterial : CityKitPipeline.GetPaletteMaterial();

            var root = new GameObject("FerrisWheelForm");
            try
            {
                var wheel = new GameObject("Wheel");
                wheel.transform.SetParent(root.transform, false);
                wheel.transform.localRotation = source.transform.rotation;
                wheel.transform.localScale = source.transform.lossyScale;
                wheel.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var renderer = wheel.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                // Aks: yatayda ince olan eksen. Z ise 90° çevir (aks X olsun).
                Bounds b = renderer.bounds;
                if (b.size.z < b.size.x)
                {
                    wheel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * wheel.transform.localRotation;
                    b = renderer.bounds;
                }
                wheel.transform.localPosition -= b.center - root.transform.position; // pivot = çark merkezi
                b = renderer.bounds;

                root.AddComponent<FormInfo>().Diameter = Mathf.Max(b.size.y, b.size.z);

                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources")) AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/_Project/Resources", "Transformations");
                PrefabUtility.SaveAsPrefabAsset(root, OutPath);
                Debug.Log($"[FerrisWheelFormSetup] {OutPath} kuruldu ({Path.GetFileName(modelPath)}, çap {Mathf.Max(b.size.y, b.size.z):0.00}).");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
