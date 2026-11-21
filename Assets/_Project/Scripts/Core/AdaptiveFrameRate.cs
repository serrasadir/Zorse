using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlobSurvivor.Core
{
    // 60 FPS, kaldıramayan telefonda 30 (Bahar, 2026-11-21). Sadece oynarken ölçer (yükleme/menü sayılmaz):
    // 60 hedefindeyken 1 sn'lik pencerelerin ortalaması art arda LowWindows kez Threshold'un altındaysa 30'a iner ve telefona yazar
    // (ısınıp yavaşlayan telefon da run içinde iner). 45'te bırakmaz: 60 Hz ekranda düzensiz takılır, sabit 30 daha akıcı.
    // 30'a inen telefon RetryAfterRuns run sonra bir kez daha 60'ı dener (o an arka planda başka iş olabilir).
    public class AdaptiveFrameRate : MonoBehaviour
    {
        private const string CapKey = "fps_cap";
        private const string RunsKey = "fps_runs_at_30";
        private const float Threshold = 52f;   // 45–50 bandında kalan telefon (A06: ~45) sabit 30 daha akıcı
        private const int LowWindows = 8;        // ~8 sn sürekli düşük
        private const int RetryAfterRuns = 5;

        private float _windowTime;
        private int _windowFrames;
        private int _lowStreak;

        public static int Cap { get; private set; } = 60;
        // Görüntü kademesi (RP asset varsayılanı hafif: MSAA 2x, %85). Zayıf telefon (30 FPS): MSAA 2x, %80, kısa gölge.
        // 60'ı rahat tutan telefon (HighWindows sn boyunca ≥ HighFps) kalıcı olarak MSAA 4x, %90'a yükselir.
        public static bool LowQuality => Cap == 30;
        private const string HighKey = "quality_high";
        private const float HighFps = 57f;
        private const int HighWindows = 15;
        private const float LowRenderScale = 0.8f;
        private const int HighMsaa = 4;
        private const float HighRenderScale = 0.9f;
        private int _highStreak;

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

        private static int s_msaa = -1;
        private static float s_scale = -1f;

        // RP asset'i çalışma anında değiştirir. Editörde dokunmaz: Play'de asset'e yazılan değer kalıcı olurdu.
        private static void ApplyQuality(bool low)
        {
            if (Application.isEditor) return;
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)) return;
            if (s_msaa < 0) { s_msaa = urp.msaaSampleCount; s_scale = urp.renderScale; }   // asset varsayılanı (hafif)
            bool high = !low && PlayerPrefs.GetInt(HighKey, 0) == 1;
            urp.msaaSampleCount = high ? HighMsaa : s_msaa;
            urp.renderScale = low ? Mathf.Min(LowRenderScale, s_scale) : high ? HighRenderScale : s_scale;
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
                PlayerPrefs.SetInt(HighKey, 0);               // yükseltilmiş kalite de geri alınır
                Apply(30, save: true);
                PlayerPrefs.SetInt(RunsKey, 0);
                return;
            }
            // Rahat 60: görüntüyü bir kez yükselt (sonraki açılışlarda da). Yükselttikten sonra düşerse yukarıdaki iner.
            _highStreak = fps >= HighFps ? _highStreak + 1 : 0;
            if (_highStreak >= HighWindows && PlayerPrefs.GetInt(HighKey, 0) == 0)
            {
                PlayerPrefs.SetInt(HighKey, 1);
                PlayerPrefs.Save();
                ApplyQuality(false);
            }
        }

        private static void Apply(int cap, bool save)
        {
            Cap = cap;
            Application.targetFrameRate = cap;
            ApplyQuality(cap == 30);
            if (!save) return;
            PlayerPrefs.SetInt(CapKey, cap);
            PlayerPrefs.Save();
        }
    }
}
