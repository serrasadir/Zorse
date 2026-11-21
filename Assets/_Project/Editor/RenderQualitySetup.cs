using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → Görüntü Kalitesini Uygula (Bahar, 2026-11-21: "daha profesyonel ve pürüzsüz").
    // RP asset'leri: 4x MSAA (FXAA yerine; kenarlar tırtıksız), çözünürlük %90 (eski %80 bulanıktı), yumuşak gölge (düşük).
    // Zemin dokuları: anizotropik 4 (yatık bakan yol/çimen keskin). İki sahnede kamera FXAA kapatılır.
    // FPS: runtime'da AdaptiveFrameRate (60, kaldıramayan telefonda 30). Tekrar çalıştırmak güvenli.
    public static class RenderQualitySetup
    {
        private static readonly string[] RpAssets = { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" };
        private static readonly string[] Scenes = { "Assets/_Project/Scenes/GameScene.unity", "Assets/_Project/Scenes/CastleScene.unity" };
        private const string GroundTextures = "Assets/_Project/Art/City/Textures";
        // Varsayılan hafif (zayıf telefon takılmadan başlasın); güçlü telefon AdaptiveFrameRate ile 4x / %90'a çıkar.
        private const int Msaa = 2;
        private const float RenderScale = 0.85f;

        [MenuItem("Tools/Blob.io/Görüntü Kalitesini Uygula")]
        public static void Run()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty) { Debug.LogError($"[RenderQuality] {active.name} kaydedilmemiş: önce kaydet."); return; }
            string returnTo = active.path;

            foreach (string path in RpAssets)
            {
                var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (rp == null) continue;
                var so = new SerializedObject(rp);
                so.FindProperty("m_MSAA").intValue = Msaa;
                so.FindProperty("m_RenderScale").floatValue = RenderScale;
                so.FindProperty("m_SoftShadowsSupported").boolValue = true;
                SerializedProperty quality = so.FindProperty("m_SoftShadowQuality");
                if (quality != null) quality.intValue = 1; // Low
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rp);
            }

            int textures = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { GroundTextures }))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is TextureImporter ti && ti.anisoLevel != 4)
                {
                    ti.anisoLevel = 4;
                    ti.SaveAndReimport();
                    textures++;
                }
            }

            int cameras = 0;
            foreach (string scenePath in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) continue;
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                {
                    UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                    if (data == null || data.antialiasing == AntialiasingMode.None) continue;
                    data.antialiasing = AntialiasingMode.None;
                    EditorUtility.SetDirty(data);
                    cameras++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(returnTo)) EditorSceneManager.OpenScene(returnTo, OpenSceneMode.Single);
            Debug.Log($"[RenderQuality] MSAA {Msaa}x, çözünürlük %{RenderScale * 100:0}, yumuşak gölge; {textures} zemin dokusu anizotropik; {cameras} kamerada FXAA kapandı.");
        }
    }
}
