using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Polis/elit kafasındaki can barı (Tools → Blob.io → Polis Can Barlarını Kur). EnemyBase hasar alınca
    // SetRatio çağırır; bar pasifken LateUpdate çalışmaz, yani vurulmamış polisin maliyeti yok.
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Transform _fillPivot;
        [SerializeField] private bool _onlyWhenDamaged = true;

        private static Camera s_camera;

        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            bool show = ratio > 0f && (!_onlyWhenDamaged || ratio < 0.999f);
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
            if (!show || _fillPivot == null) return;

            Vector3 scale = _fillPivot.localScale;
            scale.x = Mathf.Max(0.001f, ratio);
            _fillPivot.localScale = scale;
        }

        private void LateUpdate()
        {
            if (s_camera == null)
            {
                s_camera = Camera.main;
                if (s_camera == null) return;
            }
            transform.rotation = s_camera.transform.rotation;
        }
    }
}
