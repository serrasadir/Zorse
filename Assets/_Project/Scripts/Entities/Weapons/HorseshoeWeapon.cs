using UnityEngine;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Core;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Weapons
{
    // Nal Bumerangı (Mıknato'nun silahı, kart): nalı en yakın düşmana fırlatır, nal menzilin sonuna kadar düz gider,
    // mıknatısla geri döner; gidişte de dönüşte de yolundaki herkese vurur (delip geçer). HorseshoeEffect kurar ve seviyeler:
    // Sv.2 hasar + menzil · Sv.3 2 nal (ayrı hedefler) · Sv.4 dönüşte coin/kalp toplar + atış hızı ×1.3 · Sv.5 dönünce blob'un
    // çevresinde bir tur döner, hasar ×1.5. Nallar dünyada yaşar (blob'un child'ı değil), bileşenle birlikte silinir.
    public class HorseshoeWeapon : MonoBehaviour
    {
        private enum Phase { Idle, Out, Back, Orbit }

        private class Nal
        {
            public Transform View;
            public Phase Phase;
            public Vector3 Start, Dir, Last;
            public float T, Speed, Angle;
            public readonly EnemyBase[] Hit = new EnemyBase[24];
            public int HitCount;
            public readonly Transform[] Carried = new Transform[8];
            public int CarriedCount;
        }

        private const int MaxNals = 2;
        private const int EnemyLayerMask = 1 << 14;
        private const int WallMask = 1 << 16;
        private const float OutTime = 0.45f;        // menzilin sonuna varış (yavaşlayarak)
        private const float BackSpeedFactor = 1.5f; // dönüş, gidişin ortalama hızına göre
        private const float OrbitTime = 0.5f;
        private const float OrbitGap = 1f;          // tur yarıçapı = gövde + bu
        private const float HitRadius = 0.45f;      // mermi boyuyla çarpılır
        private const float CheckInterval = 0.04f;  // süpürme kontrolü (kapsül: kaçırmaz)
        private const float SpinSpeed = 900f;
        private const float MuzzleHeight = 0.7f;
        private const float Knockback = 0.25f;
        private const float NoTargetRetry = 0.1f;
        private const float TinyBlobScale = 0.5f;

        private float _damage = 10f;
        private float _range = 6f;     // gövde yüzeyinden
        private float _interval = 1.6f;
        private int _count = 1;
        private bool _collect, _orbit;
        private float _damageScale = 1f;

        private readonly Nal[] _nals = new Nal[MaxNals];
        private readonly Collider[] _hits = new Collider[32];
        private readonly Transform[] _targets = new Transform[MaxNals];
        private Transform _blobRoot;
        private float _bodyLocalRadius = 0.5f;
        private float _cooldown, _checkTimer;
        private int _shotCount;

        private float BodyRadius => _blobRoot != null ? _blobRoot.localScale.x * _bodyLocalRadius : 0.25f;
        private float EffectiveRange => BodyRadius + _range;
        private float VisualScale => Mathf.Clamp(Mathf.Sqrt((_blobRoot != null ? _blobRoot.localScale.x : TinyBlobScale) / TinyBlobScale), 1f, 3f);

        public void Setup(Mesh mesh, Material material, float damage, float range, float interval)
        {
            _damage = damage; _range = range; _interval = interval;
            _blobRoot = transform.parent;
            if (_blobRoot != null)
                foreach (SphereCollider c in _blobRoot.GetComponents<SphereCollider>())
                    if (!c.isTrigger) { _bodyLocalRadius = c.radius; break; }
            for (int i = 0; i < MaxNals; i++) // run başına tek seferlik
            {
                var go = new GameObject("Nal" + i);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                _nals[i] = new Nal { View = go.transform };
            }
        }

        public void AddDamage(float damage, float range) { _damage += damage; _range += range; }
        public void SetCount(int count) => _count = Mathf.Clamp(count, 1, MaxNals);
        public void EnableCollect(float fireRateFactor) { _collect = true; _interval /= fireRateFactor; }
        public void EnableOrbit(float damageScale) { _orbit = true; _damageScale = damageScale; }

        private void OnEnable() => BlobSurvivor.Core.GameEvents.OnEnemyKilled += HandleEnemyKilled;
        private void OnDisable() => BlobSurvivor.Core.GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        private void HandleEnemyKilled(BlobSurvivor.Data.EnemyData _)
        {
            if (BlobSkills.KillCooldownRefund > 0f) _cooldown -= BlobSkills.KillCooldownRefund; // Tetikçi Sv.5
        }

        private void OnDestroy()
        {
            foreach (Nal n in _nals)
                if (n != null && n.View != null) Destroy(n.View.gameObject);
        }

        private void Update()
        {
            if (_blobRoot == null) return;
            float dt = Time.deltaTime;
            bool anyFlying = false;
            for (int i = 0; i < MaxNals; i++)
                if (_nals[i] != null && _nals[i].Phase != Phase.Idle) { Move(_nals[i], dt); anyFlying = true; }

            _checkTimer -= dt;
            if (anyFlying && _checkTimer <= 0f)
            {
                _checkTimer = CheckInterval;
                for (int i = 0; i < MaxNals; i++)
                    if (_nals[i] != null && _nals[i].Phase != Phase.Idle) Sweep(_nals[i]);
            }

            _cooldown -= dt;
            if (_cooldown > 0f || anyFlying) return;
            int found = FindTargets();
            if (found == 0) { _cooldown = NoTargetRetry; return; }
            for (int i = 0; i < _count; i++)
                Throw(_nals[i], _targets[i % found]);
            _cooldown = BlobSkills.NextCooldown(ref _shotCount, _interval);
            Haptics.LightTap();
        }

        private Vector3 Muzzle => new Vector3(_blobRoot.position.x, MuzzleHeight, _blobRoot.position.z);

        private void Throw(Nal n, Transform target)
        {
            Vector3 dir = target.position - _blobRoot.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            n.Dir = dir.normalized;
            n.Start = Muzzle;
            n.Last = n.Start;
            n.T = 0f;
            n.Speed = EffectiveRange / OutTime;
            n.HitCount = 0;
            n.CarriedCount = 0;
            n.Phase = Phase.Out;
            n.View.position = n.Start;
            n.View.localScale = Vector3.one * VisualScale;
            n.View.gameObject.SetActive(true);
        }

        private void Move(Nal n, float dt)
        {
            n.View.Rotate(0f, SpinSpeed * dt, 0f, Space.World);
            switch (n.Phase)
            {
                case Phase.Out:
                {
                    n.T += dt / OutTime;
                    float e = 1f - (1f - Mathf.Min(1f, n.T)) * (1f - Mathf.Min(1f, n.T)); // yavaşlayarak durur
                    n.View.position = n.Start + n.Dir * (EffectiveRange * e);
                    if (n.T >= 1f) BeginPhase(n, Phase.Back);
                    break;
                }
                case Phase.Back:
                {
                    Vector3 home = Muzzle;
                    Vector3 to = home - n.View.position;
                    float step = n.Speed * BackSpeedFactor * dt;
                    if (to.sqrMagnitude <= step * step || to.sqrMagnitude <= BodyRadius * BodyRadius)
                    {
                        n.View.position = home;
                        DropCarried(n);
                        if (_orbit) { BeginPhase(n, Phase.Orbit); n.Angle = Mathf.Atan2(-to.z, -to.x); }
                        else Finish(n);
                        break;
                    }
                    n.View.position += to.normalized * step;
                    CarryPickups(n);
                    break;
                }
                case Phase.Orbit:
                {
                    n.T += dt / OrbitTime;
                    float a = n.Angle + n.T * Mathf.PI * 2f;
                    float r = BodyRadius + OrbitGap;
                    n.View.position = Muzzle + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    if (n.T >= 1f) Finish(n);
                    break;
                }
            }
        }

        private void BeginPhase(Nal n, Phase phase)
        {
            n.Phase = phase;
            n.T = 0f;
            n.HitCount = 0; // dönüşte aynı düşmana tekrar vurur
            n.Last = n.View.position;
        }

        private void Finish(Nal n)
        {
            n.Phase = Phase.Idle;
            n.View.gameObject.SetActive(false);
        }

        // Son kontrolden bu yana gidilen yol boyunca (kapsül) düşmanlara vurur; her düşmana aşama başına bir kez.
        private void Sweep(Nal n)
        {
            Vector3 cur = n.View.position;
            float radius = HitRadius * VisualScale;
            int count = Physics.OverlapCapsuleNonAlloc(n.Last, cur, radius, _hits, EnemyLayerMask);
            n.Last = cur;
            float damage = _damage * _damageScale * BlobSkills.WeaponDamageScale;
            for (int i = 0; i < count; i++)
            {
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>(); // değen düşman başına, saniyede birkaç kez
                if (enemy == null || enemy.IsDead || AlreadyHit(n, enemy)) continue;
                if (n.HitCount < n.Hit.Length) n.Hit[n.HitCount++] = enemy;
                enemy.TakeDamage(damage);
                Vector3 push = enemy.transform.position - cur;
                push.y = 0f;
                enemy.Knockback(push, Knockback);
            }
        }

        private static bool AlreadyHit(Nal n, EnemyBase enemy)
        {
            for (int i = 0; i < n.HitCount; i++) if (n.Hit[i] == enemy) return true;
            return false;
        }

        // Sv.4: dönüş yolundaki coin ve kalpler nala yapışıp blob'a gelir (blob değince toplanır).
        private void CarryPickups(Nal n)
        {
            if (!_collect) { return; }
            for (int i = 0; i < n.CarriedCount; i++)
                if (n.Carried[i] != null && n.Carried[i].gameObject.activeInHierarchy) n.Carried[i].position = n.View.position;
            if (_checkTimer > 0f || n.CarriedCount >= n.Carried.Length) return;
            int count = Physics.OverlapSphereNonAlloc(n.View.position, HitRadius * VisualScale * 2f, _hits, TierLayers.ConsumableMask);
            for (int i = 0; i < count && n.CarriedCount < n.Carried.Length; i++)
            {
                Collider c = _hits[i];
                if (c.GetComponent<Coins.CoinPickup>() == null && c.GetComponent<Loot.HeartPickup>() == null) continue;
                Transform t = c.transform;
                bool known = false;
                for (int j = 0; j < n.CarriedCount; j++) if (n.Carried[j] == t) { known = true; break; }
                if (!known) n.Carried[n.CarriedCount++] = t;
            }
        }

        private void DropCarried(Nal n)
        {
            Vector3 p = _blobRoot.position;
            for (int i = 0; i < n.CarriedCount; i++)
            {
                if (n.Carried[i] != null && n.Carried[i].gameObject.activeInHierarchy)
                    n.Carried[i].position = new Vector3(p.x, n.Carried[i].position.y, p.z);
                n.Carried[i] = null;
            }
            n.CarriedCount = 0;
        }

        // En yakın _count düşman (havadakiler hariç: nal yerden gider), görüş hattı açık olanlar.
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
                if (sqr >= d1 && found >= MaxNals) continue;
                EnemyBase enemy = _hits[i].GetComponent<EnemyBase>(); // ateş denemesinde, saniyede en çok 10 kez
                if (enemy == null || enemy.IsDead || WeaponBase.IsAirborne(enemy)) continue;
                if (Physics.Linecast(Muzzle, _hits[i].bounds.center, WallMask, QueryTriggerInteraction.Ignore)) continue;
                if (sqr < d0) { _targets[1] = _targets[0]; d1 = d0; _targets[0] = t; d0 = sqr; }
                else if (sqr < d1) { _targets[1] = t; d1 = sqr; }
                found = Mathf.Min(found + 1, MaxNals);
            }
            return found;
        }
    }
}
