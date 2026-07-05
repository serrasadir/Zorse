using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "DamageReductionEffect", menuName = "BlobSurvivor/Effects/Damage Reduction")]
    public class DamageReductionEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;
            float newArmor = Mathf.Clamp01(health.GetArmorMultiplier() - data.PerLevelValue);
            health.SetArmorMultiplier(newArmor);
        }
    }
}
