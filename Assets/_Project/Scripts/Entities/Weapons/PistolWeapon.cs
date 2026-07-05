using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class PistolWeapon : WeaponBase
    {
        protected override void Fire(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            SpawnProjectile(direction);
        }
    }
}
