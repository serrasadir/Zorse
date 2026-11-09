using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities
{
    public class ConsumableSpawner : MonoBehaviour
    {
        public static ConsumableSpawner Instance { get; private set; }

        [SerializeField] private ConsumableData[] _consumablePool;
        [SerializeField] private int _maxActive = 80;
        [SerializeField] private float _spawnRadius = 30f;
        [SerializeField] private float _minSpawnDistance = 5f;
        [SerializeField] private float _mapHalfExtent = 84f; // şehir ±86; dışı su (şehir kurulumu yazar)
        [SerializeField] private int _initialSpawnCount = 40;
        [SerializeField] private float _refillCheckInterval = 2f;
        // 2026-11-03 (Serra, Karar 15 büyük harita): geride kalan consumable 80'lik tavanı dolduruyor,
        // oyuncunun etrafı boşalıyordu. Bu mesafeden uzaktakiler havuza döner, refill onları yakına getirir
        // (Enemy/PedestrianSpawner ile aynı desen). Şehir objeleri (CityConsumable) bu listede değil.
        [SerializeField] private float _despawnDistance = 50f;

        private Transform _blobTransform;
        private readonly List<GameObject> _activeConsumables = new List<GameObject>();
        private readonly Dictionary<ConsumableData, ObjectPool<ConsumableBase>> _pools = new Dictionary<ConsumableData, ObjectPool<ConsumableBase>>();
        private float _refillTimer;
        private readonly List<ConsumableData> _validBuffer = new List<ConsumableData>(); // spawn başına List ayırmasın

        private void Awake()
        {
            Instance = this;
        }

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
                RecycleFarConsumables();
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

        private void RecycleFarConsumables()
        {
            if (_blobTransform == null) return;

            Vector3 blobPos = _blobTransform.position;
            float despawnSq = _despawnDistance * _despawnDistance;
            for (int i = _activeConsumables.Count - 1; i >= 0; i--)
            {
                GameObject go = _activeConsumables[i];
                if (go == null || (go.transform.position - blobPos).sqrMagnitude < despawnSq) continue;

                ConsumableBase consumable = go.GetComponent<ConsumableBase>();
                if (consumable != null) ReturnToPool(consumable);
            }
        }

        private void SpawnRandom(BlobTier maxTier)
        {
            ConsumableData data = GetRandomDataForTier(maxTier);
            if (data == null || !_pools.ContainsKey(data)) return;

            if (!TryGetSpawnPosition(data.SpawnYOffset, out Vector3 spawnPos)) return;

            ConsumableBase instance = _pools[data].Get(spawnPos, RandomYaw());
            instance.SetData(data);
            _activeConsumables.Add(instance.gameObject);
        }

        // Yerdeki yiyecek/çöp modelleri (J_*) hep aynı yöne bakıp dizilmiş gibi durmasın.
        private static Quaternion RandomYaw() => Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        private ConsumableData GetRandomDataForTier(BlobTier maxTier)
        {
            _validBuffer.Clear();
            foreach (ConsumableData d in _consumablePool)
                if (d != null && d.RequiredTier <= maxTier)
                    _validBuffer.Add(d);

            if (_validBuffer.Count == 0) return null;
            return _validBuffer[Random.Range(0, _validBuffer.Count)];
        }

        // Rastgele nokta bir binanın içine/çatısına denk gelebilir — SpawnPositionUtility reddeder,
        // birkaç kez denenir; hepsi tutmazsa bir dahaki refill tick'inde yeniden denenir.
        private bool TryGetSpawnPosition(float yOffset, out Vector3 spawnPos)
        {
            Vector3 center = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(_minSpawnDistance, _spawnRadius);
                Vector3 candidate = new Vector3(
                    Mathf.Clamp(center.x + randomCircle.x, -_mapHalfExtent, _mapHalfExtent), yOffset,
                    Mathf.Clamp(center.z + randomCircle.y, -_mapHalfExtent, _mapHalfExtent));
                if (SpawnPositionUtility.TryFindNavMeshPosition(candidate, yOffset, 5f, out spawnPos))
                    return true;
            }
            spawnPos = Vector3.zero;
            return false;
        }

        // B14: normal yeme akışı için pool return.
        public void ReturnToPool(ConsumableBase target)
        {
            if (target == null) return;

            _activeConsumables.Remove(target.gameObject);
            if (target.Data != null && _pools.TryGetValue(target.Data, out ObjectPool<ConsumableBase> pool))
                pool.Return(target);
        }

        private void OnTierChanged(BlobTier newTier)
        {
            // Bonus da tavana uyar — eskiden tavanı aşıp run'dan run'a birikiyordu (ResetGrowth da tetikliyor).
            CleanupInactive();
            int bonus = Mathf.Min((int)newTier * 5, _maxActive - _activeConsumables.Count);
            for (int i = 0; i < bonus; i++)
                SpawnRandom(newTier);
        }

        private void CleanupInactive()
        {
            _activeConsumables.RemoveAll(go => go == null || !go.activeInHierarchy);
        }
    }
}
