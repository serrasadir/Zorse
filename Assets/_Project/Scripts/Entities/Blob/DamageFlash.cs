using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-03: blob hasar alınca kısa bir kırmızı parlama. Materyali değiştirmez (paylaşılan asset bozulmasın),
    // sadece bu renderer'a özel MaterialPropertyBlock ile renk geçersiz kılınır, bitince kaldırılır.
    // BlobHealth.Awake ekler — sahne kurulumu gerekmez.
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Color _flashColor = new Color(1f, 0.15f, 0.15f, 1f);
        [SerializeField] private float _duration = 0.15f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private BlobHealth _health;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private int _colorProperty;
        private Color _baseColor = Color.white;
        private float _time = -1f;

        private void Awake()
        {
            _health = GetComponent<BlobHealth>();
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();

            Material material = _renderer != null ? _renderer.sharedMaterial : null;
            if (material != null)
            {
                _colorProperty = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
                if (material.HasProperty(_colorProperty)) _baseColor = material.GetColor(_colorProperty);
            }
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= HandleDamaged;
            Clear();
        }

        private void HandleDamaged(float _) => _time = 0f;

        private void Update()
        {
            if (_time < 0f || _renderer == null) return;

            _time += Time.deltaTime;
            float k = 1f - _time / _duration;
            if (k <= 0f)
            {
                Clear();
                return;
            }

            _block.SetColor(_colorProperty, Color.Lerp(_baseColor, _flashColor, k));
            _renderer.SetPropertyBlock(_block);
        }

        private void Clear()
        {
            _time = -1f;
            if (_renderer != null) _renderer.SetPropertyBlock(null);
        }
    }
}
