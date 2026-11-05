using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Sahnenin bir noktasını oyun kamerası açısıyla (60°) PNG'ye çeker; Claude görsel kontrol için kullanır.
    // Temp/snapshot.request satırı: "x z mesafe [yaw] [eat t]" → Temp/snapshot.png. Düzenleme modunda çalışır.
    // "eat t": (x, z)'ye en yakın Medium+ şehir objesi gizlenir, yeme kırıntıları t saniye ilerletilip çekilir.
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
            if (p.Length > 0 && p[0] == "ui") { CaptureUI(p.Length > 1 ? p[1] : null); return; }
            float F(int i, float d) => p.Length > i ? float.Parse(p[i], CultureInfo.InvariantCulture) : d;
            var target = new Vector3(F(0, 0f), 0f, F(1, 0f));
            if (p.Length > 4 && p[4] == "eat") CaptureEat(target, F(2, 14f), F(3, 0f), F(5, 0.15f));
            else Capture(target, F(2, 14f), F(3, 0f));
        }

        // "ui <ObjeAdı>": ekran-katmanı Canvas'ı geçici olarak kameraya bağlanır, verilen panel (pasifse) açılır,
        // 1080x1920 çekilir, her şey eski hâline döner. Panel adı yoksa o anki UI.
        private static void CaptureUI(string panelName)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            GameObject panel = null;
            if (!string.IsNullOrEmpty(panelName))
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                    if (t.name == panelName) { panel = t.gameObject; break; }
            // Bileşen tipi adı da olur (UpgradePanel, LobbyPanel...): onun _panel alanı açılır.
            if (panel == null && !string.IsNullOrEmpty(panelName))
                foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                    if (mb.GetType().Name == panelName)
                    {
                        var prop = new SerializedObject(mb).FindProperty("_panel");
                        panel = prop?.objectReferenceValue as GameObject ?? mb.gameObject;
                        break;
                    }
            bool panelWasActive = panel != null && panel.activeSelf;
            var go = new GameObject("SnapshotUICam") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(1080, 1920, 24);
            var modes = new System.Collections.Generic.List<(Canvas c, RenderMode m, Camera cam, float d)>();
            var hidden = new System.Collections.Generic.List<GameObject>();
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.42f, 0.5f);
                cam.cullingMask = 1 << 5; // UI
                cam.targetTexture = rt;
                foreach (var c in canvases)
                {
                    if (!c.isRootCanvas) continue;
                    modes.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = 1f;
                }
                if (panel != null)
                {
                    panel.SetActive(true);
                    // Panelin yolundaki her seviyede kardeşler geçici gizlenir: sadece o ekran görünsün.
                    for (Transform t = panel.transform; t.parent != null; t = t.parent)
                        foreach (Transform sib in t.parent)
                            if (sib != t && sib.gameObject.activeSelf) { sib.gameObject.SetActive(false); hidden.Add(sib.gameObject); }
                }
                Canvas.ForceUpdateCanvases();
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
                foreach (var h in hidden) h.SetActive(true);
                if (panel != null) panel.SetActive(panelWasActive);
                foreach (var (c, m, wc, d) in modes) { c.renderMode = m; c.worldCamera = wc; c.planeDistance = d; }
                RenderTexture.active = null;
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(rt);
            }
        }

        private static void CaptureEat(Vector3 target, float distance, float yaw, float t)
        {
            BlobSurvivor.Entities.CityConsumable best = null;
            float bestD = float.MaxValue;
            foreach (var c in Object.FindObjectsByType<BlobSurvivor.Entities.CityConsumable>())
            {
                if (c.RequiredTier < BlobSurvivor.Core.BlobTier.Medium) continue;
                float d = (c.transform.position - target).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            var prefab = Resources.Load<BlobSurvivor.Entities.Blob.EatCrumbs>("FX/EatCrumbs");
            if (best == null || prefab == null) { Capture(target, distance, yaw); return; }

            Renderer[] renderers = best.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            var fx = Object.Instantiate(prefab);
            fx.gameObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Vector3 s = bounds.size;
                Vector3 blob = bounds.center + new Vector3(0f, 0f, -bounds.extents.z - 2f);
                fx.Play(bounds, best.Data.CrumbColorA, best.Data.CrumbColorB, blob, Mathf.Pow(s.x * s.y * s.z, 1f / 3f));
                fx.GetComponent<ParticleSystem>().Simulate(t, true, false);
                foreach (var r in renderers) r.enabled = false;
                Capture(bounds.center, distance, yaw);
            }
            finally
            {
                foreach (var r in renderers) r.enabled = true;
                Object.DestroyImmediate(fx.gameObject);
            }
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
