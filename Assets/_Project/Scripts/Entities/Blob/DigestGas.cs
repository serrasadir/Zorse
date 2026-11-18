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
        // Görsel: sadece alanın KENARINDA dönen gaz bulutları (Blender: build_skill_fx.py → Resources/FX/Gas/FX_Gas,
        // yarıçap 1). İçi açık kalır: blob'un çevresini Vakum kasırgası doldurur, düşmanlar görünür kalsın.
        private const string CloudMeshPath = "FX/Gas/FX_Gas";
        private const float CloudSpin = 25f; // derece/sn

        private readonly Collider[] _hits = new Collider[48];
        private BlobGrowth _growth;
        private float _bodyLocalRadius = 0.5f;
        private float _timer;
        private Transform _clouds;
        private float _spin, _pulse;

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
            if (_clouds != null) Destroy(_clouds.gameObject);
        }

        private float EffectiveRadius => transform.localScale.x * _bodyLocalRadius + Radius;

        private void CreateVisual()
        {
            Material material = Resources.Load<Material>(MaterialPath);
            Mesh clouds = Resources.Load<Mesh>(CloudMeshPath);
            if (material == null || clouds == null) return;
            var go = new GameObject("DigestGasClouds");
            go.AddComponent<MeshFilter>().sharedMesh = clouds;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _clouds = go.transform;
        }

        private void LateUpdate()
        {
            if (_clouds == null) return;
            _pulse += Time.deltaTime * 2f;
            _spin += CloudSpin * Time.deltaTime;
            float d = EffectiveRadius * (1f + Mathf.Sin(_pulse) * 0.03f);
            Vector3 p = transform.position;
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
                if (enemy.IsDead) GameEvents.RaiseGasKill();
                if (FeedFraction > 0f && enemy.IsDead && enemy.Data != null && _growth != null)
                    _growth.AddMass(enemy.Data.MassReward * FeedFraction);
            }
        }
    }
}
