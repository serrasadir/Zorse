using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    public class HomingProjectile : Projectile
    {
        [SerializeField] private float _turnSpeed = 3f;
        [SerializeField] private float _detonateRadius = 0.6f;
        // Silah Gücü Sv.8 (Mıknato son formu "Sekme"): isabetten sonra en yakın düşmana seker.
        [SerializeField] private int _ultimateBounces = 2;
        [SerializeField] private float _bounceRange = 6f;

        private const int EnemyLayerMask = 1 << 14;
        private const int WallMask = 1 << 16; // sekme duvar arkasındaki düşmana gitmesin
        private static readonly Collider[] s_bounceHits = new Collider[16];

        private Transform _target;
        private int _bouncesLeft;

        public void SetTarget(Transform target) => _target = target;

        protected override void OnLaunched() => _bouncesLeft = IsUltimate ? _ultimateBounces : 0;

        protected override void Update()
        {
            // Hedef öldü/havuza döndüyse bırak — yoksa havuzdan başka yerde yeniden doğunca onu kovalardı.
            if (_target != null && !_target.gameObject.activeInHierarchy)
                _target = null;

            if (_target != null)
            {
                Vector3 toTarget = _target.position - transform.position;
                toTarget.y = 0f;

                // Dönüş hızı hedefi ıskalarsa mermi etrafında sonsuz tur atabilir (pure-pursuit
                // homing'in klasik sorunu) — yakınlık kontrolü bunu garanti şekilde kapatır.
                if (toTarget.sqrMagnitude <= _detonateRadius * _detonateRadius)
                {
                    EnemyBase enemy = _target.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(Damage);
                        OnHitEnemy(enemy);
                    }

                    if (TryBounce(enemy)) return;
                    if (enemy != null && TryPierce(enemy))
                    {
                        _target = null; // delip düz devam eder, yolundakini base.Update'in SphereCast'i vurur
                        return;
                    }

                    ReturnToPool();
                    return;
                }

                if (toTarget.sqrMagnitude > 0.0001f)
                    Direction = Vector3.Slerp(Direction, toTarget.normalized, _turnSpeed * Time.deltaTime);
            }

            base.Update();
        }

        // Vurulan düşman hariç en yakın düşmana yönelir; bulursa true.
        private bool TryBounce(EnemyBase hit)
        {
            if (_bouncesLeft <= 0) return false;

            int count = Physics.OverlapSphereNonAlloc(transform.position, _bounceRange, s_bounceHits, EnemyLayerMask);
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Transform t = s_bounceHits[i].transform;
                if (hit != null && t == hit.transform) continue;
                if (Physics.Linecast(transform.position, t.position, WallMask, QueryTriggerInteraction.Ignore)) continue;
                float sqr = (t.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = t;
                }
            }

            if (best == null) return false;

            _bouncesLeft--;
            _target = best;
            Vector3 dir = best.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) Direction = dir.normalized; // sekme keskin olsun, yavaş dönmesin
            return true;
        }
    }
}
