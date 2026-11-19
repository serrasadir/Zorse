using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // Zırh skill'inin görseli: blob'un sağ yanında (Fişek'te kafada) süzülen, gidiş yönüyle dönen "sokak eşyası" kalkan.
    // Seviyeyle form değişir: Form 0 Sv.1-2 çöp kutusu kapağı · 1 Sv.3 girilmez tabelası · 2 Sv.4 menhol · 3 Sv.5 altın menhol.
    // Zırh'ın düştüğü her darbede (BlobHealth.OnShieldHit) parlar ve şişer. Modeller: zorse-blender/build_armor_shields.py
    // → Resources/FX/Shield (tek palet materyali). ArmorEffect ekler/ayarlar; restart'ta GameManager siler.
    [RequireComponent(typeof(BlobHealth))]
    public class ArmorShield : MonoBehaviour
    {
        private const string ShieldFolder = "FX/Shield/";
        private const string MaterialPath = ShieldFolder + "Mat_ShieldPalette";
        private static readonly string[] FormMeshes =
        {
            "FX_Shield1_TrashLid", "FX_Shield2_Sign", "FX_Shield3_Manhole", "FX_Shield4_Gold",
        };

        // Yerleşim (blob yerel uzayı, gövde yarıçapı 0.5): gövdenin sağ yanında, yüzü dışa ve hafif yukarı bakar.
        private static readonly Vector3 ShieldOffset = new Vector3(0.62f, 0.05f, 0f);
        private const float ShieldYaw = 90f;           // model +Z'ye bakıyor → +X (sağ) yana çevir
        private const float ShieldTilt = -20f;         // X ekseni; eksi = yüz yukarı bakar
        private const float TurnSpeed = 10f;           // yöne dönme yumuşaklığı
        private const float MinTurnSpeedSqr = 0.3f * 0.3f;
        // Kafada (CharacterData.ArmorOnHead): tepede şapka gibi yatar, ön kenarı gidiş yönünde, hafif geriye yatık.
        public static bool OnHead;                      // GameManager her run başında yazar
        private static readonly Vector3 HeadOffset = new Vector3(0f, 0.47f, -0.02f);
        private static readonly Quaternion HeadRotation =
            Quaternion.AngleAxis(-10f, Vector3.right) * Quaternion.LookRotation(Vector3.up, Vector3.forward); // yüz yukarı, üst kenar öne
        private const float HeadScale = 0.8f;
        private const float HeadBobAmplitude = 0.012f;
        private bool _onHead;

        private const float BobAmplitude = 0.03f;
        private const float BobFrequency = 1.6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Mesh[] s_meshes;
        private static Material s_material;

        private BlobHealth _health;
        private Rigidbody _rigidbody;

        private Transform _pivot;   // blob merkezinde, gidiş yönüne döner
        private Transform _shield;  // pivot'un önünde, modeli taşır
        private MeshFilter _shieldFilter;
        private Renderer _shieldRenderer;
        private MaterialPropertyBlock _block;
        private int _form = -1;
        private float _flash;       // vuruş parlaması, sönümlenir
        private float _punch;       // ölçek şişmesi (vuruş / form değişimi)
        private float _shown;       // 0 gizli .. 1 tam görünür
        private float _bobPhase;
        private Color _lastColor = new Color(-1f, 0f, 0f, 0f);

        // form: 0..3 (bkz. sınıf yorumu). ArmorEffect seviyeye göre hesaplar.
        public void SetForm(int form) => ApplyForm(form);

        private void Awake()
        {
            _health = GetComponent<BlobHealth>();
            _rigidbody = GetComponent<Rigidbody>();
            CreateShield();
        }

        private void OnEnable() => _health.OnShieldHit += HandleShieldHit;
        private void OnDisable() => _health.OnShieldHit -= HandleShieldHit;

        private void OnDestroy()
        {
            if (_pivot != null) Destroy(_pivot.gameObject);
        }

        private void HandleShieldHit(float absorbed)
        {
            _flash = 1f;
            _punch = Mathf.Max(_punch, 0.12f);
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
            _onHead = OnHead;
            _shield.localPosition = _onHead ? HeadOffset : ShieldOffset;
            _shield.localRotation = BaseRotation();
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

        private Quaternion BaseRotation() =>
            _onHead ? HeadRotation : Quaternion.Euler(ShieldTilt, ShieldYaw, 0f);

        private void ApplyForm(int form)
        {
            if (_shieldFilter == null) return;
            form = Mathf.Clamp(form, 0, FormMeshes.Length - 1);
            if (form == _form) return;
            bool upgrade = _form >= 0;
            _form = form;
            _shieldFilter.sharedMesh = s_meshes[form];
            if (upgrade)
            {
                _punch = 0.35f; // yeni form "pop" ile gelsin (ilk çıkış: _shown 0'dan büyür)
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

            // Zırh alınınca belirir, run boyunca kalır.
            _shown = Mathf.MoveTowards(_shown, _form >= 0 ? 1f : 0f, dt * 5f);
            bool visible = _shown > 0.001f;
            if (_shieldRenderer.enabled != visible) _shieldRenderer.enabled = visible;
            if (!visible) return;

            _flash = Mathf.MoveTowards(_flash, 0f, dt * 5f);
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 1.5f);
            _bobPhase += BobFrequency * Mathf.PI * 2f * dt;

            // Pop-in'de hafif taşma (overshoot), vuruşta şişme.
            float pop = 1f - Mathf.Pow(1f - _shown, 3f) + Mathf.Sin(_shown * Mathf.PI) * 0.15f;
            _shield.localScale = Vector3.one * (pop * (1f + _punch) * (_onHead ? HeadScale : 1f));
            float bob = Mathf.Sin(_bobPhase) * (_onHead ? HeadBobAmplitude : BobAmplitude);
            _shield.localPosition = (_onHead ? HeadOffset : ShieldOffset) + new Vector3(0f, bob, 0f);
            _shield.localRotation = BaseRotation();

            // Vuruşta parlar (HDR > 1 dokuyu açar).
            float shade = 1f + _flash * 0.8f;
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
