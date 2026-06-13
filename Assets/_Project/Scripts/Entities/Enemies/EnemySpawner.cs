using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private WaveController _waveController;
        [SerializeField] private float _spawnDistance = 20f;
        [SerializeField] private int _maxActiveEnemies = 30;

        private Transform _blobTransform;
        private float _spawnTimer;
        private readonly List<GameObject> _activeEnemies = new List<GameObject>();
        private readonly Dictionary<EnemyData, ObjectPool<EnemyBase>> _pools = new Dictionary<EnemyData, ObjectPool<EnemyBase>>();

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _blobTransform = blob.transform;

            GameEvents.OnBlobTierChanged += OnTierChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnBlobTierChanged -= OnTierChanged;
        }

        private void Update()
        {
            if (_waveController?.CurrentWave == null) return;
            if (!IsGamePlaying()) return;

            CleanupInactive();

            if (_activeEnemies.Count >= _maxActiveEnemies) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _waveController.CurrentWave.SpawnRate)
            {
                _spawnTimer = 0f;
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            WaveData wave = _waveController.CurrentWave;
            if (wave.EnemyTypes == null || wave.EnemyTypes.Length == 0) return;

            EnemyData data = SelectEnemyData(wave);
            if (data?.Prefab == null) return;

            EnemyBase prefabBase = data.Prefab.GetComponent<EnemyBase>();
            if (prefabBase == null) return;

            if (!_pools.ContainsKey(data))
                _pools[data] = PoolManager.Instance.CreatePool(prefabBase, 5);

            Vector3 spawnPos = GetSpawnPosition();
            EnemyBase enemy = _pools[data].Get(spawnPos, Quaternion.identity);
            enemy.SetData(data);
            _activeEnemies.Add(enemy.gameObject);
        }

        private EnemyData SelectEnemyData(WaveData wave)
        {
            float totalWeight = 0f;
            foreach (var entry in wave.EnemyTypes) totalWeight += entry.SpawnWeight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var entry in wave.EnemyTypes)
            {
                cumulative += entry.SpawnWeight;
                if (roll <= cumulative) return entry.EnemyData;
            }
            return wave.EnemyTypes[0].EnemyData;
        }

        private const float EnemySpawnY = 0.65f;

        private Vector3 GetSpawnPosition()
        {
            Vector3 center = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            Vector2 random = Random.insideUnitCircle.normalized * _spawnDistance;
            Vector3 candidate = new Vector3(center.x + random.x, EnemySpawnY, center.z + random.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                return new Vector3(hit.position.x, EnemySpawnY, hit.position.z);

            return candidate;
        }

        private void OnTierChanged(BlobTier tier)
        {
            _maxActiveEnemies = Mathf.Min(5 * (int)tier + 5, 30);
        }

        private void CleanupInactive()
        {
            _activeEnemies.RemoveAll(go => go == null || !go.activeInHierarchy);
        }

        private bool IsGamePlaying()
        {
            return GameManager.Instance == null || GameManager.Instance.IsPlaying;
        }
    }
}
