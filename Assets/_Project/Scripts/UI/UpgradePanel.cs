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
        // Kompakt düzen (ref px, panel merkezine göre): karartma HUD'un slot şeridinin hemen altından başlar
        // (çentik/yan ekran fark etmez, FitContent ölçer). Şerit bulunamazsa ekranın üstünden HudClearance.
        private const float HudClearance = 470f, StripGap = 16f;
        private const float CardTopY = 150f, CardHeight = 290f, CardGap = 16f;
        // İçeriğin üst/alt sınırı (başlık üstü, son kart altı) ve HUD/ekran kenarına bırakılan pay.
        private const float ContentTop = 480f, ContentBottom = CardTopY - 2f * (CardHeight + CardGap) - CardHeight * 0.5f - 20f;
        private const float FitMargin = 20f;
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
        private RectTransform _content, _dim, _hudStrip;
        private float _fittedHeight = -1f;
        private Sprite _rounded, _circle, _diamond;
        // Arayüz kiti (UiKit): krem kart, solda skill slotu + ikon, renkli rozet ve tür hapı. Kurulu değilse eski düz kartlar.
        private bool _k;
        private Image[] _slots, _icons, _kindPills;
        private static readonly Color DescInk = new Color32(58, 40, 30, 255); // açıklama: koyu kahve, krem zeminde okunaklı
        private TMP_Text[] _letters;
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
                    _upgradeNameTexts[i].color = _k ? UiKit.Ink : evo ? Gold : Color.white;
                }
                if (_upgradeDescTexts != null && i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                    _upgradeDescTexts[i].text = data.GetDescriptionForLevel(current + 1);

                if (_k) KitCard(i, data, evo, rare, active);
                else if (_cardImages[i] != null) _cardImages[i].color = evo ? EvolutionCard : rare ? RareCard : CardColor;

                // Tür: K23 şekli + metin (evrimde yok).
                Color kindColor = rare ? RareTag : active ? ActiveTag : PassiveTag;
                _kindTexts[i].text = evo ? "" : rare ? "NADİR" : active ? "AKTİF" : "PASİF";
                _kindTexts[i].color = _k ? Color.white : kindColor;
                _kindShapes[i].gameObject.SetActive(!evo && !_k);
                if (_k) _kindPills[i].gameObject.SetActive(!evo);
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
                if (_k)
                {
                    UiKit.Paint(_badges[i], evo ? "Badges/badge_purple" : toMax ? "Badges/badge_red" : isNew ? "Badges/badge_green" : "Badges/badge_gold");
                    FitBorder(_badges[i]);
                    badge.color = Color.white;
                }
                else
                {
                    _badges[i].color = evo || toMax ? Gold : isNew ? NewBadge : LevelBadge;
                    badge.color = evo || toMax || isNew ? Ink : Color.white;
                }

                string note = NoteFor(sys, data, current, evo, rare, out bool warn);
                _noteTexts[i].gameObject.SetActive(note != null);
                if (note != null)
                {
                    _noteTexts[i].SetText(note);
                    _noteTexts[i].color = _k ? (warn ? UiKit.Red : UiKit.InkSoft) : warn ? NoteWarn : Subtle;
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
            FitContent();
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
            _k = UiKit.Ready;
            _growth = FindAnyObjectByType<Entities.Blob.BlobGrowth>();
            _panelGroup = _panel.GetComponent<CanvasGroup>();
            if (_panelGroup == null) _panelGroup = _panel.AddComponent<CanvasGroup>();

            // Eski başlık ("SEVİYE ATLADIN!") gizlenir, yerine gölgeli büyük başlık.
            foreach (Transform c in panel)
                if (c.GetComponent<Button>() == null) c.gameObject.SetActive(false);
            var dim = NewRect("Dim", panel, Vector2.zero, Vector2.zero);
            _dim = dim;
            SkillSlotStrip strip = FindAnyObjectByType<SkillSlotStrip>();
            if (strip != null) _hudStrip = (RectTransform)strip.transform;
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = Vector2.zero;
            dim.offsetMax = _k ? Vector2.zero : new Vector2(0f, -HudClearance); // kit: tam ekran karartma
            dim.SetAsFirstSibling();
            Image dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = Backdrop;
            dimImage.raycastTarget = false;

            // Kompakt düzen: HUD'un altından ekranın ortasına. Slot şeridi kart ekranında yok (HUD'daki görünür kalıyor).
            // İçerik ayrı kökte: ekran kısaysa (yan ekran) HUD'un altındaki alana sığacak kadar küçülüp kayar (FitContent).
            _content = NewRect("Content", panel, Vector2.zero, new Vector2(1000f, ContentTop - ContentBottom));
            RectTransform content = _content;
            if (_k) KitTitle(content, new Vector2(0f, 446f), "SEVİYE ATLADIN!");
            else Shadowed(content, new Vector2(0f, 430f), "SEVİYE ATLADIN!", 76f, Gold);
            _subtitle = Label("Subtitle", content, new Vector2(0f, 362f), new Vector2(900f, 50f), 36f, TextAlignmentOptions.Center, Subtle);
            if (_k) UiKit.Outlined(_subtitle, Color.white, false, false);
            _subtitle.text = "Bir güç seç";
            _subtitle.fontStyle = FontStyles.Normal;

            _activeCount = Label("ActiveCount", content, new Vector2(-160f, 306f), new Vector2(300f, 40f), 32f, TextAlignmentOptions.Center, ActiveTag);
            _passiveCount = Label("PassiveCount", content, new Vector2(160f, 306f), new Vector2(300f, 40f), 32f, TextAlignmentOptions.Center, PassiveTag);
            if (_k)
            {
                // Alt yazı ortada, sayaçlar iki yanda renkli haplarda (tek sıra, kartların üstünde).
                const float rowY = 352f, pillX = 372f;
                _subtitle.rectTransform.anchoredPosition = new Vector2(0f, rowY);
                _subtitle.rectTransform.sizeDelta = new Vector2(460f, 50f);
                AutoSize(_subtitle, 26f, 34f);
                int at = _activeCount.transform.GetSiblingIndex();
                Image ap = UiKit.NewImage("ActivePill", content, new Vector2(-pillX, rowY), new Vector2(240f, 70f), "Pills/pill_orange");
                Image pp = UiKit.NewImage("PassivePill", content, new Vector2(pillX, rowY), new Vector2(240f, 70f), "Pills/pill_blue");
                FitBorder(ap); FitBorder(pp);
                ap.transform.SetSiblingIndex(at);
                pp.transform.SetSiblingIndex(at + 1);
                Place(_activeCount.rectTransform, content, new Vector2(-pillX, rowY + 3f), new Vector2(220f, 44f));
                Place(_passiveCount.rectTransform, content, new Vector2(pillX, rowY + 3f), new Vector2(220f, 44f));
                UiKit.Outlined(_activeCount, Color.white);
                UiKit.Outlined(_passiveCount, Color.white);
            }

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
            _slots = new Image[n];
            _icons = new Image[n];
            _kindPills = new Image[n];
            _letters = new TMP_Text[n];
            for (int i = 0; i < n; i++)
            {
                Button button = _upgradeButtons[i];
                if (button == null) continue;
                var rect = (RectTransform)button.transform;
                Place(rect, content, new Vector2(0f, CardTopY - i * (CardHeight + CardGap)), new Vector2(920f, CardHeight));
                _cards[i] = rect;
                _cardImages[i] = button.targetGraphic as Image;
                if (_cardImages[i] != null && _k) { UiKit.Paint(_cardImages[i], "Panels/card_cream"); _cardImages[i].pixelsPerUnitMultiplier = 0.9f; }
                else if (_cardImages[i] != null && rounded != null) { _cardImages[i].sprite = rounded; _cardImages[i].type = Image.Type.Sliced; }
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
                    StyleText(_upgradeNameTexts[i], rect, new Vector2(-140f, 92f), new Vector2(620f, 70f), 52f, TextAlignmentOptions.Left, Color.white, true);
                    AutoSize(_upgradeNameTexts[i], 40f, 52f);
                }
                if (i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                {
                    // Kutu en fazla 3 satır alır (32 px'te 4. satır sığmaz); sığmayan son "…" ile kesilir.
                    StyleText(_upgradeDescTexts[i], rect, new Vector2(0f, -42f), new Vector2(840f, 120f), 36f, TextAlignmentOptions.TopLeft, Subtle, false);
                    _upgradeDescTexts[i].textWrappingMode = TextWrappingModes.Normal;
                    _upgradeDescTexts[i].overflowMode = TextOverflowModes.Ellipsis;
                    AutoSize(_upgradeDescTexts[i], 28f, 36f);
                }

                RectTransform badge = NewRect("Badge", rect, new Vector2(330f, 94f), new Vector2(220f, 60f));
                _badges[i] = badge.gameObject.AddComponent<Image>();
                _badges[i].raycastTarget = false;
                if (rounded != null) { _badges[i].sprite = rounded; _badges[i].type = Image.Type.Sliced; }
                _badgeTexts[i] = Label("Text", badge, Vector2.zero, badge.sizeDelta, 34f, TextAlignmentOptions.Center, Color.white);
                if (glyphs != null) _badgeTexts[i].spriteAsset = glyphs;
                AutoSize(_badgeTexts[i], 28f, 34f);

                // Tür etiketi: 32x32 şekil + 8 boşluk + metin (rozet altı, kutu 220x40).
                RectTransform shape = NewRect("KindShape", rect, new Vector2(236f, 42f), new Vector2(32f, 32f));
                _kindShapes[i] = shape.gameObject.AddComponent<Image>();
                _kindShapes[i].raycastTarget = false;
                _kindTexts[i] = Label("Kind", rect, new Vector2(350f, 42f), new Vector2(180f, 40f), 32f, TextAlignmentOptions.Left, Color.white);

                _noteTexts[i] = Label("SlotNote", rect, new Vector2(0f, -122f), new Vector2(840f, 40f), 32f, TextAlignmentOptions.Left, Subtle);
                if (glyphs != null) _noteTexts[i].spriteAsset = glyphs;
                _noteTexts[i].gameObject.SetActive(false);
                if (_k) KitLayout(i, rect);
            }
        }

        // Kit kart düzeni (kart 920x290): solda skill slotu (aktif turuncu daire / pasif mavi kare) ve ikon,
        // sağında ad + tür hapı, sağ üstte rozet, altta açıklama ve slot notu. Yazılar koyu mürekkep, haplarda beyaz konturlu.
        private void KitLayout(int i, RectTransform rect)
        {
            const float half = 460f, slotX = -half + 30f + 104f, textLeft = -half + 262f, textRight = half - 32f;
            float width = textRight - textLeft, cx = (textLeft + textRight) * 0.5f;

            _slots[i] = UiKit.NewImage("Slot", rect, new Vector2(slotX, 0f), new Vector2(208f, 208f), "Slots/slot_active");
            _slots[i].transform.SetAsFirstSibling();
            _icons[i] = UiKit.NewImage("Icon", _slots[i].transform, Vector2.zero, new Vector2(164f, 164f), null);
            _letters[i] = Label("Letter", (RectTransform)_slots[i].transform, Vector2.zero, new Vector2(140f, 140f), 84f, TextAlignmentOptions.Center, UiKit.Ink);
            UiKit.Text(_letters[i], true, UiKit.Ink);

            if (_upgradeNameTexts[i] != null)
            {
                Place(_upgradeNameTexts[i].rectTransform, rect, new Vector2(textLeft + (width - 230f) * 0.5f, 88f), new Vector2(width - 230f, 80f));
                UiKit.Text(_upgradeNameTexts[i], true, UiKit.Ink);
                AutoSize(_upgradeNameTexts[i], 46f, 66f);
            }

            // Tür hapı (AKTİF / PASİF / NADİR) adın altında.
            _kindPills[i] = UiKit.NewImage("KindPill", rect, new Vector2(textLeft + 86f, 34f), new Vector2(172f, 56f), "Pills/pill_orange");
            Place(_kindTexts[i].rectTransform, (RectTransform)_kindPills[i].transform, new Vector2(0f, 3f), new Vector2(160f, 46f));
            UiKit.Outlined(_kindTexts[i], Color.white, false);
            _kindTexts[i].alignment = TextAlignmentOptions.Center;
            _kindTexts[i].fontSize = 28f;
            _kindTexts[i].characterSpacing = 4f;

            var badge = (RectTransform)_badges[i].transform;
            Place(badge, rect, new Vector2(textRight - 110f, 90f), new Vector2(220f, 82f));
            Place(_badgeTexts[i].rectTransform, badge, new Vector2(0f, 4f), new Vector2(190f, 60f));
            UiKit.Outlined(_badgeTexts[i], Color.white);

            if (_upgradeDescTexts[i] != null)
            {
                Place(_upgradeDescTexts[i].rectTransform, rect, new Vector2(cx, -48f), new Vector2(width, 120f));
                UiKit.Text(_upgradeDescTexts[i], false, DescInk);
                AutoSize(_upgradeDescTexts[i], 30f, 38f);
            }
            Place(_noteTexts[i].rectTransform, rect, new Vector2(cx, -118f), new Vector2(width, 36f));
            UiKit.Text(_noteTexts[i], false, UiKit.InkSoft);
            _noteTexts[i].fontSize = 27f;
        }

        // Kartın görünümü seçeneğe göre: evrim altın kart, slot türü, ikon (yoksa adın baş harfi), tür hapı rengi.
        private void KitCard(int i, UpgradeData data, bool evo, bool rare, bool active)
        {
            UiKit.Paint(_cardImages[i], evo ? "Panels/card_gold" : "Panels/card_cream");
            _cardImages[i].raycastTarget = true;
            UiKit.Paint(_slots[i], active || rare || evo ? "Slots/slot_active" : "Slots/slot_passive");
            Sprite icon = UiKit.SkillIcon(data);
            _icons[i].sprite = icon;
            _icons[i].enabled = icon != null;
            _icons[i].preserveAspect = true;
            _letters[i].gameObject.SetActive(icon == null);
            if (icon == null && !string.IsNullOrEmpty(data.DisplayName)) _letters[i].SetText(data.DisplayName.Substring(0, 1));
            UiKit.Paint(_kindPills[i], rare ? "Pills/pill_purple" : active ? "Pills/pill_orange" : "Pills/pill_blue");
            FitBorder(_kindPills[i]);
        }

        private static void FitBorder(Image img) => UiKit.FitBorder(img);

        // Başlık: düz sarı kurdele, beyaz konturlu yazı.
        private static void KitTitle(RectTransform parent, Vector2 pos, string text)
        {
            Image band = UiKit.NewImage("Ribbon", parent, pos, new Vector2(860f, 120f), "Banners/ribbon_gold");
            FitBorder(band);
            TMP_Text t = Label("Title", (RectTransform)band.transform, new Vector2(0f, 8f), new Vector2(600f, 96f), 64f, TextAlignmentOptions.Center, Color.white);
            UiKit.Outlined(t, Color.white, true);
            AutoSize(t, 42f, 64f);
            t.text = text;
        }

        // Dik ekranda içerik yerinde durur; HUD'un altına sığmıyorsa o alana ortalanır ve gerekirse küçülür.
        private void FitContent()
        {
            if (_content == null) return;
            var panel = (RectTransform)_panel.transform;
            float h = panel.rect.height;
            if (Mathf.Approximately(h, _fittedHeight)) return;
            _fittedHeight = h;
            float clearance = HudClearance;
            if (_hudStrip != null)
            {
                var corners = new Vector3[4];
                _hudStrip.GetWorldCorners(corners); // [0] sol alt
                clearance = panel.rect.yMax - panel.InverseTransformPoint(corners[0]).y + StripGap;
            }
            _dim.offsetMax = _k ? Vector2.zero : new Vector2(0f, -clearance); // kit: tam ekran karartma
            float top = h * 0.5f - clearance - FitMargin, bottom = -h * 0.5f + FitMargin;
            float scale = 1f, y = 0f;
            if (ContentTop > top || ContentBottom < bottom)
            {
                scale = Mathf.Min(1f, (top - bottom) / (ContentTop - ContentBottom));
                y = (top + bottom) * 0.5f - scale * (ContentTop + ContentBottom) * 0.5f;
            }
            _content.anchoredPosition = new Vector2(0f, y);
            _content.localScale = new Vector3(scale, scale, 1f);
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
