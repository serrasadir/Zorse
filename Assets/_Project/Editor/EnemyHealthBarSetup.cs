using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.EditorTools
{
    // Polis ve elit prefab'ına kafa üstü can barı (EnemyHealthBar) kurar: koyu zemin + kırmızı dolgu, iki quad.
    // Materyaller prefab'da referanslı olduğu için build'de ayıklanmaz. Tekrar çalıştırmak güvenli.
    internal static class EnemyHealthBarSetup
    {
        private static readonly string[] Prefabs =
        {
            "Assets/_Project/Prefabs/Enemies/Enemy_Police.prefab",
            "Assets/_Project/Prefabs/Enemies/Enemy_ElitePolice.prefab",
        };

        private const string BgMatPath = "Assets/_Project/Materials/Mat_EnemyHealthBar_Bg.mat";
        private const string FillMatPath = "Assets/_Project/Materials/Mat_EnemyHealthBar_Fill.mat";
        private const string BarName = "HealthBar";

        private const float Width = 1.0f;
        private const float Height = 0.12f;
        private const float Border = 0.03f;
        private const float AboveHead = 0.35f; // gövde kapsülünün tepesinden yukarı

        [MenuItem("Tools/Blob.io/Polis Can Barlarını Kur")]
        private static void Setup()
        {
            Material bg = EnsureMaterial(BgMatPath, new Color(0.08f, 0.08f, 0.1f));
            Material fill = EnsureMaterial(FillMatPath, new Color(0.9f, 0.18f, 0.16f));
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            int done = 0;
            foreach (string path in Prefabs)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                Transform old = root.transform.Find(BarName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                float top = 1f;
                CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
                if (capsule != null) top = capsule.center.y + capsule.height * 0.5f;

                var bar = new GameObject(BarName);
                bar.transform.SetParent(root.transform, false);
                bar.transform.localPosition = new Vector3(0f, top + AboveHead, 0f);

                CreateQuad("Bg", bar.transform, quad, bg, Vector3.zero,
                    new Vector3(Width + Border * 2f, Height + Border * 2f, 1f));

                var pivot = new GameObject("FillPivot").transform;
                pivot.SetParent(bar.transform, false);
                pivot.localPosition = new Vector3(-Width * 0.5f, 0f, -0.01f); // quad'ın ön yüzü -Z: kameraya doğru
                CreateQuad("Fill", pivot, quad, fill, new Vector3(Width * 0.5f, 0f, 0f), new Vector3(Width, Height, 1f));

                var component = bar.AddComponent<EnemyHealthBar>();
                var so = new SerializedObject(component);
                so.FindProperty("_fillPivot").objectReferenceValue = pivot;
                so.ApplyModifiedPropertiesWithoutUndo();
                bar.SetActive(false); // vurulunca açılır

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                done++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyHealthBarSetup] Can barı kuruldu: {done} prefab.");
        }

        private static void CreateQuad(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
