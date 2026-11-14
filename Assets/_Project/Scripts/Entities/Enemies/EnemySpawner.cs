using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Entities.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private WaveController _waveController;
        // 2026-09-06 (Serra): eskiden blob'un o anki konumu etrafında sabit bir halkaya (_spawnDistance)
        // spawn oluyordu — polis nereye gitsen hep yakınında beliriyor, "haritanın her yerinde beni
        // buluyorlar" hissi tam olarak buradan geliyordu (PedestrianSpawner'ın 2026-09-05'te düzelttiği
        // aynı kümelenme deseni — polis tarafı o zaman unutulmuştu). Artık yayalarla aynı desen: tüm
        // harita alanına eşit dağılıyor, Patrol/Chase state
        // makinesi (EnemyBase._detectionRange) zaten "yakına gelince takip et" mantığını sağlıyordu,
        // sadece spawn'ın kendisi bunu geçersiz kılıyordu.
        [SerializeField] private float _mapHalfExtent = 30f;
        // 2026-11-01 (GDD_v2 Karar 15, büyük harita): dağılım hâlâ eşit ama blob etrafındaki ±_spawnRange
        // penceresinde. 30 = eski 60x60 haritanın tamamı, yani hissedilen yoğunluk değişmez.
        [SerializeField] private float _spawnRange = 30f;
        // Bu mesafeden uzakta kalan polis havuza döner (coin/ölüm yok), tavandaki yeri oyuncunun
        // yakınına yeniden spawn edilir. Detection range (15) ve ekranın epey dışında.
        [SerializeField] private float _despawnDistance = 50f;
        private const float DespawnCheckInterval = 1f;
        private float _despawnTimer;
        // Detection range'in (EnemyBase._detectionRange=15) hemen üzerinde tutulur — yeni spawn olan
        // polis anında "görüş menzilinde" başlayıp dümdüz kovalamaya geçmesin, önce dolaşsın.
        [SerializeField] private float _minDistanceFromBlob = 18f;
        // GDD_v2.md Karar 2 (31 Ağu 2026'da revize): eski tier-bazlı ölçekleme (40*tier+10, max 200)
        // gerçekçi bulunmadı — bir şehirde aynı anda 200 polis olmaz. Sabit, düşük bir tavan; ölen
        // polisin yerine yenisi gelir (havuz sürekli dolu görünür). Zamana bağlı yoğunluk artışı hâlâ
        // WaveController.SpawnDensityMultiplier üzerinden geliyor (10dk+ 2x, GetEffectiveMaxActive'da).
        [SerializeField] private int _maxActiveEnemies = 15;
        // Elit/boss NavMeshAgent kullanır (pahalı) — GDD_v2.md §7 Mimari: "aynı anda ≤8-10 agent".
        // Sürü tavanından ayrı tutulur, aksi halde weighted-random spawn elit sayısını da yükseltebilir.
        // 8 -> 4 (31 Ağu 2026): toplam tavan 200'den 15'e inince eski 8 orantısız kaldı (nüfusun
        // yarısından fazlası elit olabilirdi) — perf tavanı hâlâ geçerli ama oyun hissi için düşürüldü.
        [SerializeField] private int _maxActiveElites = 4;
        // Seviyeye göre baskı: başta az polis, seviye arttıkça tavan ve spawn hızı artar.
        // Yukarıdaki iki tavan üst sınır olarak kalır.
        [SerializeField] private float _policeCapAtStart = 8f;
        [SerializeField] private float _policeCapPerLevel = 2.2f;
        [SerializeField] private float _eliteCapAtLevel5 = 2f;
        [SerializeField] private float _eliteCapPerLevel = 1.5f;
        [SerializeField] private float _spawnSpeedupPerLevel = 0.1f; // seviye 10'da 2 kat sık

        private BlobGrowth _growth;

        private Transform _blobTransform;
        private float _spawnTimer;
        private int _activeEliteCount;
        private readonly List<GameObject> _activeEnemies = new List<GameObject>();
        private readonly Dictionary<EnemyData, ObjectPool<EnemyBase>> _pools = new Dictionary<EnemyData, ObjectPool<EnemyBase>>();

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRestart;

        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRestart;

        // Restart sahneyi yeniden yüklemiyor: önceki run'ın polisleri havuza dönmezse oyuncunun başında kalır.
        private void HandleRestart(CharacterData _)
        {
            foreach (GameObject go in _activeEnemies)
            {
                if (go == null) continue;
                EnemyBase enemy = go.GetComponent<EnemyBase>();
                if (enemy != null && enemy.Data != null && _pools.TryGetValue(enemy.Data, out ObjectPool<EnemyBase> pool))
                    pool.Return(enemy);
            }

            _activeEnemies.Clear();
            _activeEliteCount = 0;
            _spawnTimer = 0f;
        }

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null)
            {
                _blobTransform = blob.transform;
                _growth = blob.GetComponent<BlobGrowth>();
            }
        }

        private int Level => _growth != null ? _growth.CurrentLevel : 0;

        private void Update()
        {
            if (_waveController?.CurrentWave == null) return;
            if (!IsGamePlaying()) return;

            CleanupInactive();
            RecycleFarEnemies();

            if (_activeEnemies.Count >= GetEffectiveMaxActive()) return;

            _spawnTimer += Time.deltaTime;
            float density = _waveController.SpawnDensityMultiplier;
            if (_spawnTimer >= _waveController.CurrentWave.SpawnRate / ((1f + _spawnSpeedupPerLevel * Level) * density))
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
            if (data.IsElite && _activeEliteCount >= GetEffectiveMaxElites()) return;

            EnemyBase prefabBase = data.Prefab.GetComponent<EnemyBase>();
            if (prefabBase == null) return;

            if (!TryGetSpawnPosition(out Vector3 spawnPos)) return;

            if (!_pools.ContainsKey(data))
                _pools[data] = PoolManager.Instance.CreatePool(prefabBase, 5);

            EnemyBase enemy = _pools[data].Get(spawnPos, Quaternion.identity);
            enemy.WarpTo(spawnPos);
            enemy.SetData(data, _waveController.DamageMultiplier, _waveController.SpeedMultiplier);

            enemy.OnDeath -= HandleEnemyDeath;
            enemy.OnDeath += HandleEnemyDeath;

            _activeEnemies.Add(enemy.gameObject);
            if (data.IsElite) _activeEliteCount++;
        }

        private void RecycleFarEnemies()
        {
            _despawnTimer -= Time.deltaTime;
            if (_despawnTimer > 0f || _blobTransform == null) return;
            _despawnTimer = DespawnCheckInterval;

            Vector3 blobPos = _blobTransform.position;
            float despawnSq = _despawnDistance * _despawnDistance;
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                GameObject go = _activeEnemies[i];
                if ((go.transform.position - blobPos).sqrMagnitude < despawnSq) continue;

                EnemyBase enemy = go.GetComponent<EnemyBase>();
                if (enemy != null) HandleEnemyDeath(enemy); // aynı havuz dönüşü, coin/sandık Die()'da olduğu için düşmez
            }
        }

        private void HandleEnemyDeath(EnemyBase enemy)
        {
            _activeEnemies.Remove(enemy.gameObject);

            if (enemy.Data != null && enemy.Data.IsElite)
                _activeEliteCount = Mathf.Max(0, _activeEliteCount - 1);

            if (_pools.TryGetValue(enemy.Data, out ObjectPool<EnemyBase> pool))
                pool.Return(enemy);
        }

        // _maxActiveEnemies / _maxActiveElites mutlak tavan: hiçbir çarpan aşamaz (10. dk yoğunluğu sadece spawn'ı hızlandırır).
        private int GetEffectiveMaxActive() =>
            Mathf.RoundToInt(Mathf.Min(_maxActiveEnemies, _policeCapAtStart + _policeCapPerLevel * Level));

        private int GetEffectiveMaxElites()
        {
            float cap = _eliteCapAtLevel5 + _eliteCapPerLevel * (Level - 5);
            return Mathf.Clamp(Mathf.RoundToInt(cap), 1, _maxActiveElites);
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

        // Greybox/gerçek bina engelleri NavMesh'te delik açtığında rastgele nokta bir binanın
        // içine denk gelebilir — SpawnPositionUtility en yakın geçerli noktaya snap eder,
        // bulamazsa (false) bu spawn denemesi atlanır, bir dahaki tick'te yeniden denenir.
        // (2026-09-06) PedestrianSpawner.TryGetSpawnPosition ile aynı desen: blob'dan bağımsız,
        // tüm harita alanına eşit dağılım + blob'a çok yakın düşmesin diye küçük bir dışlama.
        private bool TryGetSpawnPosition(out Vector3 spawnPos)
        {
            Vector3 blobPos = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            float minDistSq = _minDistanceFromBlob * _minDistanceFromBlob;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                Vector3 candidate = SpawnPositionUtility.RandomPointNearBlob(blobPos, _spawnRange, _mapHalfExtent, EnemySpawnY);

                if ((candidate - blobPos).sqrMagnitude < minDistSq) continue;

                if (SpawnPositionUtility.TryFindNavMeshPosition(candidate, EnemySpawnY, 5f, out spawnPos))
                    return true;
            }

            spawnPos = Vector3.zero;
            return false;
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
