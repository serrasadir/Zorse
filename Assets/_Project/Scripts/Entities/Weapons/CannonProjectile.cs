using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    public class CannonProjectile : Projectile
    {
        [SerializeField] private float _aoeRadius = 2f;
        [SerializeField] private float _arcHeight = 1.2f;

        private const int EnemyLayerMask = 1 << 14;
        private readonly Collider[] _hits = new Collider[16];
        private float _baseY;
        private bool _baseYSet;

        protected override void Update()
        {
            if (!_baseYSet)
            {
                _baseY = transform.position.y;
                _baseYSet = true;
            }

            base.Update();

            Vector3 pos = transform.position;
            pos.y = _baseY + Mathf.Sin(LifetimeFraction * Mathf.PI) * _arcHeight;
            transform.position = pos;
        }

        protected override void OnHitEnemy(EnemyBase enemy)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _aoeRadius, _hits, EnemyLayerMask);
            for (int i = 0; i < count; i++)
            {
                EnemyBase splash = _hits[i].GetComponent<EnemyBase>();
                if (splash != null && splash != enemy)
                    splash.TakeDamage(Damage);
            }
        }
    }
}
