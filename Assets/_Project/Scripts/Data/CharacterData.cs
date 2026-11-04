using UnityEngine;

namespace BlobSurvivor.Data
{
    public enum CharacterPassiveType
    {
        MoveSpeed,
        MagnetPull,
        ConsumableSplit
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
        public CharacterPassiveType PassiveType => _passiveType;
        public float PassiveValue => _passiveValue;
        public bool RequiresMarketUnlock => _requiresMarketUnlock;
        public int RequiredCompletedRuns => _requiredCompletedRuns;
    }
}
