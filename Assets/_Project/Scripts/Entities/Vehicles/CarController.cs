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

        private CarData _data;
        private Transform[] _waypoints;
        private int _currentWaypointIndex;
        private float _damageTimer;
        private bool _isActive;

        public void Initialize(CarData data, Transform[] waypoints)
        {
            _data = data;
            _waypoints = waypoints;
            _currentWaypointIndex = 0;
            _damageTimer = 0f;
            _isActive = true;

            if (_waypoints != null && _waypoints.Length > 0)
            {
                transform.position = _waypoints[0].position;
                transform.rotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            if (!_isActive || _waypoints == null || _waypoints.Length == 0) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            MoveTowardsWaypoint();
            PushNearbyConsumables();
            _damageTimer -= Time.deltaTime;
        }

        private readonly Collider[] _pushHits = new Collider[16];

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

        private void MoveTowardsWaypoint()
        {
            Transform target = _waypoints[_currentWaypointIndex];

            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(target.position.x, 0f, target.position.z);
            Vector3 direction = (targetPos - myPos).normalized;

            transform.position += direction * _data.MoveSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(myPos, targetPos) <= _data.WaypointReachDistance)
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            BlobGrowth blobGrowth = other.GetComponentInParent<BlobGrowth>();
            if (blobGrowth == null) return;

            if (blobGrowth.CurrentTier >= _data.ConsumableFromTier)
            {
                blobGrowth.AddMass(_data.MassValue);

                ScoreSystem scoreSystem = FindAnyObjectByType<ScoreSystem>();
                scoreSystem?.AddScore(_data.ScoreValue);

                Consume();
            }
            else
            {
                if (_damageTimer > 0f) return;
                other.GetComponentInParent<BlobHealth>()?.TakeDamage(_data.DamageAmount);
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
