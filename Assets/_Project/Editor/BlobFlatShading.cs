using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → Blob'u Düz Yüzlü Yap (Bahar 2026-11-27: "tam küre low-poly dünyada uyumsuz"): BlobVisual_*
    // prefab'larının bütün mesh'lerinin (model + BlobTierMeshes tier/yeme/göz kırpma) düz gölgeli kopyası
    // (her üçgen kendi köşeleri ve yüz normaliyle) → Art/City/Characters/Flat, prefab'lar kopyalara bağlanır.
    // Geri almak: Karakter Görünümlerini Uygula (orijinal FBX mesh'lerini yeniden bağlar). Tekrar çalıştırmak güvenli.
    public static class BlobFlatShading
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/Blob";
        private const string OutFolder = "Assets/_Project/Art/City/Characters/Flat";

        [MenuItem("Tools/Blob.io/Blob'u Düz Yüzlü Yap")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/City/Characters", "Flat");
            var cache = new Dictionary<Mesh, Mesh>();
            int prefabs = 0;
            foreach (string guid in AssetDatabase.FindAssets("BlobVisual_ t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                        mf.sharedMesh = Flat(mf.sharedMesh, cache);
                    foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb == null) continue;
                        var so = new SerializedObject(mb);
                        SerializedProperty it = so.GetIterator();
                        while (it.Next(true))
                            if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue is Mesh m)
                                it.objectReferenceValue = Flat(m, cache);
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabs++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[BlobFlatShading] {prefabs} görünüm, {cache.Count} mesh düz yüzlü.");
        }

        private static Mesh Flat(Mesh src, Dictionary<Mesh, Mesh> cache)
        {
            if (src == null) return null;
            if (AssetDatabase.GetAssetPath(src).StartsWith(OutFolder)) return src; // zaten düz
            if (cache.TryGetValue(src, out Mesh done)) return done;

            Vector3[] v = src.vertices;
            Vector2[] uv = src.uv;
            int[] t = src.triangles;
            var verts = new Vector3[t.Length];
            var normals = new Vector3[t.Length];
            var uvs = new Vector2[t.Length];
            var tris = new int[t.Length];
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                for (int k = 0; k < 3; k++)
                {
                    verts[i + k] = v[t[i + k]];
                    normals[i + k] = n;
                    uvs[i + k] = uv.Length == v.Length ? uv[t[i + k]] : Vector2.zero;
                    tris[i + k] = i + k;
                }
            }
            var mesh = new Mesh { name = src.name + "_Flat" };
            if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            string path = $"{OutFolder}/{mesh.name}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                mesh = existing;
            }
            else AssetDatabase.CreateAsset(mesh, path);
            cache[src] = mesh;
            return mesh;
        }
    }
}
