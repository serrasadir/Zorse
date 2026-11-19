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

        // ---------------------------------------------------------------- Bayram Gecesi (evrim): misket

        private const int EnemyMask = 1 << 14;
        private const float MisketScale = 0.55f;
        private const float MisketSearchRadius = 6f;
        private static readonly Collider[] s_near = new Collider[16];

        private int _misketCount;    // 0 = bölünmez (misketin kendisi de)
        private float _misketDamage;

        // Silah her atışta yazar (havuzdan gelen mermi önceki ayarı taşımasın).
        public void SetCluster(int count, float damage)
        {
            _misketCount = count;
            _misketDamage = damage;
        }

        protected override void OnHitEnemy(BlobSurvivor.Entities.Enemies.EnemyBase enemy)
        {
            if (_misketCount <= 0 || Pool == null) return;
            Vector3 origin = transform.position;
            int found = Physics.OverlapSphereNonAlloc(origin, MisketSearchRadius, s_near, EnemyMask, QueryTriggerInteraction.Ignore);
            int target = 0;
            float spin = Random.Range(0f, 360f);
            for (int i = 0; i < _misketCount; i++)
            {
                // Sıradaki komşu düşman (vurulan hariç); kalmadıysa çember üstünde eşit aralıklı yön.
                Vector3 dir = Vector3.zero;
                while (target < found)
                {
                    Collider c = s_near[target++];
                    if (c.gameObject == enemy.gameObject) continue;
                    dir = c.bounds.center - origin;
                    break;
                }
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f)
                    dir = Quaternion.Euler(0f, spin + i * 360f / _misketCount, 0f) * Vector3.forward;

                Projectile misket = Pool.Get(origin, Quaternion.LookRotation(dir));
                misket.SetVisualScale(VisualScale * MisketScale);
                misket.Launch(Pool, dir, _misketDamage);
                if (misket is FireworkProjectile fp) fp.SetCluster(0, 0f);
            }
        }
    }
}
