using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Weapons
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected float _fireRate = 1f;
        [SerializeField] protected float _damage = 10f;
        [SerializeField] protected float _range = 8f;
        [SerializeField] protected Projectile _projectilePrefab;

        private const int EnemyLayerMask = 1 << 14;
        private readonly Collider[] _hits = new Collider[16];
        private ObjectPool<Projectile> _projectilePool;
        private float _cooldown;

        protected virtual void Awake()
        {
            if (_projectilePrefab != null && PoolManager.Instance != null)
                _projectilePool = PoolManager.Instance.CreatePool(_projectilePrefab, 10);
        }

        protected virtual void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            Transform target = FindNearestEnemy();
            if (target == null) return;

            Fire(target);
            _cooldown = 1f / _fireRate;
        }

        public void IncreaseDamage(float amount) => _damage += amount;

        protected abstract void Fire(Transform target);

        protected Transform FindNearestEnemy()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _range, _hits, EnemyLayerMask);
            Transform nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float sqr = (_hits[i].transform.position - transform.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = _hits[i].transform;
                }
            }
            return nearest;
        }

        protected Projectile SpawnProjectile(Vector3 direction)
        {
            if (_projectilePool == null) return null;

            Quaternion rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            Projectile projectile = _projectilePool.Get(transform.position, rotation);
            projectile.Launch(this, direction, _damage);
            return projectile;
        }

        public void ReturnProjectile(Projectile projectile) => _projectilePool?.Return(projectile);
    }
}
