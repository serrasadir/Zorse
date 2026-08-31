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

        [Header("Pasif")]
        [SerializeField] private CharacterPassiveType _passiveType;
        [SerializeField] private float _passiveValue;

        // M23 takip (#48): true ise LobbyPanel bu karakteri MetaProgression.MiknatoUnlocked
        // satın alınana kadar kilitli gösterir. Şu an lansımda Market'ten unlock edilen tek
        // karakter Mıknato olduğu için ayrı bir unlock-tipi alanı gerekmiyor.
        [Header("Kilit (Market)")]
        [SerializeField] private bool _requiresMarketUnlock;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public GameObject StartingWeaponPrefab => _startingWeaponPrefab;
        public CharacterPassiveType PassiveType => _passiveType;
        public float PassiveValue => _passiveValue;
        public bool RequiresMarketUnlock => _requiresMarketUnlock;
    }
}
