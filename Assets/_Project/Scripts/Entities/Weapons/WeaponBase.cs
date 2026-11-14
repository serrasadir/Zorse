using UnityEngine;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Weapons
{
    public abstract class WeaponBase : MonoBehaviour
    {
        // 2026-11-03 (Serra: "mermiler hiç iyi hissettirmiyor"): saniyede 1 × 10 hasar yerine
        // saniyede 2.5 × 4 hasar — saniye başına hasar aynı (10), ateş akıcı. Değerler prefab'larda.
        [SerializeField] protected float _fireRate = 2.5f;
        [SerializeField] protected float _damage = 4f;
        [Tooltip("Menzil gövde YÜZEYİNDEN (Vakum gibi): blob büyüdükçe menzil de onunla büyür.")]
        [SerializeField] protected float _range = 3f;
        // Ateş yayı (yüzün baktığı yöne göre, derece). 360 = her yöne; ana silahlar 360, Havai Fişek ön koni.
        [SerializeField, Range(30f, 360f)] protected float _fireArc = 360f;
        [SerializeField] protected Projectile _projectilePrefab;
        // Kartta Sv.8 açıklamasında gösterilir — karaktere özel son form (bkz. UpgradePanel).
        [SerializeField] private string _ultimateDescription;

        // Sopa (deneme): ana silah mermi yerine yüzün baktığı yöne yay şeklinde vurur, yaydaki herkese.
        // Vuruş hasarı = _damage × _fireRate × aralık (tek hedefe saniye başı hasar mermiyle aynı kalır).
        // Silah Gücü: Sv.3 arkaya da vurur · Sv.5 yay 180° · Sv.7 vuruş hızı ×1.3 · Sv.8 tam tur (360°) + hasar ×1.5.
        [Header("Sopa vuruşu")]
        [SerializeField] private bool _swing = true;
        [SerializeField] private float _swingInterval = 1f;
        [SerializeField] private float _swingArc = 120f;
        [SerializeField] private float _swingWideArc = 180f;
        [SerializeField] private float _swingFastFactor = 1.3f;
        [SerializeField] private float _swingKnockback = 0.4f;
        [SerializeField] private float _swingUltimateDamage = 1.5f;

        private const int EnemyLayerMask = 1 << 14;
        // 2026-11-03: arada bina varsa hedef alınmaz (elit polisin RangedAttackState'iyle aynı kural).
        private const int WallMask = 1 << 16; // Environment: bina engel kutuları + harita duvarı
        private const int MaxTargets = 3;
        private const int InitialPoolSize = 40;
        // 2026-11-03 (Serra: "büyüdükten sonra mermi hizam polislerden yukarıda kalıyor"): silah blob'un merkezinde,
        // mermi yatay uçuyor — Giant'ta merkez ~2 birimde, polis kapsülü ~1.65'e kadar, mermiler kafaların üstünden
        // geçiyordu. Mermi blob boyundan bağımsız, polisin göğüs hizasından çıkar.
        private const float MuzzleHeight = 0.7f; // sık ateş + çoklu hedef: oyun içinde Instantiate olmasın
        // 2026-11-04 (final boss helikopter): menzil yatay ölçülür — blob'un üstünde bu yüksekliğe kadar
        // uçan hedef de menzilde sayılır (dikey silindir). Yerdeki düşmanlar için eski küreyle aynı sonuç.
        private const float VerticalReach = 14f;

        private readonly Collider[] _hits = new Collider[32]; // ön 180° süzgeci: arkadakiler tamponu doldurmasın
        private readonly Transform[] _targets = new Transform[MaxTargets];
        private readonly float[] _targetSqr = new float[MaxTargets];
        private ObjectPool<Projectile> _projectilePool;
        private float _cooldown;
        private const float NoTargetRetry = 0.1f; // menzilde hedef yokken her kare Overlap atmasın
        private Transform _blobRoot;
        private BlobSurvivor.Entities.Blob.BlobVisual _blobVisual;
        private const float TinyBlobScale = 0.5f;   // BlobGrowth._baseScale
        private float _bodyLocalRadius = 0.5f;      // blob'un katı SphereCollider'ı (yeme trigger'ı değil)
        private const float MaxBulletScale = 3f;

        // Silah Gücü yolu (WeaponUpgradeEffect ayarlar). Restart'ta silah yeniden yaratıldığı için kendiliğinden sıfırlanır.
        protected int TargetCount { get; private set; } = 1;
        protected int Pierce { get; private set; }
        protected bool UltimateActive { get; private set; }

        // Ana silahın (karakter silahı) son form açıklaması — Silah Gücü kartı okur.
        public static string CurrentUltimateDescription { get; private set; }

        // Karakter silahı = ana silah (Silah Gücü bunu büyütür). İkincil silahlar (Havai Fişek) false döner.
        public virtual bool IsPrimary => true;

        public static WeaponBase FindPrimary(GameObject root)
        {
            foreach (WeaponBase w in root.GetComponentsInChildren<WeaponBase>())
                if (w.IsPrimary) return w;
            return null;
        }

        protected virtual void Awake()
        {
            SetProjectilePrefab(_projectilePrefab);
            if (IsPrimary) CurrentUltimateDescription = _ultimateDescription;
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
            if (UsesSwing) { Swing(); return; }

            int count = FindNearestEnemies(TargetCount);
            if (count == 0) { _cooldown = NoTargetRetry; return; }

            for (int i = 0; i < count; i++)
                Fire(_targets[i]);
            _cooldown = 1f / _fireRate;
            if (_blobVisual != null) _blobVisual.Recoil(_targets[0].position - transform.position, RecoilStrength);
        }

        private bool UsesSwing => _swing && IsPrimary;
        private SwingArcFx _swingFxFront, _swingFxBack;
        private bool _swingFlip;

        private float SwingInterval => _swingInterval / (TargetCount >= 3 ? _swingFastFactor : 1f);

        // Yaydaki (ve Sv.3'ten sonra arka yaydaki) tüm düşmanlara vurur; kimse yoksa sallamaz.
        private void Swing()
        {
            Vector3 origin = transform.position;
            Vector3 forward = _blobVisual != null && _blobVisual.HasFacing ? _blobVisual.Forward : Vector3.forward;
            forward.y = 0f;
            bool full = UltimateActive;
            bool back = TargetCount >= 2 && !full;
            float arc = full ? 360f : Pierce > 0 ? _swingWideArc : _swingArc;
            float cosHalf = Mathf.Cos(arc * 0.5f * Mathf.Deg2Rad);
            float range = EffectiveRange;
            float damage = _damage * _fireRate * SwingInterval * (full ? _swingUltimateDamage : 1f);

            int hitCount = Physics.OverlapCapsuleNonAlloc(origin, origin + Vector3.up * VerticalReach, range, _hits, EnemyLayerMask);
            bool hitFront = false, hitBack = false;
            for (int i = 0; i < hitCount; i++)
            {
                Vector3 flat = _hits[i].transform.position - origin;
                flat.y = 0f;
                float mag = flat.magnitude;
                bool front, rear;
                if (mag * mag <= OverheadSqr) { front = true; rear = false; } // tam tepedeki (helikopter)
                else
                {
                    float dot = Vector3.Dot(forward, flat) / mag;
                    front = full || dot >= cosHalf;
                    rear = back && -dot >= cosHalf;
                }
                if (!front && !rear) continue;
                if (Physics.Linecast(origin, _hits[i].bounds.center, WallMask, QueryTriggerInteraction.Ignore)) continue;

                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>(); // vuruş anında, saniyede bir
                if (enemy == null) continue;
                enemy.TakeDamage(damage);
                enemy.Knockback(flat, _swingKnockback);
                hitFront |= front;
                hitBack |= rear;
            }

            if (!hitFront && !hitBack) { _cooldown = NoTargetRetry; return; }

            _cooldown = SwingInterval;
            _swingFlip = !_swingFlip;
            float fxRadius = range;
            if (hitFront || back) PlaySwingFx(ref _swingFxFront, "SwingArc_Front", forward, fxRadius, arc, _swingFlip);
            if (back) PlaySwingFx(ref _swingFxBack, "SwingArc_Back", -forward, fxRadius, arc, !_swingFlip);
            if (_blobVisual != null) _blobVisual.Recoil(-forward, 0.6f); // vuruş yönüne hafif atılma
        }

        private void PlaySwingFx(ref SwingArcFx fx, string name, Vector3 dir, float radius, float arc, bool flip)
        {
            if (fx == null) fx = SwingArcFx.Create(name); // silah başına tek seferlik
            fx.Play(transform, dir, radius, arc, flip);
        }

        protected virtual void OnDestroy()
        {
            if (_swingFxFront != null) Destroy(_swingFxFront.gameObject);
            if (_swingFxBack != null) Destroy(_swingFxBack.gameObject);
        }

        protected virtual float RecoilStrength => 1f;
        protected BlobSurvivor.Entities.Blob.BlobVisual Visual => _blobVisual;

        public void IncreaseDamage(float amount) => _damage += amount;
        public void MultiplyFireRate(float factor) => _fireRate *= factor;
        public void IncreaseRange(float amount) => _range += amount;
        public void SetTargetCount(int count) => TargetCount = Mathf.Clamp(count, 1, MaxTargets);
        public void SetPierce(int pierce) => Pierce = Mathf.Max(0, pierce);
        public void EnableUltimate() => UltimateActive = true;

        protected abstract void Fire(Transform target);

        // Yay < 360: hedef yalnızca yüzün baktığı koni içinde. Yüz = BlobVisual.Forward (gidiş yönü; dururken son yön).
        // Görünümsüz küre ve tam tepedeki hedef (helikopter) serbest. Kaçarken arkaya ateş edilemesin diye ana silahlar 360.
        private const float OverheadSqr = 0.25f;

        // Hedef sıralaması: küçük = önce. Varsayılan en yakın; Havai Fişek boss/elit önceliği ekler.
        protected virtual float TargetCost(Collider hit, float flatSqr) => flatSqr;

        // En iyi N düşmanı _targets'a doldurur (N ≤ 3, küçük ekleme sıralaması).
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

        // 2026-09-06 (Serra): "mermiler polislerin arkasından geçip gidiyor" — non-homing silahlar
        // (Cannon/Pistol) ateş anındaki pozisyona düz çizgi atıyordu; polis steering'le sürekli yön
        // değiştirdiği için mermi menzile ulaşana kadar hedef kaymış oluyordu. Mermi hâlâ homing DEĞİL
        // (fırlatıldıktan sonra yön değişmez) — sadece nişan noktası, hedefin o anki hızına ve mermi
        // uçuş süresine göre "önden kesiliyor".
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
            projectile.Launch(_projectilePool, direction, _damage * damageScale, Pierce, UltimateActive);
            return projectile;
        }
    }
}
