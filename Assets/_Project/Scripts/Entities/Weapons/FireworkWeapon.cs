using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    // Havai Fişek: ikincil silah (kart). Uzun menzil, yavaş, tek hedefe ağır; sadece ön koniye, önce boss sonra elit.
    // Ana silah sürüyü savuşturur, bu boss'u indirir. FireworkEffect kurar ve seviyeler.
    // Görünür atar sol omuzda (sol = saldırı, sağ = kalkan) (G_Firework_1..4, zorse-blender/build_firework.py), gövdeye sabit = ateş konisi.
    public class FireworkWeapon : WeaponBase
    {
        private const float BossCost = 0f;
        private const float EliteCost = 1e4f;
        private const float NormalCost = 1e6f; // menzil² ≪ bu: sınıf önceliği mesafeden önce gelir
        private const float BurstGap = 0.15f;
        private const string Model = "Firework";
        // Atarın arka ucu, görünüm uzayında (build_firework.py "FIREWORK rear" çıktısı): duman buradan püskürür.
        private static readonly Vector3 RearLocal = new Vector3(-0.400f, 0.232f, -0.338f);

        private float _priorityDamageScale = 1f; // boss + elit
        private int _burst = 1;
        private int _burstLeft;
        private float _burstTimer;
        private Transform _burstTarget;
        private WeaponVisual _launcher;

        // Kurma: yüz (= atar) _armAngle'dan fazla dönerse _armTime boyunca ateş edemez. Dön-dur-kur-ateş ritmi.
        private float _armTime = 1f;
        private float _armCos = Mathf.Cos(30f * Mathf.Deg2Rad);
        private float _armTimer;
        private Vector3 _armDir;

        public override bool IsPrimary => false;
        protected override float RecoilStrength => 2.6f;

        public void Setup(Projectile prefab, float fireRate, float damage, float range, float arc)
        {
            Configure(prefab, fireRate, damage, range, arc);
            _launcher = gameObject.AddComponent<WeaponVisual>(); // run başına tek seferlik; obje silinince gider
            _launcher.Setup(Visual != null ? Visual.ModelRenderer : null, Model, false, "Firework"); // FireworkEffect SetForm(1) ile "pop" eder
        }

        public void SetArming(float seconds, float angle)
        {
            _armTime = Mathf.Max(0f, seconds);
            _armCos = Mathf.Cos(angle * Mathf.Deg2Rad);
            _armTimer = _armTime; // yeni takılan atar da önce kurulur
            _armDir = Visual != null ? Visual.Forward : Vector3.forward;
        }

        protected override bool CanFire => _armTimer <= 0f;

        public void SetForm(int form) => _launcher?.SetForm(form);
        public void SetPriorityDamageScale(float scale) => _priorityDamageScale = Mathf.Max(1f, scale);
        public void SetBurst(int count) => _burst = Mathf.Max(1, count);

        protected override float TargetCost(Collider hit, float flatSqr)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>(); // sadece ateş denemesinde (0.1 sn'de bir en çok)
            if (enemy == null) return NormalCost + flatSqr;
            if (enemy.IsBoss) return BossCost + flatSqr;
            return (enemy.Data != null && enemy.Data.IsElite ? EliteCost : NormalCost) + flatSqr;
        }

        protected override void Update()
        {
            if (Visual != null)
            {
                Vector3 facing = Visual.Forward;
                if (Vector3.Dot(facing, _armDir) < _armCos)
                {
                    _armDir = facing;
                    _armTimer = _armTime;
                }
            }
            bool wasArming = _armTimer > 0f;
            _armTimer -= Time.deltaTime;
            if (wasArming && _armTimer <= 0f) PlayAtRear(FireworkBurstFx.Spark); // kuruldu: fitil tutuşur
            base.Update();
            if (_burstLeft <= 0) return;
            _burstTimer -= Time.deltaTime;
            if (_burstTimer > 0f) return;
            if (_burstTarget == null || !_burstTarget.gameObject.activeInHierarchy) { _burstLeft = 0; return; }
            Shoot(_burstTarget);
            _burstLeft--;
            _burstTimer = BurstGap;
        }

        protected override void Fire(Transform target)
        {
            Shoot(target);
            _burstLeft = _burst - 1;
            _burstTimer = BurstGap;
            _burstTarget = target;
        }

        private void Shoot(Transform target)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            bool priority = enemy != null && (enemy.IsBoss || (enemy.Data != null && enemy.Data.IsElite));
            SpawnProjectile(GetAimDirection(target, lead: true), priority ? _priorityDamageScale : 1f);

            PlayAtRear(FireworkBurstFx.Smoke);
            Haptics.LightTap();
        }

        private void PlayAtRear(System.Action<Vector3, float> fx)
        {
            Renderer host = Visual != null ? Visual.ModelRenderer : null;
            if (host != null) fx(host.transform.TransformPoint(RearLocal), host.transform.lossyScale.x);
        }
    }
}
