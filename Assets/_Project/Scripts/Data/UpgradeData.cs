using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Data
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "BlobSurvivor/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private int _weight = 10;
        [SerializeField] private UpgradeEffect _effect;

        [Header("Leveling")]
        [SerializeField] private int _maxLevel = 8;
        [SerializeField] private float _perLevelValue;
        [Tooltip("İsteğe bağlı: seviyeye özel kart açıklaması (0. eleman = Sv.1). Boş/eksikse Description gösterilir. " +
                 "{ultimate} yazısı aktif silahın son form açıklamasıyla değiştirilir.")]
        [SerializeField] private string[] _levelDescriptions;

        [Tooltip("0 = yok. Oyuncu bu seviyeye gelince (henüz almadıysa) ilk kart ekranında bir kez garanti çıkar — " +
                 "ör. Havai Fişek 1. miniboss'tan (Sv.5) önce.")]
        [SerializeField] private int _guaranteedAtLevel;

        [Header("Evrim (B17)")]
        [Tooltip("true ise UpgradePanel bu kartı görsel olarak (★ + altın renk) işaretler — EvolutionSystem tarafından havuza dinamik eklenen çıktı skillerinde kullanılır.")]
        [SerializeField] private bool _isEvolution;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int Weight => _weight;
        public UpgradeEffect Effect => _effect;
        public int MaxLevel => _maxLevel;
        public float PerLevelValue => _perLevelValue;
        public bool IsEvolution => _isEvolution;
        public int GuaranteedAtLevel => _guaranteedAtLevel;

        // Seçilince ulaşılacak seviyenin açıklaması; tanımlı değilse genel açıklama.
        public string GetDescriptionForLevel(int level)
        {
            int index = level - 1;
            if (_levelDescriptions != null && index >= 0 && index < _levelDescriptions.Length
                && !string.IsNullOrEmpty(_levelDescriptions[index]))
                return _levelDescriptions[index];
            return _description;
        }
    }
}
