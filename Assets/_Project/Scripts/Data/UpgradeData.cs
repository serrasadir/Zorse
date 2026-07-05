using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Data
{
    public enum UpgradeCategory
    {
        Mobility,
        Defense,
        Magnetic,
        Weapon
    }

    [CreateAssetMenu(fileName = "UpgradeData", menuName = "BlobSurvivor/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private UpgradeCategory _category;
        [SerializeField] private int _weight = 10;
        [SerializeField] private float _effectValue;
        [SerializeField] private float _effectDuration;
        [SerializeField] private float _cooldown;
        [SerializeField] private UpgradeEffect _effect;

        [Header("Leveling")]
        [SerializeField] private int _maxLevel = 8;
        [SerializeField] private float _perLevelValue;

        [System.NonSerialized] public int CurrentLevel;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public UpgradeCategory Category => _category;
        public int Weight => _weight;
        public float EffectValue => _effectValue;
        public float EffectDuration => _effectDuration;
        public float Cooldown => _cooldown;
        public UpgradeEffect Effect => _effect;
        public int MaxLevel => _maxLevel;
        public float PerLevelValue => _perLevelValue;
    }
}
