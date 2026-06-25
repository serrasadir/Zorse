using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Vehicles
{
    public class CarSpawner : MonoBehaviour
    {
        [Header("Araç Ayarları")]
        [SerializeField] private CarController _carPrefab;
        [SerializeField] private CarData _carData;

        [Header("Yol Waypoint'leri")]
        [SerializeField] private Transform[] _waypoints;

        [Header("Spawn Ayarları")]
        [SerializeField] private float _spawnInterval = 3f;
        [SerializeField] private int _maxActiveCars = 3;
        [SerializeField] private int _poolSize = 6;

        private ObjectPool<CarController> _pool;
        private float _spawnTimer;
        private int _activeCarCount;

        private void Start()
        {
            _pool = new ObjectPool<CarController>(_carPrefab, _poolSize, transform);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_waypoints == null || _waypoints.Length < 2) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = _spawnInterval;
                TrySpawnCar();
            }
        }

        private void TrySpawnCar()
        {
            if (_activeCarCount >= _maxActiveCars) return;

            Vector3 spawnPos = _waypoints[0].position;
            CarController car = _pool.Get(spawnPos, Quaternion.identity);
            if (car == null) return;

            car.OnConsumed -= OnCarConsumed;
            car.OnConsumed += OnCarConsumed;
            car.Initialize(_carData, _waypoints);
            _activeCarCount++;
        }

        private void OnCarConsumed(CarController car)
        {
            _pool.Return(car);
            _activeCarCount = Mathf.Max(0, _activeCarCount - 1);
        }

        private void OnDrawGizmos()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;
                Gizmos.DrawSphere(_waypoints[i].position, 0.3f);

                int next = (i + 1) % _waypoints.Length;
                if (_waypoints[next] != null)
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[next].position);
            }
        }
    }
}
