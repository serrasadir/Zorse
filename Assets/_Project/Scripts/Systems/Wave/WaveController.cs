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

        public WaveData CurrentWave { get; private set; }
        public float DamageMultiplier { get; private set; } = 1f;
        public float SpeedMultiplier { get; private set; } = 1f;
        public float SpawnDensityMultiplier { get; private set; } = 1f;

        private int _currentWaveIndex = -1;

        private void OnEnable()
        {
            GameEvents.OnSurvivalTimeUpdated += CheckWaveProgression;
        }

        private void OnDisable()
        {
            GameEvents.OnSurvivalTimeUpdated -= CheckWaveProgression;
        }

        private void CheckWaveProgression(float survivalTime)
        {
            UpdateDifficultyScaling(survivalTime);

            if (_waves == null || _waves.Length == 0) return;

            for (int i = _waves.Length - 1; i >= 0; i--)
            {
                if (survivalTime >= _waves[i].TimeThreshold && i > _currentWaveIndex)
                {
                    _currentWaveIndex = i;
                    CurrentWave = _waves[i];
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

            SpawnDensityMultiplier = survivalTime >= _densityThreshold ? _densityMultiplier : 1f;

            SpeedMultiplier = survivalTime >= _speedBonusThreshold ? _speedBonusMultiplier : 1f;
        }
    }
}
