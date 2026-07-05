using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities;

namespace BlobSurvivor.Entities.Weapons
{
    public class PistolProjectile : Projectile
    {
        [SerializeField] private BlobTier _splitAboveTier = BlobTier.Small;
        [SerializeField] private int _splitCount = 3;

        protected override bool OnHitOther(Collider other)
        {
            ConsumableBase consumable = other.GetComponent<ConsumableBase>();
            if (consumable == null || consumable.RequiredTier <= _splitAboveTier) return false;

            ConsumableSpawner.Instance?.ConsumeAndSplit(consumable, _splitCount);
            ReturnToPool();
            return true;
        }
    }
}
