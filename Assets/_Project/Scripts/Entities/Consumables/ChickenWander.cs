using UnityEngine;

namespace BlobSurvivor.Entities
{
    // Kale haritasının tavukları (Bahar, 2026-11-21): yerleştirildiği noktanın çevresinde gezinir, durur, gagalar.
    // Fizik sorgusu yok: hep başlangıç noktasına yakın (Radius) kalır, yerleşim tavukları boş yerlere koyar.
    // Prefab static değil (static batching'de collider kayardı). Yenip geri açılınca (run sonu) başlangıç yerine döner.
    public class ChickenWander : MonoBehaviour
    {
        private const float Radius = 2.2f;
        private const float Speed = 0.9f;
        private const float HopHeight = 0.06f;
        private const float HopRate = 14f;

        private Vector3 _home;
        private Quaternion _homeRot;
        private Vector3 _target;
        private float _wait;
        private bool _moving;
        private Transform _model;
        private Vector3 _modelPos;

        private void Awake()
        {
            _home = transform.position;
            _homeRot = transform.rotation;
            if (transform.childCount > 0)
            {
                _model = transform.GetChild(0);
                _modelPos = _model.localPosition;
            }
        }

        private void OnEnable()
        {
            transform.SetPositionAndRotation(_home, _homeRot);
            _moving = false;
            _wait = Random.Range(0.3f, 2.5f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (!_moving)
            {
                _wait -= dt;
                if (_model != null) _model.localPosition = _modelPos;
                if (_wait > 0f) return;
                Vector2 r = Random.insideUnitCircle * Radius;
                _target = _home + new Vector3(r.x, 0f, r.y);
                _moving = true;
                return;
            }

            Vector3 to = _target - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.05f)
            {
                _moving = false;
                _wait = Random.Range(0.8f, 3f);
                return;
            }
            Vector3 dir = to / dist;
            transform.position += dir * Mathf.Min(dist, Speed * dt);
            // Modelin ön yüzü −Z: arkası gidiş yönüne bakar.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(-dir), 540f * dt);
            if (_model != null)
                _model.localPosition = _modelPos + Vector3.up * (Mathf.Abs(Mathf.Sin(Time.time * HopRate)) * HopHeight);
        }
    }
}
