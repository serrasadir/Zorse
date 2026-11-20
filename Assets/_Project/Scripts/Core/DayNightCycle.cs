using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.Core
{
    // Run'a bağlı gün (Bahar, 2026-11-20): her run sabah başlar, süreyle öğle → ikindi → gün batımı → gece. Final boss gelince
    // gece hızla gelir (en geç 17. dk zaten geceye yakın). Güneşin açısı/rengi/gücü ve üç renkli ortam ışığı yumuşakça geçer;
    // gece PaletteAO shader'ında pencereler ve ateş parlar (_NightGlow global). İki haritada da kendiliğinden kurulur
    // (sahnede Directional Light varsa). Ortam yansıması pahalı: DynamicGI.UpdateEnvironment 0.5 sn'de bir.
    public class DayNightCycle : MonoBehaviour
    {
        private struct Key
        {
            public float Time;          // sn
            public float Elevation;     // güneşin yükseklik açısı (derece)
            public Color Sun;
            public float Intensity;
            public Color Sky, Equator, Ground;
            public float Glow;          // gece pencere/ateş parlaması 0..1
        }

        private static readonly Key[] Keys =
        {
            new Key { Time = 0f,    Elevation = 34f, Sun = new Color(1f, 0.86f, 0.68f), Intensity = 1.0f,
                      Sky = new Color(0.95f, 0.88f, 0.78f), Equator = new Color(0.76f, 0.70f, 0.66f), Ground = new Color(0.44f, 0.45f, 0.55f) },
            new Key { Time = 360f,  Elevation = 58f, Sun = new Color(1f, 0.96f, 0.88f), Intensity = 1.2f,
                      Sky = new Color(1f, 0.96f, 0.88f), Equator = new Color(0.82f, 0.78f, 0.72f), Ground = new Color(0.48f, 0.50f, 0.58f) },
            new Key { Time = 720f,  Elevation = 44f, Sun = new Color(1f, 0.9f, 0.74f), Intensity = 1.15f,
                      Sky = new Color(1f, 0.93f, 0.82f), Equator = new Color(0.80f, 0.74f, 0.68f), Ground = new Color(0.46f, 0.47f, 0.56f) },
            new Key { Time = 930f,  Elevation = 16f, Sun = new Color(1f, 0.58f, 0.36f), Intensity = 0.95f,
                      Sky = new Color(1f, 0.70f, 0.55f), Equator = new Color(0.72f, 0.50f, 0.46f), Ground = new Color(0.34f, 0.29f, 0.42f), Glow = 0.35f },
            new Key { Time = 1050f, Elevation = 52f, Sun = new Color(0.55f, 0.66f, 1f), Intensity = 0.42f,
                      Sky = new Color(0.30f, 0.36f, 0.62f), Equator = new Color(0.20f, 0.22f, 0.36f), Ground = new Color(0.10f, 0.10f, 0.18f), Glow = 1f },
        };

        private const float FinalBossTime = 1050f;       // final boss gelince bu ana doğru hızlanır
        private const float CatchUpSpeed = 40f;          // sn/sn (gün batımından geceye ~3 sn)
        private const float AmbientInterval = 0.5f;
        private static readonly int NightGlowId = Shader.PropertyToID("_NightGlow");

        private Light _sun;
        private float _azimuth;
        private float _clock;       // gösterilen zaman (sn)
        private float _target;      // run süresi (ya da final boss anı)
        private bool _finalBoss;
        private float _ambientTimer;
        private float _debugOffset;  // test: DevCheats I tuşu günü ileri sarar

        public static DayNightCycle Instance { get; private set; }

        // Test (Editor I tuşu): günü sn kadar ileri al.
        public void DebugAdvance(float seconds)
        {
            _debugOffset += seconds;
            _target += seconds;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<DayNightCycle>() != null) return;
            foreach (Light l in FindObjectsByType<Light>())
                if (l.type == LightType.Directional)
                {
                    new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
                    return;
                }
        }

        private void Awake()
        {
            Instance = this;
            foreach (Light l in FindObjectsByType<Light>())
                if (l.type == LightType.Directional) { _sun = l; break; }
            if (_sun != null) _azimuth = _sun.transform.eulerAngles.y;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Apply(0f, force: true);
        }

        private void OnEnable()
        {
            GameEvents.OnSurvivalTimeUpdated += OnTime;
            GameEvents.OnCharacterSelected += OnRunStarted;
            GameEvents.OnBossSpawned += OnBoss;
        }

        private void OnDisable()
        {
            GameEvents.OnSurvivalTimeUpdated -= OnTime;
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnBossSpawned -= OnBoss;
            Shader.SetGlobalFloat(NightGlowId, 0f);
        }

        private void OnRunStarted(Data.CharacterData _)
        {
            _finalBoss = false;
            _debugOffset = 0f;
            _target = 0f;
            _clock = 0f;
            Apply(0f, force: true);
        }

        private void OnTime(float t)
        {
            if (!_finalBoss) _target = t + _debugOffset;
        }

        private void OnBoss(string name, bool isFinal)
        {
            if (!isFinal) return;
            _finalBoss = true;
            _target = Mathf.Max(_target, FinalBossTime);
        }

        private void Update()
        {
            // Zaman ileri akar; final boss'ta hedefe hızla yetişir (geri gitmez, run başı hariç).
            float step = (_finalBoss ? CatchUpSpeed : 4f) * Time.deltaTime; // normalde süreyi rahat izler (1 sn/sn)
            _clock = Mathf.MoveTowards(_clock, _target, step);
            Apply(_clock, force: false);
        }

        private void Apply(float t, bool force)
        {
            int i = 0;
            while (i < Keys.Length - 1 && t >= Keys[i + 1].Time) i++;
            Key a = Keys[i], b = Keys[Mathf.Min(i + 1, Keys.Length - 1)];
            float k = b.Time > a.Time ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.Time, b.Time, t)) : 0f;

            if (_sun != null)
            {
                _sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(a.Elevation, b.Elevation, k), _azimuth, 0f);
                _sun.color = Color.Lerp(a.Sun, b.Sun, k);
                _sun.intensity = Mathf.Lerp(a.Intensity, b.Intensity, k);
            }
            Shader.SetGlobalFloat(NightGlowId, Mathf.Lerp(a.Glow, b.Glow, k));

            _ambientTimer -= Time.unscaledDeltaTime;
            if (!force && _ambientTimer > 0f) return;
            _ambientTimer = AmbientInterval;
            RenderSettings.ambientSkyColor = Color.Lerp(a.Sky, b.Sky, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.Equator, b.Equator, k);
            RenderSettings.ambientGroundColor = Color.Lerp(a.Ground, b.Ground, k);
            DynamicGI.UpdateEnvironment();
        }
    }
}
