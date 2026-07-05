using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private float _hitRadius = 0.3f;

        private const int EnemyLayer = 14;
        // Enemy(14) + ConsumableTier1-5(9-13) — Rigidbody/Collider gerektirmez, doğrudan spatial sorgu
        private const int HitMask = (1 << 14) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13);

        private WeaponBase _owner;
        private float _elapsed;

        protected float Damage { get; private set; }
        protected Vector3 Direction { get; set; }
        protected float LifetimeFraction => _lifetime > 0f ? Mathf.Clamp01(_elapsed / _lifetime) : 0f;

        public void Launch(WeaponBase owner, Vector3 direction, float damage)
        {
            _owner = owner;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Damage = damage;
            _elapsed = 0f;
        }

        protected virtual void Update()
        {
            float travelDistance = _speed * Time.deltaTime;

            if (Physics.SphereCast(transform.position, _hitRadius, Direction, out RaycastHit hit, travelDistance, HitMask))
            {
                transform.position = hit.point;
                bool stopped = HandleHit(hit.collider);
                if (stopped) return;

                float remaining = travelDistance - hit.distance;
                if (remaining > 0f)
                    transform.position += Direction * remaining;
            }
            else
            {
                transform.position += Direction * travelDistance;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
                ReturnToPool();
        }

        private bool HandleHit(Collider other)
        {
            if (other.gameObject.layer == EnemyLayer)
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(Damage);
                    OnHitEnemy(enemy);
                    ReturnToPool();
                    return true;
                }
            }

            return OnHitOther(other);
        }

        protected virtual void OnHitEnemy(EnemyBase enemy) { }

        // true → mermi durur/pool'a döner; false → mermiyi durdurmadan geçer (pas geçme)
        protected virtual bool OnHitOther(Collider other) => false;

        protected void ReturnToPool() => _owner?.ReturnProjectile(this);
    }
}
