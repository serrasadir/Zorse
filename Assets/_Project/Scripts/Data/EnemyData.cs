using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "BlobSurvivor/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private float _maxHealth;
        [SerializeField] private float _damage;
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _attackRange;
        [SerializeField] private float _attackCooldown;
        [SerializeField] private BlobTier _spawnTier;
        [SerializeField] private int _scoreValue;

        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public float MaxHealth => _maxHealth;
        public float Damage => _damage;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public BlobTier SpawnTier => _spawnTier;
        public int ScoreValue => _scoreValue;
    }
}
