using System;
using UnityEngine;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Coins
{
    public class CoinPickup : MonoBehaviour
    {
        public event Action<CoinPickup> OnCollected;

        private int _amount = 1;
        private ScoreSystem _scoreSystem;

        private void OnEnable()
        {
            // ConsumableTier1 layer'ını paylaşır — Blob zaten bu layer'la her zaman çarpışır (Collision Matrix).
            gameObject.layer = LayerMask.NameToLayer("ConsumableTier1");
        }

        public void Initialize(int amount, ScoreSystem scoreSystem)
        {
            _amount = amount;
            _scoreSystem = scoreSystem;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Blob")) return;

            _scoreSystem?.AddCoin(_amount);
            OnCollected?.Invoke(this);
        }
    }
}
