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
        // 2026-09-05: eskiden blob'un o anki konumu etrafında bir halkaya (ring) spawn oluyordu —
        // blob fazla hareket etmeyince tüm yayalar aynı bölgede kümeleniyormuş gibi görünüyordu
        // (Serra: "tek noktadan spawn oluyor, çevrede az oluyor"). Artık blob'dan bağımsız, tüm
        // şehir alanına (GreyboxCityGenerator'ın (GridSize-1)*BlockSpacing/2 = 30 ile aynı ölçek)
        // yayılıyor — dağılım blob'un nerede olduğuna bağlı değil.
        [SerializeField] private float _mapHalfExtent = 30f;
        // 2026-11-01 (GDD_v2 Karar 15, büyük harita): EnemySpawner ile aynı desen — blob etrafındaki
        // ±_spawnRange penceresinde eşit dağılım (küçük haritada pencere = tüm harita), uzakta kalan
        // yaya havuza döner ve oyuncunun yakınına yeniden gelir.
        [SerializeField] private float _spawnRange = 30f;
        [SerializeField] private float _despawnDistance = 50f;
        [SerializeField] private float _minDistanceFromBlob = 4f;
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
            RecycleFarPedestrians();
            RefillIfNeeded();
        }

        private void RecycleFarPedestrians()
        {
            if (_blobTransform == null) return;

            Vector3 blobPos = _blobTransform.position;
            float despawnSq = _despawnDistance * _despawnDistance;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                GameObject go = _active[i];
                if ((go.transform.position - blobPos).sqrMagnitude < despawnSq) continue;

                PedestrianController pedestrian = go.GetComponent<PedestrianController>();
                if (pedestrian != null) ReturnToPool(pedestrian);
            }
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
        // yeniden denenir (bkz. SpawnPositionUtility). Blob'un tam üstüne düşmesin diye sadece
        // küçük bir dışlama mesafesi (_minDistanceFromBlob) var, geri kalanı tüm harita alanına
        // eşit dağılıyor.
        private bool TryGetSpawnPosition(float yOffset, out Vector3 spawnPos)
        {
            Vector3 blobPos = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            float minDistSq = _minDistanceFromBlob * _minDistanceFromBlob;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                Vector3 candidate = SpawnPositionUtility.RandomPointNearBlob(blobPos, _spawnRange, _mapHalfExtent, yOffset);

                if ((candidate - blobPos).sqrMagnitude < minDistSq) continue;

                if (SpawnPositionUtility.TryFindNavMeshPosition(candidate, yOffset, 5f, out spawnPos))
                    return true;
            }

            spawnPos = Vector3.zero;
            return false;
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
