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
        private float _gulpTime = -1f, _gulpAmount;
        private float _recoilTime = -1f;
        private Vector3 _recoilLocalDir;
        private const float RecoilDuration = 0.14f;
        private const float RecoilDistance = 0.09f; // kök ölçeğinde: çapın ~%9'u

        // Yüzü olan görünüm takılıysa "önle yeme" kuralı (BlobConsumption.CanEatFrom) bu yönü kullanır.
        public bool HasFacing => _visual != null;
        public Vector3 Forward => _facing * Vector3.forward;

        // Giriş ekranı: kameraya bakarken yavaşça sağa sola bakınır (ölçeksiz zaman; oyun donukken de).
        public bool MenuIdle { get; set; }
        private const float GulpDuration = 0.28f;

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
            _gulpTime = -1f;
            _recoilTime = -1f;

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

            if (MenuIdle)
            {
                float t = Time.unscaledTime;
                // Ara ara iki yana bakıp ortaya döner; hafif kafa sallama.
                float look = Mathf.Sin(t * 0.7f) * 32f * Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Sin(t * 0.35f)));
                float nod = Mathf.Sin(t * 1.3f) * 4f;
                _facing = Quaternion.Euler(nod, look, 0f) * Quaternion.LookRotation(Vector3.back);
                _visual.rotation = _facing;
                return;
            }

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
            UpdateGulp(dt);
            UpdateRecoil(dt);
        }

        // Silah ateşlenince görünüm ateş yönünün tersine kısa bir tepme yapar (sadece görsel child).
        public void Recoil(Vector3 shotDirection)
        {
            if (_visual == null) return;
            shotDirection.y = 0f;
            if (shotDirection.sqrMagnitude < 0.0001f) return;
            _recoilLocalDir = transform.InverseTransformDirection(-shotDirection.normalized).normalized;
            _recoilTime = 0f;
        }

        private void UpdateRecoil(float dt)
        {
            if (_recoilTime < 0f) return;
            _recoilTime += dt;
            float t = _recoilTime / RecoilDuration;
            if (t >= 1f)
            {
                _recoilTime = -1f;
                _visual.localPosition = Vector3.zero;
                return;
            }
            // Hızlı tepme, yumuşak dönüş.
            float k = t < 0.25f ? t / 0.25f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.25f) / 0.75f);
            _visual.localPosition = _recoilLocalDir * (RecoilDistance * k);
        }

        // Büyük lokmada "yutkunma": görünüm bir an basıklaşıp yanlara şişer, geri gelir. Sadece görsel child —
        // kök ölçeği (collider) değişmez. Varsayılan kürede (görünüm yok) çalışmaz.
        public void Gulp(float amount)
        {
            if (_visual == null) return;
            _gulpAmount = Mathf.Max(amount, _gulpTime >= 0f ? _gulpAmount : 0f);
            _gulpTime = 0f;
        }

        private void UpdateGulp(float dt)
        {
            if (_gulpTime < 0f) return;
            _gulpTime += dt;
            float t = _gulpTime / GulpDuration;
            if (t >= 1f)
            {
                _gulpTime = -1f;
                _visual.localScale = Vector3.one;
                return;
            }
            float s = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f) * _gulpAmount;
            _visual.localScale = new Vector3(1f + s * 0.6f, 1f - s, 1f + s * 0.6f);
        }
    }
}
