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
        private BlobVisual _visual;
        private float _speedMultiplier = 1f;

        // Yiyemediğimiz (bizden büyük) yayaya çarpınca yaya bizi tekmeler: geri savrulur, yuvarlanırız. Can gitmez.
        // [0] = yaya bir tier büyük, [1] = iki+ tier büyük (daha sert).
        // Savrulma mesafesi ≈ hız × süre / 2: ~5 ve ~7.5 birim. Anı okutmak için kısa ağır çekim + kamera yakınlaşması.
        private static readonly float[] KickSpeed = { 14f, 18f };
        private static readonly float[] KickTime = { 0.7f, 0.85f };
        private static readonly float[] KickRolls = { 2f, 3f };
        private static readonly float[] DizzyTime = { 1.4f, 2f };
        private static readonly float[] KickZoom = { 0.3f, 0.38f };    // kamera bu oranda yaklaşıp geri çekilir
        private const float KickZoomTime = 1.4f;
        private const float SlowMo = 0.4f;          // tekme anında oyun hızı
        private const float SlowMoHold = 0.45f;     // gerçek saniye
        private const float SlowMoRelease = 0.35f;  // normale dönüş (gerçek saniye)
        private CameraController _camera;
        private const float KickCooldown = 0.6f;
        private Vector3 _kickVelocity;
        private float _kickTime = -1f, _kickDuration, _nextKickAt;
        private float _slowMoStart = -1f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _blobGrowth = GetComponent<BlobGrowth>();
            _visual = GetComponent<BlobVisual>();

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
            float speed = _baseMoveSpeed * _speedMultiplier * FormSpeedFactor * SpeedScaleModifier();

            Vector3 targetVel = dir.sqrMagnitude > 0.01f
                ? new Vector3(dir.x, 0f, dir.y) * speed
                : Vector3.zero;

            if (_kickTime >= 0f)
            {
                // Savrulmanın ilk yarısında kontrol tamamen elden gider, sonra yumuşakça geri gelir.
                float t = _kickTime / _kickDuration;
                float k = 1f - t;
                float control = Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.5f);
                targetVel = targetVel * control + _kickVelocity * k;
                _kickTime += Time.fixedDeltaTime;
                if (_kickTime >= _kickDuration) _kickTime = -1f;
            }

            _rigidbody.linearVelocity = new Vector3(targetVel.x, _rigidbody.linearVelocity.y, targetVel.z);
        }

        // Dışarıdan savurma (yaya tekmesi vb.): hız süre boyunca sönümlenir.
        public void Kick(Vector3 velocity, float duration)
        {
            _kickVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _kickDuration = Mathf.Max(0.05f, duration);
            _kickTime = 0f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time < _nextKickAt || _blobGrowth == null) return;
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            var ped = collision.collider.GetComponent<Pedestrians.PedestrianController>(); // çarpışma anında, nadiren
            if (ped == null || !ped.isActiveAndEnabled) return;
            int diff = (int)ped.RequiredTier - (int)_blobGrowth.CurrentTier;
            if (diff <= 0) return; // yiyebiliyoruz
            int i = Mathf.Min(diff, KickSpeed.Length) - 1;
            _nextKickAt = Time.time + KickCooldown;

            Vector3 away = transform.position - ped.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = Vector3.back;
            away.Normalize();

            ped.PlayKick(transform.position);
            Kick(away * KickSpeed[i], KickTime[i]);
            // Takla savrulmadan uzun sürer: yavaş dönüş gözle takip edilsin.
            if (_visual == null) _visual = GetComponent<BlobVisual>(); // GameManager run başında ekler (Awake'ten sonra)
            if (_visual != null) _visual.Tumble(away, KickRolls[i], KickTime[i] + 0.5f);
            DizzyStars.Play(gameObject, DizzyTime[i]);
            if (_camera == null) _camera = FindAnyObjectByType<CameraController>(); // ilk tekmede bir kez
            if (_camera != null) _camera.PunchZoom(KickZoom[i], KickZoomTime);
            _slowMoStart = Time.unscaledTime;
        }

        // Tekme ağır çekimi. Sadece oyun akarken dokunur; duraklatma/kart ekranı timeScale'i kendisi yönetir.
        private void Update()
        {
            if (_slowMoStart < 0f) return;
            float e = Time.unscaledTime - _slowMoStart;
            bool playing = GameManager.Instance != null && GameManager.Instance.IsPlaying && Time.timeScale > 0f;
            if (e >= SlowMoHold + SlowMoRelease)
            {
                _slowMoStart = -1f;
                if (playing) Time.timeScale = 1f;
                return;
            }
            if (!playing) return;
            float r = Mathf.Clamp01((e - SlowMoHold) / SlowMoRelease);
            Time.timeScale = Mathf.Lerp(SlowMo, 1f, r * r);
        }

        public void SetSpeedMultiplier(float multiplier) => _speedMultiplier = multiplier;

        public float GetSpeedMultiplier() => _speedMultiplier;

        // Skill'lerden gelen anlık hız çarpanı (Yara Öfkesi, Hızlı Metabolizma, Hız Sv.5); BlobSkills yazar.
        public float SkillSpeedFactor { get; set; } = 1f;
        public float FormSpeedFactor { get; set; } = 1f; // dönüşüm (dönme dolap) hızı
        // Hızlı Metabolizma: büyüme yavaşlamasını oranla azaltır (0..1, 1 = yok). İlke 3: tamamen kaldırılmaz.
        public float SlowdownReduction { get; set; }
        public float MetaSlowdownReduction { get; set; } // Market: Sindirim Direnci (skill'le toplanır)

        public bool IsMoving => InputManager.Instance != null && InputManager.Instance.MoveDirection.sqrMagnitude > 0.01f;

        private float SpeedScaleModifier()
        {
            if (_blobGrowth == null) return SkillSpeedFactor;
            float exponent = _tierSlowdownExponent * (1f - Mathf.Clamp(SlowdownReduction + MetaSlowdownReduction, 0f, 0.9f));
            return SkillSpeedFactor / Mathf.Pow(TierLayers.SlowdownRank(_blobGrowth.CurrentTier), exponent);
        }
    }
}
