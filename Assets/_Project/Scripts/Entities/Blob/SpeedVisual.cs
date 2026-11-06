using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-06 (Serra: "skill görsel olarak etki versin ki kullanıcı hissetsin"): Hız skilinin görseli, bütün karakterlerde aynı.
    //   Sv.1-3 kaykay · Sv.4-7 alevli kaykay · Sv.8 alevli kaykay + popoda roket (alev hıza göre uzar, titrer).
    // Modeller zorse-blender/build_speed.py → Resources/FX/Speed (blob görünüm mesh'iyle aynı uzay, pivot blob merkezi).
    // Kaykay varken görünüm Lift kadar kalkar (tekerlekler yere basar); collider/yeme değişmez.
    // SpeedBoostEffect ekler ve seviyeyi verir. Yeni run'da (OnCharacterSelected) sıfırlanır; görünüm yeniden kurulunca
    // eski kaykay onunla birlikte gider, bir sonraki Hız seçiminde yeni görünüme takılır.
    public class SpeedVisual : MonoBehaviour
    {
        private const string Folder = "FX/Speed/";
        private const float Lift = 0.13f;                                         // build_speed.py LIFT
        private static readonly Vector3 FlameOffset = new Vector3(0f, -0.14f, -0.879f); // build_speed.py FLAME OFFSET (Blender x,y,z → x,z,y)
        private const int FlameBoardLevel = 4;
        private const int RocketLevel = 8;
        private const float PopDuration = 0.35f;
        private const float FullFlameSpeed = 4f;                                 // birim/sn: alev bu hızda tam boy

        private static Mesh s_skate1, s_skate2, s_rocket, s_flame;
        private static bool s_loaded;

        private BlobVisual _visual;
        private Rigidbody _rigidbody;
        private Transform _host;
        private Transform _board, _rocket, _flame;
        private MeshFilter _boardFilter;
        private int _form;
        private float _popTime = -1f;
        private Transform _popTarget;
        private bool _popFromZero;
        private float _flicker;

        private void Awake()
        {
            _visual = GetComponent<BlobVisual>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleCharacterSelected;
        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleCharacterSelected;

        private void HandleCharacterSelected(CharacterData _)
        {
            if (_board != null) Destroy(_board.gameObject);
            if (_rocket != null) Destroy(_rocket.gameObject);
            _board = _rocket = _flame = null;
            _host = null;
            _form = 0;
            _popTime = -1f;
            if (_visual != null) _visual.Lift = 0f;
        }

        public void SetLevel(int level)
        {
            int form = level >= RocketLevel ? 3 : level >= FlameBoardLevel ? 2 : level >= 1 ? 1 : 0;
            if (_visual == null) _visual = GetComponent<BlobVisual>();
            if (form == 0 || _visual == null || _visual.ModelRenderer == null) return;
            if (!EnsureParts()) return;
            if (form == _form) return;

            bool boardNew = _form <= 0;
            bool rocketNew = form >= 3 && _form < 3;
            _form = form;
            _boardFilter.sharedMesh = form >= 2 ? s_skate2 : s_skate1;
            _board.gameObject.SetActive(true);
            _rocket.gameObject.SetActive(form >= 3);
            _visual.Lift = Lift;

            _popTarget = rocketNew ? _rocket : _board;
            _popTime = 0f;
            _popFromZero = boardNew || rocketNew;
            if (_popFromZero) _popTarget.localScale = Vector3.zero;
        }

        private bool EnsureParts()
        {
            Renderer host = _visual.ModelRenderer;
            if (_host == host.transform && _board != null) return true;
            if (!LoadMeshes()) return false;

            _host = host.transform;
            _board = CreatePart("SpeedBoard", _host, s_skate1, host, out _boardFilter);
            _rocket = CreatePart("SpeedRocket", _host, s_rocket, host, out _);
            _flame = CreatePart("SpeedRocketFlame", _rocket, s_flame, host, out _);
            _flame.localPosition = FlameOffset;
            _board.gameObject.SetActive(false);
            _rocket.gameObject.SetActive(false);
            _form = 0;
            return true;
        }

        private static Transform CreatePart(string name, Transform parent, Mesh mesh, Renderer host, out MeshFilter filter)
        {
            var go = new GameObject(name);
            go.layer = host.gameObject.layer;
            go.transform.SetParent(parent, false);
            filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = host.sharedMaterial;
            mr.shadowCastingMode = host.shadowCastingMode;
            mr.receiveShadows = host.receiveShadows;
            return go.transform;
        }

        private static bool LoadMeshes()
        {
            if (s_loaded) return s_skate1 != null;
            s_loaded = true;
            s_skate1 = Load("S_Skate_1");
            s_skate2 = Load("S_Skate_2");
            s_rocket = Load("S_Rocket");
            s_flame = Load("S_RocketFlame");
            return s_skate1 != null && s_skate2 != null && s_rocket != null && s_flame != null;
        }

        private static Mesh Load(string name)
        {
            Mesh mesh = Resources.Load<Mesh>(Folder + name);
            if (mesh == null) Debug.LogWarning($"[SpeedVisual] Resources/{Folder}{name} bulunamadı.");
            return mesh;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_popTime >= 0f && _popTarget != null)
            {
                _popTime += dt;
                float t = _popTime / PopDuration;
                if (t >= 1f)
                {
                    _popTime = -1f;
                    _popTarget.localScale = Vector3.one;
                }
                else
                {
                    float s = _popFromZero
                        ? 1f - Mathf.Pow(1f - t, 3f) + Mathf.Sin(t * Mathf.PI) * 0.25f
                        : 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                    _popTarget.localScale = Vector3.one * s;
                }
            }

            // Roket alevi: hızla uzar, dururken küçük kıvılcım gibi kalır; hızlı titrer.
            if (_flame != null && _form >= 3)
            {
                float speed = 0f;
                if (_rigidbody != null)
                {
                    Vector3 v = _rigidbody.linearVelocity;
                    v.y = 0f;
                    speed = v.magnitude;
                }
                float k = Mathf.Clamp01(speed / FullFlameSpeed);
                _flicker += dt * 38f;
                float jitter = 1f + Mathf.Sin(_flicker) * 0.12f + Mathf.Sin(_flicker * 2.7f) * 0.08f;
                float length = Mathf.Lerp(0.25f, 1.25f, k) * jitter;
                float width = Mathf.Lerp(0.7f, 1f, k) * (1f + Mathf.Sin(_flicker * 1.9f) * 0.05f);
                _flame.localScale = new Vector3(width, width, length);
            }
        }
    }
}
