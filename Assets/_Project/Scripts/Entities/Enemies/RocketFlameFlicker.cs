using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Roket görünümlü merminin (HeliRocketBullet) alevini titretir: nozuldaki pivotun boyu Perlin ile oynar.
    // Mermi başına tek vektör yazımı; Helikopter Mermi Roketini Kur menüsü bağlar.
    public class RocketFlameFlicker : MonoBehaviour
    {
        [SerializeField] private Transform _flame;
        [SerializeField] private float _speed = 22f;

        private float _seed;

        private void OnEnable() => _seed = Random.value * 100f;

        private void Update()
        {
            if (_flame == null) return;
            float n = Mathf.PerlinNoise(Time.time * _speed, _seed);
            _flame.localScale = new Vector3(0.85f + 0.3f * n, 0.85f + 0.3f * n, 0.7f + 0.6f * n);
        }
    }
}
