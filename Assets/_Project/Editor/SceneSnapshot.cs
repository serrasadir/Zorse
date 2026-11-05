using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Sahnenin bir noktasını oyun kamerası açısıyla (60°) PNG'ye çeker; Claude görsel kontrol için kullanır.
    // Temp/snapshot.request satırı: "x z mesafe [yaw]" → Temp/snapshot.png. Düzenleme modunda çalışır.
    [InitializeOnLoad]
    internal static class SceneSnapshot
    {
        private const string RequestPath = "Temp/snapshot.request";
        private const string ResultPath = "Temp/snapshot.png";
        private static double s_nextPoll;

        static SceneSnapshot() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_nextPoll) return;
            s_nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling) return;
            string[] p = File.ReadAllText(RequestPath).Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            File.Delete(RequestPath);
            float F(int i, float d) => p.Length > i ? float.Parse(p[i], CultureInfo.InvariantCulture) : d;
            Capture(new Vector3(F(0, 0f), 0f, F(1, 0f)), F(2, 14f), F(3, 0f));
        }

        private static void Capture(Vector3 target, float distance, float yaw)
        {
            var go = new GameObject("SnapshotCam") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(1200, 900, 24);
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = 45f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.75f, 0.78f, 0.84f);
                Quaternion rot = Quaternion.Euler(60f, yaw, 0f);
                go.transform.SetPositionAndRotation(target - rot * Vector3.forward * distance, rot);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(ResultPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null;
                Object.DestroyImmediate(go); // önce kamera: targetTexture'ı tutarken RT silinirse uyarı basar
                Object.DestroyImmediate(rt);
            }
        }
    }
}
