using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "MagnetEffect", menuName = "BlobSurvivor/Effects/Magnet")]
    public class MagnetEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var magnet = blobRoot.GetComponent<MagnetComponent>();
            if (magnet == null)
                magnet = blobRoot.AddComponent<MagnetComponent>();

            magnet.IncreaseRadius(data.PerLevelValue);
        }
    }
}
