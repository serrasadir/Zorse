using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Systems;

namespace BlobSurvivor.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _highScoreText;
        [SerializeField] private TMP_Text _summaryText;
        [SerializeField] private TMP_Text _newRecordText;
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
            if (_panel != null)
                _panel.SetActive(false);

            _restartButton?.onClick.AddListener(Restart);
            EnsureSummaryFields();
        }

        private void Show()
        {
            if (_panel != null)
                _panel.SetActive(true);

            EnsureSummaryFields();

            ScoreSystem score = FindAnyObjectByType<ScoreSystem>();
            if (score == null) return;

            if (_scoreText != null)
                _scoreText.text = $"Skor: {score.CurrentScore:N0}";

            if (_highScoreText != null)
                _highScoreText.text = $"Highscore: {score.HighScore:N0}";

            if (_summaryText != null)
            {
                float survivalTime = GameManager.Instance != null ? GameManager.Instance.SurvivalTime : 0f;
                int minutes = (int)survivalTime / 60;
                int seconds = (int)survivalTime % 60;
                string highScoreState = score.HasNewHighScore ? "yeni rekor" : "kirilamadi";

                _summaryText.text =
                    $"Coin: {score.Coins:N0}\n" +
                    $"Sure: {minutes:00}:{seconds:00}\n" +
                    $"Onceki Rekor: {score.PreviousHighScore:N0} ({highScoreState})";
            }

            if (_newRecordText != null)
            {
                _newRecordText.gameObject.SetActive(score.HasNewHighScore);
                _newRecordText.text = "YENI REKOR!";
            }
        }

        private void Restart()
        {
            GameManager.Instance?.StartGame();
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void EnsureSummaryFields()
        {
            if (_panel == null) return;

            ConfigureBoundText(_scoreText, new Vector2(0f, 10f), 20f);
            ConfigureBoundText(_highScoreText, new Vector2(0f, -22f), 16f);

            if (_summaryText == null)
            {
                GameObject summaryObject = new GameObject("SessionSummaryText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                summaryObject.transform.SetParent(_panel.transform, false);
                _summaryText = summaryObject.GetComponent<TMP_Text>();
                _summaryText.fontSize = 18f;
                _summaryText.alignment = TextAlignmentOptions.Center;
                _summaryText.color = Color.white;
                _summaryText.enableWordWrapping = false;

                RectTransform rect = summaryObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -150f);
                rect.sizeDelta = new Vector2(360f, 90f);
            }

            if (_newRecordText == null)
            {
                GameObject recordObject = new GameObject("NewRecordText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                recordObject.transform.SetParent(_panel.transform, false);
                _newRecordText = recordObject.GetComponent<TMP_Text>();
                _newRecordText.fontSize = 22f;
                _newRecordText.fontStyle = FontStyles.Bold;
                _newRecordText.alignment = TextAlignmentOptions.Center;
                _newRecordText.color = new Color(1f, 0.82f, 0.25f, 1f);

                RectTransform rect = recordObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 120f);
                rect.sizeDelta = new Vector2(360f, 36f);
            }
        }

        private static void ConfigureBoundText(TMP_Text text, Vector2 anchoredPosition, float fontSize)
        {
            if (text == null) return;

            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(360f, 30f);
        }
    }
}
