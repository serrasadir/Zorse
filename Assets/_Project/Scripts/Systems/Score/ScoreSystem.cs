using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Systems
{
    public class ScoreSystem : MonoBehaviour
    {
        private const string HighScoreKey = "HighScore";

        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public float ScoreMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public void AddScore(int baseAmount)
        {
            CurrentScore += Mathf.RoundToInt(baseAmount * ScoreMultiplier);
            GameEvents.RaiseScoreChanged(CurrentScore);

            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
            }
        }

        public void SetMultiplier(float multiplier) => ScoreMultiplier = Mathf.Max(1f, multiplier);

        public void ResetScore()
        {
            CurrentScore = 0;
            ScoreMultiplier = 1f;
            GameEvents.RaiseScoreChanged(CurrentScore);
        }
    }
}
