using System;
using System.Collections.Generic;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Pedestrians;
using BlobSurvivor.Entities.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → Kale Sahnesini Kur: 2. harita (Kuşatılmış Kale). GameScene'in kopyası CastleScene'i açar (yoksa
    // oluşturur), şehri (City_Level01, CityShore, RoadNetwork) çıkarır, Art/City/Medieval/castle_layout.json'u
    // (tools/medieval/build_castle_layout.py) ortaçağ kitiyle kurar, sınır, blob başlangıcı, spawner sınırları, NavMesh.
    // Prefab'lar şehir hattından (CityKitPipeline.BuildPrefab: B_ bina/duvar, P_ eşya, L_ zemin). Tekrar çalıştırmak güvenli.
    public static class CastleSceneBuilder
    {
        private const string GameScenePath = "Assets/_Project/Scenes/GameScene.unity";
        internal const string CastleScenePath = "Assets/_Project/Scenes/CastleScene.unity";
        private const string LayoutPath = CityKitPipeline.MedievalRoot + "/castle_layout.json";
        private const string RootName = "Castle_Level";
        private static readonly string[] CityRoots = { "City_Level01", "CityShore", "RoadNetwork" };

        [Serializable]
        private class Item
        {
            public string mesh;
            public float x, y, z, yaw, s = 1f;
        }

        [Serializable]
        private class GroundRect
        {
            public string tex;
            public float x0, z0, x1, z1;
        }

        [Serializable]
        private class Meta
        {
            public float half = 60f;
            public float[] start = null;
            public GroundRect[] ground = null;
            public float[] zLines = null;   // bölge sınırları (güneyden kuzeye)
            public float side = 24f;         // oyun alanı yarı genişliği
            public float[] keep = null;
        }

        private const string TextureFolder = "Assets/_Project/Art/City/Textures";
        private const string SidewalkMaterial = TextureFolder + "/Mat_Sidewalk.mat";
        private const float GroundTile = 4f;     // bir doku tekrarı = 4×4 birim
        private const float GroundStep = 0.012f; // üst üste binen zeminler arası (z-fighting)

        [Serializable]
        private class Layout
        {
            public Meta meta = null;
            public Item[] items = null;
        }

        [MenuItem("Tools/Blob.io/Kale Sahnesini Kur")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            if (!OpenCastleScene()) return;

            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (json == null) { Debug.LogError($"[CastleScene] {LayoutPath} yok: python tools/medieval/build_castle_layout.py"); return; }
            Layout layout = JsonUtility.FromJson<Layout>(json.text);
            Material palette = CityKitPipeline.GetMedievalMaterial();
            if (palette == null) { Debug.LogError("[CastleScene] Ortaçağ paleti yok (Art/City/Medieval/MedievalPalette.png)."); return; }

            foreach (string name in CityRoots)
            {
                GameObject old = GameObject.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            }
            GameObject existing = GameObject.Find(RootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            // Prefab'lar (mesh başına bir kez).
            var prefabs = new Dictionary<string, GameObject>();
            foreach (Item item in layout.items)
            {
                if (prefabs.ContainsKey(item.mesh)) continue;
                GameObject prefab = CityKitPipeline.BuildPrefab(new CityKitPipeline.CityItem { mesh = item.mesh, sx = 1f, sy = 1f, sz = 1f }, palette);
                if (prefab == null) { Debug.LogError($"[CastleScene] {item.mesh} prefab'ı kurulamadı."); return; }
                if (IsRingWall(item.mesh)) prefab = SetupRingWall(prefab);
                prefabs.Add(item.mesh, prefab);
            }
            AssetDatabase.SaveAssets();

            GameObject root = new GameObject(RootName);
            root.AddComponent<CityLevel>(); // yenen objeler run sonunda geri gelir
            Meta meta = layout.meta ?? new Meta();
            float[] zLines = meta.zLines ?? new float[0];
            var castleMap = root.AddComponent<BlobSurvivor.Core.CastleMap>(); // bölgeye göre doğma, küçük harita
            castleMap.EditorSetup(zLines, meta.side, meta.half,
                meta.keep != null && meta.keep.Length == 2 ? new Vector2(meta.keep[0], meta.keep[1]) : Vector2.zero);
            foreach (Item item in layout.items)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[item.mesh], root.transform);
                go.transform.localPosition = new Vector3(item.x, item.y, item.z);
                go.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
                if (!Mathf.Approximately(item.s, 1f)) go.transform.localScale = Vector3.one * item.s;
                if (IsZoneGate(item.mesh)) AddGateSign(go, ZoneIndex(zLines, item.z));
            }
            BuildSideBounds(root.transform, meta.side, meta.half);

            if (layout.meta != null && layout.meta.ground != null) BuildGround(root.transform, layout.meta.ground);
            float half = layout.meta != null ? layout.meta.half : 60f;
            CityExpansion.BuildBounds(root.transform, half); // MapBounds "CityBounds"ı adla bulur

            // Arabalar yok (yol ağı yok); spawner sınırları haritaya.
            foreach (CarSpawner cars in UnityEngine.Object.FindObjectsByType<CarSpawner>(FindObjectsInactive.Include))
                cars.gameObject.SetActive(false);
            CityKitPipeline.AssignEnemyMapExtent(half - 1f);
            SetFloat<ConsumableSpawner>("_mapHalfExtent", half - 2f);
            SetFloat<PedestrianSpawner>("_mapHalfExtent", half - 2f);

            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null && layout.meta != null && layout.meta.start != null && layout.meta.start.Length == 2)
            {
                Vector3 p = blob.transform.position;
                blob.transform.position = new Vector3(layout.meta.start[0], p.y, layout.meta.start[1]);
            }

            Debug.Log("[MedievalSkin] " + MedievalSkinSetup.Apply()); // düşman, boss, dalga, yaya, yerdeki yiyecek
            EnemyWalkSetup.Setup("Enemy_Med");
            PedestrianRigSetup.Setup("Med");

            // Sahne GameScene'in kopyası: yüzeyler şehrin NavMesh asset'ini gösteriyorsa bake onu siler/ezer.
            // Önce bağı kopar; bake Scenes/CastleScene altına yenisini yazar.
            foreach (var surface in UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsInactive.Include))
            {
                string dataPath = surface.navMeshData != null ? AssetDatabase.GetAssetPath(surface.navMeshData) : null;
                if (dataPath != null && !dataPath.Contains("/CastleScene/")) surface.navMeshData = null;
            }
            CityKitPipeline.BakeNavMesh();
            RenderMinimap(castleMap.MinimapWorld);
            AddToBuildSettings();
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Debug.Log($"[CastleScene] Kale kuruldu: {layout.items.Length} obje, {prefabs.Count} prefab. NavMesh bake bitince tekrar kaydet (Ctrl+S).");
        }

        // ---------- halka duvarları ----------
        // Yenince yol açılsın: NavMesh'e kalıcı delik (NotWalkable hacim) yerine taşıyıcı engel (NavMeshObstacle, carve);
        // duvar yenince obje kapanır, engel kalkar, elit/boss yol bulur. Sürü polisi zaten EnvironmentObstacle'a bakar.
        // Fare delikli parçada gövde ve engel kutusu ikiye bölünür: ortada küçük blob'un sığdığı boşluk; düşman yürüyüş
        // sorguları (PropObstacle) için boşluk kapalı, elitin NavMesh'i de (engel tam boy).
        private const float HoleGap = 0.62f;      // Tiny 0.5, Little 0.76: Tiny geçer
        private static readonly string[] RingWallPrefixes = { "B_MedFence", "B_MedStoneWall", "B_MedCastleWall", "B_MedMainGate", "B_MedGateClosed" };
        private static readonly string[] GateMeshes = { "B_MedFenceGate", "B_MedGateClosed", "B_MedMainGate" };

        private static bool IsZoneGate(string mesh) => Array.IndexOf(GateMeshes, mesh) >= 0;

        private static int ZoneIndex(float[] zLines, float z)
        {
            int best = 0;
            float d = float.MaxValue;
            for (int i = 0; i < zLines.Length; i++)
                if (Mathf.Abs(zLines[i] - z) < d) { d = Mathf.Abs(zLines[i] - z); best = i; }
            return best; // kapı i. sınırda = i. bölgenin çıkışı
        }

        // Kapı üstünde boy işareti (dünya yazısı, kameraya dönük; ZoneGate günceller).
        private static void AddGateSign(GameObject gate, int zone)
        {
            var sign = new GameObject("GateSign");
            sign.transform.SetParent(gate.transform, false);
            float top = 0f;
            foreach (Renderer r in gate.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
            sign.transform.position = new Vector3(gate.transform.position.x, top + 0.6f, gate.transform.position.z - 1.2f); // kapının önünde, üstünde
            sign.transform.localScale = Vector3.one / Mathf.Max(0.01f, gate.transform.lossyScale.x);
            var tmp = sign.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "BOY";
            tmp.fontSize = 6f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(12f, 3f);
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(30, 20, 10, 255);
            var zg = gate.AddComponent<BlobSurvivor.UI.ZoneGate>();
            zg.EditorSetup(zone, tmp);
        }

        // Oyun alanının yanları: görünmez duvar (Environment). Ayrı obje: MapBounds kare CityBounds'a bakar, bunu saymaz.
        private static void BuildSideBounds(Transform root, float side, float half)
        {
            var go = new GameObject("SideBounds") { layer = LayerMask.NameToLayer("Environment") };
            go.transform.SetParent(root, false);
            foreach (float sx in new[] { -1f, 1f })
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(sx * (side + 1f), 15f, 0f);
                box.size = new Vector3(2f, 30f, half * 2f + 4f);
            }
        }

        private static bool IsRingWall(string mesh)
        {
            foreach (string p in RingWallPrefixes) if (mesh.StartsWith(p)) return true;
            return false;
        }

        private static GameObject SetupRingWall(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var volume = root.GetComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
                if (volume != null) UnityEngine.Object.DestroyImmediate(volume);
                var modifier = root.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
                if (modifier == null) modifier = root.AddComponent<Unity.AI.Navigation.NavMeshModifier>(); // Unity null: ?? çalışmaz
                modifier.ignoreFromBuild = true;

                BoxCollider body = root.GetComponent<BoxCollider>();
                if (body == null) return prefab;
                var nav = root.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (nav == null) nav = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                nav.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                nav.center = body.center;
                nav.size = body.size;
                nav.carving = true;
                nav.carveOnlyStationary = true;

                if (root.name.EndsWith("Hole"))
                {
                    SplitBox(body, HoleGap);
                    Transform obstacle = root.transform.Find("EnvironmentObstacle");
                    BoxCollider obstacleBox = obstacle != null ? obstacle.GetComponent<BoxCollider>() : null;
                    if (obstacleBox != null) SplitBox(obstacleBox, HoleGap);

                    Transform plug = root.transform.Find("HolePlug");
                    if (plug != null) UnityEngine.Object.DestroyImmediate(plug.gameObject);
                    var plugGo = new GameObject("HolePlug") { layer = CityKitPipeline.PropObstacleLayer };
                    plugGo.transform.SetParent(root.transform, false);
                    var plugBox = plugGo.AddComponent<BoxCollider>();
                    plugBox.center = new Vector3(0f, 1f, body.center.z);
                    plugBox.size = new Vector3(HoleGap + 0.1f, 2f, body.size.z);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // X boyunca ortada 'gap' boşluk bırakan iki kutu (aynı objede ikinci BoxCollider).
        private static void SplitBox(BoxCollider box, float gap)
        {
            foreach (BoxCollider other in box.GetComponents<BoxCollider>())
                if (other != box) UnityEngine.Object.DestroyImmediate(other);
            float minX = box.center.x - box.size.x / 2f, maxX = box.center.x + box.size.x / 2f;
            float left = -gap / 2f - minX, right = maxX - gap / 2f;
            if (left <= 0.05f || right <= 0.05f) return;
            Vector3 c = box.center, s = box.size;
            box.center = new Vector3(minX + left / 2f, c.y, c.z);
            box.size = new Vector3(left, s.y, s.z);
            BoxCollider second = box.gameObject.AddComponent<BoxCollider>();
            second.isTrigger = box.isTrigger;
            second.center = new Vector3(gap / 2f + right / 2f, c.y, c.z);
            second.size = new Vector3(right, s.y, s.z);
        }

        // Küçük harita: oyun alanının üstten ortografik görüntüsü (UI hariç) → Resources/UI/CastleMinimap.png (GateGuide).
        private const string MinimapPath = "Assets/_Project/Resources/UI/CastleMinimap.png";

        private static void RenderMinimap(Rect world)
        {
            const int width = 256;
            int height = Mathf.RoundToInt(width * world.height / world.width);
            var camGo = new GameObject("MinimapCam");
            var rt = new RenderTexture(width, height, 24);
            try
            {
                Camera cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = world.height / 2f;
                cam.aspect = world.width / world.height;
                cam.transform.position = new Vector3(world.center.x, 80f, world.center.y);
                cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.35f, 0.2f);
                cam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
                cam.nearClipPlane = 1f;
                cam.farClipPlane = 200f;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                System.IO.File.WriteAllBytes(MinimapPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(camGo);
            }
            AssetDatabase.ImportAsset(MinimapPath);
            var importer = AssetImporter.GetAtPath(MinimapPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        // Dokulu düzlemler (çarpışmasız; yürünen zemin sahnedeki 'Ground' collider'ı). Mesh ve materyaller asset olarak.
        private static void BuildGround(Transform root, GroundRect[] rects)
        {
            var parent = new GameObject("Ground_Medieval").transform;
            parent.SetParent(root, false);
            for (int i = 0; i < rects.Length; i++)
            {
                GroundRect r = rects[i];
                Material mat = GroundMaterial(r.tex);
                if (mat == null) continue;
                Mesh mesh = GroundMesh(i, r, (i + 1) * GroundStep);
                var go = new GameObject("Ground_" + r.tex);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            }
        }

        private static Material GroundMaterial(string tex)
        {
            string path = $"{CityKitPipeline.MedievalRoot}/Mat_Ground{tex}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/Med_{tex}.png");
            if (texture == null) { Debug.LogError($"[CastleScene] Med_{tex}.png yok (tools/medieval/build_ground_textures.py)."); return null; }
            if (mat == null)
            {
                Material template = AssetDatabase.LoadAssetAtPath<Material>(SidewalkMaterial);
                mat = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh GroundMesh(int index, GroundRect r, float y)
        {
            string path = $"{CityKitPipeline.MedievalRoot}/GroundMesh_{index}.asset";
            var mesh = new Mesh { name = "Ground_" + index };
            mesh.vertices = new[] { new Vector3(r.x0, y, r.z0), new Vector3(r.x0, y, r.z1), new Vector3(r.x1, y, r.z1), new Vector3(r.x1, y, r.z0) };
            mesh.uv = new[] { new Vector2(r.x0, r.z0) / GroundTile, new Vector2(r.x0, r.z1) / GroundTile,
                              new Vector2(r.x1, r.z1) / GroundTile, new Vector2(r.x1, r.z0) / GroundTile };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static bool OpenCastleScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == CastleScenePath) return true;
            if (active.isDirty)
            {
                Debug.LogError($"[CastleScene] Açık sahne ({active.name}) kaydedilmemiş: önce kaydet, sonra tekrar çalıştır.");
                return false;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CastleScenePath) == null && !AssetDatabase.CopyAsset(GameScenePath, CastleScenePath))
            {
                Debug.LogError("[CastleScene] GameScene kopyalanamadı.");
                return false;
            }
            EditorSceneManager.OpenScene(CastleScenePath, OpenSceneMode.Single);
            return true;
        }

        private static void SetFloat<T>(string field, float value) where T : UnityEngine.Object
        {
            T target = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedProperties();
        }

        private static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == CastleScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(CastleScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
