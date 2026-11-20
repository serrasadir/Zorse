using UnityEngine;

namespace BlobSurvivor.Core
{
    // 60 FPS, kaldıramayan telefonda 30 (Bahar, 2026-11-21). Sadece oynarken ölçer (yükleme/menü sayılmaz):
    // 60 hedefindeyken 1 sn'lik pencerelerin ortalaması art arda LowWindows kez 45'in altındaysa 30'a iner ve telefona yazar
    // (ısınıp yavaşlayan telefon da run içinde iner). 45'te bırakmaz: 60 Hz ekranda düzensiz takılır, sabit 30 daha akıcı.
    // 30'a inen telefon RetryAfterRuns run sonra bir kez daha 60'ı dener (o an arka planda başka iş olabilir).
    public class AdaptiveFrameRate : MonoBehaviour
    {
        private const string CapKey = "fps_cap";
        private const string RunsKey = "fps_runs_at_30";
        private const float Threshold = 45f;
        private const int LowWindows = 8;        // ~8 sn sürekli düşük
        private const int RetryAfterRuns = 5;

        private float _windowTime;
        private int _windowFrames;
        private int _lowStreak;

        public static int Cap { get; private set; } = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("AdaptiveFrameRate");
            DontDestroyOnLoad(go);
            go.AddComponent<AdaptiveFrameRate>();
        }

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Apply(PlayerPrefs.GetInt(CapKey, 60) == 30 ? 30 : 60, save: false);
        }

        private void OnEnable() => GameEvents.OnCharacterSelected += OnRunStarted;
        private void OnDisable() => GameEvents.OnCharacterSelected -= OnRunStarted;

        private void OnRunStarted(Data.CharacterData _)
        {
            _windowTime = 0f; _windowFrames = 0; _lowStreak = 0;
            if (Cap != 30) return;
            int runs = PlayerPrefs.GetInt(RunsKey, 0) + 1;
            if (runs >= RetryAfterRuns)
            {
                runs = 0;
                Apply(60, save: true); // tekrar dene; yine düşükse ölçüm 30'a geri indirir
            }
            PlayerPrefs.SetInt(RunsKey, runs);
        }

        private void Update()
        {
            if (Cap != 60) return;
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying)
            {
                _windowTime = 0f; _windowFrames = 0;
                return;
            }
            _windowTime += Time.unscaledDeltaTime;
            _windowFrames++;
            if (_windowTime < 1f) return;
            float fps = _windowFrames / _windowTime;
            _windowTime = 0f; _windowFrames = 0;
            _lowStreak = fps < Threshold ? _lowStreak + 1 : 0;
            if (_lowStreak >= LowWindows)
            {
                _lowStreak = 0;
                Apply(30, save: true);
                PlayerPrefs.SetInt(RunsKey, 0);
            }
        }

        private static void Apply(int cap, bool save)
        {
            Cap = cap;
            Application.targetFrameRate = cap;
            if (!save) return;
            PlayerPrefs.SetInt(CapKey, cap);
            PlayerPrefs.Save();
        }
    }
}
