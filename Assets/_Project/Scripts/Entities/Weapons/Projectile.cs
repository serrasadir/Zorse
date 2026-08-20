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
        protected float NextLifetimeFraction => _lifetime > 0f ? Mathf.Clamp01((_elapsed + Time.deltaTime) / _lifetime) : 0f;

        public void Launch(WeaponBase owner, Vector3 direction, float damage)
        {
            _owner = owner;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Damage = damage;
            _elapsed = 0f;
        }

        // Yatay hareketin dışında bu frame'e özel ek yer değiştirme (ör. CannonProjectile'ın
        // yay yüksekliği) — sadece SphereCast'in bu hareketi de "görmesi" için var, varsayılan sıfır.
        protected virtual Vector3 GetExtraMotion() => Vector3.zero;

        protected virtual void Update()
        {
            Vector3 extraMotion = GetExtraMotion();
            Vector3 castDirection;
            float travelDistance;

            if (extraMotion == Vector3.zero)
            {
                castDirection = Direction;
                travelDistance = _speed * Time.deltaTime;
            }
            else
            {
                Vector3 frameMotion = Direction * (_speed * Time.deltaTime) + extraMotion;
                travelDistance = frameMotion.magnitude;
                castDirection = travelDistance > 0f ? frameMotion / travelDistance : Direction;
            }

            if (travelDistance > 0f && Physics.SphereCast(transform.position, _hitRadius, castDirection, out RaycastHit hit, travelDistance, HitMask))
            {
                transform.position = hit.point;
                bool stopped = HandleHit(hit.collider);
                if (stopped) return;

                float remaining = travelDistance - hit.distance;
                if (remaining > 0f)
                    transform.position += castDirection * remaining;
            }
            else
            {
                transform.position += castDirection * travelDistance;
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
