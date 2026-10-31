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
        [SerializeField] private int _scoreValue;

        [Header("Elit Davranışı (A6)")]
        [SerializeField] private bool _isElite;
        [SerializeField] private int _attackHitCount = 1;
        [SerializeField] private float _attackHitInterval = 0.3f;

        [Header("Yutulabilirlik (A12 — Karar 5, yeme birincil)")]
        [SerializeField] private float _massReward = 3f;
        [SerializeField] private bool _preventConsumption;
        // 2026-09-06 (Serra): eskiden "_spawnTier" adıyla vardı ama HİÇ kullanılmıyordu (Police'te
        // anlamsızca 0, elit/boss'larda tesadüfen doğru olan 5) — asıl yutulabilirlik eşiği
        // EnemyBase.TryConsumeByBlob() içinde IsElite'e göre hardcoded'du (Giant/Medium). Alan bu
        // amaç için yeniden adlandırılıp gerçekten okunur hale getirildi (bkz. EnemyBase.cs).
        [SerializeField] private BlobTier _requiredConsumeTier = BlobTier.Medium;

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
        public int ScoreValue => _scoreValue;
        public bool IsElite => _isElite;
        public int AttackHitCount => Mathf.Max(1, _attackHitCount);
        public float AttackHitInterval => _attackHitInterval;
        public float MassReward => _massReward;
        public bool PreventConsumption => _preventConsumption;
        public BlobTier RequiredConsumeTier => _requiredConsumeTier;
        public bool RequiresConsumptionToDie => _requiresConsumptionToDie;
    }
}
