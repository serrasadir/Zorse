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
        [Tooltip("Oyuncuya görünen ad (boss barı, olay bandı). Boşsa DisplayName.")]
        [SerializeField] private string _hudName;
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
        [Tooltip("Silahla (mermi, patlama, diken) öldürülünce verilen XP. Kütle vermez; yutunca bunun yerine _massReward gelir.")]
        [SerializeField] private float _killXp;
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

        [Header("Sürü / ödül")]
        [Tooltip("Bir spawn'da kaç tane birlikte doğar (robot köpek sürüsü). Toplam düşman tavanını aşmaz.")]
        [SerializeField] private int _packSize = 1;
        [Tooltip("Ölünce düşen coin. 0 = varsayılan (polis 2, elit 5–10).")]
        [SerializeField] private int _coinOverride;
        [Tooltip("Coin düşme olasılığı. Sıradan düşman VS gibi ara sıra ama daha değerli coin düşürür (ortalama aynı).")]
        [Range(0f, 1f)] [SerializeField] private float _coinDropChance = 1f;
        [Tooltip("Zamanla yerini bu türe bırakır (bekçi → polis): spawn anında _replaceStart..End sn arasında "
                 + "doğrusal artan olasılıkla bu tür doğar. Boş = yok.")]
        [SerializeField] private EnemyData _replacedBy;
        [SerializeField] private float _replaceStart;
        [SerializeField] private float _replaceEnd;

        [Header("Ölüm efekti")]
        [Tooltip("Silahla ölünce dağıldığı kırıntıların renkleri (üniforma tonları).")]
        [SerializeField] private Color _crumbColorA = new Color(0.18f, 0.44f, 0.75f);
        [SerializeField] private Color _crumbColorB = new Color(0.23f, 0.34f, 0.66f);

        [Header("İlk görünüş bandı")]
        [Tooltip("Run'da bu tür ilk kez doğunca olay bandı (başlık + ipucu). Boş = bant yok.")]
        [SerializeField] private string _introTitle;
        [SerializeField] private string _introSub;

        [Header("Tavan")]
        [Tooltip("Aynı anda en fazla kaç tane (0 = sınırsız). Tank tipler az olsun; dolunca dalganın diğer türleri doğar. Lanet artırmaz.")]
        [SerializeField] private int _maxAlive;
        [Tooltip("Tür ilk doğduğunda tavan buradan başlar, her _maxAliveRampSeconds'ta +1 artarak _maxAlive'a çıkar.")]
        [SerializeField] private int _maxAliveStart = 1;
        [SerializeField] private float _maxAliveRampSeconds = 60f;

        [Header("Ön kalkan (çevik kuvvet)")]
        [Tooltip("Blob bu açının (yarım açı, derece) içinde öndeyse hasarın _frontShieldBlock kadarı kesilir. 0 = kalkan yok.")]
        [SerializeField, Range(0f, 180f)] private float _frontShieldHalfAngle;
        [SerializeField, Range(0f, 1f)] private float _frontShieldBlock = 0.85f;
        [Tooltip("Dönüş hızı tavanı (derece/sn). 0 = varsayılan hızlı dönüş. Kalkanlı yavaş dönsün ki etrafından dolaşılıp arkadan vurulsun.")]
        [SerializeField] private float _maxTurnSpeed;

        [Header("Dalış (motorlu polis)")]
        [Tooltip("Bu mesafede ve görüş açıkken durur, nişan alır, düz çizgide dalar. 0 = dalmaz.")]
        [SerializeField] private float _dashRange;
        [SerializeField] private float _dashSpeed = 8f;
        [SerializeField] private float _dashWindup = 0.6f;
        [SerializeField] private float _dashDuration = 0.9f;
        [SerializeField] private float _dashCooldown = 3f;

        public string DisplayName => _displayName;
        public string HudName => string.IsNullOrEmpty(_hudName) ? _displayName : _hudName;
        public GameObject Prefab => _prefab;
        public float MaxHealth => _maxHealth;
        public float Damage => _damage;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public int ScoreValue => _scoreValue;
        public bool IsElite => _isElite;
        public int MaxAlive => _maxAlive;
        public int MaxAliveStart => Mathf.Clamp(_maxAliveStart, 1, Mathf.Max(1, _maxAlive));
        public float MaxAliveRampSeconds => _maxAliveRampSeconds;
        public float MaxTurnSpeed => _maxTurnSpeed;
        public string IntroTitle => _introTitle;
        public string IntroSub => _introSub;
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
        public float KillXp => _killXp;
        public bool PreventConsumption => _preventConsumption;
        public BlobTier RequiredConsumeTier => _requiredConsumeTier;
        public float ConsumeDamage => _consumeDamage;
        public HeartPickup HeartPrefab => _heartPrefab;
        public float HeartDropChance => _heartDropChance;
        public bool RequiresConsumptionToDie => _requiresConsumptionToDie;
        public bool CrushesObstacles => _crushesObstacles;
        public int PackSize => Mathf.Max(1, _packSize);
        // Sürüyle gelen (robot köpek) polis tavanına sayılmaz, kendi tavanı var (EnemySpawner).
        public bool IsSwarm => _packSize > 1;
        public int CoinOverride => _coinOverride;
        public float CoinDropChance => _coinDropChance;
        public EnemyData ReplacedBy => _replacedBy;

        /// <summary>Bu saniyede bir spawn'ın ReplacedBy olma olasılığı (0..1).</summary>
        public float ReplaceChance(float time)
        {
            if (_replacedBy == null) return 0f;
            if (_replaceEnd <= _replaceStart) return time >= _replaceStart ? 1f : 0f;
            return Mathf.Clamp01((time - _replaceStart) / (_replaceEnd - _replaceStart));
        }
        public Color CrumbColorA => _crumbColorA;
        public Color CrumbColorB => _crumbColorB;
        public float FrontShieldHalfAngle => _frontShieldHalfAngle;
        public float FrontShieldBlock => _frontShieldBlock;
        public float DashRange => _dashRange;
        public float DashSpeed => _dashSpeed;
        public float DashWindup => _dashWindup;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;
    }
}
