using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Weapons
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private float _hitRadius = 0.3f;

        // WeaponBase'in "önden kesme" (lead) hesabı için — bkz. WeaponBase.GetPredictedAimPoint.
        public float Speed => _speed;

        private const int EnemyLayer = 14;
        private const int EnvironmentLayer = 16;
        // Enemy(14) + ConsumableTier1-5(9-13) + Environment(16, bina engelleri ve harita duvarı) —
        // Rigidbody/Collider gerektirmez, doğrudan spatial sorgu.
        private const int HitMask = (1 << 14) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13) | (1 << EnvironmentLayer);
        // Pas geçilen (ağaç, yenebilir obje) hitlerden sonra aynı karede arkadakini de görebilsin.
        private const int MaxHitsPerFrame = 3;

        // Silahı değil havuzu tutar: restart'ta silah Destroy edilir, havuz (PoolManager) yaşamaya devam eder.
        private ObjectPool<Projectile> _pool;
        private float _elapsed;

        // 2026-11-03: Silah Gücü yolu — Sv.5 delip geçme, Sv.8 karaktere özel son form (alt sınıflar okur).
        private int _pierceRemaining;
        private EnemyBase _lastPierced;

        protected float Damage { get; private set; }
        protected bool IsUltimate { get; private set; }
        protected Vector3 Direction { get; set; }
        protected float LifetimeFraction => _lifetime > 0f ? Mathf.Clamp01(_elapsed / _lifetime) : 0f;
        protected float NextLifetimeFraction => _lifetime > 0f ? Mathf.Clamp01((_elapsed + Time.deltaTime) / _lifetime) : 0f;

        // Restart sahneyi yeniden yüklemiyor: havadaki mermiler yeni run'a taşınmasın (EnemyProjectile ile aynı).
        protected virtual void OnEnable() => GameEvents.OnCharacterSelected += HandleRestart;

        protected virtual void OnDisable() => GameEvents.OnCharacterSelected -= HandleRestart;

        private void HandleRestart(CharacterData _) => ReturnToPool();

        public void Launch(ObjectPool<Projectile> pool, Vector3 direction, float damage, int pierce = 0, bool ultimate = false)
        {
            _pool = pool;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Damage = damage;
            _pierceRemaining = pierce;
            _lastPierced = null;
            IsUltimate = ultimate;
            _elapsed = 0f;
            OnLaunched();
        }

        protected virtual void OnLaunched() { }

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

            // 2026-11-03: eskiden tek SphereCast vardı ve pas geçilen hitten sonra kalan yol sorgusuz gidiliyordu
            // (arkadaki duvar/düşman o karede görülmüyordu); ayrıca mermi merkezi temas noktasına ışınlanıyordu.
            Vector3 position = transform.position;
            float remaining = travelDistance;
            for (int i = 0; i < MaxHitsPerFrame && remaining > 0f; i++)
            {
                if (!Physics.SphereCast(position, _hitRadius, castDirection, out RaycastHit hit, remaining, HitMask)) break;

                position += castDirection * hit.distance;
                remaining -= hit.distance;
                transform.position = position;
                if (HandleHit(hit.collider)) return;
            }
            transform.position = position + castDirection * Mathf.Max(0f, remaining);

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
                    if (enemy == _lastPierced) return false; // az önce deldiği düşmanı tekrar vurmasın
                    enemy.TakeDamage(Damage);
                    OnHitEnemy(enemy);
                    if (TryPierce(enemy)) return false;
                    ReturnToPool();
                    return true;
                }
            }

            if (OnHitOther(other)) return true;

            // 2026-11-03 (Serra: "binanın arkasındaki polise nişan alamamalıyım"): mermi binalardan geçmez.
            if (IsWall(other))
            {
                ReturnToPool();
                return true;
            }
            return false;
        }

        // Bina (Environment engel kutusu ya da binanın kendi collider'ı) ve harita duvarı. Ağaç, lamba gibi
        // sokak eşyaları ve yenebilir objeler duvar değildir, mermi içlerinden geçer.
        private static bool IsWall(Collider other) =>
            other.gameObject.layer == EnvironmentLayer
            || (other.TryGetComponent(out CityConsumable city) && city.BlocksProjectiles);

        // Delme hakkı varsa harcar ve true döner — mermi yoluna devam eder.
        protected bool TryPierce(EnemyBase enemy)
        {
            if (_pierceRemaining <= 0) return false;
            _pierceRemaining--;
            _lastPierced = enemy;
            return true;
        }

        protected virtual void OnHitEnemy(EnemyBase enemy) { }

        // true → mermi durur/pool'a döner; false → mermiyi durdurmadan geçer (pas geçme)
        protected virtual bool OnHitOther(Collider other) => false;

        protected void ReturnToPool()
        {
            if (_pool != null) _pool.Return(this);
            else gameObject.SetActive(false);
        }
    }
}
