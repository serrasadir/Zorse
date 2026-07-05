using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    public class HomingProjectile : Projectile
    {
        [SerializeField] private float _turnSpeed = 3f;
        [SerializeField] private float _detonateRadius = 0.6f;

        private Transform _target;

        public void SetTarget(Transform target) => _target = target;

        protected override void Update()
        {
            if (_target != null)
            {
                Vector3 toTarget = _target.position - transform.position;
                toTarget.y = 0f;

                // Dönüş hızı hedefi ıskalarsa mermi etrafında sonsuz tur atabilir (pure-pursuit
                // homing'in klasik sorunu) — yakınlık kontrolü bunu garanti şekilde kapatır.
                if (toTarget.sqrMagnitude <= _detonateRadius * _detonateRadius)
                {
                    EnemyBase enemy = _target.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(Damage);
                        OnHitEnemy(enemy);
                    }
                    ReturnToPool();
                    return;
                }

                if (toTarget.sqrMagnitude > 0.0001f)
                    Direction = Vector3.Slerp(Direction, toTarget.normalized, _turnSpeed * Time.deltaTime);
            }

            base.Update();
        }
    }
}
