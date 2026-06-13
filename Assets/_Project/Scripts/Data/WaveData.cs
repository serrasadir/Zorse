using UnityEngine;

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
        [SerializeField] private int _maxActiveCount;
        [SerializeField] private string _waveName;

        public float TimeThreshold => _timeThreshold;
        public EnemySpawnEntry[] EnemyTypes => _enemyTypes;
        public float SpawnRate => _spawnRate;
        public int MaxActiveCount => _maxActiveCount;
        public string WaveName => _waveName;
    }
}
