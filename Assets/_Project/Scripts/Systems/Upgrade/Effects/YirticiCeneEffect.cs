using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "YirticiCeneEffect", menuName = "BlobSurvivor/Effects/Yirtici Cene")]
    public class YirticiCeneEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var bonus = blobRoot.GetComponent<ConsumptionBonusComponent>();
            if (bonus == null)
                bonus = blobRoot.AddComponent<ConsumptionBonusComponent>();

            bonus.IncreaseTierBonus(Mathf.RoundToInt(data.PerLevelValue));
        }
    }
}
