using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Karakter blob görünümleri (Blender: zorse-blender/build_blobs.py → Art/City/Characters/CH_*.fbx + CharPalette.png).
    // Görünüm prefab'ını kurar ve CharacterData._blobVisualPrefab'a bağlar. Runtime'da BlobVisual takar.
    // Tekrar çalıştırmak güvenli. Sahne değişmez (sadece asset), Cmd+S gerekmez.
    public static class CharacterVisualSetup
    {
        private const string ArtFolder = CityKitPipeline.ArtRoot + "/Characters";
        private const string PalettePath = ArtFolder + "/CharPalette.png";
        private const string MaterialPath = ArtFolder + "/Mat_CharPalette.mat";
        private const string CityMaterialPath = CityKitPipeline.ArtRoot + "/Mat_CityPalette.mat";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Blob";
        private const string CharacterFolder = "Assets/_Project/Data/Characters";

        // mesh öneki → CH_<Kar>_T1..T5 (tier başına ifade). T1 varsayılan, BlobTierMeshes tier'a göre değiştirir.
        private static readonly (string mesh, string character)[] Visuals =
        {
            ("CH_Topik", "Char_Topik"),
            ("CH_Mermo", "Char_Mermo"),
            ("CH_Miknato", "Char_Miknato"),
        };
        private const int TierCount = 5;

        [MenuItem("Tools/Blob.io/Karakter Görünümlerini Uygula")]
        private static void Run()
        {
            AssetDatabase.Refresh();
            var log = new System.Text.StringBuilder();
            Material material = GetMaterial();
            if (material == null)
            {
                Debug.LogError($"[CharacterVisualSetup] {PalettePath} ya da {CityMaterialPath} yok.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Blob");

            foreach (var (mesh, character) in Visuals)
            {
                var tierMeshes = new Mesh[TierCount];
                var eatMeshes = new Mesh[TierCount];
                MeshFilter source = null;
                for (int t = 0; t < TierCount; t++)
                {
                    GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtFolder}/{mesh}_T{t + 1}.fbx");
                    MeshFilter filter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
                    GameObject eatModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtFolder}/{mesh}_T{t + 1}_Eat.fbx");
                    MeshFilter eatFilter = eatModel != null ? eatModel.GetComponentInChildren<MeshFilter>(true) : null;
                    if (eatFilter != null) eatMeshes[t] = eatFilter.sharedMesh;
                    if (filter == null) continue;
                    tierMeshes[t] = filter.sharedMesh;
                    if (source == null) source = filter;
                }
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>($"{CharacterFolder}/{character}.asset");
                if (source == null || data == null)
                {
                    log.AppendLine($"ATLANDI {mesh} → {character}: fbx ya da karakter verisi yok");
                    continue;
                }

                GameObject root = new GameObject("BlobVisual_" + mesh.Substring(3));
                GameObject child = new GameObject("Model");
                child.transform.SetParent(root.transform, false);
                child.transform.localRotation = source.transform.rotation;
                child.transform.localScale = source.transform.lossyScale;
                // Yüz +Z'de olmalı (BlobVisual +Z'yi gidiş yönüne çevirir). Blender çıktısı ön yüzü ters
                // çevirebiliyor (bkz. CityKitPipeline.ApplyModelTransform); yüz çıkıntısı hangi taraftaysa ona göre düzelt.
                if (FaceIsBackward(source.sharedMesh, child.transform))
                {
                    child.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * child.transform.localRotation;
                    log.AppendLine($"  {mesh}: yüz -Z'deydi, 180° çevrildi");
                }
                MeshFilter childFilter = child.AddComponent<MeshFilter>();
                childFilter.sharedMesh = source.sharedMesh;
                child.AddComponent<MeshRenderer>().sharedMaterial = material;
                BlobTierMeshes tiers = root.AddComponent<BlobTierMeshes>();
                SerializedObject tiersSo = new SerializedObject(tiers);
                tiersSo.FindProperty("_target").objectReferenceValue = childFilter;
                SerializedProperty list = tiersSo.FindProperty("_meshesByTier");
                list.arraySize = TierCount;
                for (int t = 0; t < TierCount; t++)
                    list.GetArrayElementAtIndex(t).objectReferenceValue = tierMeshes[t] != null ? tierMeshes[t] : source.sharedMesh;
                SerializedProperty eatList = tiersSo.FindProperty("_eatMeshesByTier");
                eatList.arraySize = TierCount;
                for (int t = 0; t < TierCount; t++)
                    eatList.GetArrayElementAtIndex(t).objectReferenceValue = eatMeshes[t] != null ? eatMeshes[t] : tierMeshes[t];
                tiersSo.ApplyModifiedPropertiesWithoutUndo();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{root.name}.prefab");
                Object.DestroyImmediate(root);

                SerializedObject so = new SerializedObject(data);
                so.FindProperty("_blobVisualPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                log.AppendLine($"OK {character}: {prefab.name}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterVisualSetup]\n" + log);
        }

        // Küre simetrik; yüz (göz/ağız) dışarı taşan tek kısım. Ön-arka uç noktaları karşılaştır.
        private static bool FaceIsBackward(Mesh mesh, Transform model)
        {
            float maxZ = float.MinValue, minZ = float.MaxValue;
            foreach (Vector3 v in mesh.vertices)
            {
                float z = (model.localRotation * Vector3.Scale(v, model.localScale)).z;
                if (z > maxZ) maxZ = z;
                if (z < minZ) minZ = z;
            }
            return -minZ > maxZ;
        }

        private static Material GetMaterial()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
            Material city = AssetDatabase.LoadAssetAtPath<Material>(CityMaterialPath);
            if (texture == null || city == null) return null;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(city);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
