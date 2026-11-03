using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class PistolWeapon : WeaponBase
    {
        // Silah Gücü Sv.8 (Mermo son formu "Pompalı"): her atış yelpaze şeklinde saçma.
        [SerializeField] private int _ultimatePellets = 5;
        [SerializeField] private float _ultimateSpreadAngle = 40f;
        [SerializeField] private float _ultimatePelletDamageScale = 0.6f;

        protected override void Fire(Transform target)
        {
            Vector3 direction = GetPredictedAimPoint(target) - transform.position;
            direction.y = 0f;

            if (!UltimateActive || _ultimatePellets <= 1)
            {
                SpawnProjectile(direction);
                return;
            }

            float step = _ultimateSpreadAngle / (_ultimatePellets - 1);
            float start = -_ultimateSpreadAngle * 0.5f;
            for (int i = 0; i < _ultimatePellets; i++)
            {
                Vector3 pelletDir = Quaternion.Euler(0f, start + step * i, 0f) * direction;
                SpawnProjectile(pelletDir, _ultimatePelletDamageScale);
            }
        }
    }
}
