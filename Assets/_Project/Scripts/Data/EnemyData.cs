using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Entities.Loot;

namespace BlobSurvivor.Data
{
    public enum EnemyAttackType { Melee, Ranged }

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

        [Header("Menzilli Saldırı")]
        [Tooltip("Melee: varsayılan yakın saldırı. Ranged: nişan lazeri + mermi (RangedAttackState). AttackRange menzildir ve oyuncu silahının menzilini (8) geçmemelidir.")]
        [SerializeField] private EnemyAttackType _attackType = EnemyAttackType.Melee;
        [SerializeField] private EnemyProjectile _projectilePrefab;
        [SerializeField] private Material _aimLineMaterial;
        [SerializeField] private float _projectileSpeed = 10f;
        [SerializeField] private float _projectileMaxDistance = 12f;
        [Tooltip("Lazerin belirmesinden ilk atışa kadar geçen toplam süre.")]
        [SerializeField] private float _aimDuration = 0.5f;
        [Tooltip("Toplam nişan süresinin son kısmı: lazer kalınlaşır ve yön sabitlenir.")]
        [SerializeField] private float _aimLockDuration = 0.2f;

        [Header("Yutulabilirlik (A12 — Karar 5, yeme birincil)")]
        [SerializeField] private float _massReward = 3f;
        [SerializeField] private bool _preventConsumption;
        // 2026-09-06 (Serra): eskiden "_spawnTier" adıyla vardı ama HİÇ kullanılmıyordu (Police'te
        // anlamsızca 0, elit/boss'larda tesadüfen doğru olan 5) — asıl yutulabilirlik eşiği
        // EnemyBase.TryConsumeByBlob() içinde IsElite'e göre hardcoded'du (Giant/Medium). Alan bu
        // amaç için yeniden adlandırılıp gerçekten okunur hale getirildi (bkz. EnemyBase.cs).
        [SerializeField] private BlobTier _requiredConsumeTier = BlobTier.Medium;
        [Tooltip("Yutulurken blob'a verdiği hasar. Blob eşiğin 1 tier üstündeyse yarısı, 2+ tier üstündeyse sıfır.")]
        [SerializeField] private float _consumeDamage;

        [Header("Kalp Düşürme")]
        [SerializeField] private HeartPickup _heartPrefab;
        [Tooltip("0-1 arası. Ölünce ya da yutulunca bu ihtimalle kalp düşer.")]
        [Range(0f, 1f)] [SerializeField] private float _heartDropChance;

        [Header("Final Boss (A16 — Karar 1+8)")]
        [Tooltip("true ise bu düşman silah hasarıyla asla ölmez (HP 1'de kilitlenir) — sadece FinalBossController'ın açtığı consumable override ile yutularak ölür.")]
        [SerializeField] private bool _requiresConsumptionToDie;
        [Tooltip("Tank: üstünden geçtiği arabaları ve küçük şehir eşyalarını ezer (TankCrusher). Binalar hariç.")]
        [SerializeField] private bool _crushesObstacles;

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
        public EnemyAttackType AttackType => _attackType;
        public EnemyProjectile ProjectilePrefab => _projectilePrefab;
        public Material AimLineMaterial => _aimLineMaterial;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileMaxDistance => _projectileMaxDistance;
        public float AimDuration => _aimDuration;
        public float AimLockDuration => _aimLockDuration;
        public float MassReward => _massReward;
        public bool PreventConsumption => _preventConsumption;
        public BlobTier RequiredConsumeTier => _requiredConsumeTier;
        public float ConsumeDamage => _consumeDamage;
        public HeartPickup HeartPrefab => _heartPrefab;
        public float HeartDropChance => _heartDropChance;
        public bool RequiresConsumptionToDie => _requiresConsumptionToDie;
        public bool CrushesObstacles => _crushesObstacles;
    }
}
