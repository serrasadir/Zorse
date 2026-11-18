using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] private WaveData[] _waves;

        [Header("Dakika Bazlı Zorluk Ölçekleme (GDD 5.2)")]
        [SerializeField] private float _eliteDamageThreshold = 300f;   // 5 dk
        [SerializeField] private float _eliteDamageMultiplier = 1.2f;
        [SerializeField] private float _densityThreshold = 600f;      // 10 dk
        [SerializeField] private float _densityMultiplier = 2f;
        [SerializeField] private float _speedBonusThreshold = 900f;   // 15 dk
        [SerializeField] private float _speedBonusMultiplier = 1.15f;
        [SerializeField] private float _finalDamageThreshold = 1200f; // 20 dk
        [SerializeField] private float _finalDamageMultiplier = 3f;
        // Sürü düşmanının canı _healthStartMinute'tan sonra dakikada bu kadar artar (10. dk'da x1.4). Boss'lar
        // etkilenmez (BossSpawner). İlk dakikalar sabit: polis ilk geldiğinde zayıf kalsın (VS gibi).
        [SerializeField] private float _healthPerMinute = 0.08f;
        [SerializeField] private float _healthStartMinute = 5f;

        public WaveData CurrentWave { get; private set; }
        public WaveData[] AllWaves => _waves; // test (DevCheats Y)
        public float DamageMultiplier { get; private set; } = 1f;
        public float SpeedMultiplier { get; private set; } = 1f;
        public float SpawnDensityMultiplier { get; private set; } = 1f;
        public float HealthMultiplier { get; private set; } = 1f;
        // Market Lanet'i (0..0.5): run başında GameManager yazar. Can, hız, spawn sıklığı ve tavan bununla çarpılır.
        public static float Curse { get; set; }

        private int _currentWaveIndex = -1;

        private void OnEnable()
        {
            GameEvents.OnSurvivalTimeUpdated += CheckWaveProgression;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnSurvivalTimeUpdated -= CheckWaveProgression;
            GameEvents.OnCharacterSelected -= HandleRestart;
        }

        // Dalga indeksi sadece ileri gider; sıfırlanmazsa restart son dalgadan başlar.
        private void HandleRestart(CharacterData _)
        {
            _currentWaveIndex = -1;
            CurrentWave = null;
            UpdateDifficultyScaling(0f);
        }

        private void CheckWaveProgression(float survivalTime)
        {
            UpdateDifficultyScaling(survivalTime);

            if (_waves == null || _waves.Length == 0) return;

            for (int i = _waves.Length - 1; i >= 0; i--)
            {
                WaveData wave = _waves[i];
                bool unlocked = survivalTime >= wave.TimeThreshold
                    || (wave.UnlockByBlobTier && GameEvents.CurrentBlobTier >= wave.UnlockTier);
                if (unlocked && i > _currentWaveIndex)
                {
                    _currentWaveIndex = i;
                    CurrentWave = _waves[i];
                    GameEvents.RaiseWaveStarted(CurrentWave.WaveName);
#if UNITY_EDITOR
                    Debug.Log($"[WaveController] Yeni dalga: {CurrentWave.WaveName}");
#endif
                    break;
                }
            }
        }

        private void UpdateDifficultyScaling(float survivalTime)
        {
            DamageMultiplier = survivalTime >= _finalDamageThreshold ? _finalDamageMultiplier
                : survivalTime >= _eliteDamageThreshold ? _eliteDamageMultiplier
                : 1f;

            SpawnDensityMultiplier = (survivalTime >= _densityThreshold ? _densityMultiplier : 1f) * (1f + Curse);

            SpeedMultiplier = (survivalTime >= _speedBonusThreshold ? _speedBonusMultiplier : 1f) * (1f + Curse);

            HealthMultiplier = (1f + _healthPerMinute * Mathf.Max(0f, survivalTime / 60f - _healthStartMinute)) * (1f + Curse);
        }
    }
}
