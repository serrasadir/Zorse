using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class CannonWeapon : WeaponBase
    {
        protected override void Fire(Transform target)
        {
            Vector3 direction = GetPredictedAimPoint(target) - transform.position;
            direction.y = 0f;
            SpawnProjectile(direction);
        }
    }
}
