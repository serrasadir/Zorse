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

        // Geğirme (evrim): BurpInterval sn'de bir gaz alanı bir anlığına BurpRadiusScale katına çıkar; içindekilere
        // hasar + geri itme + sersemletme (boss sersemlemez). 0 = kapalı.
        public float BurpInterval;
        public float BurpDamage = 30f;
        public float BurpRadiusScale = 2f;
        public float BurpKnockback = 2f;
        public float BurpStun = 0.8f;
        private const float BurpPulseTime = 0.45f;
        private const int WallMask = 1 << 16;
        private static readonly Color BurpA = new Color(0.62f, 0.32f, 0.85f), BurpB = new Color(0.86f, 0.62f, 1f);
        // Gaz mor: yeşil çimenli zeminde (kale köyü) kayboluyordu. Bulut modelinin köşe renkleri bu tona çevrilir.
        private const float GasHue = 0.78f;
        private static Mesh s_purpleClouds;
        private float _burpTimer, _burpPulse = -1f;
        private Weapons.SwingArcFx _burpFx;
        private BlobVisual _visual;

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
            if (_burpFx != null) Destroy(_burpFx.gameObject);
        }

        public void EnableBurp(float interval)
        {
            BurpInterval = interval;
            _burpTimer = 1f; // evrim seçilince hemen bir tane
            _visual = GetComponent<BlobVisual>();
        }

        private float EffectiveRadius => transform.localScale.x * _bodyLocalRadius + Radius;

        private void CreateVisual()
        {
            Material material = Resources.Load<Material>(MaterialPath);
            Mesh clouds = PurpleClouds(Resources.Load<Mesh>(CloudMeshPath));
            if (material == null || clouds == null) return;
            var go = new GameObject("DigestGasClouds");
            go.AddComponent<MeshFilter>().sharedMesh = clouds;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _clouds = go.transform;
        }

        // Modelin (FX_Gas) yeşil köşe renklerini mora çevrilmiş kopyası; uygulama boyunca bir kez.
        private static Mesh PurpleClouds(Mesh source)
        {
            if (source == null) return null;
            if (s_purpleClouds != null) return s_purpleClouds;
            Color[] colors = source.colors;
            if (colors == null || colors.Length == 0) return source;
            for (int i = 0; i < colors.Length; i++)
            {
                Color.RGBToHSV(colors[i], out float h, out float sat, out float v);
                Color c = Color.HSVToRGB(GasHue, Mathf.Min(1f, sat * 1.05f), v);
                c.a = colors[i].a;
                colors[i] = c;
            }
            s_purpleClouds = Object.Instantiate(source);
            s_purpleClouds.colors = colors;
            return s_purpleClouds;
        }

        private void LateUpdate()
        {
            if (_clouds == null) return;
            _pulse += Time.deltaTime * 2f;
            _spin += CloudSpin * Time.deltaTime;
            float d = EffectiveRadius * (1f + Mathf.Sin(_pulse) * 0.03f);
            if (_burpPulse >= 0f)
            {
                // Geğirmede bulut halkası dışa fırlar, sonra yerine döner.
                _burpPulse += Time.deltaTime / BurpPulseTime;
                if (_burpPulse >= 1f) _burpPulse = -1f;
                else d *= 1f + (BurpRadiusScale - 1f) * Mathf.Sin(_burpPulse * Mathf.PI);
            }
            Vector3 p = transform.position;
            _clouds.position = new Vector3(p.x, 0.12f, p.z);
            _clouds.rotation = Quaternion.Euler(0f, _spin, 0f);
            // Bulutlar yarıçapla yayılır, boyu daha yavaş büyür (dev blob'da gaz kule gibi olmasın).
            float h = Mathf.Sqrt(d) * (1f + Mathf.Sin(_pulse * 1.3f) * 0.06f);
            _clouds.localScale = new Vector3(d, h, d);
        }

        private void Update()
        {
            if (BurpInterval > 0f)
            {
                _burpTimer -= Time.deltaTime;
                if (_burpTimer <= 0f) { _burpTimer = BurpInterval; Burp(); }
            }
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

        private void Burp()
        {
            Vector3 origin = transform.position;
            float radius = EffectiveRadius * BurpRadiusScale;
            float damage = BurpDamage * BlobSkills.WeaponDamageScale;
            int count = Physics.OverlapCapsuleNonAlloc(origin, origin + Vector3.up * VerticalReach, radius, _hits, EnemyMask);
            for (int i = 0; i < count; i++)
            {
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>(); // geğirme anında, 6 sn'de bir
                if (enemy == null || enemy.IsDead) continue;
                if (Physics.Linecast(origin, _hits[i].bounds.center, WallMask, QueryTriggerInteraction.Ignore)) continue;
                enemy.TakeDamage(damage);
                enemy.Knockback(enemy.transform.position - origin, BurpKnockback);
                if (!enemy.IsBoss) enemy.Stun(BurpStun);
                if (enemy.IsDead) GameEvents.RaiseGasKill();
            }

            if (_burpFx == null) _burpFx = Weapons.SwingArcFx.Create("BurpShock");
            _burpFx.Play(transform, Vector3.forward, radius, 360f, false);
            _burpPulse = 0f;
            float s = transform.localScale.x;
            if (_growth != null) _growth.PunchScale(0.3f);
            if (_visual != null) _visual.Gulp(0.22f);
            Vector3 face = _visual != null && _visual.HasFacing ? _visual.Forward : Vector3.back;
            Weapons.FireworkBurstFx.Puff(origin + face * s * 0.5f + Vector3.up * s * 0.6f, BurpA, BurpB, s, 18, 2.5f, 1.5f);
            Haptics.Impact(1);
        }
    }
}
