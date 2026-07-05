using UnityEngine;
using UnityEngine.AI;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

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
        private IEnemyState _currentState;
        private float _currentHealth;
        private float _aiUpdateTimer;
        private bool _canSeeBlobCached;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            _currentHealth = _data != null ? _data.MaxHealth : 100f;
            _aiUpdateTimer = Random.Range(0f, AIUpdateInterval); // stagger — hepsi aynı anda çalışmasın
            _canSeeBlobCached = false;

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

        public void SetData(EnemyData data)
        {
            _data = data;
            if (_agent != null) _agent.speed = _data.MoveSpeed;
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
            _blobHealth?.TakeDamage(_data.Damage);
        }

        public void TakeDamage(float amount)
        {
            _currentHealth -= amount;
            if (_currentHealth <= 0f)
                Die();
        }

        private void Die()
        {
            gameObject.SetActive(false);
        }
    }
}
