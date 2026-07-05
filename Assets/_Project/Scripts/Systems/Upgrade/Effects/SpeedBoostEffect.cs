using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "SpeedBoostEffect", menuName = "BlobSurvivor/Effects/Speed Boost")]
    public class SpeedBoostEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var controller = blobRoot.GetComponent<BlobController>();
            if (controller == null) return;
            controller.SetSpeedMultiplier(controller.GetSpeedMultiplier() + data.PerLevelValue);
        }
    }
}
