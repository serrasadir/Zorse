using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Data
{
    public enum UpgradeCategory
    {
        Mobility,
        Defense,
        Magnetic,
        Weapon,
        Feeding // B16 (GDD_v2.md §5, Karar 5 — yeme birincil): sona eklendi, mevcut asset'lerin int değerleri kaymasın diye.
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

        [Header("Evrim (B17)")]
        [Tooltip("true ise UpgradePanel bu kartı görsel olarak (★ + altın renk) işaretler — EvolutionSystem tarafından havuza dinamik eklenen çıktı skillerinde kullanılır.")]
        [SerializeField] private bool _isEvolution;

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
        public bool IsEvolution => _isEvolution;
    }
}
