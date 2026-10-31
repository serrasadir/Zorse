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
        // ceza küçülüyor. Hızlanma skili her seviyede bunu biraz daha azaltabiliyor (bkz. SpeedBoostEffect).
        [SerializeField] private float _tierSlowdownExponent = 0.2f;
        private const float MinTierSlowdownExponent = 0.05f;
        // BlobHealth._maxHealth cache deseniyle aynı: restart'ta Inspector'da ayarlanmış değere
        // dönülür, hardcoded bir sabite değil (ReduceTierSlowdownExponent run-scoped bir upgrade etkisi).
        private float _baseTierSlowdownExponent;

        private Rigidbody _rigidbody;
        private BlobGrowth _blobGrowth;
        private float _speedMultiplier = 1f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _blobGrowth = GetComponent<BlobGrowth>();
            _baseTierSlowdownExponent = _tierSlowdownExponent;

            _rigidbody.linearDamping = _drag;
            _rigidbody.angularDamping = 10f;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
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

        // Hızlanma skili (SpeedBoostEffect) her seviyede çağırır — büyüme cezasını hafifçe azaltır.
        public void ReduceTierSlowdownExponent(float amount) =>
            _tierSlowdownExponent = Mathf.Max(MinTierSlowdownExponent, _tierSlowdownExponent - amount);

        // GameManager.ClearPreviousRunState() restart'ta çağırır (BlobGrowth.ResetGrowth/BlobHealth.ResetHealth ile aynı desen).
        public void ResetTierSlowdown() => _tierSlowdownExponent = _baseTierSlowdownExponent;

        private float SpeedScaleModifier()
        {
            if (_blobGrowth == null) return 1f;
            return 1f / Mathf.Pow((float)_blobGrowth.CurrentTier, _tierSlowdownExponent);
        }
    }
}
