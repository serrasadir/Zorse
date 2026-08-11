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
        [SerializeField] private int _maxActiveEnemies = 50;
        // Elit/boss NavMeshAgent kullanır (pahalı) — GDD_v2.md §7 Mimari: "aynı anda ≤8-10 agent".
        // Sürü tavanından ayrı tutulur, aksi halde weighted-random spawn elit sayısını da yükseltebilir.
        [SerializeField] private int _maxActiveElites = 8;

        private Transform _blobTransform;
        private float _spawnTimer;
        private int _activeEliteCount;
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

            if (_activeEnemies.Count >= GetEffectiveMaxActive()) return;

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

            // Elit tavanı sürüden ayrı — NavMeshAgent pahalı, weighted-random spawn'ın
            // elit sayısını kontrolsüz yükseltmesini engeller (GDD Karar 2).
            if (data.IsElite && _activeEliteCount >= _maxActiveElites) return;

            EnemyBase prefabBase = data.Prefab.GetComponent<EnemyBase>();
            if (prefabBase == null) return;

            if (!_pools.ContainsKey(data))
                _pools[data] = PoolManager.Instance.CreatePool(prefabBase, 5);

            Vector3 spawnPos = GetSpawnPosition();
            EnemyBase enemy = _pools[data].Get(spawnPos, Quaternion.identity);
            enemy.WarpTo(spawnPos);
            enemy.SetData(data, _waveController.DamageMultiplier, _waveController.SpeedMultiplier);

            enemy.OnDeath -= HandleEnemyDeath;
            enemy.OnDeath += HandleEnemyDeath;

            _activeEnemies.Add(enemy.gameObject);
            if (data.IsElite) _activeEliteCount++;
        }

        private void HandleEnemyDeath(EnemyBase enemy)
        {
            _activeEnemies.Remove(enemy.gameObject);

            if (enemy.Data != null && enemy.Data.IsElite)
                _activeEliteCount = Mathf.Max(0, _activeEliteCount - 1);

            if (_pools.TryGetValue(enemy.Data, out ObjectPool<EnemyBase> pool))
                pool.Return(enemy);
        }

        private int GetEffectiveMaxActive()
        {
            float densityMultiplier = _waveController != null ? _waveController.SpawnDensityMultiplier : 1f;
            return Mathf.RoundToInt(_maxActiveEnemies * densityMultiplier);
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
            // A14: sürü artık steering kullanıyor (NavMeshAgent değil), eski 30 tavanı NavMesh
            // pathfinding maliyetine göre konmuştu ve artık geçerli değil. GDD Karar 2 hedefi:
            // 150-200 eşzamanlı düşman @ mobil 30 FPS — tier'a göre kademeli artan yeni tavan.
            _maxActiveEnemies = Mathf.Min(40 * (int)tier + 10, 200);
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
