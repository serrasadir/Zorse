using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // Arayüz kiti: Art/UI/Kit sprite'ları (ChatGPT sheet'lerinden kesildi, kaynak ArtSource/UI) + Baloo 2 fontu.
    // Resources/UI/UiKit asset'i (Tools → Blob.io → Arayüz Kitini Kur) anahtar → sprite listesini tutar:
    // "Buttons/btn_green", "Skills/vacuum", "Glyphs/pause" … Asset yoksa Ready false, ekranlar eski görünümde kalır.
    [CreateAssetMenu(fileName = "UiKit", menuName = "BlobSurvivor/UI Kit")]
    public class UiKit : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Key;
            public Sprite Sprite;
        }

        public const string ResourcePath = "UI/UiKit";

        public bool Enabled = true;
        public List<Entry> Sprites = new List<Entry>();
        public TMP_FontAsset Bold;           // Baloo 2 ExtraBold
        public TMP_FontAsset Semi;           // Baloo 2 SemiBold
        public Material BoldOutline;         // beyaz yazı + koyu kontur + alt gölge
        public Material BoldOutlineThick;
        public Material SemiOutline;

        // Kitin kontur rengi ve ana tonları (sheet'lerden).
        public static readonly Color Ink = new Color32(13, 13, 43, 255);
        public static readonly Color Cream = new Color32(255, 243, 222, 255);
        public static readonly Color InkSoft = new Color32(92, 70, 52, 255);
        public static readonly Color Gold = new Color32(255, 200, 20, 255);
        public static readonly Color Orange = new Color32(255, 140, 30, 255);
        public static readonly Color Blue = new Color32(70, 165, 255, 255);
        public static readonly Color Green = new Color32(80, 205, 80, 255);
        public static readonly Color Purple = new Color32(170, 90, 240, 255);
        public static readonly Color Red = new Color32(240, 80, 80, 255);

        private static UiKit _current;
        private static bool _loaded;
        private Dictionary<string, Sprite> _map;

        public static UiKit Current
        {
            get
            {
                if (!_loaded) { _loaded = true; _current = Resources.Load<UiKit>(ResourcePath); }
                return _current != null && _current.Enabled ? _current : null;
            }
        }

        public static bool Ready => Current != null;

        public static Sprite Get(string key)
        {
            UiKit kit = Current;
            if (kit == null || string.IsNullOrEmpty(key)) return null;
            if (kit._map == null)
            {
                kit._map = new Dictionary<string, Sprite>(kit.Sprites.Count);
                foreach (Entry e in kit.Sprites) if (e.Sprite != null) kit._map[e.Key] = e.Sprite;
            }
            return kit._map.TryGetValue(key, out Sprite s) ? s : null;
        }

        // ---------------------------------------------------------------- yardımcılar

        public static Image Paint(Image img, string key, Color? tint = null)
        {
            if (img == null) return null;
            Sprite s = Get(key);
            if (s == null) return img;
            img.sprite = s;
            bool sliced = s.border.sqrMagnitude > 0f;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = !sliced;
            img.color = tint ?? Color.white;
            return img;
        }

        public static Image NewImage(string name, Transform parent, Vector2 pos, Vector2 size, string key, float borderScale = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            Paint(img, key);
            img.pixelsPerUnitMultiplier = borderScale;
            return img;
        }

        // Koyu yazı (krem zemin üstü).
        public static void Text(TMP_Text t, bool bold, Color color)
        {
            UiKit kit = Current;
            if (t == null || kit == null) return;
            TMP_FontAsset f = bold ? kit.Bold : kit.Semi;
            if (f != null) { t.font = f; t.fontSharedMaterial = f.material; }
            t.fontStyle = FontStyles.Normal;
            t.color = color;
        }

        // Açık renk yazı + koyu kontur + alt gölge (renkli zemin, oyun üstü).
        public static void Outlined(TMP_Text t, Color color, bool thick = false, bool bold = true)
        {
            UiKit kit = Current;
            if (t == null || kit == null) return;
            TMP_FontAsset f = bold ? kit.Bold : kit.Semi;
            Material m = bold ? (thick ? kit.BoldOutlineThick : kit.BoldOutline) : kit.SemiOutline;
            if (f != null) t.font = f;
            if (m != null) t.fontSharedMaterial = m;
            t.fontStyle = FontStyles.Normal;
            t.color = color;
        }

        // Yalnız yatay uzayan parçalar (hap, rozet, bar, kurdele): kenar payı yükseklikle orantılı küçülsün, uçlar ezilmesin.
        // Image'ın boyutu verildikten sonra çağır.
        public static void FitBorder(Image img)
        {
            if (img == null || img.sprite == null) return;
            if (img.sprite.border.y != 0f || img.sprite.border.w != 0f) return;
            float h = img.rectTransform.rect.height > 1f ? img.rectTransform.rect.height : img.rectTransform.sizeDelta.y;
            if (h > 1f) img.pixelsPerUnitMultiplier = img.sprite.rect.height / h;
            // Gerçek boy yerleşimden sonra belli olur (çapalı barlar): boyut değişince yeniden ölçeklensin.
            KitSliceFit fit = img.GetComponent<KitSliceFit>();
            if (fit == null) fit = img.gameObject.AddComponent<KitSliceFit>(); // kurulumda bir kez
            fit.Fit();
        }

        // Kitin renkli butonu: color = "gold" | "green" | "red" | "blue" | "gray". Basılı hali sprite değişimiyle
        // (btn_<renk>_pressed); renk geçişi kapalı. Buton yazısını ayrıca Outlined ile beyaz yap.
        public static void StyleButton(Button button, string color)
        {
            if (button == null || Current == null) return;
            Image img = button.targetGraphic as Image;
            if (img == null) img = button.GetComponent<Image>();
            if (img == null) return;
            Paint(img, "Buttons/btn_" + color);
            img.raycastTarget = true;
            Sprite pressed = Get("Buttons/btn_" + color + "_pressed");
            if (pressed != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                SpriteState st = button.spriteState;
                st.pressedSprite = pressed;
                st.highlightedSprite = null;
                st.selectedSprite = null;
                st.disabledSprite = null;
                button.spriteState = st;
            }
        }

        // Kutulu ikon butonu (IconButtons/pause, settings, back …): sprite olduğu gibi, basılınca hafif koyulaşır.
        public static void StyleIconButton(Button button, string iconKey)
        {
            if (button == null || Current == null) return;
            Image img = button.targetGraphic as Image;
            if (img == null) img = button.GetComponent<Image>();
            if (img == null) return;
            Paint(img, "IconButtons/" + iconKey);
            img.raycastTarget = true;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock c = button.colors;
            c.normalColor = Color.white; c.highlightedColor = Color.white; c.selectedColor = Color.white;
            c.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f); c.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            button.colors = c;
        }

        // Skill → Skills/<ikon> anahtarı (asset adına göre). Yoksa null (baş harf gösterilir).
        public static string SkillIconKey(UpgradeData data)
        {
            if (data == null) return null;
            switch (data.name)
            {
                case "Upgrade_Weapon": return "Skills/stick";
                case "Upgrade_Firework": return "Skills/firework";
                case "Upgrade_Vacuum": return "Skills/vacuum";
                case "Upgrade_KayganZemin": return "Skills/slippery";
                case "Upgrade_YerSarsintisi": return "Skills/quake";
                case "Upgrade_SindirimGazi": return "Skills/gas";
                case "Upgrade_Armor": return "Skills/armor";
                case "Upgrade_Sindirim": return "Skills/digestion";
                case "Upgrade_GeriDonusum": return "Skills/recycling";
                case "Upgrade_HizliMetabolizma": return "Skills/metabolism";
                case "Upgrade_SpeedBoost": return "Skills/speed";
                case "Upgrade_Tetikci": return "Skills/trigger";
                case "Upgrade_MaxHealth": return "Skills/max_health";
                case "Upgrade_Regen": return "Skills/regen";
                case "Upgrade_YaraOfkesi": return "Skills/wound_rage";
                case "Upgrade_Bolunme": return "Skills/split";
                case "Upgrade_KaraDelik": return "Skills/black_hole";
                default: return null;
            }
        }

        public static Sprite SkillIcon(UpgradeData data) => data != null && data.Icon != null ? data.Icon : Get(SkillIconKey(data));
    }
}
