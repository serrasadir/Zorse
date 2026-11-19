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

        private float _lastHitTime = -999f;
        public event System.Action<float> OnShieldHit;            // Zırh skill'inin düştüğü hasar (kalkan parlar)
        public event System.Action<float> OnBonusShieldHit;       // geçici kalkanın (görev, Geri Dönüşüm) yuttuğu hasar

        // Geri Dönüşüm Sv.5: can doluyken fazla iyileşme geçici kalkana döner (Zırh'tan önce yer, yenilenmez).
        public float BonusShield { get; private set; }

        // Market güçlendiricileri (GameManager run başında yazar): her hasardan sabit düşüş, saniyelik can.
        public float MetaArmor { get; set; }
        // Zırh skill'i (ArmorEffect): her darbeden düşülen hasar (Market Zırh'ıyla toplanır), darbe sonrası
        // dokunulmazlık eki ve geri sekme çarpanı. Run başında ResetHealth sıfırlar.
        public float SkillArmor { get; set; }
        public float HitInvulnerabilityBonus { get; set; }
        public float HitPushMultiplier { get; set; } = 1f;
        public float MetaRegenPerSecond { get; set; }
        private const float MetaArmorMinFraction = 0.3f; // zırh hasarı en fazla %70 keser
        private const float MetaRegenTick = 0.5f;
        private float _metaRegenTimer;

        // Kısa dokunulmazlık (darbe sonrası, Bölünme).
        private float _invulnerableUntil;
        // Hasar sonrası nefes payı: kısa dokunulmazlık + yakındaki düşmanlar geri sekilir. Kuşatmada bir darbe
        // arka arkaya beş darbeye dönmesin, kaçacak an kalsın.
        private const float HitInvulnerability = 0.35f;
        private const float HitPushReach = 1.2f;     // blob yüzeyinden
        private const float HitPushDistance = 1.1f;
        private const int EnemyLayerMask = 1 << 14;
        private static readonly Collider[] s_pushHits = new Collider[24];
        private float _solidRadius = -1f;
        public bool TemporarilyInvulnerable => Time.time < _invulnerableUntil;
        public float TimeSinceHit => Time.time - _lastHitTime;

        // Bölünme: ölümcül hasarda çağrılır; true dönerse ölüm iptal (can ve kütle orada ayarlanır).
        public System.Func<bool> SaveFromDeath;
        // Darbeyi tamamen yutan kalkan (Hurda Kalkanı): true dönerse hasar hiç işlenmez.
        public System.Func<bool> AbsorbHit;

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
            BonusShield = 0f;
            SkillArmor = 0f;
            HitInvulnerabilityBonus = 0f;
            HitPushMultiplier = 1f;
            SaveFromDeath = null;
            AbsorbHit = null;
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        private void Update()
        {
            if (!IsAlive) return;
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

            _lastHitTime = Time.time; // her darbe Rejenerasyon'un 'hasar almazsan ×2'sini erteler
            if (AbsorbHit != null && AbsorbHit())
            {
                GrantInvulnerability(HitInvulnerability); // aynı sürüden üst üste parça gitmesin
                return;
            }
            float armor = MetaArmor + SkillArmor;
            if (armor > 0f)
            {
                float reduced = Mathf.Max(amount * MetaArmorMinFraction, amount - armor);
                // Zırh skill'i varsa düşülen kısım kalkan vuruşu sayılır: kalkan parlar, kenar maviye döner.
                if (SkillArmor > 0f && reduced < amount) OnShieldHit?.Invoke(amount - reduced);
                amount = reduced;
            }
            float remainingDamage = amount;

            if (BonusShield > 0f)
            {
                float absorbed = Mathf.Min(BonusShield, remainingDamage);
                BonusShield -= absorbed;
                remainingDamage -= absorbed;
                if (absorbed > 0f) OnBonusShieldHit?.Invoke(absorbed);
            }

            if (remainingDamage > 0f)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - remainingDamage);
                GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
                OnDamaged?.Invoke(remainingDamage);
                if (CurrentHealth > 0f)
                {
                    GrantInvulnerability(HitInvulnerability + HitInvulnerabilityBonus);
                    PushEnemiesBack();
                }
            }

            if (CurrentHealth <= 0f)
            {
                if (SaveFromDeath != null && SaveFromDeath()) return;
                Die();
            }
        }

        private void PushEnemiesBack()
        {
            if (_solidRadius < 0f)
            {
                _solidRadius = 0.5f;
                foreach (SphereCollider c in GetComponents<SphereCollider>())
                    if (!c.isTrigger) { _solidRadius = c.radius; break; }
            }
            Vector3 center = transform.position;
            float radius = _solidRadius * transform.localScale.x + HitPushReach;
            int count = Physics.OverlapSphereNonAlloc(center, radius, s_pushHits, EnemyLayerMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (!s_pushHits[i].TryGetComponent(out Enemies.EnemyBase enemy)) continue;
                enemy.Knockback(enemy.transform.position - center, HitPushDistance * HitPushMultiplier);
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
