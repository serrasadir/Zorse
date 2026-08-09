using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "ShieldEffect", menuName = "BlobSurvivor/Effects/Shield")]
    public class ShieldEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;
            health.AddMaxShield(data.PerLevelValue);
        }
    }
}
