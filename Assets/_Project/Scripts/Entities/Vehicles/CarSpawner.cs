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

        // 2026-11-03 (Serra: "arabaya neredeyse denk gelmiyorum"): büyük şehirde (Karar 15) 3 araba hâlâ
        // Level01'in ±19'luk iç döngüsünde dönüyordu — haritanın ~%8'i. Artık her araba, oyuncunun
        // yakınında rastgele seçilen bir blok çevresinde (1-3 blok) döner; uzaklaşınca havuza döner,
        // refill oyuncunun yakınına yenisini getirir (polis/yaya ile aynı desen). Rotalar sokak
        // ızgarasından hesaplanır, sahnede waypoint gerekmez.
        [Header("Şehir Izgarası (CityExpansion ile aynı olmalı)")]
        [SerializeField] private int _gridBlockCount = 11;      // CityExpansion.BlockCount
        [SerializeField] private float _gridBlockPitch = 12f;   // CityExpansion.BlockPitch
        // Sokak ekseninden şerit kayması. Döngüler saat yönünün tersine kurulduğu için dışa kayma
        // = sağ şerit; komşu döngüler aynı sokağı ters yönde, karşı şeritte kullanır.
        [SerializeField] private float _laneOffset = 1f;
        [SerializeField] private int _minLoopBlocks = 1;
        [SerializeField] private int _maxLoopBlocks = 3;
        [SerializeField] private int _maxActive = 6;
        [SerializeField] private float _spawnRange = 35f;
        [SerializeField] private float _minDistanceFromBlob = 18f; // ekranın dışında doğsun
        [SerializeField] private float _despawnDistance = 50f;

        [Header("Spawn Ayarları")]
        [SerializeField] private float _spawnInterval = 3f;
        [SerializeField] private int _poolSize = 6;

        private const float DespawnCheckInterval = 1f;
        private const float MinSpacingBetweenCars = 6f;
        private const int SpawnAttempts = 5;
        private const int RouteCorners = 4;

        private ObjectPool<CarController>[] _pools;
        private readonly Dictionary<CarController, ObjectPool<CarController>> _ownerPools = new Dictionary<CarController, ObjectPool<CarController>>();
        private readonly List<CarController> _activeCars = new List<CarController>();
        private readonly Vector3[] _routeBuffer = new Vector3[RouteCorners];
        private readonly Vector3[] _cornerBuffer = new Vector3[RouteCorners];
        private float[] _streetLines;
        private Transform _blobTransform;
        private float _spawnTimer;
        private float _despawnTimer;

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRestart;

        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRestart;

        private void HandleRestart(CharacterData _)
        {
            foreach (CarController car in _activeCars)
                ReturnCar(car);
            _activeCars.Clear();
            _spawnTimer = 0f;
        }

        private void Start()
        {
            _pools = new ObjectPool<CarController>[_carPrefabs.Length];
            for (int i = 0; i < _carPrefabs.Length; i++)
                _pools[i] = new ObjectPool<CarController>(_carPrefabs[i], _poolSize, transform);

            _streetLines = new float[_gridBlockCount + 1];
            for (int i = 0; i <= _gridBlockCount; i++)
                _streetLines[i] = (i - _gridBlockCount / 2f) * _gridBlockPitch;

            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _blobTransform = blob.transform;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            RecycleFarCars();

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = _spawnInterval;
                TrySpawnCar();
            }
        }

        private void TrySpawnCar()
        {
            if (_activeCars.Count >= _maxActive || _pools.Length == 0 || _carData == null) return;
            if (!TryBuildCityRoute()) return;

            Vector3 spawnPos = new Vector3(_routeBuffer[0].x, _carData.SpawnYOffset, _routeBuffer[0].z);
            ObjectPool<CarController> pool = _pools[Random.Range(0, _pools.Length)];
            CarController car = pool.Get(spawnPos, Quaternion.identity);
            if (car == null) return;

            _ownerPools[car] = pool;

            car.OnConsumed -= OnCarConsumed;
            car.OnConsumed += OnCarConsumed;
            car.Initialize(_carData, _routeBuffer, RouteCorners);
            _activeCars.Add(car);
        }

        // Oyuncunun yakınında 1-3 bloklu bir dikdörtgen seçer; ekran dışında kalan ve başka arabaya
        // yapışık olmayan bir köşeden başlatır. _routeBuffer[0] başlangıç köşesidir.
        private bool TryBuildCityRoute()
        {
            int blocks = _gridBlockCount;
            if (blocks < 1 || _blobTransform == null) return false;

            Vector3 blobPos = _blobTransform.position;
            float minDistSq = _minDistanceFromBlob * _minDistanceFromBlob;
            float maxDistSq = _spawnRange * _spawnRange;
            float spacingSq = MinSpacingBetweenCars * MinSpacingBetweenCars;
            int rangeCells = Mathf.CeilToInt(_spawnRange / _gridBlockPitch);
            int blobCellX = CellIndex(blobPos.x);
            int blobCellZ = CellIndex(blobPos.z);

            for (int attempt = 0; attempt < SpawnAttempts; attempt++)
            {
                int width = Mathf.Clamp(Random.Range(_minLoopBlocks, _maxLoopBlocks + 1), 1, blocks);
                int height = Mathf.Clamp(Random.Range(_minLoopBlocks, _maxLoopBlocks + 1), 1, blocks);
                int cellX = Mathf.Clamp(blobCellX + Random.Range(-rangeCells, rangeCells + 1), 0, blocks - 1);
                int cellZ = Mathf.Clamp(blobCellZ + Random.Range(-rangeCells, rangeCells + 1), 0, blocks - 1);
                int i0 = Mathf.Clamp(cellX - Random.Range(0, width), 0, blocks - width);
                int j0 = Mathf.Clamp(cellZ - Random.Range(0, height), 0, blocks - height);

                float x0 = _streetLines[i0] - _laneOffset;
                float x1 = _streetLines[i0 + width] + _laneOffset;
                float z0 = _streetLines[j0] - _laneOffset;
                float z1 = _streetLines[j0 + height] + _laneOffset;

                // Saat yönünün tersine (üstten bakınca) — dışa kayma sağ şeride denk gelir.
                _cornerBuffer[0] = new Vector3(x1, 0f, z0);
                _cornerBuffer[1] = new Vector3(x1, 0f, z1);
                _cornerBuffer[2] = new Vector3(x0, 0f, z1);
                _cornerBuffer[3] = new Vector3(x0, 0f, z0);

                int start = Random.Range(0, RouteCorners);
                for (int k = 0; k < RouteCorners; k++)
                {
                    int corner = (start + k) % RouteCorners;
                    Vector3 p = _cornerBuffer[corner];
                    Vector3 toBlob = p - blobPos;
                    toBlob.y = 0f;
                    float distSq = toBlob.sqrMagnitude;
                    if (distSq < minDistSq || distSq > maxDistSq) continue;
                    if (IsNearActiveCar(p, spacingSq)) continue;

                    for (int n = 0; n < RouteCorners; n++)
                        _routeBuffer[n] = _cornerBuffer[(corner + n) % RouteCorners];
                    return true;
                }
            }

            return false;
        }

        private int CellIndex(float coord)
        {
            int cell = Mathf.FloorToInt((coord - _streetLines[0]) / _gridBlockPitch);
            return Mathf.Clamp(cell, 0, _gridBlockCount - 1);
        }

        private bool IsNearActiveCar(Vector3 point, float spacingSq)
        {
            foreach (CarController car in _activeCars)
            {
                Vector3 d = car.transform.position - point;
                d.y = 0f;
                if (d.sqrMagnitude < spacingSq) return true;
            }
            return false;
        }

        private void RecycleFarCars()
        {
            _despawnTimer -= Time.deltaTime;
            if (_despawnTimer > 0f || _blobTransform == null) return;
            _despawnTimer = DespawnCheckInterval;

            Vector3 blobPos = _blobTransform.position;
            float despawnSq = _despawnDistance * _despawnDistance;
            for (int i = _activeCars.Count - 1; i >= 0; i--)
            {
                CarController car = _activeCars[i];
                if ((car.transform.position - blobPos).sqrMagnitude < despawnSq) continue;

                _activeCars.RemoveAt(i);
                ReturnCar(car);
            }
        }

        private void OnCarConsumed(CarController car)
        {
            _activeCars.Remove(car);
            ReturnCar(car);
        }

        private void ReturnCar(CarController car)
        {
            if (car != null && _ownerPools.TryGetValue(car, out ObjectPool<CarController> pool))
                pool.Return(car);
        }
    }
}
