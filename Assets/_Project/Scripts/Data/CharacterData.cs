using UnityEngine;

namespace BlobSurvivor.Data
{
    // Karakter avantajı: kendi skill'inin 2. seviyesiyle başlar (_startingUpgrade).
    [CreateAssetMenu(fileName = "Character", menuName = "BlobSurvivor/Character")]
    public class CharacterData : ScriptableObject
    {
        [Header("Kimlik")]
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;

        [Header("Görünüm")]
        [Tooltip("Blob'un child'ı olarak takılan model (yüz +Z'de, çap 1, pivot merkez). Boş = varsayılan küre.")]
        [SerializeField] private GameObject _blobVisualPrefab;
        [Tooltip("Zırh kalkanı yanda süzülmek yerine kafada şapka gibi durur (Fişek).")]
        [SerializeField] private bool _armorOnHead;
        [Tooltip("Hızlı giderken arkada hafif rüzgar çizgileri (Topik: hız pasifi).")]
        [SerializeField] private bool _speedTrail;

        [Header("Kart Çekimi")]
        [Tooltip("Bu karakterde kart ekranında daha sık çıkan skill (Topik Hız, Mıknato Vakum, Fişek Havai Fişek, Dalcık Sopa, Cano Geri Dönüşüm).")]
        [SerializeField] private UpgradeData _favoredUpgrade;
        [Tooltip("Favori skill'in ağırlık çarpanı. 3 ≈ kart ekranlarının %41'i yerine %75'inde çıkar.")]
        [SerializeField] private float _favoredWeightMultiplier = 3f;
        [Tooltip("Run'a bu skill bu seviyede başlar (karakter pasifi). 0 = yok.")]
        [SerializeField] private UpgradeData _startingUpgrade;
        [SerializeField] private int _startingUpgradeLevel;

        [Header("Kilit")]
        [Tooltip("Kayıtta karakterin kimliği (satın alım). Değiştirme: alınmış karakter kilitlenir.")]
        [SerializeField] private string _saveId;
        [Tooltip("Market → Karakterler sekmesinde coin fiyatı. 0 = satılmaz.")]
        [SerializeField] private int _marketPrice;
        [Tooltip("Bu kadar run bitirilince açılır (ölümle biten run da sayılır). 0 = koşul yok. Dalcık: 5.")]
        [SerializeField] private int _requiredCompletedRuns;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public GameObject BlobVisualPrefab => _blobVisualPrefab;
        public bool ArmorOnHead => _armorOnHead;
        public bool SpeedTrail => _speedTrail;
        public UpgradeData FavoredUpgrade => _favoredUpgrade;
        public float FavoredWeightMultiplier => _favoredWeightMultiplier;
        public UpgradeData StartingUpgrade => _startingUpgrade;
        public int StartingUpgradeLevel => _startingUpgradeLevel;
        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;
        public int MarketPrice => _marketPrice;
        // Market'in Karakterler sekmesinde: satılan ya da run'la açılan (Topik ikisi de değil).
        public bool ShownInMarket => _marketPrice > 0 || _requiredCompletedRuns > 0;
        public int RequiredCompletedRuns => _requiredCompletedRuns;
    }
}
