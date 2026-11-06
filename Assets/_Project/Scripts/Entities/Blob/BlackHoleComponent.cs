using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    // B17 evrimi "Kara Delik" (Vakum(max) + Sindirim(max)): blob'un çevresindeki, yiyebildiği her şey
    // temasa gerek kalmadan otomatik yutulur.
    // 2026-11-03: eskiden sabit 1 birim yarıçap + sadece Tiny/Small'du — bu evrime ulaşıldığında blob zaten
    // Large/Giant olduğu için yarıçap gövdenin içinde kalıyor, yenenler de önemsiz mass veriyordu.
    // Artık yarıçap blob'un o anki gövdesiyle ölçeklenir ve tier eşiği blob'un kendi tier'ı.
    public class BlackHoleComponent : MonoBehaviour
    {
        [SerializeField] private float _radiusMultiplier = 2f; // gövde yarıçapının katı
        private const float CheckInterval = 0.1f;
        // ConsumableTier1-5 (9-13) — polis/çevre/zemin taranmasın
        private const int ConsumableMask = TierLayers.ConsumableMask;

        private BlobGrowth _growth;
        private BlobConsumption _consumption;
        private float _bodyLocalRadius = 0.5f;
        private float _timer;
        private readonly Collider[] _hits = new Collider[32];

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
            _consumption = GetComponent<BlobConsumption>();
            _vacuum = GetComponent<VacuumComponent>();
            _visual = GetComponent<BlobVisual>();
            CreateFx();

            foreach (SphereCollider c in GetComponents<SphereCollider>())
            {
                if (!c.isTrigger)
                {
                    _bodyLocalRadius = c.radius;
                    break;
                }
            }
        }

        private void Update()
        {
            UpdateIntro();
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;

            if (_growth == null || _consumption == null) return;

            float radius = transform.localScale.x * _bodyLocalRadius * _radiusMultiplier;
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, ConsumableMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                GameObject target = _hits[i].gameObject;
                if (!target.activeInHierarchy) continue; // bu tarama içinde zaten yenmiş olabilir

                IConsumable consumable = _hits[i].GetComponent<IConsumable>();
                if (consumable == null || consumable.Data == null) continue;
                if (consumable.Data.IsHazard) continue;
                if (consumable.RequiredTier > _growth.CurrentTier) continue;

                _consumption.ConsumeDirect(consumable, target);
            }
        }

        // --- Görsel: evrim anı + kalıcı olay ufku -------------------------------------------------------------
        // Evrim anı (~1.8 sn, ölçeksiz zaman): ağır çekim, açık mavi kasırga hızlanıp blob'a çöker → macenta şok
        // dalgası + titreşim + yutkunma, kasırga koyu mor-siyah olarak geri açılır, olay ufku belirir, zaman normale döner.
        // Kalıcı: blob'un altında yutma yarıçapında siyah disk + dönen macenta yığılma halkası.
        // Dokular/materyaller: Tools → Blob.io → Vakum Efektini Kur (Resources/FX/Vacuum).
        private const string FxFolder = "FX/Vacuum/";
        private const float RimTexRadius = 0.82f;   // halka dokuda r=0.82'de: kart yarı boyu = yarıçap / 0.82
        private const float RimSpin = 260f, CoreSpin = 40f;
        private const float SlowMo = 0.25f;
        private const float CollapseEnd = 0.55f, ExpandEnd = 1.25f, IntroEnd = 1.85f;

        private VacuumComponent _vacuum;
        private BlobVisual _visual;
        private Core.CameraController _camera;
        private GameObject _fx;
        private Transform _core, _rim, _shock;
        private MeshRenderer _shockRenderer;
        private MaterialPropertyBlock _shockBlock;
        private Color _shockColor;
        private float _introTime = -1f; // -1: başlamadı (kart ekranı kapanınca başlar), > IntroEnd: bitti
        private bool _burst;
        private float _horizonScale;
        private float _rimAngle, _coreAngle;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void CreateFx()
        {
            Material core = Resources.Load<Material>(FxFolder + "Mat_BlackHoleCore");
            Material rim = Resources.Load<Material>(FxFolder + "Mat_BlackHoleRim");
            Material shock = Resources.Load<Material>(FxFolder + "Mat_BlackHoleShock");
            if (core == null || rim == null || shock == null) return;

            _fx = new GameObject("BlackHoleFx");
            _core = VacuumComponent.CreateGroundQuad("Core", _fx.transform, core).transform;
            _rim = VacuumComponent.CreateGroundQuad("Rim", _fx.transform, rim).transform;
            _shockRenderer = VacuumComponent.CreateGroundQuad("Shock", _fx.transform, shock);
            _shock = _shockRenderer.transform;
            _core.localPosition = new Vector3(0f, 0.01f, 0f);  // kasırganın (0.17) üstü
            _rim.localPosition = new Vector3(0f, 0.015f, 0f);
            _shock.localPosition = new Vector3(0f, 0.02f, 0f);
            _shock.gameObject.SetActive(false);
            _shockBlock = new MaterialPropertyBlock();
            _shockColor = shock.GetColor(BaseColorId);
            _camera = FindAnyObjectByType<Core.CameraController>();
        }

        private void OnDestroy()
        {
            if (_fx != null) Destroy(_fx);
            if (_vacuum != null) { _vacuum.SetDarkFx(false); _vacuum.FxScale = 1f; _vacuum.FxSpinBoost = 1f; }
            if (_camera != null) _camera.CinematicZoom = 1f;
        }

        private static bool Playing => Core.GameManager.Instance == null || Core.GameManager.Instance.IsPlaying;

        private void UpdateIntro()
        {
            if (_introTime > IntroEnd) return;
            if (!Playing) return; // kart ekranı/duraklatma: bekle (zamanı da elleme)
            if (_introTime < 0f) _introTime = 0f;

            _introTime += Time.unscaledDeltaTime;
            float t = _introTime;

            if (t < CollapseEnd)
            {
                // Çöküş: kasırga hızlanarak blob'a büzülür.
                float u = t / CollapseEnd;
                Time.timeScale = Mathf.Lerp(1f, SlowMo, Mathf.Clamp01(u * 3f));
                if (_vacuum != null) { _vacuum.FxScale = Mathf.Lerp(1f, 0.08f, u * u); _vacuum.FxSpinBoost = Mathf.Lerp(1f, 5f, u); }
                if (_camera != null) _camera.CinematicZoom = Mathf.Lerp(1f, 0.85f, u);
                _horizonScale = 0f;
                return;
            }

            if (!_burst)
            {
                _burst = true;
                if (_vacuum != null) _vacuum.SetDarkFx(true);
                _shock.gameObject.SetActive(true);
                _growth?.PunchScale(0.35f);
                if (_visual != null) _visual.Gulp(0.45f);
                _camera?.Shake(0.3f, 0.5f);
                Core.Haptics.Impact(2);
            }

            if (t < ExpandEnd)
            {
                // Patlama: koyu kasırga geri açılır, olay ufku taşarak belirir, şok dalgası dışa yayılır.
                float u = (t - CollapseEnd) / (ExpandEnd - CollapseEnd);
                float easeOut = 1f - (1f - u) * (1f - u);
                if (_vacuum != null) { _vacuum.FxScale = Mathf.Lerp(0.08f, 1f, easeOut); _vacuum.FxSpinBoost = Mathf.Lerp(5f, 1f, u); }
                _horizonScale = easeOut + Mathf.Sin(u * Mathf.PI) * 0.25f;
                float reach = (_vacuum != null ? _vacuum.EffectiveRadius : BodyRadius() * 3f) * 1.6f;
                float r = Mathf.Lerp(BodyRadius(), reach, easeOut);
                _shock.localScale = new Vector3(r * 2f, 1f, r * 2f);
                Color c = _shockColor;
                c.a *= 1f - u;
                _shockBlock.SetColor(BaseColorId, c);
                _shockRenderer.SetPropertyBlock(_shockBlock);
                return;
            }

            // Toparlanma: zaman ve kamera normale döner.
            _shock.gameObject.SetActive(false);
            _horizonScale = 1f;
            if (_vacuum != null) { _vacuum.FxScale = 1f; _vacuum.FxSpinBoost = 1f; }
            float k = Mathf.Clamp01((t - ExpandEnd) / (IntroEnd - ExpandEnd));
            Time.timeScale = Mathf.Lerp(SlowMo, 1f, k * k);
            if (_camera != null) _camera.CinematicZoom = Mathf.Lerp(0.85f, 1f, k);
            if (t >= IntroEnd)
            {
                Time.timeScale = 1f;
                if (_camera != null) _camera.CinematicZoom = 1f;
            }
        }

        private float BodyRadius() => transform.localScale.x * _bodyLocalRadius;

        private void LateUpdate()
        {
            if (_fx == null) return;
            Vector3 p = transform.position;
            float y = _vacuum != null ? _vacuum.FxGroundY : 0.17f;
            _fx.transform.position = new Vector3(p.x, y, p.z);

            float half = BodyRadius() * _radiusMultiplier / RimTexRadius * Mathf.Max(0f, _horizonScale);
            Vector3 size = new Vector3(half * 2f, 1f, half * 2f);
            _core.localScale = size;
            _rim.localScale = size;

            float dt = Time.unscaledDeltaTime * (Time.timeScale > 0f ? 1f : 0f);
            _rimAngle += RimSpin * dt;
            _coreAngle += CoreSpin * dt;
            _rim.localRotation = Quaternion.Euler(0f, _rimAngle, 0f);
            _core.localRotation = Quaternion.Euler(0f, _coreAngle, 0f);
        }
    }
}
