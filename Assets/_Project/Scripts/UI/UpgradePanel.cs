using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.UI
{
    // Seviye atlama (3 karttan 1). HUD diliyle: koyu yarı saydam kartlar, kalın beyaz ad, sağ üstte rozet
    // (YENİ / Sv. x/8), altında seviyeye özel açıklama; evrim kartı altın. Kartlar sırayla belirir; oyun sırasında
    // sürüklerken yanlışlıkla seçilmesin diye ilk InputDelay boyunca dokunmaya kapalı. Timescale 0: ölçeksiz zaman.
    // Düzen çalışma zamanında bir kez kurulur (sahnedeki buton/yazılar yeniden biçimlenir).
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button[] _upgradeButtons;
        [SerializeField] private TMP_Text[] _upgradeNameTexts;
        [SerializeField] private TMP_Text[] _upgradeDescTexts;

        private static readonly Color Backdrop = new Color(0.05f, 0.05f, 0.17f, 0.72f);
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.88f);
        private static readonly Color EvolutionCard = new Color(0.24f, 0.17f, 0.03f, 0.92f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color NewBadge = new Color(0.35f, 0.75f, 0.4f, 1f);
        private static readonly Color LevelBadge = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);

        private const float InputDelay = 0.4f;
        private const float Stagger = 0.07f, PopTime = 0.22f;

        private UpgradeData[] _currentChoices;
        private bool _built;
        private Image[] _cardImages;
        private RectTransform[] _cards;
        private CanvasGroup[] _cardGroups;
        private Image[] _badges;
        private TMP_Text[] _badgeTexts;
        private float _shownAt = -1f;

        private void OnEnable()
        {
            GameEvents.OnUpgradeChoicesReady += ShowChoices;
            GameEvents.OnGameOver += Hide;
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeChoicesReady -= ShowChoices;
            GameEvents.OnGameOver -= Hide;
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
            _currentChoices = choices;
            if (_panel != null) _panel.SetActive(true);

            for (int i = 0; i < _upgradeButtons.Length; i++)
            {
                bool hasChoice = i < choices.Length && choices[i] != null;
                _upgradeButtons[i].gameObject.SetActive(hasChoice);
                if (!hasChoice) continue;

                UpgradeData data = choices[i];
                bool evo = data.IsEvolution;
                int current = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 0;

                if (_upgradeNameTexts != null && i < _upgradeNameTexts.Length && _upgradeNameTexts[i] != null)
                {
                    // B17: evrim çıktısı kartları ★ + altın renkle işaretlenir.
                    _upgradeNameTexts[i].text = evo ? $"★ {data.DisplayName}" : data.DisplayName;
                    _upgradeNameTexts[i].color = evo ? Gold : Color.white;
                }
                if (_upgradeDescTexts != null && i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                    _upgradeDescTexts[i].text = DescriptionFor(data);

                if (_cardImages[i] != null) _cardImages[i].color = evo ? EvolutionCard : CardColor;
                // 2026-11-03: yeni skill mi, yükseltme mi — seçilince ulaşacağı seviye.
                _badgeTexts[i].text = evo ? "EVRİM" : current == 0 ? "YENİ" : $"Sv. {current + 1}/{data.MaxLevel}";
                _badges[i].color = evo ? Gold : current == 0 ? NewBadge : LevelBadge;
                _badgeTexts[i].color = evo ? new Color(0.12f, 0.08f, 0.02f, 1f) : Color.white;
                _upgradeButtons[i].interactable = false; // InputDelay sonra açılır
            }

            _shownAt = Time.unscaledTime;
            Animate(0f);
        }

        private void Update()
        {
            if (_shownAt < 0f || _panel == null || !_panel.activeSelf) return;
            float e = Time.unscaledTime - _shownAt;
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

        private static string DescriptionFor(UpgradeData data)
        {
            int next = (UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 0) + 1;
            string text = data.GetDescriptionForLevel(next);
            return text.Contains("{ultimate}")
                ? text.Replace("{ultimate}", WeaponBase.CurrentUltimateDescription ?? "Silahın son formuna ulaşır.")
                : text;
        }

        private void SelectUpgrade(int index)
        {
            if (_currentChoices == null || index >= _currentChoices.Length) return;
            UpgradeData chosen = _currentChoices[index];
            // Önce gizle: sırada bekleyen level-up varsa yeni kartlar bu çağrının içinde açılır.
            Hide();
            GameEvents.RaiseUpgradeSelected(chosen);
        }

        private void Hide()
        {
            _shownAt = -1f;
            if (_panel != null) _panel.SetActive(false);
        }

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build()
        {
            if (_built || _panel == null) return;
            _built = true;
            var panel = (RectTransform)_panel.transform;
            CoverFullScreen(panel);

            Image backdrop = _panel.GetComponent<Image>();
            if (backdrop != null) { backdrop.color = Backdrop; backdrop.sprite = null; }
            Sprite rounded = null;
            foreach (Button b in _upgradeButtons)
                if (b != null && b.targetGraphic is Image bi && bi.sprite != null) { rounded = bi.sprite; break; }

            // Eski başlık ("SEVİYE ATLADIN!") gizlenir, yerine gölgeli büyük başlık.
            foreach (Transform c in panel)
                if (c.GetComponent<Button>() == null) c.gameObject.SetActive(false);
            Shadowed(panel, new Vector2(0f, 640f), "SEVİYE ATLADIN!", 96f, Gold);
            var sub = Label("Subtitle", panel, new Vector2(0f, 540f), new Vector2(900f, 60f), 40f, TextAlignmentOptions.Center, Subtle);
            sub.text = "Bir güç seç";
            sub.fontStyle = FontStyles.Normal;

            int n = _upgradeButtons.Length;
            _cards = new RectTransform[n];
            _cardImages = new Image[n];
            _cardGroups = new CanvasGroup[n];
            _badges = new Image[n];
            _badgeTexts = new TMP_Text[n];
            for (int i = 0; i < n; i++)
            {
                Button button = _upgradeButtons[i];
                if (button == null) continue;
                var rect = (RectTransform)button.transform;
                Place(rect, panel, new Vector2(0f, 280f - i * 350f), new Vector2(920f, 310f));
                _cards[i] = rect;
                _cardImages[i] = button.targetGraphic as Image;
                if (_cardImages[i] != null && rounded != null) { _cardImages[i].sprite = rounded; _cardImages[i].type = Image.Type.Sliced; }
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
                colors.disabledColor = Color.white; // giriş gecikmesinde soluk görünmesin
                button.colors = colors;
                _cardGroups[i] = button.GetComponent<CanvasGroup>();
                if (_cardGroups[i] == null) _cardGroups[i] = button.gameObject.AddComponent<CanvasGroup>();

                if (i < _upgradeNameTexts.Length && _upgradeNameTexts[i] != null)
                    StyleText(_upgradeNameTexts[i], rect, new Vector2(-70f, 92f), new Vector2(720f, 80f), 58f, TextAlignmentOptions.Left, Color.white, true);
                if (i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                {
                    StyleText(_upgradeDescTexts[i], rect, new Vector2(0f, -38f), new Vector2(840f, 170f), 36f, TextAlignmentOptions.TopLeft, Subtle, false);
                    _upgradeDescTexts[i].textWrappingMode = TextWrappingModes.Normal;
                }

                RectTransform badge = NewRect("Badge", rect, new Vector2(330f, 96f), new Vector2(200f, 60f));
                _badges[i] = badge.gameObject.AddComponent<Image>();
                _badges[i].raycastTarget = false;
                if (rounded != null) { _badges[i].sprite = rounded; _badges[i].type = Image.Type.Sliced; }
                _badgeTexts[i] = Label("Text", badge, Vector2.zero, badge.sizeDelta, 30f, TextAlignmentOptions.Center, Color.white);
            }
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
