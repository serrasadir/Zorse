using UnityEngine;
using BlobSurvivor.Entities.Enemies;
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

        // 2026-09-06 (Serra): "mermiler polislerin arkasından geçip gidiyor" — non-homing silahlar
        // (Cannon/Pistol) ateş anındaki pozisyona düz çizgi atıyordu; polis steering'le sürekli yön
        // değiştirdiği için mermi menzile ulaşana kadar hedef kaymış oluyordu. Mermi hâlâ homing DEĞİL
        // (fırlatıldıktan sonra yön değişmez) — sadece nişan noktası, hedefin o anki hızına ve mermi
        // uçuş süresine göre "önden kesiliyor". Hedef ani yön değiştirirse mermi yine ıskalayabilir,
        // bu bilinçli (silah kimliği farkı Metal Bilye'nin gerçek homing'i, bkz. HomingProjectile).
        protected Vector3 GetPredictedAimPoint(Transform target)
        {
            Vector3 targetPos = target.position;
            if (_projectilePrefab == null) return targetPos;

            EnemyBase enemy = target.GetComponent<EnemyBase>();
            if (enemy == null) return targetPos;

            float projectileSpeed = _projectilePrefab.Speed;
            if (projectileSpeed <= 0f) return targetPos;

            float timeToImpact = Vector3.Distance(transform.position, targetPos) / projectileSpeed;
            return targetPos + enemy.CurrentVelocity * timeToImpact;
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
