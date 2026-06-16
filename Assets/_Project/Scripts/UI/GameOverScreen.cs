using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;

namespace BlobSurvivor.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _highScoreText;
        [SerializeField] private Button _restartButton;

        private void OnEnable()
        {
            GameEvents.OnGameOver += Show;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= Show;
        }

        private void Start()
        {
            if (_panel != null) _panel.SetActive(false);
            _restartButton?.onClick.AddListener(Restart);
        }

        private void Show()
        {
            if (_panel != null) _panel.SetActive(true);

            var score = FindAnyObjectByType<Systems.ScoreSystem>();
            if (score == null) return;

            if (_scoreText != null)
                _scoreText.text = $"SKOR: {score.CurrentScore:N0}";

            if (_highScoreText != null)
                _highScoreText.text = $"EN İYİ: {score.HighScore:N0}";
        }

        private void Restart()
        {
            GameManager.Instance?.StartGame();
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
