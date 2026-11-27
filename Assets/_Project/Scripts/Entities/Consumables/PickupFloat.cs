using UnityEngine;

namespace BlobSurvivor.Entities
{
    // Yerdeki toplanabilir yiyecekler (Bahar, 2026-11-21): yerden biraz yüksekte süzülür, hafif zıplar, döner,
    // büyüyüp küçülür — "alınabilir" olduğu okunsun. Sadece model (ilk child) oynar: collider ve havuz konumu yerinde kalır.
    // ConsumableSpawner ilk çıkarışta ekler (örnek başına tek seferlik). Ekran dışındayken çalışmaz.
    public class PickupFloat : MonoBehaviour
    {
        private const float Hover = 0.22f;      // yerden yükseklik
        private const float Bounce = 0.05f;     // zıplama yüksekliği (Serra: çok zıplıyor/dönüyordu)
        private const float BounceRate = 2.4f;  // rad/sn
        private const float Pulse = 0.03f;      // büyüme-küçülme oranı
        private const float SpinSpeed = 25f;    // derece/sn

        private Transform _model;
        private Renderer _renderer;
        private Vector3 _basePos;
        private Vector3 _baseScale;
        private Quaternion _baseRot;
        private float _phase;

        private void Awake()
        {
            if (transform.childCount == 0) { enabled = false; return; }
            _model = transform.GetChild(0);
            _renderer = _model.GetComponentInChildren<Renderer>();
            _basePos = _model.localPosition;
            _baseScale = _model.localScale;
            _baseRot = _model.localRotation;
        }

        private void OnEnable()
        {
            _phase = Random.Range(0f, Mathf.PI * 2f);
            if (_model != null) Apply(0f);
        }

        private void OnDisable()
        {
            if (_model == null) return;
            _model.localPosition = _basePos;
            _model.localScale = _baseScale;
            _model.localRotation = _baseRot;
        }

        private void Update()
        {
            if (_renderer != null && !_renderer.isVisible) return;
            Apply(Time.time);
        }

        private void Apply(float t)
        {
            float b = Mathf.Abs(Mathf.Sin(t * BounceRate + _phase));          // zıplama: yere değmeden sekme
            _model.localPosition = _basePos + Vector3.up * (Hover + b * Bounce);
            float s = 1f + Mathf.Sin(t * BounceRate * 0.5f + _phase) * Pulse;
            _model.localScale = _baseScale * s;
            _model.localRotation = Quaternion.Euler(0f, (t * SpinSpeed + _phase * 57f) % 360f, 0f) * _baseRot;
        }
    }
}
