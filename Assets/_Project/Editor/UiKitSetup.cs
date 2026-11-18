using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BlobSurvivor.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BlobSurvivor.EditorTools
{
    // Arayüz kiti: Art/UI/Kit PNG'lerini sprite yapar (9-slice payları kit_borders.json'dan, üretimi ArtSource/UI/kit_borders.py),
    // Baloo 2 TTF'lerinden statik TMP font asset'i + konturlu materyalleri üretir (Art/UI/Fonts) ve hepsini
    // Resources/UI/UiKit asset'ine yazar (runtime UiKit.Get("Buttons/btn_green")). Tekrar çalıştırmak güvenli.
    public static class UiKitSetup
    {
        private const string KitDir = "Assets/_Project/Art/UI/Kit";
        private const string BorderJson = KitDir + "/kit_borders.json";
        private const string FontDir = "Assets/_Project/Art/UI/Fonts";
        private const string AssetPath = "Assets/_Project/Resources/UI/UiKit.asset";
        private static readonly Color Ink = new Color32(13, 13, 43, 255);

        [MenuItem("Tools/Blob.io/Arayüz Kitini Kur")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            Dictionary<string, Vector4> borders = ReadBorders();
            var entries = new List<UiKit.Entry>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { KitDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string key = Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileNameWithoutExtension(path);
                borders.TryGetValue(key, out Vector4 b);
                bool icon = key.StartsWith("Skills/") || key.StartsWith("Icons/") || key.StartsWith("IconButtons/");
                ConfigureSprite(path, b, icon);
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) entries.Add(new UiKit.Entry { Key = key, Sprite = s });
            }
            entries.Sort((x, y) => string.CompareOrdinal(x.Key, y.Key));

            TMP_FontAsset bold = MakeFont("Baloo2-ExtraBold");
            TMP_FontAsset semi = MakeFont("Baloo2-SemiBold");

            var kit = AssetDatabase.LoadAssetAtPath<UiKit>(AssetPath);
            if (kit == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                kit = ScriptableObject.CreateInstance<UiKit>();
                AssetDatabase.CreateAsset(kit, AssetPath);
            }
            kit.Sprites = entries;
            kit.Bold = bold;
            kit.Semi = semi;
            if (bold != null)
            {
                kit.BoldOutline = MakeMaterial(bold, "Baloo2-ExtraBold Outline", 0.2f, 0.9f);
                kit.BoldOutlineThick = MakeMaterial(bold, "Baloo2-ExtraBold OutlineThick", 0.3f, 1f);
            }
            if (semi != null) kit.SemiOutline = MakeMaterial(semi, "Baloo2-SemiBold Outline", 0.18f, 0.8f);
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiKitSetup] {entries.Count} sprite, font {(bold != null ? "OK" : "YOK")}/{(semi != null ? "OK" : "YOK")}.");
        }

        private static Dictionary<string, Vector4> ReadBorders()
        {
            var map = new Dictionary<string, Vector4>();
            if (!File.Exists(BorderJson)) { Debug.LogWarning("[UiKitSetup] kit_borders.json yok (ArtSource/UI/kit_borders.py)"); return map; }
            // "Kategori/ad": [sol, alt, sağ, üst, w, h]
            foreach (Match x in Regex.Matches(File.ReadAllText(BorderJson), "\"([\\w/]+)\"\\s*:\\s*\\[\\s*(\\d+),\\s*(\\d+),\\s*(\\d+),\\s*(\\d+)"))
                map[x.Groups[1].Value] = new Vector4(int.Parse(x.Groups[2].Value), int.Parse(x.Groups[3].Value),
                    int.Parse(x.Groups[4].Value), int.Parse(x.Groups[5].Value));
            return map;
        }

        private static void ConfigureSprite(string path, Vector4 border, bool compressed)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.spritePixelsPerUnit = 100f;
            imp.spriteBorder = border; // x=sol, y=alt, z=sağ, w=üst
            imp.maxTextureSize = 2048;
            imp.textureCompression = compressed ? TextureImporterCompression.Compressed : TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }

        private static string Charset()
        {
            var s = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++) s.Append(c);
            s.Append("ÇçĞğİıÖöŞşÜüÂâÎîÛû→←↑↓·…‘’“”–—•×°%");
            return s.ToString();
        }

        private static TMP_FontAsset MakeFont(string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{name}.ttf");
            if (font == null) { Debug.LogError($"[UiKitSetup] {FontDir}/{name}.ttf yok"); return null; }
            string path = $"{FontDir}/{name} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing; // atlas sabit; baştan üretmek referansları kırar

            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, 72, 10, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            fa.name = name + " SDF";
            AssetDatabase.CreateAsset(fa, path);
            Texture2D atlas = fa.atlasTextures[0];
            atlas.name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fa);
            fa.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            if (!fa.TryAddCharacters(Charset(), out string missing) && !string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[UiKitSetup] {name}: eksik karakter {missing}");
            fa.atlasPopulationMode = AtlasPopulationMode.Static; // runtime'da atlas değişmez (git kirlenmez)
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static Material MakeMaterial(TMP_FontAsset fa, string name, float outline, float shadow)
        {
            string path = $"{FontDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = mat == null;
            if (created) mat = new Material(fa.material) { name = name };
            Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (shader != null) mat.shader = shader;
            mat.SetTexture(ShaderUtilities.ID_MainTex, fa.atlasTexture);
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outline);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Ink);
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, Ink);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -shadow);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, outline * 2f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            if (created) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
