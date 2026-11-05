using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Entities.Enemies
{
    // Final boss alan saldırısı (issue #71): helikopter blob'un gideceği yere kırmızı daire çizer, daire dolarken
    // roket atar, dolduğu an patlar. Makineli serinin (RangedAttackState) yanında ayrı, bağımsız bir tehdit:
    // seri "yana kaç", roket "daireden çık" der. Telegraph mantığı elitin/tankın lazeriyle aynı: önce göster, sonra vur.
    // - Can azaldıkça (irtifa kademeleriyle aynı eşikler) aynı anda 1 → 2 → 3 roket; ekler hedefin çevresine düşer.
    // - Daire yarıçapı blob'la büyür: büyük blob küçük daireden hiç çıkamazdı.
    // - İsabet: patlama anında blob merkezi daire + blob yarıçapının bir kısmı içindeyse (kenarı sıyıran kurtulur).
    // Görseller (roket, daire, patlama) prefab'daki pasif şablonlardan kopyalanır ve sahne kökünde durur
    // (helikopterle birlikte kaymasın). Kurulum: CityKitPipeline.ApplyFinalBossHelicopter.
    [RequireComponent(typeof(EnemyBase))]
    public class HeliRocketStrike : MonoBehaviour
    {
        [Header("Şablonlar (pipeline bağlar, pasif)")]
        [SerializeField] private GameObject _rocketTemplate;   // Rocket > Body (mesh) + Flame (pivot nozulda)
        [SerializeField] private GameObject _markerTemplate;   // Marker > Ring + Fill (yere yatık quad)
        [SerializeField] private GameObject _blastTemplate;    // Blast > Flash + Wave (yere yatık quad)

        [Header("Zamanlama")]
        [SerializeField] private float _firstDelay = 6f;       // boss geldikten sonra ilk roket
        [SerializeField] private float _cooldown = 7f;         // bir yaylım bitince sonrakine kadar
        [SerializeField] private float _warnTime = 1.6f;       // daire görünür → patlama
        [SerializeField] private float _flightTime = 0.6f;     // roket uçuşu (uyarının son kısmı)
        [SerializeField] private float _volleyStagger = 0.3f;  // yaylımdaki roketler arası

        [Header("Alan")]
        [SerializeField] private float _baseRadius = 1.8f;
        [SerializeField] private float _radiusPerBlobRadius = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _hitBlobRadiusFactor = 0.35f;
        [SerializeField] private float _leadTime = 0.35f;      // blob'un gidişine göre öne nişan (sn)
        [SerializeField] private float _volleySpread = 2.2f;   // ek roketler hedeften yarıçapın bu katı uzağa
        [SerializeField] private float _maxRange = 16f;        // yatay; ekran dışındaki blob'a atmaz

        [Header("Hasar")]
        [SerializeField] private float _damage = 20f;          // makineli seri 36; tek roket bundan az, kaçılabilir

        private const float GroundLift = 0.17f;     // HeliShadow (0.15) üstü, z-fighting olmasın
        private const float BlastDuration = 0.4f;
        private const float RocketArcHeight = 1.5f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color RingColor = new Color(1f, 0.22f, 0.18f, 1f);
        private static readonly Color FillColor = new Color(1f, 0.22f, 0.18f, 0.32f);
        private static readonly Color FlashColor = new Color(1f, 0.72f, 0.28f, 0.95f);
        private static readonly Color WaveColor = new Color(1f, 0.95f, 0.82f, 0.85f);

        private class Strike
        {
            public GameObject Marker, Rocket, Blast;
            public Renderer Ring, Fill, Flash, Wave;
            public Transform Flame;
            public Vector3 Target, Launch;
            public float Radius, Time, BlastTime;
            public bool Active, Exploded;
        }

        private EnemyBase _enemy;
        private NavMeshAgent _agent;
        private CameraController _camera;
        private BlobHealth _blobHealth;
        private Rigidbody _blobBody;
        private Transform _blobRef;
        private Transform _holder;
        private MaterialPropertyBlock _block;
        private readonly List<Strike> _strikes = new List<Strike>();
        private float _nextVolleyTime;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _agent = GetComponent<NavMeshAgent>();
            _block = new MaterialPropertyBlock();
            foreach (GameObject t in new[] { _rocketTemplate, _markerTemplate, _blastTemplate })
                if (t != null) t.SetActive(false);
        }

        private void OnEnable()
        {
            _nextVolleyTime = Time.time + _firstDelay;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= HandleRestart;
            HideAll();
        }

        // Restart sahneyi yeniden yüklemez: havadaki roket/daire yeni run'a taşınmasın.
        private void HandleRestart(CharacterData _) => HideAll();

        private void HideAll()
        {
            foreach (Strike s in _strikes) Hide(s);
        }

        private void OnDestroy()
        {
            if (_holder != null) Destroy(_holder.gameObject);
        }

        private void Update()
        {
            if (_rocketTemplate == null || _markerTemplate == null || _blastTemplate == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // duraklatma / level-up

            Transform blob = _enemy.BlobTransform;
            if (blob != _blobRef)
            {
                _blobRef = blob;
                _blobHealth = blob != null ? blob.GetComponent<BlobHealth>() : null;
                _blobBody = blob != null ? blob.GetComponent<Rigidbody>() : null;
            }

            if (blob != null && Time.time >= _nextVolleyTime && _enemy.FlatSqrDistanceToBlob() <= _maxRange * _maxRange)
                StartVolley(blob);

            foreach (Strike s in _strikes)
                if (s.Active) Tick(s, dt);
        }

        private void StartVolley(Transform blob)
        {
            float fraction = _enemy.MaxHealth > 0f ? _enemy.CurrentHealth / _enemy.MaxHealth : 1f;
            int count = fraction > 0.75f ? 1 : fraction > 0.5f ? 2 : 3;

            Vector3 velocity = _blobBody != null ? _blobBody.linearVelocity : Vector3.zero;
            velocity.y = 0f;
            Vector3 center = blob.position + velocity * _leadTime;
            float blobRadius = BlobRadius(blob);
            float radius = _baseRadius + blobRadius * _radiusPerBlobRadius;
            center.y = GroundY();

            float angle = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                Vector3 target = center;
                if (i > 0)
                {
                    // Ekler ana dairenin iki yanına (yelpaze), blob'un kaçış yollarını daraltır ama kapatmaz.
                    float a = (angle + (i == 1 ? 0f : 180f)) * Mathf.Deg2Rad;
                    target += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * _volleySpread;
                }
                Strike s = Acquire();
                s.Target = target;
                s.Radius = radius;
                s.Time = -i * _volleyStagger;
                s.Exploded = false;
                s.Active = true;
            }
            _nextVolleyTime = Time.time + _cooldown + (count - 1) * _volleyStagger + _warnTime;
        }

        private void Tick(Strike s, float dt)
        {
            s.Time += dt;
            if (s.Time < 0f) return; // yaylımda sırasını bekliyor

            if (!s.Exploded)
            {
                float k = Mathf.Clamp01(s.Time / _warnTime);
                if (!s.Marker.activeSelf)
                {
                    s.Marker.transform.position = s.Target;
                    s.Marker.SetActive(true);
                }
                // Halka sabit boyda, nabız gibi atar (son anda hızlanır); iç disk dolarak patlamaya sayar.
                float pulse = 0.65f + 0.35f * Mathf.Sin(s.Time * Mathf.Lerp(8f, 22f, k));
                s.Ring.transform.localScale = Vector3.one * (s.Radius * 2f);
                SetColor(s.Ring, RingColor, pulse);
                s.Fill.transform.localScale = Vector3.one * (Mathf.Max(0.01f, k) * s.Radius * 2f);
                SetColor(s.Fill, FillColor, 1f);

                float launchAt = _warnTime - _flightTime;
                if (s.Time >= launchAt)
                {
                    if (!s.Rocket.activeSelf)
                    {
                        s.Launch = _enemy.AimPoint;
                        s.Rocket.SetActive(true);
                    }
                    UpdateRocket(s, Mathf.Clamp01((s.Time - launchAt) / _flightTime));
                }

                if (s.Time >= _warnTime) Explode(s);
                return;
            }

            s.BlastTime += dt;
            float b = s.BlastTime / BlastDuration;
            if (b >= 1f)
            {
                Hide(s);
                return;
            }
            float ease = 1f - (1f - b) * (1f - b);
            s.Flash.transform.localScale = Vector3.one * (s.Radius * 2f * Mathf.Lerp(0.6f, 1.05f, ease));
            SetColor(s.Flash, FlashColor, 1f - b);
            s.Wave.transform.localScale = Vector3.one * (s.Radius * 2f * Mathf.Lerp(0.8f, 1.6f, ease));
            SetColor(s.Wave, WaveColor, 1f - b);
        }

        // Yay: helikopterden önce biraz yukarı-ileri çıkar, sonra hedefe dalar. Burun teğete bakar.
        private void UpdateRocket(Strike s, float t)
        {
            Vector3 control = Vector3.Lerp(s.Launch, s.Target, 0.35f) + Vector3.up * RocketArcHeight;
            Vector3 p = Bezier(s.Launch, control, s.Target, t);
            Vector3 tangent = Bezier(s.Launch, control, s.Target, Mathf.Min(1f, t + 0.02f)) - p;
            Transform rocket = s.Rocket.transform;
            rocket.position = p;
            if (tangent.sqrMagnitude > 1e-6f) rocket.rotation = Quaternion.LookRotation(tangent);
            if (s.Flame != null)
                s.Flame.localScale = new Vector3(1f, 1f, 0.75f + 0.35f * Mathf.PerlinNoise(Time.time * 18f, s.Radius));
        }

        private void Explode(Strike s)
        {
            s.Exploded = true;
            s.BlastTime = 0f;
            s.Marker.SetActive(false);
            s.Rocket.SetActive(false);
            s.Blast.transform.position = s.Target;
            s.Blast.SetActive(true);

            Transform blob = _enemy.BlobTransform;
            if (blob == null) return;
            Vector3 d = blob.position - s.Target;
            d.y = 0f;
            float blobRadius = BlobRadius(blob);
            float reach = s.Radius + blobRadius * _hitBlobRadiusFactor;
            bool hit = d.sqrMagnitude <= reach * reach;
            if (hit) _blobHealth?.TakeDamage(_damage);

            if (_camera == null) _camera = FindAnyObjectByType<CameraController>();
            float near = s.Radius * 3f + blobRadius;
            if (_camera != null && d.sqrMagnitude <= near * near) _camera.Shake(hit ? 0.2f : 0.1f, 0.25f);
        }

        private Strike Acquire()
        {
            foreach (Strike s in _strikes)
                if (!s.Active) return s;

            if (_holder == null) _holder = new GameObject("HeliRocketFX").transform;
            var strike = new Strike
            {
                Marker = Instantiate(_markerTemplate, _holder),
                Rocket = Instantiate(_rocketTemplate, _holder),
                Blast = Instantiate(_blastTemplate, _holder),
            };
            strike.Ring = strike.Marker.transform.Find("Ring").GetComponent<Renderer>();
            strike.Fill = strike.Marker.transform.Find("Fill").GetComponent<Renderer>();
            strike.Flash = strike.Blast.transform.Find("Flash").GetComponent<Renderer>();
            strike.Wave = strike.Blast.transform.Find("Wave").GetComponent<Renderer>();
            strike.Flame = strike.Rocket.transform.Find("Flame");
            _strikes.Add(strike);
            return strike;
        }

        private static void Hide(Strike s)
        {
            s.Active = false;
            if (s.Marker != null) s.Marker.SetActive(false);
            if (s.Rocket != null) s.Rocket.SetActive(false);
            if (s.Blast != null) s.Blast.SetActive(false);
        }

        private void SetColor(Renderer renderer, Color color, float alphaFactor)
        {
            color.a *= Mathf.Clamp01(alphaFactor);
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(_block);
        }

        // Kök NavMeshAgent baseOffset kadar zeminin üstünde (bkz. CityKitPipeline: Model y = -baseOffset).
        private float GroundY() => transform.position.y - (_agent != null ? _agent.baseOffset : 0f) + GroundLift;

        private static float BlobRadius(Transform blob) => blob.localScale.x * 0.5f;

        private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
