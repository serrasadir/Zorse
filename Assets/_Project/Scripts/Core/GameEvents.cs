using System;
using BlobSurvivor.Data;

namespace BlobSurvivor.Core
{
    public static class GameEvents
    {
        public static event Action<BlobTier> OnBlobTierChanged;
        // M25: consumable'ların spawn/pool-reuse anında (event'i kaçırmış olsalar bile) blob'un güncel
        // tier'ını senkron okuyabilmesi için — ConsumableBase.SetData/OnEnable bunu Find/GetComponent
        // yapmadan okur.
        public static BlobTier CurrentBlobTier { get; private set; } = BlobTier.Tiny;
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnXPChanged;
        public static event Action<int> OnLevelUp;
        public static event Action OnGameOver;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<UpgradeData[]> OnUpgradeChoicesReady;
        public static event Action<UpgradeData> OnUpgradeSelected;
        // 2026-11-03: UpgradeSystem seviyeyi artırıp efekti uyguladıktan SONRA ateşlenir (data, yeni seviye).
        // Seviyeye bakan dinleyiciler (EvolutionSystem, HUD rozetleri) bunu dinlemeli — OnUpgradeSelected'da
        // abone sırası belirsiz olduğu için seviye henüz artmamış olabiliyordu.
        public static event Action<UpgradeData, int> OnUpgradeApplied;
        public static event Action<float, float> OnHealthChanged;
        public static event Action<float> OnSurvivalTimeUpdated;
        public static event Action<CharacterData> OnCharacterSelected;
        public static event Action<int> OnCoinsChanged;
        // A15: max <= 0 => aktif boss yok (B18 health bar bunu gizleme sinyali olarak kullanır)
        public static event Action<float, float> OnBossHealthChanged;
        // A16 (Karar 1+8, §4): final boss yenebilir fazdayken yutulunca ateşlenir — "Final boss'un ölümü = yutulması".
        // A17 run-sonu yapısı bunu dinleyip run'ı "tamamlandı/zafer" olarak kapatacak.
        public static event Action OnFinalBossConsumed;
        // A17: run GameOver'dan (ölüm) ayrı olarak "başarıyla" kapanınca ateşlenir (final boss yutuldu / run-timeout).
        public static event Action<RunEndReason> OnRunComplete;

        public static void RaiseBlobTierChanged(BlobTier tier)
        {
            CurrentBlobTier = tier;
            OnBlobTierChanged?.Invoke(tier);
        }
        public static void RaiseScoreChanged(int score) => OnScoreChanged?.Invoke(score);
        public static void RaiseXPChanged(int xp) => OnXPChanged?.Invoke(xp);
        public static void RaiseLevelUp(int level) => OnLevelUp?.Invoke(level);
        public static void RaiseGameOver() => OnGameOver?.Invoke();
        public static void RaiseGamePaused() => OnGamePaused?.Invoke();
        public static void RaiseGameResumed() => OnGameResumed?.Invoke();
        public static void RaiseUpgradeChoicesReady(UpgradeData[] choices) => OnUpgradeChoicesReady?.Invoke(choices);
        public static void RaiseUpgradeSelected(UpgradeData data) => OnUpgradeSelected?.Invoke(data);
        public static void RaiseUpgradeApplied(UpgradeData data, int level) => OnUpgradeApplied?.Invoke(data, level);
        public static void RaiseHealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);
        public static void RaiseSurvivalTimeUpdated(float time) => OnSurvivalTimeUpdated?.Invoke(time);
        public static void RaiseCharacterSelected(CharacterData data) => OnCharacterSelected?.Invoke(data);
        public static void RaiseCoinsChanged(int total) => OnCoinsChanged?.Invoke(total);
        public static void RaiseBossHealthChanged(float current, float max) => OnBossHealthChanged?.Invoke(current, max);
        public static void RaiseFinalBossConsumed() => OnFinalBossConsumed?.Invoke();
        public static void RaiseRunComplete(RunEndReason reason) => OnRunComplete?.Invoke(reason);
    }

    public enum BlobTier
    {
        Tiny = 1,
        Small = 2,
        Medium = 3,
        Large = 4,
        Giant = 5,
        Colossal = 6 // oyunda "Dev"; katmanı 20 (14 Enemy dolu), bkz. TierLayers
    }

    public static class TierLayers
    {
        public const BlobTier MaxTier = BlobTier.Colossal;
        public const int Tier6Layer = 20;
        // ConsumableTier1-5 (9-13) + ConsumableTier6 (20)
        public const int ConsumableMask = (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13) | (1 << Tier6Layer);

        public static int For(BlobTier tier) => tier == BlobTier.Colossal ? Tier6Layer : 8 + (int)tier;
    }
}
