using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "AvciFormuEffect", menuName = "BlobSurvivor/Effects/Avci Formu (Evolution)")]
    public class AvciFormuEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            if (blobRoot.GetComponent<AvciFormuComponent>() == null)
                blobRoot.AddComponent<AvciFormuComponent>();
        }
    }
}
