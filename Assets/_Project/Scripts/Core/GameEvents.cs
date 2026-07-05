using System;
using BlobSurvivor.Data;

namespace BlobSurvivor.Core
{
    public static class GameEvents
    {
        public static event Action<float> OnBlobSizeChanged;
        public static event Action<BlobTier> OnBlobTierChanged;
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnXPChanged;
        public static event Action<int> OnLevelUp;
        public static event Action OnGameOver;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<UpgradeData[]> OnUpgradeChoicesReady;
        public static event Action<UpgradeData> OnUpgradeSelected;
        public static event Action<float, float> OnHealthChanged;
        public static event Action<float> OnSurvivalTimeUpdated;
        public static event Action<int> OnConsumedCountChanged;
        public static event Action<CharacterData> OnCharacterSelected;

        public static void RaiseBlobSizeChanged(float newSize) => OnBlobSizeChanged?.Invoke(newSize);
        public static void RaiseBlobTierChanged(BlobTier tier) => OnBlobTierChanged?.Invoke(tier);
        public static void RaiseScoreChanged(int score) => OnScoreChanged?.Invoke(score);
        public static void RaiseXPChanged(int xp) => OnXPChanged?.Invoke(xp);
        public static void RaiseLevelUp(int level) => OnLevelUp?.Invoke(level);
        public static void RaiseGameOver() => OnGameOver?.Invoke();
        public static void RaiseGamePaused() => OnGamePaused?.Invoke();
        public static void RaiseGameResumed() => OnGameResumed?.Invoke();
        public static void RaiseUpgradeChoicesReady(UpgradeData[] choices) => OnUpgradeChoicesReady?.Invoke(choices);
        public static void RaiseUpgradeSelected(UpgradeData data) => OnUpgradeSelected?.Invoke(data);
        public static void RaiseHealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);
        public static void RaiseSurvivalTimeUpdated(float time) => OnSurvivalTimeUpdated?.Invoke(time);
        public static void RaiseConsumedCountChanged(int count) => OnConsumedCountChanged?.Invoke(count);
        public static void RaiseCharacterSelected(CharacterData data) => OnCharacterSelected?.Invoke(data);
    }

    public enum BlobTier
    {
        Tiny = 1,
        Small = 2,
        Medium = 3,
        Large = 4,
        Giant = 5
    }
}
