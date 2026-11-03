using System.Collections.Generic;
using UnityEngine;

namespace BlobSurvivor.Core
{
    // Kamerayla blob arasına giren binaları yarı saydam yapar. Binanın tamamen kaybolması yön
    // duygusunu bozardı; yarı saydam kalınca şehir okunur, blob da görünür.
    // Sorgu 0.1s'de bir tek SphereCast (Environment = binaların EnvironmentObstacle collider'ı).
    // Saydam materyaller Resources/OcclusionGhost altında "<orijinal ad>_Ghost" adıyla durur:
    // build'e dahil olsunlar diye runtime'da üretilmiyorlar (shader varyantı ayıklanırdı).
    [RequireComponent(typeof(Camera))]
    public class CameraOcclusionFader : MonoBehaviour
    {
        [SerializeField] private float _checkInterval = 0.1f;
        [SerializeField] private float _probeRadiusFactor = 0.8f;

        private const string GhostFolder = "OcclusionGhost/";

        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private readonly Dictionary<Collider, Renderer[]> _renderersByCollider = new Dictionary<Collider, Renderer[]>();
        private readonly Dictionary<Material, Material> _ghostByMaterial = new Dictionary<Material, Material>();
        private readonly Dictionary<Renderer, Material> _fadedOriginals = new Dictionary<Renderer, Material>();
        private readonly HashSet<Renderer> _occludingNow = new HashSet<Renderer>();
        private readonly List<Renderer> _toRestore = new List<Renderer>();

        private Transform _blob;
        private int _environmentMask;
        private float _timer;

        private void Start()
        {
            _environmentMask = LayerMask.GetMask("Environment");
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _blob = blob.transform;
        }

        private void LateUpdate()
        {
            if (_blob == null) return;

            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = _checkInterval;

            CollectOccluders();
            ApplyFades();
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<Renderer, Material> pair in _fadedOriginals)
                if (pair.Key != null) pair.Key.sharedMaterial = pair.Value;
            _fadedOriginals.Clear();
        }

        private void CollectOccluders()
        {
            _occludingNow.Clear();

            Vector3 origin = transform.position;
            Vector3 toBlob = _blob.position - origin;
            float blobRadius = _blob.localScale.x * 0.5f;
            float distance = toBlob.magnitude - blobRadius;
            if (distance <= 0f) return;

            int count = Physics.SphereCastNonAlloc(origin, blobRadius * _probeRadiusFactor, toBlob / toBlob.magnitude,
                _hits, distance, _environmentMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
                foreach (Renderer renderer in GetRenderers(_hits[i].collider))
                    _occludingNow.Add(renderer);
        }

        private void ApplyFades()
        {
            foreach (Renderer renderer in _occludingNow)
            {
                if (_fadedOriginals.ContainsKey(renderer)) continue;

                Material ghost = GetGhost(renderer.sharedMaterial);
                if (ghost == null) continue;

                _fadedOriginals.Add(renderer, renderer.sharedMaterial);
                renderer.sharedMaterial = ghost;
            }

            _toRestore.Clear();
            foreach (Renderer renderer in _fadedOriginals.Keys)
                if (!_occludingNow.Contains(renderer)) _toRestore.Add(renderer);

            foreach (Renderer renderer in _toRestore)
            {
                if (renderer != null) renderer.sharedMaterial = _fadedOriginals[renderer];
                _fadedOriginals.Remove(renderer);
            }
        }

        private Renderer[] GetRenderers(Collider collider)
        {
            if (_renderersByCollider.TryGetValue(collider, out Renderer[] renderers)) return renderers;

            // 2026-11-03 (Serra: "şehrin en güneyinde tüm binalar saydam oluyor"): sadece binaların engel kutusu.
            // Harita sınır duvarı (CityBounds, 30 birim yüksek) da Environment'ta ve şehrin kök objesinin child'ı —
            // güneyde kamera duvarın dışına çıkınca ışın duvara çarpıyor, "kardeşleri" de tüm şehir oluyordu.
            if (collider.gameObject.name != "EnvironmentObstacle")
            {
                renderers = System.Array.Empty<Renderer>();
                _renderersByCollider.Add(collider, renderers);
                return renderers;
            }

            // EnvironmentObstacle binanın child'ı: görsel, kardeş "Model" objesinde.
            Transform root = collider.transform.parent != null ? collider.transform.parent : collider.transform;
            renderers = root.GetComponentsInChildren<Renderer>(true);
            _renderersByCollider.Add(collider, renderers);
            return renderers;
        }

        private Material GetGhost(Material original)
        {
            if (original == null) return null;
            if (_ghostByMaterial.TryGetValue(original, out Material ghost)) return ghost;

            ghost = Resources.Load<Material>(GhostFolder + original.name + "_Ghost");
            _ghostByMaterial.Add(original, ghost);
            return ghost;
        }
    }
}
