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

        // 2026-11-04 (Serra): Zırh skill'i artık yüzdesel azaltma değil, kalkan havuzu — hasarı önce kalkan yer,
        // biterse can. Son hasardan _shieldRechargeDelay sn sonra _shieldRechargeDuration sn'de dolar.
        // Görseli ve dikenleri ArmorShield (Zırh seçilince eklenir) bu event'leri dinleyerek yapar.
        private const float ShieldRechargeDuration = 2f;
        private float _shieldMax;
        private float _shieldRechargeDelay = 4f;
        private float _lastHitTime = -999f;
        public float ShieldCurrent { get; private set; }
        public float ShieldMax => _shieldMax;
        public event System.Action<float, float> OnShieldChanged; // current, max
        public event System.Action<float> OnShieldHit;            // kalkanın yuttuğu hasar
        public event System.Action OnShieldBroken;

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

        // M22: restart aynı BlobHealth instance'ını yeniden kullanıyor (sahne reload yok) — bu metod
        // olmadan Zırh/Max Can skill'leri bir önceki run'dan sızardı, ve meta Max Can
        // bonusu (GameManager.ApplyMetaProgression) her restart'ta üst üste eklenip katlanırdı.
        public void ResetHealth()
        {
            _maxHealth = _baseMaxHealth;
            CurrentHealth = _maxHealth;
            Invulnerable = false;
            _shieldMax = 0f;
            ShieldCurrent = 0f;
            OnShieldChanged?.Invoke(0f, 0f);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        private void Update()
        {
            if (!IsAlive) return;
            UpdateShieldRecharge();
        }

        private void UpdateShieldRecharge()
        {
            if (_shieldMax <= 0f || ShieldCurrent >= _shieldMax) return;
            if (Time.time - _lastHitTime < _shieldRechargeDelay) return;

            ShieldCurrent = Mathf.Min(_shieldMax, ShieldCurrent + _shieldMax / ShieldRechargeDuration * Time.deltaTime);
            OnShieldChanged?.Invoke(ShieldCurrent, _shieldMax);
        }

        // Zırh seçilince/seviye atlayınca: havuz büyür ve hemen dolar.
        public void SetShield(float max, float rechargeDelay)
        {
            _shieldMax = Mathf.Max(0f, max);
            _shieldRechargeDelay = rechargeDelay;
            ShieldCurrent = _shieldMax;
            OnShieldChanged?.Invoke(ShieldCurrent, _shieldMax);
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Physical)
        {
            if (!IsAlive || amount <= 0f || Invulnerable) return;

            _lastHitTime = Time.time; // her hasar (kalkana ya da cana) yenilenmeyi erteler
            float remainingDamage = amount;

            if (ShieldCurrent > 0f)
            {
                float absorbed = Mathf.Min(ShieldCurrent, remainingDamage);
                ShieldCurrent -= absorbed;
                remainingDamage -= absorbed;
                OnShieldHit?.Invoke(absorbed);
                OnShieldChanged?.Invoke(ShieldCurrent, _shieldMax);
                if (ShieldCurrent <= 0f) OnShieldBroken?.Invoke();
            }

            if (remainingDamage > 0f)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - remainingDamage);
                GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
                OnDamaged?.Invoke(remainingDamage);
            }

            if (CurrentHealth <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
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
