using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-04 (Serra: "zırh çalışıyor mu emin olamadım, görsel olarak destekleyelim"): Zırh skill'inin görseli
    // ve dikenleri. Kalkan havuzunun kendisi BlobHealth'te; bu bileşen onun event'lerini dinler.
    // - Baloncuk: blob'un çevresinde kameraya dönük saydam halka (billboard quad — tepeden bakan kamerada
    //   küreden daha iyi okunur, tek quad). Kalkan doldukça belirginleşir, vuruşta parlar, kırılınca patlayıp
    //   kaybolur, yenilenince geri gelir.
    // - Dikenler (Sv.3+): kalkan sağlamken baloncuğa değen düşmanlara 0.5 sn'de bir hasar.
    // - Şok dalgası (Sv.8): kalkan kırılınca çevredeki düşmanlara tek seferlik hasar.
    // ArmorEffect ekler/ayarlar; restart'ta GameManager siler (baloncuk da gider).
    [RequireComponent(typeof(BlobHealth))]
    public class ArmorShield : MonoBehaviour
    {
        private const float ThornInterval = 0.5f;
        private const float BubbleRadiusFactor = 1.3f; // blob gövde yarıçapına göre (tetikleyici 0.6'nın biraz dışı)
        private const float ShockwaveExtraRadius = 2f;
        private const int EnemyLayerMask = 1 << 14;
        private const string MaterialPath = "FX/Mat_ArmorBubble";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color BubbleColor = new Color(0.55f, 0.82f, 1f, 1f);
        private static Texture2D s_ringTexture;

        private readonly Collider[] _hits = new Collider[16];
        private BlobHealth _health;
        private float _bodyRadius = 0.5f; // blob'un katı collider'ı (yerel)
        private float _thornDamagePerTick;
        private float _shockwaveDamage;
        private float _thornTimer;

        private Transform _bubble;
        private Renderer _bubbleRenderer;
        private Material _bubbleMaterial;
        private Transform _camera;
        private float _fill;       // 0..1 kalkan doluluğu
        private float _flash;      // vuruş parlaması, sönümlenir
        private float _breakTime = -1f;

        public void Configure(float thornDamagePerSecond, float shockwaveDamage)
        {
            _thornDamagePerTick = thornDamagePerSecond * ThornInterval;
            _shockwaveDamage = shockwaveDamage;
        }

        private void Awake()
        {
            _health = GetComponent<BlobHealth>();
            foreach (SphereCollider c in GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyRadius = c.radius; break; }
            CreateBubble();
        }

        private void OnEnable()
        {
            _health.OnShieldChanged += HandleShieldChanged;
            _health.OnShieldHit += HandleShieldHit;
            _health.OnShieldBroken += HandleShieldBroken;
            HandleShieldChanged(_health.ShieldCurrent, _health.ShieldMax);
        }

        private void OnDisable()
        {
            _health.OnShieldChanged -= HandleShieldChanged;
            _health.OnShieldHit -= HandleShieldHit;
            _health.OnShieldBroken -= HandleShieldBroken;
        }

        private void OnDestroy()
        {
            if (_bubble != null) Destroy(_bubble.gameObject);
            if (_bubbleMaterial != null) Destroy(_bubbleMaterial);
        }

        private void HandleShieldChanged(float current, float max) => _fill = max > 0f ? current / max : 0f;
        private void HandleShieldHit(float absorbed) => _flash = 1f;

        private void HandleShieldBroken()
        {
            _breakTime = 0f;
            if (_shockwaveDamage > 0f)
                DamageEnemiesInRadius(BubbleWorldRadius() + ShockwaveExtraRadius, _shockwaveDamage);
        }

        private float BubbleWorldRadius() => transform.localScale.x * _bodyRadius * BubbleRadiusFactor;

        private void Update()
        {
            if (_thornDamagePerTick <= 0f || _health.ShieldCurrent <= 0f) return;

            _thornTimer -= Time.deltaTime;
            if (_thornTimer > 0f) return;
            _thornTimer = ThornInterval;
            // Baloncuğa değen (merkezi değil, collider'ı) düşmanlar — OverlapSphere collider yüzeyine bakar.
            if (DamageEnemiesInRadius(BubbleWorldRadius() + 0.15f, _thornDamagePerTick) > 0)
                _flash = Mathf.Max(_flash, 0.6f);
        }

        private int DamageEnemiesInRadius(float radius, float damage)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, EnemyLayerMask);
            int damaged = 0;
            for (int i = 0; i < count; i++)
            {
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>();
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                enemy.TakeDamage(damage);
                damaged++;
            }
            return damaged;
        }

        // --- Görsel ---

        private void CreateBubble()
        {
            Material source = Resources.Load<Material>(MaterialPath);
            if (source == null)
            {
                Debug.LogWarning($"[ArmorShield] Resources/{MaterialPath} bulunamadı — baloncuk görünmeyecek.");
                return;
            }

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(quad.GetComponent<Collider>()); // blob rigidbody'sinin parçası olmasın, bir kare bile
            quad.name = "ArmorBubble";
            quad.layer = gameObject.layer;
            _bubble = quad.transform;
            _bubble.SetParent(transform, false);
            float diameter = _bodyRadius * BubbleRadiusFactor * 2f;
            _bubble.localScale = new Vector3(diameter, diameter, 1f);

            _bubbleMaterial = new Material(source);
            _bubbleMaterial.SetTexture("_BaseMap", GetRingTexture());
            _bubbleRenderer = quad.GetComponent<Renderer>();
            _bubbleRenderer.sharedMaterial = _bubbleMaterial;
            _bubbleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _bubbleRenderer.receiveShadows = false;

            if (Camera.main != null) _camera = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_bubble == null) return;

            if (_camera != null) _bubble.rotation = _camera.rotation; // hep kameraya dönük
            float diameter = _bodyRadius * BubbleRadiusFactor * 2f;

            // Kırılma: büyüyüp söner (0.25 sn), sonra kalkan dolana kadar gizli.
            float breakScale = 1f, breakAlpha = 1f;
            if (_breakTime >= 0f)
            {
                _breakTime += Time.deltaTime;
                float k = _breakTime / 0.25f;
                if (k >= 1f) _breakTime = -1f;
                else
                {
                    breakScale = 1f + 0.35f * k;
                    breakAlpha = 1f - k;
                }
            }

            bool breaking = _breakTime >= 0f;
            bool visible = breaking || _fill > 0.01f;
            if (_bubbleRenderer.enabled != visible) _bubbleRenderer.enabled = visible;
            if (!visible) return;

            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 6f);
            float alpha = breaking ? breakAlpha : Mathf.Lerp(0.35f, 0.75f, _fill) + _flash * 0.25f;
            float punch = 1f + _flash * 0.06f;
            _bubble.localScale = new Vector3(diameter, diameter, 1f) * (breakScale * punch);

            Color color = Color.Lerp(BubbleColor, Color.white, _flash * 0.6f);
            color.a = Mathf.Clamp01(alpha);
            _bubbleMaterial.SetColor(BaseColorId, color);
        }

        // Kameraya dönük "baloncuk": içi hafif dolu, kenarı belirgin halka, sol üstte parlak bir yansıma.
        private static Texture2D GetRingTexture()
        {
            if (s_ringTexture != null) return s_ringTexture;

            const int size = 128;
            s_ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "ArmorBubbleRing" };
            Color[] pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2f - 1f;
                float y = (py + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(x * x + y * y);
                float outside = Mathf.SmoothStep(0.96f, 1f, r);
                float inner = 0.12f + 0.25f * r * r;                       // kenara doğru koyulaşan iç
                float rim = Mathf.SmoothStep(0.78f, 0.95f, r);             // belirgin kenar
                float hx = x + 0.38f, hy = y - 0.38f;
                float highlight = 1f - Mathf.SmoothStep(0.05f, 0.2f, Mathf.Sqrt(hx * hx + hy * hy));
                float alpha = Mathf.Max(inner + rim * 0.75f, highlight * 0.9f) * (1f - outside);
                pixels[py * size + px] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            }
            s_ringTexture.SetPixels(pixels);
            s_ringTexture.Apply(true, true);
            return s_ringTexture;
        }
    }
}
