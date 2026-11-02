using System.IO;
using System.Text;
using BlobSurvivor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.EditorTools
{
    // 2026-11-02: "Şehir Kur"u baştan çalıştırmadan mevcut prefab'lara uygular: model yönleri (180°),
    // yüksek binaların basıklığı, saydam "ghost" materyaller ve kameraya CameraOcclusionFader.
    // Tekrar çalıştırmak güvenli: değerler eklenmez, kaynaktan yeniden hesaplanır.
    // Temp/city_fixes.request varsa script yüklemesinde kendiliğinden çalışır (IosBuildTool deseni).
    public static class CityVisualFixes
    {
        private const string RequestPath = "Temp/city_fixes.request";
        private const string ResultPath = "Temp/city_fixes.result";
        private const string GhostFolder = "Assets/_Project/Resources/OcclusionGhost";
        private const float GhostAlpha = 0.3f;

        private static readonly string[] GhostSources =
        {
            CityKitPipeline.ArtRoot + "/Mat_CityPalette.mat",
            "Assets/_Project/Art/Buildings/Restaurant/Mat_Restaurant.mat",
        };

        [InitializeOnLoadMethod]
        private static void RunPendingRequest()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                File.Delete(RequestPath);
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    File.WriteAllText(ResultPath, "ABORTED: Play Mode açık");
                    return;
                }
                File.WriteAllText(ResultPath, Apply());
            };
        }

        [MenuItem("Tools/Blob.io/Şehir Görsel Düzeltmeleri")]
        private static void ApplyFromMenu() => Debug.Log("[CityVisualFixes]\n" + Apply());

        private static string Apply()
        {
            StringBuilder log = new StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab B_", new[] { "Assets/_Project/Prefabs/City" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string mesh = Path.GetFileNameWithoutExtension(path);
                if (!mesh.StartsWith("B_")) continue;
                log.AppendLine(FixPrefab(path, mesh, refitColliders: true));
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab Car_V_", new[] { "Assets/_Project/Prefabs/City/Cars" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                log.AppendLine(FixPrefab(path, Path.GetFileNameWithoutExtension(path).Substring("Car_".Length), refitColliders: true));
            }

            log.AppendLine(FixPrefab("Assets/_Project/Prefabs/City/Pedestrian_Man.prefab", "H_Man", refitColliders: false));
            log.AppendLine(FixPrefab("Assets/_Project/Prefabs/City/Pedestrian_Woman.prefab", "H_Woman", refitColliders: false));
            log.AppendLine(FixPrefab("Assets/_Project/Prefabs/Enemies/Enemy_Police.prefab", "H_Police", refitColliders: false));
            log.AppendLine(FixPrefab("Assets/_Project/Prefabs/Enemies/Enemy_ElitePolice.prefab", "H_Elite", refitColliders: false));

            foreach (string source in GhostSources)
                log.AppendLine(CreateGhost(source));

            log.AppendLine(AddFaderToCamera());
            AssetDatabase.SaveAssets();

            // Bina ayak izleri 180° dönünce birkaç onda bir birim kayıyor (tente vb. asimetrik).
            CityKitPipeline.BakeNavMesh();
            log.AppendLine("NavMesh bake başlatıldı. Bitince sahneyi kaydet (Cmd+S).");
            return log.ToString();
        }

        private static string FixPrefab(string path, string mesh, bool refitColliders)
        {
            MeshFilter source = CityKitPipeline.LoadSourceMesh(mesh);
            if (source == null) return $"ATLANDI {path}: {mesh}.fbx yok";

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform model = contents.transform.Find("Model");
                if (model == null) return $"ATLANDI {path}: Model child yok";

                CityKitPipeline.ApplyModelTransform(model, source, mesh);

                if (refitColliders)
                {
                    // LoadPrefabContents kökü orijinde tutar: renderer.bounds prefab-yerel uzaydadır.
                    Bounds bounds = model.GetComponent<MeshRenderer>().bounds;
                    BoxCollider body = contents.GetComponent<BoxCollider>();
                    if (body != null) { body.center = bounds.center; body.size = bounds.size; }

                    Transform obstacle = contents.transform.Find("EnvironmentObstacle");
                    BoxCollider obstacleBox = obstacle != null ? obstacle.GetComponent<BoxCollider>() : null;
                    if (obstacleBox != null) { obstacleBox.center = bounds.center; obstacleBox.size = bounds.size; }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                return $"OK {Path.GetFileName(path)} rot={model.localEulerAngles.y:F0} scaleY={model.localScale.y:F2}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static string CreateGhost(string sourcePath)
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (source == null) return $"ATLANDI ghost: {sourcePath} yok";

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            if (!AssetDatabase.IsValidFolder(GhostFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "OcclusionGhost");

            string ghostPath = $"{GhostFolder}/{source.name}_Ghost.mat";
            Material ghost = new Material(source) { name = source.name + "_Ghost" };

            // URP Lit/Simple Lit transparan ayarı (Inspector'daki Surface Type = Transparent ile aynı).
            ghost.SetFloat("_Surface", 1f);
            ghost.SetFloat("_Blend", 0f);
            ghost.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            ghost.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            ghost.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            ghost.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            ghost.SetFloat("_ZWrite", 0f);
            ghost.SetOverrideTag("RenderType", "Transparent");
            ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghost.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghost.renderQueue = (int)RenderQueue.Transparent;

            Color color = ghost.GetColor("_BaseColor");
            color.a = GhostAlpha;
            ghost.SetColor("_BaseColor", color);

            AssetDatabase.DeleteAsset(ghostPath);
            AssetDatabase.CreateAsset(ghost, ghostPath);
            return $"OK ghost {ghostPath}";
        }

        private static string AddFaderToCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return "ATLANDI fader: sahnede MainCamera yok";
            if (camera.GetComponent<CameraOcclusionFader>() != null) return "OK fader zaten var";

            Undo.AddComponent<CameraOcclusionFader>(camera.gameObject);
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            return "OK fader Main Camera'ya eklendi (sahne kaydedilmeli)";
        }
    }
}
