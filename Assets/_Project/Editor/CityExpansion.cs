using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Büyük şehir (GDD_v2 Karar 15: sınırlı, ~150x150 harita). Level01'in 3x3 bloğu "mahalle şablonu"
    // olarak kullanılır: ortadaki 3x3 Level01'in birebir aynısı kalır, dış bloklar şablonlardan
    // sabit seed ile doldurulur. Zemin L_Ground yerine burada üretilir (L_Ground tek parça 40x40,
    // büyütülemez); aynı palet hücreleri ve aynı yükseklikler kullanılır.
    internal static class CityExpansion
    {
        internal const int BlockCount = 11;   // tek sayı: Level01 ortada kalsın. 11 -> 136x136, 13 -> 160x160
        internal const float BlockPitch = 12f; // blok merkezleri arası (8 blok + 4 yol)
        internal const float BlockHalf = 4f;   // 8x8 kaldırım adası
        internal const float RoadHalf = 2f;
        internal const float HalfExtent = BlockCount * BlockPitch / 2f + RoadHalf; // dış çevre yolu dahil
        private const int Seed = 1701;          // aynı seed = her kurulumda aynı şehir
        private const float ParkChance = 0.12f;
        private const int OriginalRadius = 1;   // ortadaki 3x3 = Level01

        // L_Ground ölçüleri (Blender'dan okundu): asfalt y=0, kaldırım y=0.12, taban 1 birim kalın.
        private const float SidewalkY = 0.12f;
        private const float SlabBottomY = -1f;
        private static readonly Vector2 AsphaltUV = new Vector2(0.0625f, 0.1f);
        private static readonly Vector2 SlabSideUV = new Vector2(0.1875f, 0.1f);
        private static readonly Vector2 SidewalkUV = new Vector2(0.4375f, 0.1f);
        private static readonly Vector2 CurbUV = new Vector2(0.5625f, 0.1f);
        private const string GroundMeshPath = CityKitPipeline.ArtRoot + "/CityGround_Large.asset";

        private class BlockTemplate
        {
            public bool IsPark;
            public readonly List<CityKitPipeline.CityItem> Items = new List<CityKitPipeline.CityItem>(); // blok merkezine göre
        }

        internal static float[] StreetLines()
        {
            float[] lines = new float[BlockCount + 1];
            for (int i = 0; i <= BlockCount; i++)
                lines[i] = (i - BlockCount / 2f) * BlockPitch;
            return lines;
        }

        // Araçlar (V_) ve zemin (L_Ground) şablona girmez; L_Plaza park bloklarına eklenir.
        internal static List<CityKitPipeline.CityItem> Compose(CityKitPipeline.CityItem[] level01)
        {
            Dictionary<Vector2Int, BlockTemplate> templates = ExtractTemplates(level01, out CityKitPipeline.CityItem plaza);
            List<BlockTemplate> buildingTemplates = new List<BlockTemplate>();
            BlockTemplate park = null;
            foreach (BlockTemplate t in templates.Values)
            {
                if (t.IsPark) park = t;
                else buildingTemplates.Add(t);
            }

            System.Random random = new System.Random(Seed);
            List<CityKitPipeline.CityItem> result = new List<CityKitPipeline.CityItem>();
            int half = BlockCount / 2;
            BlockTemplate[,] chosen = new BlockTemplate[BlockCount, BlockCount];

            for (int gz = -half; gz <= half; gz++)
            {
                for (int gx = -half; gx <= half; gx++)
                {
                    Vector3 center = new Vector3(gx * BlockPitch, 0f, gz * BlockPitch);
                    bool original = Mathf.Abs(gx) <= OriginalRadius && Mathf.Abs(gz) <= OriginalRadius;

                    BlockTemplate template;
                    bool mirror = false;
                    if (original)
                    {
                        template = templates[new Vector2Int(gx, gz)];
                    }
                    else
                    {
                        BlockTemplate left = gx > -half ? chosen[gx - 1 + half, gz + half] : null;
                        BlockTemplate below = gz > -half ? chosen[gx + half, gz - 1 + half] : null;
                        template = PickTemplate(random, buildingTemplates, park, left, below);
                        mirror = random.NextDouble() < 0.5;
                    }
                    chosen[gx + half, gz + half] = template;

                    foreach (CityKitPipeline.CityItem local in template.Items)
                        result.Add(Place(local, center, mirror));

                    if (template.IsPark && plaza != null)
                        result.Add(Place(plaza, center, false));
                }
            }

            return result;
        }

        // Komşu blokla aynı şablon gelmesin: tekrar hissini en çok yan yana ikizler verir.
        private static BlockTemplate PickTemplate(System.Random random, List<BlockTemplate> buildings, BlockTemplate park,
            BlockTemplate left, BlockTemplate below)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                BlockTemplate candidate = park != null && random.NextDouble() < ParkChance
                    ? park
                    : buildings[random.Next(buildings.Count)];
                if (candidate != left && candidate != below) return candidate;
            }
            return buildings[random.Next(buildings.Count)];
        }

        private static CityKitPipeline.CityItem Place(CityKitPipeline.CityItem local, Vector3 center, bool mirror)
        {
            // Binalar hep yaw=0 (ön cephe kameraya bakıyor): döndürmek yerine sadece X aynalanır.
            return new CityKitPipeline.CityItem
            {
                mesh = local.mesh,
                x = center.x + (mirror ? -local.x : local.x),
                y = local.y,
                z = center.z + local.z,
                yaw = mirror ? -local.yaw : local.yaw,
                sx = local.sx,
                sy = local.sy,
                sz = local.sz,
            };
        }

        private static Dictionary<Vector2Int, BlockTemplate> ExtractTemplates(CityKitPipeline.CityItem[] items, out CityKitPipeline.CityItem plaza)
        {
            Dictionary<Vector2Int, BlockTemplate> templates = new Dictionary<Vector2Int, BlockTemplate>();
            for (int gz = -OriginalRadius; gz <= OriginalRadius; gz++)
                for (int gx = -OriginalRadius; gx <= OriginalRadius; gx++)
                    templates[new Vector2Int(gx, gz)] = new BlockTemplate();

            plaza = null;
            foreach (CityKitPipeline.CityItem item in items)
            {
                if (item.mesh.StartsWith("V_") || item.mesh == "L_Ground") continue;

                // Köşedeki hidrant/trafik ışığı kaldırım dışına taşabiliyor: en yakın bloğa yazılır.
                int gx = Mathf.Clamp(Mathf.RoundToInt(item.x / BlockPitch), -OriginalRadius, OriginalRadius);
                int gz = Mathf.Clamp(Mathf.RoundToInt(item.z / BlockPitch), -OriginalRadius, OriginalRadius);

                CityKitPipeline.CityItem local = new CityKitPipeline.CityItem
                {
                    mesh = item.mesh,
                    x = item.x - gx * BlockPitch,
                    y = item.y,
                    z = item.z - gz * BlockPitch,
                    yaw = item.yaw,
                    sx = item.sx,
                    sy = item.sy,
                    sz = item.sz,
                };

                if (item.mesh == "L_Plaza")
                {
                    plaza = local;
                    continue;
                }
                templates[new Vector2Int(gx, gz)].Items.Add(local);
            }

            foreach (BlockTemplate t in templates.Values)
                t.IsPark = !t.Items.Exists(i => i.mesh.StartsWith("B_"));
            return templates;
        }

        // Asfalt taban + her blok için kaldırım adası (üst yüz + bordür). Tek mesh, tek materyal, collider yok:
        // fizik zemini sahnedeki 'Ground' plane'i, NavMesh render mesh'lerden bake edilir.
        internal static GameObject BuildGround(Transform parent, Material palette)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            float e = HalfExtent;

            AddFace(vertices, uvs, triangles, AsphaltUV, Vector3.up,
                new Vector3(-e, 0f, -e), new Vector3(-e, 0f, e), new Vector3(e, 0f, e), new Vector3(e, 0f, -e));
            AddBoxSides(vertices, uvs, triangles, SlabSideUV, -e, e, -e, e, SlabBottomY, 0f);

            int half = BlockCount / 2;
            for (int gz = -half; gz <= half; gz++)
            {
                for (int gx = -half; gx <= half; gx++)
                {
                    float x0 = gx * BlockPitch - BlockHalf, x1 = gx * BlockPitch + BlockHalf;
                    float z0 = gz * BlockPitch - BlockHalf, z1 = gz * BlockPitch + BlockHalf;
                    AddFace(vertices, uvs, triangles, SidewalkUV, Vector3.up,
                        new Vector3(x0, SidewalkY, z0), new Vector3(x0, SidewalkY, z1),
                        new Vector3(x1, SidewalkY, z1), new Vector3(x1, SidewalkY, z0));
                    AddBoxSides(vertices, uvs, triangles, CurbUV, x0, x1, z0, z1, 0f, SidewalkY);
                }
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GroundMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "CityGround_Large" };
                AssetDatabase.CreateAsset(mesh, GroundMeshPath);
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            GameObject ground = new GameObject("CityGround");
            ground.transform.SetParent(parent, false);
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial = palette;
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);
            return ground;
        }

        // Harita sınırı (Karar 15): dış çevre yolunun kenarında görünmez duvar. Environment layer'ı
        // Blob ve Enemy ile çarpışır (DynamicsManager), polis/yaya engel raycast'leri de görür.
        internal static void BuildBounds(Transform parent)
        {
            const float thickness = 2f;
            const float height = 30f;
            float e = HalfExtent;
            float length = 2f * (e + thickness);

            GameObject bounds = new GameObject("CityBounds") { layer = LayerMask.NameToLayer("Environment") };
            bounds.transform.SetParent(parent, false);

            AddWall(bounds, new Vector3(0f, height / 2f, e + thickness / 2f), new Vector3(length, height, thickness));
            AddWall(bounds, new Vector3(0f, height / 2f, -e - thickness / 2f), new Vector3(length, height, thickness));
            AddWall(bounds, new Vector3(e + thickness / 2f, height / 2f, 0f), new Vector3(thickness, height, length));
            AddWall(bounds, new Vector3(-e - thickness / 2f, height / 2f, 0f), new Vector3(thickness, height, length));
        }

        private static void AddWall(GameObject owner, Vector3 center, Vector3 size)
        {
            BoxCollider box = owner.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        private static void AddBoxSides(List<Vector3> v, List<Vector2> uv, List<int> t, Vector2 cell,
            float x0, float x1, float z0, float z1, float y0, float y1)
        {
            AddFace(v, uv, t, cell, Vector3.back,
                new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0));
            AddFace(v, uv, t, cell, Vector3.forward,
                new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1));
            AddFace(v, uv, t, cell, Vector3.left,
                new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1));
            AddFace(v, uv, t, cell, Vector3.right,
                new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1));
        }

        // Palet hücresi düz renk: tüm köşeler aynı UV. Sarım yönü istenen normale göre seçilir.
        private static void AddFace(List<Vector3> v, List<Vector2> uv, List<int> t, Vector2 cell, Vector3 normal,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int i = 0; i < 4; i++) uv.Add(cell);

            bool frontFacing = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f;
            if (frontFacing)
            {
                t.Add(start); t.Add(start + 1); t.Add(start + 2);
                t.Add(start); t.Add(start + 2); t.Add(start + 3);
            }
            else
            {
                t.Add(start); t.Add(start + 2); t.Add(start + 1);
                t.Add(start); t.Add(start + 3); t.Add(start + 2);
            }
        }
    }
}
