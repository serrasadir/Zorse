using UnityEngine;

namespace BlobSurvivor.Entities
{
    // Değirmen kanadı kendi ekseninde döner (Crownreach kurulumu ekler, kanat static batching dışında).
    // Ekranda değilken çalışmaz: "en azından yanındayken" yeter.
    public class MillSpin : MonoBehaviour
    {
        [SerializeField] private Vector3 _axis = Vector3.forward; // yerel; kanat düzleminin normali
        [SerializeField] private float _speed = 40f;              // derece/sn

        private Renderer _renderer;
        private Quaternion _baseRot;
        private float _angle;

        public void Setup(Vector3 axis, float speed)
        {
            _axis = axis;
            _speed = speed;
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _baseRot = transform.localRotation;
            _angle = Random.Range(0f, 360f);
        }

        private void Update()
        {
            if (_renderer != null && !_renderer.isVisible) return;
            _angle = (_angle + _speed * Time.deltaTime) % 360f;
            transform.localRotation = _baseRot * Quaternion.AngleAxis(_angle, _axis);
        }
    }
}
