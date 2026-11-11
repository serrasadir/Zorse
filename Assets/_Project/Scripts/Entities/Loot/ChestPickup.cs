using System;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Loot
{
    public class ChestPickup : MonoBehaviour
    {
        public event Action<ChestPickup> OnCollected;

        [SerializeField] private int _coinAmount = 35;
        [SerializeField] private float _rotationSpeed = 90f;

        private ScoreSystem _scoreSystem;

        private void OnEnable()
        {
            gameObject.layer = TierLayers.For(BlobTier.Tiny);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
        }

        public void Initialize(ScoreSystem scoreSystem)
        {
            _scoreSystem = scoreSystem;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Blob'un iki collider'ı aynı adımda iki OnTriggerEnter üretebilir — çift level-up olmasın.
            if (!gameObject.activeInHierarchy || !other.CompareTag("Blob")) return;

            if (_scoreSystem == null)
                _scoreSystem = FindAnyObjectByType<ScoreSystem>();

            _scoreSystem?.AddCoin(_coinAmount, CoinSource.Chest);

            int currentLevel = 0;
            BlobGrowth growth = other.GetComponentInParent<BlobGrowth>();
            if (growth != null)
                currentLevel = growth.CurrentLevel;

            GameEvents.RaiseLevelUp(currentLevel);
            OnCollected?.Invoke(this);
        }
    }
}
