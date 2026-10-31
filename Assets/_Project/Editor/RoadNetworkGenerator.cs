using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.EditorTools
{
    // Level01 sokaklarını tek mesh + tek doku olarak döşer (1 draw call, collider yok). Ölçüler
    // L_Ground'dan gelir: 8x8 bloklar arasında 4 birimlik yollar. Yüzey, zeminin mor asfaltı ve
    // çizgilerinin üstünde, kaldırımların altında kalır; böylece hiçbir şeyle üst üste binmez.
    public static class RoadNetworkGenerator
    {
        private const string RootName = "RoadNetwork";
        private const string AtlasPath = "Assets/_Project/Art/Roads/RoadAtlas.png";
        private const string MeshPath = "Assets/_Project/Art/Roads/RoadNetwork.asset";
        private const string MaterialPath = "Assets/_Project/Art/Roads/Mat_Road.mat";

        private static readonly float[] StreetLines = { -18f, -6f, 6f, 18f };
        private const float InnerLine = 6f;  // yalnızca iç kesişimler dörtlü kavşak, yaya geçitli
        private const float RoadHalf = 2f;
        private const float RoadY = 0.05f;   // çizgiler 0.03, kaldırımlar 0.14

        // Atlas: sol yarı düz yol, sağ yarı kavşak. Düz hücrenin sol şeridi kesik çizgisiz asfalttır.
        private const float StraightU0 = 0f;
        private const float StraightU1 = 0.5f;
        private const float CrossU0 = 0.5f;
        private const float CrossU1 = 1f;
        private const float PlainU0 = 0.03f;
        private const float PlainU1 = 0.18f;

        internal static void Generate()
        {
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (atlas == null)
            {
                Debug.LogError($"[RoadNetworkGenerator] {AtlasPath} bulunamadı.");
                return;
            }

            GameObject existing = GameObject.Find(RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Yol Ağı");
            root.AddComponent<MeshFilter>().sharedMesh = BuildMesh();

            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetOrCreateMaterial(atlas);
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            AssetDatabase.SaveAssets();
            Debug.Log($"[RoadNetworkGenerator] Yol ağı hazır: {root.GetComponent<MeshFilter>().sharedMesh.vertexCount / 4} parça.");
        }

        private static Mesh BuildMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            foreach (float x in StreetLines)
            {
                foreach (float z in StreetLines)
                {
                    bool fourWay = Mathf.Abs(x) <= InnerLine && Mathf.Abs(z) <= InnerLine;
                    AddQuad(vertices, uvs, triangles,
                        x - RoadHalf, x + RoadHalf, z - RoadHalf, z + RoadHalf,
                        fourWay ? CrossU0 : PlainU0, fourWay ? CrossU1 : PlainU1, false);
                }
            }

            foreach (float line in StreetLines)
            {
                for (int i = 0; i < StreetLines.Length - 1; i++)
                {
                    float start = StreetLines[i] + RoadHalf;
                    float end = StreetLines[i + 1] - RoadHalf;
                    AddQuad(vertices, uvs, triangles, line - RoadHalf, line + RoadHalf, start, end, StraightU0, StraightU1, false);
                    AddQuad(vertices, uvs, triangles, start, end, line - RoadHalf, line + RoadHalf, StraightU0, StraightU1, true);
                }
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = RootName };
                AssetDatabase.CreateAsset(mesh, MeshPath);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void AddQuad(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles,
            float x0, float x1, float z0, float z1, float u0, float u1, bool alongX)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(x0, RoadY, z0));
            vertices.Add(new Vector3(x0, RoadY, z1));
            vertices.Add(new Vector3(x1, RoadY, z1));
            vertices.Add(new Vector3(x1, RoadY, z0));

            if (alongX)
            {
                uvs.Add(new Vector2(u0, 0f));
                uvs.Add(new Vector2(u1, 0f));
                uvs.Add(new Vector2(u1, 1f));
                uvs.Add(new Vector2(u0, 1f));
            }
            else
            {
                uvs.Add(new Vector2(u0, 0f));
                uvs.Add(new Vector2(u0, 1f));
                uvs.Add(new Vector2(u1, 1f));
                uvs.Add(new Vector2(u1, 0f));
            }

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Material GetOrCreateMaterial(Texture2D atlas)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.SetTexture("_BaseMap", atlas);
            material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
