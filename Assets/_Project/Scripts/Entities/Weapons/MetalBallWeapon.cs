using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class MetalBallWeapon : WeaponBase
    {
        protected override void Fire(Transform target)
        {
            // Son forma kadar herkesle aynı düz mermi; Sv.8 "Sekme"de güdümlü olur (sekme hedefi güdümle bulur).
            Vector3 direction = GetAimDirection(target, lead: !UltimateActive);

            Projectile projectile = SpawnProjectile(direction);
            if (UltimateActive && projectile is HomingProjectile homing)
                homing.SetTarget(target);
        }
    }
}
