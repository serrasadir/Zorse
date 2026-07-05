using UnityEngine;

namespace BlobSurvivor.Entities.Weapons
{
    public class HomingProjectile : Projectile
    {
        [SerializeField] private float _turnSpeed = 3f;

        private Transform _target;

        public void SetTarget(Transform target) => _target = target;

        protected override void Update()
        {
            if (_target != null)
            {
                Vector3 toTarget = _target.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    Direction = Vector3.Slerp(Direction, toTarget.normalized, _turnSpeed * Time.deltaTime);
            }

            base.Update();
        }
    }
}
