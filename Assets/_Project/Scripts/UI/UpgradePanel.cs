using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.UI
{
    // Seviye atlama (3 karttan 1). Üstte slot şeridi + AKTİF/PASİF sayacı; kartta ad, sağ üstte rozet (YENİ / Sv. a → b / MAX / EVRİM),
    // altında tür (şekil + AKTİF/PASİF/NADİR), açıklama ve slot notu. Kartlar sırayla belirir; ilk InputDelay boyunca dokunmaya kapalı.
    // Seçim (K24, kart spec §10): 150 ms onay animasyonu, sonra olay; kuyruk varsa panel kapanmaz. Timescale 0: ölçeksiz zaman.
    // Düzen çalışma zamanında bir kez kurulur (sahnedeki buton/yazılar yeniden biçimlenir).
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button[] _upgradeButtons;
        [SerializeField] private TMP_Text[] _upgradeNameTexts;
        [SerializeField] private TMP_Text[] _upgradeDescTexts;

        private static readonly Color Backdrop = new Color(0.05f, 0.05f, 0.17f, 0.72f);
        // Kompakt düzen (ref px, panel merkezine göre): karartma ekranın üstünden HudClearance aşağıda başlar.
        private const float HudClearance = 470f;
        private const float CardTopY = 150f, CardHeight = 270f, CardGap = 16f;
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.88f);
        private static readonly Color EvolutionCard = new Color(0.24f, 0.17f, 0.03f, 0.92f);
        private static readonly Color RareCard = new Color(0.26f, 0.06f, 0.16f, 0.92f);
        private static readonly Color ActiveTag = new Color(1f, 0.6f, 0.3f, 1f);
        private static readonly Color PassiveTag = new Color(0.55f, 0.8f, 1f, 1f);
        private static readonly Color RareTag = new Color(1f, 0.45f, 0.65f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Ink = new Color(0.12f, 0.08f, 0.02f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color NewBadge = new Color(0.35f, 0.75f, 0.4f, 1f);
        private static readonly Color LevelBadge = new Color(0.15f, 0.38f, 0.85f, 1f); // beyaz yazıyla ~5.5:1
        private static readonly Color NoteWarn = new Color(1f, 0.6f, 0.3f, 1f);
        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);

        private const float InputDelay = 0.4f;
        private const float Stagger = 0.07f, PopTime = 0.22f;
        private const float ConfirmTime = 0.15f, ConfirmPunch = 0.12f, FadeOutTime = 0.15f;

        private UpgradeData[] _currentChoices;
        private bool _built;
        private Image[] _cardImages;
        private RectTransform[] _cards;
        private CanvasGroup[] _cardGroups;
        private Image[] _badges;
        private TMP_Text[] _badgeTexts;
        private TMP_Text[] _kindTexts;
        private Image[] _kindShapes;
        private TMP_Text[] _noteTexts;
        private Outline[] _outlines;
        private TMP_Text _subtitle, _activeCount, _passiveCount;
        private CanvasGroup _panelGroup;
        private Sprite _rounded, _circle, _diamond;
        private Entities.Blob.BlobGrowth _growth;
        private float _shownAt = -1f;

        // Seçim durumu: _selected >= 0 iken onay animasyonu; _fadeAt >= 0 iken panel sönüyor.
        private int _selected = -1;
        private float _selectedAt = -1f, _fadeAt = -1f;
        private bool _fired, _reshown;

        private void OnEnable()
        {
            GameEvents.OnUpgradeChoicesReady += ShowChoices;
            GameEvents.OnGameOver += Hide;
            GameEvents.OnUpgradeApplied += OnUpgradeApplied;
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeChoicesReady -= ShowChoices;
            GameEvents.OnGameOver -= Hide;
            GameEvents.OnUpgradeApplied -= OnUpgradeApplied;
        }

        private void Start()
        {
            if (_panel != null) _panel.SetActive(false);

            for (int i = 0; i < _upgradeButtons.Length; i++)
            {
                int index = i;
                _upgradeButtons[i].onClick.AddListener(() => SelectUpgrade(index));
            }
        }

        private void ShowChoices(UpgradeData[] choices)
        {
            Build();
            // Kuyruk: seçim olayının içinde, panel açıkken yeniden dolar (kapanmaz).
            bool again = _panel != null && _panel.activeSelf && _selected >= 0;
            _reshown = true;
            ResetSelection();
            _currentChoices = choices;
            if (_panel != null) _panel.SetActive(true);
            if (_panelGroup != null) { _panelGroup.alpha = 1f; _panelGroup.blocksRaycasts = true; }

            int level = _growth != null ? _growth.CurrentLevel : 0;
            if (_subtitle != null)
            {
                if (level > 0) _subtitle.SetText(again ? "Sv. {0} · Bir güç daha seç" : "Sv. {0} · Bir güç seç", level);
                else _subtitle.SetText(again ? "Bir güç daha seç" : "Bir güç seç");
            }
            UpdateCounters();

            UpgradeSystem sys = UpgradeSystem.Instance;
            for (int i = 0; i < _upgradeButtons.Length; i++)
            {
                bool hasChoice = i < choices.Length && choices[i] != null;
                _upgradeButtons[i].gameObject.SetActive(hasChoice);
                if (!hasChoice) continue;

                UpgradeData data = choices[i];
                bool evo = data.IsEvolution;
                bool rare = data.Kind == SkillKind.Rare;
                bool active = data.Kind == SkillKind.Active;
                int current = sys != null ? sys.GetLevel(data) : 0;

                if (_upgradeNameTexts != null && i < _upgradeNameTexts.Length && _upgradeNameTexts[i] != null)
                {
                    // B17: evrim çıktısı kartları ★ + altın renkle işaretlenir.
                    _upgradeNameTexts[i].text = evo ? $"★ {data.DisplayName}" : data.DisplayName;
                    _upgradeNameTexts[i].color = evo ? Gold : Color.white;
                }
                if (_upgradeDescTexts != null && i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                    _upgradeDescTexts[i].text = data.GetDescriptionForLevel(current + 1);

                if (_cardImages[i] != null) _cardImages[i].color = evo ? EvolutionCard : rare ? RareCard : CardColor;

                // Tür: K23 şekli + metin (evrimde yok).
                Color kindColor = rare ? RareTag : active ? ActiveTag : PassiveTag;
                _kindTexts[i].text = evo ? "" : rare ? "NADİR" : active ? "AKTİF" : "PASİF";
                _kindTexts[i].color = kindColor;
                _kindShapes[i].gameObject.SetActive(!evo);
                SetShape(_kindShapes[i], rare ? _diamond : active ? _circle : _rounded);
                _kindShapes[i].color = kindColor;

                // Rozet: kazanılan seviye görünür (K4). Sopa herkeste baştan var: Sv.0'da "Sv. 0 → 1".
                bool isNew = rare || (current == 0 && !data.IsBaseWeapon);
                int next = current + 1;
                bool toMax = !evo && !isNew && next >= data.MaxLevel;
                TMP_Text badge = _badgeTexts[i];
                if (evo) badge.SetText("EVRİM");
                else if (isNew) badge.SetText("YENİ");
                else if (toMax) badge.SetText("Sv. {0} <sprite name=\"ok\" tint=1> MAX", current);
                else badge.SetText("Sv. {0} <sprite name=\"ok\" tint=1> {1}", current, next);
                _badges[i].color = evo || toMax ? Gold : isNew ? NewBadge : LevelBadge;
                badge.color = evo || toMax || isNew ? Ink : Color.white;

                string note = NoteFor(sys, data, current, evo, rare, out bool warn);
                _noteTexts[i].gameObject.SetActive(note != null);
                if (note != null)
                {
                    _noteTexts[i].SetText(note);
                    _noteTexts[i].color = warn ? NoteWarn : Subtle;
                }

                _upgradeButtons[i].interactable = false; // InputDelay sonra açılır
            }

            _shownAt = Time.unscaledTime;
            Animate(0f);
        }

        // Slot notu (kart spec §6): son boş slot uyarısı, Sopa Sv.0, nadir ve evrim slot harcamaz.
        private static string NoteFor(UpgradeSystem sys, UpgradeData data, int current, bool evo, bool rare, out bool warn)
        {
            warn = false;
            if (evo) return "Slot harcamaz";
            if (rare) return "Tek kullanımlık · Slot harcamaz";
            if (current > 0 || sys == null || !data.UsesSlot) return null;
            bool active = data.Kind == SkillKind.Active;
            int owned = active ? sys.OwnedActives.Count : sys.OwnedPassives.Count;
            int slots = active ? sys.ActiveSlots : sys.PassiveSlots;
            if (owned == slots - 1)
            {
                warn = true;
                return string.Format(active ? "<sprite name=\"uyari\" tint=1> Son aktif slot ({0}/{0})"
                    : "<sprite name=\"uyari\" tint=1> Son pasif slot ({0}/{0})", slots);
            }
            return data.IsBaseWeapon ? "Aktif slot kullanır" : null;
        }

        private void OnUpgradeApplied(UpgradeData _, int __) => UpdateCounters();

        private void UpdateCounters()
        {
            UpgradeSystem sys = UpgradeSystem.Instance;
            if (_activeCount == null || sys == null) return;
            _activeCount.SetText("AKTİF {0}/{1}", sys.OwnedActives.Count, sys.ActiveSlots);
            _passiveCount.SetText("PASİF {0}/{1}", sys.OwnedPassives.Count, sys.PassiveSlots);
        }

        private void Update()
        {
            if (_panel == null || !_panel.activeSelf) return;
            float now = Time.unscaledTime;
            if (_selected >= 0) { UpdateSelection(now); return; }
            if (_shownAt < 0f) return;
            float e = now - _shownAt;
            Animate(e);
            if (e < InputDelay) return;
            for (int i = 0; i < _upgradeButtons.Length; i++)
                if (_upgradeButtons[i].gameObject.activeSelf) _upgradeButtons[i].interactable = true;
            if (e > InputDelay + Stagger * _cards.Length + PopTime) _shownAt = -1f;
        }

        private void Animate(float e)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null) continue;
                float k = Mathf.Clamp01((e - i * Stagger) / PopTime);
                float ease = 1f - (1f - k) * (1f - k);
                _cardGroups[i].alpha = k;
                _cards[i].localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, ease);
            }
        }

        private void SelectUpgrade(int index)
        {
            if (_selected >= 0 || _currentChoices == null || index >= _currentChoices.Length) return;
            _selected = index;
            _selectedAt = Time.unscaledTime;
            _fired = false;
            _fadeAt = -1f;
            _shownAt = -1f;
            for (int i = 0; i < _upgradeButtons.Length; i++) _upgradeButtons[i].interactable = false;
            if (_outlines[index] != null) _outlines[index].enabled = true;
            Haptics.Impact(1);
        }

        // 0-150 ms onay (seçilen punch + altın çerçeve, diğerleri söner) → olay → kuyruk yoksa 150 ms sönme.
        private void UpdateSelection(float now)
        {
            if (_fadeAt >= 0f)
            {
                float f = (now - _fadeAt) / FadeOutTime;
                if (f >= 1f) { Hide(); return; }
                if (_panelGroup != null) _panelGroup.alpha = 1f - f * f;
                return;
            }

            float e = now - _selectedAt;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null || !_cards[i].gameObject.activeSelf) continue;
                if (i == _selected)
                {
                    float p = Mathf.Clamp01(e / ConfirmPunch);
                    _cards[i].localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(p * Mathf.PI));
                    _cardGroups[i].alpha = 1f;
                }
                else
                {
                    float k = Mathf.Clamp01(e / ConfirmTime);
                    float ease = 1f - (1f - k) * (1f - k);
                    _cardGroups[i].alpha = Mathf.Lerp(1f, 0.35f, ease);
                    _cards[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.95f, ease);
                }
            }
            if (e < ConfirmTime || _fired) return;

            _fired = true;
            _reshown = false;
            UpgradeData chosen = _currentChoices[_selected];
            GameEvents.RaiseUpgradeSelected(chosen);
            // Kuyrukta seviye varsa ShowChoices bu çağrının içinde paneli yeniden doldurdu; game over Hide etti.
            if (_reshown || _selected < 0) return;
            _fadeAt = Time.unscaledTime;
            if (_panelGroup != null) _panelGroup.blocksRaycasts = false;
        }

        private void ResetSelection()
        {
            _selected = -1;
            _selectedAt = -1f;
            _fadeAt = -1f;
            _fired = false;
            if (_outlines == null) return;
            foreach (Outline o in _outlines)
                if (o != null) o.enabled = false;
        }

        private void Hide()
        {
            ResetSelection();
            _shownAt = -1f;
            if (_panelGroup != null) { _panelGroup.alpha = 1f; _panelGroup.blocksRaycasts = true; }
            if (_panel != null) _panel.SetActive(false);
        }

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build()
        {
            if (_built || _panel == null) return;
            _built = true;
            var panel = (RectTransform)_panel.transform;
            CoverFullScreen(panel);

            // HUD (üst blok + slot şeridi) açık kalır: panel kökü görünmez ama dokunmayı tutar, karartma HUD'un altından başlar.
            Image backdrop = _panel.GetComponent<Image>();
            if (backdrop != null) { backdrop.color = new Color(0f, 0f, 0f, 0f); backdrop.sprite = null; backdrop.raycastTarget = true; }
            Sprite rounded = null;
            foreach (Button b in _upgradeButtons)
                if (b != null && b.targetGraphic is Image bi && bi.sprite != null) { rounded = bi.sprite; break; }
            _rounded = rounded;
            _circle = Resources.Load<Sprite>("UI/Shapes/UI_Circle");
            _diamond = Resources.Load<Sprite>("UI/Shapes/UI_Diamond");
            TMP_SpriteAsset glyphs = Resources.Load<TMP_SpriteAsset>("Sprite Assets/UI_Glyphs");
            _growth = FindAnyObjectByType<Entities.Blob.BlobGrowth>();
            _panelGroup = _panel.GetComponent<CanvasGroup>();
            if (_panelGroup == null) _panelGroup = _panel.AddComponent<CanvasGroup>();

            // Eski başlık ("SEVİYE ATLADIN!") gizlenir, yerine gölgeli büyük başlık.
            foreach (Transform c in panel)
                if (c.GetComponent<Button>() == null) c.gameObject.SetActive(false);
            var dim = NewRect("Dim", panel, Vector2.zero, Vector2.zero);
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = Vector2.zero;
            dim.offsetMax = new Vector2(0f, -HudClearance);
            dim.SetAsFirstSibling();
            Image dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = Backdrop;
            dimImage.raycastTarget = false;

            // Kompakt düzen: HUD'un altından ekranın ortasına. Slot şeridi kart ekranında yok (HUD'daki görünür kalıyor).
            Shadowed(panel, new Vector2(0f, 430f), "SEVİYE ATLADIN!", 76f, Gold);
            _subtitle = Label("Subtitle", panel, new Vector2(0f, 362f), new Vector2(900f, 50f), 36f, TextAlignmentOptions.Center, Subtle);
            _subtitle.text = "Bir güç seç";
            _subtitle.fontStyle = FontStyles.Normal;

            _activeCount = Label("ActiveCount", panel, new Vector2(-160f, 306f), new Vector2(300f, 40f), 32f, TextAlignmentOptions.Center, ActiveTag);
            _passiveCount = Label("PassiveCount", panel, new Vector2(160f, 306f), new Vector2(300f, 40f), 32f, TextAlignmentOptions.Center, PassiveTag);

            int n = _upgradeButtons.Length;
            _cards = new RectTransform[n];
            _cardImages = new Image[n];
            _cardGroups = new CanvasGroup[n];
            _badges = new Image[n];
            _badgeTexts = new TMP_Text[n];
            _kindTexts = new TMP_Text[n];
            _kindShapes = new Image[n];
            _noteTexts = new TMP_Text[n];
            _outlines = new Outline[n];
            for (int i = 0; i < n; i++)
            {
                Button button = _upgradeButtons[i];
                if (button == null) continue;
                var rect = (RectTransform)button.transform;
                Place(rect, panel, new Vector2(0f, CardTopY - i * (CardHeight + CardGap)), new Vector2(920f, CardHeight));
                _cards[i] = rect;
                _cardImages[i] = button.targetGraphic as Image;
                if (_cardImages[i] != null && rounded != null) { _cardImages[i].sprite = rounded; _cardImages[i].type = Image.Type.Sliced; }
                if (_cardImages[i] != null)
                {
                    // Seçim onayı: 6 px altın çerçeve.
                    Outline outline = _cardImages[i].gameObject.AddComponent<Outline>();
                    outline.effectColor = Gold;
                    outline.effectDistance = new Vector2(6f, -6f);
                    outline.useGraphicAlpha = false;
                    outline.enabled = false;
                    _outlines[i] = outline;
                }
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
                colors.disabledColor = Color.white; // giriş gecikmesinde ve seçimde soluk görünmesin
                button.colors = colors;
                _cardGroups[i] = button.GetComponent<CanvasGroup>();
                if (_cardGroups[i] == null) _cardGroups[i] = button.gameObject.AddComponent<CanvasGroup>();

                if (i < _upgradeNameTexts.Length && _upgradeNameTexts[i] != null)
                {
                    StyleText(_upgradeNameTexts[i], rect, new Vector2(-140f, 82f), new Vector2(620f, 70f), 52f, TextAlignmentOptions.Left, Color.white, true);
                    AutoSize(_upgradeNameTexts[i], 40f, 52f);
                }
                if (i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                {
                    // Kutu en fazla 3 satır alır (32 px'te 4. satır sığmaz); sığmayan son "…" ile kesilir.
                    StyleText(_upgradeDescTexts[i], rect, new Vector2(0f, -30f), new Vector2(840f, 120f), 36f, TextAlignmentOptions.TopLeft, Subtle, false);
                    _upgradeDescTexts[i].textWrappingMode = TextWrappingModes.Normal;
                    _upgradeDescTexts[i].overflowMode = TextOverflowModes.Ellipsis;
                    AutoSize(_upgradeDescTexts[i], 32f, 36f);
                }

                RectTransform badge = NewRect("Badge", rect, new Vector2(330f, 84f), new Vector2(220f, 60f));
                _badges[i] = badge.gameObject.AddComponent<Image>();
                _badges[i].raycastTarget = false;
                if (rounded != null) { _badges[i].sprite = rounded; _badges[i].type = Image.Type.Sliced; }
                _badgeTexts[i] = Label("Text", badge, Vector2.zero, badge.sizeDelta, 34f, TextAlignmentOptions.Center, Color.white);
                if (glyphs != null) _badgeTexts[i].spriteAsset = glyphs;
                AutoSize(_badgeTexts[i], 28f, 34f);

                // Tür etiketi: 32x32 şekil + 8 boşluk + metin (rozet altı, kutu 220x40).
                RectTransform shape = NewRect("KindShape", rect, new Vector2(236f, 30f), new Vector2(32f, 32f));
                _kindShapes[i] = shape.gameObject.AddComponent<Image>();
                _kindShapes[i].raycastTarget = false;
                _kindTexts[i] = Label("Kind", rect, new Vector2(350f, 30f), new Vector2(180f, 40f), 32f, TextAlignmentOptions.Left, Color.white);

                _noteTexts[i] = Label("SlotNote", rect, new Vector2(0f, -112f), new Vector2(840f, 40f), 32f, TextAlignmentOptions.Left, Subtle);
                if (glyphs != null) _noteTexts[i].spriteAsset = glyphs;
                _noteTexts[i].gameObject.SetActive(false);
            }
        }

        private static void SetShape(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            bool sliced = sprite != null && sprite.border.sqrMagnitude > 0f;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
        }

        // PauseMenu/GameOverScreen gibi: kök Canvas'a taşınır, çentik ve alt şerit dahil tüm ekranı kaplar.
        private static void CoverFullScreen(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>(true);
            if (canvas == null) return;
            rect.SetParent(canvas.rootCanvas.transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void Place(RectTransform rect, RectTransform parent, Vector2 pos, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void StyleText(TMP_Text t, RectTransform parent, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color, bool bold)
        {
            Place(t.rectTransform, parent, pos, size);
            t.fontSize = fontSize;
            t.enableAutoSizing = false;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = alignment;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
        }

        private static void AutoSize(TMP_Text t, float min, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            Place(rect, parent, pos, size);
            return rect;
        }

        private static TMP_Text Label(string name, RectTransform parent, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = alignment;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void Shadowed(RectTransform parent, Vector2 pos, string text, float size, Color color)
        {
            var box = new Vector2(1000f, size * 1.3f);
            Label("TitleShadow", parent, pos + new Vector2(0f, -size * 0.06f), box, size, TextAlignmentOptions.Center, ShadowColor).text = text;
            Label("Title", parent, pos, box, size, TextAlignmentOptions.Center, color).text = text;
        }
    }
}
