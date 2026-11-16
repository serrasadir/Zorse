using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // Üst satırda (süre kutusunun yanında) duraklatma butonu + Devam / Yeniden Başla / Ana Menü / Ayarlar paneli. Bu obje hep aktif kalır; panel ve buton child'larıdır.
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
        }

        // 2026-11-04: pause tuşu HUD'un üst satırına, süre kutusunun sağına (HUDController.LayoutHud ile aynı stil/hiza).
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

            // Dokunma alanı 120x120 (görünmez), görsel 88x88; süre kutusuyla (190x72, üstten 104) ortadan hizalı, arada 24 px.
            RectTransform rect = _pauseButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(24f + 190f + 24f + PauseVisualSize * 0.5f, -104f - 72f * 0.5f);
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
            CreatePanelButton("RestartButton", "Yeniden Başla", -200f, () => GameManager.Instance?.StartGame());
            CreatePanelButton("MenuButton", "Ana Menü", -340f, ReturnToMenu);
            Sprite rounded = _resumeButton.targetGraphic is Image ri ? ri.sprite : null;
            CreatePanelButton("SettingsButton", "Ayarlar", -480f, () => SettingsPanel.Open(this, rounded));
        }

        private void CreatePanelButton(string name, string text, float y, UnityEngine.Events.UnityAction action)
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
