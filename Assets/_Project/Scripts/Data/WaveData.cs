using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Data
{
    [System.Serializable]
    public struct EnemySpawnEntry
    {
        public EnemyData EnemyData;
        [Range(0f, 1f)] public float SpawnWeight;
    }

    [CreateAssetMenu(fileName = "WaveData", menuName = "BlobSurvivor/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [SerializeField] private float _timeThreshold;
        [SerializeField] private EnemySpawnEntry[] _enemyTypes;
        [SerializeField] private float _spawnRate;
        [SerializeField] private string _waveName;
        // 2026-11-03 (Serra): dalga süre eşiğinden önce, blob bu tier'a ulaştığı anda da başlayabilir
        // (elit polis: Medium'da normal polisler kaçmaya başlarken tehdidi elitler devralır).
        [SerializeField] private bool _unlockByBlobTier;
        [SerializeField] private BlobTier _unlockTier = BlobTier.Medium;

        public float TimeThreshold => _timeThreshold;
        public EnemySpawnEntry[] EnemyTypes => _enemyTypes;
        public float SpawnRate => _spawnRate;
        public string WaveName => _waveName;
        public bool UnlockByBlobTier => _unlockByBlobTier;
        public BlobTier UnlockTier => _unlockTier;
    }
}
