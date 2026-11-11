using BlobSurvivor.Core;
using System;
using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Coins
{
    public class CoinPickup : MonoBehaviour
    {
        public event Action<CoinPickup> OnCollected;
        // 2026-11-03 (Serra): toplanmayan coin sonsuza kadar kalıyordu, havuz run boyunca büyüyordu.
        // Kalp gibi belli süre sonra kaybolur, son saniyelerde yanıp söner.
        public event Action<CoinPickup> OnExpired;

        [SerializeField] private float _lifetime = 20f;
        [SerializeField] private float _blinkDuration = 3f;

        private const float BlinkInterval = 0.15f;

        private int _amount = 1;
        private ScoreSystem _scoreSystem;
        private Renderer[] _renderers;
        private float _age;
        private bool _visible = true;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnEnable()
        {
            // ConsumableTier1 layer'ını paylaşır — Blob zaten bu layer'la her zaman çarpışır (Collision Matrix).
            gameObject.layer = TierLayers.For(BlobTier.Tiny);
            _age = 0f;
            _visible = false; // önceki ömrün yanıp sönmesinden kapalı kalmış olabilir — zorla aç
            SetVisible(true);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime)
            {
                _scoreSystem?.RegisterExpiredCoin(_amount);
                OnExpired?.Invoke(this);
                return;
            }

            if (_age > _lifetime - _blinkDuration)
                SetVisible(Mathf.FloorToInt(_age / BlinkInterval) % 2 == 0);
        }

        // Sadece değişince renderer'lara dokunur.
        private void SetVisible(bool visible)
        {
            if (_visible == visible || _renderers == null) return;
            _visible = visible;
            foreach (Renderer r in _renderers)
                if (r != null) r.enabled = visible;
        }

        public void Initialize(int amount, ScoreSystem scoreSystem, CoinSource source)
        {
            _amount = amount;
            _scoreSystem = scoreSystem;
            _source = source;
        }

        private CoinSource _source;

        private void OnTriggerEnter(Collider other)
        {
            // Blob'un iki collider'ı aynı adımda iki OnTriggerEnter üretebilir (coin blob'un içinde doğduğunda).
            if (!gameObject.activeInHierarchy || !other.CompareTag("Blob")) return;

            _scoreSystem?.AddCoin(_amount, _source);
            OnCollected?.Invoke(this);
        }
    }
}
