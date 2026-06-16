using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    public enum DamageType { Physical, Toxic, Chemical }

    public class BlobHealth : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _regenRate = 0f;
        [SerializeField] private float _regenInterval = 1f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => _maxHealth;
        public bool IsAlive => CurrentHealth > 0f;

        private float _armorMultiplier = 1f;
        private float _regenTimer;
        private bool _regenEnabled;

        private void Start()
        {
            CurrentHealth = _maxHealth;
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        private void Update()
        {
            if (!_regenEnabled || !IsAlive) return;

            _regenTimer += Time.deltaTime;
            if (_regenTimer >= _regenInterval)
            {
                _regenTimer = 0f;
                Heal(_regenRate);
            }
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Physical)
        {
            if (!IsAlive) return;

            float reduced = amount * _armorMultiplier;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - reduced);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);

            if (CurrentHealth <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        public float GetArmorMultiplier() => _armorMultiplier;
        public float GetRegenRate() => _regenRate;

        public void SetArmorMultiplier(float multiplier) => _armorMultiplier = Mathf.Clamp01(multiplier);

        public void IncreaseMaxHealth(float amount)
        {
            _maxHealth += amount;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, _maxHealth);
            GameEvents.RaiseHealthChanged(CurrentHealth, _maxHealth);
        }

        public void EnableRegen(float rate, float interval = 1f)
        {
            _regenRate = rate;
            _regenInterval = interval;
            _regenEnabled = true;
            _regenTimer = 0f;
        }

        public void DisableRegen() => _regenEnabled = false;

        private void Die()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();

            GameEvents.RaiseGameOver();
        }
    }
}
