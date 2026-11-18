using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // Sağ üst köşede (skor/coin kutusunun sağında) duraklatma butonu + Devam / Yeniden Başla / Ana Menü / Ayarlar paneli. Bu obje hep aktif kalır; panel ve buton child'larıdır.
    // Panel OnGamePaused'da açılır: hem butonla hem uygulama arka plana atılınca (GameManager) gelir.
    // Buton sadece run sırasında görünür (lobi ve oyun sonu ekranlarında gizli).
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _resumeButton;

        private void Awake()
        {
            _pauseButton.onClick.AddListener(() => GameManager.Instance?.PauseGame());
            _resumeButton.onClick.AddListener(() => GameManager.Instance?.ResumeFromPause());
            _panel.SetActive(false);
            _pauseButton.gameObject.SetActive(false);
            CoverFullScreen();
            PlacePauseButton();
            AddPanelButtons();
            if (UiKit.Ready) ApplyKit();
        }

        // Tuş sağ üst köşede, skor kutusuyla dikeyde ortalı (HUDController.LayoutHud ile aynı stil; kutu PauseReserve kadar solda).
        private static readonly Color PauseVisualColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);
        private static readonly Color SecondaryColor = new Color(0.16f, 0.16f, 0.3f, 1f);
        private const float PauseHitSize = 120f, PauseVisualSize = 88f;

        private void PlacePauseButton()
        {
            // Kök sahnede elle kaydırılmıştı (0 genişlik, ekran dışı): buton koordinatları güvenli alana göre olsun.
            RectTransform root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Dokunma alanı 120x120 (görünmez), görsel 88x88; sağ kenardan 24 içeride.
            RectTransform rect = _pauseButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-24f - PauseVisualSize * 0.5f,
                -HUDController.ScorePanelTop - HUDController.ScorePanelHeight * 0.5f);
            rect.sizeDelta = new Vector2(PauseHitSize, PauseHitSize);

            Image hit = _pauseButton.targetGraphic as Image;
            if (hit == null) hit = _pauseButton.GetComponent<Image>();
            Sprite sprite = hit != null ? hit.sprite : null;
            Image.Type spriteType = hit != null ? hit.type : Image.Type.Simple;
            if (hit != null)
            {
                hit.color = new Color(1f, 1f, 1f, 0f); // görünmez ama ışın alır
                hit.raycastTarget = true;
            }

            var visualGo = new GameObject("Visual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            visualGo.layer = _pauseButton.gameObject.layer;
            var visualRect = (RectTransform)visualGo.transform;
            visualRect.SetParent(rect, false);
            visualRect.SetAsFirstSibling();
            visualRect.anchorMin = visualRect.anchorMax = visualRect.pivot = new Vector2(0.5f, 0.5f);
            visualRect.anchoredPosition = Vector2.zero;
            visualRect.sizeDelta = new Vector2(PauseVisualSize, PauseVisualSize);
            // Koyu yuvarlak: daire sprite'ı, sahnedeki tuşun alfası (0.55 koyu zeminde kayboluyordu).
            Image visual = visualGo.GetComponent<Image>();
            Sprite circle = Resources.Load<Sprite>("UI/Shapes/UI_Circle");
            visual.sprite = circle != null ? circle : sprite;
            visual.type = circle != null ? Image.Type.Simple : spriteType;
            visual.preserveAspect = circle != null;
            visual.color = PauseVisualColor;
            visual.raycastTarget = false;
            _pauseButton.targetGraphic = visual; // basılı tonu görsele

            // Ton çarpanı görseli soluklaştırmasın (disabled dahil beyaz); önceki tween'den kalan renk sıfırlanır.
            ColorBlock colors = _pauseButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            _pauseButton.colors = colors;
            visual.canvasRenderer.SetColor(Color.white);

            TMP_Text label = _pauseButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.SetParent(visualRect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                labelRect.localScale = Vector3.one;
                label.text = "II";
                label.fontSize = 40f;
                label.enableAutoSizing = false;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }
        }

        // Devam'ın altına Yeniden Başla + Ana Menü + Ayarlar (Devam butonunun kopyası; bir kerelik UI kurulumu).
        private void AddPanelButtons()
        {
            _restartButton = CreatePanelButton("RestartButton", "Yeniden Başla", -200f, () => GameManager.Instance?.StartGame());
            _menuButton = CreatePanelButton("MenuButton", "Ana Menü", -340f, ReturnToMenu);
            Sprite rounded = _resumeButton.targetGraphic is Image ri ? ri.sprite : null;
            _settingsButton = CreatePanelButton("SettingsButton", "Ayarlar", -480f, () => SettingsPanel.Open(this, rounded));
        }

        private Button _restartButton, _menuButton, _settingsButton;

        private Button CreatePanelButton(string name, string text, float y, UnityEngine.Events.UnityAction action)
        {
            GameObject copy = Instantiate(_resumeButton.gameObject, _resumeButton.transform.parent);
            copy.name = name;
            RectTransform rect = copy.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(_resumeButton.GetComponent<RectTransform>().anchoredPosition.x, y);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 110f);
            Button button = copy.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            if (button.targetGraphic != null) button.targetGraphic.color = SecondaryColor;
            TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = text; label.fontSize = 46f; }
            return button;
        }

        // ---------------------------------------------------------------- arayüz kiti (UiKit.Ready)

        // Mor başlıklı krem pencere (Panels/modal_purple) butonların arkasında; başlık mor şeritte.
        // Butonlar: Devam yeşil, Yeniden Başla mavi, Ana Menü gri, Ayarlar sarı; solda beyaz glif. Bir kez, Awake'te.
        private const float ModalHeaderScale = 0.65f; // 9-slice kenar çarpanı: mor şerit ~140 birim

        private void ApplyKit()
        {
            RectTransform panel = (RectTransform)_panel.transform;
            Image dim = _panel.GetComponent<Image>();
            if (dim != null) dim.color = new Color(0.02f, 0.02f, 0.08f, 0.7f);

            float x = ((RectTransform)_resumeButton.transform).anchoredPosition.x;
            Image modal = UiKit.NewImage("Modal", panel, new Vector2(x, -190f), new Vector2(620f, 840f), "Panels/modal_purple");
            modal.pixelsPerUnitMultiplier = ModalHeaderScale;
            modal.transform.SetSiblingIndex(0);

            Transform titleT = panel.Find("Title");
            TMP_Text title = titleT != null ? titleT.GetComponent<TMP_Text>() : null;
            if (title != null)
            {
                RectTransform tr = title.rectTransform;
                tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
                tr.anchoredPosition = new Vector2(x, 151f); // modal üstü 230, şerit ortası ~80 aşağıda
                tr.sizeDelta = new Vector2(540f, 110f);
                UiKit.Outlined(title, Color.white, true);
                title.enableAutoSizing = true;
                title.fontSizeMin = 40f;
                title.fontSizeMax = 62f;
                title.alignment = TextAlignmentOptions.Center;
                title.text = "DURAKLATILDI";
            }

            KitButton(_resumeButton, "green", "Glyphs/play");
            KitButton(_restartButton, "blue", "Glyphs/restart");
            KitButton(_menuButton, "gray", "Glyphs/home");
            KitButton(_settingsButton, "gold", "Glyphs/settings");

            // Sağ üst duraklatma tuşu: kutulu ikon (görsel 88x88, dokunma alanı aynı).
            if (_pauseButton.targetGraphic is Image visual && UiKit.Get("IconButtons/pause") != null)
            {
                UiKit.Paint(visual, "IconButtons/pause");
                visual.raycastTarget = false;
                TMP_Text pauseLabel = _pauseButton.GetComponentInChildren<TMP_Text>(true);
                if (pauseLabel != null) pauseLabel.gameObject.SetActive(false); // "II" yazısı ikonda var
            }
        }

        private static void KitButton(Button button, string color, string glyph)
        {
            if (button == null) return;
            UiKit.StyleButton(button, color);
            RectTransform rect = (RectTransform)button.transform;
            if (button.targetGraphic is Image img && img.sprite != null && rect.sizeDelta.y > 1f)
                img.pixelsPerUnitMultiplier = img.sprite.rect.height / rect.sizeDelta.y; // kenarlar boya orantılı

            float glyphSize = rect.sizeDelta.y * 0.5f;
            float glyphX = -rect.sizeDelta.x * 0.5f + 34f + glyphSize * 0.5f;
            UiKit.NewImage("Glyph", rect, new Vector2(glyphX, 3f), new Vector2(glyphSize, glyphSize), glyph);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            RectTransform lr = label.rectTransform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(glyphSize + 30f, 6f); // glifin sağı; kit butonunun alt gölgesi için hafif yukarı
            lr.offsetMax = Vector2.zero;
            UiKit.Outlined(label, Color.white);
            label.alignment = TextAlignmentOptions.Center;
        }

        // Ana Menü = giriş ekranı (TitleScreen); oradan OYNA ile lobiye geçilir. Run sıfırlanır.
        private void ReturnToMenu()
        {
            GameManager.Instance?.ReturnToMenu();
            HandleRunEnded();
            TitleScreen.ShowTitle();
        }

        // Panel SafeArea'nın içindeydi: çentik ve alt çubuk şeridi açık kalıyordu. Kök Canvas'a taşınıp tüm ekranı kaplar
        // (yazı/buton ortada, güvenli alanda kalır). En üstte çizilir.
        private void CoverFullScreen()
        {
            Canvas canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null) return;
            RectTransform rect = _panel.GetComponent<RectTransform>();
            rect.SetParent(canvas.rootCanvas.transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private void OnEnable()
        {
            GameEvents.OnGamePaused += ShowPanel;
            GameEvents.OnGameResumed += HidePanel;
            GameEvents.OnCharacterSelected += HandleRunStarted;
            GameEvents.OnGameOver += HandleRunEnded;
            GameEvents.OnRunComplete += HandleRunCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnGamePaused -= ShowPanel;
            GameEvents.OnGameResumed -= HidePanel;
            GameEvents.OnCharacterSelected -= HandleRunStarted;
            GameEvents.OnGameOver -= HandleRunEnded;
            GameEvents.OnRunComplete -= HandleRunCompleted;
        }

        private void ShowPanel()
        {
            _panel.SetActive(true);
            _pauseButton.gameObject.SetActive(false);
        }

        private void HidePanel()
        {
            _panel.SetActive(false);
            // Giriş ekranı da run sıfırlarken OnCharacterSelected atar: tuş sadece oyun sırasında görünsün.
            _pauseButton.gameObject.SetActive(GameManager.Instance == null || GameManager.Instance.IsPlaying);
        }

        private void HandleRunStarted(CharacterData _) => HidePanel();

        private void HandleRunEnded()
        {
            _panel.SetActive(false);
            _pauseButton.gameObject.SetActive(false);
        }

        private void HandleRunCompleted(RunEndReason _) => HandleRunEnded();
    }
}
