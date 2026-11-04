using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class CannonWeapon : WeaponBase
    {
        protected override void Fire(Transform target)
        {
            SpawnProjectile(GetAimDirection(target, lead: true));
        }
    }
}
