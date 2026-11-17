using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Loot
{
    public class ChestSpawner : MonoBehaviour
    {
        public static ChestSpawner Instance { get; private set; }

        [SerializeField] private ChestPickup _chestPrefab;
        [SerializeField] private int _poolSize = 8;
        [SerializeField] private float _spawnYOffset = 0.35f;

        private ObjectPool<ChestPickup> _pool;
        private ScoreSystem _scoreSystem;
        private readonly List<ChestPickup> _activeChests = new List<ChestPickup>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRestart;

        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRestart;

        private void HandleRestart(CharacterData _)
        {
            foreach (ChestPickup chest in _activeChests)
                _pool.Return(chest);
            _activeChests.Clear();
        }

        private void Start()
        {
            if (_chestPrefab != null)
            {
                _pool = PoolManager.Instance != null
                    ? PoolManager.Instance.CreatePool(_chestPrefab, _poolSize)
                    : new ObjectPool<ChestPickup>(_chestPrefab, _poolSize, transform);
            }

            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
        }

        public ChestPickup SpawnChest(Vector3 position)
        {
            if (_pool == null) return null;

            Vector3 spawnPos = new Vector3(position.x, _spawnYOffset, position.z);
            ChestPickup chest = _pool.Get(spawnPos, Quaternion.identity);
            chest.Initialize(_scoreSystem);
            chest.OnCollected -= HandleCollected;
            chest.OnCollected += HandleCollected;
            _activeChests.Add(chest);
            return chest;
        }

        // Toplanmadan kaldırılan sandık (süresi dolan görev sandığı).
        public void Remove(ChestPickup chest)
        {
            if (chest == null || !_activeChests.Remove(chest)) return;
            _pool.Return(chest);
        }

        private void HandleCollected(ChestPickup chest)
        {
            _activeChests.Remove(chest);
            _pool.Return(chest);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
