using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Data
{
    [CreateAssetMenu(fileName = "ConsumableData", menuName = "BlobSurvivor/Consumable Data")]
    public class ConsumableData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _scoreValue;
        [SerializeField] private float _massValue;
        [SerializeField] private float _objectSize;
        [SerializeField] private BlobTier _requiredTier;
        [SerializeField] private bool _isHazard;
        [SerializeField] private float _hazardAmount;
        [SerializeField] private float _spawnYOffset = 0.15f;

        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public int ScoreValue => _scoreValue;
        public float MassValue => _massValue;
        public float ObjectSize => _objectSize;
        public BlobTier RequiredTier => _requiredTier;
        public bool IsHazard => _isHazard;
        public float HazardAmount => _hazardAmount;
        public float SpawnYOffset => _spawnYOffset;
    }
}
