using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Entities.Blob
{
    // Evrim "Hurda Kalkanı" (Geri Dönüşüm + Zırh): blob'un etrafında dönen araba hurdası (lastik, kapı, tampon, kaput;
    // zorse-blender/build_scrap.py → Resources/FX/Scrap, Zırh kalkanlarıyla aynı palet materyali). Değdiği düşmana vurur; gelen bir darbeyi tamamen yutar ve kırılır. Yutulan her araba yeni
    // parça ekler (en fazla MaxPieces). Restart'ta GameManager.ClearPreviousRunState siler.
    public class ScrapOrbit : MonoBehaviour
    {
        public const int MaxPieces = 4;
        public float Damage = 15f;
        public float Knockback = 0.8f;

        private const int StartPieces = 2;
        private const float OrbitRadius = 0.95f;       // blob yerel uzayında (gövde yarıçapı 0.5)
        private const float OrbitHeight = 0.15f;
        private const float OrbitSpeed = 150f;         // derece/sn
        private const float PieceScale = 0.75f;
        private const float HitRadius = 0.32f;         // yerel; dünya = × blob ölçeği
        private const float TickInterval = 0.1f;
        private const float PieceCooldown = 0.4f;
        private const float PopDuration = 0.3f;
        private const int EnemyMask = 1 << 14;
        private const string ScrapFolder = "FX/Scrap/";
        private const string MaterialPath = "FX/Shield/Mat_ShieldPalette";
        private static readonly string[] Meshes = { "FX_Scrap1_Tire", "FX_Scrap2_Door", "FX_Scrap3_Bumper", "FX_Scrap4_Hood" };
        private static readonly Color DebrisA = new Color(0.55f, 0.58f, 0.62f), DebrisB = new Color(0.85f, 0.85f, 0.8f);

        private readonly Transform[] _pieces = new Transform[MaxPieces];
        private readonly bool[] _alive = new bool[MaxPieces];
        private readonly float[] _cooldown = new float[MaxPieces];
        private readonly float[] _pop = new float[MaxPieces];
        private readonly Collider[] _hits = new Collider[8];
        private Transform _pivot;
        private BlobHealth _health;
        private float _angle, _timer;

        public int Count
        {
            get { int n = 0; for (int i = 0; i < MaxPieces; i++) if (_alive[i]) n++; return n; }
        }

        private void Awake()
        {
            _health = GetComponent<BlobHealth>();
            if (_health != null) _health.AbsorbHit = Absorb;
            Build();
            for (int i = 0; i < StartPieces; i++) AddPiece();
        }

        private void OnEnable() => GameEvents.OnVehicleEaten += AddPiece;
        private void OnDisable() => GameEvents.OnVehicleEaten -= AddPiece;

        private void OnDestroy()
        {
            if (_health != null && _health.AbsorbHit == Absorb) _health.AbsorbHit = null;
            if (_pivot != null) Destroy(_pivot.gameObject);
        }

        // Run başına tek seferlik: 4 parça objesi, gizli başlar.
        private void Build()
        {
            Material material = Resources.Load<Material>(MaterialPath);
            _pivot = new GameObject("ScrapOrbit").transform;
            _pivot.SetParent(transform, false);
            for (int i = 0; i < MaxPieces; i++)
            {
                var go = new GameObject("Scrap" + i);
                go.layer = gameObject.layer;
                go.transform.SetParent(_pivot, false);
                go.AddComponent<MeshFilter>().sharedMesh = Resources.Load<Mesh>(ScrapFolder + Meshes[i % Meshes.Length]);
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _pieces[i] = go.transform;
                go.SetActive(false);
            }
        }

        private void AddPiece()
        {
            for (int i = 0; i < MaxPieces; i++)
            {
                if (_alive[i]) continue;
                _alive[i] = true;
                _pop[i] = 0f;
                _cooldown[i] = 0f;
                _pieces[i].gameObject.SetActive(true);
                return;
            }
        }

        // BlobHealth.AbsorbHit: bir parça darbeyi yutar ve dağılır.
        private bool Absorb()
        {
            for (int i = 0; i < MaxPieces; i++)
            {
                if (!_alive[i]) continue;
                _alive[i] = false;
                _pieces[i].gameObject.SetActive(false);
                FireworkBurstFx.Debris(_pieces[i].position, DebrisA, DebrisB, transform.localScale.x);
                Haptics.Impact(1);
                return true;
            }
            return false;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _angle += OrbitSpeed * dt;
            int n = Count;
            if (n == 0) return;
            float step = 360f / n;
            int slot = 0;
            for (int i = 0; i < MaxPieces; i++)
            {
                if (!_alive[i]) continue;
                // Parçalar eşit aralıkla dizilir; dışa bakar, kendi ekseninde de döner.
                float a = _angle + step * slot++;
                Quaternion around = Quaternion.Euler(0f, a, 0f);
                _pieces[i].localPosition = around * new Vector3(0f, OrbitHeight, OrbitRadius);
                _pieces[i].localRotation = around * Quaternion.Euler(-20f, 0f, a * 2f);
                if (_pop[i] < 1f) _pop[i] = Mathf.Min(1f, _pop[i] + dt / PopDuration);
                float p = _pop[i];
                float s = PieceScale * (1f - (1f - p) * (1f - p) + Mathf.Sin(p * Mathf.PI) * 0.3f);
                _pieces[i].localScale = Vector3.one * s;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < MaxPieces; i++) _cooldown[i] -= dt;
            _timer -= dt;
            if (_timer > 0f) return;
            _timer = TickInterval;

            float radius = HitRadius * transform.localScale.x;
            Vector3 center = transform.position;
            for (int i = 0; i < MaxPieces; i++)
            {
                if (!_alive[i] || _cooldown[i] > 0f) continue;
                int count = Physics.OverlapSphereNonAlloc(_pieces[i].position, radius, _hits, EnemyMask, QueryTriggerInteraction.Ignore);
                bool hit = false;
                for (int h = 0; h < count; h++)
                {
                    EnemyBase enemy = _hits[h].GetComponent<EnemyBase>(); // parça değdiğinde, en çok 0.1 sn'de bir
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(Damage * BlobSkills.WeaponDamageScale);
                    enemy.Knockback(enemy.transform.position - center, Knockback);
                    hit = true;
                }
                if (hit) _cooldown[i] = PieceCooldown;
            }
        }
    }
}
