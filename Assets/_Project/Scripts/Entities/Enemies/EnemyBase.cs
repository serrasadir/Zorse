using UnityEngine;
using UnityEngine.AI;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Coins;
using BlobSurvivor.Entities.Loot;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Enemies
{
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] private EnemyData _data;
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _separationRadius = 1.5f;
        [SerializeField] private float _separationWeight = 1.5f;
        [SerializeField] private float _avoidanceLookahead = 1.2f;
        [SerializeField] private float _avoidanceWeight = 2f;
        [SerializeField] private float _rotationSpeed = 10f;

        private const float AIUpdateInterval = 0.15f;
        private static int s_environmentMask = -1;
        // 2026-11-04: sürü polisinin yürüyüş sorguları (düz yol, kaçınma, duvara çarpma) binalara ek olarak sokak
        // eşyalarını da görür (PropObstacle). Nişan/görüş/mermi sorguları s_environmentMask'te kalır.
        private static int s_walkMask = -1;

        public event System.Action<EnemyBase> OnDeath;

        public EnemyData Data => _data;
        public Transform BlobTransform { get; private set; }
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _data != null ? _data.MaxHealth : 0f;

        // Karar 2 (GDD_v2.md §7): sürü düşmanları NavMeshAgent kullanmaz, basit steering kullanır.
        // Elit/boss NavMeshAgent'ta kalmaya devam eder.
        public bool UsesSteering => _data != null && !_data.IsElite;

        private NavMeshAgent _agent;
        private BlobHealth _blobHealth;
        private ScoreSystem _scoreSystem;
        private IEnemyState _currentState;
        private float _currentHealth;
        private EnemyHealthBar _healthBar; // sadece polis/elit prefab'ında var

        // Vuruş geri bildirimi: model kısa süre kızarır (MaterialPropertyBlock), boss olmayan geri seker.
        private const float HitFlashDuration = 0.16f;
        private static readonly Color HitFlashColor = new Color(1f, 0.25f, 0.22f, 1f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock s_flashBlock;
        private Renderer[] _flashRenderers;
        private Color[] _flashBaseColors;
        private float _flashTime = -1f;
        private const float KnockbackDuration = 0.16f;
        private Vector3 _knockbackVelocity;
        private float _knockbackDecel;
        private float _aiUpdateTimer;
        private bool _canSeeBlobCached;
        private bool _edibleByBlobCached;
        private float _damageMultiplier = 1f;
        private float _speedMultiplier = 1f;
        // 2026-11-03: boss yetişme hızı (BossSpawner ekran dışında + uzakken ayarlar), dalga çarpanından ayrı.
        private float _catchUpMultiplier = 1f;
        private bool _isDead;
        private bool _consumableOverride;
        private Vector3 _steerTarget;
        private bool _hasSteerTarget;

        // 2026-11-03 (Serra: "binanın arkasına geçince polis dolanmak yerine binanın üstüne yürüyüp takılıyor"):
        // sürü polisi NavMeshAgent kullanmıyor, hedefe düz yürüyordu. Hedefe düz yol açıksa yine düz koşar
        // (ucuz); arada bina varsa NavMesh.CalculatePath ile köşe köşe dolanır. Yol hesabı seyrek: görüş
        // kapalıyken en fazla RepathInterval'da bir ya da hedef belirgin kaydığında.
        private const float RepathInterval = 0.5f;
        private const float RepathTargetShift = 2f;
        private const float CornerReachDistance = 0.6f;
        private const float PathSampleRadius = 3f;
        private const int MaxPathCorners = 16;
        private NavMeshPath _path;
        private readonly Vector3[] _pathCorners = new Vector3[MaxPathCorners];
        private int _pathCornerCount;
        private int _pathCornerIndex;
        private bool _followingPath;
        private float _nextRepathTime;
        private Vector3 _pathTarget;
        private Vector3 _separationCached;
        private Vector3 _avoidanceCached;
        private float _blobSolidRadius = 0.5f;
        private Vector3 _steeringVelocity;
        private EnemyAimLine _aimLine;
        private ObjectPool<EnemyProjectile> _projectilePool;

        private const float MuzzleOffset = 0.7f;
        private const int InitialProjectilePoolSize = 12;

        // State çıkışında kaybolmaması için burada tutulur: oyuncu menzilden çıkıp girerek bekleme süresini atlayamaz.
        public float NextRangedAttackTime { get; set; }
        // 2026-11-03 (Serra: "polis değer değmez hasar versin"): yakın dövüş de ilk temasta anında vurur; bekleme
        // süresi burada tutulur ki menzil kenarında girip çıkmak vuruşları hızlandırmasın.
        public float NextMeleeAttackTime { get; set; }

        // 2026-11-04 (final boss helikopter): gövdenin yerden yüksekliği (0 = yerde). Kök (NavMeshAgent) yerde
        // kalır; collider/model bu kadar yukarıda olur (FinalBossController ayarlar). Silah nişanı ve düşmanın
        // kendi namlusu buna göre hesaplanır.
        public float AirborneHeight { get; set; }
        // Havadaki gövdenin merkezi — oyuncu silahı buraya nişan alır. Yerdekiler için kök pozisyonu (davranış aynı).
        public Vector3 AimPoint => AirborneHeight > 0f ? transform.position + Vector3.up * AirborneHeight : transform.position;

        // 2026-09-06 (Serra): "mermiler polislerin arkasından geçip gidiyor" — Cannon/Pistol non-homing,
        // ateş anındaki pozisyona düz çizgi atıyor; polis steering'le (separation/avoidance) sürekli yön
        // değiştirdiği için mermi menzile ulaşana kadar hedef kaymış oluyordu. WeaponBase bu hızı okuyup
        // "önden kesme" (lead) hesaplıyor — mermiler homing OLMUYOR, sadece nişan noktası düzeliyor.
        public Vector3 CurrentVelocity => UsesSteering ? _steeringVelocity : (_agent != null ? _agent.velocity : Vector3.zero);

        // 2026-09-06 (Serra): "polis görselde bana değmiyor ama değmiş gibi davranıyor" — kök sebep
        // raycast değil, ChaseState/AttackState'in EnemyData.AttackRange'i (normal polis: 2 birim) sabit
        // bir eşik olarak kullanmasıydı. Blob küçükken (Tiny/Small) gerçek temas mesafesi (collider
        // yarıçapları toplamı, ~0.75-1.0) bu sabitin çok altında — polis görsel temastan ~1-1.5 birim
        // önce duruyor ve hasar veriyordu. Elit/boss'ta (özel saldırı deseni/alan saldırısı, GDD §7)
        // sabit AttackRange bilinçli bırakıldı; sadece sıradan sürü düşmanı artık blob'un GERÇEK o anki
        // boyutundan (transform.localScale, BlobGrowth her frame günceller) dinamik hesaplanan temas
        // mesafesini kullanıyor — büyüme formülü ileride tekrar ayarlanırsa (2026-09-06'dan önce zaten
        // iki kez değişti) bu sabit kalıp yeniden bozulmaz.
        private const float ContactBuffer = 0.15f;
        // Yakın dövüşçü elit/boss için temas mesafesinin ötesine uzanan vuruş payı.
        private const float EliteMeleeReach = 0.6f;
        private float _bodyRadius = 0.5f;

        // 2026-11-03 (Serra: "polis vururken basit bir animasyon"): iskeletsiz "hamle" — Model child'ı kısa bir
        // ileri atılma + öne eğilme + basıklaşma yapar (vuruş anında), sonra yavaşça yerine döner. Elit ateş
        // edince aynı sistemle geri teper. Asset ya da Animator gerekmez, maliyeti birkaç transform hesabı.
        private const float LungeDuration = 0.28f;
        private const float LungePeak = 0.3f;        // sürenin bu kadarında en ileride, kalanı dönüş
        private const float MeleeLungeDistance = 0.35f;
        private const float MeleeLungePitch = 18f;
        private const float RecoilDistance = -0.12f;
        private const float RecoilPitch = -8f;
        private Transform _model;
        private Vector3 _modelRestPos;
        private Quaternion _modelRestRot;
        private Vector3 _modelRestScale;
        private float _lungeTime = -1f;
        private float _lungeDistance;
        private float _lungePitch;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            CapsuleCollider body = GetComponent<CapsuleCollider>();
            _bodyRadius = body != null
                ? body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z)
                : (_agent != null ? _agent.radius : 0.5f);
            _scoreSystem = FindAnyObjectByType<ScoreSystem>();

            _model = transform.Find("Model");
            if (_model != null)
            {
                _modelRestPos = _model.localPosition;
                _modelRestRot = _model.localRotation;
                _modelRestScale = _model.localScale;
            }
            CacheFlashRenderers();

            if (s_environmentMask < 0)
                s_environmentMask = LayerMask.GetMask("Environment");
            if (s_walkMask < 0)
                s_walkMask = LayerMask.GetMask("Environment", "PropObstacle", "Hazard"); // + arabalar (Hazard); PropObstacle yoksa atlanır
        }

        private void OnEnable()
        {
            _currentHealth = _data != null ? _data.MaxHealth : 100f;
            if (_healthBar != null) _healthBar.SetRatio(1f);
            _knockbackVelocity = Vector3.zero;
            _aiUpdateTimer = Random.Range(0f, AIUpdateInterval); // stagger — hepsi aynı anda çalışmasın
            _canSeeBlobCached = false;
            _damageMultiplier = 1f;
            _speedMultiplier = 1f;
            _catchUpMultiplier = 1f;
            _isDead = false;
            _consumableOverride = false;
            AirborneHeight = 0f; // FinalBossController her kare yeniden ayarlar
            _hasSteerTarget = false;
            _followingPath = false;
            _nextRepathTime = 0f;
            _steeringVelocity = Vector3.zero;
            NextRangedAttackTime = 0f;
            NextMeleeAttackTime = 0f;

            if (_projectilePool == null && _data != null && _data.ProjectilePrefab != null && PoolManager.Instance != null)
                _projectilePool = PoolManager.Instance.CreatePool(_data.ProjectilePrefab, InitialProjectilePoolSize);

            // Blob sahnede tek ve kalıcı — her spawn'da FindWithTag/GetComponents (GC) yerine bir kere bulunur.
            GameObject blob = BlobTransform == null ? GameObject.FindWithTag("Blob") : null;
            if (blob != null)
            {
                BlobTransform = blob.transform;
                _blobHealth = blob.GetComponent<BlobHealth>();

                // Blob'da iki SphereCollider var (trigger=yeme algılama, solid=fiziksel/dokunma) —
                // temas mesafesi için solid olanın yarıçapı gerekli (M25 notuna bkz., CLAUDE.md).
                SphereCollider[] blobColliders = blob.GetComponents<SphereCollider>();
                foreach (SphereCollider c in blobColliders)
                {
                    if (!c.isTrigger)
                    {
                        _blobSolidRadius = c.radius;
                        break;
                    }
                }
            }

            if (UsesSteering)
            {
                if (_agent != null) _agent.enabled = false;
                SwarmSteering.Instance?.Register(this);
            }
            else
            {
                if (_agent != null) _agent.enabled = true;
                if (_agent != null && _data != null) _agent.speed = _data.MoveSpeed;
            }

            ChangeState(new PatrolState());
        }

        private void OnDisable()
        {
            SwarmSteering.Instance?.Unregister(this);
            ResetModelPose(); // havuza hamlenin ortasında dönerse bir sonraki spawn'da eğik kalmasın
            ClearFlash();
        }

        // Sadece opak, _BaseColor'lı renderer'lar (helikopterin projektörü/gölgesi gibi saydamlar hariç).
        private void CacheFlashRenderers()
        {
            Renderer[] all = _model != null ? _model.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            var list = new System.Collections.Generic.List<Renderer>(all.Length);
            var colors = new System.Collections.Generic.List<Color>(all.Length);
            foreach (Renderer r in all)
            {
                Material m = r.sharedMaterial;
                if (m == null || !m.HasProperty(BaseColorId) || m.renderQueue >= 2500) continue;
                list.Add(r);
                colors.Add(m.GetColor(BaseColorId));
            }
            _flashRenderers = list.ToArray();
            _flashBaseColors = colors.ToArray();
        }

        private void UpdateFlash()
        {
            if (_flashTime < 0f) return;
            _flashTime += Time.deltaTime;
            float k = 1f - _flashTime / HitFlashDuration;
            if (k <= 0f)
            {
                ClearFlash();
                return;
            }
            if (s_flashBlock == null) s_flashBlock = new MaterialPropertyBlock();
            for (int i = 0; i < _flashRenderers.Length; i++)
            {
                s_flashBlock.SetColor(BaseColorId, Color.Lerp(_flashBaseColors[i], _flashBaseColors[i] * HitFlashColor, k));
                _flashRenderers[i].SetPropertyBlock(s_flashBlock);
            }
        }

        private void ClearFlash()
        {
            if (_flashTime < 0f || _flashRenderers == null) return;
            _flashTime = -1f;
            foreach (Renderer r in _flashRenderers) r.SetPropertyBlock(null); // SRP batch'e geri döner
        }

        // Vuruşla geri sekme (yatay). Boss'lar sekmez. Duvar/eşya varsa önünde durur (vuruş anında tek ışın).
        public void Knockback(Vector3 direction, float distance)
        {
            if (_isDead || IsBoss || distance <= 0f) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            direction.Normalize();
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance + _bodyRadius, s_walkMask, QueryTriggerInteraction.Ignore))
                distance = hit.distance - _bodyRadius;
            if (distance < 0.02f) return;

            float speed = 2f * distance / KnockbackDuration;
            if (speed * speed <= _knockbackVelocity.sqrMagnitude) return; // daha güçlüsü sürüyor
            _knockbackVelocity = direction * speed;
            _knockbackDecel = speed / KnockbackDuration;
        }

        private void UpdateKnockback()
        {
            if (_knockbackVelocity.sqrMagnitude < 0.0001f) return;
            float dt = Time.deltaTime;
            Vector3 step = _knockbackVelocity * dt;
            if (UsesSteering) transform.position += step;
            else if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.Move(step);
            _knockbackVelocity = Vector3.MoveTowards(_knockbackVelocity, Vector3.zero, _knockbackDecel * dt);
        }

        private void PlayLunge(float distance, float pitch)
        {
            if (_model == null) return;
            _lungeTime = 0f;
            _lungeDistance = distance;
            _lungePitch = pitch;
        }

        private void AnimateLunge()
        {
            if (_lungeTime < 0f || _model == null) return;

            _lungeTime += Time.deltaTime;
            float t = _lungeTime / LungeDuration;
            if (t >= 1f)
            {
                ResetModelPose();
                return;
            }

            // Hızlı çıkış (vuruş anı), yumuşak dönüş.
            float k = t < LungePeak
                ? Mathf.Sin(t / LungePeak * Mathf.PI * 0.5f)
                : 1f - Mathf.SmoothStep(0f, 1f, (t - LungePeak) / (1f - LungePeak));

            // Model'in parent'ı kök: Vector3.forward kökün baktığı yön (saldırıda blob'a döner), +X ekseni etrafında
            // pozitif açı öne eğilme. Model'in pivotu ayaklarda, yani eğilme ayak üstünde olur.
            _model.localPosition = _modelRestPos + Vector3.forward * (_lungeDistance * k);
            _model.localRotation = Quaternion.AngleAxis(_lungePitch * k, Vector3.right) * _modelRestRot;
            float squash = Mathf.Abs(k) * 0.1f;
            _model.localScale = new Vector3(
                _modelRestScale.x * (1f + squash * 0.6f),
                _modelRestScale.y * (1f - squash),
                _modelRestScale.z * (1f + squash * 0.6f));
        }

        private void ResetModelPose()
        {
            _lungeTime = -1f;
            if (_model == null) return;
            _model.localPosition = _modelRestPos;
            _model.localRotation = _modelRestRot;
            _model.localScale = _modelRestScale;
        }

        private void Update()
        {
            _aiUpdateTimer -= Time.deltaTime;
            bool aiTick = _aiUpdateTimer <= 0f;
            if (aiTick)
            {
                _aiUpdateTimer = AIUpdateInterval;
                _canSeeBlobCached = ComputeCanSeeBlob();
                _edibleByBlobCached = ComputeEdibleByBlob();

                // 2026-11-03 (Serra): sürü polisi blob onu yiyebilecek boyuttaysa (Medium)
                // kovalamak yerine kaçar. Elit/boss kaçmaz (UsesSteering = elit değil).
                if (_edibleByBlobCached && _canSeeBlobCached && !(_currentState is FleeState))
                    ChangeState(new FleeState());

                // Ayrım (separation) ve engel kaçınma sorguları pahalı (komşu grid taraması / raycast) —
                // her frame değil, throttle tick'te hesaplanıp cache'lenir. Seek her frame taze kalır
                // (ucuz, sadece vektör çıkarma) — hareket akıcı görünür, ağır kısım throttle'lanır.
                if (UsesSteering)
                {
                    _separationCached = SwarmSteering.Instance != null
                        ? SwarmSteering.Instance.GetSeparationVector(this, _separationRadius)
                        : Vector3.zero;
                    if (_hasSteerTarget) UpdateSteeringPath();
                    _avoidanceCached = ComputeAvoidance();
                }
            }

            _currentState?.Update(this, aiTick);

            // 2026-11-03 (Serra: "mermiler polislerin içinden geçiyor"): duran polis (AttackState → StopMoving)
            // son koşu hızını raporlamaya devam ediyordu; silah "önden kesme" ile boşluğa nişan alıyordu.
            _steeringVelocity = Vector3.zero;
            if (UsesSteering && _hasSteerTarget)
                ApplySteeringMovement();

            UpdateKnockback();
            AnimateLunge();
            UpdateFlash();
        }

        // Throttle tick'inde: hedefe düz yol açık mı? Değilse NavMesh yolu (gerekirse yeniden) hesaplanır.
        private void UpdateSteeringPath()
        {
            float radius = _agent != null ? _agent.radius : 0.5f;
            if (HasClearPath(_steerTarget, radius))
            {
                _followingPath = false;
                return;
            }

            bool targetShifted = (_steerTarget - _pathTarget).sqrMagnitude > RepathTargetShift * RepathTargetShift;
            if (_followingPath && !targetShifted && Time.time < _nextRepathTime) return;

            _nextRepathTime = Time.time + RepathInterval;
            _pathTarget = _steerTarget;
            _followingPath = false;

            // Sürü polisi Humanoid NavMesh'te yürür (tank'ın ayrı NavMesh'i var — filtresiz sorgu belirsizlik uyarısı verir).
            NavMeshQueryFilter filter = SpawnPositionUtility.HumanoidFilter;
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit from, PathSampleRadius, filter)) return;
            if (!NavMesh.SamplePosition(_steerTarget, out NavMeshHit to, PathSampleRadius, filter)) return;

            _path ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(from.position, to.position, filter, _path)) return;

            _pathCornerCount = _path.GetCornersNonAlloc(_pathCorners);
            _pathCornerIndex = 1; // 0. köşe bulunduğumuz yer
            _followingPath = _pathCornerCount > 1;
        }

        private bool HasClearPath(Vector3 target, float radius)
        {
            Vector3 to = target - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance < 0.01f || s_walkMask == 0) return true;
            return !Physics.SphereCast(transform.position, radius * 0.9f, to / distance, out _, distance, s_walkMask);
        }

        // Yol izleniyorsa sıradaki köşe, değilse doğrudan hedef. Köşeye varınca bir sonrakine geçer.
        private Vector3 CurrentWaypoint()
        {
            if (!_followingPath) return _steerTarget;

            while (_pathCornerIndex < _pathCornerCount)
            {
                Vector3 corner = _pathCorners[_pathCornerIndex];
                Vector3 delta = corner - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > CornerReachDistance * CornerReachDistance) return corner;
                _pathCornerIndex++;
            }

            _followingPath = false;
            return _steerTarget;
        }

        private Vector3 ComputeAvoidance()
        {
            if (s_walkMask == 0) return Vector3.zero;

            Vector3 forward = _hasSteerTarget ? CurrentWaypoint() - transform.position : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return Vector3.zero;
            forward.Normalize();

            // Gövde kalınlığında süpürme: ince ışın bina köşesini ıskalıyor, gövde çarpıp kilitleniyordu.
            float bodyRadius = _agent != null ? _agent.radius : 0.5f;
            if (!Physics.SphereCast(transform.position, bodyRadius * 0.9f, forward, out RaycastHit hit, _avoidanceLookahead, s_walkMask))
                return Vector3.zero;

            // Duvara sıkışmayı önle: engelden uzaklaştıran, hareket yönüne dik bir sapma.
            Vector3 away = Vector3.Cross(Vector3.up, hit.normal);
            if (Vector3.Dot(away, forward) < 0f) away = -away;
            return away;
        }

        private void ApplySteeringMovement()
        {
            Vector3 toTarget = CurrentWaypoint() - transform.position;
            toTarget.y = 0f;

            Vector3 seek = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.zero;
            Vector3 moveDir = seek + _separationCached * _separationWeight + _avoidanceCached * _avoidanceWeight;
            if (moveDir.sqrMagnitude < 0.0001f) return;
            moveDir.Normalize();

            float speed = _data.MoveSpeed * _speedMultiplier * _catchUpMultiplier;
            _steeringVelocity = moveDir * speed;
            Vector3 step = ClampStepAgainstEnvironment(_steeringVelocity * Time.deltaTime);
            transform.position += step;
            // 2026-11-03: nişan tahmini (WeaponBase.GetPredictedAimPoint) için GERÇEK hız — duvara takılınca
            // istenen hız değil, fiilen gidilen yol.
            _steeringVelocity = Time.deltaTime > 0f ? step / Time.deltaTime : Vector3.zero;

            // Ani yön sıçramalarını yumuşat (Slerp) — separation kaynaklı titreşimi görsel olarak azaltır.
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), _rotationSpeed * Time.deltaTime);
        }

        // 2026-09-06 (Serra): "yaya ve polisler binaların içinden geçememeli" — ComputeAvoidance() zaten
        // vardı ama sadece YUMUŞAK bir sapmaydı (throttle tick'te hesaplanan tek bir raycast'e göre yön
        // karıştırma); dar açılarda veya tam köşede yetersiz kalıp binaya girmeyi engelleyemiyordu. Bu,
        // HER FRAME çalışan sert bir engel — o frame'in hareket adımı boyunca Environment'a çarpıyorsa
        // adımı çarpma noktasının hemen öncesine kısıtlar (klasik "slide/stop" collider'sız karakter
        // kontrolcüsü tekniği). Tek bir kısa mesafeli SphereCast, 15 polis + 25 yaya için bile per-frame
        // maliyeti ihmal edilebilir düzeyde.
        private const float EnvironmentSkin = 0.05f;

        private Vector3 ClampStepAgainstEnvironment(Vector3 step)
        {
            float stepDist = step.magnitude;
            if (stepDist < 0.0001f || s_walkMask == 0) return step;

            float radius = _agent != null ? _agent.radius : 0.5f;
            Vector3 dir = step / stepDist;
            if (!Physics.SphereCast(transform.position, radius, dir, out RaycastHit hit, stepDist, s_walkMask))
                return step;

            // 2026-11-03: duvara kadar git, kalan adımı duvar boyunca kaydır (eskiden adım sıfırlanıyordu,
            // köşeye sürtünen polis kilitleniyordu). İç köşede ikinci duvarda durur.
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
            if (Physics.SphereCast(transform.position + toWall, radius, slideDir, out RaycastHit slideHit, slideDist, s_walkMask))
                slide = slideDir * Mathf.Max(0f, slideHit.distance - EnvironmentSkin);

            return toWall + slide;
        }

        private bool ComputeCanSeeBlob()
        {
            if (BlobTransform == null) return false;
            float sqrDist = (transform.position - BlobTransform.position).sqrMagnitude;
            return sqrDist <= _detectionRange * _detectionRange;
        }

        // BossSpawner gibi çağıranlar, tek bir prefab'ı birden fazla EnemyData ("stage") ile paylaşır —
        // OnEnable() zaten prefab'da serialize edilmiş (muhtemelen yanlış/stale) _data'ya göre
        // _currentHealth'i set etmiş olur; burada doğru data'ya göre yeniden set edilmesi şart,
        // yoksa boss yanlış HP'yle (spawn anındaki prefab-serialized data'nın MaxHealth'iyle) hayata başlar.
        public void SetData(EnemyData data, float damageMultiplier = 1f, float speedMultiplier = 1f)
        {
            _data = data;
            _currentHealth = data.MaxHealth;
            if (_healthBar != null) _healthBar.SetRatio(1f);
            // Havuzlu örnek başına bir kez eklenir (tank miniboss). Veri değişirse açık/kapalı izler.
            if (data.CrushesObstacles || TryGetComponent(out TankCrusher _))
            {
                TankCrusher crusher = GetComponent<TankCrusher>();
                if (crusher == null) crusher = gameObject.AddComponent<TankCrusher>();
                crusher.enabled = data.CrushesObstacles;
            }
            // Mermi havuzunu ilk atıştan önce ısıt (atış anında toplu Instantiate olmasın).
            if (data.ProjectilePrefab != null && PoolManager.Instance != null)
                _projectilePool = PoolManager.Instance.CreatePool(data.ProjectilePrefab, InitialProjectilePoolSize);
            _damageMultiplier = Mathf.Max(0f, damageMultiplier);
            _speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            ApplyAgentSpeed();
        }

        public void SetCatchUpMultiplier(float multiplier)
        {
            multiplier = Mathf.Max(1f, multiplier);
            if (Mathf.Approximately(multiplier, _catchUpMultiplier)) return;
            _catchUpMultiplier = multiplier;
            ApplyAgentSpeed();
        }

        private void ApplyAgentSpeed()
        {
            if (_agent != null && _data != null) _agent.speed = _data.MoveSpeed * _speedMultiplier * _catchUpMultiplier;
        }

        public void ChangeState(IEnemyState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState.Enter(this);
        }

        public void WarpTo(Vector3 position)
        {
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                // Spawn noktaları varsayılan (Humanoid) NavMesh'ten seçilir; geniş ajan (tank, kendi NavMesh'i)
                // bina dibine düşen noktada mesh dışında kalmasın diye kendi ajan tipinin mesh'ine oturtulur.
                if (_agent.agentTypeID != 0)
                {
                    NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = _agent.agentTypeID, areaMask = NavMesh.AllAreas };
                    if (NavMesh.SamplePosition(position, out NavMeshHit hit, 8f, filter))
                        position = hit.position;
                }
                _agent.Warp(position);
            }
            else
                transform.position = position;
        }

        public void SetDestination(Vector3 destination)
        {
            if (UsesSteering)
            {
                _steerTarget = destination;
                _hasSteerTarget = true;
                return;
            }

            if (_agent != null && _agent.isOnNavMesh)
                _agent.SetDestination(destination);
        }

        public void StopMoving()
        {
            if (UsesSteering)
            {
                _hasSteerTarget = false;
                return;
            }

            if (_agent != null && _agent.isOnNavMesh)
                _agent.ResetPath();
        }

        public bool CanSeeBlob() => _canSeeBlobCached;
        public bool IsEdibleByBlob => _edibleByBlobCached;
        public float DetectionRange => _detectionRange;

        private bool ComputeEdibleByBlob()
        {
            if (!UsesSteering || _data == null || _data.PreventConsumption || BlobTransform == null) return false;
            return GameEvents.CurrentBlobTier >= _data.RequiredConsumeTier;
        }

        // Elit/boss'ta EnemyData.AttackRange bilinçli bir "menzil/alan saldırısı" değeridir, olduğu gibi
        // kalır. Sıradan sürü düşmanında gerçek temas mesafesine (iki collider'ın o anki dünya-uzayı
        // yarıçapları toplamı + küçük bir tampon) dayanır — bkz. yukarıdaki ContactBuffer notu.
        public float EffectiveAttackRange()
        {
            if (_data == null) return 0f;

            float blobRadius = BlobTransform != null ? BlobTransform.localScale.x * _blobSolidRadius : 0f;
            float contact = _bodyRadius + blobRadius + ContactBuffer;
            // Elit/boss sabit menzilini kullanır ama büyük blob'da gövdeler bu menzile hiç
            // yaklaşamıyordu (miniboss blob'a yapışıp hiç vurmuyordu) — temas mesafesinin altına inmez.
            if (_data.IsElite) return Mathf.Max(_data.AttackRange, contact + EliteMeleeReach);
            return contact;
        }

        /// <summary>Blob'a yatay (XZ) uzaklığın karesi — Y farkı (büyük blob'un merkezi yükselir) menzile sayılmaz.</summary>
        public float FlatSqrDistanceToBlob()
        {
            if (BlobTransform == null) return float.MaxValue;
            Vector3 d = BlobTransform.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude;
        }

        public void PerformAttack()
        {
            PlayLunge(MeleeLungeDistance, MeleeLungePitch);
            _blobHealth?.TakeDamage(_data.Damage * _damageMultiplier);
        }

        public Vector3 DirectionToBlob()
        {
            if (BlobTransform == null) return transform.forward;

            Vector3 direction = BlobTransform.position - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }

        public void FaceTowards(Vector3 direction)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), _rotationSpeed * Time.deltaTime);
        }

        public bool HasLineOfSightToBlob()
        {
            if (BlobTransform == null) return false;

            if (AirborneHeight > 0f)
                return !Physics.Linecast(AimPoint, BlobTransform.position, s_environmentMask, QueryTriggerInteraction.Collide);

            Vector3 toBlob = BlobTransform.position - transform.position;
            toBlob.y = 0f;
            return !Physics.Raycast(transform.position, toBlob.normalized, toBlob.magnitude, s_environmentMask, QueryTriggerInteraction.Collide);
        }

        // Havadaki düşman blob'a yukarıdan (3D) nişan alır; yerdekiler yatay (eski davranış).
        public Vector3 AttackDirectionToBlob()
        {
            if (AirborneHeight <= 0f || BlobTransform == null) return DirectionToBlob();
            Vector3 d = BlobTransform.position - AimPoint;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.down;
        }

        // Lazer merminin gerçek yolunu göstersin diye binada kesilir.
        public float ComputeAimLength(Vector3 direction)
        {
            float maxDistance = _data.ProjectileMaxDistance;
            return Physics.Raycast(AimPoint, direction, out RaycastHit hit, maxDistance, s_environmentMask, QueryTriggerInteraction.Collide)
                ? hit.distance
                : maxDistance;
        }

        public void ShowAimLine(Vector3 direction, float length, float width)
        {
            if (_aimLine == null)
            {
                if (_data.AimLineMaterial == null) return;
                _aimLine = new EnemyAimLine(transform, _data.AimLineMaterial);
            }

            Vector3 origin = AimPoint;
            _aimLine.Show(origin + direction * MuzzleOffset, origin + direction * length, width);
        }

        public void HideAimLine() => _aimLine?.Hide();

        public void FireProjectile(Vector3 direction)
        {
            // Havuz atış anında veriden çözülür: boss'larda veri spawn'dan SONRA SetData ile gelir,
            // OnEnable'daki ısınma prefab'ın eski verisine bakar (yanlış/eksik havuz). Sözlük araması, sadece atışta.
            if (_data.ProjectilePrefab == null || PoolManager.Instance == null) return;
            _projectilePool = PoolManager.Instance.CreatePool(_data.ProjectilePrefab, InitialProjectilePoolSize);

            PlayLunge(RecoilDistance, RecoilPitch);
            // Büyük gövdede (tank) mermi gövdenin içinden değil namlu ucundan çıksın.
            Vector3 position = AirborneHeight > 0f
                ? AimPoint + direction * MuzzleOffset
                : transform.position + direction * Mathf.Max(MuzzleOffset, _bodyRadius);
            EnemyProjectile projectile = _projectilePool.Get(position, Quaternion.LookRotation(direction));
            projectile.Launch(_projectilePool, _blobHealth, _blobSolidRadius, direction,
                _data.ProjectileSpeed, _data.ProjectileMaxDistance, _data.Damage * _damageMultiplier);
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            if (_healthBar != null && MaxHealth > 0f) _healthBar.SetRatio(_currentHealth / MaxHealth);
            if (_flashRenderers != null && _flashRenderers.Length > 0) _flashTime = 0f;

            if (_currentHealth <= 0f)
            {
                // A16/Karar 1+8: final boss silahla öldürülemez — "vurarak son faza getirirsin, yutarak bitirirsin."
                // HP 1'de kilitlenir, ölüm sadece TryConsumeByBlob üzerinden gerçekleşebilir.
                if (_data != null && _data.RequiresConsumptionToDie)
                    _currentHealth = 1f;
                else
                    Die();
            }
        }

        // FinalBossController, HP eşiği altına inince bunu true yapar — yenebilir faza geçiş.
        // PreventConsumption asset-seviyesinde (paylaşımlı SO), bu ise instance-seviyesinde geçici override.
        public void SetConsumableOverride(bool value) => _consumableOverride = value;

        // Karar 5 (GDD_v2.md §4 — yeme birincil): sürü düşmanı Tier3+'ta, elit Tier5'te temasla yutulabilir.
        // Mass ödülü BlobGrowth.AddMass üzerinden verilmeli — mass=XP zaten birleşik (Karar 7).
        // TryConsumeByBlob'un yan etkisiz hâli (önle yeme kontrolünden önce sorulur).
        public bool CanBeConsumedBy(BlobTier blobTier)
        {
            if (_isDead || _data == null) return false;
            if (_data.PreventConsumption && !_consumableOverride) return false;
            return blobTier >= _data.RequiredConsumeTier;
        }

        // Boss'lar (helikopter yukarıdan, uzanarak yutulur) önle yeme kuralına girmez.
        public bool IsBoss => _data != null && _data.PreventConsumption;

        public bool TryConsumeByBlob(BlobTier blobTier, out float massReward)
        {
            massReward = 0f;
            if (_isDead || _data == null) return false;
            // A15/Karar 8: miniboss/final boss normalde hiçbir tier'da yutulamaz, silahla öldürülür —
            // A16: final boss'un yenebilir fazında _consumableOverride bunu geçici olarak açar.
            if (_data.PreventConsumption && !_consumableOverride) return false;

            if (blobTier < _data.RequiredConsumeTier) return false;

            massReward = _data.MassReward;
            Die();
            return true;
        }

        // 2026-11-02 (Serra): polisi yemek bedelsizdi. Eşiğe yeni ulaşmışken ısırık acıtır, büyüdükçe azalır
        // ve 2 tier fark kalınca biter.
        public float GetConsumeDamage(BlobTier blobTier)
        {
            if (_data == null || _data.ConsumeDamage <= 0f) return 0f;

            int tierGap = (int)blobTier - (int)_data.RequiredConsumeTier;
            if (tierGap >= 2) return 0f;

            float damage = _data.ConsumeDamage * _damageMultiplier;
            return tierGap == 1 ? damage * 0.5f : damage;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            _scoreSystem?.AddScore(_data.ScoreValue);

            int coinAmount = _data.IsElite ? Random.Range(5, 11) : 1;
            CoinSpawner.Instance?.SpawnCoin(transform.position, coinAmount);
            if (_data.IsElite)
                ChestSpawner.Instance?.SpawnChest(transform.position);
            if (_data.HeartPrefab != null && Random.value < _data.HeartDropChance)
                HeartPickup.Spawn(_data.HeartPrefab, transform.position);

            // Pool'a dönüş çağıranın (EnemySpawner) sorumluluğunda — CoinSpawner/CoinPickup'taki pattern'in aynısı.
            OnDeath?.Invoke(this);
        }
    }
}
