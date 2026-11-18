using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Systems;

namespace BlobSurvivor.UI
{
    // Oyun sonu ekranı (ölüm = GAME OVER, final boss yutuldu = KAZANDIN). HUD'un diliyle: yarı saydam gece mavisi
    // paneller, kalın beyaz yazı, coin altın. Düzen çalışma zamanında bir kez kurulur (HUDController.LayoutHud gibi);
    // sahnedeki eski başlık/skor yazıları gizlenir, Restart butonu yeniden biçimlenir, Ana Menü butonu eklenir.
    // Timescale 0 olduğu için giriş animasyonu ve sayaçlar ölçeksiz zamanla akar.
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private Button _restartButton;

        // HUDController / PauseMenu ile aynı renkler.
        private static readonly Color Backdrop = new Color(0.05f, 0.05f, 0.17f, 0.85f);
        private static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.17f, 0.55f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color Crimson = new Color(0.545f, 0f, 0f, 1f);
        private static readonly Color Secondary = new Color(0.16f, 0.16f, 0.3f, 1f);
        private static readonly Color LoseTitle = new Color(1f, 0.42f, 0.38f, 1f);

        private const float IntroTime = 0.28f;
        private const float CountTime = 0.9f;
        private const float InputDelay = 0.4f; // sürüklerken ölünce parmağı kaldırmak butona basmasın

        private bool _built;
        private CanvasGroup _group;
        private RectTransform _content;
        private TMP_Text _title, _subtitle, _record;
        private TMP_Text _time, _score, _best, _coins, _credit;
        private Button _menuButton;
        private bool _k; // arayüz kiti kurulu (UiKit.Ready)
        private GameObject _winBanner, _loseBanner;

        private float _shownAt = -1f;
        private int _targetScore, _targetCoins, _targetCredit;

        private void OnEnable()
        {
            GameEvents.OnGameOver += ShowGameOver;
            GameEvents.OnRunComplete += ShowRunComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= ShowGameOver;
            GameEvents.OnRunComplete -= ShowRunComplete;
        }

        private void ShowGameOver() => Show(false);
        private void ShowRunComplete(RunEndReason reason) => Show(true);

        private void Start()
        {
            if (_panel != null) _panel.SetActive(false);
            _restartButton?.onClick.AddListener(Restart);
        }

        private void Show(bool won)
        {
            if (_panel == null) return;
            Build();
            _panel.SetActive(true);

            ScoreSystem score = FindAnyObjectByType<ScoreSystem>();
            float t = GameManager.Instance != null ? GameManager.Instance.SurvivalTime : 0f;

            _title.text = won ? "KAZANDIN!" : "GAME OVER";
            _title.color = _k ? Color.white : won ? Gold : LoseTitle;
            if (_winBanner != null) _winBanner.SetActive(won);
            if (_loseBanner != null) _loseBanner.SetActive(!won);
            _subtitle.text = won ? "Helikopteri yuttun, şehirde kırıntı kalmadı" : "Polis seni yakaladı";
            _time.text = $"{(int)t / 60:00}:{(int)t % 60:00}";

            _targetScore = score != null ? score.CurrentScore : 0;
            _targetCoins = score != null ? score.Coins : 0;
            _targetCredit = Mathf.RoundToInt(_targetCoins * (won ? 1f : 0.5f)); // MetaProgression ile aynı oran
            _best.text = score != null ? $"{Mathf.Max(score.HighScore, _targetScore):N0}" : "0";
            _record.gameObject.SetActive(score != null && score.HasNewHighScore);
            SetCounters(0f);

            SetButtonsInteractable(false);
            _shownAt = Time.unscaledTime;
            _group.alpha = 0f;
            _content.localScale = Vector3.one * 0.92f;
        }

        private void Update()
        {
            if (_shownAt < 0f || _panel == null || !_panel.activeSelf) return;
            float e = Time.unscaledTime - _shownAt;
            if (e >= InputDelay) SetButtonsInteractable(true);

            float intro = Mathf.Clamp01(e / IntroTime);
            _group.alpha = intro;
            float pop = 1f - (1f - intro) * (1f - intro);
            _content.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, pop);

            float count = Mathf.Clamp01((e - IntroTime) / CountTime);
            SetCounters(1f - (1f - count) * (1f - count));
            if (count >= 1f) _shownAt = -1f;
        }

        private void SetButtonsInteractable(bool on)
        {
            if (_restartButton != null && _restartButton.interactable != on) _restartButton.interactable = on;
            if (_menuButton != null && _menuButton.interactable != on) _menuButton.interactable = on;
        }

        private void SetCounters(float k)
        {
            _score.text = $"{Mathf.RoundToInt(_targetScore * k):N0}";
            _coins.text = $"{Mathf.RoundToInt(_targetCoins * k):N0}";
            _credit.text = $"+{Mathf.RoundToInt(_targetCredit * k):N0}";
        }

        private void Restart()
        {
            _shownAt = -1f;
            GameManager.Instance?.StartGame();
            if (_panel != null) _panel.SetActive(false);
        }

        private void ReturnToMenu()
        {
            _shownAt = -1f;
            GameManager.Instance?.ReturnToMenuFromRunEnd();
            if (_panel != null) _panel.SetActive(false);
            TitleScreen.ShowTitle(); // Ana Menü = giriş ekranı
        }

        // ---------------------------------------------------------------- tek seferlik düzen

        private void Build()
        {
            if (_built) return;
            _built = true;
            _k = UiKit.Ready;

            CoverFullScreen();
            Image backdrop = _panel.GetComponent<Image>();
            if (backdrop == null) backdrop = _panel.AddComponent<Image>();
            backdrop.color = Backdrop;
            backdrop.sprite = null;
            _group = _panel.GetComponent<CanvasGroup>();
            if (_group == null) _group = _panel.AddComponent<CanvasGroup>();

            // Eski yazılar (Game Over başlığı, skor/özet) gizlenir; sadece Restart butonu yeniden kullanılır.
            foreach (Transform child in _panel.transform)
                if (_restartButton == null || child != _restartButton.transform)
                    child.gameObject.SetActive(false);

            Sprite rounded = _restartButton != null && _restartButton.targetGraphic is Image ri ? ri.sprite : null;

            _content = NewRect("Content", (RectTransform)_panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 1500f));

            // Kit: başlık bandı yazının arkasında (KAZANDIN = sarı kurdele, GAME OVER = kırmızı plaka; Show seçer).
            if (_k)
            {
                Image win = UiKit.NewImage("WinRibbon", _content, new Vector2(0f, 600f), new Vector2(880f, 170f), "Banners/ribbon_gold");
                UiKit.FitBorder(win);
                Image lose = UiKit.NewImage("LosePlate", _content, new Vector2(0f, 600f), new Vector2(680f, 150f), "Banners/plate_red");
                UiKit.FitBorder(lose);
                _winBanner = win.gameObject; _loseBanner = lose.gameObject;
            }

            _title = Label("Title", _content, new Vector2(0f, _k ? 608f : 600f), new Vector2(860f, 140f), _k ? 84f : 112f, TextAlignmentOptions.Center, Color.white);
            if (_k) UiKit.Outlined(_title, Color.white, true);
            _subtitle = Label("Subtitle", _content, new Vector2(0f, _k ? 495f : 505f), new Vector2(860f, 60f), 38f, TextAlignmentOptions.Center, Subtle);
            _subtitle.fontStyle = FontStyles.Normal;
            if (_k) UiKit.Outlined(_subtitle, Color.white, false, false);

            RectTransform badge = Panel("RecordBadge", _content, new Vector2(0f, 400f), new Vector2(_k ? 470f : 420f, 84f), rounded, Gold);
            _record = Label("Text", badge, Vector2.zero, badge.sizeDelta, 44f, TextAlignmentOptions.Center, new Color(0.12f, 0.08f, 0.02f, 1f));
            _record.text = "YENİ REKOR!";
            if (_k)
            {
                Image badgeImage = badge.GetComponent<Image>();
                UiKit.Paint(badgeImage, "Badges/badge_gold");
                UiKit.FitBorder(badgeImage);
                UiKit.NewImage("Star", badge, new Vector2(-205f, 6f), new Vector2(96f, 96f), "Icons/star");
                _record.rectTransform.anchoredPosition = new Vector2(24f, 4f);
                UiKit.Outlined(_record, Color.white);
            }

            RectTransform card = Panel("StatsCard", _content, new Vector2(0f, 20f), new Vector2(760f, 600f), rounded, PanelColor);
            if (_k)
            {
                Image cardImage = card.GetComponent<Image>();
                UiKit.Paint(cardImage, "Panels/card_cream");
                cardImage.pixelsPerUnitMultiplier = 0.9f; // UpgradePanel kartıyla aynı çerçeve kalınlığı
            }
            string[] names = { "Süre", "Skor", "Rekor", "Toplanan coin", "Kazanılan coin" };
            TMP_Text[] values = new TMP_Text[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                float y = 228f - i * 114f;
                TMP_Text nameText = Label(names[i] + "Label", card, new Vector2(-150f, y), new Vector2(380f, 90f), 42f, TextAlignmentOptions.Left, Subtle);
                nameText.text = names[i];
                bool gold = i >= 3;
                // Kit: coin satırlarında değerin sağında coin ikonu, değer biraz sola.
                values[i] = Label(names[i] + "Value", card, new Vector2(_k && gold ? 90f : 150f, y), new Vector2(380f, 90f), i == 1 ? 58f : 48f,
                    TextAlignmentOptions.Right, gold ? Gold : Color.white);
                if (_k)
                {
                    UiKit.Text(nameText, false, UiKit.InkSoft);
                    UiKit.Text(values[i], true, UiKit.Ink);
                    if (gold) UiKit.NewImage("Coin", card, new Vector2(318f, y + 2f), new Vector2(60f, 60f), "Icons/coin");
                }
                if (i < names.Length - 1)
                {
                    RectTransform line = NewRect("Line", card, new Vector2(0.5f, 0.5f), new Vector2(0f, y - 57f), new Vector2(680f, 2f));
                    var img = line.gameObject.AddComponent<Image>();
                    img.color = _k ? new Color(UiKit.InkSoft.r, UiKit.InkSoft.g, UiKit.InkSoft.b, 0.18f) : new Color(1f, 1f, 1f, 0.08f);
                    img.raycastTarget = false;
                }
            }
            _time = values[0]; _score = values[1]; _best = values[2]; _coins = values[3]; _credit = values[4];

            if (_restartButton != null)
            {
                StyleButton(_restartButton, _content, new Vector2(0f, -420f), new Vector2(700f, 140f), "Tekrar Oyna", 58f, Crimson, rounded);
                GameObject copy = Instantiate(_restartButton.gameObject, _content); // tek seferlik UI kurulumu
                copy.name = "MenuButton";
                _menuButton = copy.GetComponent<Button>();
                _menuButton.onClick = new Button.ButtonClickedEvent();
                _menuButton.onClick.AddListener(ReturnToMenu);
                StyleButton(_menuButton, _content, new Vector2(0f, -590f), new Vector2(700f, 116f), "Ana Menü", 46f, Secondary, rounded);
                if (_k)
                {
                    KitButton(_restartButton, "green", "Glyphs/restart");
                    KitButton(_menuButton, "gray", "Glyphs/home");
                }
            }
        }

        // PauseMenu gibi: panel kök Canvas'a taşınıp çentik/alt şerit dahil tüm ekranı kaplar, en üstte çizilir.
        private void CoverFullScreen()
        {
            Canvas canvas = _panel.GetComponentInParent<Canvas>(true);
            RectTransform rect = _panel.GetComponent<RectTransform>();
            if (canvas == null || rect == null) return;
            rect.SetParent(canvas.rootCanvas.transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void StyleButton(Button button, RectTransform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color, Sprite sprite)
        {
            RectTransform rect = (RectTransform)button.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            if (button.targetGraphic is Image image)
            {
                image.color = color;
                if (sprite != null) { image.sprite = sprite; image.type = Image.Type.Sliced; }
            }
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white; // giriş korumasında soluk görünmesin
            button.colors = colors;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
                label.fontSize = fontSize;
                label.fontStyle = FontStyles.Bold;
                label.color = Color.white;
                label.enableAutoSizing = false;
            }
        }

        // Kit butonu: renkli sprite (basılı hali sprite değişimi), kenarlar boya orantılı, solda beyaz glif, konturlu yazı.
        // Giriş korumasında (interactable false) disabled sprite yok → normal görünür.
        private static void KitButton(Button button, string color, string glyph)
        {
            UiKit.StyleButton(button, color);
            RectTransform rect = (RectTransform)button.transform;
            if (button.targetGraphic is Image img && img.sprite != null)
                img.pixelsPerUnitMultiplier = img.sprite.rect.height / rect.sizeDelta.y;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            UiKit.Outlined(label, Color.white, rect.sizeDelta.y >= 130f);
            label.alignment = TextAlignmentOptions.Center;

            // Glif + yazı birlikte ortalı: glif yazının hemen solunda (bir kez ölçülür, metin sabit).
            float glyphSize = rect.sizeDelta.y * 0.5f;
            const float gap = 16f;
            float shift = (glyphSize + gap) * 0.5f;
            label.ForceMeshUpdate();
            float textWidth = label.preferredWidth;
            UiKit.NewImage("Glyph", rect, new Vector2(shift - textWidth * 0.5f - gap - glyphSize * 0.5f, 4f), new Vector2(glyphSize, glyphSize), glyph);
            RectTransform lr = label.rectTransform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(shift * 2f, 8f); // alt gölge payı: yazı hafif yukarı
            lr.offsetMax = Vector2.zero;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Panel(string name, RectTransform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            RectTransform rect = NewRect(name, parent, new Vector2(0.5f, 0.5f), pos, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null) { image.sprite = sprite; image.type = Image.Type.Sliced; }
            return rect;
        }

        private static TMP_Text Label(string name, RectTransform parent, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = NewRect(name, parent, new Vector2(0.5f, 0.5f), pos, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
