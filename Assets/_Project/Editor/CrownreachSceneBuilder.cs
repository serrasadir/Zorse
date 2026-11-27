using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    public static class CrownreachSceneBuilder
    {
        private const string Art = "Assets/_Project/Art/Crownreach/";
        private static readonly string[] Districts = { "01_Farmland", "02_WalledTown", "03_Garrison", "04_RoyalCastle" };

        // Claude görsel kontrolü: Temp/crown_shot.request "x y z yaw pitch fov" → Temp/crown_shot.png (açık sahne).
        [MenuItem("Tools/Blob.io/Crownreach Çekim")]
        public static void Shot()
        {
            string[] p = System.IO.File.Exists("Temp/crown_shot.request")
                ? System.IO.File.ReadAllText("Temp/crown_shot.request").Trim().Split(' ') : new[] { "0", "90", "-110", "0", "40", "40" };
            float F(int i) => float.Parse(p[i], System.Globalization.CultureInfo.InvariantCulture);
            // İsteğe bağlı 7.-10. alan: prefab yolu ve konumu (x y z), çekimden sonra silinir.
            GameObject placed = null;
            if (p.Length >= 10)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p[6]);
                if (prefab != null)
                {
                    placed = (GameObject)Object.Instantiate(prefab);
                    placed.transform.SetPositionAndRotation(new Vector3(F(7), F(8), F(9)), Quaternion.Euler(0f, 160f, 0f));
                    placed.transform.localScale = Vector3.one * 1.5f;
                }
            }
            var camGo = new GameObject("ShotCam");
            var rt = new RenderTexture(1280, 800, 24);
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.transform.position = new Vector3(F(0), F(1), F(2));
                cam.transform.rotation = Quaternion.Euler(F(4), F(3), 0f);
                cam.fieldOfView = F(5);
                cam.farClipPlane = 600f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.25f, 0.35f, 0.36f);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 800, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
                System.IO.File.WriteAllBytes("Temp/crown_shot.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
                if (placed != null) Object.DestroyImmediate(placed);
            }
        }

        // Claude oyun testi: Play'e girer, run başlatır, 4 ve 14 sn sonra Game görünümünü Temp/crown_play_N.png'ye çeker,
        // blob'u Temp/crown_play.request'teki "x z" noktasına koyar (varsa), 16 sn sonra çıkar.
        [MenuItem("Tools/Blob.io/Crownreach Oyun Testi")]
        public static void PlayTest()
        {
            SessionState.SetInt("CrownPlayTest", 1);
            EditorApplication.isPlaying = true;
        }

        // Play'e girerken domain yeniden yüklenir: durum SessionState'te, döngü her yüklemede yeniden bağlanır.
        [InitializeOnLoadMethod]
        private static void HookPlayTest()
        {
            if (SessionState.GetInt("CrownPlayTest", 0) == 0) return;
            double t0 = -1;
            int step = 0;
            void Tick()
            {
                if (!EditorApplication.isPlaying) return;
                if (t0 < 0) t0 = EditorApplication.timeSinceStartup;
                double t = EditorApplication.timeSinceStartup - t0;
                if (step == 0 && t > 3)
                {
                    step = 1;
                    var gm = Object.FindAnyObjectByType<BlobSurvivor.Core.GameManager>();
                    if (gm != null) gm.StartGame();
                    if (System.IO.File.Exists("Temp/crown_play.request"))
                    {
                        string[] p = System.IO.File.ReadAllText("Temp/crown_play.request").Trim().Split(' ');
                        var blob = GameObject.FindWithTag("Blob");
                        if (blob != null && p.Length >= 2)
                        {
                            var rb = blob.GetComponent<Rigidbody>();
                            Vector3 pos = new Vector3(float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), blob.transform.position.y,
                                float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));
                            if (rb != null) rb.position = pos;
                            blob.transform.position = pos;
                        }
                    }
                }
                else if (step == 1 && t > 6) { step = 2; ScreenCapture.CaptureScreenshot("Temp/crown_play_1.png"); }
                else if (step == 2 && t > 14) { step = 3; ScreenCapture.CaptureScreenshot("Temp/crown_play_2.png"); }
                else if (step == 3 && t > 16)
                {
                    step = 4;
                    SessionState.SetInt("CrownPlayTest", 0);
                    EditorApplication.update -= Tick;
                    EditorApplication.isPlaying = false;
                }
            }
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Blob.io/Crownreach Veri Kontrolü")]
        public static void DataCheck()
        {
            var sb = new StringBuilder();
            int nulls = 0, total = 0;
            foreach (var c in Object.FindObjectsByType<BlobSurvivor.Entities.CityConsumable>(FindObjectsInactive.Include))
            {
                total++;
                if (c.Data == null) nulls++;
                if (c.name.StartsWith("stall") || c.name.StartsWith("cart") || total % 80 == 0)
                    sb.AppendLine($"{c.name} data={(c.Data != null ? c.Data.name : "NULL")} tier={c.RequiredTier} layer={LayerMask.LayerToName(c.gameObject.layer)} cols={c.GetComponents<Collider>().Length}");
            }
            sb.Insert(0, $"toplam={total} veri yok={nulls}\n");
            System.IO.File.WriteAllText("Temp/crown_data.txt", sb.ToString());
        }

        [MenuItem("Tools/Blob.io/Karakter Mesh Raporu")]
        public static void CharacterReport()
        {
            var sb = new StringBuilder();
            foreach (string guid in AssetDatabase.FindAssets("CH_ t:Model", new[] { "Assets/_Project/Art/City/Characters" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_Blink") || path.Contains("_Eat")) continue;
                foreach (Object a in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (a is Mesh m) sb.AppendLine($"{System.IO.Path.GetFileName(path)} {m.name} v={m.vertexCount} t={m.triangles.Length / 3} sub={m.subMeshCount} b={m.bounds.size} uv={m.uv.Length} col={m.colors.Length}");
            }
            foreach (string p in new[] { "Assets/_Project/Prefabs/Blob/BlobVisual_Topik.prefab", "Assets/_Project/Prefabs/Blob/BlobVisual_Cano.prefab" })
            {
                var pr = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                foreach (var r in pr.GetComponentsInChildren<MeshRenderer>(true))
                    sb.AppendLine($"{p}: {r.name} mat={string.Join(",", System.Array.ConvertAll(r.sharedMaterials, x => x != null ? x.name + "/" + x.shader.name : "null"))} scale={r.transform.lossyScale}");
            }
            System.IO.File.WriteAllText("Temp/ch_report.txt", sb.ToString());
        }

        [MenuItem("Tools/Blob.io/Crownreach Raporu")]
        public static void Report()
        {
            var sb = new StringBuilder();
            var kinds = new Dictionary<string, (int n, Vector3 min, Vector3 max)>();
            Bounds all = default; bool any = false;
            foreach (string d in Districts)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + d + ".fbx");
                var go = (GameObject)Object.Instantiate(model);
                try
                {
                    Bounds db = default; bool dany = false;
                    var mrs = go.GetComponentsInChildren<MeshRenderer>();
                    foreach (var mr in mrs)
                    {
                        Bounds b = mr.bounds;
                        if (!dany) { db = b; dany = true; } else db.Encapsulate(b);
                        if (!any) { all = b; any = true; } else all.Encapsulate(b);
                        string kind = mr.name;
                        int us = kind.LastIndexOf('_');
                        if (us > 0) kind = kind.Substring(0, us);
                        if (kind.StartsWith("GroundAndDetails")) kind = "GroundAndDetails";
                        kinds.TryGetValue(kind, out var k);
                        if (k.n == 0) { k.min = b.size; k.max = b.size; }
                        k.min = Vector3.Min(k.min, b.size); k.max = Vector3.Max(k.max, b.size); k.n++;
                        kinds[kind] = k;
                    }
                    sb.AppendLine($"{d}: {mrs.Length} mesh, bounds {db.min} .. {db.max}, root scale {go.transform.localScale}");
                    int shown = 0;
                    foreach (var mr in mrs)
                    {
                        if (!mr.name.StartsWith("GroundAndDetails") || shown++ > 6) continue;
                        sb.AppendLine($"   {mr.name} {mr.bounds.min} .. {mr.bounds.max} verts {mr.GetComponent<MeshFilter>().sharedMesh.vertexCount}");
                    }
                }
                finally { Object.DestroyImmediate(go); }
            }
            sb.AppendLine($"ALL {all.min} .. {all.max}");
            foreach (var kv in kinds) sb.AppendLine($"{kv.Key}: n={kv.Value.n} size {kv.Value.min} .. {kv.Value.max}");
            // Zemin yükseklik haritası (2 birim ızgara): sadece GroundAndDetails mesh'leri.
            var tmp = new List<GameObject>();
            foreach (string d in Districts)
            {
                var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art + d + ".fbx"));
                tmp.Add(go);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.name.StartsWith("GroundAndDetails")) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            Physics.SyncTransforms();
            const string ramp = "~-.0123456789ABCDEFGH";
            for (float z = 58f; z >= -60f; z -= 2f)
            {
                var line = new StringBuilder($"{z,5} ");
                for (float x = -36f; x <= 36f; x += 1f)
                {
                    if (!Physics.Raycast(new Vector3(x, 50f, z), Vector3.down, out RaycastHit hit, 100f)) { line.Append(' '); continue; }
                    int i = Mathf.Clamp(Mathf.RoundToInt(hit.point.y * 2f) + 3, 0, ramp.Length - 1);
                    line.Append(ramp[i]);
                }
                sb.AppendLine(line.ToString());
            }
            foreach (var go in tmp) Object.DestroyImmediate(go);
            System.IO.File.WriteAllText("Temp/crownreach_report.txt", sb.ToString());
            Debug.Log("[Crownreach] rapor: Temp/crownreach_report.txt");
        }
    }
}
