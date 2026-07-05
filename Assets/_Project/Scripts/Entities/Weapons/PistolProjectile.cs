using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities;

namespace BlobSurvivor.Entities.Weapons
{
    public class PistolProjectile : Projectile
    {
        [SerializeField] private BlobTier _splitAboveTier = BlobTier.Small;
        [SerializeField] private int _splitCount = 3;

        protected override void OnHitOther(Collider other)
        {
            ConsumableBase consumable = other.GetComponent<ConsumableBase>();
            if (consumable == null || consumable.RequiredTier <= _splitAboveTier) return;

            ConsumableSpawner.Instance?.ConsumeAndSplit(consumable, _splitCount);
            ReturnToPool();
        }
    }
}
