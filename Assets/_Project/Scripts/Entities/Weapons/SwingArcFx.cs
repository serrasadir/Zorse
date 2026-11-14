using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    // Sopa vuruşunun beyaz hilal izi: yay bir uçtan öbürüne süpürülür, sonra söner.
    // Mesh bir kez kurulur, vuruş boyunca köşeleri yeniden yazılır (ayırma yok). Materyal Resources/FX/SpeedTrail (köşe rengi + saydam).
    public class SwingArcFx : MonoBehaviour
    {
        private const int Segments = 24;
        private const float SweepTime = 0.12f;
        private const float FadeTime = 0.16f;
        private const float Height = 0.5f;

        private Mesh _mesh;
        private MeshRenderer _renderer;
        private readonly Vector3[] _verts = new Vector3[(Segments + 1) * 2];
        private readonly Color[] _colors = new Color[(Segments + 1) * 2];
        private float _time = -1f;
        private float _radius, _thickness, _arc;
        private bool _flip;
        private Transform _follow;

        public static SwingArcFx Create(string name)
        {
            var go = new GameObject(name);
            var fx = go.AddComponent<SwingArcFx>();
            fx.Build();
            return fx;
        }

        private void Build()
        {
            _mesh = new Mesh { name = "SwingArc" };
            _mesh.MarkDynamic();
            _mesh.vertices = _verts;
            _mesh.colors = _colors;
            // İki yüzlü (shader'ın cull ayarından bağımsız görünsün).
            var tris = new int[Segments * 12];
            for (int i = 0, t = 0; i < Segments; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
                tris[t++] = a; tris[t++] = b; tris[t++] = c;
                tris[t++] = b; tris[t++] = d; tris[t++] = c;
            }
            _mesh.triangles = tris;
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 40f);

            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = Resources.Load<Material>("FX/SpeedTrail/Mat_SpeedTrail");
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
        }

        // radius: yayın dış kenarı (merkezden), arc: derece. flip: süpürme yönü (vuruşlar sırayla sağdan/soldan).
        public void Play(Transform follow, Vector3 forward, float radius, float arc, bool flip)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            _follow = follow;
            transform.rotation = Quaternion.LookRotation(forward.normalized);
            _radius = radius;
            _thickness = Mathf.Max(0.25f, radius * 0.22f);
            _arc = arc;
            _flip = flip;
            _time = 0f;
            _renderer.enabled = true;
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (_time < 0f) return;
            _time += Time.deltaTime;
            if (_time > SweepTime + FadeTime)
            {
                _time = -1f;
                _renderer.enabled = false;
                return;
            }

            if (_follow != null)
            {
                Vector3 p = _follow.position;
                transform.position = new Vector3(p.x, Height, p.z);
            }

            float reveal = Mathf.Clamp01(_time / SweepTime);
            float fade = 1f - Mathf.Clamp01((_time - SweepTime) / FadeTime);
            float half = _arc * 0.5f * Mathf.Deg2Rad;

            for (int i = 0; i <= Segments; i++)
            {
                float u = i / (float)Segments;          // yay boyunca 0..1 (süpürme sırası)
                float s = _flip ? 1f - u : u;
                float angle = Mathf.Lerp(-half, half, s);
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                // Hilal: ortası kalın, uçları sivri; süpürmenin önü henüz görünmez, kuyruk daha soluk.
                float taper = Mathf.Sin(Mathf.PI * u);
                float visible = u <= reveal ? 1f : 0f;
                float w = _thickness * taper * visible;
                _verts[i * 2] = dir * _radius;
                _verts[i * 2 + 1] = dir * (_radius - w);
                float alpha = fade * visible * Mathf.Lerp(0.35f, 0.95f, u);
                _colors[i * 2] = new Color(1f, 1f, 1f, alpha);
                _colors[i * 2 + 1] = new Color(1f, 1f, 1f, alpha * 0.4f);
            }
            _mesh.vertices = _verts;
            _mesh.colors = _colors;
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
