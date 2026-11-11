using BlobSurvivor.Core;
using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Entities;

namespace BlobSurvivor.Entities.Blob
{
    public class VacuumComponent : MonoBehaviour
    {
        [SerializeField] private float _pullSpeed = 6f;
        // 2026-11-03: menzil artık blob merkezinden değil gövde yüzeyinden ölçülüyor — eskiden sabit
        // yarıçap Giant blob'da (gövde ~2) gövdenin sadece 1-1.5 birim dışına uzanıyordu.
        [SerializeField] private float _baseReach = 1.2f; // runtime AddComponent: değer buradan gelir

        // 2026-09-06 (Serra): Temmuz 2026'da bir kod incelemesinde flag'lenmiş ama hiç düzeltilmemiş bir
        // eksikti — sorgu LayerMask'siz her frame çalışıyordu (CLAUDE.md'nin kendi throttle/LayerMask
        // kuralına aykırı, Enemy/Ground/Environment collider'ları da boşuna taranıyordu). Aynı dosyada
        // "yaya/polis çekilmesin" düzeltmesi yapılırken ikisi birlikte ele alındı: LayerMask artık sadece
        // ConsumableTier1-5'i tarıyor (Enemy zaten IConsumable değil, ekstra güvenlik), throttle standart
        // 0.15s tick'e bağlandı.
        private const float UpdateInterval = 0.15f;
        private static int s_consumableMask = -1;
        private float _updateTimer;

        // Gövde yüzeyinden itibaren çekme mesafesi (Vakum skili artırır).
        public float Reach { get; private set; }
        // Blob merkezinden toplam tarama yarıçapı: gövde + menzil.
        public float EffectiveRadius => transform.localScale.x * _bodyLocalRadius + Reach;

        private float _bodyLocalRadius = 0.5f;

        private void OnEnable()
        {
            if (Reach <= 0f) Reach = _baseReach;
            _updateTimer = Random.Range(0f, UpdateInterval);
            if (_fx != null) _fx.SetActive(true);
        }

        private void OnDisable()
        {
            if (_fx != null) _fx.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_fx != null) Destroy(_fx); // restart'ta bileşen silinir (GameManager), efekt de gider
        }

        // --- Menzil efekti: zeminde menzil boyunca dönen hafif kasırga (iki spiral rüzgar katmanı, içe doğru döner). ---
        // Dokular/materyaller: Tools → Blob.io → Vakum Efektini Kur (Resources/FX/Vacuum). Run başına bir kez kurulur.
        private const string FxFolder = "FX/Vacuum/";
        private const float SpinA = 110f, SpinB = 170f; // derece/sn; pozitif Y = tepeden saat yönü → kollar içe akar
        private const float GroundY = 0.17f; // yol 0.05, kaldırım 0.14, helikopter gölgesi 0.15 üstü
        private static Mesh s_quad;
        private GameObject _fx;
        private Transform _swirlA, _swirlB;
        private MeshRenderer _swirlRendererA, _swirlRendererB;
        private float _spinAngleA, _spinAngleB;

        // Kara Delik evrimi (BlackHoleComponent) kullanır: çökme/genişleme ölçeği ve dönüş hızı çarpanı.
        public float FxScale { get; set; } = 1f;
        public float FxSpinBoost { get; set; } = 1f;
        public float FxGroundY => GroundY;

        // Koyu kasırga (Kara Delik): aynı dokular, mor-siyah materyal, daha hızlı döner.
        public void SetDarkFx(bool dark)
        {
            if (_fx == null) return;
            Material a = Resources.Load<Material>(FxFolder + (dark ? "Mat_VacuumSwirlDarkA" : "Mat_VacuumSwirlA"));
            Material b = Resources.Load<Material>(FxFolder + (dark ? "Mat_VacuumSwirlDarkB" : "Mat_VacuumSwirlB"));
            if (a != null) _swirlRendererA.sharedMaterial = a;
            if (b != null) _swirlRendererB.sharedMaterial = b;
            _darkSpin = dark ? DarkSpinMultiplier : 1f;
        }
        private const float DarkSpinMultiplier = 1.6f;
        private float _darkSpin = 1f;

        private void CreateFx()
        {
            Material a = Resources.Load<Material>(FxFolder + "Mat_VacuumSwirlA");
            Material b = Resources.Load<Material>(FxFolder + "Mat_VacuumSwirlB");
            if (a == null || b == null) return;
            _fx = new GameObject("VacuumRange"); // blob'a bağlı değil: blob ölçeği efekti etkilemesin
            _swirlRendererA = CreateQuad("SwirlA", a);
            _swirlRendererB = CreateQuad("SwirlB", b);
            _swirlA = _swirlRendererA.transform;
            _swirlB = _swirlRendererB.transform;
            _swirlB.localPosition = new Vector3(0f, 0.005f, 0f);
        }

        private MeshRenderer CreateQuad(string name, Material mat) => CreateGroundQuad(name, _fx.transform, mat);

        // Zemine yatık efekt kartı (Kara Delik de kullanır).
        public static MeshRenderer CreateGroundQuad(string name, Transform parent, Material mat)
        {
            if (s_quad == null) s_quad = BuildGroundQuad();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = s_quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return r;
        }

        // XZ düzleminde, yukarı bakan 1×1 kare (merkez pivot).
        private static Mesh BuildGroundQuad()
        {
            var m = new Mesh { name = "VacuumQuad" };
            m.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private void LateUpdate()
        {
            if (_fx == null || Reach <= 0f) return;
            float d = EffectiveRadius * 2f * FxScale;
            Vector3 p = transform.position;
            _fx.transform.position = new Vector3(p.x, GroundY, p.z);
            float dt = Time.unscaledDeltaTime * (Time.timeScale > 0f ? 1f : 0f); // duraklatmada durur, ağır çekimde dönmeye devam
            float spin = _darkSpin * FxSpinBoost;
            _spinAngleA += SpinA * spin * dt;
            _spinAngleB += SpinB * spin * dt;
            _swirlA.localScale = new Vector3(d, 1f, d);
            _swirlB.localScale = new Vector3(d, 1f, d);
            _swirlA.localRotation = Quaternion.Euler(0f, _spinAngleA, 0f);
            _swirlB.localRotation = Quaternion.Euler(0f, 90f + _spinAngleB, 0f);
        }

        private BlobGrowth _growth;
        private readonly Collider[] _hits = new Collider[64];
        // Ağır sorgu (OverlapSphere) sadece throttle tick'inde çalışır, sonucu burada cache'lenir —
        // hareketin kendisi (MoveTowards) her frame bu cache üzerinden uygulanır (EnemyBase'in
        // separation/avoidance'ı cache'leyip her frame uygulama deseniyle aynı, aksi halde nesneler
        // 0.15s'de bir "sıçrayarak" hareket eder, akıcı çekilmez).
        private readonly List<Transform> _pulledTargets = new List<Transform>();

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();

            // Blob'da iki SphereCollider var: trigger (yeme) ve solid (gövde) — gövde yarıçapı solid olanınki.
            foreach (SphereCollider c in GetComponents<SphereCollider>())
            {
                if (!c.isTrigger)
                {
                    _bodyLocalRadius = c.radius;
                    break;
                }
            }

            if (s_consumableMask < 0)
            {
                s_consumableMask = TierLayers.ConsumableMask;
            }
            CreateFx();
        }

        private void Update()
        {
            if (Reach <= 0f) return;

            _updateTimer -= Time.deltaTime;
            if (_updateTimer <= 0f)
            {
                _updateTimer = UpdateInterval;
                RefreshPulledTargets();
            }

            float step = _pullSpeed * Time.deltaTime;
            for (int i = 0; i < _pulledTargets.Count; i++)
            {
                Transform target = _pulledTargets[i];
                if (target == null) continue;
                target.position = Vector3.MoveTowards(target.position, transform.position, step);
            }
        }

        private void RefreshPulledTargets()
        {
            _pulledTargets.Clear();

            int count = Physics.OverlapSphereNonAlloc(transform.position, EffectiveRadius, _hits, s_consumableMask);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].gameObject == gameObject) continue;

                var consumable = _hits[i].GetComponent<IConsumable>();
                if (consumable == null || !consumable.IsVacuumable) continue;
                if (_growth != null && consumable.RequiredTier > _growth.CurrentTier) continue;

                _pulledTargets.Add(_hits[i].transform);
            }
        }

        public void IncreaseReach(float amount) => Reach += amount;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.2f);
            Gizmos.DrawSphere(transform.position, EffectiveRadius);
        }
    }
}
