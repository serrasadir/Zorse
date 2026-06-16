using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "RegenBoostEffect", menuName = "BlobSurvivor/Effects/Regen Boost")]
    public class RegenBoostEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;
            float newRate = health.GetRegenRate() + data.EffectValue;
            health.EnableRegen(newRate);
        }
    }
}
