using UnityEngine;

namespace BlobSurvivor.Core
{
    // Vuruş durması: ağır bir düşman devrilince oyun çok kısa donar (his). Sadece oyun normal hızda akarken;
    // duraklatma, kart ekranı ve ağır çekimler (tekme, Kara Delik) timeScale'i kendisi yönetir, onlara dokunulmaz.
    public class HitStop : MonoBehaviour
    {
        private const float StopScale = 0.03f;

        private static HitStop s_instance;
        private float _until = -1f;

        public static void Play(float duration)
        {
            if (!Mathf.Approximately(Time.timeScale, 1f)) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (s_instance == null)
            {
                var go = new GameObject("HitStop") { hideFlags = HideFlags.HideInHierarchy };
                s_instance = go.AddComponent<HitStop>(); // tek seferlik kurulum
            }
            s_instance._until = Time.unscaledTime + duration;
            Time.timeScale = StopScale;
        }

        private void Update()
        {
            if (_until < 0f || Time.unscaledTime < _until) return;
            _until = -1f;
            if (Mathf.Approximately(Time.timeScale, StopScale)) Time.timeScale = 1f; // başkası değiştirdiyse bırak
        }
    }
}
