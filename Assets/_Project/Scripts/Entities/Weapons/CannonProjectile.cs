using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Weapons
{
    public class CannonProjectile : Projectile
    {
        [SerializeField] private float _aoeRadius = 2f;
        [SerializeField] private float _arcHeight = 1.2f;

        private const int EnemyLayerMask = 1 << 14;
        private readonly Collider[] _hits = new Collider[16];
        private float _baseY;
        private bool _baseYSet;

        // Yay yüksekliğini artık SphereCast'ten SONRA değil, hareketin bir parçası olarak
        // uyguluyoruz — eskiden mermi çarpışma kontrolünden habersiz yükselip alçalıyordu,
        // bu da yay tepe noktasındayken düşmanın üzerinden uçup gitmesine sebep oluyordu.
        protected override Vector3 GetExtraMotion()
        {
            if (!_baseYSet)
            {
                _baseY = transform.position.y;
                _baseYSet = true;
            }

            float currentY = _baseY + Mathf.Sin(LifetimeFraction * Mathf.PI) * _arcHeight;
            float nextY = _baseY + Mathf.Sin(NextLifetimeFraction * Mathf.PI) * _arcHeight;
            return new Vector3(0f, nextY - currentY, 0f);
        }

        protected override void OnHitEnemy(EnemyBase enemy)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _aoeRadius, _hits, EnemyLayerMask);
            for (int i = 0; i < count; i++)
            {
                EnemyBase splash = _hits[i].GetComponent<EnemyBase>();
                if (splash != null && splash != enemy)
                    splash.TakeDamage(Damage);
            }
        }
    }
}
