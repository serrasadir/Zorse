using UnityEngine;
using UnityEngine.UI;

namespace BlobSurvivor.UI
{
    // Yan ekran desteği: UI dik ekrana göre çizildi (1080 genişlik). Dikte ölçek genişlikten, yanda yükseklikten gelir:
    // yan ekranda canvas LandscapeHeight birim yüksekliğinde olur, UI ekranı kaplamaz. Yön değişince yeniden ayarlanır.
    // Run başında 1080×1920 / ScaleWithScreenSize her CanvasScaler'a kendiliğinden eklenir (sahne kurulumu gerekmez).
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveCanvasScaler : MonoBehaviour
    {
        public const float PortraitWidth = 1080f, PortraitHeight = 1920f;
        public const float LandscapeHeight = 1500f; // yan ekranda UI dik ekranın ~%72'si boyunda
        private const float CheckInterval = 0.25f;

        private CanvasScaler _scaler;
        private bool _landscape, _applied;
        private float _timer;

        private static bool s_sceneHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnSceneCanvases()
        {
            // Harita değişince (MapSelection.Switch → LoadScene) yeni sahnede de kurulsun: AfterSceneLoad sadece ilk sahnede çalışır.
            if (!s_sceneHooked) { s_sceneHooked = true; UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => InstallOnSceneCanvases(); }
            foreach (CanvasScaler s in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
                Attach(s);
        }

        public static void Attach(CanvasScaler s)
        {
            if (s.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return;
            if (!Mathf.Approximately(s.referenceResolution.x, PortraitWidth)) return;
            if (s.GetComponent<AdaptiveCanvasScaler>() == null) s.gameObject.AddComponent<AdaptiveCanvasScaler>();
        }

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;
            Apply();
        }

        private void Apply()
        {
            bool landscape = Screen.width > Screen.height;
            if (_applied && landscape == _landscape) return;
            _applied = true;
            _landscape = landscape;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            if (landscape)
            {
                _scaler.referenceResolution = new Vector2(PortraitHeight, LandscapeHeight);
                _scaler.matchWidthOrHeight = 1f;
            }
            else
            {
                _scaler.referenceResolution = new Vector2(PortraitWidth, PortraitHeight);
                _scaler.matchWidthOrHeight = 0f;
            }
        }
    }
}
