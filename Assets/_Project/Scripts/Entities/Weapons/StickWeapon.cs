using UnityEngine;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    // Ana silah (bütün karakterlerde): ağaç dalı sopa. Yüzün baktığı yöne yay şeklinde vurur, yaydaki herkese;
    // kimse yoksa sallamaz, bina arkasındakine vurmaz. Sopa skill'i (WeaponUpgradeEffect) güçlendirir:
    // Sv.1 +hasar/+menzil · Sv.2 yay 180° · Sv.3 arkaya da + hasar · Sv.4 vuruş hızı ×1.3 · Sv.5 tam tur + hasar ×1.5.
    // GameManager her run başında blob'un child'ı olarak kurar (prefab: Prefabs/Weapons/StickWeapon).
    public class StickWeapon : MonoBehaviour
    {
        [SerializeField] private float _hitDamage = 10f;
        [Tooltip("Menzil gövde YÜZEYİNDEN (Vakum gibi): blob büyüdükçe menzil de onunla büyür.")]
        [SerializeField] private float _range = 2f;
        [SerializeField] private float _interval = 1f;
        [SerializeField] private float _arc = 120f;
        [SerializeField] private float _wideArc = 180f;
        [SerializeField] private float _fastFactor = 1.3f;
        [SerializeField] private float _knockback = 0.4f;
        [SerializeField] private float _fullCircleDamage = 1.5f;

        [Header("Lamba Direği (evrim)")]
        [SerializeField] private float _lampRangeFactor = 1.5f;
        [SerializeField] private float _lampKnockbackFactor = 2f;
        [Tooltip("Savuruş başına ağza uçan en fazla düşman (en ağırlar); kalanlar yerinde kırıntıya dağılır.")]
        [SerializeField] private int _maxFlights = 3;

        private const int EnemyLayerMask = 1 << 14;
        private const int WallMask = 1 << 16; // Environment: bina engel kutuları + harita duvarı
        // Menzil yatay ölçülür: blob'un üstünde bu yüksekliğe kadar uçan hedef (helikopter) de sayılır.
        private const float VerticalReach = 14f;
        private const float OverheadSqr = 0.25f; // tam tepedeki hedef her yayda sayılır
        private const float NoTargetRetry = 0.1f; // menzilde kimse yokken her kare Overlap atmasın

        private readonly Collider[] _hits = new Collider[32];
        private float _cooldown;
        private int _shotCount; // Tetikçi Sv.3 "her N. vuruş tekrar"
        private Transform _blobRoot;
        private BlobVisual _blobVisual;
        private WeaponVisual _stickVisual;
        private float _bodyLocalRadius = 0.5f; // blob'un katı SphereCollider'ı (yeme trigger'ı değil)
        private SwingArcFx _fxFront, _fxBack;

        // Sopa skill'i açar; restart'ta silah yeniden yaratıldığı için kendiliğinden sıfırlanır.
        private bool _backSwing, _wide, _fast, _fullCircle;
        private bool _lamp; // Lamba Direği: öldürdüğünü yutar

        // Lamba Direği savuruşunda öldürülecekler (vuruştan sonra, en ağır önce işlenir).
        private readonly EnemyBase[] _devoured = new EnemyBase[32];
        private int _devouredCount;
        private BlobConsumption _consumption;

        public void AddDamage(float damage, float range)
        {
            _hitDamage += damage;
            _range += range;
        }

        public void EnableBackSwing() => _backSwing = true;
        public void EnableWideArc() => _wide = true;
        public void EnableFastSwing() => _fast = true;
        public void EnableFullCircle() => _fullCircle = true;
        public void EnableLampPost() { _lamp = true; _fullCircle = true; }
        public bool IsLampPost => _lamp;

        private float Interval => _interval / (_fast ? _fastFactor : 1f);
        private float EffectiveRange => (_blobRoot != null ? _blobRoot.localScale.x * _bodyLocalRadius : 0f) + _range * (_lamp ? _lampRangeFactor : 1f);

        private void Awake()
        {
            _blobVisual = GetComponentInParent<BlobVisual>();
            _blobRoot = transform.parent;
            if (_blobRoot == null) return;
            _stickVisual = _blobRoot.GetComponent<WeaponVisual>();
            _consumption = _blobRoot.GetComponent<BlobConsumption>();
            foreach (SphereCollider c in _blobRoot.GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
        }

        private void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;
            _cooldown = Swing() ? BlobSkills.NextCooldown(ref _shotCount, Interval) : NoTargetRetry;
        }

        // Tetikçi Sv.5: her öldürmede bekleme kısalır.
        private void OnEnable() => BlobSurvivor.Core.GameEvents.OnEnemyKilled += HandleEnemyKilled;
        private void OnDisable() => BlobSurvivor.Core.GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        private void HandleEnemyKilled(BlobSurvivor.Data.EnemyData _)
        {
            if (BlobSkills.KillCooldownRefund > 0f) _cooldown -= BlobSkills.KillCooldownRefund;
        }

        // Yaydaki (Sv.3'ten sonra arka yaydaki de) tüm düşmanlara vurur. Kimseye değmezse false.
        private bool Swing()
        {
            Vector3 origin = transform.position;
            Vector3 forward = _blobVisual != null && _blobVisual.HasFacing ? _blobVisual.Forward : Vector3.forward;
            forward.y = 0f;
            bool full = _fullCircle;
            bool back = _backSwing && !full;
            float arc = full ? 360f : _wide ? _wideArc : _arc;
            float cosHalf = Mathf.Cos(arc * 0.5f * Mathf.Deg2Rad);
            float range = EffectiveRange;
            float damage = _hitDamage * (full ? _fullCircleDamage : 1f) * BlobSkills.WeaponDamageScale; // Yara Öfkesi

            float knockback = _knockback * (_lamp ? _lampKnockbackFactor : 1f);
            _devouredCount = 0;
            int hitCount = Physics.OverlapCapsuleNonAlloc(origin, origin + Vector3.up * VerticalReach, range, _hits, EnemyLayerMask);
            bool hitFront = false, hitBack = false;
            for (int i = 0; i < hitCount; i++)
            {
                Vector3 flat = _hits[i].transform.position - origin;
                flat.y = 0f;
                float mag = flat.magnitude;
                bool front, rear;
                if (mag * mag <= OverheadSqr) { front = true; rear = false; }
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
                hitFront |= front;
                hitBack |= rear;
                if (_lamp && _consumption != null && enemy.WouldDieFrom(damage) && _devouredCount < _devoured.Length)
                {
                    _devoured[_devouredCount++] = enemy;
                    continue;
                }
                enemy.TakeDamage(damage);
                enemy.Knockback(flat, knockback);
            }
            if (!hitFront && !hitBack) return false;
            if (_devouredCount > 0) DevourKilled();

            PlayFx(ref _fxFront, "SwingArc_Front", forward, range, arc);
            if (back) PlayFx(ref _fxBack, "SwingArc_Back", -forward, range, arc);
            if (_blobVisual != null) _blobVisual.Recoil(-forward, 0.6f); // vuruş yönüne hafif atılma
            if (_stickVisual != null) _stickVisual.PlaySwing(arc, full);
            return true;
        }

        // Lamba Direği: en ağır _maxFlights düşman havaya fırlayıp ağza uçar, kalanlar yerinde kırıntıya dağılır.
        // Hepsinin kütlesi gelir; şişme ve titreşim savuruş başına bir kez.
        private void DevourKilled()
        {
            int flights = Mathf.Min(_maxFlights, _devouredCount);
            for (int f = 0; f < flights; f++) // kısmi seçim sıralaması: en ağır başa
            {
                int best = f;
                for (int j = f + 1; j < _devouredCount; j++)
                    if (MassOf(_devoured[j]) > MassOf(_devoured[best])) best = j;
                (_devoured[f], _devoured[best]) = (_devoured[best], _devoured[f]);
            }
            for (int i = 0; i < _devouredCount; i++)
            {
                EnemyBase enemy = _devoured[i];
                _devoured[i] = null;
                bool fly = i < flights;
                if (fly) SlurpFx.PlayFlight(enemy.gameObject, _blobRoot); // havuza dönmeden önce kopyası alınır
                if (!enemy.Devour(!fly, out float mass)) continue;
                _consumption.AddEnemyMass(mass, enemy.Data != null ? enemy.Data.RequiredConsumeTier : BlobSurvivor.Core.BlobTier.Tiny);
            }
            _devouredCount = 0;
            _consumption.Punch();
            BlobSurvivor.Core.Haptics.Impact(1);
        }

        private static float MassOf(EnemyBase e) => e.Data != null ? e.Data.MassReward : 0f;

        private void PlayFx(ref SwingArcFx fx, string name, Vector3 dir, float radius, float arc)
        {
            if (fx == null) fx = SwingArcFx.Create(name); // silah başına tek seferlik
            fx.Play(transform, dir, radius, arc, false);
        }

        private void OnDestroy()
        {
            if (_fxFront != null) Destroy(_fxFront.gameObject);
            if (_fxBack != null) Destroy(_fxBack.gameObject);
        }
    }
}
