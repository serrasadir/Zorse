using System;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Vehicles
{
    public class CarController : MonoBehaviour
    {
        public event Action<CarController> OnConsumed;

        private enum CarState { Moving, Stopped }

        private CarData _data;
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

        private bool IsCarAhead()
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _data.CarDetectionDistance))
                return hit.collider.GetComponent<CarController>() != null;
            return false;
        }

        private bool IsBlobDangerous()
        {
            if (_blobTransform == null || _blobGrowth == null) return false;
            float distSq = (transform.position - _blobTransform.position).sqrMagnitude;
            float radiusSq = _data.BlobDetectionRadius * _data.BlobDetectionRadius;
            if (distSq > radiusSq) return false;
            return _blobGrowth.CurrentTier >= _data.ConsumableFromTier;
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

        private void PushNearbyConsumables()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 1.2f, _pushHits);
            for (int i = 0; i < count; i++)
            {
                // 2026-11-03 (Serra: "önümde görünürde bir şey yok ama takılıyorum"): araba şehir objelerini de
                // itiyordu. Şehir objeleri static batching'de — görseli yerinde kalıyor, sadece collider kayıyordu
                // (itilen otobüs durağı sokağın ortasında görünmez duvar oldu). Vakumun çekebildiği (= taşınabilir)
                // küçük objeler itilir; şehir objeleri ve yayalar (IsVacuumable=false) itilmez.
                IConsumable consumable = _pushHits[i].GetComponent<IConsumable>();
                if (consumable == null || !consumable.IsVacuumable) continue;
                Transform t = _pushHits[i].transform;
                // Yatay it — eskiden Y de dahildi, araba merkezi alçakta olduğu için objeler havaya kalkıyordu.
                Vector3 pushDir = t.position - transform.position;
                pushDir.y = 0f;
                pushDir = pushDir.sqrMagnitude > 0.0001f ? pushDir.normalized : transform.right;
                // Artık her frame değil throttle tick'inde (AIUpdateInterval) çağrılıyor — mesafe buna
                // göre ölçeklendi, aksi halde itme eskisinin ~1/9'una düşerdi (Time.deltaTime tek
                // frame'lik, çağrı arası boşluğu hesaba katmazdı).
                t.position = Vector3.MoveTowards(t.position, t.position + pushDir, 8f * AIUpdateInterval);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive) return;

            BlobGrowth blobGrowth = collision.gameObject.GetComponentInParent<BlobGrowth>();
            if (blobGrowth == null) return;

            if (blobGrowth.CurrentTier >= _data.ConsumableFromTier)
            {
                blobGrowth.AddMass(_data.MassValue);
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

        private void Consume()
        {
            _isActive = false;
            OnConsumed?.Invoke(this);
        }
    }
}
