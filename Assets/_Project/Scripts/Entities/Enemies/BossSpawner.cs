using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

        private const float SpawnY = 0.65f;

        private Transform _blobTransform;
        private int _nextStageIndex;
        private EnemyBase _activeBoss;
        private BossStage _activeStage;
        private float _lastReportedHealth = -1f;
        private readonly Dictionary<EnemyData, ObjectPool<EnemyBase>> _pools = new Dictionary<EnemyData, ObjectPool<EnemyBase>>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _blobTransform = blob.transform;

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

            float current = _activeBoss.CurrentHealth;
            if (!Mathf.Approximately(current, _lastReportedHealth))
            {
                _lastReportedHealth = current;
                GameEvents.RaiseBossHealthChanged(current, _activeBoss.MaxHealth);
            }
        }

        private void HandleRestart(CharacterData _)
        {
            _nextStageIndex = 0;
            _activeBoss = null;
            _lastReportedHealth = -1f;
        }

        private void CheckSpawnTrigger(float survivalTime)
        {
            if (_bossData?.Stages == null || _nextStageIndex >= _bossData.Stages.Length) return;
            if (_activeBoss != null) return; // aynı anda tek boss

            BossStage stage = _bossData.Stages[_nextStageIndex];
            if (survivalTime < stage.SpawnTime) return;

            SpawnStage(stage);
            _nextStageIndex++;
        }

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
        }

        private void HandleBossDeath(EnemyBase boss)
        {
            boss.OnDeath -= HandleBossDeath;

            if (_pools.TryGetValue(boss.Data, out ObjectPool<EnemyBase> pool))
                pool.Return(boss);

            int bonusCoin = Random.Range(_activeStage.BonusCoinMin, _activeStage.BonusCoinMax + 1);
            if (bonusCoin > 0)
                CoinSpawner.Instance?.SpawnCoin(boss.transform.position, bonusCoin);

            _activeBoss = null;
            GameEvents.RaiseBossHealthChanged(0f, 0f);
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 center = _blobTransform != null ? _blobTransform.position : Vector3.zero;
            Vector2 random = Random.insideUnitCircle.normalized * _spawnDistance;
            Vector3 candidate = new Vector3(center.x + random.x, SpawnY, center.z + random.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                return new Vector3(hit.position.x, SpawnY, hit.position.z);

            return candidate;
        }
    }
}
