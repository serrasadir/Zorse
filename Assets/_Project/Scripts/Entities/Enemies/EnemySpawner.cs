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
        [SerializeField] private float _mapHalfExtent = 30f;
        // Kamera yoksa yedek: blob etrafındaki ±_spawnRange karesine eşit dağılım.
        [SerializeField] private float _spawnRange = 30f;

        [Header("Ekran kenarı doğuşu")]
        // Düşman ekranın hemen dışında, ağırlıkla blob'un gittiği yönde doğar: kaçış yolu kapanır.
        [SerializeField] private float _minSpawnDistance = 10f;
        [SerializeField] private float _maxSpawnDistance = 40f;
        [SerializeField] private float _offscreenMargin = 0.06f; // viewport payı
        [SerializeField, Range(0f, 1f)] private float _aheadChance = 0.65f;
        [SerializeField] private float _aheadSpread = 70f; // gidiş yönünün ± derecesi

        [Header("Arkada kalanı öne taşıma")]
        // Ekran dışında, gidiş yönünün arkasında bu kadar sn kalan düşman öne ışınlanır ve kovalar.
        [SerializeField] private float _relocateAfter = 3f;
        [SerializeField] private float _relocateAlert = 6f;

        [Header("Kuşatma")]
        // Belirli aralıkla sıradan polislerden ekran kenarında tam halka; hepsi kovalar.
        [SerializeField] private float _encircleFirstTime = 150f;
        [SerializeField] private float _encircleInterval = 110f;
        [SerializeField] private int _encircleMinLevel = 3;
        [SerializeField] private int _encircleCountBase = 10;
        [SerializeField] private float _encircleCountPerLevel = 0.6f;
        [SerializeField] private int _encircleCountMax = 16;
        [SerializeField] private float _encircleAlert = 12f;
        [SerializeField] private int _encircleOvershoot = 8; // polis tavanını en fazla bu kadar aşar

        [Header("Tavanlar")]
        // Bu mesafeden uzakta kalan düşman havuza döner (coin/ölüm yok), yeri yeniden spawn edilir.
        [SerializeField] private float _despawnDistance = 50f;
        private const float DespawnCheckInterval = 1f;
        private float _despawnTimer;
        // Yedek (kamerasız) doğuşta blob'a en yakın mesafe.
        [SerializeField] private float _minDistanceFromBlob = 18f;
        // Mutlak tavan; zamana bağlı yoğunluk (WaveController.SpawnDensityMultiplier) sadece spawn'ı hızlandırır.
        [SerializeField] private int _maxActiveEnemies = 15;
        // Elit/boss NavMeshAgent kullanır (pahalı): ayrı, düşük tavan.
        [SerializeField] private int _maxActiveElites = 4;
        // Seviyeye göre baskı: başta az polis, seviye arttıkça tavan ve spawn hızı artar.
        // Yukarıdaki iki tavan üst sınır olarak kalır.
        [SerializeField] private float _policeCapAtStart = 8f;
        [SerializeField] private float _policeCapPerLevel = 2.2f;
        [SerializeField] private float _eliteCapAtLevel5 = 2f;
        [SerializeField] private float _eliteCapPerLevel = 1.5f;
        [SerializeField] private float _spawnSpeedupPerLevel = 0.1f; // seviye 10'da 2 kat sık

        // Sürü (robot köpek): polis tavanından ayrı, kendi tavanı; o da seviyeyle artar.
        [SerializeField] private float _swarmCapAtStart = 8f;
        [SerializeField] private float _swarmCapPerLevel = 1f;
        [SerializeField] private int _maxActiveSwarm = 18;

        private BlobGrowth _growth;

        private Transform _blobTransform;
        private float _spawnTimer;
        private int _activeEliteCount;
        private readonly List<GameObject> _activeEnemies = new List<GameObject>(); // polis + elit
        private readonly List<GameObject> _activeSwarm = new List<GameObject>();   // robot köpek
        private readonly Dictionary<EnemyData, ObjectPool<EnemyBase>> _pools = new Dictionary<EnemyData, ObjectPool<EnemyBase>>();
        private readonly Dictionary<GameObject, float> _behindTime = new Dictionary<GameObject, float>();

        private Camera _camera;
        private Vector3 _lastBlobPos;
        private Vector3 _blobVelocity; // yumuşatılmış, düz
        private const float MovingSpeedSq = 0.6f * 0.6f;
        private bool BlobMoving => _blobVelocity.sqrMagnitude > MovingSpeedSq;

        private float _survivalTime;
        private float _nextEncircleTime;
        private bool _bossActive;

        private void OnEnable()
        {
            GameEvents.OnCharacterSelected += HandleRestart;
            GameEvents.OnSurvivalTimeUpdated += OnSurvivalTime;
            GameEvents.OnBossHealthChanged += OnBossHealth;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= HandleRestart;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTime;
            GameEvents.OnBossHealthChanged -= OnBossHealth;
        }

        private void OnSurvivalTime(float t) => _survivalTime = t;
        private void OnBossHealth(float current, float max) => _bossActive = max > 0f;

        // Restart sahneyi yeniden yüklemiyor: önceki run'ın polisleri havuza dönmezse oyuncunun başında kalır.
        // Sıradan düşmanın şu an polis olma oranı (bekçi → polis geçişi). Görev metni buna bakar.
        public static float PoliceShare { get; private set; }

        private void HandleRestart(CharacterData _)
        {
            PoliceShare = 0f;
            ReturnAll(_activeEnemies);
            ReturnAll(_activeSwarm);
            _activeEliteCount = 0;
            _spawnTimer = 0f;
            _behindTime.Clear();
            _survivalTime = 0f;
            _nextEncircleTime = _encircleFirstTime;
            _bossActive = false;
            _blobVelocity = Vector3.zero;
            if (_blobTransform != null) _lastBlobPos = _blobTransform.position;
        }

        private void ReturnAll(List<GameObject> list)
        {
            foreach (GameObject go in list)
            {
                if (go == null) continue;
                EnemyBase enemy = go.GetComponent<EnemyBase>();
                if (enemy != null && enemy.Data != null && _pools.TryGetValue(enemy.Data, out ObjectPool<EnemyBase> pool))
                    pool.Return(enemy);
            }
            list.Clear();
        }

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null)
            {
                _blobTransform = blob.transform;
                _growth = blob.GetComponent<BlobGrowth>();
                _lastBlobPos = _blobTransform.position;
            }
            _camera = Camera.main;
            _nextEncircleTime = _encircleFirstTime;
        }

        private int Level => _growth != null ? _growth.CurrentLevel : 0;

        private void Update()
        {
            if (_waveController?.CurrentWave == null) return;
            if (!IsGamePlaying()) return;

            TrackBlobMotion();
            CleanupInactive();
            RecycleFarEnemies();
            TryEncircle();

            if (_activeEnemies.Count >= GetEffectiveMaxActive() && _activeSwarm.Count >= GetEffectiveMaxSwarm()) return;

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

            EnemyData data = ResolveVariant(SelectEnemyData(wave));
            if (data?.Prefab == null) return;

            // Elit tavanı sürüden ayrı: NavMeshAgent pahalı.
            if (data.IsElite && _activeEliteCount >= GetEffectiveMaxElites()) return;
            int room = data.IsSwarm ? GetEffectiveMaxSwarm() - _activeSwarm.Count : GetEffectiveMaxActive() - _activeEnemies.Count;
            if (room <= 0) return;

            if (!EnsurePool(data)) return;
            if (!TryGetSpawnPosition(out Vector3 spawnPos)) return;

            // Sürü (robot köpek): lider noktanın çevresine, tavan elverdiği kadar.
            int pack = Mathf.Min(data.PackSize, room);
            for (int i = 0; i < pack; i++)
            {
                Vector3 pos = spawnPos;
                if (i > 0)
                {
                    Vector2 off = Random.insideUnitCircle.normalized * PackSpread;
                    if (!SpawnPositionUtility.TryFindNavMeshPosition(spawnPos + new Vector3(off.x, 0f, off.y), EnemySpawnY, 2f, out pos))
                        continue;
                }
                SpawnOne(data, pos);
            }
        }

        private const float PackSpread = 1.2f;

        private bool EnsurePool(EnemyData data)
        {
            if (_pools.ContainsKey(data)) return true;
            EnemyBase prefabBase = data.Prefab.GetComponent<EnemyBase>();
            if (prefabBase == null) return false;
            _pools[data] = PoolManager.Instance.CreatePool(prefabBase, 5);
            return true;
        }

        private EnemyBase SpawnOne(EnemyData data, Vector3 spawnPos)
        {
            EnemyBase enemy = _pools[data].Get(spawnPos, Quaternion.identity);
            enemy.WarpTo(spawnPos);
            enemy.SetData(data, _waveController.DamageMultiplier, _waveController.SpeedMultiplier, _waveController.HealthMultiplier);

            enemy.OnDeath -= HandleEnemyDeath;
            enemy.OnDeath += HandleEnemyDeath;

            (data.IsSwarm ? _activeSwarm : _activeEnemies).Add(enemy.gameObject);
            if (data.IsElite) _activeEliteCount++;
            return enemy;
        }

        private void RecycleFarEnemies()
        {
            _despawnTimer -= Time.deltaTime;
            if (_despawnTimer > 0f || _blobTransform == null) return;
            _despawnTimer = DespawnCheckInterval;

            RecycleFar(_activeEnemies);
            RecycleFar(_activeSwarm);
        }

        private void RecycleFar(List<GameObject> list)
        {
            Vector3 blobPos = _blobTransform.position;
            float despawnSq = _despawnDistance * _despawnDistance;
            bool moving = BlobMoving && _camera != null;
            Vector3 moveDir = moving ? _blobVelocity.normalized : Vector3.zero;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                GameObject go = list[i];
                Vector3 offset = go.transform.position - blobPos;
                offset.y = 0f;
                float sq = offset.sqrMagnitude;
                if (sq >= despawnSq)
                {
                    EnemyBase far = go.GetComponent<EnemyBase>();
                    if (far != null) HandleEnemyDeath(far); // aynı havuz dönüşü, coin/sandık Die()'da olduğu için düşmez
                    continue;
                }

                // Gidiş yönünün arkasında ve ekran dışında kalan düşman öne taşınır.
                bool behind = moving && sq > 0.01f
                    && Vector3.Dot(offset / Mathf.Sqrt(sq), moveDir) < -0.3f
                    && IsOffscreen(go.transform.position);
                if (!behind) { _behindTime.Remove(go); continue; }

                _behindTime.TryGetValue(go, out float t);
                t += DespawnCheckInterval;
                if (t < _relocateAfter) { _behindTime[go] = t; continue; }
                _behindTime.Remove(go);

                EnemyBase enemy = go.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                if (!TryGetEdgeSpawnPosition(true, out Vector3 pos)) continue;
                enemy.WarpTo(pos);
                enemy.Alert(_relocateAlert);
            }
        }

        private void TrackBlobMotion()
        {
            if (_blobTransform == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 pos = _blobTransform.position;
            Vector3 v = (pos - _lastBlobPos) / dt;
            v.y = 0f;
            _lastBlobPos = pos;
            if (v.sqrMagnitude > 400f) return; // ışınlanma/restart sıçraması
            _blobVelocity = Vector3.Lerp(_blobVelocity, v, 1f - Mathf.Exp(-4f * dt));
        }

        // Test (DevCheats Y): dalgalardaki özel türlerden (kalkan, dalış) ikişer tane ekran kenarında, kovalar.
        public string DebugSpawnSpecialTypes()
        {
            if (_waveController == null || _waveController.AllWaves == null || _blobTransform == null || _camera == null) return "Spawner hazır değil";
            var seen = new HashSet<EnemyData>();
            int spawned = 0;
            foreach (WaveData wave in _waveController.AllWaves)
            {
                if (wave == null || wave.EnemyTypes == null) continue;
                foreach (var entry in wave.EnemyTypes)
                {
                    EnemyData d = entry.EnemyData;
                    if (d == null || !seen.Add(d) || (d.DashRange <= 0f && d.FrontShieldHalfAngle <= 0f) || !EnsurePool(d)) continue;
                    for (int i = 0; i < 2; i++)
                    {
                        if (!TryGetEdgeSpawnPosition(false, out Vector3 pos)) continue;
                        SpawnOne(d, pos).Alert(10f);
                        spawned++;
                    }
                }
            }
            return $"Özel tür: {spawned} düşman";
        }

        // Test (DevCheats L): zaman/seviye/boss koşulunu atlayıp hemen kuşatır.
        public string DebugEncircle()
        {
            int before = _activeEnemies.Count;
            TryEncircle(true);
            return $"Kuşatma: {_activeEnemies.Count - before} polis";
        }

        private void TryEncircle(bool force = false)
        {
            if (!force && (_survivalTime < _nextEncircleTime || _bossActive || Level < _encircleMinLevel)) return;
            if (_blobTransform == null || _camera == null || _waveController?.CurrentWave == null) return;
            _nextEncircleTime = _survivalTime + _encircleInterval;

            EnemyData data = PickEncircleData(_waveController.CurrentWave);
            if (data?.Prefab == null || !EnsurePool(data)) return;

            int wanted = Mathf.Min(_encircleCountMax, Mathf.RoundToInt(_encircleCountBase + _encircleCountPerLevel * Level));
            int count = Mathf.Min(wanted, _maxActiveEnemies + _encircleOvershoot - _activeEnemies.Count);
            if (count < 4) return;

            float start = Random.Range(0f, 360f);
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, start + i * 360f / count, 0f) * Vector3.forward;
                if (!TryEdgePointInDirection(dir, out Vector3 pos)) continue;
                EnemyData variant = ResolveVariant(data);
                if (variant?.Prefab == null || !EnsurePool(variant)) variant = data;
                SpawnOne(variant, pos).Alert(_encircleAlert);
                spawned++;
            }
            if (spawned > 0) GameEvents.RaiseEncirclement();
        }

        // Dalgadaki en ağırlıklı sıradan (elit/sürü/boss olmayan) düşman.
        private static EnemyData PickEncircleData(WaveData wave)
        {
            if (wave?.EnemyTypes == null) return null;
            EnemyData best = null;
            float bestWeight = -1f;
            foreach (var entry in wave.EnemyTypes)
            {
                EnemyData d = entry.EnemyData;
                if (d == null || d.IsElite || d.IsSwarm || d.PreventConsumption) continue;
                if (entry.SpawnWeight > bestWeight) { best = d; bestWeight = entry.SpawnWeight; }
            }
            return best;
        }

        private void HandleEnemyDeath(EnemyBase enemy)
        {
            _behindTime.Remove(enemy.gameObject);
            if (!_activeEnemies.Remove(enemy.gameObject)) _activeSwarm.Remove(enemy.gameObject);

            if (enemy.Data != null && enemy.Data.IsElite)
                _activeEliteCount = Mathf.Max(0, _activeEliteCount - 1);

            if (_pools.TryGetValue(enemy.Data, out ObjectPool<EnemyBase> pool))
                pool.Return(enemy);
        }

        // _maxActiveEnemies / _maxActiveElites mutlak tavan: hiçbir çarpan aşamaz (kuşatma hariç, _encircleOvershoot).
        // Lanet: tavan +%10/seviye, mutlak üst sınır +%5/seviye (mobilde steering maliyeti; 30 → en fazla 37).
        private int GetEffectiveMaxActive() =>
            Mathf.RoundToInt(Mathf.Min(_maxActiveEnemies * (1f + WaveController.Curse * 0.5f),
                (_policeCapAtStart + _policeCapPerLevel * Level) * (1f + WaveController.Curse)));

        private int GetEffectiveMaxSwarm() =>
            Mathf.RoundToInt(Mathf.Min(_maxActiveSwarm * (1f + WaveController.Curse * 0.5f),
                (_swarmCapAtStart + _swarmCapPerLevel * Level) * (1f + WaveController.Curse)));

        private int GetEffectiveMaxElites()
        {
            float cap = _eliteCapAtLevel5 + _eliteCapPerLevel * (Level - 5);
            return Mathf.Clamp(Mathf.RoundToInt(cap), 1, _maxActiveElites);
        }

        // Bekçi zamanla polise döner: dalga tablosu bekçi yazar, oranı süre belirler (EnemyData.ReplaceChance).
        private EnemyData ResolveVariant(EnemyData data)
        {
            if (data?.ReplacedBy == null) return data;
            PoliceShare = data.ReplaceChance(_survivalTime);
            return Random.value < data.ReplaceChance(_survivalTime) ? data.ReplacedBy : data;
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

        private bool TryGetSpawnPosition(out Vector3 spawnPos)
        {
            if (TryGetEdgeSpawnPosition(false, out spawnPos)) return true;
            return TryGetRandomSpawnPosition(out spawnPos);
        }

        // Ekran kenarının hemen dışı; blob hareket ediyorsa çoğunlukla (forceAhead: hep) önünde.
        private bool TryGetEdgeSpawnPosition(bool forceAhead, out Vector3 spawnPos)
        {
            spawnPos = Vector3.zero;
            if (_camera == null || _blobTransform == null) return false;
            bool ahead = BlobMoving && (forceAhead || Random.value < _aheadChance);
            float baseAngle = ahead ? Mathf.Atan2(_blobVelocity.x, _blobVelocity.z) * Mathf.Rad2Deg : 0f;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                float angle = ahead ? baseAngle + Random.Range(-_aheadSpread, _aheadSpread) : Random.Range(0f, 360f);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                if (TryEdgePointInDirection(dir, out spawnPos)) return true;
            }
            return false;
        }

        private const float EdgeStep = 2f;

        // Bu yönde ekranın dışına çıkılan ilk mesafe; NavMesh'e oturan nokta da ekran dışında olmalı.
        private bool TryEdgePointInDirection(Vector3 dir, out Vector3 spawnPos)
        {
            Vector3 blobPos = _blobTransform.position;
            blobPos.y = EnemySpawnY;
            float minSq = _minSpawnDistance * _minSpawnDistance;
            for (float r = _minSpawnDistance; r <= _maxSpawnDistance; r += EdgeStep)
            {
                Vector3 c = ClampToMap(blobPos + dir * r);
                if (!IsOffscreen(c)) continue;
                c = ClampToMap(blobPos + dir * (r + EdgeStep));
                if (SpawnPositionUtility.TryFindNavMeshPosition(c, EnemySpawnY, 3f, out spawnPos)
                    && IsOffscreen(spawnPos) && (spawnPos - blobPos).sqrMagnitude >= minSq)
                    return true;
                break;
            }
            spawnPos = Vector3.zero;
            return false;
        }

        private Vector3 ClampToMap(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, -_mapHalfExtent, _mapHalfExtent), p.y, Mathf.Clamp(p.z, -_mapHalfExtent, _mapHalfExtent));

        // Düşmanın ayağı ve tepesi (≈1.5 birim) görünür alanın dışında mı.
        private bool IsOffscreen(Vector3 p) =>
            OutsideViewport(_camera.WorldToViewportPoint(p))
            && OutsideViewport(_camera.WorldToViewportPoint(p + Vector3.up * 1.5f));

        private bool OutsideViewport(Vector3 vp)
        {
            if (vp.z <= 0f) return true;
            float m = _offscreenMargin;
            return vp.x < -m || vp.x > 1f + m || vp.y < -m || vp.y > 1f + m;
        }

        // Yedek: blob etrafındaki karede rastgele nokta (bina içine düşerse NavMesh'e oturtulur).
        private bool TryGetRandomSpawnPosition(out Vector3 spawnPos)
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
            _activeSwarm.RemoveAll(go => go == null || !go.activeInHierarchy);
        }

        private bool IsGamePlaying()
        {
            return GameManager.Instance == null || GameManager.Instance.IsPlaying;
        }
    }
}
