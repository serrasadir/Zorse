using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Enemies;
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
            if (consumable != null)
            {
                if (consumable.RequiredTier <= _blobGrowth.CurrentTier)
                    Consume(consumable, other.gameObject);
                else if (consumable.Data.IsHazard)
                    _blobHealth.TakeDamage(consumable.Data.HazardAmount);
                return;
            }

            // Karar 5 (yeme birincil): sürü Tier3+, elit Tier5'te temasla yutulabilir (A12 API'si).
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null && enemy.TryConsumeByBlob(_blobGrowth.CurrentTier, out float massReward))
            {
                _blobGrowth.AddMass(massReward);
                _blobGrowth.PunchScale();
            }
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

            // B14: pool'a gerçek dönüş — önceden sadece SetActive(false) yapılıyordu, pool Queue'su
            // hiç dolmuyordu (ConsumeAndSplit'teki doğru pattern'in normal yeme akışına taşınması).
            ConsumableBase pooled = obj.GetComponent<ConsumableBase>();
            if (pooled != null)
                ConsumableSpawner.Instance?.ReturnToPool(pooled);
            else
                obj.SetActive(false);

#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
