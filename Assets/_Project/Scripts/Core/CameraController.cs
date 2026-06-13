using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Core
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _followSmoothing = 5f;

        [Header("Height & Angle")]
        [SerializeField] private float _baseHeight = 15f;
        [SerializeField] private float _baseTilt = 60f;

        [Header("Zoom by Tier")]
        [SerializeField] private float[] _heightPerTier = { 15f, 20f, 30f, 45f, 65f };
        [SerializeField] private float _zoomSmoothing = 3f;

        [Header("Bounds")]
        [SerializeField] private bool _useBounds;
        [SerializeField] private Vector2 _minBounds;
        [SerializeField] private Vector2 _maxBounds;

        private float _targetHeight;
        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _targetHeight = _baseHeight;
        }

        private void Start()
        {
            if (_target != null)
                transform.position = CalculatePosition(_target.position);

            transform.rotation = Quaternion.Euler(_baseTilt, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desiredPos = CalculatePosition(_target.position);
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * _followSmoothing);
        }

        private Vector3 CalculatePosition(Vector3 targetPos)
        {
            float tiltRad = _baseTilt * Mathf.Deg2Rad;
            float zOffset = -_baseHeight / Mathf.Tan(tiltRad);

            Vector3 pos = new Vector3(targetPos.x, _baseHeight, targetPos.z + zOffset);

            if (_useBounds)
            {
                pos.x = Mathf.Clamp(pos.x, _minBounds.x, _maxBounds.x);
                pos.z = Mathf.Clamp(pos.z, _minBounds.y, _maxBounds.y);
            }

            return pos;
        }

        public void SetTarget(Transform target) => _target = target;
    }
}
