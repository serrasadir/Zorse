using System;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Vehicles
{
    public class CarController : MonoBehaviour
    {
        public event Action<CarController> OnConsumed;

        private enum CarState { Moving, Stopped }

        private CarData _data;
        private Transform[] _waypoints;
        private int _currentWaypointIndex;
        private float _damageTimer;
        private bool _isActive;
        private CarState _state = CarState.Moving;
        private float _currentSpeed;

        // Cached references — FindAnyObjectByType'ı runtime'da çağırmamak için
        private Transform _blobTransform;
        private BlobGrowth _blobGrowth;
        private ScoreSystem _scoreSystem;

        // Throttle: physics sorguları her frame değil, belirli aralıkla
        private float _aiUpdateTimer;
        private const float AIUpdateInterval = 0.15f;
        private bool _cachedIsCarAhead;
        private bool _cachedIsBlobDangerous;

        private readonly Collider[] _pushHits = new Collider[16];

        public void Initialize(CarData data, Transform[] waypoints)
        {
            _data = data;
            _waypoints = waypoints;
            _currentWaypointIndex = 0;
            _damageTimer = 0f;
            _isActive = true;
            _state = CarState.Moving;
            _currentSpeed = data.MoveSpeed;
            _aiUpdateTimer = UnityEngine.Random.Range(0f, AIUpdateInterval); // stagger — hepsi aynı anda çalışmasın

            if (_blobTransform == null)
            {
                GameObject blob = GameObject.FindWithTag("Blob");
                if (blob != null)
                {
                    _blobTransform = blob.transform;
                    _blobGrowth = blob.GetComponent<BlobGrowth>();
                }
            }

            if (_scoreSystem == null)
                _scoreSystem = FindAnyObjectByType<ScoreSystem>();

            if (_waypoints != null && _waypoints.Length > 0)
            {
                transform.position = new Vector3(_waypoints[0].position.x, _data.SpawnYOffset, _waypoints[0].position.z);
                transform.rotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            if (!_isActive || _waypoints == null || _waypoints.Length == 0) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            // AI sorgularını throttle et
            _aiUpdateTimer -= Time.deltaTime;
            if (_aiUpdateTimer <= 0f)
            {
                _aiUpdateTimer = AIUpdateInterval;
                _cachedIsCarAhead = IsCarAhead();
                _cachedIsBlobDangerous = IsBlobDangerous();
            }

            UpdateState();
            UpdateSpeed();

            if (_currentSpeed > 0.01f)
            {
                MoveTowardsWaypoint();
                PushNearbyConsumables();
            }

            _damageTimer -= Time.deltaTime;
        }

        private void UpdateState()
        {
            _state = (_cachedIsCarAhead || _cachedIsBlobDangerous) ? CarState.Stopped : CarState.Moving;
        }

        private bool IsCarAhead()
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _data.CarDetectionDistance))
                return hit.collider.GetComponent<CarController>() != null;
            return false;
        }

        private bool IsBlobDangerous()
        {
            if (_blobTransform == null || _blobGrowth == null) return false;
            float distSq = (transform.position - _blobTransform.position).sqrMagnitude;
            float radiusSq = _data.BlobDetectionRadius * _data.BlobDetectionRadius;
            if (distSq > radiusSq) return false;
            return _blobGrowth.CurrentTier >= _data.ConsumableFromTier;
        }

        private void UpdateSpeed()
        {
            float targetSpeed = _state == CarState.Moving ? _data.MoveSpeed : 0f;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _data.BrakeSpeed * Time.deltaTime);
        }

        private void MoveTowardsWaypoint()
        {
            Transform target = _waypoints[_currentWaypointIndex];

            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(target.position.x, 0f, target.position.z);
            Vector3 direction = (targetPos - myPos).normalized;

            transform.position += direction * _currentSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(myPos, targetPos) <= _data.WaypointReachDistance)
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
        }

        private void PushNearbyConsumables()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 1.2f, _pushHits);
            for (int i = 0; i < count; i++)
            {
                if (_pushHits[i].GetComponent<IConsumable>() == null) continue;
                Transform t = _pushHits[i].transform;
                Vector3 pushDir = (t.position - transform.position).normalized;
                if (pushDir == Vector3.zero) pushDir = transform.right;
                t.position = Vector3.MoveTowards(t.position, t.position + pushDir, 8f * Time.deltaTime);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive) return;

            BlobGrowth blobGrowth = collision.gameObject.GetComponentInParent<BlobGrowth>();
            if (blobGrowth == null) return;

            if (blobGrowth.CurrentTier >= _data.ConsumableFromTier)
            {
                blobGrowth.AddMass(_data.MassValue);
                _scoreSystem?.AddScore(_data.ScoreValue);
                Consume();
            }
            else
            {
                if (_damageTimer > 0f) return;
                collision.gameObject.GetComponentInParent<BlobHealth>()?.TakeDamage(_data.DamageAmount);
                _damageTimer = _data.DamageCooldown;
            }
        }

        private void Consume()
        {
            _isActive = false;
            OnConsumed?.Invoke(this);
        }
    }
}
