using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Pedestrians
{
    // M24 (GDD_v2.md Karar 14): ConsumableBase'i miras alır — IConsumable/layer-atama/pool-return
    // zaten hazır (bkz. ConsumableBase.ReturnToOwner override'ı), burada sadece wander/kaçış hareketi
    // ve animasyonsuz bob eklendi. NavMeshAgent/SwarmSteering KULLANMAZ — SwarmSteering EnemyBase'e
    // sıkı bağlı (Register(EnemyBase)) olduğu için genelleştirmek dev-a dosyasına dokunmayı gerektirirdi;
    // yayalar ayrıca birbirinden kaçınmaya (separation) ihtiyaç duymuyor, bağımsız wander yeterli.
    public class PedestrianController : ConsumableBase
    {
        private const float AIUpdateInterval = 0.4f;

        // 2026-08-31: NavMesh hedef seçimi tek başına yetmedi — WanderRadius (8) blok aralığından
        // (12) küçük olduğu için bazı yayalar binalarla her yönden kuşatılabiliyor, o zaman HİÇBİR
        // rastgele hedef bulunamaz (şans değil, geometri meselesi). EnemyBase.ComputeAvoidance ile
        // aynı teknik: throttle'lı raycast, yol üstünde engel varsa ondan uzaklaştıran bir sapma
        // ekler — hedefin kusursuz olmasına gerek kalmaz, yaya engelin etrafından "sıyrılır".
        [SerializeField] private float _avoidanceLookahead = 1.2f;
        [SerializeField] private float _avoidanceWeight = 2f;

        private static int s_environmentMask = -1;

        private Transform _blobTransform;
        private BlobGrowth _blobGrowth;
        private Vector3 _spawnCenter;
        private Vector3 _wanderTarget;
        private Vector3 _avoidanceCached;
        private float _aiTimer;
        private float _bobTimer;
        private bool _fleeing;
        private bool _wanderTargetSet;

        private PedestrianData PedData => Data as PedestrianData;

        protected override void OnEnable()
        {
            base.OnEnable();

            _aiTimer = Random.Range(0f, AIUpdateInterval);
            _bobTimer = Random.Range(0f, Mathf.PI * 2f);
            _fleeing = false;
            _wanderTargetSet = false;
            _spawnCenter = transform.position;

            if (_blobTransform == null)
            {
                GameObject blob = GameObject.FindWithTag("Blob");
                if (blob != null)
                {
                    _blobTransform = blob.transform;
                    _blobGrowth = blob.GetComponent<BlobGrowth>();
                }
            }

            // PickNewWanderTarget() burada değil Update()'te çağrılıyor: spawner Get()'ten (bu OnEnable'ı
            // tetikler) SONRA SetData() çağırıyor, yani PedData ilk aktivasyonda hâlâ null olabilir.
        }

        private void Update()
        {
            if (PedData == null) return;

            if (!_wanderTargetSet)
            {
                _wanderTargetSet = true;
                PickNewWanderTarget();
            }

            _aiTimer -= Time.deltaTime;
            if (_aiTimer <= 0f)
            {
                _aiTimer = AIUpdateInterval;
                UpdateFleeState();
                _avoidanceCached = ComputeAvoidance();
            }

            Move();
            ApplyBob();
        }

        private Vector3 ComputeAvoidance()
        {
            if (s_environmentMask < 0)
                s_environmentMask = LayerMask.GetMask("Environment");

            Vector3 forward = _fleeing && _blobTransform != null
                ? transform.position - _blobTransform.position
                : _wanderTarget - transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return Vector3.zero;
            forward.Normalize();

            if (!Physics.Raycast(transform.position, forward, out RaycastHit hit, _avoidanceLookahead, s_environmentMask))
                return Vector3.zero;

            Vector3 away = Vector3.Cross(Vector3.up, hit.normal);
            if (Vector3.Dot(away, forward) < 0f) away = -away;
            return away;
        }

        private void UpdateFleeState()
        {
            if (_blobTransform == null || _blobGrowth == null) return;

            bool canBeEaten = _blobGrowth.CurrentTier >= RequiredTier;
            float sqrDist = (transform.position - _blobTransform.position).sqrMagnitude;
            bool wasFleeing = _fleeing;
            _fleeing = canBeEaten && sqrDist <= PedData.FleeTriggerDistance * PedData.FleeTriggerDistance;

            if (!_fleeing && wasFleeing)
                PickNewWanderTarget();
        }

        private const int WanderTargetMaxAttempts = 5;

        // Bina gibi engeller NavMesh'te delik açtığında rastgele hedef bir binanın içine denk
        // gelebilir. İLK VERSİYONDA (2026-08-31) bulunamayınca _wanderTarget hiç güncellenmiyordu —
        // yaya zaten o hedefe ulaşmış olduğu için "hedefe ulaştım -> yeni hedef seç -> yine
        // başarısız" döngüsüne girip kalıcı olarak yerinde sayıyordu (Move() her frame erken
        // dönüyor ama ApplyBob() ondan sonra koşulsuz çağrıldığı için bob animasyonu devam
        // ediyordu — Serra'nın gözlemlediği "yürümeyi bıraktı ama animasyon sürdü" bug'ı buydu).
        // Düzeltme: birkaç kez dene, hepsi başarısız olursa spawn noktasına dön (spawn anında
        // zaten NavMesh'te doğrulanmıştı) — _wanderTarget her çağrıda kesin bir değer alır.
        private void PickNewWanderTarget()
        {
            for (int i = 0; i < WanderTargetMaxAttempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * PedData.WanderRadius;
                Vector3 candidate = _spawnCenter + new Vector3(offset.x, 0f, offset.y);

                if (SpawnPositionUtility.TryFindNavMeshPosition(candidate, 0f, 3f, out Vector3 valid))
                {
                    _wanderTarget = valid;
                    return;
                }
            }

            _wanderTarget = _spawnCenter;
        }

        private void Move()
        {
            Vector3 direction;
            float speed;

            if (_fleeing)
            {
                direction = transform.position - _blobTransform.position;
                speed = PedData.FleeSpeed;
            }
            else
            {
                direction = _wanderTarget - transform.position;
                if (direction.sqrMagnitude < 0.04f)
                {
                    PickNewWanderTarget();
                    return;
                }
                speed = PedData.MoveSpeed;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            direction.Normalize();

            Vector3 blended = direction + _avoidanceCached * _avoidanceWeight;
            if (blended.sqrMagnitude > 0.0001f)
                direction = blended.normalized;

            transform.position += direction * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void ApplyBob()
        {
            _bobTimer += Time.deltaTime * PedData.BobSpeed;
            Vector3 pos = transform.position;
            pos.y = Data.SpawnYOffset + Mathf.Abs(Mathf.Sin(_bobTimer)) * PedData.BobHeight;
            transform.position = pos;
        }

        public override void ReturnToOwner() => PedestrianSpawner.Instance?.ReturnToPool(this);
    }
}
