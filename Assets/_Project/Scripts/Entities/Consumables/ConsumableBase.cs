using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Entities
{
    public class ConsumableBase : MonoBehaviour, IConsumable
    {
        [SerializeField] private ConsumableData _data;

        public ConsumableData Data => _data;
        public BlobTier RequiredTier => _data != null ? _data.RequiredTier : BlobTier.Tiny;

        public void SetData(ConsumableData data) => _data = data;

        public virtual void OnConsumed() { }

        private void OnEnable()
        {
            AssignLayer();
        }

        private void AssignLayer()
        {
            if (_data == null) return;
            int layerIndex = 8 + (int)_data.RequiredTier;
            gameObject.layer = layerIndex;
        }
    }
}
