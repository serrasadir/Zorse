using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // GDD'deki "Hızlanma (Bot)" — oyuncu tetiklemez, kendi cooldown'ında otomatik ateşler.
    // Proje tek input kuralına uyar (GDD_v2.md §17): ikinci bir tuş gerektirmez.
    public class DashComponent : MonoBehaviour
    {
        [SerializeField] private float _dashSpeedMultiplier = 1.8f;
        [SerializeField] private float _dashDuration = 0.6f;
        [SerializeField] private float _dashCooldown = 6f;
        [SerializeField] private float _minCooldown = 1.5f;

        private BlobController _controller;
        private float _cooldownTimer;
        private float _durationTimer;
        private bool _isDashing;

        private void Awake()
        {
            _controller = GetComponent<BlobController>();
            _cooldownTimer = _dashCooldown;
        }

        private void Update()
        {
            if (_isDashing)
            {
                _durationTimer -= Time.deltaTime;
                if (_durationTimer <= 0f)
                    EndDash();
                return;
            }

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
                StartDash();
        }

        private void StartDash()
        {
            if (_controller == null) return;

            _isDashing = true;
            _durationTimer = _dashDuration;
            _controller.SetSpeedMultiplier(_controller.GetSpeedMultiplier() * _dashSpeedMultiplier);
        }

        private void EndDash()
        {
            _isDashing = false;
            _cooldownTimer = _dashCooldown;

            if (_controller != null)
                _controller.SetSpeedMultiplier(_controller.GetSpeedMultiplier() / _dashSpeedMultiplier);
        }

        // Level başına: cooldown azalır, süre biraz artar (DashEffect çağırır).
        public void ApplyLevel(float cooldownReduction)
        {
            _dashCooldown = Mathf.Max(_minCooldown, _dashCooldown - cooldownReduction);
            _dashDuration += 0.05f;
        }
    }
}
