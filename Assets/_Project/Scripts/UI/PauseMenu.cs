using UnityEngine;
using UnityEngine.UI;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // Sağ üst duraklatma butonu + "Devam" paneli. Bu obje hep aktif kalır; panel ve buton child'larıdır.
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
            _pauseButton.gameObject.SetActive(true);
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
