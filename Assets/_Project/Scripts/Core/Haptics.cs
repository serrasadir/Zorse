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
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private const long AndroidDurationMs = 15;
        private const int AndroidAmplitude = 50; // 1-255
        private static AndroidJavaObject _vibrator;
        private static AndroidJavaClass _effectClass;
        private static bool _androidInitDone;
        private static bool _androidSupportsAmplitude;
#endif

        public static void LightTap()
        {
            float now = Time.unscaledTime;
            if (now - _lastTime < MinInterval) return;
            _lastTime = now;

#if UNITY_IOS && !UNITY_EDITOR
            BlobHaptics_LightImpact();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidLightTap();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void AndroidLightTap()
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
                    using (AndroidJavaObject effect = _effectClass.CallStatic<AndroidJavaObject>("createOneShot", AndroidDurationMs, AndroidAmplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", AndroidDurationMs);
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
