using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems.Meta;

namespace BlobSurvivor.UI
{
    // Karakter seçimi (giriş ekranında OYNA'dan sonra). HUD diliyle: yarı saydam gece mavisi kartlar, kalın beyaz
    // yazı, kredi altın. Her kart: portre (CharacterData.Icon, build_blobs.py), ad, pasif açıklaması; kilitliyse
    // soluk + koşul rozeti. Karta dokunmak oyunu başlatır. Düzen çalışma zamanında bir kez kurulur (sahne kurulumu yok).
    // Bu bileşen kendi paneliyle aynı objede, run sırasında pasif: bulan taraf FindObjectsInactive.Include kullanmalı.
    public class LobbyPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _hud;
        [SerializeField] private CharacterData[] _characters;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Image[] _icons;
        [SerializeField] private TMP_Text[] _nameTexts;

        // M23: Market'e giriş — MarketPanel'in kendi "geri" butonu LobbyPanel.Show + MarketPanel.Hide'ı
        // Inspector'da iki ayrı OnClick listener olarak wire ediyor.
        [SerializeField] private Button _marketButton;
        [SerializeField] private MarketPanel _marketPanel;

        private static readonly Color Backdrop = new Color(0.05f, 0.05f, 0.17f, 0.7f);
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.82f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.78f);
        private static readonly Color Secondary = new Color(0.16f, 0.16f, 0.3f, 1f);
        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);

        private class Card
        {
            public Image Portrait;
            public TMP_Text Name, Description, Lock;
            public RectTransform LockBadge;
            public CanvasGroup Group;
        }

        private static readonly bool MarketEnabled = false; // Market ekranı henüz hazır değil (const değil: unreachable uyarısı)

        private Card[] _cards;
        private TMP_Text _credit;
        private bool _built;

        private void Start()
        {
            Build();
            if (_panel != null) _panel.SetActive(true);
            if (_hud != null) _hud.SetActive(false);

            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                if (index >= _characters.Length || _characters[index] == null) continue;
                _buttons[i].onClick.AddListener(() => OnCharacterSelected(_characters[index]));
            }

            if (_marketButton != null && MarketEnabled)
                _marketButton.onClick.AddListener(OpenMarket);

            RefreshCharacterButtons();
        }

        // MarketPanel'in "geri" butonu Inspector'dan bunu çağırır. Market'te Mıknato satın alınmış olabileceğinden
        // kilit durumu her Show()'da yeniden değerlendirilir.
        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            RefreshCharacterButtons();
        }

        // Giriş ekranı açıkken lobi ve HUD gizli.
        public void HideForTitle()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_hud != null) _hud.SetActive(false);
        }

        public void ShowFromRun()
        {
            if (_hud != null) _hud.SetActive(false);
            Show();
        }

        // Kilitler: Mıknato Market'te 500 krediyle (RequiresMarketUnlock), Mermo 3 run bitirince
        // (RequiredCompletedRuns, ölüm de sayılır). Kilitliyken kart soluk, koşul rozette yazar.
        private void RefreshCharacterButtons()
        {
            Build();
            MetaProgression meta = MetaProgression.Instance;
            bool miknatoUnlocked = meta == null || meta.MiknatoUnlocked;
            int completedRuns = meta != null ? meta.CompletedRuns : int.MaxValue;
            if (_credit != null) _credit.text = $"{(meta != null ? meta.Credit : 0):N0} kredi";

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (i >= _characters.Length || _characters[i] == null) continue;
                CharacterData data = _characters[i];
                bool marketLocked = data.RequiresMarketUnlock && !miknatoUnlocked;
                int runsLeft = Mathf.Max(0, data.RequiredCompletedRuns - completedRuns);
                bool locked = marketLocked || runsLeft > 0;
                string lockReason = marketLocked ? "Market'te açılır" : $"{runsLeft} run sonra açılır";

                if (_buttons[i] != null) _buttons[i].interactable = !locked;
                if (_cards == null || i >= _cards.Length || _cards[i] == null) continue;

                Card card = _cards[i];
                card.Portrait.sprite = data.Icon;
                card.Portrait.enabled = data.Icon != null;
                card.Name.text = data.DisplayName;
                card.Description.text = data.Description;
                card.LockBadge.gameObject.SetActive(locked);
                card.Lock.text = $"KİLİTLİ · {lockReason}";
                card.Group.alpha = locked ? 0.55f : 1f;
            }
        }

        private void OpenMarket()
        {
            if (_panel != null) _panel.SetActive(false);
            _marketPanel?.Show();
        }

        private void OnCharacterSelected(CharacterData data)
        {
            if (_panel != null) _panel.SetActive(false);
            if (_hud != null) _hud.SetActive(true);
            GameManager.Instance.StartGame(data);
        }

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build()
        {
            if (_built || _panel == null) return;
            _built = true;
            var panel = (RectTransform)_panel.transform;

            Image backdrop = _panel.GetComponent<Image>();
            Sprite rounded = _marketButton != null && _marketButton.targetGraphic is Image mi ? mi.sprite : null;
            if (backdrop != null) { backdrop.color = Backdrop; backdrop.sprite = null; }

            Shadowed("Title", panel, new Vector2(0f, 760f), new Vector2(1000f, 120f), "KARAKTERİNİ SEÇ", 92f, Color.white);

            RectTransform chip = Box("CreditChip", panel, new Vector2(0f, 650f), new Vector2(360f, 72f), rounded, CardColor);
            _credit = Text("Credit", chip, Vector2.zero, chip.sizeDelta, 38f, TextAlignmentOptions.Center, Gold);

            _cards = new Card[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
                if (_buttons[i] != null) _cards[i] = BuildCard(_buttons[i], panel, new Vector2(0f, 330f - i * 390f), rounded);

            // Market henüz yok: gri, basılmaz, "YAKINDA". Açılınca MarketEnabled = true yeter.
            if (_marketButton != null)
            {
                Place((RectTransform)_marketButton.transform, panel, new Vector2(0f, -800f), new Vector2(640f, 120f));
                StyleImage(_marketButton.targetGraphic as Image, MarketEnabled ? Secondary : new Color(0.32f, 0.32f, 0.36f, 0.9f), rounded);
                foreach (Transform c in _marketButton.transform) c.gameObject.SetActive(false);
                _marketButton.interactable = MarketEnabled;
                ColorBlock mc = _marketButton.colors;
                mc.disabledColor = Color.white; // rengi yukarıda verildi, Unity'nin ek soluklaştırması olmasın
                _marketButton.colors = mc;
                Text("Label", (RectTransform)_marketButton.transform, new Vector2(0f, MarketEnabled ? 0f : 12f), new Vector2(640f, 70f), 50f,
                    TextAlignmentOptions.Center, MarketEnabled ? Color.white : new Color(1f, 1f, 1f, 0.45f)).text = "MARKET";
                if (!MarketEnabled)
                    Text("Soon", (RectTransform)_marketButton.transform, new Vector2(0f, -32f), new Vector2(640f, 40f), 26f,
                        TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.45f)).text = "YAKINDA";
            }

            // Geri: giriş ekranına (run zaten sıfır, sadece ekran değişir).
            RectTransform back = Box("BackButton", panel, new Vector2(-420f, 760f), new Vector2(120f, 100f), rounded, CardColor);
            var backImage = back.GetComponent<Image>();
            backImage.raycastTarget = true;
            var backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(() => TitleScreen.ShowTitle());
            Text("Label", back, Vector2.zero, back.sizeDelta, 56f, TextAlignmentOptions.Center, Color.white).text = "<";
        }

        private Card BuildCard(Button button, RectTransform panel, Vector2 pos, Sprite rounded)
        {
            var rect = (RectTransform)button.transform;
            Place(rect, panel, pos, new Vector2(900f, 340f));
            StyleImage(button.targetGraphic as Image, CardColor, rounded);
            foreach (Transform c in rect) c.gameObject.SetActive(false); // eski "Button" yazısı
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white; // soluklaştırmayı CanvasGroup yapar
            colors.pressedColor = new Color(0.75f, 0.75f, 0.85f, 1f);
            button.colors = colors;

            CanvasGroup group = button.gameObject.GetComponent<CanvasGroup>(); // Unity null'ı: ?? kullanılmaz
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            var card = new Card { Group = group };

            RectTransform frame = Box("PortraitFrame", rect, new Vector2(-300f, 0f), new Vector2(270f, 270f), rounded, new Color(1f, 1f, 1f, 0.06f));
            var portraitRect = NewRect("Portrait", frame, new Vector2(0f, 6f), new Vector2(250f, 250f));
            card.Portrait = portraitRect.gameObject.AddComponent<Image>();
            card.Portrait.preserveAspect = true;
            card.Portrait.raycastTarget = false;

            card.Name = Text("Name", rect, new Vector2(140f, 86f), new Vector2(560f, 90f), 70f, TextAlignmentOptions.Left, Color.white);
            card.Description = Text("Description", rect, new Vector2(140f, -34f), new Vector2(560f, 150f), 34f, TextAlignmentOptions.TopLeft, Subtle);
            card.Description.fontStyle = FontStyles.Normal;
            card.Description.textWrappingMode = TextWrappingModes.Normal;

            card.LockBadge = Box("LockBadge", rect, new Vector2(140f, -128f), new Vector2(560f, 58f), rounded, new Color(1f, 0.82f, 0.25f, 0.95f));
            card.Lock = Text("Text", card.LockBadge, Vector2.zero, card.LockBadge.sizeDelta, 30f, TextAlignmentOptions.Center,
                new Color(0.12f, 0.08f, 0.02f, 1f));
            return card;
        }

        private static void Place(RectTransform rect, RectTransform parent, Vector2 pos, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void StyleImage(Image image, Color color, Sprite sprite)
        {
            if (image == null) return;
            image.color = color;
            if (sprite != null) { image.sprite = sprite; image.type = Image.Type.Sliced; }
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            Place(rect, parent, pos, size);
            return rect;
        }

        private static RectTransform Box(string name, RectTransform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            StyleImage(image, color, sprite);
            return rect;
        }

        private static TMP_Text Text(string name, RectTransform parent, Vector2 pos, Vector2 size, float fontSize,
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

        private static void Shadowed(string name, RectTransform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color)
        {
            Text(name + "Shadow", parent, pos + new Vector2(0f, -fontSize * 0.06f), size, fontSize, TextAlignmentOptions.Center, ShadowColor).text = text;
            Text(name, parent, pos, size, fontSize, TextAlignmentOptions.Center, color).text = text;
        }
    }
}
