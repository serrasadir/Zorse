using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Coins;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Enemies
{
    // A15 (GDD Karar 8 + §7): 4./8. dk'da aynı tasarım, artan stat'lı miniboss.
    // EnemySpawner'ın ağırlıklı-random havuzundan bağımsız — sabit zamanlı, tekil spawn.
    public class BossSpawner : MonoBehaviour
    {
        public static BossSpawner Instance { get; private set; }

        [SerializeField] private BossData _bossData;
        [SerializeField] private float _spawnDistance = 20f;

        // 2026-11-03 (Serra: "minibosstan rahatça kaçabiliyorum"): boss ekran dışındayken VE uzaktayken hızlanır,
        // ekrana girince normal hızına döner — ışınlama gibi seni bulur ama gözünün önünde belirmez, binaların
        // etrafından gerçekten dolaşarak gelir. Mesafe kamera uzaklaşmasından bağımsız: ekran kontrolü onu zaten kapsar.
        [Header("Yetişme")]
        [SerializeField] private float _catchUpDistance = 25f;
        [SerializeField] private float _catchUpSpeedMultiplier = 2f;
        [SerializeField] private float _catchUpCheckInterval = 0.25f;
        private const float ViewportMargin = 0.05f;

        private Camera _camera;
        private float _catchUpTimer;

        private const float SpawnY = 0.65f;

        private Transform _blobTransform;
        private int _nextStageIndex;
        private EnemyBase _activeBoss;
        private BossStage _activeStage;
        private float _lastReportedHealth = -1f;
        private readonly Dictionary<EnemyData, ObjectPool<EnemyBase>> _pools = new Dictionary<EnemyData, ObjectPool<EnemyBase>>();

        private BossDeathEffect _deathEffect;

        private void Awake()
        {
            Instance = this;
            _deathEffect = GetComponent<BossDeathEffect>();
            if (_deathEffect == null) _deathEffect = gameObject.AddComponent<BossDeathEffect>();
        }

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _blobTransform = blob.transform;
            _camera = Camera.main;

            GameEvents.OnSurvivalTimeUpdated += CheckSpawnTrigger;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDestroy()
        {
            GameEvents.OnSurvivalTimeUpdated -= CheckSpawnTrigger;
            GameEvents.OnCharacterSelected -= HandleRestart;

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_activeBoss == null) return;

            UpdateCatchUp();

            float current = _activeBoss.CurrentHealth;
            if (!Mathf.Approximately(current, _lastReportedHealth))
            {
                _lastReportedHealth = current;
                GameEvents.RaiseBossHealthChanged(current, _activeBoss.MaxHealth);
            }
        }

        private void UpdateCatchUp()
        {
            _catchUpTimer -= Time.deltaTime;
            if (_catchUpTimer > 0f || _blobTransform == null) return;
            _catchUpTimer = _catchUpCheckInterval;

            Vector3 bossPos = _activeBoss.transform.position;
            Vector3 delta = bossPos - _blobTransform.position;
            delta.y = 0f;
            bool far = delta.sqrMagnitude > _catchUpDistance * _catchUpDistance;
            _activeBoss.SetCatchUpMultiplier(far && !IsOnScreen(bossPos) ? _catchUpSpeedMultiplier : 1f);
        }

        private bool IsOnScreen(Vector3 worldPos)
        {
            if (_camera == null) return false;
            Vector3 v = _camera.WorldToViewportPoint(worldPos);
            return v.z > 0f && v.x > -ViewportMargin && v.x < 1f + ViewportMargin
                && v.y > -ViewportMargin && v.y < 1f + ViewportMargin;
        }

        private void HandleRestart(CharacterData _)
        {
            DismissActiveBoss();
            _nextStageIndex = 0;
            _lastReportedHealth = -1f;
        }

        // Aktif boss'u ödülsüz havuza döndürür (restart, final boss gelişi, DevCheats).
        private void DismissActiveBoss()
        {
            if (_activeBoss == null) return;
            _activeBoss.OnDeath -= HandleBossDeath;
            if (_pools.TryGetValue(_activeBoss.Data, out ObjectPool<EnemyBase> pool))
                pool.Return(_activeBoss);
            _activeBoss = null;
            GameEvents.RaiseBossHealthChanged(0f, 0f);
        }

        // Her aşama kendi süresinde çıkar; süresi geçmiş önceki aşamalar atlanır (sadece en sonuncusu çıkar).
        // Aynı anda tek boss: ara aşama aktif boss'u bekler, son aşama (final boss) beklemez — aktif boss çekilir.
        private void CheckSpawnTrigger(float survivalTime)
        {
            BossStage[] stages = _bossData?.Stages;
            if (stages == null || _nextStageIndex >= stages.Length) return;

            int due = -1;
            for (int i = _nextStageIndex; i < stages.Length; i++)
                if (survivalTime >= stages[i].SpawnTime) due = i;
            if (due < 0) return;

            bool isFinal = due == stages.Length - 1;
            if (_activeBoss != null)
            {
                if (!isFinal) return;
                DismissActiveBoss();
            }

            SpawnStage(stages[due]);
            _nextStageIndex = due + 1;
        }

#if UNITY_EDITOR
        // DevCheats N: aktif boss'u ödülsüz kaldırır, sıradaki aşamayı hemen çıkarır.
        public string DebugSpawnNextStage()
        {
            if (_bossData?.Stages == null || _nextStageIndex >= _bossData.Stages.Length)
                return "Çıkacak boss aşaması kalmadı.";

            DismissActiveBoss();
            BossStage stage = _bossData.Stages[_nextStageIndex];
            SpawnStage(stage);
            _nextStageIndex++;
            return $"Aşama {_nextStageIndex}/{_bossData.Stages.Length}: {stage.EnemyData?.DisplayName}";
        }
#endif

        private void SpawnStage(BossStage stage)
        {
            EnemyData data = stage.EnemyData;
            if (data?.Prefab == null) return;

            EnemyBase prefabBase = data.Prefab.GetComponent<EnemyBase>();
            if (prefabBase == null) return;

            if (!_pools.ContainsKey(data))
                _pools[data] = PoolManager.Instance.CreatePool(prefabBase, 1);

            Vector3 spawnPos = GetSpawnPosition();
            EnemyBase boss = _pools[data].Get(spawnPos, Quaternion.identity);
            boss.WarpTo(spawnPos);
            boss.SetData(data);

            boss.OnDeath -= HandleBossDeath;
            boss.OnDeath += HandleBossDeath;

            _activeBoss = boss;
            _activeStage = stage;
            _lastReportedHealth = -1f;

#if UNITY_EDITOR
            Debug.Log($"[BossSpawner] Spawn: {data.DisplayName} — CurrentHealth={boss.CurrentHealth}, MaxHealth={boss.MaxHealth}");
#endif
        }

        private void HandleBossDeath(EnemyBase boss)
        {
            boss.OnDeath -= HandleBossDeath;

#if UNITY_EDITOR
            Debug.Log($"[BossSpawner] Öldü/yutuldu: {boss.Data.DisplayName}");
#endif

            // Miniboss'un ölüm sahnesi (görüntü kopyası). Final boss'un yutulma anı FinalBossController'da.
            if (boss.GetComponent<FinalBossController>() == null)
                _deathEffect.Play(boss, boss.Data.Prefab);
            else
                _deathEffect.PlaySwallow(boss, boss.Data.Prefab, GameEvents.RaiseFinalBossConsumed);

            if (_pools.TryGetValue(boss.Data, out ObjectPool<EnemyBase> pool))
                pool.Return(boss);

            int bonusCoin = Random.Range(_activeStage.BonusCoinMin, _activeStage.BonusCoinMax + 1);
            if (bonusCoin > 0)
                CoinSpawner.Instance?.SpawnCoin(boss.transform.position, bonusCoin);

            _activeBoss = null;
            GameEvents.RaiseBossHealthChanged(0f, 0f);
        }

        // Boss mutlaka çıkmalı: birkaç rastgele yön denenir (bina içi/çatı reddedilir), hiçbiri tutmazsa ham aday.
        private Vector3 GetSpawnPosition()
        {
            Vector3 center = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            Vector3 candidate = center;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 random = Random.insideUnitCircle.normalized * _spawnDistance;
                candidate = new Vector3(center.x + random.x, SpawnY, center.z + random.y);
                if (SpawnPositionUtility.TryFindNavMeshPosition(candidate, SpawnY, 5f, out Vector3 valid))
                    return valid;
            }
            return candidate;
        }
    }
}
