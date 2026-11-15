using UnityEngine;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Weapons
{
    // Mermi atan otomatik silahların tabanı (şu an sadece Havai Fişek). Ana silah sopa: StickWeapon.
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected float _fireRate = 2.5f;
        [SerializeField] protected float _damage = 4f;
        [Tooltip("Menzil gövde YÜZEYİNDEN (Vakum gibi): blob büyüdükçe menzil de onunla büyür.")]
        [SerializeField] protected float _range = 3f;
        // Ateş yayı (yüzün baktığı yöne göre, derece). 360 = her yöne.
        [SerializeField, Range(30f, 360f)] protected float _fireArc = 360f;
        [SerializeField] protected Projectile _projectilePrefab;
        private const int EnemyLayerMask = 1 << 14;
        // 2026-11-03: arada bina varsa hedef alınmaz (elit polisin RangedAttackState'iyle aynı kural).
        private const int WallMask = 1 << 16; // Environment: bina engel kutuları + harita duvarı
        private const int MaxTargets = 1;
        private const int InitialPoolSize = 40;
        // Mermi blob boyundan bağımsız, polisin göğüs hizasından çıkar (büyük blob'da kafaların üstünden geçmesin).
        private const float MuzzleHeight = 0.7f;
        // 2026-11-04 (final boss helikopter): menzil yatay ölçülür — blob'un üstünde bu yüksekliğe kadar
        // uçan hedef de menzilde sayılır (dikey silindir). Yerdeki düşmanlar için eski küreyle aynı sonuç.
        private const float VerticalReach = 14f;

        private readonly Collider[] _hits = new Collider[32];
        private readonly Transform[] _targets = new Transform[MaxTargets];
        private readonly float[] _targetSqr = new float[MaxTargets];
        private ObjectPool<Projectile> _projectilePool;
        private float _cooldown;
        private int _shotCount; // Tetikçi Sv.3 "her N. atış tekrar"
        private const float NoTargetRetry = 0.1f; // menzilde hedef yokken her kare Overlap atmasın
        private Transform _blobRoot;
        private BlobSurvivor.Entities.Blob.BlobVisual _blobVisual;
        private const float TinyBlobScale = 0.5f;   // BlobGrowth._baseScale
        private float _bodyLocalRadius = 0.5f;      // blob'un katı SphereCollider'ı (yeme trigger'ı değil)
        private const float MaxBulletScale = 3f;

        protected virtual void Awake()
        {
            SetProjectilePrefab(_projectilePrefab);
            _blobVisual = GetComponentInParent<BlobSurvivor.Entities.Blob.BlobVisual>();
            _blobRoot = transform.parent;
            if (_blobRoot != null)
                foreach (SphereCollider c in _blobRoot.GetComponents<SphereCollider>())
                    if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
        }

        // Merkezden ölçülen gerçek menzil = gövde yarıçapı + yüzeyden menzil.
        protected float EffectiveRange =>
            (_blobRoot != null ? _blobRoot.localScale.x * _bodyLocalRadius : 0f) + _range;

        // Runtime'da kurulan silah (skill efekti) için: AddComponent'ten sonra çağrılır.
        protected void Configure(Projectile prefab, float fireRate, float damage, float range, float arc)
        {
            _fireRate = fireRate;
            _damage = damage;
            _range = range;
            _fireArc = arc;
            SetProjectilePrefab(prefab);
        }

        private void SetProjectilePrefab(Projectile prefab)
        {
            _projectilePrefab = prefab;
            _projectilePool = prefab != null && PoolManager.Instance != null
                ? PoolManager.Instance.CreatePool(prefab, InitialPoolSize) : null;
        }

        // Kamera blob'la birlikte uzaklaşıyor: mermi de büyümezse Giant'ta görünmez. Tiny 1 → Dev ~3.
        private float BulletScale()
        {
            float blob = _blobRoot != null ? _blobRoot.localScale.x : TinyBlobScale;
            return Mathf.Clamp(Mathf.Sqrt(blob / TinyBlobScale), 1f, MaxBulletScale);
        }

        protected virtual void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            int count = FindNearestEnemies(MaxTargets);
            if (count == 0) { _cooldown = NoTargetRetry; return; }

            for (int i = 0; i < count; i++)
                Fire(_targets[i]);
            _cooldown = BlobSkills.NextCooldown(ref _shotCount, 1f / _fireRate);
            if (_blobVisual != null) _blobVisual.Recoil(_targets[0].position - transform.position, RecoilStrength);
        }

        // Tetikçi Sv.5: her öldürmede bekleme kısalır.
        protected virtual void OnEnable() => BlobSurvivor.Core.GameEvents.OnEnemyKilled += HandleEnemyKilled;
        protected virtual void OnDisable() => BlobSurvivor.Core.GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        private void HandleEnemyKilled(BlobSurvivor.Data.EnemyData _)
        {
            if (BlobSkills.KillCooldownRefund > 0f) _cooldown -= BlobSkills.KillCooldownRefund;
        }

        protected virtual float RecoilStrength => 1f;
        protected BlobSurvivor.Entities.Blob.BlobVisual Visual => _blobVisual;

        public void IncreaseDamage(float amount) => _damage += amount;
        public void MultiplyFireRate(float factor) => _fireRate *= factor;
        public void IncreaseRange(float amount) => _range += amount;

        protected abstract void Fire(Transform target);

        // Yay < 360: hedef yalnızca yüzün baktığı koni içinde. Yüz = BlobVisual.Forward (gidiş yönü; dururken son yön).
        // Görünümsüz küre ve tam tepedeki hedef (helikopter) serbest.
        private const float OverheadSqr = 0.25f;

        // Hedef sıralaması: küçük = önce. Varsayılan en yakın; Havai Fişek boss/elit önceliği ekler.
        protected virtual float TargetCost(Collider hit, float flatSqr) => flatSqr;

        // En iyi N düşmanı _targets'a doldurur (küçük ekleme sıralaması).
        private int FindNearestEnemies(int wanted)
        {
            Vector3 origin = transform.position;
            bool frontOnly = _fireArc < 359.9f && _blobVisual != null && _blobVisual.HasFacing;
            Vector3 forward = frontOnly ? _blobVisual.Forward : Vector3.forward;
            float cosHalf = Mathf.Cos(_fireArc * 0.5f * Mathf.Deg2Rad);
            int hitCount = Physics.OverlapCapsuleNonAlloc(origin, origin + Vector3.up * VerticalReach, EffectiveRange, _hits, EnemyLayerMask);
            int found = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Transform t = _hits[i].transform;
                Vector3 flat = t.position - origin;
                flat.y = 0f;
                float flatSqr = flat.sqrMagnitude;
                if (frontOnly && flatSqr > OverheadSqr && Vector3.Dot(forward, flat) < cosHalf * Mathf.Sqrt(flatSqr)) continue; // yay dışında
                float sqr = TargetCost(_hits[i], flatSqr);

                int slot = found;
                while (slot > 0 && _targetSqr[slot - 1] > sqr) slot--;
                if (slot >= wanted) continue;
                // Görüş hattı sadece listeye girecek adaylar için sorgulanır (en fazla birkaç Linecast).
                // Collider merkezi: havadaki gövdeye (helikopter) çizgi çekilir, kökü yerde kalıyor.
                if (Physics.Linecast(origin, _hits[i].bounds.center, WallMask, QueryTriggerInteraction.Ignore)) continue;

                int last = Mathf.Min(found, wanted - 1);
                for (int j = last; j > slot; j--)
                {
                    _targets[j] = _targets[j - 1];
                    _targetSqr[j] = _targetSqr[j - 1];
                }
                _targets[slot] = t;
                _targetSqr[slot] = sqr;
                if (found < wanted) found++;
            }
            return found;
        }

        // Mermi güdümlü değil: nişan noktası hedefin o anki hızına ve uçuş süresine göre önden kesilir.
        protected Vector3 GetPredictedAimPoint(Transform target)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            Vector3 targetPos = AimPointOf(target, enemy);
            if (_projectilePrefab == null || enemy == null) return targetPos;

            float projectileSpeed = _projectilePrefab.Speed;
            if (projectileSpeed <= 0f) return targetPos;

            float timeToImpact = Vector3.Distance(transform.position, targetPos) / projectileSpeed;
            return targetPos + enemy.CurrentVelocity * timeToImpact;
        }

        // Hedefin nişan noktası: havadaki düşmanda gövde merkezi, yerdekilerde kök (eski davranış).
        public static Vector3 AimPointOf(Transform target, EnemyBase enemy) =>
            enemy != null ? enemy.AimPoint : target.position;

        public static bool IsAirborne(EnemyBase enemy) => enemy != null && enemy.AirborneHeight > 0f;

        // Ateş yönü: yerdeki hedefe yatay (eski davranış), havadaki hedefe namludan 3D — oyuncu yukarı ateş eder.
        protected Vector3 GetAimDirection(Transform target, bool lead)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            Vector3 aimPoint = lead ? GetPredictedAimPoint(target) : AimPointOf(target, enemy);
            if (IsAirborne(enemy))
                return aimPoint - new Vector3(transform.position.x, MuzzleHeight, transform.position.z);

            Vector3 direction = aimPoint - transform.position;
            direction.y = 0f;
            return direction;
        }

        protected Projectile SpawnProjectile(Vector3 direction, float damageScale = 1f)
        {
            if (_projectilePool == null) return null;

            Quaternion rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            Vector3 muzzle = new Vector3(transform.position.x, MuzzleHeight, transform.position.z);
            Projectile projectile = _projectilePool.Get(muzzle, rotation);
            projectile.SetVisualScale(BulletScale());
            projectile.Launch(_projectilePool, direction, _damage * damageScale * BlobSkills.WeaponDamageScale);
            return projectile;
        }
    }
}
