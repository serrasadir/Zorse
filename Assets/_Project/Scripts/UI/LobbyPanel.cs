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

        // Market'e giriş; Market'in geri tuşu Show()'u çağırır.
        [SerializeField] private Button _marketButton;
        [SerializeField] private MarketPanel _marketPanel;

        private static readonly Color Backdrop = new Color(0.05f, 0.05f, 0.17f, 0.7f);
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.82f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.78f);
        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);

        private class Card
        {
            public Image Portrait;
            public TMP_Text Name, Description, Lock;
            public RectTransform LockBadge;
            public GameObject LockIcon; // kit: portre köşesinde kilit rozeti
            public CanvasGroup Group;
        }

        private Card[] _cards;
        private TMP_Text _credit;
        private bool _built;
        private float _cardHeight;
        // Arayüz kiti (UiKit): krem kartlar, kırmızı başlık plakası, altın coin hapı. Kurulu değilse eski gece mavisi düzen.
        private bool _k;
        private static readonly Color DescInk = new Color32(58, 40, 30, 255);

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

            if (_marketButton != null)
                _marketButton.onClick.AddListener(OpenMarket);
            if (MetaProgression.Instance != null) MetaProgression.Instance.Changed += RefreshCharacterButtons;

            RefreshCharacterButtons();
        }

        private void OnDestroy()
        {
            if (MetaProgression.Instance != null) MetaProgression.Instance.Changed -= RefreshCharacterButtons;
        }

        public RectTransform PanelRect => _panel != null ? (RectTransform)_panel.transform : null;

        // Market'in Karakterler sekmesi bu sırayla listeler (ShownInMarket olanlar).
        public CharacterData[] Characters => _characters;

        // Market'in geri tuşu bunu çağırır. Market'te karakter satın alınmış olabileceğinden
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

        // Kilitler: Market'te coin'le (CharacterData.MarketPrice) ya da run sayısıyla (RequiredCompletedRuns,
        // ölüm de sayılır; Dalcık). Kilitliyken kart soluk, koşul rozette yazar.
        private void RefreshCharacterButtons()
        {
            Build();
            MetaProgression meta = MetaProgression.Instance;
            int credit = meta != null ? meta.Credit : 0;
            if (_credit != null) _credit.text = _k ? $"{credit:N0}" : $"{credit:N0} coin";

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (i >= _characters.Length || _characters[i] == null) continue;
                CharacterData data = _characters[i];
                bool marketLocked = meta != null && !meta.IsCharacterPurchased(data);
                int runsLeft = meta != null ? meta.CharacterRunsLeft(data) : 0;
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
                if (card.LockIcon != null) card.LockIcon.SetActive(locked);
                card.Lock.text = $"KİLİTLİ · {lockReason}";
                card.Group.alpha = locked ? 0.55f : 1f;
            }
        }

        private void OpenMarket()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_marketPanel != null) _marketPanel.Show(this);
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
            _k = UiKit.Ready;
            var panel = (RectTransform)_panel.transform;

            Image backdrop = _panel.GetComponent<Image>();
            Sprite rounded = _marketButton != null && _marketButton.targetGraphic is Image mi ? mi.sprite : null;
            if (backdrop != null) { backdrop.sprite = null; backdrop.color = Color.clear; }
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.anchoredPosition = Vector2.zero; panel.sizeDelta = Vector2.zero;
            ScreenBackdrop(panel, Backdrop);

            if (_k)
            {
                KitTitle(panel, new Vector2(0f, 760f), "KARAKTERİNİ SEÇ");
                _credit = KitCoinChip(panel, new Vector2(0f, 650f), new Vector2(360f, 72f), out _);
            }
            else
            {
                Shadowed("Title", panel, new Vector2(0f, 760f), new Vector2(1000f, 120f), "KARAKTERİNİ SEÇ", 92f, Color.white);
                RectTransform chip = Box("CreditChip", panel, new Vector2(0f, 650f), new Vector2(360f, 72f), rounded, CardColor);
                _credit = Text("Credit", chip, Vector2.zero, chip.sizeDelta, 38f, TextAlignmentOptions.Center, Gold);
            }

            // Sahnede 3 buton var; fazla karakter için ilki bir kez kopyalanır (listener'lar Start'ta bağlanır).
            if (_buttons.Length > 0 && _buttons[0] != null && _characters.Length > _buttons.Length)
            {
                var all = new Button[_characters.Length];
                for (int i = 0; i < all.Length; i++)
                    all[i] = i < _buttons.Length ? _buttons[i] : Instantiate(_buttons[0], _buttons[0].transform.parent);
                _buttons = all;
            }

            // 3 karta kadar büyük kart; fazlası başlık ile Market düğmesi arasına sığacak kadar alçak. Yerleri LayoutCards'ta.
            int count = _buttons.Length;
            _cardHeight = count <= 3 ? 340f : 240f;
            _cards = new Card[count];
            for (int i = 0; i < count; i++)
                if (_buttons[i] != null) _cards[i] = BuildCard(_buttons[i], panel, Vector2.zero, _cardHeight, rounded);

            if (_marketButton != null)
            {
                Place((RectTransform)_marketButton.transform, panel, new Vector2(0f, -800f), new Vector2(640f, 120f));
                StyleImage(_marketButton.targetGraphic as Image, Gold, rounded);
                foreach (Transform c in _marketButton.transform) c.gameObject.SetActive(false);
                _marketButton.interactable = true;
                TMP_Text marketLabel = Text("Label", (RectTransform)_marketButton.transform, Vector2.zero, new Vector2(640f, 70f), 50f,
                    TextAlignmentOptions.Center, new Color(0.12f, 0.08f, 0.02f, 1f));
                marketLabel.text = "MARKET";
                if (_k)
                {
                    UiKit.StyleButton(_marketButton, "gold");
                    if (_marketButton.targetGraphic is Image marketImage) marketImage.pixelsPerUnitMultiplier = 1.15f; // 120 boy: alt/üst kenar sığsın
                    marketLabel.rectTransform.anchoredPosition = new Vector2(0f, 5f);
                    UiKit.Outlined(marketLabel, Color.white, true);
                    marketLabel.fontSize = 56f;
                }
            }

            // Geri: giriş ekranına (run zaten sıfır, sadece ekran değişir).
            Vector2 iconSize = _k ? new Vector2(112f, 112f) : new Vector2(120f, 100f);
            RectTransform back = Box("BackButton", panel, new Vector2(-420f, 885f), iconSize, rounded, CardColor);
            var backImage = back.GetComponent<Image>();
            backImage.raycastTarget = true;
            var backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(() => TitleScreen.ShowTitle());
            if (_k) UiKit.StyleIconButton(backButton, "back");
            else Text("Label", back, Vector2.zero, back.sizeDelta, 56f, TextAlignmentOptions.Center, Color.white).text = "<";

            // Ayarlar (dişli): sağ üstte, geri tuşunun karşısında.
            RectTransform gear = Box("SettingsButton", panel, new Vector2(420f, 885f), iconSize, rounded, CardColor);
            var gearImage = gear.GetComponent<Image>();
            gearImage.raycastTarget = true;
            var gearButton = gear.gameObject.AddComponent<Button>();
            gearButton.targetGraphic = gearImage;
            gearButton.onClick.AddListener(() => SettingsPanel.Open(this, rounded));
            if (_k) UiKit.StyleIconButton(gearButton, "settings");
            else
            {
                Image gearIcon = NewRect("Icon", gear, Vector2.zero, new Vector2(64f, 64f)).gameObject.AddComponent<Image>();
                gearIcon.sprite = Resources.Load<Sprite>("UI/Shapes/UI_Gear");
                gearIcon.preserveAspect = true;
                gearIcon.raycastTarget = false;
            }

            PinToEdge(panel, 1f, "Title", "TitleShadow", "CreditChip", "BackButton", "SettingsButton");
            if (_marketButton != null) PinToEdge((RectTransform)_marketButton.transform, 0f);
            LayoutCards();
        }

        // ---------------------------------------------------------------- kit parçaları (Market de kullanır)

        // Başlık: kırmızı plaka + beyaz konturlu yazı. Obje adı "Title" (PinToEdge bulur).
        internal static void KitTitle(RectTransform panel, Vector2 pos, string text, float width = 780f)
        {
            Image plate = UiKit.NewImage("Title", panel, pos, new Vector2(width, 146f), "Banners/plate_red");
            UiKit.FitBorder(plate);
            TMP_Text t = Text("Label", plate.rectTransform, new Vector2(0f, 4f), new Vector2(width - 140f, 108f), 68f, TextAlignmentOptions.Center, Color.white);
            UiKit.Outlined(t, Color.white, true);
            t.enableAutoSizing = true;
            t.fontSizeMin = 44f;
            t.fontSizeMax = 68f;
            t.text = text;
        }

        // Coin hapı: altın hap, solda coin ikonu, yanında sayı (beyaz, konturlu). Obje adı "CreditChip".
        internal static TMP_Text KitCoinChip(RectTransform panel, Vector2 pos, Vector2 size, out RectTransform chip)
        {
            Image pill = UiKit.NewImage("CreditChip", panel, pos, size, "Pills/pill_gold");
            UiKit.FitBorder(pill);
            chip = pill.rectTransform;
            float icon = size.y * 0.9f;
            UiKit.NewImage("Coin", chip, new Vector2(-size.x * 0.5f + icon * 0.5f + 12f, 0f), new Vector2(icon, icon), "Icons/coin");
            TMP_Text t = Text("Credit", chip, new Vector2(icon * 0.5f, 3f), new Vector2(size.x - icon - 40f, size.y), 40f, TextAlignmentOptions.Center, Color.white);
            UiKit.Outlined(t, Color.white);
            return t;
        }

        // Ekran dönünce (panel boyu değişince) kartlar yeniden dizilir.
        private void OnRectTransformDimensionsChange()
        {
            if (_built) LayoutCards();
        }

        // Dik ekranda tek sütun (1920'de eski yerleri). Yan ekranda 3'ten fazla kart iki sütuna dizilir (tek kalan ortada),
        // coin kutusu ile Market düğmesi arasına ortalanır; yine sığmazsa küçülür.
        private const float ChipBottomFromTop = 960f - 650f + 36f, MarketTopFromBottom = 960f - 800f + 60f, LayoutMargin = 20f;
        private const float ColumnOffset = 480f;

        private void LayoutCards()
        {
            if (_buttons == null || _buttons.Length == 0) return;
            Rect area = ((RectTransform)_panel.transform).rect;
            int count = _buttons.Length;
            bool twoColumns = area.width > area.height && count > 3;
            float pitch = count <= 3 ? 390f : 260f;
            int rows = twoColumns ? (count + 1) / 2 : count;
            float stack = (rows - 1) * pitch + _cardHeight;

            float top = area.height * 0.5f - ChipBottomFromTop - LayoutMargin, bottom = -area.height * 0.5f + MarketTopFromBottom + LayoutMargin;
            float scale = Mathf.Min(1f, (top - bottom) / stack);
            // Tek sütunda 1920'deki eski yerleri: 3 kart 330'dan, fazlası −65 merkezli.
            float center = twoColumns ? (top + bottom) * 0.5f : count <= 3 ? 330f - (count - 1) * 0.5f * pitch : -65f;
            if (scale < 1f) center = (top + bottom) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                if (_buttons[i] == null) continue;
                int row = twoColumns ? i / 2 : i;
                float x = 0f;
                if (twoColumns && !(i == count - 1 && count % 2 == 1)) x = i % 2 == 0 ? -ColumnOffset : ColumnOffset;
                float y = center + ((rows - 1) * 0.5f - row) * pitch * scale;
                var rect = (RectTransform)_buttons[i].transform;
                rect.anchoredPosition = new Vector2(x * scale, y);
                rect.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // Düzen 1080x1920'ye göre çizildi. Uzun ekranda başlık bloğu üste, Market düğmesi alta yapışır (1920'de konum aynı).
        internal static void PinToEdge(RectTransform panel, float edge, params string[] names)
        {
            foreach (string n in names)
                if (panel.Find(n) is RectTransform r) PinToEdge(r, edge);
        }

        internal static void PinToEdge(RectTransform r, float edge)
        {
            Vector2 p = r.anchoredPosition;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, edge);
            p.y += edge > 0.5f ? -960f : 960f;
            r.anchoredPosition = p;
        }

        // Panel güvenli alanın içinde; arka plan çentik ve alt çubuk şeridini de kaplasın diye taşar (ekran dışı kırpılır).
        internal static void ScreenBackdrop(RectTransform panel, Color color)
        {
            var go = new GameObject("ScreenBackdrop", typeof(RectTransform));
            go.layer = panel.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(panel, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-400f, -600f); rect.offsetMax = new Vector2(400f, 600f);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private Card BuildCard(Button button, RectTransform panel, Vector2 pos, float height, Sprite rounded)
        {
            var rect = (RectTransform)button.transform;
            Place(rect, panel, pos, new Vector2(900f, height));
            float k = height / 340f;
            if (_k && button.targetGraphic is Image cardImage) { UiKit.Paint(cardImage, "Panels/card_cream"); cardImage.raycastTarget = true; }
            else StyleImage(button.targetGraphic as Image, CardColor, rounded);
            foreach (Transform c in rect) c.gameObject.SetActive(false); // eski "Button" yazısı
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white; // soluklaştırmayı CanvasGroup yapar
            colors.pressedColor = new Color(0.75f, 0.75f, 0.85f, 1f);
            button.colors = colors;

            CanvasGroup group = button.gameObject.GetComponent<CanvasGroup>(); // Unity null'ı: ?? kullanılmaz
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            var card = new Card { Group = group };

            Vector2 framePos = new Vector2(-300f - (1f - k) * 50f, 0f);
            RectTransform frame = _k
                ? UiKit.NewImage("PortraitFrame", rect, framePos, new Vector2(270f, 270f) * k, "Slots/slot_passive").rectTransform
                : Box("PortraitFrame", rect, framePos, new Vector2(270f, 270f) * k, rounded, new Color(1f, 1f, 1f, 0.06f));
            var portraitRect = NewRect("Portrait", frame, new Vector2(0f, 6f * k), (_k ? new Vector2(220f, 220f) : new Vector2(250f, 250f)) * k);
            card.Portrait = portraitRect.gameObject.AddComponent<Image>();
            card.Portrait.preserveAspect = true;
            card.Portrait.raycastTarget = false;

            card.Name = Text("Name", rect, new Vector2(140f, 86f * k), new Vector2(560f, 90f * k), 70f * Mathf.Sqrt(k), TextAlignmentOptions.Left, Color.white);
            card.Description = Text("Description", rect, new Vector2(140f, -34f * k), new Vector2(560f, 150f * k), 34f * Mathf.Sqrt(k), TextAlignmentOptions.TopLeft, Subtle);
            card.Description.fontStyle = FontStyles.Normal;
            card.Description.textWrappingMode = TextWrappingModes.Normal;

            if (_k)
            {
                // Krem kartta koyu mürekkep; kilit: gri hap + portre köşesinde kilit rozeti.
                UiKit.Text(card.Name, true, UiKit.Ink);
                UiKit.Text(card.Description, false, DescInk);
                var badgeSize = new Vector2(560f, 64f * Mathf.Sqrt(k));
                Image pill = UiKit.NewImage("LockBadge", rect, new Vector2(140f, -128f * k), badgeSize, "Pills/pill_gray");
                UiKit.FitBorder(pill);
                card.LockBadge = pill.rectTransform;
                card.Lock = Text("Text", card.LockBadge, new Vector2(0f, 3f), new Vector2(520f, badgeSize.y), 30f * Mathf.Sqrt(k),
                    TextAlignmentOptions.Center, Color.white);
                UiKit.Outlined(card.Lock, Color.white, false, false);
                float lockSize = 110f * k;
                card.LockIcon = UiKit.NewImage("LockIcon", frame, new Vector2(95f * k, -95f * k), new Vector2(lockSize, lockSize * 171f / 219f),
                    "Badges/badge_lock").gameObject;
                return card;
            }

            card.LockBadge = Box("LockBadge", rect, new Vector2(140f, -128f * k), new Vector2(560f, 58f * Mathf.Sqrt(k)), rounded, new Color(1f, 0.82f, 0.25f, 0.95f));
            card.Lock = Text("Text", card.LockBadge, Vector2.zero, card.LockBadge.sizeDelta, 30f * Mathf.Sqrt(k), TextAlignmentOptions.Center,
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
