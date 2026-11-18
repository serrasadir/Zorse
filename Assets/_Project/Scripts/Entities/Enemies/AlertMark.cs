using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Düşman blob'u ilk görünce başının üstünde kısa "!" (zıplayarak belirir, kameraya döner, söner).
    // İşaret child'ı editörde kurulur (EnemyVariantSetup.AddAlertMark); runtime'da üretim yok.
    public class AlertMark : MonoBehaviour
    {
        [SerializeField] private Transform _mark;

        private const float PopTime = 0.18f;
        private const float HoldTime = 0.9f;
        private const float FadeTime = 0.2f;

        private Vector3 _baseScale;
        private float _t = -1f;
        private Transform _camera;

        private void Awake()
        {
            if (_mark == null) { enabled = false; return; }
            _baseScale = _mark.localScale;
            _mark.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _t = -1f;
            if (_mark != null) _mark.gameObject.SetActive(false);
        }

        public void Play()
        {
            if (_mark == null) return;
            if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
            _t = 0f;
            _mark.gameObject.SetActive(true);
            Apply(0f);
        }

        private void LateUpdate()
        {
            if (_t < 0f) return;
            _t += Time.deltaTime;
            if (_t >= PopTime + HoldTime + FadeTime)
            {
                _t = -1f;
                _mark.gameObject.SetActive(false);
                return;
            }
            Apply(_t);
        }

        private void Apply(float t)
        {
            float s;
            if (t < PopTime)
            {
                float k = t / PopTime; // geri taşmalı büyüme
                s = 1f + 2.7f * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f);
            }
            else if (t < PopTime + HoldTime) s = 1f;
            else s = 1f - (t - PopTime - HoldTime) / FadeTime;
            _mark.localScale = _baseScale * Mathf.Max(0f, s);
            if (_camera != null) _mark.rotation = _camera.rotation;
        }
    }
}
