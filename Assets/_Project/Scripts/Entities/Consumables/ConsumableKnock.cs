using UnityEngine;

namespace BlobSurvivor.Entities
{
    // Araba çarpınca yerdeki küçük obje yana savrulur: hızla kayar, yavaşlayarak durur, biraz döner.
    // Sadece kayarken Update çalışır (enabled), durunca kendini kapatır. CarController ilk çarpışmada ekler.
    public class ConsumableKnock : MonoBehaviour
    {
        private const float Deceleration = 14f;   // m/sn²
        private const float SpinPerSpeed = 120f;  // derece/sn, hız başına
        private const int EnvironmentMask = 1 << 16;

        private Vector3 _velocity;
        private float _spin;

        public bool IsSliding => enabled;

        private void Awake() => enabled = false;

        public void Knock(Vector3 velocity)
        {
            velocity.y = 0f;
            float speed = velocity.magnitude;
            if (speed < 0.01f) return;

            // Bina/duvar varsa savrulma mesafesini kısalt (duvarın içine girmesin).
            float stopDistance = speed * speed / (2f * Deceleration);
            Vector3 dir = velocity / speed;
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, stopDistance + 0.3f, EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                float allowed = Mathf.Max(0f, hit.distance - 0.3f);
                speed = Mathf.Sqrt(2f * Deceleration * allowed);
                if (speed < 0.01f) return;
            }

            _velocity = dir * speed;
            _spin = (Random.value < 0.5f ? -1f : 1f) * speed * SpinPerSpeed;
            enabled = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += _velocity * dt;
            transform.Rotate(0f, _spin * dt, 0f, Space.World);

            float speed = _velocity.magnitude;
            float newSpeed = speed - Deceleration * dt;
            if (newSpeed <= 0f) { enabled = false; return; }
            _velocity *= newSpeed / speed;
            _spin *= newSpeed / speed;
        }

        private void OnDisable() => _velocity = Vector3.zero;
    }
}
