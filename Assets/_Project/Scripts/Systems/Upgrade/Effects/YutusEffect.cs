using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "YutusEffect", menuName = "BlobSurvivor/Effects/Yutus")]
    public class YutusEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var healOnConsume = blobRoot.GetComponent<HealOnConsumeComponent>();
            if (healOnConsume == null)
                healOnConsume = blobRoot.AddComponent<HealOnConsumeComponent>();

            healOnConsume.IncreaseHealAmount(data.PerLevelValue);
        }
    }
}
