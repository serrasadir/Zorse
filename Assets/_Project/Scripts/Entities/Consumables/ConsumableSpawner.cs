using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities
{
    public class ConsumableSpawner : MonoBehaviour
    {
        [SerializeField] private ConsumableData[] _consumablePool;
        [SerializeField] private int _maxActive = 80;
        [SerializeField] private float _spawnRadius = 30f;
        [SerializeField] private float _minSpawnDistance = 5f;
        [SerializeField] private int _initialSpawnCount = 40;
        [SerializeField] private float _refillCheckInterval = 2f;

        private Transform _blobTransform;
        private readonly List<GameObject> _activeConsumables = new List<GameObject>();
        private readonly Dictionary<ConsumableData, ObjectPool<ConsumableBase>> _pools = new Dictionary<ConsumableData, ObjectPool<ConsumableBase>>();
        private float _refillTimer;

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _blobTransform = blob.transform;

            InitializePools();
            SpawnInitialBatch();

            GameEvents.OnBlobTierChanged += OnTierChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnBlobTierChanged -= OnTierChanged;
        }

        private void Update()
        {
            _refillTimer += Time.deltaTime;
            if (_refillTimer >= _refillCheckInterval)
            {
                _refillTimer = 0f;
                RefillIfNeeded();
            }

            CleanupInactive();
        }

        private void InitializePools()
        {
            if (PoolManager.Instance == null) return;
            foreach (ConsumableData data in _consumablePool)
            {
                if (data?.Prefab == null) continue;
                ConsumableBase prefabComponent = data.Prefab.GetComponent<ConsumableBase>();
                if (prefabComponent == null) continue;
                _pools[data] = PoolManager.Instance.CreatePool(prefabComponent, 10);
            }
        }

        private void SpawnInitialBatch()
        {
            for (int i = 0; i < _initialSpawnCount; i++)
                SpawnRandom(BlobTier.Tiny);
        }

        private void RefillIfNeeded()
        {
            int deficit = _maxActive - _activeConsumables.Count;
            if (deficit <= 0) return;
            int toSpawn = Mathf.Min(deficit, 5);
            for (int i = 0; i < toSpawn; i++)
                SpawnRandom(BlobTier.Tiny);
        }

        private void SpawnRandom(BlobTier maxTier)
        {
            ConsumableData data = GetRandomDataForTier(maxTier);
            if (data == null || !_pools.ContainsKey(data)) return;

            Vector3 spawnPos = GetRandomSpawnPosition();
            ConsumableBase instance = _pools[data].Get(spawnPos, Quaternion.identity);
            instance.SetData(data);
            _activeConsumables.Add(instance.gameObject);
        }

        private ConsumableData GetRandomDataForTier(BlobTier maxTier)
        {
            List<ConsumableData> valid = new List<ConsumableData>();
            foreach (ConsumableData d in _consumablePool)
                if (d != null && d.RequiredTier <= maxTier)
                    valid.Add(d);

            if (valid.Count == 0) return null;
            return valid[Random.Range(0, valid.Count)];
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(_minSpawnDistance, _spawnRadius);
            return new Vector3(center.x + randomCircle.x, 0f, center.z + randomCircle.y);
        }

        private void OnTierChanged(BlobTier newTier)
        {
            int bonus = (int)newTier * 5;
            for (int i = 0; i < bonus; i++)
                SpawnRandom(newTier);
        }

        private void CleanupInactive()
        {
            _activeConsumables.RemoveAll(go => go == null || !go.activeInHierarchy);
        }
    }
}
