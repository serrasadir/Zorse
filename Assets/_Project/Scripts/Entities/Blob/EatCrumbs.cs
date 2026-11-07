using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // Yenen büyük objenin hacminden fışkırıp blob'a çekilen kırıntılar (küçük küpler, objenin palet renkleri).
    // Prefab: Resources/FX/EatCrumbs (Tools → Blob.io → Yeme Efektini Kur). EatEffects havuzda tutar.
    [RequireComponent(typeof(ParticleSystem))]
    public class EatCrumbs : MonoBehaviour
    {
        private ParticleSystem _ps;

        private void Awake() => _ps = GetComponent<ParticleSystem>();

        // size = objenin boyu (kenarların geometrik ortalaması); sayı, kırıntı boyu ve hız bununla ölçeklenir.
        public void Play(Bounds bounds, Color a, Color b, Vector3 blobCenter, float size)
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>(); // editör önizlemesinde Awake çalışmaz
            float k = Mathf.Clamp(size, 1f, 6f);
            transform.SetPositionAndRotation(bounds.center, Quaternion.identity);

            ParticleSystem.ShapeModule shape = _ps.shape;
            shape.scale = bounds.size * 0.85f;

            ParticleSystem.MainModule main = _ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            float sizeK = Mathf.Pow(k, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f * sizeK, 0.24f * sizeK);
            float speedK = Mathf.Sqrt(k);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * speedK, 3f * speedK);

            // Fışkırdıktan sonra blob'a doğru çekilir.
            Vector3 toBlob = blobCenter - bounds.center;
            Vector3 pull = toBlob.sqrMagnitude > 0.01f ? toBlob.normalized * (7f * speedK) : Vector3.zero;
            ParticleSystem.ForceOverLifetimeModule force = _ps.forceOverLifetime;
            force.x = pull.x;
            force.y = pull.y + 3f;
            force.z = pull.z;

            if (!_ps.isPlaying) _ps.Play();
            _ps.Emit(Mathf.Clamp(Mathf.RoundToInt(20f * Mathf.Pow(k, 1.2f)), 20, 90));
        }

        // Havai fişek patlaması / duman: noktadan her yöne saçılır, sonra düşer (gravity < 0) ya da yükselir.
        public void PlayBurst(Vector3 point, Color a, Color b, float size, int count, float speed, float gravity)
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            transform.SetPositionAndRotation(point, Quaternion.identity);

            ParticleSystem.ShapeModule shape = _ps.shape;
            shape.scale = Vector3.one * (0.15f * size);

            ParticleSystem.MainModule main = _ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * size, 0.15f * size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f * size, speed * size);

            ParticleSystem.ForceOverLifetimeModule force = _ps.forceOverLifetime;
            force.x = 0f;
            force.y = gravity;
            force.z = 0f;

            if (!_ps.isPlaying) _ps.Play();
            _ps.Emit(count);
        }

        public void Stop() => _ps.Clear();
    }
}
