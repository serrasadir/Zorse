using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // Sindirim Gazı (aktif aura): blob çevresinde hep açık zehirli alan, alandaki TÜM düşmanlara vurur (Bahar, 2026-11-15).
    // Sv.3 gazdakiler yavaşlar · Sv.5 gazda ölenin
    // kütlesinin yarısı sana. Değerleri DigestGasEffect yazar; restart'ta GameManager siler.
    public class DigestGas : MonoBehaviour
    {
        public float Dps = 4f;
        public float Radius = 1f;          // gövde yüzeyinden
        public float Slow;                 // 0 = yok
        public float FeedFraction;         // gazda ölenin kütlesinden pay (0 = yok)

        private const float TickInterval = 0.25f;
        private const int EnemyMask = 1 << 14;
        private const float VerticalReach = 3f;
        private const string MaterialPath = "FX/SpeedTrail/Mat_SpeedTrail"; // köşe rengi + saydam
        private static readonly Color GasColor = new Color(0.5f, 0.95f, 0.3f, 0.28f);
        private const int Segments = 40;

        private readonly Collider[] _hits = new Collider[48];
        private BlobGrowth _growth;
        private float _bodyLocalRadius = 0.5f;
        private float _timer;
        private Transform _fx;
        private Mesh _mesh;
        private float _pulse;

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
            foreach (SphereCollider c in GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
            _timer = Random.Range(0f, TickInterval);
            CreateVisual();
        }

        private void OnDestroy()
        {
            if (_fx != null) Destroy(_fx.gameObject);
            if (_clouds != null) Destroy(_clouds.gameObject);
            if (_mesh != null) Destroy(_mesh);
        }

        private float EffectiveRadius => transform.localScale.x * _bodyLocalRadius + Radius;

        // Merkezi hafif, kenarı sönen yeşil disk (birim yarıçap; ölçekle büyür).
        private void CreateVisual()
        {
            Material material = Resources.Load<Material>(MaterialPath);
            if (material == null) return;
            _mesh = new Mesh { name = "GasDisc" };
            var verts = new Vector3[Segments + 1];
            var colors = new Color[Segments + 1];
            var tris = new int[Segments * 6]; // iki yüzlü (shader cull ayarından bağımsız)
            verts[0] = Vector3.zero;
            colors[0] = new Color(GasColor.r, GasColor.g, GasColor.b, GasColor.a * 0.5f);
            for (int i = 0; i < Segments; i++)
            {
                float a = i / (float)Segments * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                colors[i + 1] = GasColor;
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 1 == Segments ? 1 : i + 2;
                int o = Segments * 3 + i * 3;
                tris[o] = 0; tris[o + 1] = tris[i * 3 + 2]; tris[o + 2] = i + 1;
            }
            _mesh.vertices = verts;
            _mesh.colors = colors;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();

            var go = new GameObject("DigestGas");
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _fx = go.transform;

            // Gaz bulutları (Blender: build_skill_fx.py → Resources/FX/Gas/FX_Gas, yarıçap 1, renk/saydamlık köşe renginde).
            Mesh clouds = Resources.Load<Mesh>(CloudMeshPath);
            if (clouds == null) return;
            var cloudGo = new GameObject("DigestGasClouds");
            cloudGo.AddComponent<MeshFilter>().sharedMesh = clouds;
            var cr = cloudGo.AddComponent<MeshRenderer>();
            cr.sharedMaterial = material;
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cr.receiveShadows = false;
            _clouds = cloudGo.transform;
        }

        private const string CloudMeshPath = "FX/Gas/FX_Gas";
        private const float CloudSpin = 25f; // derece/sn
        private Transform _clouds;
        private float _spin;

        private void LateUpdate()
        {
            if (_fx == null) return;
            _pulse += Time.deltaTime * 2f;
            float d = EffectiveRadius * (1f + Mathf.Sin(_pulse) * 0.03f);
            Vector3 p = transform.position;
            _fx.position = new Vector3(p.x, 0.18f, p.z);
            _fx.localScale = new Vector3(d, 1f, d);
            if (_clouds == null) return;
            _spin += CloudSpin * Time.deltaTime;
            _clouds.position = new Vector3(p.x, 0.12f, p.z);
            _clouds.rotation = Quaternion.Euler(0f, _spin, 0f);
            // Bulutlar yarıçapla yayılır, boyu daha yavaş büyür (dev blob'da gaz kule gibi olmasın).
            float h = Mathf.Sqrt(d) * (1f + Mathf.Sin(_pulse * 1.3f) * 0.06f);
            _clouds.localScale = new Vector3(d, h, d);
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = TickInterval;

            Vector3 origin = transform.position;
            float damage = Dps * TickInterval * BlobSkills.WeaponDamageScale;
            int count = Physics.OverlapCapsuleNonAlloc(origin, origin + Vector3.up * VerticalReach, EffectiveRadius, _hits, EnemyMask);
            for (int i = 0; i < count; i++)
            {
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                if (Slow > 0f) enemy.ApplySlow(1f - Slow, TickInterval + 0.15f);
                enemy.TakeDamage(damage);
                if (FeedFraction > 0f && enemy.IsDead && enemy.Data != null && _growth != null)
                    _growth.AddMass(enemy.Data.MassReward * FeedFraction);
            }
        }
    }
}
