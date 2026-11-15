using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Entities.Blob
{
    // Yer Sarsıntısı (aktif): darbe aldıkça dolar (Bahar, 2026-11-15: kütleyle dolunca yanında düşman olmayabiliyordu;
    // hasar alıyorsan düşman yakındadır). Her DamagePerShake hasarda (kalkanın yuttuğu dahil) yere vurur: çevreye
    // alan hasarı + geri itme (Sv.3+ sersemletme). Alan blob boyuyla büyür. Sv.5 Deprem: her 3. sarsıntı büyük.
    // Tetikçi bekleme çarpanı gereken hasarı da azaltır. Değerleri GroundQuakeEffect yazar; restart'ta GameManager siler.
    public class GroundQuake : MonoBehaviour
    {
        public float DamagePerShake = 15f;   // alınan hasar
        public float Damage = 15f;           // verilen hasar
        public float StunTime;
        public float Radius = 2.5f;          // gövde yüzeyinden
        public int BigEvery;                 // Sv.5 Deprem: her N. sarsıntı büyük (0 = yok)
        public float BigDamageScale = 3f, BigRadiusScale = 2f;

        private const float MinInterval = 1.5f;   // karşılık verme hissi; sersemletmeyle sürekli kilitlemesin
        private const float Knockback = 0.6f;
        private const float VerticalReach = 3f;
        private const int EnemyMask = 1 << 14;
        private const int WallMask = 1 << 16;

        private readonly Collider[] _hits = new Collider[48];
        private BlobHealth _health;
        private float _bodyLocalRadius = 0.5f;
        private float _accumulated;
        private float _nextShakeAt;
        private int _shakeCount;
        private SwingArcFx _fx;
        private QuakeRingFx _ring;

        private void Awake()
        {
            _health = GetComponent<BlobHealth>();
            foreach (SphereCollider c in GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
        }

        private void OnEnable()
        {
            if (_health == null) return;
            _health.OnDamaged += HandleHit;
            _health.OnShieldHit += HandleHit;
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.OnDamaged -= HandleHit;
            _health.OnShieldHit -= HandleHit;
        }

        private void OnDestroy()
        {
            if (_fx != null) Destroy(_fx.gameObject);
            if (_ring != null) Destroy(_ring.gameObject);
        }

        private void HandleHit(float damage)
        {
            _accumulated += damage;
            float need = Mathf.Max(1f, DamagePerShake * BlobSkills.CooldownScale);
            if (_accumulated < need || Time.time < _nextShakeAt || _health == null || !_health.IsAlive) return;
            _accumulated = Mathf.Min(_accumulated - need, need); // birikme en fazla bir sarsıntı kadar
            _shakeCount++;
            bool big = BigEvery > 0 && _shakeCount % BigEvery == 0;
            Shake(big ? BigDamageScale : 1f, big ? BigRadiusScale : 1f);
        }

        private void Shake(float damageScale, float radiusScale)
        {
            _nextShakeAt = Time.time + MinInterval;
            Vector3 origin = transform.position;
            float radius = transform.localScale.x * _bodyLocalRadius + Radius * radiusScale;
            float damage = Damage * damageScale * BlobSkills.WeaponDamageScale;

            int count = Physics.OverlapCapsuleNonAlloc(origin, origin + Vector3.up * VerticalReach, radius, _hits, EnemyMask);
            for (int i = 0; i < count; i++)
            {
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>(); // sarsıntı anında, nadiren
                if (enemy == null || enemy.IsDead) continue;
                if (Physics.Linecast(origin, _hits[i].bounds.center, WallMask, QueryTriggerInteraction.Ignore)) continue;
                Vector3 away = enemy.transform.position - origin;
                enemy.TakeDamage(damage);
                enemy.Knockback(away, Knockback * radiusScale);
                if (StunTime > 0f) enemy.Stun(StunTime);
            }

            if (_fx == null) _fx = SwingArcFx.Create("QuakeShock");
            _fx.Play(transform, Vector3.forward, radius, 360f, false); // beyaz şok dalgası
            if (_ring == null) _ring = QuakeRingFx.Create();
            if (_ring != null) _ring.Play(origin, radius, damageScale > 1f ? 1.6f : 1f); // kırık asfalt halkası
            Haptics.Impact(damageScale > 1f ? 2 : 1);
        }
    }
}
