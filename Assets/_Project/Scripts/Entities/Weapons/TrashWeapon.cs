using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    // Çöp Fırlatma (Cano'nun silahı, kart). Yenen her cansız nesne (eşya, araba) +1 çöp cephanesi; depoda çöp varsa
    // 1 sn'de bir, boşken 3 sn'de bir en yakın düşmana yay çizerek çöp atar. Düştüğü yerde alan hasarı. TrashThrowEffect seviyeler.
    // Çöpler ve lekeler dünyada yaşar (havuz: run başına bir kez kurulur), bileşenle birlikte silinir.
    public class TrashWeapon : MonoBehaviour
    {
        private class Shot
        {
            public Transform View;
            public bool Active, Fragment;
            public Vector3 From, To;
            public float T, Duration, Apex;
            public Vector3 Spin;
        }

        private class Puddle
        {
            public Transform View;
            public float Until, Tick;
            public Vector3 Center;
            public float Radius;
        }

        private const int ShotPool = 16;
        private const int PuddlePool = 8;
        private const int MaxTargets = 2;
        private const int Fragments = 3;
        private const int EnemyLayerMask = 1 << 14;
        private const float FullInterval = 1f;   // depoda çöp varken
        private const float EmptyInterval = 3f;  // depo boşken (silahsız kalmasın)
        private const float FlightTime = 0.55f;
        private const float FragmentTime = 0.3f;
        private const float FragmentHop = 1.6f;
        private const float FragmentDamage = 0.4f;
        private const float FragmentRadius = 0.6f; // ana alanın oranı
        private const float PuddleTick = 0.25f;
        private const float GroundY = 0.15f;
        private const float Knockback = 0.3f;
        private const float NoTargetRetry = 0.15f;
        private const float TinyBlobScale = 0.5f;
        private const string PuddleMeshPath = "FX/Slime/FX_Slime";
        private const string PuddleMaterialPath = "FX/SpeedTrail/Mat_SpeedTrail";
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color PuddleColor = new Color(0.45f, 0.5f, 0.18f, 0.55f);
        private static readonly Color DustA = new Color(0.55f, 0.47f, 0.36f), DustB = new Color(0.36f, 0.42f, 0.2f);

        private float _damage, _radius, _range;
        private int _ammo, _ammoCap;
        private int _count = 1;
        private int _bigBiteAmmo = 1;
        private float _puddleSlow = 1f, _puddleTime;
        private float _fireRate = 1f;
        private bool _split;

        private Mesh[] _meshes;
        private readonly Shot[] _shots = new Shot[ShotPool];
        private readonly Puddle[] _puddles = new Puddle[PuddlePool];
        private readonly Collider[] _hits = new Collider[32];
        private readonly Transform[] _targets = new Transform[MaxTargets];
        private Transform _blobRoot;
        private BlobSkills _skills;
        private float _bodyLocalRadius = 0.5f;
        private float _cooldown;
        private int _shotCount;
        private MaterialPropertyBlock _block;

        private float BlobScale => _blobRoot != null ? _blobRoot.localScale.x : TinyBlobScale;
        private float SizeFactor => Mathf.Clamp(Mathf.Sqrt(BlobScale / TinyBlobScale), 1f, 3f);
        private float EffectiveRange => BlobScale * _bodyLocalRadius + _range;
        public int Ammo => _ammo;

        public void Setup(Mesh[] meshes, Material material, float damage, float radius, float range, int ammoCap)
        {
            _meshes = meshes; _damage = damage; _radius = radius; _range = range; _ammoCap = ammoCap;
            _blobRoot = transform.parent;
            if (_blobRoot == null) return;
            foreach (SphereCollider c in _blobRoot.GetComponents<SphereCollider>())
                if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
            _skills = BlobSkills.Of(_blobRoot.gameObject);
            _skills.Ate += HandleAte;

            for (int i = 0; i < ShotPool; i++) // run başına tek seferlik
            {
                var go = new GameObject("Cop" + i);
                go.AddComponent<MeshFilter>();
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                _shots[i] = new Shot { View = go.transform };
            }
        }

        public void Upgrade(float damage, int ammoCap) { _damage += damage; _ammoCap = Mathf.Max(_ammoCap, ammoCap); }
        public void SetCount(int count) => _count = Mathf.Clamp(count, 1, MaxTargets);
        public void EnableFinalForm(float fireRate) { _fireRate = fireRate; _split = true; }

        public void EnableBigBite(int ammo, float slow, float time)
        {
            _bigBiteAmmo = ammo; _puddleSlow = slow; _puddleTime = time;
            Mesh mesh = Resources.Load<Mesh>(PuddleMeshPath);
            Material material = Resources.Load<Material>(PuddleMaterialPath);
            _block = new MaterialPropertyBlock();
            _block.SetColor(BaseColorId, PuddleColor);
            for (int i = 0; i < PuddlePool; i++) // bir kez
            {
                var go = new GameObject("CopLekesi" + i);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.SetPropertyBlock(_block);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _puddles[i] = new Puddle { View = go.transform };
            }
        }

        private void HandleAte(EatKind kind, BlobTier foodTier)
        {
            if (kind != EatKind.Object && kind != EatKind.Vehicle) return;
            int gain = _bigBiteAmmo > 1 && foodTier >= BlobTier.Medium ? _bigBiteAmmo : 1;
            _ammo = Mathf.Min(_ammoCap, _ammo + gain);
        }

        private void OnEnable() => GameEvents.OnEnemyKilled += HandleEnemyKilled;
        private void OnDisable() => GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        private void HandleEnemyKilled(BlobSurvivor.Data.EnemyData _)
        {
            if (BlobSkills.KillCooldownRefund > 0f) _cooldown -= BlobSkills.KillCooldownRefund; // Tetikçi Sv.5
        }

        private void OnDestroy()
        {
            if (_skills != null) _skills.Ate -= HandleAte;
            foreach (Shot s in _shots) if (s != null && s.View != null) Destroy(s.View.gameObject);
            foreach (Puddle p in _puddles) if (p != null && p.View != null) Destroy(p.View.gameObject);
        }

        private void Update()
        {
            if (_blobRoot == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < ShotPool; i++)
                if (_shots[i] != null && _shots[i].Active) Fly(_shots[i], dt);
            for (int i = 0; i < PuddlePool; i++)
                if (_puddles[i] != null && _puddles[i].View.gameObject.activeSelf) TickPuddle(_puddles[i]);

            _cooldown -= dt;
            if (_cooldown > 0f) return;
            int found = FindTargets();
            if (found == 0) { _cooldown = NoTargetRetry; return; }
            bool loaded = _ammo > 0;
            if (loaded) _ammo--;
            for (int i = 0; i < _count; i++)
                Launch(_targets[i % found]);
            _cooldown = BlobSkills.NextCooldown(ref _shotCount, (loaded ? FullInterval : EmptyInterval) / _fireRate);
        }

        private void Launch(Transform target)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>(); // atış anında, saniyede en çok birkaç kez
            Vector3 to = target.position + (enemy != null ? enemy.CurrentVelocity * FlightTime : Vector3.zero);
            to.y = GroundY;
            Vector3 from = _blobRoot.position + Vector3.up * (BlobScale * _bodyLocalRadius * 1.6f);
            float dist = Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));
            Fire(from, to, FlightTime, 1.2f + dist * 0.25f, false);
        }

        private void Fire(Vector3 from, Vector3 to, float duration, float apex, bool fragment)
        {
            Shot s = null;
            for (int i = 0; i < ShotPool; i++) if (!_shots[i].Active) { s = _shots[i]; break; }
            if (s == null) return;
            s.Active = true; s.Fragment = fragment;
            s.From = from; s.To = to; s.T = 0f; s.Duration = duration; s.Apex = apex;
            s.Spin = Random.onUnitSphere * 540f;
            if (_meshes != null && _meshes.Length > 0)
                s.View.GetComponent<MeshFilter>().sharedMesh = _meshes[Random.Range(0, _meshes.Length)]; // atış anında
            s.View.localScale = Vector3.one * (SizeFactor * (fragment ? 0.55f : 1f));
            s.View.position = from;
            s.View.rotation = Random.rotation;
            s.View.gameObject.SetActive(true);
        }

        private void Fly(Shot s, float dt)
        {
            s.T += dt / s.Duration;
            float t = Mathf.Min(1f, s.T);
            Vector3 p = Vector3.Lerp(s.From, s.To, t);
            p.y += s.Apex * 4f * t * (1f - t);
            s.View.position = p;
            s.View.Rotate(s.Spin * dt, Space.World);
            if (s.T < 1f) return;

            s.Active = false;
            s.View.gameObject.SetActive(false);
            Land(s.To, s.Fragment);
        }

        private void Land(Vector3 point, bool fragment)
        {
            float radius = _radius * SizeFactor * (fragment ? FragmentRadius : 1f);
            float damage = _damage * (fragment ? FragmentDamage : 1f) * BlobSkills.WeaponDamageScale;
            int count = Physics.OverlapSphereNonAlloc(point, radius, _hits, EnemyLayerMask);
            for (int i = 0; i < count; i++)
            {
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>(); // isabette, alandakiler
                if (enemy == null || enemy.IsDead) continue;
                enemy.TakeDamage(damage);
                Vector3 push = enemy.transform.position - point;
                push.y = 0f;
                enemy.Knockback(push, Knockback);
            }
            FireworkBurstFx.Puff(point, DustA, DustB, BlobScale, fragment ? 4 : 8, 2.5f, 6f);
            if (fragment) return;

            if (_puddleTime > 0f) SpawnPuddle(point, radius);
            if (_split)
                for (int k = 0; k < Fragments; k++)
                {
                    float a = (k / (float)Fragments + Random.Range(-0.08f, 0.08f)) * Mathf.PI * 2f;
                    Vector3 to = point + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (FragmentHop * SizeFactor);
                    Fire(point + Vector3.up * 0.2f, to, FragmentTime, 0.8f, true);
                }
        }

        private void SpawnPuddle(Vector3 point, float radius)
        {
            Puddle p = null;
            float oldest = float.MaxValue;
            for (int i = 0; i < PuddlePool; i++)
            {
                if (_puddles[i] == null) return;
                if (!_puddles[i].View.gameObject.activeSelf) { p = _puddles[i]; break; }
                if (_puddles[i].Until < oldest) { oldest = _puddles[i].Until; p = _puddles[i]; }
            }
            if (p == null) return;
            p.Center = point; p.Radius = radius;
            p.Until = Time.time + _puddleTime;
            p.Tick = 0f;
            p.View.position = new Vector3(point.x, 0.03f, point.z);
            p.View.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            p.View.localScale = Vector3.one * radius;
            p.View.gameObject.SetActive(true);
        }

        private void TickPuddle(Puddle p)
        {
            if (Time.time >= p.Until) { p.View.gameObject.SetActive(false); return; }
            p.Tick -= Time.deltaTime;
            if (p.Tick > 0f) return;
            p.Tick = PuddleTick;
            int count = Physics.OverlapSphereNonAlloc(p.Center, p.Radius, _hits, EnemyLayerMask);
            for (int i = 0; i < count; i++)
                _hits[i].GetComponent<EnemyBase>()?.ApplySlow(_puddleSlow, PuddleTick + 0.15f); // 0.25 sn'de bir
        }

        // En yakın iki düşman (havadakiler hariç: çöp yere düşer).
        private int FindTargets()
        {
            Vector3 origin = _blobRoot.position;
            int hitCount = Physics.OverlapSphereNonAlloc(origin, EffectiveRange, _hits, EnemyLayerMask);
            int found = 0;
            float d0 = float.MaxValue, d1 = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Transform t = _hits[i].transform;
                Vector3 flat = t.position - origin;
                flat.y = 0f;
                float sqr = flat.sqrMagnitude;
                if (sqr >= d1) continue;
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>(); // atış denemesinde
                if (enemy == null || enemy.IsDead || WeaponBase.IsAirborne(enemy)) continue;
                if (sqr < d0) { _targets[1] = _targets[0]; d1 = d0; _targets[0] = t; d0 = sqr; }
                else { _targets[1] = t; d1 = sqr; }
                found = Mathf.Min(found + 1, MaxTargets);
            }
            return found;
        }
    }
}
