using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.UI
{
    // K21 kenar uyarısı (hud.md §4.2): can vuruşu kırmızı, kalkan vuruşu mavi yarım şiddet; kuşatılmada flaş yerine
    // sabit ton (erişilebilirlik §2.4). Düşük can: ayrı nabız katmanı + can barı solunda "!" + tek güçlü haptik.
    // Hasar/kalkan haptiği de burada. Ölçeksiz zaman.
    public class EdgeWarning : MonoBehaviour
    {
        private const float CalmGap = 1.0f;
        private const float Rise = 0.06f, Fade = 0.3f;
        private const float PeakMin = 0.25f, PeakMax = 0.6f, DamageRef = 15f;
        private const float SustainAlpha = 0.35f, SustainBlend = 0.15f, SustainHold = 0.5f, SustainRelease = 0.3f;
        private const float ShieldScale = 0.5f, ColorBlend = 0.15f;
        private const float LowFraction = 0.30f, LowRearm = 0.40f;
        private const float LowMin = 0.06f, LowMax = 0.18f, LowHz = 1.2f;
        private const float DamageHapticInterval = 0.5f;

        private static readonly Color HitRed = new Color(1f, 0.15f, 0.15f);
        private static readonly Color ShieldBlue = new Color(0.55f, 0.82f, 1f);

        private enum Phase { Idle, FlashRise, FlashFade, Sustain, Release }

        private GameObject _hud;
        private Image _vignette;
        private GameObject _lowBadge;
        private BlobHealth _health;

        private Phase _phase;
        private float _phaseTime, _peak, _alpha, _releaseFrom, _blendSpeed;
        private float _colorT = 1f; // 0 = mavi, 1 = kırmızı
        private float _lastHitTime = -99f, _lastRedTime = -99f, _lastHapticTime = -99f;

        // Aynı karedeki vuruşlar LateUpdate'te tek vuruş: kalkan + can = kırmızı.
        private bool _pendingRed, _pendingBlue;
        private float _pendingRedDamage, _pendingBlueDamage;

        private bool _low, _lowHapticArmed = true;
        private float _lowTime;

        internal void Init(GameObject hud, RectTransform layer, Slider healthBar)
        {
            _hud = hud;
            var go = new GameObject("EdgeWarning", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = layer.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(layer, false);
            HudFeedbackLayer.Stretch(rect);
            _vignette = go.GetComponent<Image>();
            _vignette.sprite = BuildVignetteSprite();
            _vignette.type = Image.Type.Simple;
            _vignette.raycastTarget = false;
            _vignette.color = new Color(1f, 1f, 1f, 0f);
            _vignette.enabled = false;

            BuildLowBadge(healthBar);
            Bind(FindAnyObjectByType<BlobHealth>());
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += OnHealthChanged;
            GameEvents.OnCharacterSelected += OnRunStarted;
            GameEvents.OnGameOver += ResetState;
            GameEvents.OnRunComplete += OnRunComplete;
            Subscribe(true);
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= OnHealthChanged;
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnGameOver -= ResetState;
            GameEvents.OnRunComplete -= OnRunComplete;
            Subscribe(false);
        }

        private void OnRunStarted(CharacterData _)
        {
            ResetState();
            Bind(FindAnyObjectByType<BlobHealth>()); // run başına bir kez
        }

        private void OnRunComplete(RunEndReason _) => ResetState();

        private void Bind(BlobHealth health)
        {
            if (health == _health) return;
            Subscribe(false);
            _health = health;
            Subscribe(true);
        }

        private void Subscribe(bool on)
        {
            if (_health == null) return;
            if (on)
            {
                _health.OnDamaged += OnDamaged;
                _health.OnShieldHit += OnShieldHit;
                _health.OnBonusShieldHit += OnShieldHit;
            }
            else
            {
                _health.OnDamaged -= OnDamaged;
                _health.OnShieldHit -= OnShieldHit;
                _health.OnBonusShieldHit -= OnShieldHit;
            }
        }

        private void OnDamaged(float amount) { _pendingRed = true; _pendingRedDamage += amount; }
        private void OnShieldHit(float amount) { _pendingBlue = true; _pendingBlueDamage += amount; }

        private void OnHealthChanged(float current, float max)
        {
            if (max <= 0f) return;
            float f = current / max;
            if (!_low && current > 0f && f <= LowFraction)
            {
                _low = true;
                _lowTime = 0f;
                if (_lowBadge != null) _lowBadge.SetActive(true);
                if (_lowHapticArmed)
                {
                    _lowHapticArmed = false;
                    Haptics.Impact(2);
                    _lastHapticTime = Time.unscaledTime; // aynı karede ikinci (orta) titreşim olmasın
                }
            }
            else if (_low && f > LowFraction)
            {
                _low = false;
                if (_lowBadge != null) _lowBadge.SetActive(false);
            }
            if (!_lowHapticArmed && f > LowRearm) _lowHapticArmed = true;
        }

        private void LateUpdate()
        {
            if (_vignette == null) return;
            GameManager gm = GameManager.Instance;
            bool visible = (_hud == null || _hud.activeInHierarchy) && (gm == null || gm.CurrentState != GameState.Menu);
            if (!visible)
            {
                _pendingRed = _pendingBlue = false;
                _pendingRedDamage = _pendingBlueDamage = 0f;
                if (_vignette.enabled) _vignette.enabled = false;
                return;
            }

            float now = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;
            if (_pendingRed || _pendingBlue) ApplyHit(now);
            if (_phase == Phase.Idle && !_low)
            {
                if (_vignette.enabled) _vignette.enabled = false;
                return;
            }

            StepDamageLayer(now, dt);

            float lowAlpha = 0f;
            if (_low)
            {
                _lowTime += dt;
                lowAlpha = Mathf.Lerp(LowMin, LowMax, 0.5f + 0.5f * Mathf.Sin(_lowTime * LowHz * 2f * Mathf.PI));
            }

            // Katmanlar toplanmaz, büyük olan çizilir.
            Color c = _alpha >= lowAlpha ? Color.Lerp(ShieldBlue, HitRed, _colorT) : HitRed;
            c.a = Mathf.Max(_alpha, lowAlpha);
            if (c.a <= 0.001f)
            {
                if (_vignette.enabled) _vignette.enabled = false;
                return;
            }
            if (!_vignette.enabled) _vignette.enabled = true;
            _vignette.color = c;
        }

        private void ApplyHit(float now)
        {
            bool red = _pendingRed;
            float damage = red ? _pendingRedDamage : _pendingBlueDamage;
            _pendingRed = _pendingBlue = false;
            _pendingRedDamage = _pendingBlueDamage = 0f;

            float gap = now - _lastHitTime;
            _lastHitTime = now;
            if (red) _lastRedTime = now;
            float scale = red ? 1f : ShieldScale;

            if (now - _lastHapticTime >= DamageHapticInterval)
            {
                _lastHapticTime = now;
                Haptics.Impact(red ? 1 : 0);
            }

            switch (_phase)
            {
                case Phase.Idle:
                    if (gap >= CalmGap) StartFlash(damage, scale, red);
                    else EnterSustain(now);
                    break;
                case Phase.FlashRise:
                    _peak = Mathf.Max(_peak, Peak(damage) * scale); // yeni flaş yok
                    if (red) _colorT = 1f;
                    break;
                default:
                    EnterSustain(now);
                    break;
            }
        }

        private void StartFlash(float damage, float scale, bool red)
        {
            _phase = Phase.FlashRise;
            _phaseTime = 0f;
            _alpha = 0f;
            _peak = Peak(damage) * scale;
            _colorT = red ? 1f : 0f;
        }

        private void EnterSustain(float now)
        {
            if (_phase == Phase.Sustain) return;
            _phase = Phase.Sustain;
            _blendSpeed = Mathf.Max(Mathf.Abs(SustainTarget(now) - _alpha), 0.05f) / SustainBlend;
        }

        private float SustainTarget(float now) => SustainAlpha * (now - _lastRedTime < SustainHold ? 1f : ShieldScale);

        private static float Peak(float damage) => Mathf.Lerp(PeakMin, PeakMax, Mathf.Clamp01(damage / DamageRef));

        private void StepDamageLayer(float now, float dt)
        {
            _phaseTime += dt;
            switch (_phase)
            {
                case Phase.FlashRise:
                    _alpha = _peak * Mathf.Clamp01(_phaseTime / Rise);
                    if (_phaseTime >= Rise) { _phase = Phase.FlashFade; _phaseTime = 0f; _alpha = _peak; }
                    break;
                case Phase.FlashFade:
                    _alpha = _peak * (1f - Mathf.Clamp01(_phaseTime / Fade));
                    if (_phaseTime >= Fade) GoIdle();
                    break;
                case Phase.Sustain:
                    bool red = now - _lastRedTime < SustainHold;
                    _alpha = Mathf.MoveTowards(_alpha, SustainAlpha * (red ? 1f : ShieldScale), _blendSpeed * dt);
                    _colorT = Mathf.MoveTowards(_colorT, red ? 1f : 0f, dt / ColorBlend);
                    if (now - _lastHitTime >= SustainHold) { _phase = Phase.Release; _phaseTime = 0f; _releaseFrom = _alpha; }
                    break;
                case Phase.Release:
                    _alpha = _releaseFrom * (1f - Mathf.Clamp01(_phaseTime / SustainRelease));
                    if (_phaseTime >= SustainRelease) GoIdle();
                    break;
            }
        }

        private void GoIdle()
        {
            _phase = Phase.Idle;
            _phaseTime = 0f;
            _alpha = 0f;
        }

        private void ResetState()
        {
            GoIdle();
            _pendingRed = _pendingBlue = false;
            _pendingRedDamage = _pendingBlueDamage = 0f;
            _lastHitTime = _lastRedTime = -99f;
            _low = false;
            _lowHapticArmed = true;
            if (_lowBadge != null) _lowBadge.SetActive(false);
            if (_vignette != null) _vignette.enabled = false;
        }

        // Can barının sol ucunun 12 px solunda 48x48 kırmızı daire + 36 px "!". Barın çocuğu: bar taşınınca/uzayınca gelir.
        private void BuildLowBadge(Slider healthBar)
        {
            if (healthBar == null) return;
            var barRect = (RectTransform)healthBar.transform;
            var go = new GameObject("LowHealthBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = barRect.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(barRect, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-12f, 0f);
            rect.sizeDelta = new Vector2(48f, 48f);
            var image = go.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>("UI/Shapes/UI_Circle");
            image.preserveAspect = true;
            image.color = HitRed;
            image.raycastTarget = false;

            var textGo = new GameObject("Mark", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.layer = go.layer;
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            HudFeedbackLayer.Stretch(textRect);
            var text = textGo.GetComponent<TMP_Text>();
            text.text = "!";
            text.fontSize = 36f;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;

            go.SetActive(false);
            _lowBadge = go;
        }

        // Kenarlarda yoğun, ortada şeffaf beyaz gradyan; kurulumda bir kez. Kenar kalınlıkları dikey ekranda ~eşit (~210 ref px).
        private static Sprite BuildVignetteSprite()
        {
            const int W = 128, H = 256;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.name = "EdgeWarningVignette";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float ey = EdgeRamp(Mathf.Abs((y + 0.5f) / H * 2f - 1f), 0.78f);
                for (int x = 0; x < W; x++)
                {
                    float ex = EdgeRamp(Mathf.Abs((x + 0.5f) / W * 2f - 1f), 0.6f);
                    float a = 1f - (1f - ex) * (1f - ey);
                    px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static float EdgeRamp(float d, float inner)
        {
            float t = Mathf.Clamp01((d - inner) / (1f - inner));
            return t * t * (3f - 2f * t);
        }
    }
}
