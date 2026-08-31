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

        private Transform _blobTransform;
        private BlobGrowth _blobGrowth;
        private Vector3 _spawnCenter;
        private Vector3 _wanderTarget;
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
            }

            Move();
            ApplyBob();
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

        // Bina gibi engeller NavMesh'te delik açtığında rastgele hedef bir binanın içine denk
        // gelebilir — bulunamazsa mevcut hedef korunur, Move()'daki "hedefe ulaştı" kontrolü
        // zaten periyodik olarak bu metodu tekrar tetikliyor, bir sonraki denemede düzelir.
        private void PickNewWanderTarget()
        {
            Vector2 offset = Random.insideUnitCircle * PedData.WanderRadius;
            Vector3 candidate = _spawnCenter + new Vector3(offset.x, 0f, offset.y);

            if (SpawnPositionUtility.TryFindNavMeshPosition(candidate, 0f, 3f, out Vector3 valid))
                _wanderTarget = valid;
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
