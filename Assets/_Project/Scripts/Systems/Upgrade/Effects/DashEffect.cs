using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "DashEffect", menuName = "BlobSurvivor/Effects/Dash")]
    public class DashEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            DashComponent dash = blobRoot.GetComponent<DashComponent>() ?? blobRoot.AddComponent<DashComponent>();
            dash.ApplyLevel(data.PerLevelValue);
        }
    }
}
