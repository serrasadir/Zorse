using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class MetalBallWeapon : WeaponBase
    {
        protected override void Fire(Transform target)
        {
            Vector3 direction = GetAimDirection(target, lead: false);

            Projectile projectile = SpawnProjectile(direction);
            if (projectile is HomingProjectile homing)
                homing.SetTarget(target);
        }
    }
}
