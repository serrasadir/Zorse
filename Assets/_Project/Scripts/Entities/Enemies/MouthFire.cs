using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Nişan lazeri yerine ağızda biriken ateş topu (ejderha). EnemyBase nişan alırken Show, bitince Hide çağırır;
    // ateş topu bu noktadan çıkar (EnemyBase.FireProjectile, HeliRocketStrike._launchPoint).
    // Kurulum: MedievalSkinSetup.SkinDragon (gövdenin child'ı, burnun hemen önü).
    public class MouthFire : MonoBehaviour
    {
        [SerializeField] private Transform _ball;
        [SerializeField] private float _trackingScale = 0.45f;
        [SerializeField] private float _lockedScale = 1f;
        [SerializeField] private float _growSpeed = 4f;

        private float _target;
        private float _scale;

        public Vector3 Position => transform.position;

        private void OnEnable()
        {
            _target = 0f;
            _scale = 0f;
            Apply();
        }

        public void Show(bool locked) => _target = locked ? _lockedScale : _trackingScale;

        public void Hide()
        {
            _target = 0f;
            _scale = 0f; // ateş edince ağızdan çıkmış olur
            Apply();
        }

        private void Update()
        {
            if (_target <= 0f && _scale <= 0f) return;
            _scale = Mathf.MoveTowards(_scale, _target, _growSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            if (_ball == null) return;
            bool on = _scale > 0.001f;
            if (_ball.gameObject.activeSelf != on) _ball.gameObject.SetActive(on);
            if (!on) return;
            float flicker = 1f + 0.12f * Mathf.Sin(Time.time * 31f) + 0.06f * Mathf.Sin(Time.time * 53f);
            _ball.localScale = Vector3.one * (_scale * flicker);
        }
    }
}
