using UnityEngine;

namespace BlobSurvivor.Entities
{
    // Parçalı modelde yürüyüş (polis/elit/çevik kuvvet; Tools → Blob.io → Polis Yürüyüş Parçalarını Kur kurar):
    // bacaklar karşılıklı, kollar karşı bacakla eklemde sallanır. Faz gidilen yola bağlı, açı hızla büyür, dururken
    // (vururken, nişan alırken) eklemler yumuşakça düzleşir. Eksen mesh-yerel sol-sağ; parçalar Model'in child'ı.
    public class LimbWalk : MonoBehaviour
    {
        [SerializeField] private Transform _legL, _legR, _armL, _armR; // kol boş olabilir (kalkan tutan kol)
        [SerializeField] private Vector3 _axis = Vector3.right;
        [SerializeField] private float _stepRate = 4.5f;  // birim yol başına faz (radyan)
        [SerializeField] private float _legSwing = 34f;   // derece
        [SerializeField] private float _armSwing = 28f;
        [SerializeField] private float _fullSpeed = 2.3f; // bu hızda tam açı

        private Vector3 _lastPosition;
        private float _phase, _amount;

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _amount = 0f;
            Pose(0f);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > 2f) distance = 0f; // ışınlanma (öne taşıma, havuz)
            _phase += distance * _stepRate;
            float target = Mathf.Clamp01(distance / dt / Mathf.Max(0.1f, _fullSpeed));
            _amount = Mathf.MoveTowards(_amount, target, dt * 6f);
            Pose(Mathf.Sin(_phase) * _amount);
        }

        private void Pose(float s)
        {
            if (_legL != null) _legL.localRotation = Quaternion.AngleAxis(s * _legSwing, _axis);
            if (_legR != null) _legR.localRotation = Quaternion.AngleAxis(-s * _legSwing, _axis);
            if (_armL != null) _armL.localRotation = Quaternion.AngleAxis(-s * _armSwing, _axis);
            if (_armR != null) _armR.localRotation = Quaternion.AngleAxis(s * _armSwing, _axis);
        }
    }
}
