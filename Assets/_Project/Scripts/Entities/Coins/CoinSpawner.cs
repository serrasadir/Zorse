using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Coins
{
    public class CoinSpawner : MonoBehaviour
    {
        public static CoinSpawner Instance { get; private set; }

        [SerializeField] private CoinPickup _coinPrefab;
        [SerializeField] private int _poolSize = 40;
        [SerializeField] private float _spawnYOffset = 0.15f; // Consumable'larla aynı yükseklik — düşman NavMeshAgent BaseOffset yüzünden zeminden yüksekte ölebiliyor

        private ObjectPool<CoinPickup> _pool;
        private ScoreSystem _scoreSystem;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_coinPrefab != null)
                _pool = new ObjectPool<CoinPickup>(_coinPrefab, _poolSize, transform);

            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
        }

        public void SpawnCoin(Vector3 position, int amount)
        {
            if (_pool == null) return;

            Vector3 spawnPos = new Vector3(position.x, _spawnYOffset, position.z);
            CoinPickup coin = _pool.Get(spawnPos, Quaternion.identity);
            coin.Initialize(amount, _scoreSystem);
            coin.OnCollected -= HandleCollected;
            coin.OnCollected += HandleCollected;
        }

        private void HandleCollected(CoinPickup coin)
        {
            _pool.Return(coin);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
