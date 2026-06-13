using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] private WaveData[] _waves;

        public WaveData CurrentWave { get; private set; }
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
    }
}
