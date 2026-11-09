using UnityEngine;

namespace BlobSurvivor.Data
{
    // Karakter avantajı çoğunlukla başlangıç skill'i (_startingUpgrade). Pasif sadece Mıknato'da (çekim).
    public enum CharacterPassiveType
    {
        None,
        MagnetPull
    }

    [CreateAssetMenu(fileName = "Character", menuName = "BlobSurvivor/Character")]
    public class CharacterData : ScriptableObject
    {
        [Header("Kimlik")]
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;

        [Header("Başlangıç Silahı")]
        [SerializeField] private GameObject _startingWeaponPrefab;

        [Header("Görünüm")]
        [Tooltip("Blob'un child'ı olarak takılan model (yüz +Z'de, çap 1, pivot merkez). Boş = varsayılan küre.")]
        [SerializeField] private GameObject _blobVisualPrefab;
        [Tooltip("Zırh kalkanı yanda süzülmek yerine kafada şapka gibi durur (Mermo: yanlarda keleşleri var).")]
        [SerializeField] private bool _armorOnHead;
        [Tooltip("Hızlı giderken arkada hafif rüzgar çizgileri (Topik: hız pasifi).")]
        [SerializeField] private bool _speedTrail;
        [Tooltip("Silah Gücü görseli: Resources/FX/Weapons/G_<bu>_1..4 (build_weapons.py). Boş = silah görünmez.")]
        [SerializeField] private string _weaponModel;
        [Tooltip("Silah Sv.0'dan görünür (Mermo). Kapalıysa Silah Gücü alınınca belirir.")]
        [SerializeField] private bool _weaponVisibleFromStart;

        [Header("Kart Çekimi")]
        [Tooltip("Bu karakterde kart ekranında daha sık çıkan skill (Topik Hız, Mıknato Vakum, Mermo Silah Gücü).")]
        [SerializeField] private UpgradeData _favoredUpgrade;
        [Tooltip("Favori skill'in ağırlık çarpanı. 2 ≈ kart ekranlarının %41'i yerine %63'ünde çıkar.")]
        [SerializeField] private float _favoredWeightMultiplier = 2f;
        [Tooltip("Run'a bu skill bu seviyede başlar (ör. Mermo: Silah Gücü Sv.2). 0 = yok.")]
        [SerializeField] private UpgradeData _startingUpgrade;
        [SerializeField] private int _startingUpgradeLevel;

        [Header("Pasif")]
        [SerializeField] private CharacterPassiveType _passiveType;
        [SerializeField] private float _passiveValue;

        // Kilit: true ise Market'te MiknatoCharacter satın alınana kadar kilitli (sadece Mıknato).
        [Header("Kilit")]
        [SerializeField] private bool _requiresMarketUnlock;
        [Tooltip("Bu kadar run bitirilince açılır (ölümle biten run da sayılır). 0 = koşul yok. Mermo: 3.")]
        [SerializeField] private int _requiredCompletedRuns;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public GameObject StartingWeaponPrefab => _startingWeaponPrefab;
        public GameObject BlobVisualPrefab => _blobVisualPrefab;
        public bool ArmorOnHead => _armorOnHead;
        public bool SpeedTrail => _speedTrail;
        public string WeaponModel => _weaponModel;
        public bool WeaponVisibleFromStart => _weaponVisibleFromStart;
        public UpgradeData FavoredUpgrade => _favoredUpgrade;
        public float FavoredWeightMultiplier => _favoredWeightMultiplier;
        public UpgradeData StartingUpgrade => _startingUpgrade;
        public int StartingUpgradeLevel => _startingUpgradeLevel;
        public CharacterPassiveType PassiveType => _passiveType;
        public float PassiveValue => _passiveValue;
        public bool RequiresMarketUnlock => _requiresMarketUnlock;
        public int RequiredCompletedRuns => _requiredCompletedRuns;
    }
}
