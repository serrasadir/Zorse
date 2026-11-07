using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    // Havai fişek roketi: her atış sıradaki renkte (gövde + iz), durduğu yerde (düşman, duvar ya da menzil sonunda
    // havada) o renkte kıvılcım patlaması. Model X_Firework (beyaz gövde boyanır), Mermi Modellerini Uygula kurar.
    public class FireworkProjectile : Projectile
    {
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.30f, 0.35f),  // kırmızı
            new Color(0.25f, 0.85f, 0.40f), // yeşil
            new Color(0.30f, 0.60f, 1f),  // mavi
            new Color(1f, 0.85f, 0.20f),  // sarı
            new Color(1f, 0.45f, 0.85f),  // pembe
            new Color(0.55f, 0.95f, 1f),  // camgöbeği
        };
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static int s_nextColor;

        private Renderer _model;
        private TrailRenderer _trailRenderer;
        private MaterialPropertyBlock _block;
        private Color _color;

        protected override void Awake()
        {
            base.Awake();
            _model = GetComponentInChildren<MeshRenderer>(true);
            _trailRenderer = GetComponent<TrailRenderer>();
            _block = new MaterialPropertyBlock();
        }

        protected override void OnLaunched()
        {
            _color = Colors[s_nextColor];
            s_nextColor = (s_nextColor + 1) % Colors.Length;
            if (_model != null)
            {
                _block.SetColor(BaseColorId, _color);
                _model.SetPropertyBlock(_block);
            }
            if (_trailRenderer != null)
            {
                _trailRenderer.startColor = Color.Lerp(_color, Color.white, 0.5f);
                _trailRenderer.endColor = _color;
            }
        }

        protected override void OnImpact() =>
            FireworkBurstFx.Burst(transform.position, _color, transform.localScale.x);
    }
}
