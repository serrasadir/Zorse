using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "HealthBoostEffect", menuName = "BlobSurvivor/Effects/Health Boost")]
    public class HealthBoostEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;
            health.IncreaseMaxHealth(data.EffectValue);
        }
    }
}
