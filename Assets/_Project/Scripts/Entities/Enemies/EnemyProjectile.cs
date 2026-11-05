using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Enemies
{
    // Elit polisin pooled mermisi. Oyuncu mermisi (Projectile) gibi Rigidbody/Collider kullanmaz.
    // Blob isabeti collider yerine XZ düzleminde hesaplanır: SphereCast başlangıçta iç içe olduğu
    // collider'ı görmez, bu yüzden blob'a dipten ateşlenen mermi içinden geçerdi.
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float _hitRadius = 0.2f;
        // >0: roket gibi patlar (helikopter mermisi). Blob'a, yere ya da duvara değince ya da menzil bitince
        // patlama noktası; blob yüzeyi bu yarıçap içindeyse hasar alır (ıskalayıp yakına düşen de vurur).
        [SerializeField] private float _splashRadius;
        private const float GroundY = 0.1f;

        private static int s_environmentMask = -1;

        private ObjectPool<EnemyProjectile> _pool;
        private BlobHealth _blobHealth;
        private Transform _blobTransform;
        private float _blobLocalRadius;
        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _remainingDistance;

        private void Awake()
        {
            if (s_environmentMask < 0)
                s_environmentMask = LayerMask.GetMask("Environment");
        }

        // Restart sahneyi yeniden yüklemediği için havadaki mermiler yeni run'a taşınmamalı.
        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRestart;

        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRestart;

        private void HandleRestart(CharacterData _) => ReturnToPool();

        public void Launch(ObjectPool<EnemyProjectile> pool, BlobHealth blobHealth, float blobLocalRadius,
            Vector3 direction, float speed, float maxDistance, float damage)
        {
            _pool = pool;
            _blobHealth = blobHealth;
            _blobTransform = blobHealth != null ? blobHealth.transform : null;
            _blobLocalRadius = blobLocalRadius;
            _direction = direction;
            // Roket görünümlü mermide (helikopter) burun gidiş yönüne baksın; küre mermide fark etmez.
            if (direction.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(direction);
            _speed = speed;
            _damage = damage;
            _remainingDistance = maxDistance;
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            if (step <= 0f) return;

            bool hitWall = Physics.Raycast(transform.position, _direction, out RaycastHit hit, step,
                s_environmentMask, QueryTriggerInteraction.Collide);
            if (hitWall) step = hit.distance;

            Vector3 end = transform.position + _direction * step;
            if (SegmentHitsBlob(transform.position, end))
            {
                if (_splashRadius > 0f) { Explode(end); return; }
                _blobHealth.TakeDamage(_damage);
                ReturnToPool();
                return;
            }

            if (_splashRadius > 0f && end.y <= GroundY && _direction.y < 0f)
            {
                float k = (transform.position.y - GroundY) / Mathf.Max(1e-4f, transform.position.y - end.y);
                Explode(Vector3.Lerp(transform.position, end, Mathf.Clamp01(k)));
                return;
            }

            transform.position = end;
            _remainingDistance -= step;
            if (hitWall || _remainingDistance <= 0f)
            {
                if (_splashRadius > 0f) { Explode(end); return; }
                ReturnToPool();
            }
        }

        private void Explode(Vector3 point)
        {
            if (_blobTransform != null && _blobHealth != null)
            {
                Vector3 d = _blobTransform.position - point;
                d.y = 0f;
                float blobRadius = _blobTransform.localScale.x * _blobLocalRadius;
                if (d.magnitude - blobRadius <= _splashRadius) _blobHealth.TakeDamage(_damage);
            }
            ProjectileBlastFx.Play(new Vector3(point.x, Mathf.Max(point.y, GroundY), point.z), _splashRadius);
            ReturnToPool();
        }

        private bool SegmentHitsBlob(Vector3 start, Vector3 end)
        {
            if (_blobTransform == null) return false;

            Vector2 from = new Vector2(start.x, start.z);
            Vector2 segment = new Vector2(end.x, end.z) - from;
            Vector2 toBlob = new Vector2(_blobTransform.position.x, _blobTransform.position.z) - from;

            float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(toBlob, segment) / segment.sqrMagnitude) : 0f;
            float hitDistance = _blobTransform.localScale.x * _blobLocalRadius + _hitRadius;
            return (toBlob - segment * t).sqrMagnitude <= hitDistance * hitDistance;
        }

        private void ReturnToPool()
        {
            if (_pool != null)
                _pool.Return(this);
            else
                gameObject.SetActive(false);
        }
    }
}
