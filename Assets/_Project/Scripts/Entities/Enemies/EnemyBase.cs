using UnityEngine;
using UnityEngine.AI;
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

        private const float AIUpdateInterval = 0.15f;

        public EnemyData Data => _data;
        public Transform BlobTransform { get; private set; }

        private NavMeshAgent _agent;
        private BlobHealth _blobHealth;
        private ScoreSystem _scoreSystem;
        private IEnemyState _currentState;
        private float _currentHealth;
        private float _aiUpdateTimer;
        private bool _canSeeBlobCached;
        private float _damageMultiplier = 1f;
        private float _speedMultiplier = 1f;

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

            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null)
            {
                BlobTransform = blob.transform;
                _blobHealth = blob.GetComponent<BlobHealth>();
            }

            if (_agent != null && _data != null)
                _agent.speed = _data.MoveSpeed;

            ChangeState(new PatrolState());
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

        public void SetDestination(Vector3 destination)
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.SetDestination(destination);
        }

        public void StopMoving()
        {
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
            _currentHealth -= amount;
            if (_currentHealth <= 0f)
                Die();
        }

        private void Die()
        {
            _scoreSystem?.AddScore(_data.ScoreValue);

            int coinAmount = _data.IsElite ? Random.Range(5, 11) : 1;
            CoinSpawner.Instance?.SpawnCoin(transform.position, coinAmount);
            if (_data.IsElite)
                ChestSpawner.Instance?.SpawnChest(transform.position);

            gameObject.SetActive(false);
        }
    }
}
