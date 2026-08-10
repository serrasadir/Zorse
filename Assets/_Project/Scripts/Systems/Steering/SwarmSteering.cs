using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Systems
{
    public class SwarmSteering : MonoBehaviour
    {
        public static SwarmSteering Instance { get; private set; }

        [SerializeField] private float _cellSize = 3f;
        [SerializeField] private float _rebuildInterval = 0.1f;

        private readonly List<EnemyBase> _registered = new List<EnemyBase>();
        private readonly Dictionary<Vector2Int, List<EnemyBase>> _grid = new Dictionary<Vector2Int, List<EnemyBase>>();
        private float _rebuildTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            _rebuildTimer -= Time.deltaTime;
            if (_rebuildTimer > 0f) return;

            _rebuildTimer = _rebuildInterval;
            RebuildGrid();
        }

        public void Register(EnemyBase enemy)
        {
            if (!_registered.Contains(enemy))
                _registered.Add(enemy);
        }

        public void Unregister(EnemyBase enemy)
        {
            _registered.Remove(enemy);
        }

        private void RebuildGrid()
        {
            _grid.Clear();

            for (int i = 0; i < _registered.Count; i++)
            {
                EnemyBase enemy = _registered[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                Vector2Int cell = ToCell(enemy.transform.position);
                if (!_grid.TryGetValue(cell, out List<EnemyBase> list))
                {
                    list = new List<EnemyBase>();
                    _grid[cell] = list;
                }
                list.Add(enemy);
            }
        }

        // Yakın komşulardan uzaklaştıran normalize edilmemiş bir yön döner (sıfır vektör = yakın komşu yok).
        public Vector3 GetSeparationVector(EnemyBase self, float radius)
        {
            Vector3 result = Vector3.zero;
            Vector2Int center = ToCell(self.transform.position);
            float sqrRadius = radius * radius;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (!_grid.TryGetValue(new Vector2Int(center.x + dx, center.y + dz), out List<EnemyBase> list))
                        continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        EnemyBase other = list[i];
                        if (other == self || other == null) continue;

                        Vector3 offset = self.transform.position - other.transform.position;
                        float sqrDist = offset.sqrMagnitude;
                        if (sqrDist > sqrRadius || sqrDist < 0.0001f) continue;

                        result += offset.normalized / Mathf.Sqrt(sqrDist);
                    }
                }
            }

            return result;
        }

        private Vector2Int ToCell(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / _cellSize), Mathf.FloorToInt(position.z / _cellSize));
        }
    }
}
