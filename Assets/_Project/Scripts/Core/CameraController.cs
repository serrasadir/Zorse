using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Core
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _followSmoothing = 5f;

        [Header("Height & Angle")]
        [SerializeField] private float _baseHeight = 15f;
        [SerializeField] private float _baseTilt = 60f;
        // Dar FOV + uzak kamera = daha az perspektif bozulması (Hole.io gibi minyatür şehir görünümü).
        // FOV düşürülürse aynı alanı görmek için Base Height da artırılmalı (80°→45°: yükseklik ~2 kat).
        [SerializeField, Range(20f, 90f)] private float _fieldOfView = 80f;

        // 2026-11-03 (Serra: "bir süre sonra kamera tamamen blob'u kapsıyor"): kamera blob büyüdükçe uzaklaşır,
        // ama büyümeden YAVAŞ — blob ekranda yine de büyür, büyüme hissi korunur. Yükseklik çarpanı
        // (ölçek / başlangıç ölçeği) ^ _zoomExponent: 0 = hiç uzaklaşmaz, 1 = blob ekranda hep aynı boyda kalır.
        // Örnek (0.3, 2026-11-03'te 0.5'ten indirildi — Serra: "kamera çok uzaklaşıyor, büyüme hissi yok"):
        // blob 8 kat büyüyünce kamera ~1.9 kat uzaklaşır, blob ekranda ~4.3 kat büyük görünür (0.5'te 2.8 kat).
        // (2026-08'de kaldırılan tier zoom'u kademeliydi ve "tak" diye sıçrıyordu; bu sürekli ve yumuşak.)
        [Header("Büyüdükçe Uzaklaşma")]
        [SerializeField, Range(0f, 1f)] private float _zoomExponent = 0.3f;
        [SerializeField] private float _maxHeightMultiplier = 4f;
        [SerializeField] private float _zoomSmoothing = 1.5f;

        // 2026-11-03: blob hasar alınca kısa sarsıntı. Beğenilmezse Hit Shake Amplitude = 0 kapatır.
        [Header("Hasar Sarsıntısı")]
        [SerializeField] private float _hitShakeAmplitude = 0.12f;
        [SerializeField] private float _hitShakeDuration = 0.15f;

        // 2026-11-04 (Serra: "helikopter gelince kamera bir anda uzaklaşsın, helikopteri de blobu da göreyim"):
        // final boss sahnedeyken kamera ikisini birden kadraja alır — takip noktası aralarına kayar, yükseklik
        // helikopterin irtifası + aradaki yatay mesafeye göre artar. Geçiş normal zoom'dan hızlı; boss ölünce döner.
        [Header("Final Boss Çerçeveleme")]
        [SerializeField] private float _bossFramingSmoothing = 3f;
        [SerializeField, Range(0f, 1f)] private float _bossFocusWeight = 0.35f; // 0 = blob, 1 = helikopter
        [SerializeField] private float _bossFramingPadding = 6f;
        [SerializeField] private float _bossZoomBoost = 1.3f;
        [SerializeField] private float _bossMaxHeightMultiplier = 3.5f;
        [SerializeField] private float _bossFramingMaxDistance = 40f; // bundan uzaktaki boss'u kadraja alma
        // Giriş: ilk bu kadar sn ikisi birden kadrajda. Sonra uzaklık kalır, kamera blob'u ortalar (oyuncu ekrandan çıkmasın).
        [SerializeField] private float _bossIntroDuration = 3f;

        [Header("Bounds")]
        [SerializeField] private bool _useBounds;
        [SerializeField] private Vector2 _minBounds;
        [SerializeField] private Vector2 _maxBounds;

        // Gölge mesafesi (URP asset) bu kamera uzaklığına göre ayarlanmıştı (yükseklik 12, eğim 65°).
        private const float ShadowReferenceDistance = 13.24f;
        private const float ReferenceFov = 80f; // boss çerçeveleme formülü bu FOV'a göre

        private Camera _camera;
        private float _punch = 1f, _punchTime = -1f, _punchAmount, _punchDuration;
        private BlobGrowth _growth;
        private BlobHealth _health;
        private float _heightMultiplier = 1f;
        private FinalBossController _trackedBoss;
        private float _bossIntroTime;
        private float _heldBossZoom;
        // Sarsıntı yumuşak takibe karışmasın diye takip pozisyonu ayrı tutulur, sarsıntı üstüne eklenir.
        private Vector3 _followPosition;
        private float _shakeTime = -1f;
        // Final boss yutulma sahnesi: <1 = blob'a yaklaş. Yavaş çekimde de akıcı olsun diye ölçeksiz zamanla yumuşar.
        public float CinematicZoom { get; set; } = 1f;
        private float _shakeAmplitude, _shakeDuration;

        // Kamera uzaklaşınca gölge mesafesi (URP asset'inde, kameradan ölçülür) blob'a yetmez olur ve gölgeler
        // kaybolur. Mesafe, kameranın uzaklaştığı kadar artırılır; oyundan çıkınca asset'in değeri geri yüklenir.
        private UniversalRenderPipelineAsset _urpAsset;
        private float _baseShadowDistance = -1f;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            ApplyFov();
            if (_target != null)
            {
                _growth = _target.GetComponent<BlobGrowth>();
                _health = _target.GetComponent<BlobHealth>();
                if (_health != null) _health.OnDamaged += HandleDamaged;
                transform.position = CalculatePosition(_target.position);
            }
            _followPosition = transform.position;

            transform.rotation = Quaternion.Euler(_baseTilt, 0f, 0f);

            _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_urpAsset != null) _baseShadowDistance = _urpAsset.shadowDistance;
            UpdateShadowDistance();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) ApplyFov();
        }

        private void ApplyFov()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera != null) _camera.fieldOfView = _fieldOfView;
        }

        // Aynı yer alanını kadraja almak için dar FOV'da kaç kat uzak durmak gerekir.
        private float FovDistanceFactor =>
            Mathf.Tan(ReferenceFov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(_fieldOfView * 0.5f * Mathf.Deg2Rad);

        // Gölge mesafesi kameradan ölçülür: kamera referanstan ne kadar uzaksa o kadar artır ki blob'un çevresinde gölge kalsın.
        private const float MaxShadowDistance = 45f;

        private void UpdateShadowDistance()
        {
            if (_urpAsset == null || _baseShadowDistance < 0f) return;
            float distance = _heightMultiplier * _baseHeight / Mathf.Sin(_baseTilt * Mathf.Deg2Rad);
            // Üst sınır: büyük blob'da kamera uzaklaşınca bütün şehir gölge çizmesin (mobil).
            _urpAsset.shadowDistance = Mathf.Min(MaxShadowDistance, _baseShadowDistance + Mathf.Max(0f, distance - ShadowReferenceDistance));
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(float _)
        {
            if (_hitShakeAmplitude > 0f) Shake(_hitShakeAmplitude, _hitShakeDuration);
        }

        private void OnDisable()
        {
            if (_urpAsset != null && _baseShadowDistance >= 0f)
                _urpAsset.shadowDistance = _baseShadowDistance;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            FinalBossController boss = FinalBossController.Active;
            if (boss != _trackedBoss)
            {
                _trackedBoss = boss;
                _bossIntroTime = 0f;
                _heldBossZoom = 0f;
            }

            // Giriş süresi sadece helikopter kadraja alınabilir mesafedeyken işler.
            bool intro = boss != null && _bossIntroTime < _bossIntroDuration && IsBossFramable(boss);
            if (intro) _bossIntroTime += Time.deltaTime;

            Vector3 focus = intro ? Vector3.Lerp(_target.position, boss.BodyCenter, _bossFocusWeight) : _target.position;

            UpdateZoom(intro ? boss : null);
            _punch = UpdatePunch();

            Vector3 desiredPos = CalculatePosition(focus);
            _followPosition = Vector3.Lerp(_followPosition, desiredPos, Time.deltaTime * _followSmoothing);
            transform.position = _followPosition + ShakeOffset();
        }

        // Kısa yakınlaşıp geri uzaklaşma (yaya tekmesi vb. anı vurgulamak için). amount 0.2 = %20 yaklaş.
        public void PunchZoom(float amount, float duration)
        {
            _punchAmount = amount;
            _punchDuration = Mathf.Max(0.05f, duration);
            _punchTime = 0f;
        }

        private float UpdatePunch()
        {
            if (_punchTime < 0f) return 1f;
            _punchTime += Time.deltaTime;
            float t = _punchTime / _punchDuration;
            if (t >= 1f) { _punchTime = -1f; return 1f; }
            // Hızlı yaklaş, yavaşça geri çekil.
            float k = t < 0.2f ? Mathf.SmoothStep(0f, 1f, t / 0.2f) : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.8f);
            return 1f - _punchAmount * k;
        }

        // Hasar ve büyük lokmalar (EatEffects) çağırır; süren sarsıntıdan güçlüyse onu ezer.
        public void Shake(float amplitude, float duration)
        {
            if (_shakeTime >= 0f && _shakeAmplitude * (1f - _shakeTime / _shakeDuration) > amplitude) return;
            _shakeAmplitude = amplitude;
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTime = 0f;
        }

        private Vector3 ShakeOffset()
        {
            if (_shakeTime < 0f) return Vector3.zero;

            _shakeTime += Time.deltaTime;
            float k = 1f - _shakeTime / _shakeDuration;
            if (k <= 0f)
            {
                _shakeTime = -1f;
                return Vector3.zero;
            }
            // Kamera uzaklaştıkça aynı genlik ekranda küçülür — zoom ile ölçeklenir ki her boyda aynı hissettirsin.
            return Random.insideUnitSphere * (_shakeAmplitude * _heightMultiplier * k);
        }

        private bool IsBossFramable(FinalBossController boss)
        {
            Vector3 d = boss.BodyCenter - _target.position;
            d.y = 0f;
            return d.sqrMagnitude <= _bossFramingMaxDistance * _bossFramingMaxDistance;
        }

        private void UpdateZoom(FinalBossController boss)
        {
            float target = 1f;
            if (_growth != null && _growth.BaseScale > 0f)
                target = Mathf.Min(_maxHeightMultiplier, Mathf.Pow(_growth.SmoothScale / _growth.BaseScale, _zoomExponent));

            float smoothing = _zoomSmoothing;
            if (boss != null)
            {
                Vector3 bossPos = boss.BodyCenter;
                Vector3 flat = bossPos - _target.position;
                flat.y = 0f;
                // Helikopterin irtifası + aralarındaki mesafe + pay kadar yükseklik; kamera helikopterin altına düşmesin.
                // Boss gelince en az _bossZoomBoost kat uzaklaşır ("bir anda" hissi), üst sınır blob zoom'unu kısmaz.
                float needed = Mathf.Max(target * _bossZoomBoost, (bossPos.y + flat.magnitude + _bossFramingPadding) * FovDistanceFactor / _baseHeight);
                target = Mathf.Min(Mathf.Max(_bossMaxHeightMultiplier, target), needed);
                smoothing = _bossFramingSmoothing;
                _heldBossZoom = Mathf.Max(_heldBossZoom, _heightMultiplier);
            }
            else if (_trackedBoss != null && _heldBossZoom > target)
            {
                // Girişten sonra: ulaşılan uzaklık boss ölene kadar kalır.
                target = _heldBossZoom;
            }

            float previous = _heightMultiplier;
            bool cinematic = !Mathf.Approximately(CinematicZoom, 1f);
            if (cinematic) target *= CinematicZoom;
            float dt = cinematic ? Time.unscaledDeltaTime * 2f : Time.deltaTime;
            _heightMultiplier = Mathf.Lerp(_heightMultiplier, target, dt * smoothing);

            // Gölge mesafesi sadece belirgin değişimde güncellenir (asset property'si, her kare dokunmaya gerek yok).
            if (Mathf.Abs(_heightMultiplier - previous) > 0.001f) UpdateShadowDistance();
        }

        private Vector3 CalculatePosition(Vector3 targetPos)
        {
            float height = _baseHeight * _heightMultiplier * _punch;
            float tiltRad = _baseTilt * Mathf.Deg2Rad;
            float zOffset = -height / Mathf.Tan(tiltRad);

            Vector3 pos = new Vector3(targetPos.x, height, targetPos.z + zOffset);

            if (_useBounds)
            {
                pos.x = Mathf.Clamp(pos.x, _minBounds.x, _maxBounds.x);
                pos.z = Mathf.Clamp(pos.z, _minBounds.y, _maxBounds.y);
            }

            return pos;
        }
    }
}
