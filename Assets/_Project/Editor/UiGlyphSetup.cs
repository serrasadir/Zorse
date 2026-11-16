using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → UI Glif ve Şekillerini Kur: UI_Glyphs TMP Sprite Asset'i (ok, yukari, asagi, uyari; beyaz,
    // tint ile renk alır) ve slot şekil sprite'ları (daire, elmas, ayarlar dişlisi; Resources/UI/Shapes). Hepsi koddan çizilir.
    // TMP Settings'e dokunmaz: varsayılan sprite asset EmojiOne kalır, UI_Glyphs metinlere ayrıca bağlanır.
    // Tekrar çalıştırmak güvenli.
    public static class UiGlyphSetup
    {
        private const string GlyphFolder = "Assets/_Project/Art/UI/Glyphs";
        private const string GlyphTexturePath = GlyphFolder + "/UI_Glyphs.png";
        private const string SpriteAssetFolder = "Assets/TextMesh Pro/Resources/Sprite Assets";
        private const string SpriteAssetPath = SpriteAssetFolder + "/UI_Glyphs.asset";
        private const string ShapeFolder = "Assets/_Project/Resources/UI/Shapes";
        private const int GlyphSize = 64, ShapeSize = 128;

        private static readonly string[] GlyphNames = { "ok", "yukari", "asagi", "uyari" };

        [MenuItem("Tools/Blob.io/UI Glif ve Şekillerini Kur")]
        public static void Run()
        {
            EnsureFolder(GlyphFolder);
            EnsureFolder(SpriteAssetFolder);
            EnsureFolder(ShapeFolder);

            WriteShape("UI_Circle", Circle);
            WriteShape("UI_Diamond", Diamond);
            WriteShape("UI_Gear", Gear);

            Texture2D sheet = WriteGlyphSheet();
            TMP_SpriteAsset asset = BuildSpriteAsset(sheet);
            Verify(asset);
        }

        // ---------------------------------------------------------------- şekiller (u, v ∈ [-1, 1], y yukarı; dönen değer işaretli mesafe, içi negatif)

        private static float Circle(float u, float v) => Mathf.Sqrt(u * u + v * v) - 0.96f;

        private static float Diamond(float u, float v) =>
            Polygon(u, v, new Vector2(0f, 0.96f), new Vector2(-0.96f, 0f), new Vector2(0f, -0.96f), new Vector2(0.96f, 0f));

        // Ayarlar dişlisi: 8 diş, ortası delik.
        private static float Gear(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            float tooth = Mathf.Clamp01((Mathf.Cos(8f * Mathf.Atan2(v, u)) + 0.3f) * 4f);
            float body = r - (0.72f + 0.24f * tooth);
            return Mathf.Max(body, -(r - 0.3f));
        }

        private static float Arrow(float u, float v)
        {
            float shaft = Box(u + 0.3f, v, 0.45f, 0.16f);
            float head = Polygon(u, v, new Vector2(0.82f, 0f), new Vector2(0.05f, 0.62f), new Vector2(0.05f, -0.62f));
            return Mathf.Min(shaft, head);
        }

        private static float Up(float u, float v) =>
            Polygon(u, v, new Vector2(0f, 0.75f), new Vector2(-0.82f, -0.62f), new Vector2(0.82f, -0.62f));

        private static float Down(float u, float v) => Up(u, -v);

        private static float Warning(float u, float v)
        {
            float tri = Polygon(u, v, new Vector2(0f, 0.9f), new Vector2(-0.92f, -0.78f), new Vector2(0.92f, -0.78f));
            float bar = Box(u, v - 0.2f, 0.1f, 0.3f);
            float dot = Mathf.Sqrt(u * u + (v + 0.42f) * (v + 0.42f)) - 0.12f;
            return Mathf.Max(tri, -Mathf.Min(bar, dot)); // ünlem üçgenden oyulur
        }

        private static float Box(float u, float v, float halfW, float halfH) =>
            Mathf.Max(Mathf.Abs(u) - halfW, Mathf.Abs(v) - halfH);

        // Dışbükey çokgen, köşeler saat yönünün tersine. İçeride tam, dışarıda yaklaşık mesafe (kenar yumuşatmaya yeter).
        private static float Polygon(float u, float v, params Vector2[] p)
        {
            float d = float.MinValue;
            for (int i = 0; i < p.Length; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % p.Length];
                Vector2 e = (b - a).normalized;
                Vector2 outward = new Vector2(e.y, -e.x);
                d = Mathf.Max(d, Vector2.Dot(new Vector2(u, v) - a, outward));
            }
            return d;
        }

        private static float Coverage(float distance, float pixel) => Mathf.Clamp01(0.5f - distance / pixel);

        // ---------------------------------------------------------------- dokular

        private static void WriteShape(string name, System.Func<float, float, float> sdf)
        {
            var px = new Color[ShapeSize * ShapeSize];
            float pixel = 2f / ShapeSize;
            for (int y = 0; y < ShapeSize; y++)
            for (int x = 0; x < ShapeSize; x++)
            {
                float u = (x + 0.5f) / ShapeSize * 2f - 1f, v = (y + 0.5f) / ShapeSize * 2f - 1f;
                px[y * ShapeSize + x] = new Color(1f, 1f, 1f, Coverage(sdf(u, v), pixel));
            }
            string path = $"{ShapeFolder}/{name}.png";
            WritePng(path, ShapeSize, ShapeSize, px);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            ApplyCommon(importer, ShapeSize);
        }

        private static Texture2D WriteGlyphSheet()
        {
            System.Func<float, float, float>[] shapes = { Arrow, Up, Down, Warning };
            int width = GlyphSize * shapes.Length;
            var px = new Color[width * GlyphSize];
            const float inset = 0.88f; // hücre kenarında boşluk (bilinear taşmasın)
            float pixel = 2f / GlyphSize / inset;
            for (int g = 0; g < shapes.Length; g++)
            for (int y = 0; y < GlyphSize; y++)
            for (int x = 0; x < GlyphSize; x++)
            {
                float u = ((x + 0.5f) / GlyphSize * 2f - 1f) / inset, v = ((y + 0.5f) / GlyphSize * 2f - 1f) / inset;
                px[y * width + g * GlyphSize + x] = new Color(1f, 1f, 1f, Coverage(shapes[g](u, v), pixel));
            }
            WritePng(GlyphTexturePath, width, GlyphSize, px);

            var importer = (TextureImporter)AssetImporter.GetAtPath(GlyphTexturePath);
            importer.textureType = TextureImporterType.Default;
            ApplyCommon(importer, width);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GlyphTexturePath);
        }

        private static void WritePng(string path, int width, int height, Color[] px)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        private static void ApplyCommon(TextureImporter importer, int maxSize)
        {
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.NextPowerOfTwo(maxSize);
            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------- TMP Sprite Asset

        private static TMP_SpriteAsset BuildSpriteAsset(Texture2D sheet)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(asset, SpriteAssetPath);
            }

            // Sürüm boşken TMP eski formattan "yükseltme" yapıp tabloları siler: önce sürüm yazılır (setter internal).
            var so = new SerializedObject(asset);
            SerializedProperty version = so.FindProperty("m_Version");
            if (version != null) { version.stringValue = "1.1.0"; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning("[UiGlyphSetup] m_Version alanı bulunamadı; TMP sürümü değişmiş olabilir.");

            if (asset.spriteInfoList == null) asset.spriteInfoList = new List<TMP_Sprite>();
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);
            asset.spriteSheet = sheet;

            List<TMP_SpriteGlyph> glyphs = asset.spriteGlyphTable;
            List<TMP_SpriteCharacter> characters = asset.spriteCharacterTable;
            glyphs.Clear();
            characters.Clear();
            for (int i = 0; i < GlyphNames.Length; i++)
            {
                // Taban çizgisinin biraz altına iner, yüksekliği fontun üst çizgisine ölçeklenir (faceInfo boş).
                var glyph = new TMP_SpriteGlyph((uint)i,
                    new GlyphMetrics(GlyphSize, GlyphSize, 0f, GlyphSize * 0.88f, GlyphSize),
                    new GlyphRect(i * GlyphSize, 0, GlyphSize, GlyphSize), 1f, 0);
                glyphs.Add(glyph);
                characters.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = GlyphNames[i] });
            }

            Material material = asset.material;
            if (material == null)
            {
                Shader shader = Shader.Find("TextMeshPro/Sprite");
                if (shader == null) { Debug.LogError("[UiGlyphSetup] TextMeshPro/Sprite shader'ı yok."); return asset; }
                material = new Material(shader) { name = "UI_Glyphs Material" };
                AssetDatabase.AddObjectToAsset(material, asset);
                asset.material = material;
            }
            material.SetTexture("_MainTex", sheet);
            EditorUtility.SetDirty(material);

            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SpriteAssetPath);
            return AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath);
        }

        // Sprite asset'i doğrudan atanmış geçici bir metinde <sprite name="ok"> gerçekten sprite olarak çözülüyor mu.
        private static void Verify(TMP_SpriteAsset asset)
        {
            if (asset == null) { Debug.LogError("[UiGlyphSetup] UI_Glyphs oluşturulamadı."); return; }
            foreach (string n in GlyphNames)
                if (asset.GetSpriteIndexFromName(n) < 0) { Debug.LogError($"[UiGlyphSetup] '{n}' sprite'ı tabloda yok."); return; }

            var go = new GameObject("UiGlyphTest") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var text = go.AddComponent<TextMeshPro>();
                text.spriteAsset = asset;
                text.text = "A<sprite name=\"ok\" tint=1>B";
                text.ForceMeshUpdate();
                TMP_TextInfo info = text.textInfo;
                bool ok = info.characterCount >= 3 && info.characterInfo[1].elementType == TMP_TextElementType.Sprite;
                if (ok) Debug.Log($"[UiGlyphSetup] OK: UI_Glyphs ({GlyphNames.Length} sprite) + daire/elmas şekilleri hazır. Test metni sprite'ı çözdü.");
                else Debug.LogError($"[UiGlyphSetup] Test metni sprite'ı çözmedi (karakter: {info.characterCount}).");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
