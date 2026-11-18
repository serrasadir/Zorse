using BlobSurvivor.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlobSurvivor.EditorTools
{
    // Kit parçalarını gerçek Canvas'ta (1080x1920, oyunla aynı ölçek) çizip Temp/ui_preview.png'ye yazar.
    // Sahneye dokunmaz (önizleme sahnesi). Claude'un UI'ı Play'e girmeden görmesi için.
    public static class UiKitPreview
    {
        [MenuItem("Tools/Blob.io/Arayüz Kiti Önizleme")]
        public static void Render()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            var rt = new RenderTexture(1080, 1920, 24);
            try
            {
                var camGo = new GameObject("PreviewCam");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                Camera cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.25f, 0.27f, 0.36f);
                cam.targetTexture = rt;

                var canvasGo = new GameObject("Canvas", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(canvasGo, scene);
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 5f;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0f;
                Transform root = canvasGo.transform;

                float y = 820f;
                if (System.IO.File.Exists("Temp/ui_preview_mult.txt"))
                {
                    // Ölçek deneyi: her satır bir çarpan, kurdele 860x120
                    foreach (string line in System.IO.File.ReadAllLines("Temp/ui_preview_mult.txt"))
                    {
                        if (!float.TryParse(line.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float m)) continue;
                        Image t = UiKit.NewImage("m" + m, root, new Vector2(0f, y), new Vector2(860f, 120f), "Banners/ribbon_gold");
                        t.pixelsPerUnitMultiplier = m;
                        y -= 150f;
                    }
                }
                Add(root, "Banners/ribbon_gold", new Vector2(0f, y), new Vector2(860f, 120f)); y -= 170f;
                Add(root, "Banners/ribbon_red", new Vector2(0f, y), new Vector2(760f, 150f)); y -= 190f;
                Add(root, "Banners/plate_red", new Vector2(0f, y), new Vector2(780f, 130f)); y -= 170f;
                Add(root, "Buttons/btn_green", new Vector2(-250f, y), new Vector2(420f, 130f));
                Add(root, "Buttons/btn_gold", new Vector2(250f, y), new Vector2(420f, 120f)); y -= 160f;
                Add(root, "Pills/pill_orange", new Vector2(-300f, y), new Vector2(240f, 70f));
                Add(root, "Pills/pill_blue", new Vector2(0f, y), new Vector2(172f, 56f));
                Add(root, "Badges/badge_green", new Vector2(300f, y), new Vector2(220f, 82f)); y -= 130f;
                Add(root, "Bars/bar_track", new Vector2(0f, y), new Vector2(720f, 40f)); y -= 90f;
                Add(root, "Panels/card_cream", new Vector2(0f, y - 80f), new Vector2(920f, 290f)); y -= 360f;
                Add(root, "Panels/hud_navy", new Vector2(-300f, y), new Vector2(220f, 90f));
                Add(root, "Panels/modal_purple", new Vector2(200f, y - 160f), new Vector2(620f, 500f));

                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                System.IO.File.WriteAllBytes("Temp/ui_preview.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Debug.Log("[UiKitPreview] Temp/ui_preview.png");
            }
            finally
            {
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Add(Transform root, string key, Vector2 pos, Vector2 size)
        {
            Image img = UiKit.NewImage(key, root, pos, size, key);
            UiKit.FitBorder(img);
        }
    }
}
