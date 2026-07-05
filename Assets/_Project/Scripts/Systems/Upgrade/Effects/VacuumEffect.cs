using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "VacuumEffect", menuName = "BlobSurvivor/Effects/Vacuum")]
    public class VacuumEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var vacuum = blobRoot.GetComponent<VacuumComponent>();
            if (vacuum == null)
                vacuum = blobRoot.AddComponent<VacuumComponent>();

            vacuum.IncreaseRadius(data.PerLevelValue);
        }
    }
}
