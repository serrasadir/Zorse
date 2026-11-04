using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-04 (Serra: "zırh çalışıyor mu emin olamadım, görsel olarak destekleyelim"): Zırh skill'inin görseli
    // ve dikenleri. Kalkan havuzunun kendisi BlobHealth'te; bu bileşen onun event'lerini dinler.
    // - Görsel (2026-11-04 revize, Serra): baloncuk kalktı → blob'un önünde süzülen, gidiş yönüne dönen
    //   "sokak eşyası" kalkan. Seviyeyle form değişir, her form yeni bir mekanikle aynı anda gelir:
    //     Form 0 Sv.1-2 çöp kutusu kapağı · Form 1 Sv.3-5 dikenli girilmez tabelası (dikenler) ·
    //     Form 2 Sv.6-7 dikenli menhol kapağı (güçlü dikenler) · Form 3 Sv.8 altın menhol (şok dalgası)
    //   Modeller: zorse-blender/build_armor_shields.py → Resources/FX/Shield (tek palet materyali).
    //   Kalkan azaldıkça koyulaşır, vuruşta parlar ve şişer, kırılınca dönerek küçülüp kaybolur,
    //   dolmaya başlayınca geri "pop" eder. Kesin değer HUD'daki kalkan barında.
    // - Dikenler (Sv.3+): kalkan sağlamken blob'a değen düşmanlara 0.5 sn'de bir hasar.
    // - Şok dalgası (Sv.8): kalkan kırılınca çevredeki düşmanlara tek seferlik hasar.
    // ArmorEffect ekler/ayarlar; restart'ta GameManager siler (kalkan modeli de gider).
    [RequireComponent(typeof(BlobHealth))]
    public class ArmorShield : MonoBehaviour
    {
        private const float ThornInterval = 0.5f;
        private const float ThornRadiusFactor = 1.3f; // blob gövde yarıçapına göre (tetikleyici 0.6'nın biraz dışı)
        private const float ShockwaveExtraRadius = 2f;
        private const int EnemyLayerMask = 1 << 14;

        private const string ShieldFolder = "FX/Shield/";
        private const string MaterialPath = ShieldFolder + "Mat_ShieldPalette";
        private static readonly string[] FormMeshes =
        {
            "FX_Shield1_TrashLid", "FX_Shield2_Sign", "FX_Shield3_Manhole", "FX_Shield4_Gold",
        };

        // Yerleşim (blob yerel uzayı, gövde yarıçapı 0.5): gövdenin hemen önünde, yüzü kameraya doğru hafif yukarı eğik.
        private static readonly Vector3 ShieldOffset = new Vector3(0f, 0.05f, 0.62f);
        private const float ShieldTilt = -20f;         // X ekseni; eksi = yüz yukarı bakar
        private const float TurnSpeed = 10f;           // yöne dönme yumuşaklığı
        private const float MinTurnSpeedSqr = 0.3f * 0.3f;
        private const float BobAmplitude = 0.03f;
        private const float BobFrequency = 1.6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Mesh[] s_meshes;
        private static Material s_material;

        private readonly Collider[] _hits = new Collider[16];
        private BlobHealth _health;
        private Rigidbody _rigidbody;
        private float _bodyRadius = 0.5f; // blob'un katı collider'ı (yerel)
        private float _thornDamagePerTick;
        private float _shockwaveDamage;
        private float _thornTimer;

        private Transform _pivot;   // blob merkezinde, gidiş yönüne döner
        private Transform _shield;  // pivot'un önünde, modeli taşır
        private MeshFilter _shieldFilter;
        private Renderer _shieldRenderer;
        private MaterialPropertyBlock _block;
        private int _form = -1;
        private float _fill;        // 0..1 kalkan doluluğu
        private float _flash;       // vuruş parlaması, sönümlenir
        private float _punch;       // ölçek şişmesi (vuruş / form değişimi)
        private float _shown;       // 0 gizli .. 1 tam görünür
        private float _breakTime = -1f;
        private float _bobPhase;
        private Color _lastColor = new Color(-1f, 0f, 0f, 0f);

        // form: 0..3 (bkz. sınıf yorumu). ArmorEffect seviyeye göre hesaplar.
        public void Configure(float thornDamagePerSecond, float shockwaveDamage, int form)
        {
            _thornDamagePerTick = thornDamagePerSecond * ThornInterval;
            _shockwaveDamage = shockwaveDamage;
            SetForm(form);
        }

        private void Awake()
        {
            _health = GetComponent<BlobHealth>();
            _rigidbody = GetComponent<Rigidbody>();
            foreach (SphereCollider c in GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyRadius = c.radius; break; }
            CreateShield();
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
            if (_pivot != null) Destroy(_pivot.gameObject);
        }

        private void HandleShieldChanged(float current, float max) => _fill = max > 0f ? current / max : 0f;

        private void HandleShieldHit(float absorbed)
        {
            _flash = 1f;
            _punch = Mathf.Max(_punch, 0.12f);
        }

        private void HandleShieldBroken()
        {
            _breakTime = 0f;
            if (_shockwaveDamage > 0f)
                DamageEnemiesInRadius(ThornWorldRadius() + ShockwaveExtraRadius, _shockwaveDamage);
        }

        private float ThornWorldRadius() => transform.localScale.x * _bodyRadius * ThornRadiusFactor;

        private void Update()
        {
            if (_thornDamagePerTick <= 0f || _health.ShieldCurrent <= 0f) return;

            _thornTimer -= Time.deltaTime;
            if (_thornTimer > 0f) return;
            _thornTimer = ThornInterval;
            // Blob'a değen (merkezi değil, collider'ı) düşmanlar — OverlapSphere collider yüzeyine bakar.
            if (DamageEnemiesInRadius(ThornWorldRadius() + 0.15f, _thornDamagePerTick) > 0)
            {
                _flash = Mathf.Max(_flash, 0.6f);
                _punch = Mathf.Max(_punch, 0.08f);
            }
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

        private static bool LoadAssets()
        {
            if (s_meshes != null) return s_material != null;
            s_material = Resources.Load<Material>(MaterialPath);
            s_meshes = new Mesh[FormMeshes.Length];
            for (int i = 0; i < FormMeshes.Length; i++)
            {
                s_meshes[i] = Resources.Load<Mesh>(ShieldFolder + FormMeshes[i]);
                if (s_meshes[i] == null)
                    Debug.LogWarning($"[ArmorShield] Resources/{ShieldFolder}{FormMeshes[i]} bulunamadı.");
            }
            if (s_material == null)
                Debug.LogWarning($"[ArmorShield] Resources/{MaterialPath} bulunamadı — kalkan görünmeyecek.");
            return s_material != null;
        }

        private void CreateShield()
        {
            if (!LoadAssets()) return;

            _pivot = new GameObject("ArmorShieldPivot").transform;
            _pivot.SetParent(transform, false);

            // Collider eklenmez: blob rigidbody'sinin parçası olmasın.
            var go = new GameObject("ArmorShield");
            go.layer = gameObject.layer;
            _shield = go.transform;
            _shield.SetParent(_pivot, false);
            _shield.localPosition = ShieldOffset;
            _shield.localRotation = Quaternion.Euler(ShieldTilt, 0f, 0f);
            _shieldFilter = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = s_material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _shieldRenderer = mr;
            _block = new MaterialPropertyBlock();
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
            _shield.localScale = Vector3.zero;
            _shieldRenderer.enabled = false;
        }

        private void SetForm(int form)
        {
            if (_shieldFilter == null) return;
            form = Mathf.Clamp(form, 0, FormMeshes.Length - 1);
            if (form == _form) return;
            bool upgrade = _form >= 0;
            _form = form;
            _shieldFilter.sharedMesh = s_meshes[form];
            if (upgrade)
            {
                _punch = 0.35f; // yeni form "pop" ile gelsin
                _flash = 1f;
            }
        }

        private void LateUpdate()
        {
            if (_shield == null) return;
            float dt = Time.deltaTime;

            // Gidiş yönüne dön (yatay hız); dururken son yönde kal.
            if (_rigidbody != null)
            {
                Vector3 v = _rigidbody.linearVelocity;
                v.y = 0f;
                if (v.sqrMagnitude > MinTurnSpeedSqr)
                {
                    Quaternion target = Quaternion.LookRotation(v, Vector3.up);
                    _pivot.rotation = Quaternion.Slerp(_pivot.rotation, target, 1f - Mathf.Exp(-TurnSpeed * dt));
                }
            }

            // Kırılma: 0.3 sn'de dönerek küçülür, sonra kalkan dolmaya başlayana kadar gizli.
            float breakSpin = 0f;
            bool breaking = _breakTime >= 0f;
            if (breaking)
            {
                _breakTime += dt;
                float k = _breakTime / 0.3f;
                if (k >= 1f) { _breakTime = -1f; _shown = 0f; breaking = false; }
                else { _shown = 1f - k; breakSpin = k * 360f; }
            }
            else
            {
                float target = _fill > 0.01f ? 1f : 0f;
                _shown = Mathf.MoveTowards(_shown, target, dt * 5f);
            }

            bool visible = _shown > 0.001f;
            if (_shieldRenderer.enabled != visible) _shieldRenderer.enabled = visible;
            if (!visible) return;

            _flash = Mathf.MoveTowards(_flash, 0f, dt * 5f);
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 1.5f);
            _bobPhase += BobFrequency * Mathf.PI * 2f * dt;

            // Pop-in'de hafif taşma (overshoot), vuruşta şişme.
            float pop = breaking ? _shown : 1f - Mathf.Pow(1f - _shown, 3f) + Mathf.Sin(_shown * Mathf.PI) * 0.15f;
            _shield.localScale = Vector3.one * (pop * (1f + _punch));
            _shield.localPosition = ShieldOffset + new Vector3(0f, Mathf.Sin(_bobPhase) * BobAmplitude, 0f);
            _shield.localRotation = Quaternion.Euler(ShieldTilt, 0f, breakSpin);

            // Doluluk: azaldıkça koyulaşır; vuruşta parlar (HDR > 1 dokuyu açar).
            float shade = Mathf.Lerp(0.55f, 1f, _fill) + _flash * 0.8f;
            Color color = new Color(shade, shade, shade, 1f);
            if (color != _lastColor)
            {
                _lastColor = color;
                _block.SetColor(BaseColorId, color);
                _shieldRenderer.SetPropertyBlock(_block);
            }
        }
    }
}
