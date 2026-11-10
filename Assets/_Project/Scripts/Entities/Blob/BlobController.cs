using UnityEngine;
using BlobSurvivor.Input;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    public class BlobController : MonoBehaviour
    {
        [SerializeField] private float _baseMoveSpeed = 5f;
        [SerializeField] private float _drag = 8f;

        // GDD_v2.md §17 Pillar 2 "büyüdükçe yavaşla" (hız = 1/tier^üs) korunur ama ayarlanabilir kalır —
        // 2026-09-06 (Serra): eski üs 0.5 (1/√tier) Giant'ta hızı %44.7'ye düşürüyordu, "çok zor takip
        // ediliyorum" şikayetine yol açtı. 0.2'ye düşürüldü (Giant'ta ~%72.5) — takas hissi kalıyor,
        // ceza küçülüyor. Run boyunca sabit (Hız skili sadece çarpanı artırır).
        [SerializeField] private float _tierSlowdownExponent = 0.2f;

        private Rigidbody _rigidbody;
        private BlobGrowth _blobGrowth;
        private float _speedMultiplier = 1f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _blobGrowth = GetComponent<BlobGrowth>();

            _rigidbody.linearDamping = _drag;
            _rigidbody.angularDamping = 10f;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            // Fizik 50 Hz, ekran 30/60 Hz: interpolasyonsuz blob ve kamera titrer.
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            if (InputManager.Instance == null) return;

            Vector2 dir = InputManager.Instance.MoveDirection;
            float speed = _baseMoveSpeed * _speedMultiplier * SpeedScaleModifier();

            Vector3 targetVel = dir.sqrMagnitude > 0.01f
                ? new Vector3(dir.x, 0f, dir.y) * speed
                : Vector3.zero;

            _rigidbody.linearVelocity = new Vector3(targetVel.x, _rigidbody.linearVelocity.y, targetVel.z);
        }

        public void SetSpeedMultiplier(float multiplier) => _speedMultiplier = multiplier;

        public float GetSpeedMultiplier() => _speedMultiplier;

        private float SpeedScaleModifier()
        {
            if (_blobGrowth == null) return 1f;
            return 1f / Mathf.Pow(TierLayers.SlowdownRank(_blobGrowth.CurrentTier), _tierSlowdownExponent);
        }
    }
}
