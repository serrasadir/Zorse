using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // "Höpleme": yenen objenin görsel kopyası kısa sürede küçülerek blob'un ortasına çekilir (obje kendisi
    // anında havuza döner). Sabit kopya havuzu, ilk yemede bir kez kurulur; mesh/materyal kaynak objeden
    // paylaşılır (yeni materyal yok). Static batching'deki şehir objelerinin mesh'i birleşik: onlarda
    // CityConsumable.SlurpMesh (editörde yazılan orijinal mesh) kullanılır; büyükler (allowStatic=false) atlanır.
    public class SlurpFx : MonoBehaviour
    {
        private const int Count = 16;
        private const float Duration = 0.14f;
        private const float SpinDegrees = 160f;
        // Uçuş (Lamba Direği): öldürülen düşman havaya fırlayıp yayla ağza düşer.
        private const float FlightDuration = 0.38f;
        private const float FlightSpin = 540f;
        private const float FlightHopBase = 1.2f, FlightHopPerUnit = 0.35f;

        private static SlurpFx s_instance;

        private struct Item
        {
            public Transform Transform;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Vector3 From;
            public Vector3 Scale;
            public Quaternion Rotation;
            public float Time;
            public float Duration;
            public float Hop; // 0 = düz çekim
        }

        private Item[] _items;
        private int _next;
        private Transform _target;

        public static void Play(GameObject source, Transform blob, bool allowStatic) => Play(source, blob, allowStatic, false);

        // Havaya fırlayıp ağza uçar (statik batch'li şehir objesi değil, düşman için).
        public static void PlayFlight(GameObject source, Transform blob) => Play(source, blob, false, true);

        private static void Play(GameObject source, Transform blob, bool allowStatic, bool flight)
        {
            if (source == null || blob == null) return;
            MeshRenderer renderer = LargestRenderer(source);
            if (renderer == null) return;
            Mesh mesh;
            if (renderer.isPartOfStaticBatch)
            {
                if (!allowStatic) return;
                CityConsumable city = source.GetComponent<CityConsumable>();
                mesh = city != null ? city.SlurpMesh : null;
            }
            else
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                mesh = filter != null ? filter.sharedMesh : null;
            }
            if (mesh == null) return;
            if (s_instance == null) Create();
            s_instance.Spawn(renderer, mesh, blob, flight);
        }

        private static MeshRenderer LargestRenderer(GameObject source)
        {
            MeshRenderer best = null;
            float bestSize = 0f;
            foreach (MeshRenderer r in source.GetComponentsInChildren<MeshRenderer>(false)) // yeme anında, obje başına bir kez
            {
                if (!r.enabled) continue;
                float size = r.bounds.size.sqrMagnitude;
                if (size > bestSize) { best = r; bestSize = size; }
            }
            return best;
        }

        private static void Create()
        {
            var go = new GameObject("SlurpFx");
            s_instance = go.AddComponent<SlurpFx>(); // tek seferlik kurulum
            s_instance._items = new Item[Count];
            for (int i = 0; i < Count; i++)
            {
                var child = new GameObject("Slurp");
                child.transform.SetParent(go.transform, false);
                var item = new Item
                {
                    Transform = child.transform,
                    Filter = child.AddComponent<MeshFilter>(),
                    Renderer = child.AddComponent<MeshRenderer>(),
                    Time = -1f,
                };
                item.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                item.Renderer.receiveShadows = false;
                child.SetActive(false);
                s_instance._items[i] = item;
            }
        }

        private void Spawn(MeshRenderer renderer, Mesh mesh, Transform blob, bool flight)
        {
            _target = blob;
            ref Item item = ref _items[_next]; // hepsi meşgulse en eskisi yeniden kullanılır
            _next = (_next + 1) % _items.Length;

            Transform src = renderer.transform;
            item.Filter.sharedMesh = mesh;
            item.Renderer.sharedMaterials = renderer.sharedMaterials;
            item.From = src.position;
            item.Rotation = src.rotation;
            item.Scale = src.lossyScale;
            item.Time = 0f;
            item.Duration = flight ? FlightDuration : Duration;
            float dist = flight ? Vector3.Distance(item.From, blob.position) : 0f;
            item.Hop = flight ? FlightHopBase + dist * FlightHopPerUnit : 0f;
            item.Transform.SetPositionAndRotation(item.From, item.Rotation);
            item.Transform.localScale = item.Scale;
            item.Transform.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_items == null || _target == null) return;
            // Blob'un ortası (görünür gövde merkezi yerden yarıçap kadar yukarıda).
            Vector3 center = _target.position + Vector3.up * (_target.localScale.y * 0.5f);
            float dt = Time.deltaTime;
            for (int i = 0; i < _items.Length; i++)
            {
                ref Item item = ref _items[i];
                if (item.Time < 0f) continue;
                item.Time += dt / item.Duration;
                if (item.Time >= 1f)
                {
                    item.Time = -1f;
                    item.Transform.gameObject.SetActive(false);
                    continue;
                }
                float t = item.Time;
                if (item.Hop > 0f)
                {
                    // Yay: yukarı fırlar, takla atarak ağza düşer; sonda küçülür.
                    item.Transform.position = Vector3.LerpUnclamped(item.From, center, t) + Vector3.up * (item.Hop * Mathf.Sin(t * Mathf.PI));
                    item.Transform.rotation = Quaternion.AngleAxis(FlightSpin * t, Vector3.right) * item.Rotation;
                    item.Transform.localScale = item.Scale * Mathf.Lerp(1f, 0.25f, t * t);
                    continue;
                }
                float pull = t * t; // yavaş başlar, içine hızla girer
                item.Transform.position = Vector3.LerpUnclamped(item.From, center, pull);
                item.Transform.rotation = Quaternion.AngleAxis(SpinDegrees * t, Vector3.up) * item.Rotation;
                item.Transform.localScale = item.Scale * Mathf.Lerp(1f, 0.15f, t);
            }
        }
    }
}
