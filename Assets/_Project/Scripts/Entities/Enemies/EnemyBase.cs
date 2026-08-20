using UnityEngine;
using UnityEngine.AI;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Coins;
using BlobSurvivor.Entities.Loot;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Enemies
{
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] private EnemyData _data;
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _separationRadius = 1.5f;
        [SerializeField] private float _separationWeight = 1.5f;
        [SerializeField] private float _avoidanceLookahead = 1.2f;
        [SerializeField] private float _avoidanceWeight = 2f;
        [SerializeField] private float _rotationSpeed = 10f;

        private const float AIUpdateInterval = 0.15f;
        private static int s_environmentMask = -1;

        public event System.Action<EnemyBase> OnDeath;
        // B17 evrimi "Avcı Formu": silahla vurulan sürü düşmanlarını dinlemek için statik hit event'i.
        public static event System.Action<EnemyBase> OnAnyEnemyDamaged;

        public EnemyData Data => _data;
        public Transform BlobTransform { get; private set; }
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _data != null ? _data.MaxHealth : 0f;

        // Karar 2 (GDD_v2.md §7): sürü düşmanları NavMeshAgent kullanmaz, basit steering kullanır.
        // Elit/boss NavMeshAgent'ta kalmaya devam eder.
        public bool UsesSteering => _data != null && !_data.IsElite;

        private NavMeshAgent _agent;
        private BlobHealth _blobHealth;
        private ScoreSystem _scoreSystem;
        private IEnemyState _currentState;
        private float _currentHealth;
        private float _aiUpdateTimer;
        private bool _canSeeBlobCached;
        private float _damageMultiplier = 1f;
        private float _speedMultiplier = 1f;
        private bool _isDead;
        private bool _consumableOverride;
        private float _tierBypassUntil = -1f;
        private Vector3 _steerTarget;
        private bool _hasSteerTarget;
        private Vector3 _separationCached;
        private Vector3 _avoidanceCached;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _scoreSystem = FindAnyObjectByType<ScoreSystem>();

            if (s_environmentMask < 0)
                s_environmentMask = LayerMask.GetMask("Environment");
        }

        private void OnEnable()
        {
            _currentHealth = _data != null ? _data.MaxHealth : 100f;
            _aiUpdateTimer = Random.Range(0f, AIUpdateInterval); // stagger — hepsi aynı anda çalışmasın
            _canSeeBlobCached = false;
            _damageMultiplier = 1f;
            _speedMultiplier = 1f;
            _isDead = false;
            _consumableOverride = false;
            _tierBypassUntil = -1f;
            _hasSteerTarget = false;

            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null)
            {
                BlobTransform = blob.transform;
                _blobHealth = blob.GetComponent<BlobHealth>();
            }

            if (UsesSteering)
            {
                if (_agent != null) _agent.enabled = false;
                SwarmSteering.Instance?.Register(this);
            }
            else
            {
                if (_agent != null) _agent.enabled = true;
                if (_agent != null && _data != null) _agent.speed = _data.MoveSpeed;
            }

            ChangeState(new PatrolState());
        }

        private void OnDisable()
        {
            SwarmSteering.Instance?.Unregister(this);
        }

        private void Update()
        {
            _aiUpdateTimer -= Time.deltaTime;
            bool aiTick = _aiUpdateTimer <= 0f;
            if (aiTick)
            {
                _aiUpdateTimer = AIUpdateInterval;
                _canSeeBlobCached = ComputeCanSeeBlob();

                // Ayrım (separation) ve engel kaçınma sorguları pahalı (komşu grid taraması / raycast) —
                // her frame değil, throttle tick'te hesaplanıp cache'lenir. Seek her frame taze kalır
                // (ucuz, sadece vektör çıkarma) — hareket akıcı görünür, ağır kısım throttle'lanır.
                if (UsesSteering)
                {
                    _separationCached = SwarmSteering.Instance != null
                        ? SwarmSteering.Instance.GetSeparationVector(this, _separationRadius)
                        : Vector3.zero;
                    _avoidanceCached = ComputeAvoidance();
                }
            }

            _currentState?.Update(this, aiTick);

            if (UsesSteering && _hasSteerTarget)
                ApplySteeringMovement();
        }

        private Vector3 ComputeAvoidance()
        {
            if (s_environmentMask == 0) return Vector3.zero;

            Vector3 forward = _hasSteerTarget ? _steerTarget - transform.position : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return Vector3.zero;
            forward.Normalize();

            if (!Physics.Raycast(transform.position, forward, out RaycastHit hit, _avoidanceLookahead, s_environmentMask))
                return Vector3.zero;

            // Duvara sıkışmayı önle: engelden uzaklaştıran, hareket yönüne dik bir sapma.
            Vector3 away = Vector3.Cross(Vector3.up, hit.normal);
            if (Vector3.Dot(away, forward) < 0f) away = -away;
            return away;
        }

        private void ApplySteeringMovement()
        {
            Vector3 toTarget = _steerTarget - transform.position;
            toTarget.y = 0f;

            Vector3 seek = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.zero;
            Vector3 moveDir = seek + _separationCached * _separationWeight + _avoidanceCached * _avoidanceWeight;
            if (moveDir.sqrMagnitude < 0.0001f) return;
            moveDir.Normalize();

            float speed = _data.MoveSpeed * _speedMultiplier;
            transform.position += moveDir * speed * Time.deltaTime;

            // Ani yön sıçramalarını yumuşat (Slerp) — separation kaynaklı titreşimi görsel olarak azaltır.
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), _rotationSpeed * Time.deltaTime);
        }

        private bool ComputeCanSeeBlob()
        {
            if (BlobTransform == null) return false;
            float sqrDist = (transform.position - BlobTransform.position).sqrMagnitude;
            return sqrDist <= _detectionRange * _detectionRange;
        }

        // BossSpawner gibi çağıranlar, tek bir prefab'ı birden fazla EnemyData ("stage") ile paylaşır —
        // OnEnable() zaten prefab'da serialize edilmiş (muhtemelen yanlış/stale) _data'ya göre
        // _currentHealth'i set etmiş olur; burada doğru data'ya göre yeniden set edilmesi şart,
        // yoksa boss yanlış HP'yle (spawn anındaki prefab-serialized data'nın MaxHealth'iyle) hayata başlar.
        public void SetData(EnemyData data, float damageMultiplier = 1f, float speedMultiplier = 1f)
        {
            _data = data;
            _currentHealth = data.MaxHealth;
            _damageMultiplier = Mathf.Max(0f, damageMultiplier);
            _speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            if (_agent != null) _agent.speed = _data.MoveSpeed * _speedMultiplier;
        }

        public void ChangeState(IEnemyState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState.Enter(this);
        }

        public void WarpTo(Vector3 position)
        {
            if (_agent != null && _agent.isActiveAndEnabled)
                _agent.Warp(position);
            else
                transform.position = position;
        }

        public void SetDestination(Vector3 destination)
        {
            if (UsesSteering)
            {
                _steerTarget = destination;
                _hasSteerTarget = true;
                return;
            }

            if (_agent != null && _agent.isOnNavMesh)
                _agent.SetDestination(destination);
        }

        public void StopMoving()
        {
            if (UsesSteering)
            {
                _hasSteerTarget = false;
                return;
            }

            if (_agent != null && _agent.isOnNavMesh)
                _agent.ResetPath();
        }

        public bool CanSeeBlob() => _canSeeBlobCached;

        public void PerformAttack()
        {
            _blobHealth?.TakeDamage(_data.Damage * _damageMultiplier);
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            OnAnyEnemyDamaged?.Invoke(this);

            if (_currentHealth <= 0f)
            {
                // A16/Karar 1+8: final boss silahla öldürülemez — "vurarak son faza getirirsin, yutarak bitirirsin."
                // HP 1'de kilitlenir, ölüm sadece TryConsumeByBlob üzerinden gerçekleşebilir.
                if (_data != null && _data.RequiresConsumptionToDie)
                    _currentHealth = 1f;
                else
                    Die();
            }
        }

        // FinalBossController, HP eşiği altına inince bunu true yapar — yenebilir faza geçiş.
        // PreventConsumption asset-seviyesinde (paylaşımlı SO), bu ise instance-seviyesinde geçici override.
        public void SetConsumableOverride(bool value) => _consumableOverride = value;

        // B17 evrimi "Avcı Formu": silahla vurulan sürü düşmanları kısa süreliğine tier şartı olmadan
        // yutulabilir olur. Elit/boss'u kapsamaz (GDD: "sürü düşmanları").
        public void MarkTemporarilyConsumable(float duration) => _tierBypassUntil = Mathf.Max(_tierBypassUntil, Time.time + duration);

        // Karar 5 (GDD_v2.md §4 — yeme birincil): sürü düşmanı Tier3+'ta, elit Tier5'te temasla yutulabilir.
        // Mass ödülü BlobGrowth.AddMass üzerinden verilmeli — mass=XP zaten birleşik (Karar 7).
        public bool TryConsumeByBlob(BlobTier blobTier, out float massReward)
        {
            massReward = 0f;
            if (_isDead || _data == null) return false;
            // A15/Karar 8: miniboss/final boss normalde hiçbir tier'da yutulamaz, silahla öldürülür —
            // A16: final boss'un yenebilir fazında _consumableOverride bunu geçici olarak açar.
            if (_data.PreventConsumption && !_consumableOverride) return false;

            bool tierBypassed = !_data.IsElite && Time.time <= _tierBypassUntil;
            BlobTier requiredTier = _data.IsElite ? BlobTier.Giant : BlobTier.Medium;
            if (!tierBypassed && blobTier < requiredTier) return false;

            massReward = _data.MassReward;
            Die();
            return true;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            _scoreSystem?.AddScore(_data.ScoreValue);

            int coinAmount = _data.IsElite ? Random.Range(5, 11) : 1;
            CoinSpawner.Instance?.SpawnCoin(transform.position, coinAmount);
            if (_data.IsElite)
                ChestSpawner.Instance?.SpawnChest(transform.position);

            // Pool'a dönüş çağıranın (EnemySpawner) sorumluluğunda — CoinSpawner/CoinPickup'taki pattern'in aynısı.
            OnDeath?.Invoke(this);
        }
    }
}
