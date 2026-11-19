using System;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Entities.Blob
{
    // Evrim anı (~1.8 sn, ölçeksiz zaman; Kara Delik'in kendi sahnesinin genel hali): ağır çekim + kamera yaklaşır,
    // evrim renginde halka blob'a çöker → patlama anında evrim uygulanır (onBurst), renkli şok dalgası + kırıntı,
    // blob şişip yutkunur, kamera sarsılır → zaman ve kamera normale döner. Kart ekranı/duraklatmada bekler.
    // Halka: Kara Delik'in şok dalgası materyali (Tools → Blob.io → Vakum Efektini Kur), renk MaterialPropertyBlock'la.
    public class EvolutionMoment : MonoBehaviour
    {
        private const float SlowMo = 0.25f, Zoom = 0.85f;
        private const float ChargeEnd = 0.6f, BurstEnd = 1.25f, End = 1.85f;
        private const float RingStart = 3f, RingReach = 4f; // gövde yarıçapının katı
        private const string ShockMaterial = "FX/Vacuum/Mat_BlackHoleShock";
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Transform _blob;
        private BlobGrowth _growth;
        private BlobVisual _visual;
        private CameraController _camera;
        private Transform _ring;
        private MeshRenderer _ringRenderer;
        private MaterialPropertyBlock _block;
        private Color _color;
        private Action _onBurst, _onDone;
        private float _time = -1f;
        private bool _burst;

        public bool Busy => _time >= 0f;

        public void Play(Transform blob, Color color, Action onBurst, Action onDone)
        {
            _blob = blob;
            _growth = blob.GetComponent<BlobGrowth>();  // evrim anında, nadiren
            _visual = blob.GetComponent<BlobVisual>();
            if (_camera == null) _camera = FindAnyObjectByType<CameraController>();
            _color = color;
            _onBurst = onBurst;
            _onDone = onDone;
            _time = 0f;
            _burst = false;
            EnsureRing();
            if (_ring != null) _ring.gameObject.SetActive(true);
            Haptics.Impact(1);
        }

        // Run sıfırlanınca yarıda kalan an: zaman ve kamera geri, evrim uygulanmaz.
        public void Cancel()
        {
            if (_time < 0f) return;
            _time = -1f;
            _onBurst = _onDone = null;
            Time.timeScale = 1f;
            if (_camera != null) _camera.CinematicZoom = 1f;
            if (_ring != null) _ring.gameObject.SetActive(false);
        }

        private void EnsureRing()
        {
            if (_ring != null) return;
            Material mat = Resources.Load<Material>(ShockMaterial);
            if (mat == null) return;
            _ringRenderer = VacuumComponent.CreateGroundQuad("EvolutionRing", transform, mat); // tek seferlik
            _ring = _ringRenderer.transform;
            _block = new MaterialPropertyBlock();
            _ring.gameObject.SetActive(false);
        }

        private float BodyRadius => _blob != null ? _blob.localScale.x * 0.5f : 0.5f;

        private void Update()
        {
            if (_time < 0f) return;
            if (_blob == null) { Cancel(); return; }
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return; // kart/duraklatma: bekle
            _time += Time.unscaledDeltaTime;
            float t = _time;
            float body = BodyRadius;

            if (t < ChargeEnd)
            {
                // Toplanma: halka dışarıdan blob'a çöker, zaman yavaşlar, kamera yaklaşır.
                float u = t / ChargeEnd;
                Time.timeScale = Mathf.Lerp(1f, SlowMo, Mathf.Clamp01(u * 3f));
                if (_camera != null) _camera.CinematicZoom = Mathf.Lerp(1f, Zoom, u);
                SetRing(Mathf.Lerp(body * RingStart, body * 0.4f, u * u), Mathf.Clamp01(u * 2f));
                return;
            }

            if (!_burst)
            {
                _burst = true;
                Action burst = _onBurst;
                _onBurst = null;
                burst?.Invoke(); // evrim burada uygulanır (yazı + yeni görünüm)
                _growth?.PunchScale(0.35f);
                if (_visual != null) _visual.Gulp(0.45f);
                _camera?.Shake(0.3f, 0.5f);
                Haptics.Impact(2);
                Vector3 c = _blob.position + Vector3.up * body;
                FireworkBurstFx.Puff(c, _color, Color.Lerp(_color, Color.white, 0.6f), _blob.localScale.x, 28, 6f, -4f);
            }

            if (t < BurstEnd)
            {
                // Patlama: şok dalgası dışa yayılıp söner.
                float u = (t - ChargeEnd) / (BurstEnd - ChargeEnd);
                float ease = 1f - (1f - u) * (1f - u);
                SetRing(Mathf.Lerp(body, body * RingReach, ease), 1f - u);
                return;
            }

            // Toparlanma: zaman ve kamera normale döner.
            if (_ring != null) _ring.gameObject.SetActive(false);
            float k = Mathf.Clamp01((t - BurstEnd) / (End - BurstEnd));
            Time.timeScale = Mathf.Lerp(SlowMo, 1f, k * k);
            if (_camera != null) _camera.CinematicZoom = Mathf.Lerp(Zoom, 1f, k);
            if (t < End) return;

            Time.timeScale = 1f;
            if (_camera != null) _camera.CinematicZoom = 1f;
            _time = -1f;
            Action done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        private void SetRing(float radius, float alpha)
        {
            if (_ring == null) return;
            Vector3 p = _blob.position;
            _ring.position = new Vector3(p.x, 0.2f, p.z);
            _ring.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
            Color c = _color;
            c.a = Mathf.Clamp01(alpha);
            _block.SetColor(BaseColorId, c);
            _ringRenderer.SetPropertyBlock(_block);
        }
    }
}
