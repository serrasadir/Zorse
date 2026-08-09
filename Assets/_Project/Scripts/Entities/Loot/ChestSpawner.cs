using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Loot
{
    public class ChestSpawner : MonoBehaviour
    {
        public static ChestSpawner Instance { get; private set; }

        [SerializeField] private ChestPickup _chestPrefab;
        [SerializeField] private int _poolSize = 8;
        [SerializeField] private float _spawnYOffset = 0.35f;

        private ObjectPool<ChestPickup> _pool;
        private ScoreSystem _scoreSystem;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_chestPrefab != null)
            {
                _pool = PoolManager.Instance != null
                    ? PoolManager.Instance.CreatePool(_chestPrefab, _poolSize)
                    : new ObjectPool<ChestPickup>(_chestPrefab, _poolSize, transform);
            }

            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
        }

        public void SpawnChest(Vector3 position)
        {
            if (_pool == null) return;

            Vector3 spawnPos = new Vector3(position.x, _spawnYOffset, position.z);
            ChestPickup chest = _pool.Get(spawnPos, Quaternion.identity);
            chest.Initialize(_scoreSystem);
            chest.OnCollected -= HandleCollected;
            chest.OnCollected += HandleCollected;
        }

        private void HandleCollected(ChestPickup chest)
        {
            _pool.Return(chest);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
