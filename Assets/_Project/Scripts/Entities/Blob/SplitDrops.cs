using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // Bölünme: ölümcül darbede kaybedilen kütle etrafa jöle parçaları olarak saçılır (Blender: build_skill_fx.py →
    // Resources/FX/Split/FX_SplitDrop). Parçalar dışa doğru fırlar, yerde Lifetime sn durur (son saniyelerde yanıp
    // söner); blob değince kaybedilen kütlenin payı geri gelir (XP vermez). Fizik sorgusu yok: birkaç parçaya
    // mesafe bakılır. BlobSkills.TrySplit ekler; restart'ta GameManager siler.
    public class SplitDrops : MonoBehaviour
    {
        private const int Count = 8;
        private const float Lifetime = 10f, BlinkTime = 2f, BlinkInterval = 0.15f;
        private const float FlyTime = 0.5f, ArmTime = 0.8f;   // havadayken ve hemen sonra toplanmaz
        private const float SizeOfBlob = 0.3f;                 // parça çapı = blob çapı × bu
        private const string MeshPath = "FX/Split/FX_SplitDrop";
        private const string MaterialPath = "FX/SpeedTrail/Mat_SpeedTrail";

        private readonly Transform[] _drops = new Transform[Count];
        private readonly MeshRenderer[] _renderers = new MeshRenderer[Count];
        private readonly Vector3[] _from = new Vector3[Count], _to = new Vector3[Count];
        private readonly float[] _mass = new float[Count];
        private readonly bool[] _alive = new bool[Count];
        private float _age = -1f;
        private float _size;
        private BlobGrowth _growth;
        private float _bodyLocalRadius = 0.5f;
        private Transform _root;

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
            foreach (SphereCollider c in GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }

        public void Scatter(float lostMass)
        {
            Mesh mesh = Resources.Load<Mesh>(MeshPath);
            Material material = Resources.Load<Material>(MaterialPath);
            if (mesh == null || material == null || lostMass <= 0f) return;
            if (_root == null) _root = new GameObject("SplitDrops").transform;

            Vector3 c = transform.position;
            float bodyRadius = transform.localScale.x * _bodyLocalRadius;
            _size = Mathf.Max(0.3f, bodyRadius * 2f * SizeOfBlob);
            for (int i = 0; i < Count; i++)
            {
                if (_drops[i] == null)
                {
                    var go = new GameObject("Drop" + i);
                    go.transform.SetParent(_root, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    _renderers[i] = go.AddComponent<MeshRenderer>();
                    _renderers[i].sharedMaterial = material;
                    _renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _drops[i] = go.transform;
                }
                float a = (i + Random.Range(-0.3f, 0.3f)) / Count * Mathf.PI * 2f;
                float d = bodyRadius + Random.Range(1.5f, 3f);
                Vector3 to = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
                to = BlobSurvivor.Core.MapBounds.Clamp(to, _size);
                _from[i] = new Vector3(c.x, 0f, c.z);
                _to[i] = new Vector3(to.x, 0f, to.z);
                _mass[i] = lostMass / Count;
                _alive[i] = true;
                _renderers[i].enabled = true;
                _drops[i].localScale = Vector3.one * _size;
            }
            _age = 0f;
        }

        private void Update()
        {
            if (_age < 0f) return;
            _age += Time.deltaTime;
            bool any = false;
            Vector3 blob = transform.position;
            float pickup = transform.localScale.x * _bodyLocalRadius + _size * 0.4f;
            bool blinkOff = _age > Lifetime - BlinkTime && Mathf.FloorToInt(_age / BlinkInterval) % 2 == 1;

            for (int i = 0; i < Count; i++)
            {
                if (!_alive[i]) continue;
                if (_age >= Lifetime) { Kill(i); continue; }
                any = true;

                float k = Mathf.Clamp01(_age / FlyTime);
                Vector3 p = Vector3.Lerp(_from[i], _to[i], 1f - (1f - k) * (1f - k));
                p.y = Mathf.Sin(k * Mathf.PI) * 1.2f + 0.05f; // yay çizerek düşer
                float wobble = 1f + Mathf.Sin(_age * 6f + i) * 0.06f;
                _drops[i].position = p;
                _drops[i].localScale = new Vector3(_size * wobble, _size / wobble, _size * wobble);
                _renderers[i].enabled = !blinkOff;

                if (_age < ArmTime) continue;
                Vector3 flat = p - blob;
                flat.y = 0f;
                if (flat.sqrMagnitude > pickup * pickup) continue;
                if (_growth != null) _growth.RestoreMass(_mass[i]);
                _growth?.PunchScale(0.08f);
                BlobSurvivor.Core.Haptics.LightTap();
                Kill(i);
            }
            if (!any) _age = -1f;
        }

        private void Kill(int i)
        {
            _alive[i] = false;
            if (_renderers[i] != null) _renderers[i].enabled = false;
        }
    }
}
