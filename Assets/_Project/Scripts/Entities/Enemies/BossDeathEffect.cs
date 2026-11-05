using System;
using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Blob;
using Object = UnityEngine.Object;

namespace BlobSurvivor.Entities.Enemies
{
    // Boss'lar bir anda kaybolmasın. Havuza dönen boss'un yerine görüntüsünün kopyası (boss tipi başına bir kez
    // kurulur, sonra yeniden kullanılır) kısa bir sahne oynar; boss mantığı etkilenmez. BossSpawner ekler ve çağırır.
    //  - Play (miniboss): beyaz parlama + titreme, patlama kırıntıları, yana yatıp yere gömülme (~1.3 sn).
    //  - PlaySwallow (final boss): yavaş çekim, helikopter dönerek küçülüp blob'a emilir, kocaman yutkunma +
    //    patlama, zaman normale döner, geğirme, kısa bekleme, sonra onDone (run biter; toplam ~4 sn).
    //    Sahne ölçeksiz zamanla akar; blob bu sırada hasar almaz.
    public class BossDeathEffect : MonoBehaviour
    {
        private const float DeathDuration = 1.3f;
        private const float SuckDuration = 1.15f;   // helikopterin blob'a emilmesi
        // Yutkunmadan sonra: zaman normale döner (RecoverTime), geğirme (BurpAt), KAZANDIN'dan önce bekleme.
        private const float RecoverTime = 0.8f;
        private const float BurpAt = 1.3f;
        private const float SwallowDuration = SuckDuration + 2.9f;
        private const float SlowMo = 0.2f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color Metal = new Color(0.23f, 0.25f, 0.28f);   // palet "metal"
        private static readonly Color Navy = new Color(0.18f, 0.25f, 0.4f);     // palet "navy"
        private static readonly Color White = new Color(0.95f, 0.94f, 0.92f);   // palet "white"
        private static readonly Color Fire = new Color(0.96f, 0.64f, 0.36f);    // palet "orange"
        private static readonly Color Spark = new Color(0.96f, 0.77f, 0.27f);   // palet "yellow"

        private class Proxy
        {
            public Transform Root;
            public Renderer[] Renderers;
            public Transform[] Parts;
        }

        private enum Mode { None, Death, Swallow }

        private readonly Dictionary<GameObject, Proxy> _proxies = new Dictionary<GameObject, Proxy>();
        private MaterialPropertyBlock _block; // Awake'te: alan başlatıcısında oluşturmak cihazda UnityException verir
        private EatCrumbs[] _crumbs;
        private int _nextCrumb;
        private CameraController _camera;

        private Mode _mode;
        private Proxy _active;
        private float _time;
        private Vector3 _startPos;
        private Quaternion _startRot;
        private Vector3 _startScale;
        private Bounds _bounds;
        private float _rollSign;
        private int _stage;
        private float _nextTrail;
        private float _spin;
        private Action _onDone;
        private Transform _blob;
        private BlobHealth _blobHealth;
        private BlobGrowth _blobGrowth;
        private BlobVisual _blobVisual;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _camera = FindAnyObjectByType<CameraController>();
            EatCrumbs prefab = Resources.Load<EatCrumbs>("FX/EatCrumbs");
            if (prefab == null) return;
            Transform container = new GameObject("BossDeathCrumbs").transform;
            _crumbs = new EatCrumbs[4];
            for (int i = 0; i < _crumbs.Length; i++)
                _crumbs[i] = Instantiate(prefab, container); // tek seferlik kurulum
        }

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRunStart;
        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRunStart;

        // Restart sahnenin ortasında gelirse: sahne iptal, run bitiş olayı atılmaz.
        private void HandleRunStart(Data.CharacterData _)
        {
            _onDone = null;
            Finish();
            if (_crumbs != null) foreach (EatCrumbs c in _crumbs) c.Stop();
        }

        // ---------------------------------------------------------------- miniboss

        public void Play(EnemyBase boss, GameObject prefab)
        {
            if (!Begin(boss, prefab, Mode.Death)) return;
            Burst(_bounds, Metal, Navy, 1f, _bounds.center + Vector3.down * 3f);
            _camera?.Shake(0.22f, 0.35f);
            Haptics.Impact(2);
        }

        // ---------------------------------------------------------------- final boss

        public void PlaySwallow(EnemyBase boss, GameObject prefab, Action onDone)
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob == null || !Begin(boss, prefab, Mode.Swallow))
            {
                onDone?.Invoke();
                return;
            }
            _onDone = onDone;
            _blob = blob.transform;
            _blobHealth = blob.GetComponent<BlobHealth>();
            _blobGrowth = blob.GetComponent<BlobGrowth>();
            _blobVisual = blob.GetComponent<BlobVisual>();
            if (_blobHealth != null) _blobHealth.Invulnerable = true;
            _spin = 0f;
            _nextTrail = 0.12f;

            Time.timeScale = SlowMo;
            Burst(_bounds, Spark, Fire, 0.7f, BlobCenter());
            _camera?.Shake(0.15f, 0.3f);
            Haptics.Impact(1);
        }

        private void Update()
        {
            if (_mode == Mode.None) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return; // duraklatma
            if (_mode == Mode.Death) UpdateDeath();
            else UpdateSwallow();
        }

        private void UpdateDeath()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _time += dt;
            float t = _time / DeathDuration;

            float flash = Mathf.Clamp01(1f - _time / 0.25f) * (Mathf.Repeat(_time * 14f, 1f) < 0.5f ? 1f : 0.6f);
            SetTint(Color.Lerp(Color.white, new Color(2.2f, 1.6f, 1.2f), flash));

            Vector3 jitter = _time < 0.35f ? UnityEngine.Random.insideUnitSphere * 0.12f * (1f - _time / 0.35f) : Vector3.zero;
            float sink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, t));
            float hop = Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI) * 0.35f;
            _active.Root.position = _startPos + jitter + Vector3.up * (hop - sink * (_bounds.size.y + 0.5f));
            _active.Root.rotation = _startRot * Quaternion.Euler(sink * 10f, 0f, _rollSign * sink * 28f);
            _active.Root.localScale = _startScale * Mathf.Lerp(1f, 0.75f, sink);

            Vector3 down = _bounds.center + Vector3.down * 3f;
            if (_stage == 0 && t >= 0.3f) { _stage = 1; Burst(_bounds, Fire, Spark, 0.9f, down); _camera?.Shake(0.12f, 0.25f); Haptics.Impact(1); }
            if (_stage == 1 && t >= 0.8f) { _stage = 2; Burst(_bounds, Metal, Fire, 0.6f, down); }
            if (t >= 1f) Finish();
        }

        private void UpdateSwallow()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;

            if (_time < SuckDuration)
            {
                Time.timeScale = SlowMo; // duraklat-devam sonrası da yavaş çekim sürsün
                float u = _time / SuckDuration;
                float ease = u * u * (3f - 2f * u) * u;                    // sona doğru hızlanan emilme
                Vector3 target = BlobCenter();
                _spin += Mathf.Lerp(300f, 1500f, u) * dt;                   // fırıldak gibi dönerek gelir
                _active.Root.position = Vector3.Lerp(_startPos, target, ease) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.6f;
                _active.Root.rotation = Quaternion.Euler(0f, _spin, Mathf.Sin(u * 9f) * 18f) * _startRot;
                _active.Root.localScale = _startScale * Mathf.Lerp(1f, 0.04f, ease);
                SetTint(Color.Lerp(Color.white, new Color(1.6f, 1.4f, 1.3f), Mathf.Repeat(_time * 6f, 1f) < 0.5f ? 0.4f : 0f));
                if (_camera != null) _camera.CinematicZoom = Mathf.Lerp(1f, 0.72f, u);

                if (_time >= _nextTrail)                                     // arkasında kıvılcım/duman izi
                {
                    _nextTrail += 0.18f;
                    var b = new Bounds(_active.Root.position, Vector3.one * Mathf.Lerp(1.6f, 0.5f, u));
                    Burst(b, _stage++ % 2 == 0 ? Spark : Metal, Fire, 0.5f, target);
                }
                return;
            }

            if (_active.Root.gameObject.activeSelf)                         // yutuldu!
            {
                foreach (Renderer r in _active.Renderers) r.SetPropertyBlock(null);
                _active.Root.gameObject.SetActive(false);
                float s = _blob.lossyScale.x;
                Burst(new Bounds(BlobCenter(), Vector3.one * s * 1.6f), Navy, White, 1.4f, BlobCenter());
                Burst(new Bounds(BlobCenter(), Vector3.one * s * 1.2f), Fire, Spark, 1.1f, BlobCenter());
                _blobGrowth?.PunchScale(0.4f);
                _blobVisual?.Gulp(0.45f);
                _camera?.Shake(0.35f, 0.5f);
                Haptics.Impact(2);
                _stage = 100;
            }

            // Yutkunduktan sonra zaman yavaşça normale döner, kamera geri açılır; sonunda "geğirme" dokunuşu.
            float since = _time - SuckDuration;
            float recover = Mathf.Clamp01(since / RecoverTime);
            Time.timeScale = Mathf.Lerp(SlowMo, 1f, recover * recover);
            if (_camera != null) _camera.CinematicZoom = Mathf.Lerp(0.72f, 0.8f, recover);
            if (_stage == 100 && since >= BurpAt)                          // geğirme: şişip ağız açar, minik kırıntı bulutu
            {
                _stage = 101;
                Haptics.Impact(1);
                _blobGrowth?.PunchScale(0.3f);
                _blobVisual?.Gulp(0.22f);
                float s = _blob.lossyScale.x;
                Vector3 face = _blobVisual != null && _blobVisual.HasFacing ? _blobVisual.Forward : Vector3.back;
                Vector3 mouth = BlobCenter() + face * s * 0.45f + Vector3.up * s * 0.2f;
                Burst(new Bounds(mouth, Vector3.one * s * 0.5f), White, Metal, 0.6f, mouth + Vector3.up * 4f);
            }

            if (_time >= SwallowDuration)
            {
                Action done = _onDone;
                _onDone = null;
                Finish();
                done?.Invoke();
            }
        }

        private Vector3 BlobCenter() => _blob != null ? _blob.position + Vector3.up * _blob.lossyScale.y * 0.5f : _startPos;

        // ---------------------------------------------------------------- ortak

        private bool Begin(EnemyBase boss, GameObject prefab, Mode mode)
        {
            Action pending = _onDone;
            _onDone = null;
            Finish();
            pending?.Invoke(); // önceki sahne yarıda kaldıysa sonucu kaybolmasın

            Proxy proxy = GetProxy(boss, prefab);
            if (proxy == null) return false;
            SyncPose(proxy, boss);
            proxy.Root.SetPositionAndRotation(boss.transform.position, boss.transform.rotation);
            proxy.Root.localScale = boss.transform.lossyScale;
            proxy.Root.gameObject.SetActive(true);

            _active = proxy;
            _mode = mode;
            _bounds = CombinedBounds(proxy.Renderers);
            _startPos = proxy.Root.position;
            _startRot = proxy.Root.rotation;
            _startScale = proxy.Root.localScale;
            _rollSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            _time = 0f;
            _stage = 0;
            // Final boss'ta helikopter havada: kopya kökü gövdenin merkezine taşınır ki dönerken yerinde dönsün.
            if (mode == Mode.Swallow) RecenterOn(proxy, _bounds.center);
            _startPos = proxy.Root.position;
            return true;
        }

        private void Finish()
        {
            if (_mode == Mode.Swallow)
            {
                Time.timeScale = GameManager.Instance != null && GameManager.Instance.IsPlaying ? 1f : Time.timeScale;
                if (_camera != null) _camera.CinematicZoom = 1f;
            }
            if (_active != null)
            {
                foreach (Renderer r in _active.Renderers) r.SetPropertyBlock(null);
                _active.Root.gameObject.SetActive(false);
            }
            _active = null;
            _mode = Mode.None;
        }

        private void Burst(Bounds bounds, Color a, Color b, float sizeScale, Vector3 pullTo)
        {
            if (_crumbs == null) return;
            Vector3 s = bounds.size;
            float size = Mathf.Pow(Mathf.Max(1e-3f, s.x * s.y * s.z), 1f / 3f) * sizeScale;
            _crumbs[_nextCrumb].Play(bounds, a, b, pullTo, size);
            _nextCrumb = (_nextCrumb + 1) % _crumbs.Length;
        }

        private void SetTint(Color c)
        {
            foreach (Renderer r in _active.Renderers)
            {
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(_block);
            }
        }

        private static Bounds CombinedBounds(Renderer[] rs)
        {
            if (rs.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 2f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        private static void RecenterOn(Proxy p, Vector3 worldCenter)
        {
            Vector3 offset = worldCenter - p.Root.position;
            p.Root.position = worldCenter;
            foreach (Transform part in p.Parts) part.position -= offset;
        }

        // Sadece "Model" altı (varsa): gölge, roket/nişan dairesi gibi ek parçalar kopyalanmaz.
        private static IEnumerable<MeshRenderer> SourceRenderers(EnemyBase boss)
        {
            Transform model = boss.transform.Find("Model");
            Component root = model != null ? (Component)model : boss;
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || r.GetComponent<HeliShadow>() != null || r.name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) yield return r;
            }
        }

        // Boss'un görünür MeshRenderer'larının kopyası (bileşen/collider yok). Tip başına bir kez kurulur.
        private Proxy GetProxy(EnemyBase boss, GameObject prefab)
        {
            if (prefab == null) return null;
            if (_proxies.TryGetValue(prefab, out Proxy p)) return p;

            var root = new GameObject($"{prefab.name}_Death").transform;
            var renderers = new List<Renderer>();
            var parts = new List<Transform>();
            foreach (MeshRenderer src in SourceRenderers(boss))
            {
                var go = new GameObject(src.name);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = src.GetComponent<MeshFilter>().sharedMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterials = src.sharedMaterials;
                renderers.Add(r);
                parts.Add(go.transform);
            }
            root.gameObject.SetActive(false);
            p = new Proxy { Root = root, Renderers = renderers.ToArray(), Parts = parts.ToArray() };
            _proxies[prefab] = p;
            return p;
        }

        // Parçalar boss'un o anki pozuna (helikopter irtifası, eğimi, rotor açısı) göre yerleşir.
        private static void SyncPose(Proxy p, EnemyBase boss)
        {
            Transform b = boss.transform;
            int i = 0;
            foreach (MeshRenderer src in SourceRenderers(boss))
            {
                if (i >= p.Parts.Length) break;
                Transform t = src.transform, part = p.Parts[i++];
                part.localPosition = b.InverseTransformPoint(t.position);
                part.localRotation = Quaternion.Inverse(b.rotation) * t.rotation;
                Vector3 bs = b.lossyScale, ts = t.lossyScale;
                part.localScale = new Vector3(bs.x != 0f ? ts.x / bs.x : 1f, bs.y != 0f ? ts.y / bs.y : 1f, bs.z != 0f ? ts.z / bs.z : 1f);
            }
        }
    }
}
