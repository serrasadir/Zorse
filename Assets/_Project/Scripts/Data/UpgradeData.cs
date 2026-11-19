using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Data
{
    // Skill türü (design/gdd/skill-sistemi.md): aktif ve pasif ayrı slotlara girer (3 + 3). Nadir kart slot tutmaz.
    public enum SkillKind { Active, Passive, Rare }

    [CreateAssetMenu(fileName = "UpgradeData", menuName = "BlobSurvivor/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private SkillKind _kind = SkillKind.Passive;
        [SerializeField] private int _weight = 10;
        [SerializeField] private UpgradeEffect _effect;

        [Header("Leveling")]
        [SerializeField] private int _maxLevel = 5;
        [SerializeField] private float _perLevelValue;
        [Tooltip("İsteğe bağlı: seviyeye özel kart açıklaması (0. eleman = Sv.1). Boş/eksikse Description gösterilir. " +
                 "{ultimate} yazısı aktif silahın son form açıklamasıyla değiştirilir.")]
        [SerializeField] private string[] _levelDescriptions;

        [Header("Evrim (B17)")]
        [Tooltip("Evrim çıktısı: kart havuzunda yok, EvolutionSystem verir (HUD yazısı ★ ile).")]
        [SerializeField] private bool _isEvolution;

        [Header("HUD")]
        [Tooltip("2 harflik kısaltma (ikon gelene kadar slot şeridinde). Boşsa adın ilk 2 harfi.")]
        [SerializeField] private string _abbr;
        [Tooltip("Temel silah (Sopa): Sv.0'da da sahip olunan gibi görünür.")]
        [SerializeField] private bool _isBaseWeapon;
        private string _abbrCache;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public SkillKind Kind => _kind;
        public int Weight => _weight;
        public UpgradeEffect Effect => _effect;
        public int MaxLevel => _maxLevel;
        public float PerLevelValue => _perLevelValue;
        public bool IsEvolution => _isEvolution;
        public bool IsBaseWeapon => _isBaseWeapon;

        public string Abbreviation
        {
            get
            {
                if (!string.IsNullOrEmpty(_abbr)) return _abbr;
                if (_abbrCache == null)
                {
                    string name = _displayName ?? "";
                    _abbrCache = (name.Length > 2 ? name.Substring(0, 2) : name).ToUpper(new System.Globalization.CultureInfo("tr-TR"));
                }
                return _abbrCache;
            }
        }
        // Evrim ve nadir kart slot tutmaz.
        public bool UsesSlot => !_isEvolution && _kind != SkillKind.Rare;

        // Seçilince ulaşılacak seviyenin açıklaması; tanımlı değilse genel açıklama.
        public string GetDescriptionForLevel(int level)
        {
            int index = level - 1;
            if (_levelDescriptions != null && index >= 0 && index < _levelDescriptions.Length
                && !string.IsNullOrEmpty(_levelDescriptions[index]))
                return _levelDescriptions[index];
            return _description;
        }

#if UNITY_EDITOR
        // Skill Setini Kur menüsü (Editor) için.
        public void EditorSetup(string id, string displayName, SkillKind kind, int weight, UpgradeEffect effect,
            int maxLevel, float perLevel, string[] levelDescriptions, string abbr, bool isBaseWeapon)
        {
            _abbr = abbr; _isBaseWeapon = isBaseWeapon; _abbrCache = null;
            _id = id; _displayName = displayName; _kind = kind; _weight = weight; _effect = effect;
            _maxLevel = maxLevel; _perLevelValue = perLevel; _levelDescriptions = levelDescriptions;
            _description = levelDescriptions != null && levelDescriptions.Length > 0 ? levelDescriptions[0] : displayName;
        }

        // Evrim çıktısı (tek seviye, slot harcamaz).
        public void EditorSetupEvolution(string id, string displayName, string description, UpgradeEffect effect, string abbr, int weight = 10)
        {
            EditorSetup(id, displayName, SkillKind.Passive, weight, effect, 1, 0f, new[] { description }, abbr, false);
            _isEvolution = true;
        }

#endif
    }
}
