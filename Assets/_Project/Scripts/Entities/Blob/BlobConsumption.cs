using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Blob
{
    public class BlobConsumption : MonoBehaviour
    {
        private BlobGrowth _blobGrowth;
        private BlobHealth _blobHealth;
        private int _consumedCount;

        private void Awake()
        {
            _blobGrowth = GetComponent<BlobGrowth>();
            _blobHealth = GetComponent<BlobHealth>();
        }

        private void OnTriggerEnter(Collider other)
        {
            IConsumable consumable = other.GetComponent<IConsumable>();
            if (consumable == null) return;

            if (consumable.RequiredTier <= _blobGrowth.CurrentTier)
                Consume(consumable, other.gameObject);
            else if (!consumable.Data.IsHazard)
                _blobHealth.TakeDamage(consumable.Data.MassValue * 0.5f);
        }

        private void Consume(IConsumable consumable, GameObject obj)
        {
            if (_blobGrowth == null) return;
            _blobGrowth.AddMass(consumable.Data.MassValue);
            _blobGrowth.PunchScale();

            ScoreSystem scoreSystem = FindAnyObjectByType<ScoreSystem>();
            scoreSystem?.AddScore(consumable.Data.ScoreValue);

            _consumedCount++;
            GameEvents.RaiseConsumedCountChanged(_consumedCount);

            consumable.OnConsumed();
            obj.SetActive(false);

#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
