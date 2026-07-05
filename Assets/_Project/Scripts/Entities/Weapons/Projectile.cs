using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifetime = 3f;

        private const int EnemyLayer = 14;

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
            transform.position += Direction * _speed * Time.deltaTime;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
                ReturnToPool();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == EnemyLayer)
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(Damage);
                    OnHitEnemy(enemy);
                    ReturnToPool();
                    return;
                }
            }

            OnHitOther(other);
        }

        protected virtual void OnHitEnemy(EnemyBase enemy) { }

        protected virtual void OnHitOther(Collider other) { }

        protected void ReturnToPool() => _owner?.ReturnProjectile(this);
    }
}
