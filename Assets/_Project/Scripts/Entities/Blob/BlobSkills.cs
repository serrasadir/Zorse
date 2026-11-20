using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    public enum EatKind { Object, Pedestrian, Vehicle, Enemy }

    // Pasif skill'lerin çalışma zamanı durumu (design/gdd/skill-sistemi.md). Efektler (Systems/Upgrade/Effects)
    // alanları seviyeye göre yazar; bu bileşen yeme/tier/can olaylarında uygular. Silahların okuduğu değerler
    // statik (silah bu bileşenden önce kurulur). Run başında GameManager.ClearPreviousRunState hemen siler
    // (DestroyImmediate: başlangıç skill'i aynı karede yenisini kurar) ve statikleri sıfırlar.
    [DisallowMultipleComponent]
    public class BlobSkills : MonoBehaviour
    {
        // --- Silah/pickup'ların okuduğu statikler ---
        public static float CooldownScale = 1f;      // Tetikçi: bekleme süresi çarpanı
        public static int RepeatEvery;               // Tetikçi Sv.3: her N. atış hemen tekrarlanır (0 = yok)
        public static float KillCooldownRefund;      // Tetikçi Sv.5: öldürmede bekleme −sn
        public static float HeartMultiplier = 1f;    // Can Artışı: kalplerin iyileştirmesi
        public static float WeaponDamageScale = 1f;  // Yara Öfkesi: öfkeliyken silah hasarı

        // Tetikçi'yi silahlar (sopa, Havai Fişek) bununla uygular: atış sayacı silahın kendisinde.
        public const float RepeatDelay = 0.12f;
        public static float NextCooldown(ref int shotCount, float interval)
        {
            shotCount++;
            if (RepeatEvery > 0 && shotCount % RepeatEvery == 0) return RepeatDelay;
            return interval * CooldownScale;
        }

        public static void ResetStatics()
        {
            CooldownScale = 1f;
            RepeatEvery = 0;
            KillCooldownRefund = 0f;
            HeartMultiplier = 1f;
            WeaponDamageScale = 1f;
        }

        public static BlobSkills Of(GameObject blobRoot)
        {
            BlobSkills s = blobRoot.GetComponent<BlobSkills>();
            return s != null ? s : blobRoot.AddComponent<BlobSkills>();
        }

        // --- Sindirim ---
        [HideInInspector] public float HeavyBiteBonus;      // Sv.3 Ağır Lokma: kendi tier'ındaki lokmalar +oran
        [HideInInspector] public int GluttonEvery;          // Sv.5 Obur: her N. lokma
        [HideInInspector] public float GluttonMultiplier = 3f;
        private int _biteCount;

        // --- Geri Dönüşüm ---
        [HideInInspector] public float RecycleHeal;         // cansız lokma başına can
        [HideInInspector] public float CarHeal;             // Sv.3 Hurdacı: araba ek can
        [HideInInspector] public float OverflowShieldCap;   // Sv.5: can doluyken fazlası geçici kalkan (üst sınır)

        // --- Hızlı Metabolizma (yavaşlama azaltma BlobController.SlowdownReduction'da) ---
        [HideInInspector] public float TierUpBoost;         // Sv.3: tier atlayınca hız +oran
        [HideInInspector] public float TierUpBoostDuration = 3f;
        [HideInInspector] public float BigBiteBoost;        // Sv.5: büyük lokma sonrası hız +oran
        [HideInInspector] public float BigBiteBoostDuration = 1f;
        private float _boostUntil, _boostAmount;

        // --- Hız Sv.5 Roket ---
        [HideInInspector] public float SurfBoost;           // Sabun Sörfü: eski izdeyken hız +oran (SlipTrail yazar)
        [HideInInspector] public float RocketBonus;
        [HideInInspector] public float RocketDelay = 2f;
        private float _movingTime;

        // --- Rejenerasyon ---
        [HideInInspector] public float RegenPerSecond;
        [HideInInspector] public float RegenIdleDelay;      // Sv.3: bu kadar sn hasar almazsa ×2 (0 = yok)
        [HideInInspector] public float TierUpHealFraction;  // Sv.5: tier atlayınca max canın oranı
        private float _regenPending, _regenTimer;
        private const float RegenInterval = 0.25f;

        // --- Yara Öfkesi ---
        [HideInInspector] public float RageThreshold;       // can oranı bunun altındayken öfkeli (0 = skill yok)
        [HideInInspector] public float RageSpeed;
        [HideInInspector] public float RageDamage;
        [HideInInspector] public bool RageNoBite;           // Sv.5: öfkeliyken ısırık hasarı yok
        public bool Enraged { get; private set; }
        public bool NoBiteDamage => RageNoBite && Enraged;

        // --- Bölünme (nadir) ---
        [HideInInspector] public float SplitMassFraction = 0.3f;
        [HideInInspector] public float SplitHealthFraction = 0.3f;
        [HideInInspector] public float SplitInvulnerability = 2f;
        private bool _splitReady;

        private BlobGrowth _growth;
        private BlobHealth _health;
        private BlobController _controller;
        private BlobConsumption _consumption;
        private BlobTier _lastTier;

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
            _health = GetComponent<BlobHealth>();
            _controller = GetComponent<BlobController>();
            _consumption = GetComponent<BlobConsumption>();
            if (_consumption != null) _consumption.Skills = this;
            _lastTier = _growth != null ? _growth.CurrentTier : BlobTier.Tiny;
        }

        private void OnEnable() => GameEvents.OnBlobTierChanged += HandleTierChanged;

        private void OnDisable() => GameEvents.OnBlobTierChanged -= HandleTierChanged;

        private void OnDestroy()
        {
            ResetStatics();
            if (_controller != null) { _controller.SkillSpeedFactor = 1f; _controller.SlowdownReduction = 0f; }
            if (_consumption != null && _consumption.Skills == this) _consumption.Skills = null;
            if (_health != null && _splitReady) _health.SaveFromDeath = null;
        }

        public void EnableSplit()
        {
            if (_health == null || _splitReady) return;
            _splitReady = true;
            _health.SaveFromDeath = TrySplit;
        }

        // Yeme anında, kütle eklenmeden önce: lokmanın kütle çarpanı.
        public float MassMultiplier(EatKind kind, BlobTier foodTier)
        {
            float m = 1f;
            if (HeavyBiteBonus > 0f && _growth != null && foodTier >= _growth.CurrentTier) m += HeavyBiteBonus;
            if (GluttonEvery > 0 && ++_biteCount % GluttonEvery == 0) m *= GluttonMultiplier;
            return m;
        }

        // Yeme olayı (Çöp Fırlatma cephanesi). Bileşen run başında yeniden kurulur, dinleyiciler onunla gider.
        public event System.Action<EatKind, BlobTier> Ate;

        // Yeme anında, kütle eklendikten sonra.
        public void OnAte(EatKind kind, BlobTier foodTier)
        {
            Ate?.Invoke(kind, foodTier);
            if (_health != null && (kind == EatKind.Object || kind == EatKind.Vehicle))
            {
                float heal = RecycleHeal + (kind == EatKind.Vehicle ? CarHeal : 0f);
                if (heal > 0f)
                {
                    float missing = _health.MaxHealth - _health.CurrentHealth;
                    if (missing > 0f) _health.Heal(heal);
                    if (OverflowShieldCap > 0f && heal > missing) _health.AddBonusShield(heal - missing, OverflowShieldCap);
                }
            }

            if (BigBiteBoost > 0f && _growth != null && foodTier >= _growth.CurrentTier)
                Boost(BigBiteBoost, BigBiteBoostDuration);
        }

        private void HandleTierChanged(BlobTier tier)
        {
            bool up = tier > _lastTier;
            _lastTier = tier;
            if (!up) return;
            if (TierUpBoost > 0f) Boost(TierUpBoost, TierUpBoostDuration);
            if (TierUpHealFraction > 0f && _health != null) _health.Heal(_health.MaxHealth * TierUpHealFraction);
        }

        private void Boost(float amount, float duration)
        {
            if (Time.time >= _boostUntil || amount >= _boostAmount) _boostAmount = amount;
            _boostUntil = Mathf.Max(_boostUntil, Time.time + duration);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Yara Öfkesi
            Enraged = RageThreshold > 0f && _health != null && _health.IsAlive
                      && _health.CurrentHealth < _health.MaxHealth * RageThreshold;
            WeaponDamageScale = Enraged ? 1f + RageDamage : 1f;

            // Hız çarpanı: geçici hız + roket + öfke
            if (RocketBonus > 0f && _controller != null)
                _movingTime = _controller.IsMoving ? _movingTime + dt : 0f;
            float factor = 1f;
            if (Time.time < _boostUntil) factor += _boostAmount;
            if (RocketBonus > 0f && _movingTime >= RocketDelay) factor += RocketBonus;
            if (Enraged) factor += RageSpeed;
            factor += SurfBoost;
            if (_controller != null) _controller.SkillSpeedFactor = factor;

            // Rejenerasyon (toplu uygulanır: her karede HUD olayı üretmesin)
            if (RegenPerSecond > 0f && _health != null && _health.IsAlive)
            {
                float rate = RegenPerSecond;
                if (RegenIdleDelay > 0f && _health.TimeSinceHit >= RegenIdleDelay) rate *= 2f;
                _regenPending += rate * dt;
                _regenTimer -= dt;
                if (_regenTimer <= 0f)
                {
                    _regenTimer = RegenInterval;
                    if (_health.CurrentHealth < _health.MaxHealth) _health.Heal(_regenPending);
                    _regenPending = 0f;
                }
            }
        }

        // Ölümcül hasarda bir kez: kütlenin bir kısmını kaybet, bir tier düş, kısa dokunulmazlıkla devam et.
        private bool TrySplit()
        {
            if (!_splitReady || _health == null) return false;
            _splitReady = false;
            _health.SaveFromDeath = null;
            if (_growth != null)
            {
                float lost = _growth.LoseMassForSplit(SplitMassFraction);
                _growth.PunchScale(0.35f);
                // Kaybedilen kütle etrafa jöle parçaları olarak saçılır; toplanırsa geri gelir.
                SplitDrops drops = GetComponent<SplitDrops>();
                if (drops == null) drops = gameObject.AddComponent<SplitDrops>();
                drops.Scatter(lost);
            }
            _health.SetHealth(_health.MaxHealth * SplitHealthFraction);
            _health.GrantInvulnerability(SplitInvulnerability);
            Haptics.Impact(2);
            return true;
        }
    }
}
