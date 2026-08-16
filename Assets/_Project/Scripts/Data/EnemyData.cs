using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "BlobSurvivor/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private float _maxHealth;
        [SerializeField] private float _damage;
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _attackRange;
        [SerializeField] private float _attackCooldown;
        [SerializeField] private BlobTier _spawnTier;
        [SerializeField] private int _scoreValue;

        [Header("Elit Davranışı (A6)")]
        [SerializeField] private bool _isElite;
        [SerializeField] private int _attackHitCount = 1;
        [SerializeField] private float _attackHitInterval = 0.3f;

        [Header("Yutulabilirlik (A12 — Karar 5, yeme birincil)")]
        [SerializeField] private float _massReward = 3f;
        [SerializeField] private bool _preventConsumption;

        [Header("Final Boss (A16 — Karar 1+8)")]
        [Tooltip("true ise bu düşman silah hasarıyla asla ölmez (HP 1'de kilitlenir) — sadece FinalBossController'ın açtığı consumable override ile yutularak ölür.")]
        [SerializeField] private bool _requiresConsumptionToDie;

        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public float MaxHealth => _maxHealth;
        public float Damage => _damage;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public BlobTier SpawnTier => _spawnTier;
        public int ScoreValue => _scoreValue;
        public bool IsElite => _isElite;
        public int AttackHitCount => Mathf.Max(1, _attackHitCount);
        public float AttackHitInterval => _attackHitInterval;
        public float MassReward => _massReward;
        public bool PreventConsumption => _preventConsumption;
        public bool RequiresConsumptionToDie => _requiresConsumptionToDie;
    }
}
