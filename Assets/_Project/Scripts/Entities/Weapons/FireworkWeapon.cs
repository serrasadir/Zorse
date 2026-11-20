using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    // Havai Fişek: ikincil silah (kart). Uzun menzil, yavaş, tek hedefe ağır; her yöne, önce boss sonra elit.
    // Sopa sürüyü savuşturur, bu boss'u indirir. FireworkEffect kurar ve seviyeler.
    // Görünür atar sol omuzda (sol = saldırı, sağ = kalkan) (G_Firework_1..4, zorse-blender/build_firework.py), gövdeye sabit.
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
        private int _misketCount;                // Bayram Gecesi: isabette bölünme
        private float _misketDamageFactor;
        private int _burst = 1;
        private int _burstLeft;
        private int _burstIndex;
        private float _burstTimer;
        private readonly Transform[] _burstTargets = new Transform[6];
        private int _burstTargetCount;
        private WeaponVisual _launcher;

        protected override float RecoilStrength => 2.6f;

        public void Setup(Projectile prefab, float fireRate, float damage, float range)
        {
            Configure(prefab, fireRate, damage, range, 360f);
            _launcher = gameObject.AddComponent<WeaponVisual>(); // run başına tek seferlik; obje silinince gider
            _launcher.Setup(Visual != null ? Visual.ModelRenderer : null, Model, false, "Firework"); // FireworkEffect SetForm(1) ile "pop" eder
        }

        public void SetForm(int form) => _launcher?.SetForm(form);
        public void SetPriorityDamageScale(float scale) => _priorityDamageScale = Mathf.Max(1f, scale);
        public void SetBurst(int count) => _burst = Mathf.Clamp(count, 1, _burstTargets.Length);

        // Son form: her roket ayrı düşmana. Hedef roketten azsa (ör. tek boss) sırayla tekrar eder.
        protected override int TargetsWanted => _burst;
        public void EnableCluster(int count, float damageFactor) { _misketCount = count; _misketDamageFactor = damageFactor; }
        public bool HasCluster => _misketCount > 0;

        protected override float TargetCost(Collider hit, float flatSqr)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>(); // sadece ateş denemesinde (0.1 sn'de bir en çok)
            if (enemy == null) return NormalCost + flatSqr;
            if (enemy.IsBoss) return BossCost + flatSqr;
            return (enemy.Data != null && enemy.Data.IsElite ? EliteCost : NormalCost) + flatSqr;
        }

        protected override void Update()
        {
            base.Update();
            if (_burstLeft <= 0) return;
            _burstTimer -= Time.deltaTime;
            if (_burstTimer > 0f) return;
            Transform target = NextBurstTarget();
            if (target == null) { _burstLeft = 0; return; }
            Shoot(target);
            _burstLeft--;
            _burstTimer = BurstGap;
        }

        protected override void Fire(Transform target) => Shoot(target);

        protected override void FireVolley(Transform[] targets, int count)
        {
            _burstTargetCount = Mathf.Min(count, _burstTargets.Length);
            for (int i = 0; i < _burstTargetCount; i++) _burstTargets[i] = targets[i];
            _burstIndex = 0;
            Shoot(targets[0]);
            _burstLeft = _burst - 1;
            _burstTimer = BurstGap;
        }

        // Sıradaki hedef; o ölmüşse listedeki sonraki canlıya geçer.
        private Transform NextBurstTarget()
        {
            for (int tries = 0; tries < _burstTargetCount; tries++)
            {
                _burstIndex = (_burstIndex + 1) % _burstTargetCount;
                Transform t = _burstTargets[_burstIndex];
                if (t != null && t.gameObject.activeInHierarchy) return t;
            }
            return null;
        }

        private void Shoot(Transform target)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            bool priority = enemy != null && (enemy.IsBoss || (enemy.Data != null && enemy.Data.IsElite));
            Projectile rocket = SpawnProjectile(GetAimDirection(target, lead: true), priority ? _priorityDamageScale : 1f);
            if (rocket is FireworkProjectile firework)
                firework.SetCluster(_misketCount, _damage * _misketDamageFactor * BlobSkills.WeaponDamageScale);

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
