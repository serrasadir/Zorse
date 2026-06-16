using UnityEngine;


namespace BlobSurvivor.Entities.Blob
{
    public class MagnetComponent : MonoBehaviour
    {
        [SerializeField] private float _pullSpeed = 12f;

        public float Radius { get; private set; }

        private readonly Collider[] _hits = new Collider[32];

        private void Update()
        {
            if (Radius <= 0f) return;

            int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, _hits);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].gameObject == gameObject) continue;
                if (_hits[i].GetComponent<IConsumable>() == null) continue;

                Transform target = _hits[i].transform;
                target.position = Vector3.MoveTowards(target.position, transform.position, _pullSpeed * Time.deltaTime);
            }
        }

        public void IncreaseRadius(float amount) => Radius += amount;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, Radius);
        }
    }
}
