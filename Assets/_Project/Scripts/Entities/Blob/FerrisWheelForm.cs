using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // Özel dönüşüm: haritadaki tek dönme dolabı yiyince 20 sn dönme dolap çarkı olursun (OYUN.md §4).
    //  - Dönüşüm (~1,2 sn, oyun donuk, ölçeksiz zaman): blob ile çark gittikçe hızlanarak yer değiştirir (Mario).
    //  - Çark: gidiş yönünde tekerlek gibi yuvarlanır, ×1,6 hız; üstünden geçtiği polisi/köpeği ezer, boss'a sürekli
    //    hasar, bir üst tier'a kadar her şeyi yer. Hasar alır. Son 3 sn yanıp söner, sonra kısa ters dönüşüm.
    // Görsel: Resources/Transformations/FerrisWheelForm (Tools → Blob.io → Dönme Dolap Formunu Kur). Run başında biter.
    public class FerrisWheelForm : MonoBehaviour
    {
        public const string SourceName = "P_FerrisWheel";
        private const string FormResource = "Transformations/FerrisWheelForm";

        private const float Duration = 20f;
        private const float SpeedFactor = 1.6f;
        private const float DiameterScale = 1.7f;      // çark çapı / blob çapı
        private const float CrushInterval = 0.1f;
        private const float BossDamagePerTick = 20f;   // saniyede 200
        private const float CrushDamage = 99999f;      // boss olmayan anında ölür
        private const float WarnTime = 3f;
        private const float BlinkHz = 8f;
        private const float CameraZoom = 1.25f;
        private const float DustInterval = 0.18f;
        private const float UnmorphDuration = 0.5f;
        // Dönüşümde blob ↔ çark geçişleri arası süre (gittikçe hızlanır), ölçeksiz.
        private static readonly float[] MorphSteps = { 0.2f, 0.17f, 0.14f, 0.12f, 0.1f, 0.08f, 0.07f, 0.06f, 0.06f, 0.05f };
        private static readonly Color DustA = new Color(0.72f, 0.75f, 0.78f);
        private static readonly Color DustB = new Color(0.95f, 0.94f, 0.92f);

        private enum Phase { None, Morph, Rolling, Unmorph }

        public static bool IsActive { get; private set; }

        private Phase _phase;
        private float _timer, _stepTimer, _crushTimer, _dustTimer;
        private int _step;
        private bool _formShown;
        private float _roll, _yaw;
        private float _punch;

        private Transform _form;
        private Renderer[] _formRenderers;
        private float _nativeDiameter = 1f;
        private BlobVisual _visual;
        private BlobGrowth _growth;
        private BlobController _controller;
        private BlobConsumption _consumption;
        private Rigidbody _rigidbody;
        private CameraController _camera;
        private readonly Collider[] _hits = new Collider[48];
        private static readonly int CrushMask = TierLayers.ConsumableMask | (1 << 14); // + Enemy

        // BlobConsumption: dönme dolap yenince.
        public static void Begin(GameObject blob)
        {
            FerrisWheelForm form = blob.GetComponent<FerrisWheelForm>();
            if (form == null) form = blob.AddComponent<FerrisWheelForm>(); // run başına bir kez
            form.StartForm();
        }

        private void Awake()
        {
            _visual = GetComponent<BlobVisual>();
            _growth = GetComponent<BlobGrowth>();
            _controller = GetComponent<BlobController>();
            _consumption = GetComponent<BlobConsumption>();
            _rigidbody = GetComponent<Rigidbody>();
            _camera = FindAnyObjectByType<CameraController>();
            GameObject prefab = Resources.Load<GameObject>(FormResource);
            if (prefab == null)
            {
                Debug.LogWarning("[FerrisWheelForm] Resources/" + FormResource + " yok (Tools → Blob.io → Dönme Dolap Formunu Kur).");
                return;
            }
            GameObject instance = Instantiate(prefab); // run başına bir kez
            instance.name = "FerrisWheelForm";
            _form = instance.transform;
            _formRenderers = instance.GetComponentsInChildren<Renderer>(true);
            FormInfo info = instance.GetComponent<FormInfo>();
            if (info != null && info.Diameter > 0.01f) _nativeDiameter = info.Diameter;
            instance.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterSelected += ForceEnd;
            GameEvents.OnGameOver += ForceEndNow;
            GameEvents.OnRunComplete += ForceEndRun;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= ForceEnd;
            GameEvents.OnGameOver -= ForceEndNow;
            GameEvents.OnRunComplete -= ForceEndRun;
        }

        private void OnDestroy()
        {
            if (_form != null) Destroy(_form.gameObject);
            if (_phase != Phase.None) Restore();
        }

        private void ForceEnd(CharacterData _) => ForceEndNow();
        private void ForceEndRun(RunEndReason _) => ForceEndNow();

        private void ForceEndNow()
        {
            if (_phase == Phase.None) return;
            if (_phase == Phase.Morph && Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 1f;
            Restore();
        }

        private void StartForm()
        {
            if (_form == null || _phase != Phase.None) return;
            _phase = Phase.Morph;
            IsActive = true;
            _step = 0;
            _stepTimer = MorphSteps[0];
            _formShown = false;
            _yaw = _visual != null ? Quaternion.LookRotation(_visual.Forward).eulerAngles.y : 0f;
            _roll = 0f;
            Time.timeScale = 0f; // dönüşüm anı: her şey durur
            Haptics.Impact(1);
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Morph: UpdateMorph(); break;
                case Phase.Rolling: UpdateRolling(); break;
                case Phase.Unmorph: UpdateUnmorph(); break;
            }
        }

        // ---------------------------------------------------------------- dönüşüm

        private void UpdateMorph()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return; // duraklatıldı
            _stepTimer -= Time.unscaledDeltaTime;
            _punch = Mathf.MoveTowards(_punch, 0f, Time.unscaledDeltaTime * 4f);
            PlaceForm(0f);
            if (_stepTimer > 0f) return;

            SwapVisible(!_formShown);
            _punch = 0.25f;
            _step++;
            if (_step < MorphSteps.Length)
            {
                _stepTimer = MorphSteps[_step];
                return;
            }

            // Son geçiş: çark kalır.
            SwapVisible(true);
            _punch = 0.4f;
            Time.timeScale = 1f;
            _phase = Phase.Rolling;
            _timer = Duration;
            _crushTimer = 0f;
            _dustTimer = 0f;
            if (_controller != null) _controller.FormSpeedFactor = SpeedFactor;
            if (_camera != null) _camera.CinematicZoom = CameraZoom;
            DeathFx.Play(FormBounds(), new Color(0.95f, 0.45f, 0.4f), new Color(1f, 0.85f, 0.35f), true);
            GameEvents.RaiseTransformation("DÖNME DOLAP!", "Ez geç! · 20 sn");
            Haptics.Impact(2);
        }

        private void SwapVisible(bool showForm)
        {
            _formShown = showForm;
            _form.gameObject.SetActive(showForm);
            _visual?.SetHidden(showForm);
        }

        // ---------------------------------------------------------------- çark

        private void UpdateRolling()
        {
            float dt = Time.deltaTime;
            _timer -= dt;
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 3f);

            // Gidiş yönüne dönüp yuvarlanır (yol / yarıçap).
            Vector3 velocity = _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            if (speed > 0.3f) _yaw = Mathf.LerpAngle(_yaw, Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg, 1f - Mathf.Exp(-12f * dt));
            float radius = Radius();
            _roll += speed * dt / Mathf.Max(0.1f, radius) * Mathf.Rad2Deg;
            PlaceForm(_roll);

            // Son saniyeler: yanıp söner.
            bool visible = _timer > WarnTime || Mathf.FloorToInt(_timer * BlinkHz * 2f) % 2 == 0;
            foreach (Renderer r in _formRenderers) if (r != null && r.enabled != visible) r.enabled = visible;

            _crushTimer -= dt;
            if (_crushTimer <= 0f)
            {
                _crushTimer = CrushInterval;
                Crush(radius);
            }

            _dustTimer -= dt;
            if (_dustTimer <= 0f && speed > 1f)
            {
                _dustTimer = DustInterval;
                Vector3 foot = transform.position;
                foot.y = 0.1f;
                CrushFx.Play(new Bounds(foot, Vector3.one * radius * 0.6f), DustA, DustB);
            }

            if (_timer <= 0f) BeginUnmorph();
        }

        private void Crush(float radius)
        {
            Vector3 center = transform.position + Vector3.up * radius * 0.5f;
            int count = Physics.OverlapSphereNonAlloc(center, radius * 0.95f, _hits, CrushMask, QueryTriggerInteraction.Collide);
            BlobTier reach = _growth != null ? (BlobTier)Mathf.Min((int)BlobTier.Colossal, (int)_growth.CurrentTier + 1) : BlobTier.Tiny;
            for (int i = 0; i < count; i++)
            {
                GameObject target = _hits[i].gameObject;
                if (!target.activeInHierarchy) continue;

                if (target.layer == 14)
                {
                    EnemyBase enemy = target.GetComponent<EnemyBase>(); // 0,1 sn'de bir, temas edenler
                    if (enemy == null || enemy.IsDead) continue;
                    if (enemy.IsBoss)
                    {
                        if (enemy.AirborneHeight <= 0f) enemy.TakeDamage(BossDamagePerTick);
                    }
                    else enemy.TakeDamage(CrushDamage);
                    continue;
                }

                IConsumable consumable = _hits[i].GetComponent<IConsumable>();
                if (consumable == null || consumable.Data == null || consumable.Data.IsHazard) continue;
                if (consumable.RequiredTier > reach) continue;
                _consumption?.ConsumeDirect(consumable, target);
            }
        }

        private void BeginUnmorph()
        {
            _phase = Phase.Unmorph;
            _timer = UnmorphDuration;
            _stepTimer = 0.1f;
            foreach (Renderer r in _formRenderers) if (r != null) r.enabled = true;
            if (_controller != null) _controller.FormSpeedFactor = 1f;
            if (_camera != null) _camera.CinematicZoom = 1f;
        }

        private void UpdateUnmorph()
        {
            float dt = Time.deltaTime;
            _timer -= dt;
            _stepTimer -= dt;
            PlaceForm(_roll);
            if (_stepTimer <= 0f)
            {
                _stepTimer = 0.08f;
                SwapVisible(!_formShown);
            }
            if (_timer > 0f) return;
            DeathFx.Play(FormBounds(), DustA, DustB, false);
            Haptics.Impact(1);
            Restore();
        }

        private void Restore()
        {
            _phase = Phase.None;
            IsActive = false;
            if (_form != null)
            {
                _form.gameObject.SetActive(false);
                foreach (Renderer r in _formRenderers) if (r != null) r.enabled = true;
            }
            _visual?.SetHidden(false);
            if (_controller != null) _controller.FormSpeedFactor = 1f;
            if (_camera != null) _camera.CinematicZoom = 1f;
        }

        // ---------------------------------------------------------------- yerleşim

        private float Radius() => transform.localScale.x * DiameterScale * 0.5f;

        // Çark merkezi yerden yarıçap kadar yukarıda; aks yatay, gidiş yönüne dik.
        private void PlaceForm(float roll)
        {
            if (_form == null) return;
            float radius = Radius();
            Vector3 position = transform.position;
            position.y = radius;
            _form.position = position;
            _form.rotation = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(roll, 0f, 0f);
            _form.localScale = Vector3.one * (radius * 2f / _nativeDiameter * (1f + _punch));
        }

        private Bounds FormBounds()
        {
            float radius = Radius();
            return new Bounds(transform.position + Vector3.up * radius, Vector3.one * radius * 2f);
        }
    }
}
