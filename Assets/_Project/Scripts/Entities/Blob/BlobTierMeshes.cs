using BlobSurvivor.Core;
using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-04: karakter görünümünün tier'a göre değişen mesh'i (Topik: T1 mutlu → T5 öfkeli, turuncudan kırmızıya)
    // ve yeme anı mesh'i (ağız açık, _eatDuration kadar). Sadece sharedMesh referansı değişir: animasyon/ek hesap yok.
    // Görünüm prefab'ında durur (CharacterVisualSetup kurar). Index = tier - 1; eksikse son mesh kullanılır.
    // Aynı renderer kaldığı için DamageFlash/BlobVisual etkilenmez.
    public class BlobTierMeshes : MonoBehaviour
    {
        [SerializeField] private MeshFilter _target;
        [SerializeField] private Mesh[] _meshesByTier;
        [SerializeField] private Mesh[] _eatMeshesByTier;
        [SerializeField] private float _eatDuration = 0.15f;

        private BlobGrowth _growth;
        private BlobTier _tier = BlobTier.Tiny;
        private float _eatTimer;

        private void OnEnable()
        {
            _growth = GetComponentInParent<BlobGrowth>();
            if (_growth != null) _growth.OnPunched += HandleAte;
            GameEvents.OnBlobTierChanged += HandleTierChanged;
            _eatTimer = 0f;
            HandleTierChanged(GameEvents.CurrentBlobTier);
        }

        private void OnDisable()
        {
            if (_growth != null) _growth.OnPunched -= HandleAte;
            GameEvents.OnBlobTierChanged -= HandleTierChanged;
        }

        private void Update()
        {
            if (_eatTimer <= 0f) return;
            _eatTimer -= Time.deltaTime;
            if (_eatTimer <= 0f) Show(_meshesByTier);
        }

        private void HandleTierChanged(BlobTier tier)
        {
            _tier = tier;
            Show(_eatTimer > 0f ? _eatMeshesByTier : _meshesByTier);
        }

        // Peş peşe yerken (Kara Delik, vakum) süre uzar, ağız açık kalır.
        private void HandleAte()
        {
            if (_eatMeshesByTier == null || _eatMeshesByTier.Length == 0) return;
            if (_eatTimer <= 0f) Show(_eatMeshesByTier);
            _eatTimer = _eatDuration;
        }

        private void Show(Mesh[] meshes)
        {
            if (_target == null || meshes == null || meshes.Length == 0) return;
            int index = Mathf.Clamp((int)_tier - 1, 0, meshes.Length - 1);
            if (meshes[index] != null) _target.sharedMesh = meshes[index];
        }
    }
}
