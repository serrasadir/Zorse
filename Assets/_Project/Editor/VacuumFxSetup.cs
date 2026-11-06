using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.EditorTools
{
    // Vakum menzil efekti: zeminde dönen hafif kasırga (iki spiral rüzgar katmanı) → Resources/FX/Vacuum (VacuumComponent yükler).
    // Tekrar çalıştırmak güvenli.
    internal static class VacuumFxSetup
    {
        private const string Folder = "Assets/_Project/Resources/FX/Vacuum";
        private const int Size = 512;

        [MenuItem("Tools/Blob.io/Vakum Efektini Kur")]
        private static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Resources/FX", "Vacuum");

            // Spiral rüzgar çizgileri, iki katman (farklı kol sayısı/kıvrım; runtime'da farklı hızda döner).
            Texture2D a = WriteTexture("VacuumSwirlA", (r, th) => Swirl(r, th, 5, 1.1f, 0.10f, 0f));
            Texture2D b = WriteTexture("VacuumSwirlB", (r, th) => Swirl(r, th, 3, 0.7f, 0.07f, 2f));
            EnsureMaterial("Mat_VacuumSwirlA", a, new Color(0.8f, 0.95f, 1f, 0.45f));
            EnsureMaterial("Mat_VacuumSwirlB", b, new Color(0.8f, 0.95f, 1f, 0.32f));
            // Kara Delik evrimi: koyu spiral katmanlar (alfa karıştırma koyulaştırır).
            // Ayrı dokular, 1.5 kat çizgi (maliyet aynı: çizgiler dokuda, kart sayısı değişmez). Siyah + lacivert; halka indigo.
            Texture2D darkA = WriteTexture("VacuumSwirlDarkA", (r, th) => Swirl(r, th, 8, 1.1f, 0.09f, 0f));
            Texture2D darkB = WriteTexture("VacuumSwirlDarkB", (r, th) => Swirl(r, th, 5, 0.7f, 0.07f, 2f));
            EnsureMaterial("Mat_VacuumSwirlDarkA", darkA, new Color(0.05f, 0.03f, 0.1f, 0.85f));
            EnsureMaterial("Mat_VacuumSwirlDarkB", darkB, new Color(0.16f, 0.27f, 0.78f, 0.7f));
            // Olay ufku: siyah çekirdek (r < 0.72, 0.86'da biter) + mor yığılma halkası (r ≈ 0.82, kesik kesik yaylar).
            Texture2D core = WriteTexture("BlackHoleCore", (r, th) => 1f - Smooth(0.68f, 0.86f, r));
            Texture2D rim = WriteTexture("BlackHoleRim", (r, th) =>
            {
                float band = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.82f) / 0.07f);
                float arcs = 0.45f + 0.55f * Smooth(0.2f, 1f, (Mathf.Cos(th * 3f + r * 9f) + 1f) * 0.5f);
                float hot = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.79f) / 0.015f); // iç kenarda ince sıcak çizgi
                return Mathf.Clamp01(band * band * (3f - 2f * band) * arcs + hot * 0.8f);
            });
            Texture2D shock = WriteTexture("BlackHoleShock", (r, th) =>
            {
                float t = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.93f) / 0.07f);
                return r > 1f ? 0f : t * t * (3f - 2f * t);
            });
            EnsureMaterial("Mat_BlackHoleCore", core, new Color(0.02f, 0f, 0.04f, 0.92f));
            EnsureMaterial("Mat_BlackHoleRim", rim, new Color(0.62f, 0.42f, 1f, 1f));
            EnsureMaterial("Mat_BlackHoleShock", shock, new Color(0.55f, 0.45f, 1f, 0.9f));

            foreach (string old in new[] { "VacuumRing.png", "VacuumPulse.png", "Mat_VacuumRing.mat", "Mat_VacuumPulse.mat" })
                AssetDatabase.DeleteAsset($"{Folder}/{old}");
            AssetDatabase.SaveAssets();
            Debug.Log("[VacuumFxSetup] Vakum efekti dokuları ve materyalleri kuruldu → Resources/FX/Vacuum.");
        }

        private static float Smooth(float a, float b, float t)
        {
            t = Mathf.Clamp01((t - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // Kol: cos(kol × açı + kıvrım × 2π r) tepeleri; kol boyunca kesikli (rüzgar), merkez ve kenarda söner.
        private static float Swirl(float r, float th, int arms, float twist, float width, float seed)
        {
            float phase = th * arms + twist * r * Mathf.PI * 2f;
            float streak = Smooth(1f - width, 1f, (Mathf.Cos(phase) + 1f) * 0.5f);
            float armId = Mathf.Repeat(Mathf.Floor(phase / (Mathf.PI * 2f)), arms);
            float dash = Mathf.Clamp01(0.55f + 0.45f * Mathf.Cos(r * Mathf.PI * 2f * 2.2f + armId * 1.7f + seed));
            float env = Smooth(0.18f, 0.42f, r) * (1f - Smooth(0.78f, 1f, r));
            return streak * dash * env;
        }

        private static Texture2D WriteTexture(string name, System.Func<float, float, float> alphaAt)
        {
            string path = $"{Folder}/{name}.png";
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size * 2f - 1f, v = (y + 0.5f) / Size * 2f - 1f;
                px[y * Size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alphaAt(Mathf.Sqrt(u * u + v * v), Mathf.Atan2(v, u))));
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = Size;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureMaterial(string name, Texture2D texture, Color color)
        {
            string path = $"{Folder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }
    }
}
