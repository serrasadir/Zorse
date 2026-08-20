using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "KaraDelikEffect", menuName = "BlobSurvivor/Effects/Kara Delik (Evolution)")]
    public class KaraDelikEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            if (blobRoot.GetComponent<BlackHoleComponent>() == null)
                blobRoot.AddComponent<BlackHoleComponent>();
        }
    }
}
