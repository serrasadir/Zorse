using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.EditorTools
{
    // Şehrin dışı: kenar boyunca sur/rıhtım (W_SeaWall, köşelerde W_WallTower) + alçakta kayan su.
    // Hepsi sadece görsel (collider yok, yenmez); sınırı CityBounds'un görünmez duvarı tutar.
    // Sahnedeki gri 'Ground' plane'inin sadece renderer'ı kapanır (fizik zemini kalır).
    // Ayrıca kaldırım karosu ve su materyallerini kurar. GameScene açıkken; sonra Şehir Kur (v2) karoyu
    // zemine uygular (ya da tekrar kurulmuş şehirde bu menü yeter), sonra Cmd+S. Tekrar çalıştırmak güvenli.
    internal static class CityShore
    {
        private const string RootName = "CityShore";
        private const string TexRoot = CityKitPipeline.ArtRoot + "/Textures";
        private const string WaterMaterialPath = TexRoot + "/Mat_Water.mat";
        private const string WaterMeshPath = TexRoot + "/WaterPlane.asset";
        private const float WaterY = -0.9f;
        private const float WaterHalf = 500f;
        private const float WaterTile = 16f;    // dokunun dünyada tekrar boyu
        private const float Segment = 4f;       // W_SeaWall boyu

        [MenuItem("Tools/Blob.io/Şehir Kıyısını Kur")]
        private static void Run()
        {
            var log = new StringBuilder();
            Material sidewalk = EnsureMaterial(CityExpansion.SidewalkMaterialPath, TexRoot + "/T_Sidewalk.png", 0f);
            Material water = EnsureMaterial(WaterMaterialPath, TexRoot + "/T_Water.png", 0.35f);
            ApplySidewalkToGround(sidewalk, log);

            GameObject existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            var root = new GameObject(RootName);
            // Su ve sur NavMesh'e girmesin (yürünebilir alan şehir sınırında bitmeli).
            var shoreModifier = root.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            shoreModifier.ignoreFromBuild = true;
            shoreModifier.applyToChildren = true;

            BuildWater(root.transform, water);
            int walls = BuildWalls(root.transform, log);
            log.AppendLine($"Sur: {walls} parça + 4 kule, su {WaterHalf * 2}x{WaterHalf * 2} y={WaterY}");

            GameObject ground = GameObject.Find("Ground");
            var groundRenderer = ground != null ? ground.GetComponent<MeshRenderer>() : null;
            if (groundRenderer != null && groundRenderer.enabled)
            {
                groundRenderer.enabled = false;
                log.AppendLine("Gri Ground plane'in renderer'ı kapatıldı (collider duruyor)");
            }
            // 200x200 plane NavMesh'e girmesin: suyun altında yürünebilir alan kalıyor, çöp/elit oraya gidiyordu.
            // Bake: Şehir Görsel Düzeltmeleri.
            if (ground != null)
            {
                var modifier = ground.GetComponent<Unity.AI.Navigation.NavMeshModifier>(); // Unity null'ı: ?? kullanılmaz
                if (modifier == null) modifier = ground.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                modifier.ignoreFromBuild = true;
                modifier.applyToChildren = false;
                log.AppendLine("Ground NavMesh'ten çıkarıldı (Şehir Görsel Düzeltmeleri ile yeniden bake)");
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/city_shore.txt", log.ToString());
            Debug.Log("[CityShore]\n" + log);
        }

        private static Material EnsureMaterial(string path, string texPath, float smoothness)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); // şehirle aynı shader: yeni varyant yok
                AssetDatabase.CreateAsset(mat, path);
            }
            ConfigureTexture(texPath);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // İçe aktarma ayarlarını CityKitPipeline'daki ön işlemci verir (Art/City/Textures = Repeat + mipmap);
        // eski ayarla gelmiş dokular için bir kez yeniden içe aktarılır.
        private static void ConfigureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.wrapMode != TextureWrapMode.Repeat || !importer.mipmapEnabled))
                importer.SaveAndReimport();
        }

        // Şehir yeniden kurulmadan karo: mevcut CityGround'un ikinci alt-mesh'i yoksa zemin yeniden üretilir.
        private static void ApplySidewalkToGround(Material sidewalk, StringBuilder log)
        {
            GameObject ground = GameObject.Find("CityGround");
            if (ground == null) { log.AppendLine("UYARI: CityGround yok (Şehir Kur (v2) çalıştır)"); return; }
            var renderer = ground.GetComponent<MeshRenderer>();
            Material palette = renderer.sharedMaterials[0];
            Transform parent = ground.transform.parent;
            int index = ground.transform.GetSiblingIndex();
            Object.DestroyImmediate(ground);
            GameObject rebuilt = CityExpansion.BuildGround(parent, palette);
            rebuilt.transform.SetSiblingIndex(index);
            log.AppendLine($"Kaldırım karosu: {(rebuilt.GetComponent<MeshRenderer>().sharedMaterials.Length > 1 ? "uygulandı" : "materyal yok!")}");
        }

        private static void BuildWater(Transform root, Material water)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(WaterMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "WaterPlane" };
                AssetDatabase.CreateAsset(mesh, WaterMeshPath);
            }
            float h = WaterHalf, u = WaterHalf / WaterTile;
            mesh.Clear();
            mesh.vertices = new[] { new Vector3(-h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h), new Vector3(h, 0, -h) };
            mesh.uv = new[] { new Vector2(-u, -u), new Vector2(-u, u), new Vector2(u, u), new Vector2(u, -u) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            var go = new GameObject("Water");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, WaterY, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = water;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<WaterScroll>(); // kayan doku: static batching'e girmez (tek obje)
        }

        private static int BuildWalls(Transform root, StringBuilder log)
        {
            MeshFilter wallSrc = LoadSource("W_SeaWall"), towerSrc = LoadSource("W_WallTower");
            var palette = AssetDatabase.LoadAssetAtPath<Material>(CityKitPipeline.Kit2Root + "/Mat_CityPaletteV2.mat");
            if (wallSrc == null || towerSrc == null || palette == null) { log.AppendLine("UYARI: W_ FBX ya da palet yok"); return 0; }

            float e = CityExpansion.HalfExtent;
            int count = 0;
            int perSide = Mathf.RoundToInt(2f * e / Segment);
            // Model ön yüzü (su tarafı) Unity'de -Z: güney yaw 0, kuzey 180, doğu -90, batı 90.
            (Vector3 dir, Vector3 along, float yaw)[] sides =
            {
                (Vector3.back, Vector3.right, 0f), (Vector3.forward, Vector3.right, 180f),
                (Vector3.right, Vector3.forward, -90f), (Vector3.left, Vector3.forward, 90f),
            };
            foreach (var (dir, along, yaw) in sides)
                for (int i = 0; i < perSide; i++)
                {
                    Vector3 pos = dir * e + along * (-e + Segment * (i + 0.5f));
                    Place(root, wallSrc, "W_SeaWall", palette, pos, yaw);
                    count++;
                }
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Place(root, towerSrc, "W_WallTower", palette, new Vector3(sx * (e + 0.5f), 0f, sz * (e + 0.5f)), 0f);
            return count;
        }

        private static MeshFilter LoadSource(string mesh)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{CityKitPipeline.Kit2Root}/{mesh}.fbx");
            return model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
        }

        private static void Place(Transform root, MeshFilter source, string mesh, Material material, Vector3 pos, float yaw)
        {
            var go = new GameObject(mesh);
            go.transform.SetParent(root, false);
            go.transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var model = new GameObject("Model");
            model.transform.SetParent(go.transform, false);
            CityKitPipeline.ApplyModelTransform(model.transform, source, mesh);
            model.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            var r = model.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic);
        }
    }
}
