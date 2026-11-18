using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.UI
{
    // K19 slot şeridi (HUD ve kart ekranı ortak): 3 aktif (daire) + 3 pasif (yuvarlak kare), alım sırasıyla.
    // Slotlar Build'de bir kez kurulur; sonra yalnız metin/renk/SetActive. Kaynak UpgradeSystem'in sıralı listeleri.
    public class SkillSlotStrip : MonoBehaviour
    {
        private const int GroupSize = 3;
        private const float Gap = 8f, GroupGap = 24f, FrameWidth = 4f;
        private const float FadeTime = 0.3f, PunchTime = 0.22f, PunchScale = 1.15f;
        private const float FrameAlpha = 0.8f, EmptyAlpha = 0.25f;

        // Tek Image ile içi boş halka çizilemiyor: çerçeve alttaki dolu şekil, zemin üstte (0.55 yerine 0.85, renk sızmasın).
        private static readonly Color Ground = new Color(0.05f, 0.05f, 0.17f, 0.85f);
        private static readonly Color PillColor = new Color(0.05f, 0.05f, 0.17f, 0.9f);
        private static readonly Color ActiveColor = new Color(1f, 0.6f, 0.3f, 1f);
        private static readonly Color PassiveColor = new Color(0.55f, 0.8f, 1f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Ink = new Color(0.12f, 0.08f, 0.02f, 1f);

        private class Slot
        {
            public RectTransform Root, Content;
            public Image Frame, Fill, Pill, Icon;
            public TMP_Text Abbr, Level;
            public CanvasGroup Group;
            public bool IsActive;
            public UpgradeData Data;
            public int ShownLevel = -1;
            public float AnimStart = -1f;
            public bool AnimIsFade;
        }

        private readonly Slot[] _slots = new Slot[GroupSize * 2];
        private bool _built, _dirty, _animating;
        // Arayüz kiti: slot sprite'ları (aktif turuncu daire, pasif mavi kare, boş gri), skill ikonu, seviye hapı.
        private bool _k;

        public float Width { get; private set; }

        public static SkillSlotStrip Create(RectTransform parent, string name, float slotSize, Sprite rounded)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var strip = go.AddComponent<SkillSlotStrip>();
            strip.Build(slotSize, rounded);
            return strip;
        }

        private void Build(float size, Sprite rounded)
        {
            _k = UiKit.Ready;
            Sprite circle = Resources.Load<Sprite>("UI/Shapes/UI_Circle");
            var rect = (RectTransform)transform;
            Width = GroupSize * 2 * size + (GroupSize - 1) * 2 * Gap + GroupGap;
            rect.sizeDelta = new Vector2(Width, size);
            rect.localScale = Vector3.one;
            float passiveStart = GroupSize * (size + Gap) - Gap + GroupGap;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool active = i < GroupSize;
                int k = active ? i : i - GroupSize;
                float x = (active ? 0f : passiveStart) + k * (size + Gap);
                _slots[i] = CreateSlot(rect, x, size, active ? (circle != null ? circle : rounded) : rounded, active);
                if (_k) KitSlot(_slots[i], size);
            }
            _built = true;
            Refresh(null);
        }

        private static Slot CreateSlot(RectTransform parent, float x, float size, Sprite shape, bool active)
        {
            var s = new Slot { IsActive = active };
            s.Root = NewRect("Slot", parent);
            s.Root.anchorMin = s.Root.anchorMax = new Vector2(0f, 0.5f);
            s.Root.pivot = new Vector2(0.5f, 0.5f);
            s.Root.anchoredPosition = new Vector2(x + size * 0.5f, 0f);
            s.Root.sizeDelta = new Vector2(size, size);

            s.Frame = AddImage(s.Root, shape);
            RectTransform fill = NewRect("Fill", s.Root);
            Stretch(fill, FrameWidth);
            s.Fill = AddImage(fill, shape);
            s.Fill.color = Ground;

            s.Content = NewRect("Content", s.Root);
            Stretch(s.Content, 0f);
            s.Group = s.Content.gameObject.AddComponent<CanvasGroup>();
            s.Group.interactable = false;
            s.Group.blocksRaycasts = false;

            bool large = size >= 90f;
            RectTransform abbr = NewRect("Abbr", s.Content);
            Stretch(abbr, 0f);
            s.Abbr = AddText(abbr, large ? 40f : 32f, Color.white);

            float pillSize = size * 0.44f;
            RectTransform pill = NewRect("Pill", s.Content);
            pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(1f, 0f);
            pill.anchoredPosition = new Vector2(pillSize * 0.15f, -pillSize * 0.15f);
            pill.sizeDelta = new Vector2(pillSize, pillSize);
            s.Pill = AddImage(pill, shape);
            RectTransform level = NewRect("Level", pill);
            Stretch(level, 0f);
            s.Level = AddText(level, large ? 34f : 28f, Color.white);
            return s;
        }

        // Kit görünümü: çerçeve slot sprite'ı (iç zemin sprite'ta), ortada ikon, sağ altta yatay hap.
        private static void KitSlot(Slot s, float size)
        {
            UiKit.Paint(s.Frame, s.IsActive ? "Slots/slot_active" : "Slots/slot_passive");
            s.Fill.enabled = false;
            s.Icon = UiKit.NewImage("Icon", s.Content, Vector2.zero, new Vector2(size * 0.66f, size * 0.66f), null);
            s.Icon.preserveAspect = true;
            s.Icon.enabled = false; // sprite'sız beyaz kare görünmesin
            s.Icon.rectTransform.SetSiblingIndex(0);
            UiKit.Text(s.Abbr, true, UiKit.Ink);

            float h = size * 0.42f;
            RectTransform pill = s.Pill.rectTransform;
            pill.sizeDelta = new Vector2(h * 1.45f, h);
            pill.anchoredPosition = new Vector2(h * 0.3f, -h * 0.2f);
            UiKit.Paint(s.Pill, "Pills/pill_gray");
            PillBorder(s.Pill, h);
            UiKit.Outlined(s.Level, Color.white);
            s.Level.fontSize = size >= 90f ? 30f : 24f;
        }

        // Hap yalnız yatay 9-slice: kenar payı yükseklikle orantılı.
        private static void PillBorder(Image img, float h)
        {
            if (img.sprite != null && h > 1f) img.pixelsPerUnitMultiplier = img.sprite.rect.height / h;
        }

        private void OnEnable()
        {
            GameEvents.OnUpgradeApplied += OnUpgradeApplied;
            GameEvents.OnCharacterSelected += OnCharacterSelected;
            Refresh(null);
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeApplied -= OnUpgradeApplied;
            GameEvents.OnCharacterSelected -= OnCharacterSelected;
            if (!_built) return;
            foreach (Slot s in _slots)
            {
                s.AnimStart = -1f;
                s.Root.localScale = Vector3.one;
                s.Group.alpha = 1f;
            }
            _animating = false;
        }

        private void OnUpgradeApplied(UpgradeData data, int level) => Refresh(data);

        // Dinleyici sırası belirsiz (UpgradeSystem'in sıfırlamasından önce çalışabilir): listeyi sonraki karede oku.
        private void OnCharacterSelected(CharacterData _) => _dirty = true;

        // Listeden yeniden kurar; changed verilirse o slot animasyonlu (yeni: fade, seviye: punch).
        public void Refresh(UpgradeData changed)
        {
            if (!_built || _slots[0] == null) return; // Play'de kod yeniden yüklenince slotlar düşer
            UpgradeSystem sys = UpgradeSystem.Instance;
            FillGroup(sys, sys != null ? sys.OwnedActives : null, 0, changed);
            FillGroup(sys, sys != null ? sys.OwnedPassives : null, GroupSize, changed);
        }

        private void FillGroup(UpgradeSystem sys, IReadOnlyList<UpgradeData> list, int offset, UpgradeData changed)
        {
            for (int k = 0; k < GroupSize; k++)
            {
                UpgradeData data = list != null && k < list.Count ? list[k] : null;
                int level = data != null && sys != null ? sys.GetLevel(data) : 0;
                Apply(_slots[offset + k], data, level, data != null && data == changed);
            }
        }

        private void Apply(Slot s, UpgradeData data, int level, bool animate)
        {
            if (s.Data == data && s.ShownLevel == level) return;
            bool wasEmpty = s.Data == null;
            s.Data = data;
            s.ShownLevel = level;
            Color kind = s.IsActive ? ActiveColor : PassiveColor;

            if (data == null)
            {
                if (_k) UiKit.Paint(s.Frame, "Slots/slot_empty", WithAlpha(Color.white, FrameAlpha));
                else
                {
                    s.Frame.color = WithAlpha(kind, EmptyAlpha);
                    s.Fill.color = WithAlpha(Ground, Ground.a * 0.5f);
                }
                s.Content.gameObject.SetActive(false);
                s.AnimStart = -1f;
                s.Root.localScale = Vector3.one;
                return;
            }

            bool max = level >= data.MaxLevel;
            s.Content.gameObject.SetActive(true);
            s.Fill.color = Ground;
            s.Abbr.SetText(data.Abbreviation);
            s.Level.SetText("{0}", level);
            s.Pill.color = max ? Gold : PillColor;
            s.Level.color = max ? Ink : Color.white;
            if (_k) KitApply(s, data, max);
            else s.Frame.color = WithAlpha(max ? Gold : kind, FrameAlpha);
            s.Group.alpha = 1f;
            if (!animate) return;
            s.AnimIsFade = wasEmpty;
            s.AnimStart = Time.unscaledTime;
            if (wasEmpty) s.Group.alpha = 0f;
            _animating = true;
        }

        // Yalnız slot değişince (her kare değil).
        private static void KitApply(Slot s, UpgradeData data, bool max)
        {
            UiKit.Paint(s.Frame, s.IsActive ? "Slots/slot_active" : "Slots/slot_passive");
            Sprite icon = UiKit.SkillIcon(data);
            s.Icon.sprite = icon;
            s.Icon.enabled = icon != null;
            s.Abbr.gameObject.SetActive(icon == null); // ikon yoksa kısaltma
            UiKit.Paint(s.Pill, max ? "Pills/pill_gold" : "Pills/pill_gray");
            PillBorder(s.Pill, s.Pill.rectTransform.sizeDelta.y);
            s.Level.color = Color.white;
        }

        private void Update()
        {
            if (_dirty) { _dirty = false; Refresh(null); }
            if (!_animating) return;
            bool any = false;
            float now = Time.unscaledTime;
            foreach (Slot s in _slots)
            {
                if (s.AnimStart < 0f) continue;
                float t = now - s.AnimStart;
                if (s.AnimIsFade)
                {
                    float k = Mathf.Clamp01(t / FadeTime);
                    s.Group.alpha = k;
                    if (k >= 1f) s.AnimStart = -1f; else any = true;
                }
                else
                {
                    float k = Mathf.Clamp01(t / PunchTime);
                    s.Root.localScale = Vector3.one * (1f + (PunchScale - 1f) * Mathf.Sin(k * Mathf.PI));
                    if (k >= 1f) { s.AnimStart = -1f; s.Root.localScale = Vector3.one; }
                    else any = true;
                }
            }
            _animating = any;
        }

        private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Image AddImage(RectTransform rect, Sprite sprite)
        {
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            bool sliced = sprite != null && sprite.border.sqrMagnitude > 0f;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = !sliced;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text AddText(RectTransform rect, float size, Color color)
        {
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
