using BlobSurvivor.Core;
using UnityEngine;

namespace BlobSurvivor.Entities
{
    // Restart sahneyi yeniden yüklemiyor (bkz. GameManager.ApplyCharacter), bu yüzden yenen şehir
    // objeleri run bitince ve run başında (duraklatmadan yeniden başla / ana menü) burada geri açılır.
    public class CityLevel : MonoBehaviour
    {
        private CityConsumable[] _consumables;

        private void Awake()
        {
            _consumables = GetComponentsInChildren<CityConsumable>(true);
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += RestoreAll;
            GameEvents.OnRunComplete += HandleRunComplete;
            GameEvents.OnCharacterSelected += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= RestoreAll;
            GameEvents.OnRunComplete -= HandleRunComplete;
            GameEvents.OnCharacterSelected -= HandleRunStarted;
        }

        private void HandleRunComplete(RunEndReason reason) => RestoreAll();
        private void HandleRunStarted(Data.CharacterData _) => RestoreAll();

        private void RestoreAll()
        {
            foreach (CityConsumable consumable in _consumables)
            {
                if (consumable.gameObject.activeSelf) continue;
                consumable.gameObject.SetActive(true);
                consumable.IgnoreBlob();
            }
        }
    }
}
