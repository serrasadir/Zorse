using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Pedestrians;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → Crownreach Haritasını Kur: 2. harita. CrownreachScene'i açar (ortaçağ düşman/yaya/boss kurulumu
    // sahnede hazır: MedievalSkinSetup), eski kökleri çıkarır, Art/Crownreach'teki dört bölge FBX'ini
    // (tools/crownreach/build_atlas.py) oynanır hale getirir:
    //  - Dünya 180° döner: tarla güneyde (başlangıç), kale kuzeyde (CastleMap kuralı: bölgeler güneyden kuzeye).
    //  - Teraslar (0 / 1 / 2 / 3 / 3.5 m) y=0'a indirilir: oyun düz zemin varsayar.
    //  - GroundAndDetails mesh'leri bağlı parçalara bölünür: düz olanlar zemin (çarpışmasız), yüksek olanlar (burç, kaya,
    //    süs) kümelenip yenebilir obje olur. Adlı objeler (ev, ağaç, duvar...) tek tek yenebilir; havadakiler (bayrak,
    //    çark, sergideki yiyecek) altındaki objeye bağlanır, onunla yenir.
    //  - Bölge sınırları kapılardan; kapı ve sınır duvarları sabit tier (Little / Medium / Large), kale duvarları Giant.
    //  - Ada kenarına görünmez duvar, altta su, küçük harita, NavMesh bake. Tekrar çalıştırmak güvenli.
    public static class CrownreachMapBuilder
    {
        private const string Art = "Assets/_Project/Art/Crownreach/";
        private const string GeneratedFolder = Art + "Generated";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Consumables/Crownreach";
        internal const string ScenePath = "Assets/_Project/Scenes/CrownreachScene.unity";
        private const string MinimapAsset = "Assets/_Project/Resources/UI/CrownreachMinimap.png";
        private const string MinimapResource = "UI/CrownreachMinimap";
        private const string WaterMaterialPath = "Assets/_Project/Art/City/Textures/Mat_Water.mat";
        private const string RootName = "Crownreach_Level";
        private static readonly string[] Districts = { "01_Farmland", "02_WalledTown", "03_Garrison", "04_RoyalCastle" };
        private static readonly string[] OldRoots = { "Castle_Level", "City_Level01", "CityShore", "RoadNetwork", RootName };

        // Orijinal (dönmeden önce) z'ye göre teras yüksekliği: tarla 0, kasaba 1, garnizon 2, kale 3.
        private static float DistrictTerrace(float z) => z > 21f ? 0f : z > -7f ? 1f : z > -31f ? 2f : 3f;
        // Sınır çizgilerinin en düşük tier'ı (parça kendi boyundan büyükse o): tarla çıkışı Medium, kasaba Large, garnizon Giant.
        private static readonly BlobTier[] GateTiers = { BlobTier.Medium, BlobTier.Large, BlobTier.Giant };
        private const BlobTier CastleWallTier = BlobTier.Giant;

        private const float FlatHeight = 0.6f;      // zeminden bu kadar yükselmeyen parça zemin sayılır
        private const float SlabMinArea = 30f;
        private const float GroundSlice = 0.4f;     // çarpışma ayak izi zemine yakın geometriden
        private const float Elevated = 0.3f;        // altı bu kadar havada olan obje altındakine bağlanır
        private const float LineReach = 2.2f;       // sınır çizgisine bu kadar yakın duvar/kule sınırın tier'ını alır
        private static readonly string[] Buildings = { "house", "barn", "tower", "wall", "gate", "mill", "fountain", "stall", "siege", "cart",
            "forge", "tavern", "clocktower", "witchhut", "alchemist", "chapel", "watchtower", "grandstand" }; // yeni köy yapıları da ev gibi engel
        private static readonly string[] MultiBox = { "wall", "gate", "fence", "garden" };
        // Atlas oyunun boy kuralına göre büyük (ev ~5 m: blob en son tier'da yerdi). Kale haritasının oranına yakın ölçek.
        internal const float WorldScale = 0.6f;
        // Yerleşim objelerden daha az küçülür: sokaklar/aralar açılır (Bahar: "büyükken geçemiyorum"). Duvar, kapı, çit,
        // bahçe ve zemin yerleşimle birlikte uzar (aralarında boşluk kalmasın); ev, ağaç, burç kendi ortasında küçülür.
        // Detaylı prototip enine genişlemeyi kendisi yapıyor (cityX). 1.0: aralar prototipin 1,67 katı (1.2 dağınıktı,
        // 0.9 sıkışıktı; Serra).
        internal const float SpreadScale = 1.0f;
        private static readonly Vector3 Spread = new Vector3(SpreadScale, 1f, SpreadScale);
        // Yayalar blob'un bölgesinde doğar; ilk bölge (tarla) kaleninkinden dar, 25'le kalabalıktı (Serra).
        private const int MaxPedestrians = 15;
        // Tarla zaten yiyecek dolu: yerdeki yiyecek şehirden (40 / 80) az.
        private const int InitialFood = 20;
        private const int MaxFood = 50;
        private static readonly string[] Stretch = { "wall", "gate", "fence", "garden" };
        private static readonly Matrix4x4 Turn = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));
        private static readonly Matrix4x4 GroundScale = Turn * Matrix4x4.Scale(new Vector3(SpreadScale, WorldScale, SpreadScale));

        private static Matrix4x4 Placement(Obj o)
        {
            if (Array.IndexOf(Stretch, o.kind) >= 0) return GroundScale;
            var pivot = new Vector3(o.bounds.center.x, 0f, o.bounds.center.z);
            return Turn * Matrix4x4.Translate(Vector3.Scale(pivot, Spread)) * Matrix4x4.Scale(Vector3.one * WorldScale) * Matrix4x4.Translate(-pivot);
        }

        private class Part
        {
            public Mesh mesh;          // FBX mesh'i ya da üretilmiş (dünya uzayında) mesh
            public Matrix4x4 world;    // son dünya dönüşümü
        }

        private class Obj
        {
            public string name, kind;
            public readonly List<Part> parts = new List<Part>();
            public readonly List<Vector3> verts = new List<Vector3>(); // son dünya uzayı
            public readonly List<int> tris = new List<int>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public Bounds bounds;
            public Obj supporter;
            public readonly List<Obj> riders = new List<Obj>();
            public BlobTier tier;
            public int gateZone = -1;

            // Teras indirilmiş orijinal uzaydan son dünya uzayına.
            public void Place(Matrix4x4 x)
            {
                var old = new List<Part>(parts);
                parts.Clear(); verts.Clear(); tris.Clear(); uvs.Clear();
                foreach (Part p in old) Add(new Part { mesh = p.mesh, world = x * p.world });
            }

            public void Add(Part p)
            {
                parts.Add(p);
                int baseIndex = verts.Count;
                foreach (Vector3 v in p.mesh.vertices) verts.Add(p.world.MultiplyPoint3x4(v));
                foreach (int t in p.mesh.triangles) tris.Add(baseIndex + t);
                Vector2[] uv = p.mesh.uv;
                for (int i = 0; i < p.mesh.vertexCount; i++) uvs.Add(i < uv.Length ? uv[i] : Vector2.zero);
                RecalcBounds();
            }

            public void RecalcBounds()
            {
                bounds = new Bounds(verts[0], Vector3.zero);
                foreach (Vector3 v in verts) bounds.Encapsulate(v);
            }
        }

        // Bağlı parça (aynı konumu paylaşan köşeler birleşir: düz gölgeli mesh'te her yüzün kendi köşesi var).
        private class Comp
        {
            public List<int> tris = new List<int>();
            public Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            public float offset;
            public bool slab;
            public Vector3 Center => (min + max) * 0.5f;
            public float Area => (max.x - min.x) * (max.z - min.z);
        }

        private class GroundSource
        {
            public string name;
            public Mesh mesh;
            public Vector3[] orig;      // orijinal dünya (dönmeden önce)
            public Vector3[] normals;
            public Vector2[] uv;
            public int[] owner;         // köşe → parça
            public List<Comp> comps;
        }

        [MenuItem("Tools/Blob.io/Crownreach Haritasını Kur")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            // Palet her atlas çıktısında yeniden dizilir: önceki kurulumun okuduğu kopya zemin türlerini karıştırır.
            if (s_paletteRead != null) Object.DestroyImmediate(s_paletteRead);
            s_paletteRead = null;
            if (!OpenScene()) return;
            Material palette = AssetDatabase.LoadAssetAtPath<Material>(Art + "AtlasPalette.mat");
            if (palette == null) { Debug.LogError("[Crownreach] AtlasPalette.mat yok (Tools/Blob.io/Crownreach Atlas/Open Scene bir kez kurar)."); return; }
            EnsureGhostMaterial(palette);

            foreach (string n in OldRoots)
                for (GameObject old = GameObject.Find(n); old != null; old = GameObject.Find(n))
                    Object.DestroyImmediate(old);
            ResetFolder(GeneratedFolder);
            ResetFolder(DataFolder);

            // 1) Kaynaklar: zemin parçaları ve adlı objeler (orijinal dünya uzayında).
            var grounds = new List<GroundSource>();
            var named = new List<(string name, Mesh mesh, Matrix4x4 m)>();
            foreach (string d in Districts)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + d + ".fbx");
                if (model == null) { Debug.LogError($"[Crownreach] {d}.fbx yok."); return; }
                GameObject temp = Object.Instantiate(model);
                try
                {
                    temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    foreach (MeshFilter mf in temp.GetComponentsInChildren<MeshFilter>())
                    {
                        if (mf.sharedMesh == null) continue;
                        Matrix4x4 m = mf.transform.localToWorldMatrix;
                        if (mf.name.StartsWith("GroundAndDetails")) grounds.Add(MakeGround(mf.name, mf.sharedMesh, m));
                        else named.Add((mf.name, mf.sharedMesh, m));
                    }
                }
                finally { Object.DestroyImmediate(temp); }
            }

            // 2) Teraslar: geniş düz parçalar (slab) kendi yüksekliğine göre, diğerleri üstünde durduğu slab'a göre iner.
            var slabs = new List<Comp>();
            foreach (GroundSource g in grounds)
                foreach (Comp c in g.comps)
                {
                    float terrace = DistrictTerrace(c.Center.z);
                    c.slab = c.Area > SlabMinArea && c.max.y > -0.2f && c.max.y < terrace + 0.6f;
                    if (!c.slab) continue;
                    float top = Mathf.Round(c.max.y * 2f) / 2f;
                    c.offset = Mathf.Abs(c.max.y - top) < 0.06f && top >= terrace ? top : terrace; // iç kale platformu 3.5
                    slabs.Add(c);
                }
            float Support(Vector3 bottomCenter)
            {
                float best = float.NegativeInfinity, offset = DistrictTerrace(bottomCenter.z);
                foreach (Comp s in slabs)
                {
                    // Pay: teras kenarındaki kapı/duvar iki slab'ın arasında durabilir.
                    if (bottomCenter.x < s.min.x - 1f || bottomCenter.x > s.max.x + 1f || bottomCenter.z < s.min.z - 1f || bottomCenter.z > s.max.z + 1f) continue;
                    if (s.max.y > bottomCenter.y + 0.1f || s.max.y <= best) continue;
                    best = s.max.y;
                    offset = s.offset;
                }
                return offset;
            }
            Matrix4x4 Lower(float offset) => Matrix4x4.Translate(new Vector3(0f, -offset, 0f));

            // 3) Zemin: düz parçalar zemin mesh'ine, yüksek parçalar kümelenip objeye.
            var level = new GameObject(RootName);
            level.AddComponent<CityLevel>(); // yenen objeler run sonunda geri gelir
            var groundParent = new GameObject("Ground_Crownreach").transform;
            groundParent.SetParent(level.transform, false);
            var tall = new List<(GroundSource g, Comp c)>();
            var tallSet = new HashSet<Comp>();
            var groundObjects = new List<GameObject>();
            foreach (GroundSource g in grounds)
            {
                foreach (Comp c in g.comps)
                {
                    if (c.slab) continue;
                    c.offset = Support(new Vector3(c.Center.x, c.min.y, c.Center.z));
                    if (c.max.y - c.offset > FlatHeight && c.Area < 400f) { tall.Add((g, c)); tallSet.Add(c); }
                }
                var flat = new HashSet<Comp>(g.comps.Where(c => !tallSet.Contains(c)));
                groundObjects.AddRange(GroundObjects(g, flat, palette, groundParent));
            }

            var objs = new List<Obj>();
            foreach (List<(GroundSource g, Comp c)> cluster in ClusterTall(tall))
            {
                Mesh mesh = ClusterMesh(cluster, $"Detail_{objs.Count:000}");
                var o = new Obj { name = mesh.name, kind = "detail" };
                o.Add(new Part { mesh = mesh, world = Matrix4x4.identity });
                objs.Add(o);
            }
            foreach (var (name, mesh, m) in named)
            {
                Bounds ob = TransformBounds(mesh.bounds, m);
                float offset = Support(new Vector3(ob.center.x, ob.min.y, ob.center.z));
                if (KindOf(name) == "person") continue; // prototipin duran insan figürleri: yayalar zaten geziyor
                var o = new Obj { name = name, kind = KindOf(name) };
                o.Add(new Part { mesh = mesh, world = Lower(offset) * m });
                objs.Add(o);
            }

            // 4) Havadaki objeler altındakine bağlanır (bayrak burca, çark değirmene, yiyecek sergiye).
            foreach (Obj o in objs)
            {
                if (o.bounds.min.y < Elevated / WorldScale || Array.IndexOf(Buildings, o.kind) >= 0) continue; // yapılar kendi başına
                var p = new Vector3(o.bounds.center.x, o.bounds.min.y, o.bounds.center.z);
                Obj best = null;
                float bestVolume = float.MaxValue;
                foreach (Obj s in objs)
                {
                    if (s == o || s.bounds.min.y >= Elevated / WorldScale) continue;
                    Bounds e = s.bounds;
                    e.Expand(0.6f);
                    if (!e.Contains(p)) continue;
                    float v = s.bounds.size.x * s.bounds.size.y * s.bounds.size.z;
                    if (v < bestVolume) { bestVolume = v; best = s; }
                }
                if (best != null) { o.supporter = best; best.riders.Add(o); }
            }
            // Son yerleşim: bağlı objeler taşıyıcısının dönüşümüyle (bayrak burcun üstünde kalsın).
            foreach (Obj o in objs)
            {
                if (o.supporter != null) continue;
                Matrix4x4 x = Placement(o);
                o.Place(x);
                foreach (Obj r in o.riders) r.Place(x);
            }

            // 5) Bölge sınırları kapılardan.
            List<Obj> gates = objs.Where(o => o.kind == "gate" && o.supporter == null).OrderBy(o => o.bounds.center.z).ToList();
            float[] zLines = gates.Select(g => g.bounds.center.z).ToArray();
            if (zLines.Length != GateTiers.Length)
            {
                Debug.LogWarning($"[Crownreach] {zLines.Length} kapı bulundu (3 bekleniyordu); sınırlar varsayılan.");
                zLines = new[] { -21f * SpreadScale, 7f * SpreadScale, 31f * SpreadScale };
                gates.Clear();
            }
            // Duvar, kapı ve çitler blob boyunda parçalara (kapı tabelası kapının ortasındaki parçaya).
            float SegLenFor(Obj o)
            {
                int line = gates.Contains(o) ? gates.IndexOf(o) : LineOf(o, zLines);
                BlobTier tier = o.kind == "fence" && !gates.Contains(o) ? BlobTier.Little
                    : line >= 0 ? GateTiers[Mathf.Min(line, GateTiers.Length - 1)] : CastleWallTier;
                return Mathf.Max(1.2f, TierRules.BlobDiameter(TierRules.Thresholds[(int)tier - 1]));
            }
            foreach (Obj o in objs.ToList())
            {
                if (o.supporter != null || (o.kind != "wall" && o.kind != "gate" && o.kind != "fence")) continue;
                List<Obj> parts = Segment(o, SegLenFor(o));
                int gi = gates.IndexOf(o);
                if (gi >= 0)
                    gates[gi] = parts.OrderBy(sg => Mathf.Abs(sg.bounds.center.x - o.bounds.center.x) + Mathf.Abs(sg.bounds.center.z - o.bounds.center.z)).First();
                objs.Remove(o);
                objs.AddRange(parts);
            }
            for (int i = 0; i < gates.Count; i++) gates[i].gateZone = i;

            // 6) Ada kenarı: zemin mesh'lerine geçici çarpışma, satır satır yürünebilir genişlik.
            var rows = ScanIsland(groundObjects);
            if (rows.Count == 0) { Debug.LogError("[Crownreach] Zemin taranamadı."); return; }
            float zFirst = rows.Keys.Min(), zLast = rows.Keys.Max();
            System.IO.File.WriteAllLines("Temp/crownreach_rows.txt", rows.Select(r => $"{r.Key} {r.Value.x} {r.Value.y}"));
            // Bölge yan sınırı: satırların ortanca yarı genişliği (en dar satır liman/uç yüzünden bütün haritayı daraltırdı;
            // dışarı düşen doğma adayı zaten NavMesh kontrolünde elenir).
            List<float> halfWidths = rows.Where(r => r.Key > zFirst + 3f && r.Key < zLast - 3f)
                .Select(r => Mathf.Min(-r.Value.x, r.Value.y)).OrderBy(w => w).ToList();
            float side = halfWidths[halfWidths.Count / 2] - 0.5f;
            float half = Mathf.Ceil(Mathf.Max(-zFirst, zLast) + 1f);

            // 7) Objeler.
            var objectsParent = new GameObject("Objects").transform;
            objectsParent.SetParent(level.transform, false);
            var dataCache = new Dictionary<string, ConsumableData>();
            var report = new StringBuilder();
            int built = 0;
            Obj keep = null;
            foreach (Obj o in objs)
            {
                if (o.supporter != null) continue;
                o.tier = TierOf(o, zLines);
                if (o.kind == "detail" && MapRegionsZone(zLines, o.bounds.center.z) == zLines.Length
                    && (keep == null || o.bounds.size.y > keep.bounds.size.y)) keep = o;
                built++;
            }
            if (keep != null && keep.bounds.size.y > 5f) keep.tier = BlobTier.Colossal;
            foreach (Obj o in objs)
            {
                if (o.supporter != null) continue;
                GameObject go = BuildObject(o, palette, objectsParent, zLines, dataCache);
                FlattenBase(go);
                report.AppendLine($"{o.name}\t{o.kind}\t{o.tier}\t+{string.Join(",", o.riders.Select(r => r.name))}\tc={Fmt(o.bounds.center)} s={Fmt(o.bounds.size)}\tboxes={go.GetComponents<BoxCollider>().Length}");
            }

            BuildEdges(level.transform, rows);
            CityExpansion.BuildBounds(level.transform, half + 2f); // MapBounds "CityBounds"ı adla bulur
            BuildWater(level.transform);

            var castleMap = level.AddComponent<CastleMap>();
            Vector2 keepXZ = keep != null ? new Vector2(keep.bounds.center.x, keep.bounds.center.z) : new Vector2(0f, zLast - 8f);
            castleMap.EditorSetup(zLines, side, half, keepXZ);
            castleMap.EditorSetMinimap(MinimapResource);

            // Arabalar yok; spawner sınırları haritaya.
            foreach (var cars in Object.FindObjectsByType<BlobSurvivor.Entities.Vehicles.CarSpawner>(FindObjectsInactive.Include))
                cars.gameObject.SetActive(false);
            CityKitPipeline.AssignEnemyMapExtent(half - 1f);
            SetFloat<ConsumableSpawner>("_mapHalfExtent", half - 2f);
            SetFloat<PedestrianSpawner>("_mapHalfExtent", half - 2f);
            SetInt<PedestrianSpawner>("_maxActive", MaxPedestrians);
            SetInt<ConsumableSpawner>("_initialSpawnCount", InitialFood);
            SetInt<ConsumableSpawner>("_maxActive", MaxFood);

            // Başlangıç: lonca pazar meydanı, kuzey tezgâh sırası ile kuyu arasındaki açık yol (Serra: kenarda değil, ortada).
            GameObject blob = GameObject.FindWithTag("Blob");
            float startZ = -StartProtoZ * SpreadScale;
            if (blob != null) blob.transform.position = new Vector3(0f, blob.transform.position.y, startZ);

            GameObject groundPlane = GameObject.Find("Ground");
            if (groundPlane != null) // Unity Plane 10 × 10: adanın tamamını taşısın
            {
                float s = (half + 20f) / 5f;
                groundPlane.transform.localScale = new Vector3(s, 1f, s);
            }
            Physics.SyncTransforms();
            Collider groundCollider = groundPlane != null ? groundPlane.GetComponent<Collider>() : null;
            string groundInfo = groundCollider != null ? $"{groundCollider.bounds.min}..{groundCollider.bounds.max}" : "YOK";

            // Kale sahnesinin NavMesh asset'ine yazmasın: bağı kopar, bake CrownreachScene altına yenisini yazar.
            foreach (NavMeshSurface surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include))
            {
                string dataPath = surface.navMeshData != null ? AssetDatabase.GetAssetPath(surface.navMeshData) : null;
                if (dataPath != null && !dataPath.Contains("/CrownreachScene/")) surface.navMeshData = null;
            }
            CityKitPipeline.BakeNavMesh();
            MapBuildTools.RenderMinimap(castleMap.MinimapWorld, MinimapAsset);
            AddToBuildSettings();
            ApplyLook();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(level.scene);
            EditorSceneManager.SaveScene(level.scene);

            string summary = $"objeler={built} (bağlı {objs.Count - built}), zemin={groundObjects.Count}, sınırlar=[{string.Join(", ", zLines.Select(z => z.ToString("0.0", CultureInfo.InvariantCulture)))}], " +
                             $"side={side:0.0}, half={half:0}, ada z={zFirst}..{zLast}, başlangıç z={startZ}, burç={(keep != null ? keep.name : "yok")}, Ground={groundInfo}";
            System.IO.File.WriteAllText("Temp/crownreach_build.txt", summary + "\n" + report);
            Debug.Log("[Crownreach] Kuruldu: " + summary + ". NavMesh bake bitince tekrar kaydet (Ctrl+S).");
        }

        private static string Fmt(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.0},{1:0.0},{2:0.0})", v.x, v.y, v.z);

        private static int MapRegionsZone(float[] zLines, float z)
        {
            int zone = 0;
            foreach (float l in zLines) if (z > l) zone++;
            return zone;
        }

        private static string KindOf(string name)
        {
            int us = name.IndexOf('_');
            return us > 0 ? name.Substring(0, us) : name;
        }

        private static BlobTier TierOf(Obj o, float[] zLines)
        {
            Vector3 s = o.bounds.size;
            var wdh = new Vector3(Mathf.Max(s.x, s.z), s.y, Mathf.Min(s.x, s.z));
            BlobTier size = TierRules.TierForSize(TierRules.SizeOf(wdh));
            BlobTier Max(BlobTier a, BlobTier b) => a > b ? a : b;
            if (o.gateZone >= 0) return Max(size, GateTiers[Mathf.Min(o.gateZone, GateTiers.Length - 1)]);
            int line = LineOf(o, zLines);
            if (line >= 0 && o.kind != "fence") return Max(size, GateTiers[Mathf.Min(line, GateTiers.Length - 1)]);
            bool wallish = o.kind == "wall" || o.kind == "tower" || o.kind == "gate";
            if (wallish && MapRegionsZone(zLines, o.bounds.center.z) == zLines.Length) return Max(size, CastleWallTier);
            return size;
        }

        // Sınıra değen duvar/kule/kapı parçası o sınırın tier'ını alır (birden çoksa büyüğü: geçidi erken açmasın).
        private static int LineOf(Obj o, float[] zLines)
        {
            bool wallish = o.kind == "wall" || o.kind == "tower" || o.kind == "gate" || o.kind == "fence";
            if (!wallish) return -1;
            int line = -1;
            for (int i = 0; i < zLines.Length; i++)
                if (o.bounds.min.z - LineReach < zLines[i] && o.bounds.max.z + LineReach > zLines[i]) line = i;
            return line;
        }

        // ---------- duvar parçaları ----------
        // Bütün sur hattı tek obje olunca küçük blob 40 m'lik duvarı bir lokmada yiyordu (Bahar: "benden büyük şeyleri
        // yiyorum"). Duvar/kapı/çit bağlı parçalarına ayrılır, uzun parçalar segLen boyunda dilimlenir (köşeler dilim
        // aralığına kırpılır: kutu tam bölünür), dilimler segLen ızgarasında gruplanıp ayrı obje olur; mazgallar
        // ortası hangi hücredeyse ona.
        private static List<Obj> Segment(Obj o, float segLen)
        {
            Vector3[] v = o.verts.ToArray();
            int[] t = o.tris.ToArray();
            int[] owner = Components(v, t, out int count);
            var compTris = new List<int>[count];
            for (int i = 0; i < count; i++) compTris[i] = new List<int>();
            for (int i = 0; i < t.Length; i += 3)
            {
                List<int> list = compTris[owner[t[i]]];
                list.Add(t[i]); list.Add(t[i + 1]); list.Add(t[i + 2]);
            }
            var cells = new Dictionary<Vector2Int, (List<Vector3> v, List<Vector2> uv, List<int> t)>();
            Vector2Int Cell(Vector3 c) => new Vector2Int(Mathf.FloorToInt(c.x / segLen), Mathf.FloorToInt(c.z / segLen));
            foreach (List<int> ct in compTris)
            {
                if (ct.Count == 0) continue;
                Vector3 mn = Vector3.positiveInfinity, mx = Vector3.negativeInfinity;
                foreach (int i in ct) { mn = Vector3.Min(mn, v[i]); mx = Vector3.Max(mx, v[i]); }
                bool alongX = mx.x - mn.x >= mx.z - mn.z;
                float lo = alongX ? mn.x : mn.z, hi = alongX ? mx.x : mx.z;
                int pieces = Mathf.Max(1, Mathf.CeilToInt((hi - lo) / segLen - 0.15f));
                for (int k = 0; k < pieces; k++)
                {
                    float a = Mathf.Lerp(lo, hi, (float)k / pieces), b = Mathf.Lerp(lo, hi, (float)(k + 1) / pieces);
                    Vector3 center = (mn + mx) * 0.5f;
                    if (alongX) center.x = (a + b) * 0.5f; else center.z = (a + b) * 0.5f;
                    Vector2Int key = Cell(center);
                    if (!cells.TryGetValue(key, out var cell)) cells[key] = cell = (new List<Vector3>(), new List<Vector2>(), new List<int>());
                    var remap = new Dictionary<int, int>();
                    foreach (int i in ct)
                    {
                        if (!remap.TryGetValue(i, out int n))
                        {
                            Vector3 q = v[i];
                            if (pieces > 1) { if (alongX) q.x = Mathf.Clamp(q.x, a, b); else q.z = Mathf.Clamp(q.z, a, b); }
                            remap[i] = n = cell.v.Count;
                            cell.v.Add(q);
                            cell.uv.Add(o.uvs[i]);
                        }
                        cell.t.Add(n);
                    }
                }
            }
            var result = new List<Obj>();
            foreach (var kv in cells)
            {
                var mesh = new Mesh { name = $"{o.name}_{result.Count:00}" };
                mesh.SetVertices(kv.Value.v);
                mesh.SetUVs(0, kv.Value.uv);
                mesh.SetTriangles(kv.Value.t, 0);
                mesh.RecalculateNormals(); // FBX'te köşeler yüz başına ayrık: düz gölge korunur
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GeneratedFolder}/{mesh.name}.asset");
                var seg = new Obj { name = mesh.name, kind = o.kind };
                seg.Add(new Part { mesh = mesh, world = Matrix4x4.identity });
                result.Add(seg);
            }
            // Bağlı objeler (bayrak) en yakın parçaya.
            foreach (Obj r in o.riders)
            {
                Obj best = result.OrderBy(sg => (sg.bounds.center - r.bounds.center).sqrMagnitude).First();
                r.supporter = best;
                best.riders.Add(r);
            }
            return result;
        }

        // ---------- zemin parçaları ----------

        private static GroundSource MakeGround(string name, Mesh mesh, Matrix4x4 m)
        {
            Vector3[] v = mesh.vertices;
            var g = new GroundSource { name = name, mesh = mesh, orig = new Vector3[v.Length], normals = mesh.normals, uv = mesh.uv };
            for (int i = 0; i < v.Length; i++) g.orig[i] = m.MultiplyPoint3x4(v[i]);
            Matrix4x4 nm = m.inverse.transpose;
            for (int i = 0; i < g.normals.Length; i++) g.normals[i] = nm.MultiplyVector(g.normals[i]).normalized;
            int[] tris = mesh.triangles;
            g.owner = Components(g.orig, tris, out int count);
            var comps = new Comp[count];
            for (int i = 0; i < count; i++) comps[i] = new Comp();
            for (int t = 0; t < tris.Length; t += 3)
            {
                Comp c = comps[g.owner[tris[t]]];
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = g.orig[tris[t + k]];
                    c.min = Vector3.Min(c.min, p);
                    c.max = Vector3.Max(c.max, p);
                    c.tris.Add(tris[t + k]);
                }
            }
            g.comps = comps.Where(c => c.tris.Count > 0).ToList();
            return g;
        }

        // köşe → parça indeksi (0..count-1)
        private static int[] Components(Vector3[] v, int[] tris, out int count)
        {
            var parent = new int[v.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int a) { while (parent[a] != a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; }
            void Union(int a, int b) { a = Find(a); b = Find(b); if (a != b) parent[a] = b; }
            var first = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < v.Length; i++)
            {
                var key = Vector3Int.RoundToInt(v[i] * 1000f);
                if (first.TryGetValue(key, out int j)) Union(i, j); else first[key] = i;
            }
            for (int t = 0; t < tris.Length; t += 3) { Union(tris[t], tris[t + 1]); Union(tris[t], tris[t + 2]); }
            var index = new Dictionary<int, int>();
            var owner = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                int r = Find(i);
                if (!index.TryGetValue(r, out int id)) index[r] = id = index.Count;
                owner[i] = id;
            }
            count = index.Count;
            return owner;
        }

        // Kale haritasının dokuları (Serra): çimen rengi parçalar Med_Grass; kumlu toprak yollar ve dar patikalar
        // (en fazla NarrowPath geniş, ince, uzun, açık renk) Med_Dirt; dünya uzayında GroundTile birimde bir tekrar.
        // Diğerleri (geniş taş cadde, meydan, tarla, kıyı) palet.
        private const bool TexturedGround = true;
        private const float NarrowPath = 2.6f;      // prototip birimi: patikalar 1.9–2.4, caddeler 4.9+
        private const float StartProtoZ = 74.6f; // prototip z: pazar meydanının ortası (kuyu 76.1'den başlar)
        private static readonly Vector2 TownZ = new Vector2(-8f, 19f); // surlu kasaba (prototip z): dar yol kuralı yok
        private const float GroundTile = 4f;
        private const string GrassMaterialPath = Art + "Mat_CrownreachGrass.mat";
        private const string DirtMaterialPath = Art + "Mat_CrownreachDirt.mat";
        private enum GroundKind { Palette, Grass, Dirt }

        private static Texture2D s_paletteRead;

        private static GroundKind KindOfGround(GroundSource g, Comp c)
        {
            if (!TexturedGround || c.tris.Count == 0 || g.uv.Length == 0) return GroundKind.Palette;
            if (s_paletteRead == null)
            {
                s_paletteRead = new Texture2D(2, 2);
                s_paletteRead.LoadImage(System.IO.File.ReadAllBytes(Art + "AtlasPalette.png"));
            }
            Vector2 uv = g.uv[c.tris[0]];
            Color col = s_paletteRead.GetPixelBilinear(uv.x, uv.y);
            Color.RGBToHSV(col, out float h, out float sat, out float v);
            h *= 360f;
            if (h > 65f && h < 160f && sat > 0.25f) return GroundKind.Grass;
            if (h > 25f && h < 55f && sat > 0.25f && sat < 0.6f && v > 0.8f) return GroundKind.Dirt;
            // Surlu kasabanın yolları taş kalır (palet); liman ve lonca yolları prototipte toprak renginde.
            if (c.Center.z > TownZ.x && c.Center.z < TownZ.y) return GroundKind.Palette;
            Vector3 size = c.max - c.min;
            bool narrow = Mathf.Min(size.x, size.z) <= NarrowPath && Mathf.Max(size.x, size.z) >= 6f && size.y < 0.3f;
            if (narrow && sat < 0.45f && v > 0.65f) return GroundKind.Dirt;
            return GroundKind.Palette;
        }

        private static Material GroundTexMaterial(string path, string texture, Material palette)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(palette);
                AssetDatabase.CreateAsset(mat, path);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Art/City/Textures/{texture}.png");
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static IEnumerable<GameObject> GroundObjects(GroundSource g, HashSet<Comp> keep, Material palette, Transform parent)
        {
            var byKind = new Dictionary<GroundKind, List<Comp>>();
            foreach (Comp c in keep)
            {
                GroundKind k = KindOfGround(g, c);
                if (!byKind.TryGetValue(k, out var list)) byKind[k] = list = new List<Comp>();
                list.Add(c);
            }
            foreach (var kv in byKind)
            {
                Material mat = kv.Key == GroundKind.Grass ? GroundTexMaterial(GrassMaterialPath, "Med_Grass", palette)
                    : kv.Key == GroundKind.Dirt ? GroundTexMaterial(DirtMaterialPath, "Med_Dirt", palette) : palette;
                GameObject go = GroundObject(g, kv.Value, kv.Key != GroundKind.Palette, mat, parent, kv.Key.ToString());
                if (go != null) yield return go;
            }
        }

        private static GameObject GroundObject(GroundSource g, List<Comp> keep, bool worldUv, Material mat, Transform parent, string suffix)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            foreach (Comp c in keep)
            {
                var remap = new Dictionary<int, int>();
                Matrix4x4 f = GroundScale * Matrix4x4.Translate(new Vector3(0f, -c.offset, 0f));
                float bias = LayerBias(c);
                foreach (int i in c.tris)
                {
                    if (!remap.TryGetValue(i, out int n))
                    {
                        remap[i] = n = verts.Count;
                        Vector3 p = f.MultiplyPoint3x4(g.orig[i]);
                        if (p.y > 0f) p.y = p.y * Flatten + bias;
                        verts.Add(p);
                        normals.Add(i < g.normals.Length ? Turn.MultiplyVector(g.normals[i]) : Vector3.up);
                        uvs.Add(worldUv ? new Vector2(p.x, p.z) / GroundTile : i < g.uv.Length ? g.uv[i] : Vector2.zero);
                    }
                    tris.Add(n);
                }
            }
            if (tris.Count == 0) return null;
            var mesh = new Mesh { name = $"Ground_{g.name}_{suffix}" };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{GeneratedFolder}/{mesh.name}.asset");
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        private static List<List<(GroundSource g, Comp c)>> ClusterTall(List<(GroundSource g, Comp c)> tall)
        {
            int n = tall.Count;
            var parent = Enumerable.Range(0, n).ToArray();
            int Find(int a) { while (parent[a] != a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; }
            Bounds B(int i)
            {
                var c = tall[i].c;
                var b = new Bounds();
                b.SetMinMax(c.min - new Vector3(0f, c.offset, 0f), c.max - new Vector3(0f, c.offset, 0f));
                b.Expand(0.05f);
                return b;
            }
            var bounds = Enumerable.Range(0, n).Select(B).ToArray();
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (bounds[i].Intersects(bounds[j])) { int a = Find(i), b = Find(j); if (a != b) parent[a] = b; }
            return Enumerable.Range(0, n).GroupBy(Find).Select(gr => gr.Select(i => tall[i]).ToList()).ToList();
        }

        private static Mesh ClusterMesh(List<(GroundSource g, Comp c)> cluster, string name)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            foreach (var (g, c) in cluster)
            {
                Matrix4x4 f = Matrix4x4.Translate(new Vector3(0f, -c.offset, 0f)); // yerleşim objeyle (Placement)
                var remap = new Dictionary<int, int>();
                foreach (int i in c.tris)
                {
                    if (!remap.TryGetValue(i, out int n))
                    {
                        remap[i] = n = verts.Count;
                        verts.Add(f.MultiplyPoint3x4(g.orig[i]));
                        normals.Add(i < g.normals.Length ? g.normals[i] : Vector3.up);
                        uvs.Add(i < g.uv.Length ? g.uv[i] : Vector2.zero);
                    }
                    tris.Add(n);
                }
            }
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{GeneratedFolder}/{name}.asset");
            return mesh;
        }

        private static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var r = new Bounds(m.MultiplyPoint3x4(b.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                r.Encapsulate(m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return r;
        }

        // ---------- objeler ----------

        private static GameObject BuildObject(Obj o, Material palette, Transform parent, float[] zLines, Dictionary<string, ConsumableData> dataCache)
        {
            var root = new GameObject(o.name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(o.bounds.center.x, o.bounds.min.y, o.bounds.center.z);
            Vector3 origin = root.transform.position;
            Mesh slurp = null;
            int index = 0;
            foreach (Obj part in new[] { o }.Concat(o.riders))
                foreach (Part p in part.parts)
                {
                    var model = new GameObject(index == 0 ? "Model" : "Model_" + index);
                    model.transform.SetParent(root.transform, false);
                    model.transform.SetPositionAndRotation(p.world.GetColumn(3), p.world.rotation);
                    model.transform.localScale = p.world.lossyScale;
                    model.AddComponent<MeshFilter>().sharedMesh = p.mesh;
                    var mr = model.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = palette;
                    if (part.kind == "MillBlades") AddMillSpin(model);
                    else GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic);
                    if (slurp == null) slurp = p.mesh;
                    index++;
                }
            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);

            // Tam boy (riders dahil): yeme alanı ve yükseklik.
            Bounds full = o.bounds;
            foreach (Obj r in o.riders) full.Encapsulate(r.bounds);
            full.center -= origin;

            List<Bounds> boxes = Boxes(o, origin, full);
            if (o.kind == "gate") // kemerin altı açık: kapıyı yemeden geçilmesin, kutu yere iner
                for (int i = 0; i < boxes.Count; i++)
                {
                    Bounds g = boxes[i];
                    g.SetMinMax(new Vector3(g.min.x, -origin.y, g.min.z), g.max);
                    boxes[i] = g;
                }
            foreach (Bounds b in boxes)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }

            ConsumableData data = DataFor(o, root.transform, dataCache);
            var consumable = root.AddComponent<CityConsumable>();
            root.layer = TierLayers.For(o.tier);
            var so = new SerializedObject(consumable);
            so.FindProperty("_data").objectReferenceValue = data;
            so.FindProperty("_slurpMesh").objectReferenceValue = slurp;

            bool zoneWall = o.gateZone >= 0 || ((o.kind == "wall" || o.kind == "tower" || o.kind == "fence") && TouchesLine(o, zLines));
            bool building = Array.IndexOf(Buildings, o.kind) >= 0 || (o.kind == "detail" && full.size.y > 1.5f && full.size.x * full.size.z > 4f);
            if (building || zoneWall)
            {
                var obstacle = new GameObject("EnvironmentObstacle") { layer = LayerMask.NameToLayer("Environment") };
                obstacle.transform.SetParent(root.transform, false);
                foreach (Bounds b in boxes)
                {
                    var box = obstacle.AddComponent<BoxCollider>();
                    box.center = b.center;
                    box.size = b.size;
                }
                so.FindProperty("_obstacle").objectReferenceValue = obstacle.GetComponent<BoxCollider>();
            }

            if (zoneWall)
            {
                // Yenince yol açılsın: kalıcı delik yerine taşıyıcı engel (kale halka duvarları gibi).
                var modifier = root.AddComponent<NavMeshModifier>();
                modifier.ignoreFromBuild = true;
                for (int i = 0; i < boxes.Count; i++)
                {
                    var carve = new GameObject("NavCarve_" + i);
                    carve.transform.SetParent(root.transform, false);
                    var nav = carve.AddComponent<NavMeshObstacle>();
                    nav.shape = NavMeshObstacleShape.Box;
                    nav.center = boxes[i].center;
                    nav.size = boxes[i].size;
                    nav.carving = true;
                    nav.carveOnlyStationary = true;
                }
            }
            else if (boxes.Count == 1)
            {
                CityKitPipeline.ApplyNavBlocker(root, boxes[0], humanoidOnly: !building);
                if (!building) CityKitPipeline.ApplyPropObstacle(root, boxes[0]);
            }
            else
            {
                for (int i = 0; i < boxes.Count; i++)
                {
                    var block = new GameObject("NavBlock_" + i);
                    block.transform.SetParent(root.transform, false);
                    CityKitPipeline.ApplyNavBlocker(block, boxes[i], humanoidOnly: !building);
                }
            }

            // Görünür boy ayak izinden çok büyükse (ağaç tacı, bayraklı burç) görünür boy kadar tetikleyici yeme alanı.
            if (!building && boxes.Count == 1 && full.size.x * full.size.z > boxes[0].size.x * boxes[0].size.z * 1.5f)
            {
                var zoneGo = new GameObject(CityConsumable.EatZoneName) { layer = root.layer };
                zoneGo.transform.SetParent(root.transform, false);
                var zone = zoneGo.AddComponent<BoxCollider>();
                zone.isTrigger = true;
                zone.center = full.center;
                zone.size = full.size;
                so.FindProperty("_eatZone").objectReferenceValue = zone;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (o.gateZone >= 0) MapBuildTools.AddGateSign(root, o.gateZone);
            return root;
        }

        private static bool TouchesLine(Obj o, float[] zLines)
        {
            foreach (float z in zLines)
                if (o.bounds.min.z - LineReach < z && o.bounds.max.z + LineReach > z) return true;
            return false;
        }

        // Çarpışma kutuları (kök yerel): çoğu obje tek ayak izi kutusu; duvar/kapı/çit/bahçe gibi içi boş şekiller
        // zemine değen bağlı parçalarının kutuları (yoksa 20×20 surun içi dolu duvar olurdu).
        private static List<Bounds> Boxes(Obj o, Vector3 origin, Bounds full)
        {
            float bottom = o.bounds.min.y, sliceTop = bottom + GroundSlice;
            var result = new List<Bounds>();
            bool multi = Array.IndexOf(MultiBox, o.kind) >= 0 || (o.kind == "detail" && o.bounds.size.x * o.bounds.size.z > 60f);
            if (multi)
            {
                Vector3[] v = o.verts.ToArray();
                int[] tris = o.tris.ToArray();
                int[] owner = Components(v, tris, out int count);
                var mins = Enumerable.Repeat(Vector3.positiveInfinity, count).ToArray();
                var maxs = Enumerable.Repeat(Vector3.negativeInfinity, count).ToArray();
                for (int i = 0; i < v.Length; i++)
                {
                    mins[owner[i]] = Vector3.Min(mins[owner[i]], v[i]);
                    maxs[owner[i]] = Vector3.Max(maxs[owner[i]], v[i]);
                }
                for (int c = 0; c < count; c++)
                {
                    if (mins[c].y > sliceTop || float.IsInfinity(mins[c].x)) continue;
                    Vector3 size = maxs[c] - mins[c];
                    if (size.x * size.z < 0.01f) continue;
                    var b = new Bounds();
                    b.SetMinMax(new Vector3(mins[c].x, bottom, mins[c].z) - origin, new Vector3(maxs[c].x, full.max.y + origin.y, maxs[c].z) - origin);
                    result.Add(b);
                }
                // İç içe kutular atılır.
                var kept = new List<Bounds>();
                for (int i = 0; i < result.Count; i++)
                {
                    Bounds b = result[i];
                    bool inside = false;
                    for (int j = 0; j < result.Count && !inside; j++)
                    {
                        if (j == i) continue;
                        Bounds x = result[j];
                        inside = x.min.x <= b.min.x + 0.01f && x.min.z <= b.min.z + 0.01f && x.max.x >= b.max.x - 0.01f && x.max.z >= b.max.z - 0.01f
                                 && (x.size.x * x.size.z > b.size.x * b.size.z || j < i);
                    }
                    if (!inside) kept.Add(b);
                }
                result = kept;
                if (result.Count > 0 && result.Count <= 48) return result;
                result.Clear();
            }
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector3 p in o.verts)
            {
                if (p.y > sliceTop) continue;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }
            if (minX > maxX) { minX = o.bounds.min.x; maxX = o.bounds.max.x; minZ = o.bounds.min.z; maxZ = o.bounds.max.z; }
            var single = new Bounds();
            single.SetMinMax(new Vector3(minX, bottom, minZ) - origin, new Vector3(maxX, Mathf.Max(full.max.y + origin.y, bottom + 0.2f), maxZ) - origin);
            if (single.size.x < 0.2f) single.size = new Vector3(0.2f, single.size.y, single.size.z);
            if (single.size.z < 0.2f) single.size = new Vector3(single.size.x, single.size.y, 0.2f);
            result.Add(single);
            return result;
        }

        private static ConsumableData DataFor(Obj o, Transform measured, Dictionary<string, ConsumableData> cache)
        {
            Color a = Color.grey, b = Color.grey;
            bool colors = EatEffectSetup.TryCrumbColors(measured, out a, out b);
            string key = $"{o.kind}_{o.tier}_{ColorUtility.ToHtmlStringRGB(a)}{ColorUtility.ToHtmlStringRGB(b)}";
            if (cache.TryGetValue(key, out ConsumableData data)) return data;
            // Değerler asset oluşturulmadan önce yazılır, hemen diske kaydedilir: önce oluşturup sonra yazınca kayıt
            // kayboluyordu (tier 0 = her boy yer, Bahar: "en miniğken pazarı yiyorum").
            data = ScriptableObject.CreateInstance<ConsumableData>();
            int mass = (int)TierRules.MassFor(o.tier);
            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = o.kind;
            so.FindProperty("_scoreValue").intValue = mass;
            so.FindProperty("_massValue").floatValue = mass;
            so.FindProperty("_requiredTier").intValue = (int)o.tier;
            if (colors) EatEffectSetup.SetColors(so, a, b);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, $"{DataFolder}/Crown_{key}.asset");
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            cache[key] = data;
            return data;
        }

        // ---------- ada kenarı, su ----------

        // z satırı (1 birim) → yürünebilir x aralığı (min, max).
        private const float WaterBelow = -0.45f;    // liman suyu (~-0.36) bununla -0.15 arasında; adanın deniz eteği daha aşağıda

        private static SortedDictionary<float, Vector2> ScanIsland(List<GameObject> groundObjects)
        {
            var temp = new HashSet<Collider>();
            foreach (GameObject g in groundObjects)
            {
                var mc = g.AddComponent<MeshCollider>();
                mc.sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
                temp.Add(mc);
            }
            Physics.SyncTransforms();
            var rows = new SortedDictionary<float, Vector2>();
            var wetRight = new HashSet<float>();
            var wetLeft = new HashSet<float>();
            try
            {
                Bounds all = new Bounds();
                foreach (Collider c in temp) all.Encapsulate(c.bounds);
                for (float z = Mathf.Floor(all.min.z); z <= all.max.z; z += 1f)
                {
                    float lo = float.MaxValue, hi = float.MinValue, wetLo = float.MaxValue, wetHi = float.MinValue;
                    for (float x = Mathf.Floor(all.min.x); x <= all.max.x; x += 0.5f)
                        foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(x, 40f, z), Vector3.down, 80f))
                        {
                            if (!temp.Contains(hit.collider)) continue;
                            if (hit.point.y > -0.15f)
                            {
                                lo = Mathf.Min(lo, x);
                                hi = Mathf.Max(hi, x);
                                break;
                            }
                            if (hit.point.y > WaterBelow) { wetLo = Mathf.Min(wetLo, x); wetHi = Mathf.Max(wetHi, x); }
                        }
                    if (lo > hi) continue;
                    rows[z] = new Vector2(lo, hi);
                    if (wetHi > hi) wetRight.Add(z);
                    if (wetLo < lo) wetLeft.Add(z);
                }
                // Duvar altında zemin yok (satır daralır): komşu satırların en genişi alınır.
                var raw = new SortedDictionary<float, Vector2>(rows);
                foreach (float z in raw.Keys)
                {
                    // Su kenarında (liman) yumuşatma yok: duvar iskele kenarında kalsın.
                    Vector2 r = raw[z];
                    for (float dz = -1f; dz <= 1f; dz += 1f) // sur 1.4 m kalın: ±1 yeter
                        if (raw.TryGetValue(z + dz, out Vector2 n))
                            r = new Vector2(wetLeft.Contains(z) ? r.x : Mathf.Min(r.x, n.x), wetRight.Contains(z) ? r.y : Mathf.Max(r.y, n.y));
                    rows[z] = r;
                }
            }
            finally
            {
                foreach (Collider c in temp) Object.DestroyImmediate(c);
            }
            return rows;
        }

        private static void BuildEdges(Transform parent, SortedDictionary<float, Vector2> rows)
        {
            var go = new GameObject("IslandEdges") { layer = LayerMask.NameToLayer("Environment") };
            go.transform.SetParent(parent, false);
            // Kalın: iskeleden yana inince arkadaki liman suyuna geçilmesin (2 m'lik duvarın arkası açıktı).
            const float height = 30f, thick = 12f;
            void Add(Vector3 c, Vector3 s) { var b = go.AddComponent<BoxCollider>(); b.center = c; b.size = s; }
            var keys = rows.Keys.ToList();
            int start = 0;
            for (int i = 1; i <= keys.Count; i++)
            {
                bool same = i < keys.Count && Mathf.Abs(rows[keys[i]].x - rows[keys[start]].x) < 0.01f
                            && Mathf.Abs(rows[keys[i]].y - rows[keys[start]].y) < 0.01f && keys[i] - keys[i - 1] < 1.5f;
                if (same) continue;
                float z0 = keys[start] - 0.5f, z1 = keys[i - 1] + 0.5f;
                Vector2 r = rows[keys[start]];
                Add(new Vector3(r.x - 0.25f - thick / 2f, height / 2f, (z0 + z1) / 2f), new Vector3(thick, height, z1 - z0 + 0.6f));
                Add(new Vector3(r.y + 0.25f + thick / 2f, height / 2f, (z0 + z1) / 2f), new Vector3(thick, height, z1 - z0 + 0.6f));
                start = i;
            }
            float zf = keys[0] - 0.5f, zl = keys[keys.Count - 1] + 0.5f;
            Add(new Vector3(0f, height / 2f, zf - thick / 2f), new Vector3(200f, height, thick));
            Add(new Vector3(0f, height / 2f, zl + thick / 2f), new Vector3(200f, height, thick));
        }

        private static void BuildWater(Transform parent)
        {
            Material water = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            if (water == null) { Debug.LogWarning("[Crownreach] Su materyali yok: " + WaterMaterialPath); return; }
            const float e = 200f, y = -1.2f;
            var mesh = new Mesh { name = "Crownreach_Water" };
            mesh.vertices = new[] { new Vector3(-e, y, -e), new Vector3(-e, y, e), new Vector3(e, y, e), new Vector3(e, y, -e) };
            mesh.uv = new[] { new Vector2(-e, -e) / 4f, new Vector2(-e, e) / 4f, new Vector2(e, e) / 4f, new Vector2(e, -e) / 4f };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{GeneratedFolder}/{mesh.name}.asset");
            var go = new GameObject("Water");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = water;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            go.AddComponent<WaterScroll>();
        }

        // ---------- görünüm ----------
        // Prototipin (Desktop/Crownreach.html, three.js) ışığı birebir: DirectionalLight 0xffe4b3 × 2.45 (-48, 78, 40),
        // HemisphereLight 0xfff6df / 0x8e9b87 × 1.9, MeshStandard roughness 1, tonemapping yok. three.js Lambert'i π'ye
        // böler, URP bölmez: yoğunluklar /π (güneş 0.78, ortam 0.6, doğrusal uzayda). Deniz = sayfanın arka plan rengi.
        // Kale/şehir ışığına dokunmaz: kendi renk profili ve kendi deniz materyali.
        private static readonly Color SeaColor = new Color(0.29f, 0.4f, 0.396f); // #4a6665
        private const string ProfilePath = Art + "CrownreachVolumeProfile.asset";
        private const string SeaMaterialPath = Art + "Mat_CrownreachSea.mat";

        [MenuItem("Tools/Blob.io/Crownreach Işığını Uygula")]
        public static void ApplyLookMenu()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) { Debug.LogError("[Crownreach] Önce CrownreachScene'i aç."); return; }
            ApplyLook();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[Crownreach] Işık uygulandı.");
        }

        private static Color Hemi(Color srgb, float k) => (srgb.linear * k).gamma;

        internal static void ApplyLook()
        {
            const float hemi = 1.9f / Mathf.PI;
            Color sky = new Color(1f, 0.965f, 0.875f), ground = new Color(0.557f, 0.608f, 0.529f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hemi(sky, hemi);
            RenderSettings.ambientEquatorColor = Hemi(Color.Lerp(sky.linear, ground.linear, 0.5f).gamma, hemi);
            RenderSettings.ambientGroundColor = Hemi(ground, hemi);
            RenderSettings.ambientIntensity = 1f;
            // Hafif derinlik: uzak kısımlar sayfanın arka planı gibi denizin rengine kayar.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = SeaColor;
            RenderSettings.fogDensity = 0.0025f; // oyun kamerası uzakta: daha yoğunu bütün ekranı griye boğuyordu
            DynamicGI.UpdateEnvironment();

            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.type != LightType.Directional) continue;
                light.color = new Color(1f, 0.894f, 0.702f);
                light.intensity = 2.45f / Mathf.PI;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1f; // gölgede sadece ortam ışığı kalır (three.js gibi)
                // three (x, y, z) → Unity (-x, y, z), sonra dünya 180° döndü: güneş (-48, 78, -40)'ta.
                light.transform.rotation = Quaternion.LookRotation(-new Vector3(-48f, 78f, -40f).normalized);
                break;
            }
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if (cam.clearFlags == CameraClearFlags.SolidColor) cam.backgroundColor = SeaColor;

            GameObject go = GameObject.Find("GlobalVolume");
            var volume = go != null ? go.GetComponent<UnityEngine.Rendering.Volume>() : null;
            if (volume != null && volume.sharedProfile != null)
            {
                string current = AssetDatabase.GetAssetPath(volume.sharedProfile);
                if (current != ProfilePath)
                {
                    AssetDatabase.DeleteAsset(ProfilePath);
                    AssetDatabase.CopyAsset(current, ProfilePath);
                    volume.sharedProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(ProfilePath);
                }
                var profile = volume.sharedProfile;
                if (profile.TryGet(out UnityEngine.Rendering.Universal.Tonemapping tone))
                    tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.None);
                if (profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments color))
                {
                    color.postExposure.Override(0f);
                    color.contrast.Override(0f);
                    color.saturation.Override(0f);
                }
                if (profile.TryGet(out UnityEngine.Rendering.Universal.WhiteBalance white))
                    white.temperature.Override(0f);
                if (profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
                    vignette.intensity.Override(0.25f); // sayfanın kenara doğru koyulaşan radyal arka planı
                EditorUtility.SetDirty(profile);
            }

            // Deniz: düz, soluk camgöbeği (kale/şehrin parlak mavi su dokusu yerine).
            Material sea = AssetDatabase.LoadAssetAtPath<Material>(SeaMaterialPath);
            if (sea == null)
            {
                sea = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Mat_CrownreachSea" };
                AssetDatabase.CreateAsset(sea, SeaMaterialPath);
            }
            sea.shader = Shader.Find("Universal Render Pipeline/Unlit");
            sea.SetColor("_BaseColor", SeaColor);
            EditorUtility.SetDirty(sea);
            GameObject water = GameObject.Find(RootName + "/Water");
            if (water != null)
            {
                water.GetComponent<MeshRenderer>().sharedMaterial = sea;
                var scroll = water.GetComponent<WaterScroll>();
                if (scroll != null) Object.DestroyImmediate(scroll);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- sahne ----------

        private static bool OpenScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return true;
            if (active.isDirty)
            {
                Debug.LogError($"[Crownreach] Açık sahne ({active.name}) kaydedilmemiş: önce kaydet, sonra tekrar çalıştır.");
                return false;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError("[Crownreach] CrownreachScene yok (sahne yerinde yeniden kurulur; kaynak kale sahnesi kaldırıldı).");
                return false;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static void ResetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static void EnsureGhostMaterial(Material palette)
        {
            const string cityGhost = "Assets/_Project/Resources/OcclusionGhost/Mat_CityPalette_Ghost.mat";
            string path = $"Assets/_Project/Resources/OcclusionGhost/{palette.name}_Ghost.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null && !AssetDatabase.CopyAsset(cityGhost, path)) return;
            Material ghost = AssetDatabase.LoadAssetAtPath<Material>(path);
            ghost.SetTexture("_BaseMap", palette.GetTexture("_BaseMap"));
            EditorUtility.SetDirty(ghost);
        }

        // Değirmen kanadı döner: static batching dışı, eksen = mesh'in en ince kenarı (kanat düzleminin normali).
        private const float MillSpinSpeed = 40f;

        private static void AddMillSpin(GameObject blades)
        {
            GameObjectUtility.SetStaticEditorFlags(blades, 0);
            Vector3 e = blades.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            Vector3 axis = e.x <= e.y && e.x <= e.z ? Vector3.right : e.y <= e.z ? Vector3.up : Vector3.forward;
            if (!blades.TryGetComponent(out MillSpin spin)) spin = blades.AddComponent<MillSpin>();
            spin.Setup(axis, MillSpinSpeed);
            EditorUtility.SetDirty(spin);
        }

        // Elle düzenlemeden sonra (bina/eşya taşındı): haritayı yeniden kurmadan NavMesh'i bake eder, mini haritayı çeker.
        // Yeniden kurulum (Crownreach Haritasını Kur) elle yapılan değişiklikleri siler.
        [MenuItem("Tools/Blob.io/Crownreach NavMesh ve Mini Haritayı Yenile")]
        private static void RefreshNavAndMinimap()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath) { Debug.LogError("[Crownreach] Önce CrownreachScene'i aç."); return; }
            var castleMap = Object.FindAnyObjectByType<CastleMap>(FindObjectsInactive.Include);
            if (castleMap == null) { Debug.LogError("[Crownreach] Sahnede CastleMap yok."); return; }
            CityKitPipeline.BakeNavMesh();
            MapBuildTools.RenderMinimap(castleMap.MinimapWorld, MinimapAsset);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Crownreach] NavMesh ve mini harita yenilendi.");
        }

        // Kurulmuş sahnede kanatları döndürür (haritayı yeniden kurmadan).
        [MenuItem("Tools/Blob.io/Crownreach Değirmenlerini Döndür")]
        private static void MillSpinMenu()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            int n = 0;
            foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith("MillBlades")) continue;
                AddMillSpin(mf.gameObject);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Crownreach] {n} değirmen kanadı döner");
        }

        // Zemin yüzeyleri (yol, meydan, tarla) prototipte birkaç cm yüksek; blob, yaya ve yiyecek y = 0'da yürür, gömülü
        // görünüyordu. Sıfırın üstü Flatten ile ezilir (yükseklik sırası korunur: yol çimenin üstünde). Aynı yükseklikteki
        // yüzeylerde (meydan taşı ve çimen titreşiyordu) küçük olan bir tık üstte; bu pay en küçük yükseklik farkından
        // (çimen → yol ~0.009) küçük kalır. Zemine yakın objeler de aynı oranda iner.
        private const float Flatten = 0.15f;
        private const float LayerStep = 0.0008f;    // alan yarıya indikçe bu kadar yukarı
        private const float BaseArea = 300f;
        private const float MaxFlattenBase = 0.6f;

        private static float LayerBias(Comp c) =>
            LayerStep * Mathf.Clamp(Mathf.Log(BaseArea / Mathf.Max(0.01f, c.Area), 2f), 0f, 10f);

        private static void FlattenBase(GameObject go)
        {
            Vector3 p = go.transform.position;
            if (p.y > 0f && p.y < MaxFlattenBase) go.transform.position = new Vector3(p.x, p.y * Flatten, p.z);
        }

        private static void SetInt<T>(string field, int value) where T : Object
        {
            T target = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) return;
            prop.intValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetFloat<T>(string field, float value) where T : Object
        {
            T target = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
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
            if (scenes.Exists(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
