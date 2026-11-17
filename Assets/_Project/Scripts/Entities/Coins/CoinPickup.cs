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
        private const float PopDuration = 0.38f, PopHeight = 1.3f;
        private Vector3 _popFrom, _popTo;
        private float _popTime = -1f;
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
            _popTime = -1f;
            _visible = false; // önceki ömrün yanıp sönmesinden kapalı kalmış olabilir — zorla aç
            SetVisible(true);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_popTime >= 0f) UpdatePop();
            else
            {
                Vector3 p = transform.position;
                if (PickupMagnet.Pull(ref p)) transform.position = p;
            }
            if (_age >= _lifetime)
            {
                _scoreSystem?.RegisterExpiredCoin(_amount);
                OnExpired?.Invoke(this);
                return;
            }

            if (_age > _lifetime - _blinkDuration)
                SetVisible(Mathf.FloorToInt(_age / BlinkInterval) % 2 == 0);
        }

        // Ölen düşmandan fırlama: yatayda kısa bir yay, sonra yerde durur.
        public void Pop(Vector3 offset)
        {
            _popFrom = transform.position;
            _popTo = _popFrom + new Vector3(offset.x, 0f, offset.z);
            _popTime = 0f;
        }

        private void UpdatePop()
        {
            _popTime += Time.deltaTime;
            float t = Mathf.Clamp01(_popTime / PopDuration);
            transform.position = Vector3.Lerp(_popFrom, _popTo, t) + Vector3.up * (4f * PopHeight * t * (1f - t));
            if (t >= 1f) _popTime = -1f;
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
