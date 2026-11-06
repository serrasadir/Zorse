using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-06 (Serra: "durduk yere değil, hasar vurdukça can artsın"): Can Emme skill'i (eski Rejenerasyon,
    // id regen_boost). Düşmana gerçekten işleyen hasarın Fraction'ı kadar can; mermi, patlama, diken, şok dalgası
    // hepsi sayılır. İyileşme 0.2 sn'de bir toplu uygulanır (her vuruşta HUD olayı üretmesin).
    // RegenBoostEffect ekler/artırır; restart'ta GameManager siler.
    [RequireComponent(typeof(BlobHealth))]
    public class LifestealComponent : MonoBehaviour
    {
        private const float ApplyInterval = 0.2f;

        public float Fraction { get; private set; }

        private BlobHealth _health;
        private float _pending;
        private float _timer;

        private void Awake() => _health = GetComponent<BlobHealth>();

        private void OnEnable() => EnemyBase.OnDamageDealt += HandleDamageDealt;

        private void OnDisable() => EnemyBase.OnDamageDealt -= HandleDamageDealt;

        public void AddFraction(float amount) => Fraction += amount;

        private void HandleDamageDealt(float damage) => _pending += damage * Fraction;

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = ApplyInterval;

            if (_pending <= 0.01f || _health == null) return;
            if (_health.CurrentHealth < _health.MaxHealth) _health.Heal(_pending);
            _pending = 0f; // canın doluyken biriktirmez
        }
    }
}
