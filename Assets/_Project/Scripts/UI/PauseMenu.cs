using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // Üst satırda (süre kutusunun yanında) duraklatma butonu + Devam / Yeniden Başla / Ana Menü paneli. Bu obje hep aktif kalır; panel ve buton child'larıdır.
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
        private static readonly Color HudPanelColor = new Color(0.05f, 0.05f, 0.17f, 0.55f);
        private static readonly Color SecondaryColor = new Color(0.16f, 0.16f, 0.3f, 1f);

        private void PlacePauseButton()
        {
            // Kök sahnede elle kaydırılmıştı (0 genişlik, ekran dışı): buton koordinatları güvenli alana göre olsun.
            RectTransform root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            RectTransform rect = _pauseButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f + 190f + 12f, -104f);
            rect.sizeDelta = new Vector2(72f, 72f);
            if (_pauseButton.targetGraphic != null) _pauseButton.targetGraphic.color = HudPanelColor;
            TMP_Text label = _pauseButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.fontSize = 34f;
        }

        // Devam'ın altına Yeniden Başla + Ana Menü (Devam butonunun kopyası; bir kerelik UI kurulumu).
        private void AddPanelButtons()
        {
            CreatePanelButton("RestartButton", "Yeniden Başla", -200f, () => GameManager.Instance?.StartGame());
            CreatePanelButton("MenuButton", "Ana Menü", -340f, ReturnToMenu);
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
