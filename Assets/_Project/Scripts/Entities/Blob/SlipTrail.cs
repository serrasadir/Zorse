using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // Kaygan Zemin (aktif): blob arkasında kaygan iz bırakır, izdeki düşmanlar yavaşlar.
    // Sv.3 robot köpekler (sürü) izde kayar · Sv.5 asit iz: yutamadıklarına hasar. Değerleri SlipTrailEffect yazar.
    // İz, 0.25 sn'de bir bırakılan noktalardan oluşur; sorgu 0.2 sn'de bir, blob çevresinde tek OverlapCapsule.
    // Restart'ta GameManager.ClearPreviousRunState siler.
    public class SlipTrail : MonoBehaviour
    {
        public float Duration = 2f;      // iz ömrü (sn)
        public float Slow = 0.3f;        // yavaşlatma oranı
        public float SwarmSlip;          // >0: sürü düşmanı bu kadar sn kayar
        public float AcidDps;            // >0: yutamadıklarına sn başı hasar

        private const float DropInterval = 0.25f;
        private const float TickInterval = 0.2f;
        private const float MinDropDistance = 0.3f;
        private const float WidthFactor = 0.8f;   // iz yarı genişliği = gövde yarıçapı × bu + sabit
        private const float WidthExtra = 0.3f;
        private const float SlipCooldown = 1.5f;
        private const int EnemyMask = 1 << 14;
        private const int MaxPoints = 24;
        // Görsel: her iz noktasına yere yapışan sümük lekesi (Blender: build_skill_fx.py → Resources/FX/Slime/FX_Slime,
        // yarıçap 0.5, renk/saydamlık köşe renginde). Leke belirir, ömrünün sonunda küçülüp söner. Asit: sarı ton.
        private const string MaterialPath = "FX/SpeedTrail/Mat_SpeedTrail";
        private const string SplatMeshPath = "FX/Slime/FX_Slime";
        private const float SplatGrow = 0.12f, SplatFade = 0.5f, SplatY = 0.15f;
        private static readonly Color SlimeTint = Color.white;
        private static readonly Color AcidTint = new Color(1f, 0.95f, 0.35f, 1f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Vector3[] _points = new Vector3[MaxPoints];
        private readonly float[] _times = new float[MaxPoints];
        private int _head, _count;
        private float _dropTimer, _tickTimer;
        private readonly Collider[] _hits = new Collider[32];
        private BlobGrowth _growth;
        private float _bodyLocalRadius = 0.5f;

        private Mesh _splatMesh;
        private Material _material;
        private readonly Transform[] _splats = new Transform[MaxPoints];
        private readonly MeshRenderer[] _splatRenderers = new MeshRenderer[MaxPoints];
        private readonly float[] _splatScale = new float[MaxPoints];
        private MaterialPropertyBlock _block;
        private Transform _splatRoot;

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
            foreach (SphereCollider c in GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
            _tickTimer = Random.Range(0f, TickInterval);
            _splatMesh = Resources.Load<Mesh>(SplatMeshPath);
            _material = Resources.Load<Material>(MaterialPath);
            _block = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (_splatRoot != null) Destroy(_splatRoot.gameObject);
        }

        private float HalfWidth => transform.localScale.x * _bodyLocalRadius * WidthFactor + WidthExtra;

        // İz noktası bırakılınca o noktanın lekesi kurulur (yuva başına tek seferlik obje, sonra yeniden kullanılır).
        private void PlaceSplat(int index, Vector3 point)
        {
            if (_splatMesh == null || _material == null) return;
            if (_splatRoot == null) _splatRoot = new GameObject("SlipTrailSplats").transform;
            if (_splats[index] == null)
            {
                var go = new GameObject("Splat" + index);
                go.transform.SetParent(_splatRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = _splatMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                _splats[index] = go.transform;
                _splatRenderers[index] = r;
            }
            _splats[index].position = new Vector3(point.x, SplatY, point.z);
            _splats[index].rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            _splatScale[index] = HalfWidth * 2f * Random.Range(0.95f, 1.15f); // model yarıçapı 0.5
            _splatRenderers[index].enabled = true;
        }

        private void LateUpdate()
        {
            if (_splatRoot == null) return;
            float now = Time.time;
            Color tint = AcidDps > 0f ? AcidTint : SlimeTint;
            for (int i = 0; i < MaxPoints; i++)
            {
                MeshRenderer r = _splatRenderers[i];
                if (r == null || !r.enabled) continue;
                float age = now - _times[i];
                float left = Duration - age;
                if (left <= 0f) { r.enabled = false; continue; }
                float grow = Mathf.Clamp01(age / SplatGrow);
                float fade = Mathf.Clamp01(left / SplatFade);
                float s = _splatScale[i] * (0.6f + 0.4f * grow) * (0.7f + 0.3f * fade);
                _splats[i].localScale = new Vector3(s, 1f, s);
                tint.a = fade;
                _block.SetColor(BaseColorId, tint);
                r.SetPropertyBlock(_block);
            }
        }

        private void Update()
        {
            float now = Time.time;
            _dropTimer -= Time.deltaTime;
            if (_dropTimer <= 0f)
            {
                _dropTimer = DropInterval;
                Vector3 p = transform.position;
                p.y = 0f;
                int last = (_head - 1 + MaxPoints) % MaxPoints;
                if (_count == 0 || (p - _points[last]).sqrMagnitude > MinDropDistance * MinDropDistance)
                {
                    _points[_head] = p;
                    _times[_head] = now;
                    PlaceSplat(_head, p);
                    _head = (_head + 1) % MaxPoints;
                    if (_count < MaxPoints) _count++;
                }
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;
            Tick(now);
        }

        private void Tick(float now)
        {
            Vector3 origin = transform.position;
            origin.y = 0f;
            float half = HalfWidth;
            float reachSqr = 0f;
            bool any = false;
            for (int i = 0; i < _count; i++)
            {
                if (now - _times[i] > Duration) continue;
                any = true;
                reachSqr = Mathf.Max(reachSqr, (_points[i] - origin).sqrMagnitude);
            }
            if (!any) return;

            float radius = Mathf.Sqrt(reachSqr) + half;
            int hitCount = Physics.OverlapCapsuleNonAlloc(origin, origin + Vector3.up * 3f, radius, _hits, EnemyMask);
            BlobSurvivor.Core.BlobTier tier = _growth != null ? _growth.CurrentTier : BlobSurvivor.Core.BlobTier.Tiny;
            float halfSqr = half * half;

            for (int h = 0; h < hitCount; h++)
            {
                Vector3 e = _hits[h].transform.position;
                e.y = 0f;
                bool inTrail = false;
                for (int i = 0; i < _count && !inTrail; i++)
                    inTrail = now - _times[i] <= Duration && (_points[i] - e).sqrMagnitude <= halfSqr;
                if (!inTrail) continue;

                EnemyBase enemy = _hits[h].GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                enemy.ApplySlow(1f - Slow, TickInterval + 0.15f);
                if (SwarmSlip > 0f && enemy.Data != null && enemy.Data.IsSwarm) enemy.Slip(SwarmSlip, SlipCooldown);
                if (AcidDps > 0f && !enemy.CanBeConsumedBy(tier)) enemy.TakeDamage(AcidDps * TickInterval);
            }
        }
    }
}
