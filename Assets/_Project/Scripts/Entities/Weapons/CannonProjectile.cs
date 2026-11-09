using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    // Son forma kadar herkesle aynı düz mermi. Silah Gücü Sv.8 (Topik son formu "Büyük Patlama"):
    // isabette _aoeRadius × _ultimateAoeMultiplier içindeki diğer düşmanlara da tam hasar.
    public class CannonProjectile : Projectile
    {
        [SerializeField] private float _aoeRadius = 2f;
        [SerializeField] private float _ultimateAoeMultiplier = 2.5f;

        private const int EnemyLayerMask = 1 << 14;
        private readonly Collider[] _hits = new Collider[16];

        protected override void OnHitEnemy(EnemyBase enemy)
        {
            if (!IsUltimate) return;

            float radius = _aoeRadius * _ultimateAoeMultiplier;
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, EnemyLayerMask);
            for (int i = 0; i < count; i++)
            {
                EnemyBase splash = _hits[i].GetComponent<EnemyBase>();
                if (splash != null && splash != enemy)
                {
                    splash.TakeDamage(Damage);
                    splash.Knockback(splash.transform.position - transform.position, 0.3f); // patlamadan dışa
                }
            }
        }
    }
}
