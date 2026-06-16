using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;

namespace BlobSurvivor.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Slider _healthBar;

        [Header("Score & Timer")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _tierText;

        [Header("XP")]
        [SerializeField] private Slider _xpBar;
        [SerializeField] private TMP_Text _levelText;

        private float _survivalTime;

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += OnHealthChanged;
            GameEvents.OnScoreChanged += OnScoreChanged;
            GameEvents.OnSurvivalTimeUpdated += OnSurvivalTimeUpdated;
            GameEvents.OnBlobTierChanged += OnTierChanged;
            GameEvents.OnXPChanged += OnXPChanged;
            GameEvents.OnLevelUp += OnLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= OnHealthChanged;
            GameEvents.OnScoreChanged -= OnScoreChanged;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTimeUpdated;
            GameEvents.OnBlobTierChanged -= OnTierChanged;
            GameEvents.OnXPChanged -= OnXPChanged;
            GameEvents.OnLevelUp -= OnLevelUp;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (_healthBar != null)
            {
                _healthBar.maxValue = max;
                _healthBar.value = current;
            }
        }

        private void OnScoreChanged(int score)
        {
            if (_scoreText != null)
                _scoreText.text = score.ToString("N0");
        }

        private void OnSurvivalTimeUpdated(float time)
        {
            _survivalTime = time;
            if (_timerText != null)
            {
                int minutes = (int)time / 60;
                int seconds = (int)time % 60;
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void OnTierChanged(BlobTier tier)
        {
            if (_tierText != null)
                _tierText.text = tier.ToString().ToUpper();
        }

        private void OnXPChanged(int xp)
        {
            if (_xpBar == null) return;
            var blob = FindAnyObjectByType<Entities.Blob.BlobGrowth>();
            if (blob == null) return;
            _xpBar.maxValue = blob.XPThreshold;
            _xpBar.value = blob.CurrentXP;
        }

        private void OnLevelUp(int level)
        {
            if (_levelText != null)
                _levelText.text = $"Lv.{level}";
        }
    }
}
