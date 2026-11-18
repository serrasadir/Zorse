using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // MenuRequest "prefabshot <prefab yolu> [poz 0..1]": prefab'ı boş önizleme sahnesinde, oyun kamerasına yakın
    // açıyla (önden-çapraz ve yukarıdan) Temp/prefab_front.png + Temp/prefab_top.png olarak çeker. Poz: ShieldLower
    // varsa kalkanı o oranda indirir (0 = kalkık, 1 = inik). Görsel kontrol için (Unity'ye geçmeden).
    internal static class PrefabShot
    {
        internal static string Run(string args)
        {
            string[] p = args.Split(' ');
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p[0]);
            if (prefab == null) return $"YOK\t{p[0]}";
            float pose = p.Length > 1 ? float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;

            var preview = new PreviewRenderUtility();
            try
            {
                GameObject go = preview.InstantiatePrefabInScene(prefab);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                ApplyShieldPose(go, pose);

                Bounds b = new Bounds(go.transform.position, Vector3.zero);
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                    if (r is MeshRenderer || r is SkinnedMeshRenderer) b.Encapsulate(r.bounds);

                preview.lights[0].intensity = 1.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(50f, 30f, 0f);
                preview.lights[1].intensity = 0.6f;
                preview.ambientColor = new Color(0.45f, 0.45f, 0.5f);

                Save(preview, b, new Vector3(-0.8f, 1.1f, 1.3f), "Temp/prefab_front.png"); // önden-çapraz (+Z ileri)
                Save(preview, b, new Vector3(0f, 1f, 0.25f), "Temp/prefab_top.png");      // oyun kamerası gibi yukarıdan
                return $"OK\tprefabshot {p[0]} poz {pose:0.##}";
            }
            finally
            {
                preview.Cleanup();
            }
        }

        private static void ApplyShieldPose(GameObject go, float pose)
        {
            foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb == null || mb.GetType().Name != "ShieldLower") continue;
                var so = new SerializedObject(mb);
                var shield = so.FindProperty("_shield").objectReferenceValue as Transform;
                Quaternion lowered = so.FindProperty("_lowered").quaternionValue;
                if (shield != null) shield.localRotation = Quaternion.Slerp(Quaternion.identity, lowered, pose);
            }
        }

        private static void Save(PreviewRenderUtility preview, Bounds b, Vector3 dir, string path)
        {
            const int size = 512;
            Camera cam = preview.camera;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 1.3f;
            cam.transform.position = b.center + dir.normalized * 10f;
            cam.transform.LookAt(b.center);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.75f, 0.78f, 0.82f);

            preview.BeginStaticPreview(new Rect(0, 0, size, size));
            preview.Render(true);
            Texture2D tex = preview.EndStaticPreview();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
