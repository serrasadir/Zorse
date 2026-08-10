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

        private const float AIUpdateInterval = 0.15f;

        public event System.Action<EnemyBase> OnDeath;

        public EnemyData Data => _data;
        public Transform BlobTransform { get; private set; }

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
        private Vector3 _steerTarget;
        private bool _hasSteerTarget;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
        }

        private void OnEnable()
        {
            _currentHealth = _data != null ? _data.MaxHealth : 100f;
            _aiUpdateTimer = Random.Range(0f, AIUpdateInterval); // stagger — hepsi aynı anda çalışmasın
            _canSeeBlobCached = false;
            _damageMultiplier = 1f;
            _speedMultiplier = 1f;
            _isDead = false;
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
            }

            _currentState?.Update(this, aiTick);

            if (UsesSteering && _hasSteerTarget)
                ApplySteeringMovement();
        }

        private void ApplySteeringMovement()
        {
            Vector3 toTarget = _steerTarget - transform.position;
            toTarget.y = 0f;

            Vector3 seek = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.zero;
            Vector3 separation = SwarmSteering.Instance != null
                ? SwarmSteering.Instance.GetSeparationVector(this, _separationRadius)
                : Vector3.zero;

            Vector3 moveDir = seek + separation * _separationWeight;
            if (moveDir.sqrMagnitude < 0.0001f) return;
            moveDir.Normalize();

            float speed = _data.MoveSpeed * _speedMultiplier;
            transform.position += moveDir * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(moveDir);
        }

        private bool ComputeCanSeeBlob()
        {
            if (BlobTransform == null) return false;
            float sqrDist = (transform.position - BlobTransform.position).sqrMagnitude;
            return sqrDist <= _detectionRange * _detectionRange;
        }

        public void SetData(EnemyData data, float damageMultiplier = 1f, float speedMultiplier = 1f)
        {
            _data = data;
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
            if (_currentHealth <= 0f)
                Die();
        }

        // Karar 5 (GDD_v2.md §4 — yeme birincil): sürü düşmanı Tier3+'ta, elit Tier5'te temasla yutulabilir.
        // Mass ödülü BlobGrowth.AddMass üzerinden verilmeli — mass=XP zaten birleşik (Karar 7).
        public bool TryConsumeByBlob(BlobTier blobTier, out float massReward)
        {
            massReward = 0f;
            if (_isDead || _data == null) return false;

            BlobTier requiredTier = _data.IsElite ? BlobTier.Giant : BlobTier.Medium;
            if (blobTier < requiredTier) return false;

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
