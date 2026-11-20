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
        private class GroundPath
        {
            public string tex;
            public float hw;
            public bool loop;
            public float[] pts;   // x0, z0, x1, z1, ... (yumuşatılmış eğri, build_castle_layout.py)
        }

        [Serializable]
        private class GroundDisc
        {
            public string tex;
            public float x, z, r;
        }

        [Serializable]
        private class Meta
        {
            public GroundPath[] paths = null;
            public GroundDisc[] discs = null;
            public float half = 60f;
            public float[] start = null;
            public GroundRect[] ground = null;
            public float[] zLines = null;   // bölge sınırları (güneyden kuzeye)
            public float side = 24f;         // oyun alanı yarı genişliği
            public float[] keep = null;
        }

        private const string TextureFolder = "Assets/_Project/Art/City/Textures";
        private const string SidewalkMaterial = TextureFolder + "/Mat_Sidewalk.mat";
        // Toprak dokusu turuncu blob'la karışıyordu (Bahar): koyu, kahverengiye çekilir.
        private static readonly Color DirtTint = new Color(0.5f, 0.44f, 0.4f);
        private const float GroundTile = 4f;     // bir doku tekrarı = 4×4 birim
        private const float GroundStep = 0.003f; // üst üste binen zeminler arası (z-fighting); temas gölgesi hepsinin üstünde

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
                if (item.mesh.StartsWith("B_MedFence") && item.mesh != "B_MedFenceGate") prefab = MakeInedible(prefab); // çitler yenmez: bölgeden kapıdan geçilir
                if ((item.mesh.StartsWith("B_") || item.mesh.StartsWith("P_")) && item.mesh != "P_MedChicken") AddContactShadow(prefab); // tavuk gezinir
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

            if (layout.meta != null && layout.meta.ground != null) BuildGround(root.transform, layout.meta);
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
            ApplyCastleLighting();
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

        // Yenmeyen engel (Bahar: çitler yenmesin): yeme bileşeni kalkar, kök Environment katmanında katı kutu.
        // Fare delikli parçanın ortadaki boşluğu (SplitBox) korunur: en küçük blob yine geçer.
        private static GameObject MakeInedible(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var consumable = root.GetComponent<CityConsumable>();
                if (consumable != null) UnityEngine.Object.DestroyImmediate(consumable);
                Transform zone = root.transform.Find(CityConsumable.EatZoneName);
                if (zone != null) UnityEngine.Object.DestroyImmediate(zone.gameObject);
                root.layer = LayerMask.NameToLayer("Environment");
                foreach (BoxCollider box in root.GetComponents<BoxCollider>()) box.isTrigger = false;
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

        // Kale ışığı (Bahar, 2026-11-20: ChatGPT referanslarına yakın): sıcak, alçak güneş, yumuşak gölge; gökten sıcak,
        // yerden serin ortam; renk profili şehrinkinin kopyası (Art/City/Medieval/CastleVolumeProfile), biraz sıcak ve doygun.
        private const string CastleProfilePath = CityKitPipeline.MedievalRoot + "/CastleVolumeProfile.asset";

        private static void ApplyCastleLighting() => ApplyWarmLighting(CastleProfilePath);

        // Şehir de aynı sıcak ışığı kullanır (CitySoftSetup), kendi profil kopyasıyla.
        // Nötr gün ışığı (Bahar, 2026-11-21: "sarı ışık filtresini kaldır"): güneş ve ortam beyaza yakın, beyaz dengesi 0.
        internal static void ApplyWarmLighting(string profilePath)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.94f, 0.96f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.78f, 0.79f, 0.80f);
            RenderSettings.ambientGroundColor = new Color(0.46f, 0.47f, 0.52f);
            DynamicGI.UpdateEnvironment();

            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.type != LightType.Directional) continue;
                light.color = new Color(1f, 0.98f, 0.95f);
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.55f; // yumuşak, renkli zeminde kirli durmasın
                Vector3 e = light.transform.eulerAngles;
                light.transform.rotation = Quaternion.Euler(48f, e.y, 0f); // biraz alçak: gölgeler uzun ve okunur
                break;
            }

            GameObject go = GameObject.Find("GlobalVolume");
            var volume = go != null ? go.GetComponent<UnityEngine.Rendering.Volume>() : null;
            if (volume == null || volume.sharedProfile == null) return;
            string cityPath = AssetDatabase.GetAssetPath(volume.sharedProfile);
            if (cityPath != profilePath)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath) == null)
                    AssetDatabase.CopyAsset(cityPath, profilePath);
                volume.sharedProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
            }
            var profile = volume.sharedProfile;
            if (profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments color))
            {
                color.postExposure.Override(0.3f);
                color.contrast.Override(8f);
                color.saturation.Override(12f);
            }
            if (!profile.TryGet(out UnityEngine.Rendering.Universal.WhiteBalance white))
            {
                white = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true);
                AssetDatabase.AddObjectToAsset(white, profile);
            }
            white.temperature.Override(0f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        // ---------- temas gölgesi ----------
        // Lightmap bake yerine (yenen binanın gölgesi yerde kalırdı): her bina/eşyanın altında yumuşak kenarlı koyu leke,
        // nesnenin child'ı olduğu için yenince o da kaybolur. Tek doku + tek saydam materyal, statik birleşir.
        private const string ShadowTexPath = "Assets/_Project/Art/FX/ContactShadow.png"; // Art/City dışında: oradaki ön işlemci alfayı siler
        private const string ShadowMatPath = CityKitPipeline.MedievalRoot + "/Mat_ContactShadow.mat";
        private const string ShadowMeshPath = CityKitPipeline.MedievalRoot + "/ContactShadowQuad.asset";
        private const float ShadowY = 0.08f;         // zemin düzlemlerinin (en fazla ~0.06) üstünde
        private const float ShadowGrow = 1.12f; // koyu çekirdek ayak izi kadar, dışa doğru yumuşak sönüm (pay boyla orantılı)
        private static Material s_shadowMat;
        private static Mesh s_shadowMesh;

        internal static void AddContactShadow(GameObject prefab)
        {
            EnsureShadowAssets();
            if (s_shadowMat == null) return;
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform old = root.transform.Find("ContactShadow");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                BoxCollider body = root.GetComponent<BoxCollider>();
                if (body == null) return;
                Bounds b = new Bounds(body.center, body.size);
                foreach (BoxCollider extra in root.GetComponents<BoxCollider>()) // fare delikli parça: iki kutu
                    b.Encapsulate(new Bounds(extra.center, extra.size));
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad); // Unity'nin dörtgeni (helikopter gölgesi gibi)
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                go.name = "ContactShadow";
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(b.center.x, ShadowY, b.center.z);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // yere yatık, yüzü yukarı
                float pad = Mathf.Clamp(0.6f * Mathf.Min(b.size.x, b.size.z), 0.3f, 1.6f);
                go.transform.localScale = new Vector3(b.size.x * ShadowGrow + pad, b.size.z * ShadowGrow + pad, 1f);
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = s_shadowMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureShadowAssets()
        {
            if (s_shadowMat != null && s_shadowMesh != null) return;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowTexPath) == null)
            {
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // Yuvarlatılmış kare: kenara doğru yumuşak sönüm.
                        float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f), v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                        float d = Mathf.Pow(Mathf.Pow(u, 4f) + Mathf.Pow(v, 4f), 0.25f);
                        float a = 1f - Mathf.SmoothStep(0.55f, 1f, d);
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                    }
                System.IO.File.WriteAllBytes(ShadowTexPath, tex.EncodeToPNG()); // eğri değişirse dosyayı sil, yeniden üretilir
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(ShadowTexPath);
                if (AssetImporter.GetAtPath(ShadowTexPath) is TextureImporter ti)
                {
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.mipmapEnabled = false;
                    ti.filterMode = FilterMode.Bilinear; // Art/City ön işlemcisi Point yapar: gradyan basamaklanmasın
                    ti.SaveAndReimport();
                }
            }
            s_shadowMat = AssetDatabase.LoadAssetAtPath<Material>(ShadowMatPath);
            if (s_shadowMat == null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null) return;
                s_shadowMat = new Material(unlit) { name = "Mat_ContactShadow" };
                s_shadowMat.SetFloat("_Surface", 1f);          // Transparent
                s_shadowMat.SetFloat("_Blend", 0f);            // Alpha
                s_shadowMat.SetFloat("_ZWrite", 0f);
                s_shadowMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                s_shadowMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                s_shadowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                s_shadowMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 50;
                s_shadowMat.SetOverrideTag("RenderType", "Transparent");
                AssetDatabase.CreateAsset(s_shadowMat, ShadowMatPath);
            }
            // Saydamlık her seferinde baştan (URP materyal arayüzünün yaptığı kurulum; kodla açınca kendiliğinden olmuyor).
            s_shadowMat.SetFloat("_Surface", 1f);
            s_shadowMat.SetFloat("_Blend", 0f);
            s_shadowMat.SetFloat("_ZWrite", 0f);
            s_shadowMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_shadowMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_shadowMat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            s_shadowMat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_shadowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            s_shadowMat.SetOverrideTag("RenderType", "Transparent");
            s_shadowMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 50;
            s_shadowMat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowTexPath));
            s_shadowMat.SetColor("_BaseColor", new Color(0.1f, 0.08f, 0.14f, 0.6f)); // mor-siyah: sıcak sahnede yumuşak
            EditorUtility.SetDirty(s_shadowMat);

            s_shadowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ShadowMeshPath);
            if (s_shadowMesh == null)
            {
                s_shadowMesh = new Mesh { name = "ContactShadowQuad" };
                s_shadowMesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
                s_shadowMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
                s_shadowMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                s_shadowMesh.RecalculateNormals();
                s_shadowMesh.RecalculateBounds();
                AssetDatabase.CreateAsset(s_shadowMesh, ShadowMeshPath);
            }
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
        // Sıra: dikdörtgenler, yol şeritleri, daireler (meydanlar yolun üstünde); her katman bir öncekinin hafif üstünde (z-fighting).
        private static void BuildGround(Transform root, Meta meta)
        {
            var parent = new GameObject("Ground_Medieval").transform;
            parent.SetParent(root, false);
            GroundRect[] rects = meta.ground;
            int layer = 0;
            for (int i = 0; i < rects.Length; i++)
                AddGroundObject(parent, rects[i].tex, GroundMesh(i, rects[i], ++layer * GroundStep));
            if (meta.paths != null)
                for (int i = 0; i < meta.paths.Length; i++)
                    AddGroundObject(parent, meta.paths[i].tex, PathMesh(i, meta.paths[i], ++layer * GroundStep));
            if (meta.discs != null)
                for (int i = 0; i < meta.discs.Length; i++)
                    AddGroundObject(parent, meta.discs[i].tex, DiscMesh(i, meta.discs[i], ++layer * GroundStep));
        }

        private static void AddGroundObject(Transform parent, string tex, Mesh mesh)
        {
            Material mat = GroundMaterial(tex);
            if (mat == null || mesh == null) return;
            var go = new GameObject("Ground_" + tex);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            if (tex == "Water" && go.GetComponent<BlobSurvivor.Core.WaterScroll>() == null) go.AddComponent<BlobSurvivor.Core.WaterScroll>();
        }

        // Yol: eğri boyunca şerit, kesitte 4 köşe (dış kenar koyu, iç açık: yol yere oyulmuş görünür).
        // UV dünya koordinatından: doku dikişsiz, gerilmez.
        private static readonly float[] PathColumns = { 1f, 0.62f, -0.62f, -1f };   // sol dış → sağ dış (yarı genişlik oranı)

        private static Mesh PathMesh(int index, GroundPath p, float y)
        {
            int n = p.pts.Length / 2;
            if (n < 2) return null;
            int cols = PathColumns.Length;
            var verts = new Vector3[n * cols];
            var uv = new Vector2[verts.Length];
            var col = new Color[verts.Length];
            for (int i = 0; i < n; i++)
            {
                Vector2 c = new Vector2(p.pts[i * 2], p.pts[i * 2 + 1]);
                int a = p.loop ? (i - 1 + n) % n : Mathf.Max(i - 1, 0);
                int b = p.loop ? (i + 1) % n : Mathf.Min(i + 1, n - 1);
                Vector2 dir = new Vector2(p.pts[b * 2] - p.pts[a * 2], p.pts[b * 2 + 1] - p.pts[a * 2 + 1]).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x) * p.hw;
                for (int k = 0; k < cols; k++)
                {
                    Vector2 q = c + side * PathColumns[k];
                    int v = i * cols + k;
                    verts[v] = new Vector3(q.x, y, q.y);
                    uv[v] = q / GroundTile;
                    float shade = Macro(q.x, q.y) * (Mathf.Abs(PathColumns[k]) > 0.99f ? PathEdgeShade : 1f);
                    col[v] = new Color(shade, shade, shade, 1f);
                }
            }
            int segs = p.loop ? n : n - 1;
            var tris = new int[segs * (cols - 1) * 6];
            int t = 0;
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % n;
                for (int k = 0; k < cols - 1; k++)
                {
                    // üstten bakınca saat yönü (Unity ön yüz)
                    tris[t++] = i * cols + k; tris[t++] = j * cols + k; tris[t++] = j * cols + k + 1;
                    tris[t++] = i * cols + k; tris[t++] = j * cols + k + 1; tris[t++] = i * cols + k + 1;
                }
            }
            return SaveGroundMesh($"PathMesh_{index}", verts, uv, tris, col);
        }

        private static Mesh DiscMesh(int index, GroundDisc d, float y)
        {
            const int n = 48;
            var verts = new Vector3[1 + n * 2];
            var uv = new Vector2[verts.Length];
            var col = new Color[verts.Length];
            void Put(int v, float x, float z, float shade)
            {
                verts[v] = new Vector3(x, y, z);
                uv[v] = new Vector2(x, z) / GroundTile;
                float m = Macro(x, z) * shade;
                col[v] = new Color(m, m, m, 1f);
            }
            Put(0, d.x, d.z, 1f);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Put(1 + i, d.x + Mathf.Cos(a) * d.r * 0.85f, d.z + Mathf.Sin(a) * d.r * 0.85f, 1f);   // iç halka
                Put(1 + n + i, d.x + Mathf.Cos(a) * d.r, d.z + Mathf.Sin(a) * d.r, PathEdgeShade);  // kenar koyu
            }
            var tris = new int[n * 9];
            int t = 0;
            for (int i = 0; i < n; i++)
            {
                int a0 = 1 + i, a1 = 1 + (i + 1) % n, b0 = 1 + n + i, b1 = 1 + n + (i + 1) % n;
                tris[t++] = 0; tris[t++] = a1; tris[t++] = a0;   // saat yönü (üstten)
                tris[t++] = a0; tris[t++] = a1; tris[t++] = b1;
                tris[t++] = a0; tris[t++] = b1; tris[t++] = b0;
            }
            return SaveGroundMesh($"DiscMesh_{index}", verts, uv, tris, col);
        }

        private static Mesh SaveGroundMesh(string name, Vector3[] verts, Vector2[] uv, int[] tris, Color[] colors)
        {
            string path = $"{CityKitPipeline.MedievalRoot}/{name}.asset";
            var mesh = new Mesh { name = name, vertices = verts, uv = uv, colors = colors, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents(); // normal haritası
            mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // Zemin: PaletteAO + normal haritası (Med_<Ad>_N.png, build_ground_normals.py) + köşe rengi (geniş açık-koyu lekeler,
        // yol kenarı koyu). Bahar 2026-11-21: "zemin çok 2D duruyor".
        private static Material GroundMaterial(string tex)
        {
            if (tex == "Water") return AssetDatabase.LoadAssetAtPath<Material>(TextureFolder + "/Mat_Water.mat"); // hendek: şehir kıyısının suyu
            string path = $"{CityKitPipeline.MedievalRoot}/Mat_Ground{tex}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/Med_{tex}.png");
            if (texture == null) { Debug.LogError($"[CastleScene] Med_{tex}.png yok (tools/medieval/build_ground_textures.py)."); return null; }
            Shader shader = Shader.Find("BlobSurvivor/PaletteAO");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null) mat.shader = shader;
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetColor("_BaseColor", tex == "Dirt" ? DirtTint : Color.white);
            mat.SetFloat("_AOStrength", 1f);
            string normalPath = $"{TextureFolder}/Med_{tex}_N.png";
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
            {
                ni.textureType = TextureImporterType.NormalMap;
                ni.SaveAndReimport();
            }
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            if (normal != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private const float GroundCell = 2f;      // zemin ızgarası (lekeler köşe renginde)
        private const float PathEdgeShade = 0.7f; // yol kenarı koyu: "yere oyulmuş" görünür

        // Geniş, yumuşak açık-koyu lekeler: döşemenin tekrarı kaybolur.
        private static float Macro(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.045f + 31.7f, z * 0.045f + 11.3f) * 0.65f
                    + Mathf.PerlinNoise(x * 0.13f + 5.1f, z * 0.13f + 77.9f) * 0.35f;
            return Mathf.Lerp(0.8f, 1f, Mathf.SmoothStep(0.15f, 0.85f, n));
        }

        private static Mesh GroundMesh(int index, GroundRect r, float y)
        {
            int nx = Mathf.Max(1, Mathf.CeilToInt((r.x1 - r.x0) / GroundCell));
            int nz = Mathf.Max(1, Mathf.CeilToInt((r.z1 - r.z0) / GroundCell));
            var verts = new Vector3[(nx + 1) * (nz + 1)];
            var uv = new Vector2[verts.Length];
            var col = new Color[verts.Length];
            for (int iz = 0; iz <= nz; iz++)
                for (int ix = 0; ix <= nx; ix++)
                {
                    float x = Mathf.Lerp(r.x0, r.x1, (float)ix / nx), z = Mathf.Lerp(r.z0, r.z1, (float)iz / nz);
                    int v = iz * (nx + 1) + ix;
                    verts[v] = new Vector3(x, y, z);
                    uv[v] = new Vector2(x, z) / GroundTile;
                    float m = Macro(x, z);
                    col[v] = new Color(m, m, m, 1f);
                }
            var tris = new int[nx * nz * 6];
            int t = 0;
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    int a = iz * (nx + 1) + ix, b = a + nx + 1; // b: kuzeydeki
                    tris[t++] = a; tris[t++] = b; tris[t++] = b + 1;
                    tris[t++] = a; tris[t++] = b + 1; tris[t++] = a + 1;
                }
            return SaveGroundMesh($"GroundMesh_{index}", verts, uv, tris, col);
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
