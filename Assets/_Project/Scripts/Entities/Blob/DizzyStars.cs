using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // Sersemleme yıldızları: yaya tekmesinden sonra blob'un tepesinde dönen sarı yıldızlar (çizgi film "dizzy").
    // Yıldız mesh'i kodda kurulur (turuncu kenarlı sarı 5 köşe), materyal Rüzgar İzi'ninki (köşe rengi, unlit).
    // İlk kullanımda bir kez kurulur, sonra sadece açılıp kapanır.
    public class DizzyStars : MonoBehaviour
    {
        private const string MaterialPath = "FX/SpeedTrail/Mat_SpeedTrail";
        private const int Count = 3;
        private const float OrbitSpeed = 300f;  // derece/sn
        private const float PopTime = 0.15f;

        private static Mesh s_mesh;
        private Transform[] _stars;
        private float _start, _until = -1f;
        private float _angle;
        private Camera _camera;

        public static void Play(GameObject blob, float duration)
        {
            DizzyStars d = blob.GetComponent<DizzyStars>();
            if (d == null) d = blob.AddComponent<DizzyStars>();
            d.Begin(duration);
        }

        private void Begin(float duration)
        {
            if (_stars == null && !Build()) return;
            _start = Time.time;
            _until = Mathf.Max(_until, Time.time + duration);
            for (int i = 0; i < _stars.Length; i++) _stars[i].gameObject.SetActive(true);
        }

        private bool Build()
        {
            Material material = Resources.Load<Material>(MaterialPath);
            if (material == null) return false;
            if (s_mesh == null) s_mesh = BuildStarMesh();
            _camera = Camera.main;
            _stars = new Transform[Count];
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("DizzyStar");
                go.AddComponent<MeshFilter>().sharedMesh = s_mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _stars[i] = go.transform;
            }
            return true;
        }

        private void OnDisable() => Hide();

        private void OnDestroy()
        {
            if (_stars == null) return;
            foreach (Transform s in _stars) if (s != null) Destroy(s.gameObject);
        }

        private void Hide()
        {
            _until = -1f;
            if (_stars == null) return;
            foreach (Transform s in _stars) if (s != null) s.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_until < 0f) return;
            float now = Time.time;
            if (now >= _until) { Hide(); return; }

            // Açılırken "pop", biterken küçülerek kaybolur.
            float pop = Mathf.Min(1f, (now - _start) / PopTime, (_until - now) / PopTime);
            pop = Mathf.Max(0f, pop);
            _angle += OrbitSpeed * Time.deltaTime;

            float body = transform.localScale.x * 0.5f;   // gövde yarıçapı (katı collider 0.5)
            Vector3 center = transform.position + Vector3.up * (body * 1.5f);
            float orbit = body * 0.85f;
            float size = body * 0.38f * pop;
            if (_camera == null) _camera = Camera.main;
            Quaternion face = _camera != null ? _camera.transform.rotation : Quaternion.identity;

            for (int i = 0; i < _stars.Length; i++)
            {
                float a = (_angle + i * 360f / _stars.Length) * Mathf.Deg2Rad;
                Vector3 p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * orbit
                            + Vector3.up * (Mathf.Sin(a * 2f + i) * body * 0.08f);
                _stars[i].position = p;
                _stars[i].rotation = face * Quaternion.Euler(0f, 0f, _angle * 1.5f + i * 40f);
                _stars[i].localScale = Vector3.one * size;
            }
        }

        // Kameraya bakan düz yıldız: önde sarı yıldız, arkasında biraz büyük turuncu kenar.
        private static Mesh BuildStarMesh()
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var colors = new System.Collections.Generic.List<Color>();
            var tris = new System.Collections.Generic.List<int>();
            AddStar(verts, colors, tris, 1.3f, 0.02f, new Color(0.95f, 0.45f, 0.1f, 1f), new Color(0.95f, 0.45f, 0.1f, 1f));
            AddStar(verts, colors, tris, 1f, 0f, new Color(1f, 0.98f, 0.75f, 1f), new Color(1f, 0.82f, 0.15f, 1f));
            var mesh = new Mesh { name = "DizzyStar" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddStar(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Color> c,
            System.Collections.Generic.List<int> t, float radius, float z, Color inner, Color outer)
        {
            int start = v.Count;
            v.Add(new Vector3(0f, 0f, z));
            c.Add(inner);
            for (int i = 0; i < 10; i++)
            {
                float a = (90f + i * 36f) * Mathf.Deg2Rad;
                float r = (i % 2 == 0 ? 1f : 0.45f) * radius;
                v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z));
                c.Add(outer);
            }
            for (int i = 0; i < 10; i++)
            {
                // Kameraya bakan yüz (+Z kameradan uzağa): saat yönü.
                t.Add(start);
                t.Add(start + 1 + (i + 1) % 10);
                t.Add(start + 1 + i);
            }
        }
    }
}
