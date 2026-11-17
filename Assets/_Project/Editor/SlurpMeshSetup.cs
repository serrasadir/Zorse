using BlobSurvivor.Entities;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Şehir eşyalarının höplemesi (SlurpFx) için her CityConsumable prefab'ına modelinin orijinal mesh'ini yazar
    // (oyunda static batching mesh'leri birleştirdiği için runtime'da okunamaz). Şehir kurulumu da yazar;
    // bu menü mevcut prefab'ları günceller. Tekrar çalıştırmak güvenli.
    internal static class SlurpMeshSetup
    {
        private const string CityPrefabs = "Assets/_Project/Prefabs/City";

        [MenuItem("Tools/Blob.io/Höpleme Mesh'lerini Kur")]
        private static void Setup()
        {
            int updated = 0, skipped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { CityPrefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                CityConsumable consumable = prefab != null ? prefab.GetComponent<CityConsumable>() : null;
                if (consumable == null) continue;

                Mesh mesh = LargestMesh(prefab);
                if (mesh == null) { skipped++; continue; }
                var so = new SerializedObject(consumable);
                SerializedProperty prop = so.FindProperty("_slurpMesh");
                if (prop.objectReferenceValue == mesh) continue;
                prop.objectReferenceValue = mesh;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
                updated++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[SlurpMeshSetup] {updated} prefab güncellendi, {skipped} mesh'siz atlandı.");
        }

        // SlurpFx'in runtime'da seçtiği renderer'la aynı: dünya boyutu en büyük olan.
        private static Mesh LargestMesh(GameObject root)
        {
            Mesh best = null;
            float bestSize = 0f;
            foreach (MeshFilter f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || f.GetComponent<MeshRenderer>() == null) continue;
                float size = Vector3.Scale(f.sharedMesh.bounds.size, f.transform.lossyScale).sqrMagnitude;
                if (size > bestSize) { best = f.sharedMesh; bestSize = size; }
            }
            return best;
        }
    }
}
