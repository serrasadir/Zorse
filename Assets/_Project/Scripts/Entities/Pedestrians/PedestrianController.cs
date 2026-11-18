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
        // 2026-09-06 (Serra): "yaya binaların içinden geçememeli" — EnemyBase.ClampStepAgainstEnvironment
        // ile birebir aynı teknik (bkz. oradaki not): yumuşak sapma (avoidance) yetmediği köşe/dar açı
        // durumlarında her frame çalışan sert bir SphereCast bloğu. Yayanın gerçek bir collider yarıçapı
        // olmadığı için (trigger consumable collider'ı bu işe uygun değil) küçük bir sabit kullanılıyor.
        [SerializeField] private float _bodyRadius = 0.4f;

        private static int s_walkMask = -1;

        private Transform _blobTransform;
        private BlobGrowth _blobGrowth;
        private Vector3 _spawnCenter;
        private Vector3 _wanderTarget;
        private Vector3 _avoidanceCached;
        private float _aiTimer;
        private float _bobTimer;
        private bool _fleeing;
        private bool _wanderTargetSet;

        // 2026-11-03 (Serra: "yayalar duvar kenarında durup bekliyor"): hedefe ulaşamayan yaya yeni hedef
        // seçmiyordu. Bu süre içinde bu kadar ilerleyemezse takılmış sayılır ve yeni hedef seçer.
        private const float StuckCheckInterval = 1.2f;
        private const float StuckMinProgress = 0.3f;
        private float _stuckTimer;
        private Vector3 _stuckCheckPos;
        // Hiç açık hedef bulunamazsa her kare yeniden aramasın (NavMesh + SphereCast); kısa bir bekleme.
        private const float RetryPickDelay = 0.6f;
        private float _retryPickAt;

        private MeshFilter _modelFilter;
        private Renderer _modelRenderer; // ekran dışındayken kol-bacak sallanmaz

        // Parçalı görünüm (PedestrianRig): bacak/kollar Model'in child'ı, eklemde sallanır. Havuzdaki her
        // yaya için bir kez kurulur; rig'siz varyantta kapalı.
        private Transform _legL, _legR, _armL, _armR;
        private MeshFilter _legLF, _legRF, _armLF, _armRF;
        private PedestrianRig _rig;
        private float _walkPhase, _walkAmount;
        private Vector3 _lastWalkPos;

        // Bizden küçük blob çarpınca tekme: blob'a döner, geriye yaslanıp öne savrulur, kısa durur.
        private const float KickDuration = 0.4f;
        private float _kickTime = -1f;
        private Vector3 _kickToward;

        private PedestrianData PedData => Data as PedestrianData;

        // 2026-09-06 (Serra): yayalar dünyayı canlandıran arka plan NPC'leri — Vakum'un onları
        // ekrandan öbür ucundan çekip getirmesi istenmiyor, sadece yürüyerek/kaçarak hareket etmeliler.
        public override bool IsVacuumable => false;

        protected override void OnEnable()
        {
            base.OnEnable();

            _aiTimer = Random.Range(0f, AIUpdateInterval);
            _bobTimer = Random.Range(0f, Mathf.PI * 2f);
            _fleeing = false;
            _wanderTargetSet = false;
            _spawnCenter = transform.position;
            _stuckTimer = StuckCheckInterval;
            _stuckCheckPos = transform.position;
            _retryPickAt = 0f;
            _kickTime = -1f;
            _walkPhase = Random.Range(0f, Mathf.PI * 2f);
            _walkAmount = 0f;
            _lastWalkPos = transform.position;

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
                ApplyRandomVariant(); // SetData'dan sonraki ilk kare — PedData artık dolu (bkz. OnEnable notu)
                PickNewWanderTarget();
            }

            _aiTimer -= Time.deltaTime;
            if (_aiTimer <= 0f)
            {
                _aiTimer = AIUpdateInterval;
                UpdateFleeState();
                _avoidanceCached = ComputeAvoidance();
            }

            if (_kickTime >= 0f)
            {
                UpdateKick();
                AnimateLimbs();
                return;
            }

            Move();
            ApplyBob();
            AnimateLimbs();
            CheckStuck();
        }

        public void PlayKick(Vector3 blobPosition)
        {
            _kickToward = blobPosition - transform.position;
            _kickToward.y = 0f;
            if (_kickToward.sqrMagnitude < 0.0001f) _kickToward = transform.forward;
            _kickTime = 0f;
        }

        private void UpdateKick()
        {
            _kickTime += Time.deltaTime;
            float t = _kickTime / KickDuration;
            Quaternion face = Quaternion.LookRotation(_kickToward.normalized);
            if (t >= 1f)
            {
                _kickTime = -1f;
                transform.rotation = face;
                _stuckTimer = StuckCheckInterval;
                _stuckCheckPos = transform.position;
                return;
            }
            // Önce geriye yaslanır (ayak kalkar), sonra öne savrulur, dik duruşa döner.
            float lean = t < 0.35f ? -35f * (t / 0.35f) : Mathf.Lerp(-35f, 20f, Mathf.Sin((t - 0.35f) / 0.65f * Mathf.PI));
            if (t > 0.75f) lean *= 1f - (t - 0.75f) / 0.25f;
            transform.rotation = face * Quaternion.Euler(lean, 0f, 0f);
        }

        // Görsel çeşitlilik: PedestrianData.VariantMeshes'ten rastgele biri (havuzdan her çıkışta yeniden).
        private void ApplyRandomVariant()
        {
            Mesh[] variants = PedData.VariantMeshes;
            if (variants == null || variants.Length == 0) return;
            if (_modelFilter == null) _modelFilter = GetComponentInChildren<MeshFilter>();
            if (_modelFilter == null) return;
            if (_modelRenderer == null) _modelRenderer = _modelFilter.GetComponent<Renderer>();
            Mesh mesh = variants[Random.Range(0, variants.Length)];
            if (mesh == null) return;

            _rig = PedData.FindRig(mesh);
            _modelFilter.sharedMesh = _rig != null ? _rig.Body : mesh;
            ApplyRig();
        }

        private void ApplyRig()
        {
            if (_rig == null)
            {
                if (_legL != null) SetLimbsActive(false);
                return;
            }
            if (_legL == null) BuildLimbs();
            SetLimb(_legL, _legLF, _rig.LegL, _rig.HipL);
            SetLimb(_legR, _legRF, _rig.LegR, _rig.HipR);
            SetLimb(_armL, _armLF, _rig.ArmL, _rig.ShoulderL);
            SetLimb(_armR, _armRF, _rig.ArmR, _rig.ShoulderR);
        }

        // Parçası olmayan uzuv (kolu gövdede kalan iri yaya) kapalı kalır.
        private static void SetLimb(Transform limb, MeshFilter filter, Mesh mesh, Vector3 joint)
        {
            limb.gameObject.SetActive(mesh != null);
            filter.sharedMesh = mesh;
            limb.localPosition = joint;
            limb.localRotation = Quaternion.identity;
        }

        private void SetLimbsActive(bool active)
        {
            _legL.gameObject.SetActive(active);
            _legR.gameObject.SetActive(active);
            _armL.gameObject.SetActive(active);
            _armR.gameObject.SetActive(active);
        }

        // Model'in altına dört parça (aynı materyal ve katman). Havuzdaki yaya başına bir kez.
        private void BuildLimbs()
        {
            Transform model = _modelFilter.transform;
            MeshRenderer source = _modelFilter.GetComponent<MeshRenderer>();
            _legL = NewLimb("LegL", model, source, out _legLF);
            _legR = NewLimb("LegR", model, source, out _legRF);
            _armL = NewLimb("ArmL", model, source, out _armLF);
            _armR = NewLimb("ArmR", model, source, out _armRF);
        }

        private static Transform NewLimb(string name, Transform parent, MeshRenderer source, out MeshFilter filter)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            if (source != null)
            {
                renderer.sharedMaterials = source.sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // gövdenin gölgesi yeter
                renderer.receiveShadows = source.receiveShadows;
            }
            return go.transform;
        }

        // Adım: gidilen yola bağlı faz; hız arttıkça açı büyür, dururken eklemler yavaşça düzleşir.
        // Bacaklar karşılıklı, kollar karşı bacakla birlikte.
        private void AnimateLimbs()
        {
            if (_rig == null || _legL == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 delta = transform.position - _lastWalkPos;
            _lastWalkPos = transform.position;
            delta.y = 0f;
            float distance = _kickTime >= 0f ? 0f : delta.magnitude;
            float speed = distance / dt;
            _walkPhase += distance * PedData.StepRate;
            float target = Mathf.Clamp01(speed / Mathf.Max(0.1f, PedData.MoveSpeed));
            _walkAmount = Mathf.MoveTowards(_walkAmount, target, dt * 6f);
            if (_modelRenderer != null && !_modelRenderer.isVisible) return; // ekran dışı: faz ilerler, poz yazılmaz

            float s = Mathf.Sin(_walkPhase) * _walkAmount;
            Vector3 axis = _rig.SwingAxis;
            _legL.localRotation = Quaternion.AngleAxis(s * PedData.LegSwing, axis);
            _legR.localRotation = Quaternion.AngleAxis(-s * PedData.LegSwing, axis);
            _armL.localRotation = Quaternion.AngleAxis(-s * PedData.ArmSwing, axis);
            _armR.localRotation = Quaternion.AngleAxis(s * PedData.ArmSwing, axis);
        }

        private void CheckStuck()
        {
            _stuckTimer -= Time.deltaTime;
            if (_stuckTimer > 0f) return;
            _stuckTimer = StuckCheckInterval;

            Vector3 moved = transform.position - _stuckCheckPos;
            moved.y = 0f;
            _stuckCheckPos = transform.position;
            if (moved.sqrMagnitude < StuckMinProgress * StuckMinProgress && !_fleeing)
                PickNewWanderTarget();
        }

        private Vector3 ComputeAvoidance()
        {
            if (s_walkMask < 0)
                s_walkMask = LayerMask.GetMask("Environment", "PropObstacle", "Hazard"); // 2026-11-04: sokak eşyaları + arabalar da

            Vector3 forward = _fleeing && _blobTransform != null
                ? transform.position - _blobTransform.position
                : _wanderTarget - transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return Vector3.zero;
            forward.Normalize();

            // Gövde kalınlığında süpürme: ince ışın bina köşesini ıskalıyor, gövde ise çarpıp kilitleniyordu.
            if (!Physics.SphereCast(transform.position, _bodyRadius, forward, out RaycastHit hit, _avoidanceLookahead, s_walkMask))
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
        // gelebilir — birkaç kez dene, hepsi başarısız olursa spawn noktasına dön (spawn anında
        // zaten NavMesh'te doğrulanmıştı). yOverride olarak _spawnCenter.y kullanılıyor (0f DEĞİL —
        // 2026-08-31'de burada 0f sabitlemek gerçek kök sebepti, bkz. Move()'daki not).
        private void PickNewWanderTarget()
        {
            if (Time.time < _retryPickAt) return;

            for (int i = 0; i < WanderTargetMaxAttempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * PedData.WanderRadius;
                Vector3 candidate = _spawnCenter + new Vector3(offset.x, 0f, offset.y);

                if (SpawnPositionUtility.TryFindNavMeshPosition(candidate, _spawnCenter.y, 3f, out Vector3 valid)
                    && HasClearPath(valid))
                {
                    _wanderTarget = valid;
                    return;
                }
            }

            // Hiçbiri tutmadı: dönüş yolu açıksa spawn noktası, değilse olduğu yerde kalıp bir sonraki takılma
            // kontrolünde yeniden dener (eskiden hedefi duvarın arkasında da olsa spawn noktasına sabitliyordu).
            if (HasClearPath(_spawnCenter))
            {
                _wanderTarget = _spawnCenter;
                return;
            }
            _wanderTarget = transform.position; // kısa süre dur, sonra yeniden dene
            _retryPickAt = Time.time + RetryPickDelay;
        }

        // Yaya hedefe düz yürüyor (NavMesh yolu kullanmıyor) — arada bina olan hedef seçilmez.
        private bool HasClearPath(Vector3 target)
        {
            if (s_walkMask < 0)
                s_walkMask = LayerMask.GetMask("Environment", "PropObstacle", "Hazard"); // 2026-11-04: sokak eşyaları + arabalar da

            Vector3 to = target - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance < 0.01f) return true;
            return !Physics.SphereCast(transform.position, _bodyRadius, to / distance, out _, distance, s_walkMask);
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
                // KRİTİK: Y düzleştirmesi burada, "ulaştım mı" kontrolünden ÖNCE yapılmalı —
                // aksi halde spawn/bob yüksekliği farkı (~0.75-0.85) 3D mesafeyi şişirip eşiği
                // (0.04) hep aştırıyor, "ulaştım" hiç tetiklenmiyor; X/Z'de gerçekten hedefe
                // varan yaya hiç yeni hedef seçmeden kalıcı duruyordu (kök sebep buydu).
                direction.y = 0f;
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

            Vector3 step = ClampStepAgainstEnvironment(direction * speed * Time.deltaTime);
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Bkz. EnemyBase.ClampStepAgainstEnvironment — aynı sert-engel tekniği, yaya için kopyalandı
        // (proje konvansiyonu: ComputeAvoidance de aynı şekilde iki dosyada ayrı ayrı yaşıyor, ortak
        // SwarmSteering'e bağlı olmadığı için).
        private const float EnvironmentSkin = 0.05f;

        private Vector3 ClampStepAgainstEnvironment(Vector3 step)
        {
            // ComputeAvoidance() de aynı lazy-init'i yapıyor ama throttle tick'ine bağlı — Move() her
            // frame çalıştığı için (ilk pedestrian'ın ilk tick'inden ÖNCE de) burada bağımsız garanti
            // ediliyor, aksi halde -1 (tüm layer'lar) mask'iyle yanlışlıkla her şeye çarpardı.
            if (s_walkMask < 0)
                s_walkMask = LayerMask.GetMask("Environment", "PropObstacle", "Hazard"); // 2026-11-04: sokak eşyaları + arabalar da

            float stepDist = step.magnitude;
            if (stepDist < 0.0001f || s_walkMask == 0) return step;

            Vector3 dir = step / stepDist;
            if (!Physics.SphereCast(transform.position, _bodyRadius, dir, out RaycastHit hit, stepDist, s_walkMask))
                return step;

            // Duvara kadar git, kalan adımı duvar boyunca kaydır (eskiden adım sıfırlanıyor, köşede kilitleniyordu).
            float travel = Mathf.Max(0f, hit.distance - EnvironmentSkin);
            Vector3 toWall = dir * travel;
            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.0001f) return toWall;
            normal.Normalize();

            Vector3 slide = Vector3.ProjectOnPlane(dir * (stepDist - travel), normal);
            float slideDist = slide.magnitude;
            if (slideDist < 0.0001f) return toWall;

            Vector3 slideDir = slide / slideDist;
            if (Physics.SphereCast(transform.position + toWall, _bodyRadius, slideDir, out RaycastHit slideHit, slideDist, s_walkMask))
                slide = slideDir * Mathf.Max(0f, slideHit.distance - EnvironmentSkin); // iç köşe: ikinci duvarda dur

            return toWall + slide;
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
