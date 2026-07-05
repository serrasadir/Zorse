using UnityEngine;
using BlobSurvivor.Entities;

namespace BlobSurvivor.Entities.Blob
{
    public class VacuumComponent : MonoBehaviour
    {
        [SerializeField] private float _pullSpeed = 6f;
        [SerializeField] private float _baseRadius = 3f;

        public float Radius { get; private set; }

        private void Reset() => Radius = _baseRadius;
        private void OnEnable() { if (Radius <= 0f) Radius = _baseRadius; }

        private BlobGrowth _growth;
        private readonly Collider[] _hits = new Collider[64];

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
        }

        private void Update()
        {
            if (Radius <= 0f) return;

            int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, _hits);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].gameObject == gameObject) continue;

                var consumable = _hits[i].GetComponent<IConsumable>();
                if (consumable == null) continue;
                if (_growth != null && consumable.RequiredTier > _growth.CurrentTier) continue;

                Transform target = _hits[i].transform;
                target.position = Vector3.MoveTowards(target.position, transform.position, _pullSpeed * Time.deltaTime);
            }
        }

        public void IncreaseRadius(float amount) => Radius += amount;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.2f);
            Gizmos.DrawSphere(transform.position, Radius);
        }
    }
}
