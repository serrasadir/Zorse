using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Vehicles
{
    public class CarSpawner : MonoBehaviour
    {
        [Header("Araç Ayarları")]
        [SerializeField] private CarController[] _carPrefabs;
        [SerializeField] private CarData _carData;

        [Header("Yol Waypoint'leri")]
        [SerializeField] private Transform[] _waypoints;

        [Header("Spawn Ayarları")]
        [SerializeField] private float _spawnInterval = 3f;
        [SerializeField] private int _maxActiveCars = 3;
        [SerializeField] private int _poolSize = 6;

        private ObjectPool<CarController>[] _pools;
        private readonly Dictionary<CarController, ObjectPool<CarController>> _ownerPools = new Dictionary<CarController, ObjectPool<CarController>>();
        private float _spawnTimer;
        private int _activeCarCount;

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRestart;

        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRestart;

        private void HandleRestart(CharacterData _)
        {
            foreach (KeyValuePair<CarController, ObjectPool<CarController>> owner in _ownerPools)
            {
                if (owner.Key.gameObject.activeSelf)
                    owner.Value.Return(owner.Key);
            }

            _activeCarCount = 0;
            _spawnTimer = 0f;
        }

        private void Start()
        {
            _pools = new ObjectPool<CarController>[_carPrefabs.Length];
            for (int i = 0; i < _carPrefabs.Length; i++)
                _pools[i] = new ObjectPool<CarController>(_carPrefabs[i], _poolSize, transform);
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
            if (_activeCarCount >= _maxActiveCars || _pools.Length == 0) return;

            Vector3 wp = _waypoints[0].position;
            Vector3 spawnPos = new Vector3(wp.x, _carData.SpawnYOffset, wp.z);
            ObjectPool<CarController> pool = _pools[Random.Range(0, _pools.Length)];
            CarController car = pool.Get(spawnPos, Quaternion.identity);
            if (car == null) return;

            _ownerPools[car] = pool;

            car.OnConsumed -= OnCarConsumed;
            car.OnConsumed += OnCarConsumed;
            car.Initialize(_carData, _waypoints);
            _activeCarCount++;
        }

        private void OnCarConsumed(CarController car)
        {
            _ownerPools[car].Return(car);
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
