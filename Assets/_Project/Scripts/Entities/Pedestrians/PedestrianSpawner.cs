using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Pedestrians
{
    // M24: EnemySpawner'ın canlı sayacını (dev-a dosyası) sorgulayıp dinamik paylaşmak yerine,
    // sürü düşman tavanının (40*tier+10, max 200) belirgin şekilde altında, sabit/konservatif bir
    // kendi tavanıyla çalışıyor (varsayılan 20) — "aynı ajan bütçesini paylaşır" ilkesi burada canlı
    // koordinasyon yerine baştan küçük tutulan bir pay ile karşılanıyor.
    public class PedestrianSpawner : MonoBehaviour
    {
        public static PedestrianSpawner Instance { get; private set; }

        [SerializeField] private PedestrianData[] _pedestrianPool;
        [SerializeField] private int _maxActive = 20;
        [SerializeField] private float _spawnRadius = 25f;
        [SerializeField] private float _minSpawnDistance = 6f;
        [SerializeField] private int _initialSpawnCount = 12;
        [SerializeField] private float _refillCheckInterval = 3f;

        private Transform _blobTransform;
        private readonly List<GameObject> _active = new List<GameObject>();
        private readonly Dictionary<PedestrianData, ObjectPool<PedestrianController>> _pools = new Dictionary<PedestrianData, ObjectPool<PedestrianController>>();
        private float _refillTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _blobTransform = blob.transform;

            InitializePools();
            for (int i = 0; i < _initialSpawnCount; i++)
                SpawnRandom();
        }

        private void Update()
        {
            _refillTimer += Time.deltaTime;
            if (_refillTimer < _refillCheckInterval) return;

            _refillTimer = 0f;
            CleanupInactive();
            RefillIfNeeded();
        }

        private void InitializePools()
        {
            if (PoolManager.Instance == null || _pedestrianPool == null) return;

            foreach (PedestrianData data in _pedestrianPool)
            {
                if (data == null || data.Prefab == null) continue;
                PedestrianController prefabComponent = data.Prefab.GetComponent<PedestrianController>();
                if (prefabComponent == null) continue;
                _pools[data] = PoolManager.Instance.CreatePool(prefabComponent, 6);
            }
        }

        private void RefillIfNeeded()
        {
            int deficit = _maxActive - _active.Count;
            if (deficit <= 0) return;

            int toSpawn = Mathf.Min(deficit, 3);
            for (int i = 0; i < toSpawn; i++)
                SpawnRandom();
        }

        private void SpawnRandom()
        {
            if (_pedestrianPool == null || _pedestrianPool.Length == 0) return;

            PedestrianData data = _pedestrianPool[Random.Range(0, _pedestrianPool.Length)];
            if (data == null || !_pools.ContainsKey(data)) return;

            if (!TryGetSpawnPosition(data.SpawnYOffset, out Vector3 spawnPos)) return;

            PedestrianController instance = _pools[data].Get(spawnPos, Quaternion.identity);
            instance.SetData(data);
            _active.Add(instance.gameObject);
        }

        // Greybox/gerçek bina engelleri NavMesh'te delik açtığında rastgele nokta bir binanın
        // içine denk gelebilir — bu spawn denemesi o zaman atlanır, bir dahaki refill tick'inde
        // yeniden denenir (bkz. SpawnPositionUtility).
        private bool TryGetSpawnPosition(float yOffset, out Vector3 spawnPos)
        {
            Vector3 center = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(_minSpawnDistance, _spawnRadius);
            Vector3 candidate = new Vector3(center.x + randomCircle.x, yOffset, center.z + randomCircle.y);

            return SpawnPositionUtility.TryFindNavMeshPosition(candidate, yOffset, 5f, out spawnPos);
        }

        public void ReturnToPool(PedestrianController target)
        {
            if (target == null) return;

            _active.Remove(target.gameObject);
            if (_pools.TryGetValue((PedestrianData)target.Data, out ObjectPool<PedestrianController> pool))
                pool.Return(target);
        }

        private void CleanupInactive()
        {
            _active.RemoveAll(go => go == null || !go.activeInHierarchy);
        }
    }
}
