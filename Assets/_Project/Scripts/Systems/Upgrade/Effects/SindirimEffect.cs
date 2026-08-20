using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "SindirimEffect", menuName = "BlobSurvivor/Effects/Sindirim")]
    public class SindirimEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var growth = blobRoot.GetComponent<BlobGrowth>();
            if (growth == null) return;
            growth.IncreaseMassGainMultiplier(data.PerLevelValue);
        }
    }
}
