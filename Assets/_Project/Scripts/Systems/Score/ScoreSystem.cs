using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Systems
{
    // Run verisi (RunAnalytics): coin'in nereden geldiği.
    public enum CoinSource { Police, Elite, Boss, Chest }

    public class ScoreSystem : MonoBehaviour
    {
        private const string HighScoreKey = "HighScore";

        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public int PreviousHighScore { get; private set; }
        public bool HasNewHighScore { get; private set; }
        public int Coins { get; private set; }

        // M22: Market'ten satın alınan "Coin Kazanımı" kalıcı bonusu (GameManager.ApplyMetaProgression).
        private float _coinGainMultiplier = 1f;
        // Çarpanın kesiri birikir: 1 coin × 1.05 her seferinde 1'e yuvarlanıp bonus kayboluyordu.
        private float _coinFraction;

        private readonly int[] _coinsBySource = new int[4];
        public int CoinsFrom(CoinSource source) => _coinsBySource[(int)source];
        public int ExpiredCoins { get; private set; } // yerde kalıp kaybolan (çarpansız)

        private void Awake()
        {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            PreviousHighScore = HighScore;
        }

        public void AddScore(int baseAmount)
        {
            CurrentScore += baseAmount;
            GameEvents.RaiseScoreChanged(CurrentScore);

            if (CurrentScore > HighScore)
            {
                HasNewHighScore = true;
                HighScore = CurrentScore;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
            }
        }

        public void AddCoin(int amount, CoinSource source)
        {
            if (amount <= 0) return;
            _coinFraction += amount * _coinGainMultiplier;
            int whole = Mathf.FloorToInt(_coinFraction);
            _coinFraction -= whole;
            Coins += whole;
            _coinsBySource[(int)source] += whole;
            GameEvents.RaiseCoinsChanged(Coins);
        }

        public void RegisterExpiredCoin(int amount) => ExpiredCoins += Mathf.Max(0, amount);

        public void SetCoinGainMultiplier(float multiplier) => _coinGainMultiplier = Mathf.Max(1f, multiplier);

        public void ResetScore()
        {
            CurrentScore = 0;
            Coins = 0;
            _coinGainMultiplier = 1f;
            _coinFraction = 0f;
            System.Array.Clear(_coinsBySource, 0, _coinsBySource.Length);
            ExpiredCoins = 0;
            PreviousHighScore = HighScore;
            HasNewHighScore = false;
            GameEvents.RaiseScoreChanged(CurrentScore);
            GameEvents.RaiseCoinsChanged(Coins);
        }
    }
}
