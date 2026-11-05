using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BlobSurvivor.Core
{
    // 2026-11-02 (Serra: "yediğimde mesaj gelmiş gibi titriyor"): Handheld.Vibrate() ~400ms tam bildirim titreşimi.
    // Yerine hafif dokunuş:
    //  - iOS: UIImpactFeedbackGenerator (Light) — Assets/Plugins/iOS/BlobHaptics.mm
    //  - Android: 15ms düşük genlikli tek atım (API 26+), eski cihazlarda Handheld.Vibrate yedeği
    //    (bu referans aynı zamanda Unity'nin VIBRATE iznini manifest'e eklemesini sağlar).
    // Arka arkaya yemelerde titreşim birbirine eklenip uğultuya dönmesin diye MinInterval ile seyreltilir.
    public static class Haptics
    {
        private const float MinInterval = 0.08f;
        private static float _lastTime = -1f;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void BlobHaptics_LightImpact();
        [DllImport("__Internal")] private static extern void BlobHaptics_Impact(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private const long AndroidDurationMs = 15;
        private const int AndroidAmplitude = 50; // 1-255
        private static AndroidJavaObject _vibrator;
        private static AndroidJavaClass _effectClass;
        private static bool _androidInitDone;
        private static bool _androidSupportsAmplitude;
#endif

        public static void LightTap() => Impact(0);

        // 0 = hafif (her yeme), 1 = orta (Large/Giant lokma), 2 = güçlü (Dev lokma). Güçlü olan seyreltmeyi deler.
        public static void Impact(int strength)
        {
            float now = Time.unscaledTime;
            if (strength == 0 && now - _lastTime < MinInterval) return;
            _lastTime = now;

#if UNITY_IOS && !UNITY_EDITOR
            if (strength == 0) BlobHaptics_LightImpact();
            else BlobHaptics_Impact(strength);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidTap(strength);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void AndroidTap(int strength)
        {
            try
            {
                if (!_androidInitDone)
                {
                    _androidInitDone = true;
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        _androidSupportsAmplitude = version.GetStatic<int>("SDK_INT") >= 26;
                    if (_androidSupportsAmplitude)
                        _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }

                if (_vibrator == null) return;

                if (_androidSupportsAmplitude)
                {
                    long ms = AndroidDurationMs + strength * 15;
                    int amp = Mathf.Min(255, AndroidAmplitude + strength * 80);
                    using (AndroidJavaObject effect = _effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amp))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", AndroidDurationMs + strength * 15);
                }
            }
            catch (System.Exception)
            {
                _vibrator = null;
                Handheld.Vibrate();
            }
        }
#endif
    }
}
