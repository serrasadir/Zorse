using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Ejderha kanat çırpması: iki kanat omuzdaki pivotlarından gövde boyunca uzanan eksende iner kalkar.
    // Eksenler kurulumda gövde uzayında hesaplanır (MedievalSkinSetup.SkinDragon); +açı kanadı kaldırır.
    // Ekran dışındayken poz yazmaz.
    public class WingFlap : MonoBehaviour
    {
        [SerializeField] private Transform _left;
        [SerializeField] private Transform _right;
        [SerializeField] private Vector3 _leftAxis = Vector3.forward;
        [SerializeField] private Vector3 _rightAxis = Vector3.back;
        [SerializeField] private Renderer _body;
        [SerializeField] private float _amplitude = 32f; // derece
        [SerializeField] private float _lift = 8f;       // ortalama hafif yukarıda (süzülme)
        [SerializeField] private float _frequency = 1.4f; // çırpış/sn

        private float _phase;

        private void OnEnable() => _phase = Random.value;

        private void Update()
        {
            _phase += Time.deltaTime * _frequency;
            if (_phase > 1f) _phase -= 1f;
            if (_body != null && !_body.isVisible) return;
            // Aşağı vuruş hızlı, kalkış yavaş: sinüsün eğik hali.
            float s = Mathf.Sin(_phase * 2f * Mathf.PI);
            float angle = _lift + _amplitude * (s >= 0f ? s : s * 0.8f);
            if (_left != null) _left.localRotation = Quaternion.AngleAxis(angle, _leftAxis);
            if (_right != null) _right.localRotation = Quaternion.AngleAxis(angle, _rightAxis);
        }
    }
}
