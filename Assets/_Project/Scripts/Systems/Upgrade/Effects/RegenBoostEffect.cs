using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Can Emme (eski Rejenerasyon; sınıf/asset adı guid için aynı): vurulan hasarın PerLevelValue'su kadar can, seviyeyle toplanır.
    [CreateAssetMenu(fileName = "RegenBoostEffect", menuName = "BlobSurvivor/Effects/Regen Boost")]
    public class RegenBoostEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            if (blobRoot.GetComponent<BlobHealth>() == null) return;
            var lifesteal = blobRoot.GetComponent<LifestealComponent>();
            if (lifesteal == null) lifesteal = blobRoot.AddComponent<LifestealComponent>();
            lifesteal.AddFraction(data.PerLevelValue);
        }
    }
}
