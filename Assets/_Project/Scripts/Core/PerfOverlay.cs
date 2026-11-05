using BlobSurvivor.Entities.Blob;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlobSurvivor.Core
{
    // 2026-11-04: telefonda performans ölçümü (şehir kiti v2 denemesi). Gizli başlar.
    //   Aç/kapa: ekrana 3 parmakla dokun (Editor'da F3). Durum PlayerPrefs'te kalır.
    //   Gösterir: ortalama FPS (0.5 sn), son 2 sn'nin en kötü karesi (ms, takılma), hedef FPS.
    //   "+500 kütle" düğmesi: Giant'a hızlı çıkıp en kötü anı (kamera en uzak, en çok bina ekranda) ölçmek için.
    // YAYIN ÖNCESİ: Enabled = false (oyunculara hile düğmesi gitmesin). Kapalıyken hiç oluşmaz, maliyeti sıfır.
    public class PerfOverlay : MonoBehaviour
    {
        public static readonly bool Enabled = true; // const değil: sabit dal "unreachable code" uyarısı veriyordu

        private const string PrefKey = "perf_overlay";
        private const float AvgWindow = 0.5f;
        private const float WorstWindow = 2f;

        private bool _visible;
        private bool _wasThreeFingers;
        private float _accTime;
        private int _accFrames;
        private float _fps;
        private float _worstMs, _worstAccMs, _worstTimer;
        private GUIStyle _style, _button;
        private BlobGrowth _growth;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Enabled) return;
            GameObject go = new GameObject("PerfOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfOverlay>();
        }

        private void Awake()
        {
            _visible = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        }

        private void Update()
        {
            HandleToggle();

            float dt = Time.unscaledDeltaTime;
            _accTime += dt;
            _accFrames++;
            if (_accTime >= AvgWindow)
            {
                _fps = _accFrames / _accTime;
                _accTime = 0f;
                _accFrames = 0;
            }
            _worstAccMs = Mathf.Max(_worstAccMs, dt * 1000f);
            _worstTimer += dt;
            if (_worstTimer >= WorstWindow)
            {
                _worstMs = _worstAccMs;
                _worstAccMs = 0f;
                _worstTimer = 0f;
            }
        }

        private void HandleToggle()
        {
            bool toggle = false;
            Touchscreen touch = Touchscreen.current;
            if (touch != null)
            {
                int pressed = 0;
                foreach (var t in touch.touches)
                    if (t.press.isPressed) pressed++;
                bool three = pressed >= 3;
                if (three && !_wasThreeFingers) toggle = true;
                _wasThreeFingers = three;
            }
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) toggle = true;
            if (!toggle) return;
            _visible = !_visible;
            PlayerPrefs.SetInt(PrefKey, _visible ? 1 : 0);
        }

        private void OnGUI()
        {
            if (!_visible) return;
            float s = Screen.height / 900f;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                _button = new GUIStyle(GUI.skin.button);
            }
            _style.fontSize = Mathf.RoundToInt(18 * s);
            _button.fontSize = Mathf.RoundToInt(16 * s);

            int target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 30; // iOS varsayılanı 30
            _style.normal.textColor = _fps >= target * 0.95f ? Color.green : _fps >= target * 0.8f ? Color.yellow : Color.red;
            Rect r = new Rect(10 * s, 60 * s, 320 * s, 90 * s);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 8 * s, r.y + 4 * s, r.width, 26 * s), $"FPS {_fps:F1} / hedef {target}", _style);
            _style.normal.textColor = _worstMs <= 1000f / target * 1.5f ? Color.white : Color.yellow;
            GUI.Label(new Rect(r.x + 8 * s, r.y + 30 * s, r.width, 26 * s), $"en kötü kare {_worstMs:F0} ms (2 sn)", _style);

            if (_growth == null) _growth = FindAnyObjectByType<BlobGrowth>();
            if (_growth != null)
            {
                _style.normal.textColor = Color.white;
                GUI.Label(new Rect(r.x + 8 * s, r.y + 56 * s, r.width, 26 * s), $"kütle {_growth.CurrentMass:F0} · {_growth.CurrentTier}", _style);
                if (GUI.Button(new Rect(r.x, r.yMax + 6 * s, 150 * s, 40 * s), "+500 kütle", _button))
                    _growth.DebugAddMassWithoutXP(500f);
            }
        }
    }
}
