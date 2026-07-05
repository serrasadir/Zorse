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

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public GameObject StartingWeaponPrefab => _startingWeaponPrefab;
        public CharacterPassiveType PassiveType => _passiveType;
        public float PassiveValue => _passiveValue;
    }
}
