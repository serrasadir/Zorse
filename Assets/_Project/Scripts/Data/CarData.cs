using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Data
{
    [CreateAssetMenu(fileName = "CarData", menuName = "BlobSurvivor/Car Data")]
    public class CarData : ScriptableObject
    {
        [Header("Hareket")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _waypointReachDistance = 0.3f;

        [Header("Hasar (Blob'a)")]
        [SerializeField] private float _damageAmount = 10f;
        [SerializeField] private float _damageCooldown = 0.5f;

        [Header("Consumable (Blob yediğinde)")]
        [SerializeField] private float _massValue = 15f;
        [SerializeField] private int _scoreValue = 50;
        [SerializeField] private BlobTier _consumableFromTier = BlobTier.Large;

        public float MoveSpeed => _moveSpeed;
        public float WaypointReachDistance => _waypointReachDistance;
        public float DamageAmount => _damageAmount;
        public float DamageCooldown => _damageCooldown;
        public float MassValue => _massValue;
        public int ScoreValue => _scoreValue;
        public BlobTier ConsumableFromTier => _consumableFromTier;
    }
}
