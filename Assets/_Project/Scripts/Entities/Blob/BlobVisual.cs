using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-04: karaktere özel blob görünümü (CharacterData.BlobVisualPrefab, ör. Topik = yüzlü turuncu top).
    // Prefab blob'un child'ı olur; kökteki küre renderer'ı kapanır, büyüme kök ölçeğiyle aynen çalışır.
    // Prefab null ise varsayılan küre geri açılır (görünümü olmayan karakterler).
    // Yüz prefabın +Z'sinde; gidiş yönüne yumuşakça döner (blob fiziksel olarak dönmüyor, rotasyon kilitli).
    // GameManager.ApplyCharacter her run başında çağırır — restart'ta eski görünüm silinip yeniden kurulur.
    public class BlobVisual : MonoBehaviour
    {
        private const float TurnSpeed = 10f;
        private const float MinTurnSpeed = 0.3f; // birim/sn; bunun altında (durunca, itilince) yön korunur

        private Renderer _defaultRenderer;
        private Rigidbody _rigidbody;
        private DamageFlash _flash;
        private Transform _visual;
        private Quaternion _facing = Quaternion.LookRotation(Vector3.back); // başta kameraya bakar
        private Vector3 _lastPosition;

        private void Awake()
        {
            _defaultRenderer = GetComponent<Renderer>();
            _rigidbody = GetComponent<Rigidbody>();
            _lastPosition = transform.position;
        }

        public void Apply(GameObject prefab)
        {
            if (_flash == null) _flash = GetComponent<DamageFlash>();
            if (_visual != null)
            {
                Destroy(_visual.gameObject);
                _visual = null;
            }

            _facing = Quaternion.LookRotation(Vector3.back);
            _lastPosition = transform.position;

            if (prefab == null)
            {
                if (_defaultRenderer != null) _defaultRenderer.enabled = true;
                _flash?.SetRenderer(_defaultRenderer);
                return;
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.name = "Visual";
            _visual = instance.transform;
            _visual.localPosition = Vector3.zero;
            _visual.localScale = Vector3.one;
            _visual.rotation = _facing;

            if (_defaultRenderer != null) _defaultRenderer.enabled = false;
            _flash?.SetRenderer(instance.GetComponentInChildren<Renderer>());
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            _lastPosition = position;
            if (_visual == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return; // duraklatma

            // Gidiş yönü: önce Rigidbody hızı (BlobController her FixedUpdate'te set ediyor), yoksa kare farkı.
            Vector3 velocity = _rigidbody != null ? _rigidbody.linearVelocity : delta / dt;
            velocity.y = 0f;
            if (velocity.magnitude > MinTurnSpeed)
            {
                Quaternion target = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                _facing = Quaternion.Slerp(_facing, target, 1f - Mathf.Exp(-TurnSpeed * dt));
            }
            _visual.rotation = _facing;
        }
    }
}
