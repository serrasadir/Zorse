using System;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Vehicles
{
    public class CarController : MonoBehaviour
    {
        public event Action<CarController> OnConsumed;

        private enum CarState { Moving, Stopped }

        private CarData _data;
        // Araç tipine göre tier/kütle (boya göre "Tier'ları Boya Göre Kur" yazar); 0 = CarData'daki değer.
        [SerializeField] private int _tierOverride;
        [SerializeField] private float _massOverride;
        private BlobTier ConsumeTier => _tierOverride > 0 ? (BlobTier)_tierOverride : _data.ConsumableFromTier;
        private float MassValue => _massOverride > 0f ? _massOverride : _data.MassValue;
        // 2026-11-03: rota artık Transform değil nokta listesi — CarSpawner büyük şehrin sokak ızgarasından
        // runtime'da üretiyor. Araba kendi tamponunu tutar, spawner'ın geçici dizisi yeniden kullanılabilir.
        private Vector3[] _waypoints = new Vector3[0];
        private int _waypointCount;
        // İki araba kavşakta birbirinin önünü raycast'le kesip sonsuza kadar bekleyebilir — bu kadar
        // bekledikten sonra kısa süreliğine "önümde araba var" kontrolü yok sayılır.
        private const float CarBlockGiveUpTime = 2f;
        private const float CarBlockIgnoreTime = 1f;
        private float _blockedByCarTime;
        private float _ignoreCarsUntil;
        private int _currentWaypointIndex;
        private float _damageTimer;
        private bool _isActive;
        private CarState _state = CarState.Moving;
        private float _currentSpeed;

        // Cached references — FindAnyObjectByType'ı runtime'da çağırmamak için
        private Transform _blobTransform;
        private BlobGrowth _blobGrowth;
        private ScoreSystem _scoreSystem;

        // Throttle: physics sorguları her frame değil, belirli aralıkla
        private float _aiUpdateTimer;
        private const float AIUpdateInterval = 0.15f;
        private bool _cachedIsCarAhead;
        private bool _cachedIsBlobDangerous;

        private readonly Collider[] _pushHits = new Collider[16];

        // 2026-11-04 (Serra: "yayalar bir şeylerin içinden geçiyor"): önündeki kontrol eskiden ince, maskesiz tek
        // ışındı ve sadece arabayı tanıyordu (yayanın, polisin içinden geçiyordu). Artık kendi şeridi genişliğinde
        // süpürme: önünde araba, yaya ya da (yerde yürüyen) polis varsa durur. Karşı şerit (2 birim yanda) ve
        // kaldırım süpürmeye girmez. Yarıçap dar tutuldu ki yan kaldırımdaki yaya yüzünden durmasın.
        private const float AheadSweepRadius = 0.5f;
        private const float StopMargin = 2f; // 6 hız / 15 fren ≈ 1.2 duruş mesafesi + pay
        // Hazard(15, arabalar) + Enemy(14) + ConsumableTier1-5(9-13, yayalar bu katmanlarda)
        private const int AheadMask = (1 << 15) | (1 << 14) | TierLayers.ConsumableMask;
        private readonly RaycastHit[] _aheadHits = new RaycastHit[8];
        private float _halfLength = 1f;

        // 2026-11-04 (Serra: "elit polisler bir şeylerin içinden geçmemeli"): NavMeshAgent'lı düşmanlar (elit,
        // boss) fizik collider'larına bakmaz, hareketli araba NavMesh'e bake edilemez. Oymayan (carve=false)
        // NavMeshObstacle: ajanların yerel kaçınması arabanın etrafından dolanır, NavMesh yeniden hesaplanmaz (ucuz).
        // Prefab başına elle kurmamak için havuzdaki her araba ilk uyanışta bir kez ekler.
        private void Awake()
        {
            BoxCollider box = GetComponentInChildren<BoxCollider>();
            if (box != null) _halfLength = box.size.z * box.transform.lossyScale.z * 0.5f;
            if (box == null || box.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null) return;

            UnityEngine.AI.NavMeshObstacle obstacle = box.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.center = box.center;
            obstacle.size = box.size;
            obstacle.carving = false;
        }

        public void Initialize(CarData data, Vector3[] waypoints, int count)
        {
            _data = data;
            if (_waypoints.Length < count) _waypoints = new Vector3[count];
            for (int i = 0; i < count; i++) _waypoints[i] = waypoints[i];
            _waypointCount = count;
            _currentWaypointIndex = count > 1 ? 1 : 0; // 0. köşede doğar, 1.'ye doğru gider
            _blockedByCarTime = 0f;
            _ignoreCarsUntil = 0f;
            _damageTimer = 0f;
            _isActive = true;
            _state = CarState.Moving;
            _currentSpeed = data.MoveSpeed;
            _aiUpdateTimer = UnityEngine.Random.Range(0f, AIUpdateInterval); // stagger — hepsi aynı anda çalışmasın

            if (_blobTransform == null)
            {
                GameObject blob = GameObject.FindWithTag("Blob");
                if (blob != null)
                {
                    _blobTransform = blob.transform;
                    _blobGrowth = blob.GetComponent<BlobGrowth>();
                }
            }

            if (_scoreSystem == null)
                _scoreSystem = FindAnyObjectByType<ScoreSystem>();

            if (_waypointCount > 0)
            {
                transform.position = new Vector3(_waypoints[0].x, _data.SpawnYOffset, _waypoints[0].z);
                Vector3 heading = _waypointCount > 1 ? _waypoints[1] - _waypoints[0] : Vector3.forward;
                heading.y = 0f;
                transform.rotation = heading.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(heading) : Quaternion.identity;
            }
        }

        private void Update()
        {
            if (!_isActive || _waypointCount == 0) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            // AI sorgularını throttle et
            _aiUpdateTimer -= Time.deltaTime;
            bool aiTick = _aiUpdateTimer <= 0f;
            if (aiTick)
            {
                _aiUpdateTimer = AIUpdateInterval;
                _cachedIsCarAhead = Time.time >= _ignoreCarsUntil && IsCarAhead();
                _cachedIsBlobDangerous = IsBlobDangerous();
            }

            if (_cachedIsCarAhead)
            {
                _blockedByCarTime += Time.deltaTime;
                if (_blockedByCarTime >= CarBlockGiveUpTime)
                {
                    _blockedByCarTime = 0f;
                    _ignoreCarsUntil = Time.time + CarBlockIgnoreTime;
                    _cachedIsCarAhead = false;
                }
            }
            else _blockedByCarTime = 0f;

            UpdateState();
            UpdateSpeed();

            if (_currentSpeed > 0.01f)
            {
                MoveTowardsWaypoint();
                // 2026-09-06 (Serra): her frame OverlapSphere çalışıyordu (CLAUDE.md kuralına aykırı) —
                // aynı throttle tick'ine bağlandı.
                if (aiTick) PushNearbyConsumables();
            }

            _damageTimer -= Time.deltaTime;
        }

        private void UpdateState()
        {
            _state = (_cachedIsCarAhead || _cachedIsBlobDangerous) ? CarState.Stopped : CarState.Moving;
        }

        // Önde araba, yaya ya da polis var mı (isim tarihsel — artık sadece araba değil).
        private bool IsCarAhead()
        {
            float distance = Mathf.Max(_data.CarDetectionDistance, _halfLength + StopMargin);
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            int count = Physics.SphereCastNonAlloc(origin, AheadSweepRadius, transform.forward, _aheadHits, distance,
                AheadMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider c = _aheadHits[i].collider;
                if (c == null || c.transform.IsChildOf(transform)) continue;
                CarController car = c.GetComponentInParent<CarController>();
                if (car != null && car != this) return true;
                if (c.GetComponentInParent<Pedestrians.PedestrianController>() != null) return true;
                EnemyBase enemy = c.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.AirborneHeight <= 0f) return true; // helikopterin altından geçer
            }
            return false;
        }

        private bool IsBlobDangerous()
        {
            if (_blobTransform == null || _blobGrowth == null) return false;
            float distSq = (transform.position - _blobTransform.position).sqrMagnitude;
            float radiusSq = _data.BlobDetectionRadius * _data.BlobDetectionRadius;
            if (distSq > radiusSq) return false;
            return _blobGrowth.CurrentTier >= ConsumeTier;
        }

        private void UpdateSpeed()
        {
            float targetSpeed = _state == CarState.Moving ? _data.MoveSpeed : 0f;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _data.BrakeSpeed * Time.deltaTime);
        }

        private void MoveTowardsWaypoint()
        {
            Vector3 target = _waypoints[_currentWaypointIndex];

            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(target.x, 0f, target.z);
            Vector3 direction = (targetPos - myPos).normalized;

            transform.position += direction * _currentSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);

            if ((myPos - targetPos).sqrMagnitude <= _data.WaypointReachDistance * _data.WaypointReachDistance)
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypointCount;
        }

        private const int ConsumableMask = TierLayers.ConsumableMask;

        // Önündeki küçük objeleri yana savurur (ConsumableKnock kayarak taşır). Şehir objeleri static
        // batching'de, yayalar kendi yürür — ikisi de IsVacuumable=false, itilmez.
        private void PushNearbyConsumables()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 1.2f, _pushHits, ConsumableMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                IConsumable consumable = _pushHits[i].GetComponent<IConsumable>();
                if (consumable == null || !consumable.IsVacuumable) continue;
                Transform t = _pushHits[i].transform;
                if (!t.TryGetComponent(out ConsumableKnock knock))
                    knock = t.gameObject.AddComponent<ConsumableKnock>(); // obje başına bir kez (havuzda kalır)
                if (knock.IsSliding) continue;

                Vector3 toItem = t.position - transform.position;
                float side = Vector3.Dot(toItem, transform.right) >= 0f ? 1f : -1f;
                Vector3 dir = (transform.right * side * 0.8f + transform.forward * 0.6f).normalized;
                float speed = Mathf.Clamp(_currentSpeed * 0.9f, 4f, 9f);
                knock.Knock(dir * speed);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive) return;

            BlobGrowth blobGrowth = collision.gameObject.GetComponentInParent<BlobGrowth>();
            if (blobGrowth == null) return;

            if (blobGrowth.CurrentTier >= ConsumeTier)
            {
                blobGrowth.AddMass(MassValue);
                _scoreSystem?.AddScore(_data.ScoreValue);
                Consume();
            }
            else
            {
                if (_damageTimer > 0f) return;
                collision.gameObject.GetComponentInParent<BlobHealth>()?.TakeDamage(_data.DamageAmount);
                _damageTimer = _data.DamageCooldown;
            }
        }

        // TankCrusher: miniboss tankı üstünden geçince araba havuza döner (OnConsumed → CarSpawner).
        public void Crush()
        {
            if (_isActive) Consume();
        }

        private void Consume()
        {
            _isActive = false;
            OnConsumed?.Invoke(this);
        }
    }
}
