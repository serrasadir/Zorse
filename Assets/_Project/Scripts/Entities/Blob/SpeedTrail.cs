using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-06 (Serra: "Topik'in hızlı olduğu belli olsun"): hızlı giderken arkada hafif rüzgar çizgileri —
    // iki yandan ve tepeden üç ince, yarı saydam beyaz iz. Sadece CharacterData.SpeedTrail açık karakterde (Topik).
    // Çizgiler kök altında gidiş yönüne dönen bir pivotta: konum blob ölçeğiyle büyür, kalınlık ölçekle ayarlanır.
    // Materyal: Tools → Blob.io → Rüzgar İzini Kur (Resources/FX/SpeedTrail). GameManager her run başında ekler/siler.
    public class SpeedTrail : MonoBehaviour
    {
        private const string MaterialPath = "FX/SpeedTrail/Mat_SpeedTrail";
        private const float EmitAboveSpeed = 2.5f;   // birim/sn (Topik Tiny ~6.25, Giant ~4)
        private const float TrailTime = 0.28f;
        private const float WidthPerScale = 0.07f;    // kök ölçeği (= çap) başına iz kalınlığı
        private const float TurnSpeed = 12f;
        private static readonly Vector3[] Offsets =   // kök-yerel (gövde yarıçapı 0.5), arkaya doğru
        {
            new Vector3(-0.42f, 0.05f, -0.25f), new Vector3(0.42f, 0.05f, -0.25f), new Vector3(0f, 0.38f, -0.3f),
        };

        private Rigidbody _rigidbody;
        private Transform _pivot;
        private TrailRenderer[] _trails;
        private bool _emitting;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            Material material = Resources.Load<Material>(MaterialPath);
            if (material == null) return;

            _pivot = new GameObject("SpeedTrailPivot").transform;
            _pivot.SetParent(transform, false);
            _trails = new TrailRenderer[Offsets.Length];
            for (int i = 0; i < Offsets.Length; i++)
            {
                var go = new GameObject("SpeedTrail" + i);
                go.transform.SetParent(_pivot, false);
                go.transform.localPosition = Offsets[i];
                var t = go.AddComponent<TrailRenderer>();
                t.sharedMaterial = material;
                t.time = TrailTime;
                t.minVertexDistance = 0.15f;
                t.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.85f, 0.93f, 1f), 1f) },
                          new[] { new GradientAlphaKey(i == 2 ? 0.28f : 0.38f, 0f), new GradientAlphaKey(0f, 1f) });
                t.colorGradient = g;
                t.alignment = LineAlignment.View;
                t.numCapVertices = 0;
                t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                t.receiveShadows = false;
                t.emitting = false;
                _trails[i] = t;
            }
        }

        private void OnEnable()
        {
            if (_trails == null) return;
            foreach (TrailRenderer t in _trails) t.Clear(); // restart'ta eski konumdan çizgi çekmesin
        }

        private void OnDestroy()
        {
            if (_pivot != null) Destroy(_pivot.gameObject);
        }

        private void LateUpdate()
        {
            if (_trails == null || _rigidbody == null) return;

            Vector3 v = _rigidbody.linearVelocity;
            v.y = 0f;
            bool fast = v.sqrMagnitude > EmitAboveSpeed * EmitAboveSpeed && Time.timeScale > 0f;
            if (fast)
                _pivot.rotation = Quaternion.Slerp(_pivot.rotation, Quaternion.LookRotation(v, Vector3.up),
                    1f - Mathf.Exp(-TurnSpeed * Time.deltaTime));

            float width = transform.localScale.x * WidthPerScale;
            for (int i = 0; i < _trails.Length; i++)
            {
                _trails[i].widthMultiplier = width;
                if (fast != _emitting) _trails[i].emitting = fast;
            }
            _emitting = fast;
        }
    }
}
