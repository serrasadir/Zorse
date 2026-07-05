using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class MetalBallWeapon : WeaponBase
    {
        protected override void Fire(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            Projectile projectile = SpawnProjectile(direction);
            if (projectile is HomingProjectile homing)
                homing.SetTarget(target);
        }
    }
}
