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
        public static event Action<EnemyData> OnEnemyKilled; // polis, elit, boss (silahla ya da yutularak)
        public static event Action<EnemyData, bool> OnEnemyDefeated; // OnEnemyKilled + yutularak mı
        public static event Action<BlobTier> OnBlobAte; // blob bir şey yedi (yenenin tier'ı; düşman dahil)
        public static event Action<string, string> OnMissionStarted; // anlık görev: başlık, alt satır
        public static event Action<string> OnMissionCompleted;       // ödül metni
        public static event Action OnVehicleEaten;                   // hareketli araba yutuldu
        public static event Action<string> OnSkillUnlocked;          // oynayarak açılan skill (görünen ad)
        public static event Action<string, string> OnTransformation; // özel dönüşüm başladı: başlık, alt satır
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
        // Boss spawn olduğu anda bir kez (HUD adı, final mi). Boss gitti = OnBossHealthChanged(max <= 0).
        public static event Action<string, bool> OnBossSpawned;
        // A16 (Karar 1+8, §4): final boss yenebilir fazdayken yutulunca ateşlenir — "Final boss'un ölümü = yutulması".
        // A17 run-sonu yapısı bunu dinleyip run'ı "tamamlandı/zafer" olarak kapatacak.
        public static event Action OnFinalBossConsumed;
        public static event Action OnEncirclement; // polis halkası etrafta doğdu
        public static event Action<string> OnWaveStarted; // WaveData.WaveName
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
        public static void RaiseEnemyKilled(EnemyData data) => OnEnemyKilled?.Invoke(data);
        public static void RaiseEnemyDefeated(EnemyData data, bool eaten) => OnEnemyDefeated?.Invoke(data, eaten);
        public static void RaiseBlobAte(BlobTier foodTier) => OnBlobAte?.Invoke(foodTier);
        public static void RaiseMissionStarted(string title, string sub) => OnMissionStarted?.Invoke(title, sub);
        public static void RaiseMissionCompleted(string reward) => OnMissionCompleted?.Invoke(reward);
        public static void RaiseVehicleEaten() => OnVehicleEaten?.Invoke();
        public static void RaiseSkillUnlocked(string name) => OnSkillUnlocked?.Invoke(name);
        public static void RaiseTransformation(string title, string sub) => OnTransformation?.Invoke(title, sub);
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
        public static void RaiseBossSpawned(string hudName, bool isFinal) => OnBossSpawned?.Invoke(hudName, isFinal);
        public static void RaiseFinalBossConsumed() => OnFinalBossConsumed?.Invoke();
        public static void RaiseEncirclement() => OnEncirclement?.Invoke();
        public static void RaiseWaveStarted(string waveName) => OnWaveStarted?.Invoke(waveName);
        public static void RaiseRunComplete(RunEndReason reason) => OnRunComplete?.Invoke(reason);
    }

    public enum BlobTier
    {
        Tiny = 1,
        Little = 2,  // Tiny ile Small arası; katmanı 21
        Small = 3,
        Medium = 4,
        Large = 5,
        Giant = 6,
        Colossal = 7 // oyunda "Dev"; katmanı 20 (14 Enemy dolu)
    }

    public static class TierLayers
    {
        public const BlobTier MaxTier = BlobTier.Colossal;
        public const int ColossalLayer = 20;
        public const int LittleLayer = 21;
        // Tiny 9, Small 10, Medium 11, Large 12, Giant 13, Colossal 20, Little 21 (sonradan eklenenler boş katmanlarda)
        public const int ConsumableMask = (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13) | (1 << ColossalLayer) | (1 << LittleLayer);

        public static int For(BlobTier tier)
        {
            switch (tier)
            {
                case BlobTier.Little: return LittleLayer;
                case BlobTier.Colossal: return ColossalLayer;
                case BlobTier.Tiny: return 9;
                default: return 7 + (int)tier; // Small 10 .. Giant 13
            }
        }

        // Büyüme yavaşlaması (hız = 1 / sıra ^ üs) Little eklenmeden önceki sıralamayla: denge değişmesin.
        public static float SlowdownRank(BlobTier tier) => tier == BlobTier.Tiny ? 1f : tier == BlobTier.Little ? 1.5f : (int)tier - 1;

        // Tier'a göre dizilerde (karakter yüzü) Little, Tiny'ninkini kullanır.
        public static int LegacyIndex(BlobTier tier) => tier <= BlobTier.Little ? 0 : (int)tier - 2;
    }
}
