using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    public enum DamageType { Physical, Toxic, Chemical }

    public class BlobHealth : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => _maxHealth;
        public bool IsAlive => CurrentHealth > 0f;
        // Final boss yutulma sahnesinde (BossDeathEffect) hasar alınmaz; run başında sıfırlanır.
        public bool Invulnerable { get; set; }

        // Zırh skill'i: kalkan havuzu. Hasarı önce kalkan yer, biterse can. Son hasardan _shieldRechargeDelay sn
        // sonra _shieldRechargeDuration sn'de dolar. Görseli ArmorShield (Zırh seçilince eklenir) bu event'lerle yapar.
        private float _shieldRechargeDuration = 2f;
        private float _shieldMax;
        private float _shieldRechargeDelay = 4f;
        private float _lastHitTime = -999f;
        // Zırh kilometre taşları: kırılınca dokunulmazlık (sn) ve ikinci katman (kırılınca bir kez anında dolar, bekleme sn).
        private float _breakInvulnerability;
        private float _secondLayerCooldown;
        private float _secondLayerReadyAt;
        public float ShieldCurrent { get; private set; }
        public float ShieldMax => _shieldMax;
        public event System.Action<float, float> OnShieldChanged; // current, max
        public event System.Action<float> OnShieldHit;            // kalkanın yuttuğu hasar
        public event System.Action<float> OnBonusShieldHit;       // geçici kalkanın (Geri Dönüşüm) yuttuğu hasar
        public event System.Action OnShieldBroken;

        // Geri Dönüşüm Sv.5: can doluyken fazla iyileşme geçici kalkana döner (Zırh'tan önce yer, yenilenmez).
        public float BonusShield { get; private set; }

        // Market güçlendiricileri (GameManager run başında yazar): her hasardan sabit düşüş, saniyelik can.
        public float MetaArmor { get; set; }
        public float MetaRegenPerSecond { get; set; }
        private const float MetaArmorMinFraction = 0.3f; // zırh hasarı en fazla %70 keser
        private const float MetaRegenTick = 0.5f;
        private float _metaRegenTimer;

        // Kısa dokunulmazlık (Zırh kırılınca, Bölünme).
        private float _invulnerableUntil;
        public bool TemporarilyInvulnerable => Time.time < _invulnerableUntil;
        public float TimeSinceHit => Time.time - _lastHitTime;

        // Bölünme: ölümcül hasarda çağrılır; true dönerse ölüm iptal (can ve kütle orada ayarlanır).
        public System.Func<bool> SaveFromDeath;

        private float _baseMaxHealth;

        // 2026-11-03: hasar geri bildirimi (DamageFlash kırmızı parlama, CameraController sarsıntı) dinler.
        public event System.Action<float> OnDamaged;

        private void Awake()
        {
            _baseMaxHealth = _maxHealth;
            // Sahne kurulumu gerekmesin diye kırmızı parlama bileşeni burada eklenir.
            if (GetComponent<DamageFlash>() == null) gameObject.AddComponent<DamageFlash>();
        }

        private void Start()
        {
            CurrentHealth = _maxHealth;
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        // M22: restart aynı BlobHealth instance'ını yeniden kullanıyor (sahne reload yok). Bu metod
        // olmadan Zırh/Max Can skill'leri bir önceki run'dan sızardı, ve meta Max Can
        // bonusu (GameManager.ApplyMetaProgression) her restart'ta üst üste eklenip katlanırdı.
        public void ResetHealth()
        {
            _maxHealth = _baseMaxHealth;
            CurrentHealth = _maxHealth;
            Invulnerable = false;
            _invulnerableUntil = 0f;
            _shieldMax = 0f;
            ShieldCurrent = 0f;
            BonusShield = 0f;
            _shieldRechargeDuration = 2f;
            _breakInvulnerability = 0f;
            _secondLayerCooldown = 0f;
            _secondLayerReadyAt = 0f;
            SaveFromDeath = null;
            OnShieldChanged?.Invoke(0f, 0f);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        private void Update()
        {
            if (!IsAlive) return;
            UpdateShieldRecharge();
            UpdateMetaRegen();
        }

        // Seyrek iyileştirme: her kare can olayı (HUD) üretmesin.
        private void UpdateMetaRegen()
        {
            if (MetaRegenPerSecond <= 0f || CurrentHealth >= _maxHealth) { _metaRegenTimer = 0f; return; }
            _metaRegenTimer += Time.deltaTime;
            if (_metaRegenTimer < MetaRegenTick) return;
            Heal(MetaRegenPerSecond * _metaRegenTimer);
            _metaRegenTimer = 0f;
        }

        private void UpdateShieldRecharge()
        {
            if (_shieldMax <= 0f || ShieldCurrent >= _shieldMax) return;
            if (Time.time - _lastHitTime < _shieldRechargeDelay) return;

            ShieldCurrent = Mathf.Min(_shieldMax, ShieldCurrent + _shieldMax / _shieldRechargeDuration * Time.deltaTime);
            OnShieldChanged?.Invoke(ShieldCurrent, _shieldMax);
        }

        // Zırh seçilince/seviye atlayınca: havuz büyür ve hemen dolar.
        public void SetShield(float max, float rechargeDelay, float rechargeDuration = 2f,
            float breakInvulnerability = 0f, float secondLayerCooldown = 0f)
        {
            _shieldMax = Mathf.Max(0f, max);
            _shieldRechargeDelay = rechargeDelay;
            _shieldRechargeDuration = Mathf.Max(0.1f, rechargeDuration);
            _breakInvulnerability = breakInvulnerability;
            if (secondLayerCooldown > 0f && _secondLayerCooldown <= 0f) _secondLayerReadyAt = Time.time;
            _secondLayerCooldown = secondLayerCooldown;
            ShieldCurrent = _shieldMax;
            OnShieldChanged?.Invoke(ShieldCurrent, _shieldMax);
        }

        public void GrantInvulnerability(float seconds) =>
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);

        public void AddBonusShield(float amount, float cap)
        {
            if (amount <= 0f) return;
            BonusShield = Mathf.Min(cap, BonusShield + amount);
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Physical)
        {
            if (!IsAlive || amount <= 0f || Invulnerable || TemporarilyInvulnerable) return;

            _lastHitTime = Time.time; // her hasar (kalkana ya da cana) yenilenmeyi erteler
            if (MetaArmor > 0f) amount = Mathf.Max(amount * MetaArmorMinFraction, amount - MetaArmor);
            float remainingDamage = amount;

            if (BonusShield > 0f)
            {
                float absorbed = Mathf.Min(BonusShield, remainingDamage);
                BonusShield -= absorbed;
                remainingDamage -= absorbed;
                if (absorbed > 0f) OnBonusShieldHit?.Invoke(absorbed);
            }

            if (ShieldCurrent > 0f && remainingDamage > 0f)
            {
                float absorbed = Mathf.Min(ShieldCurrent, remainingDamage);
                ShieldCurrent -= absorbed;
                remainingDamage -= absorbed;
                OnShieldHit?.Invoke(absorbed);
                if (ShieldCurrent <= 0f) BreakShield();
                OnShieldChanged?.Invoke(ShieldCurrent, _shieldMax);
                if (TemporarilyInvulnerable) remainingDamage = 0f; // kırılma anındaki fazla hasar da yenmez
            }

            if (remainingDamage > 0f)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - remainingDamage);
                GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
                OnDamaged?.Invoke(remainingDamage);
            }

            if (CurrentHealth <= 0f)
            {
                if (SaveFromDeath != null && SaveFromDeath()) return;
                Die();
            }
        }

        private void BreakShield()
        {
            OnShieldBroken?.Invoke();
            if (_breakInvulnerability > 0f) GrantInvulnerability(_breakInvulnerability);
            // İkinci katman: hazırsa kalkan bir kez anında dolar.
            if (_secondLayerCooldown > 0f && Time.time >= _secondLayerReadyAt)
            {
                _secondLayerReadyAt = Time.time + _secondLayerCooldown;
                ShieldCurrent = _shieldMax;
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        // Bölünme: ölümcül hasardan sonra (can 0 iken) canı doğrudan ayarlar.
        public void SetHealth(float value)
        {
            CurrentHealth = Mathf.Clamp(value, 1f, _maxHealth);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        public void IncreaseMaxHealth(float amount)
        {
            _maxHealth += amount;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, _maxHealth);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        private void Die()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
                return;
            }

            GameEvents.RaiseGameOver();
        }
    }
}
