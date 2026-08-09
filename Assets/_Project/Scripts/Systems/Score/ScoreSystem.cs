using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Systems
{
    public class ScoreSystem : MonoBehaviour
    {
        private const string HighScoreKey = "HighScore";

        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public int PreviousHighScore { get; private set; }
        public bool HasNewHighScore { get; private set; }
        public float ScoreMultiplier { get; private set; } = 1f;
        public int Coins { get; private set; }

        private void Awake()
        {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            PreviousHighScore = HighScore;
        }

        public void AddScore(int baseAmount)
        {
            CurrentScore += Mathf.RoundToInt(baseAmount * ScoreMultiplier);
            GameEvents.RaiseScoreChanged(CurrentScore);

            if (CurrentScore > HighScore)
            {
                HasNewHighScore = true;
                HighScore = CurrentScore;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
            }
        }

        public void SetMultiplier(float multiplier) => ScoreMultiplier = Mathf.Max(1f, multiplier);

        public void AddCoin(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            GameEvents.RaiseCoinsChanged(Coins);
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            ScoreMultiplier = 1f;
            Coins = 0;
            PreviousHighScore = HighScore;
            HasNewHighScore = false;
            GameEvents.RaiseScoreChanged(CurrentScore);
            GameEvents.RaiseCoinsChanged(Coins);
        }
    }
}
