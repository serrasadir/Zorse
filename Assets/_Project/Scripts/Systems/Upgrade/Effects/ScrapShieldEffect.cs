using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Evrim "Hurda Kalkanı" (Geri Dönüşüm 5 + Zırh): etrafında dönen hurda parçaları (ScrapOrbit).
    [CreateAssetMenu(fileName = "ScrapShieldEffect", menuName = "BlobSurvivor/Effects/Hurda Kalkanı (Evolution)")]
    public class ScrapShieldEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            if (blobRoot.GetComponent<ScrapOrbit>() != null) return;
            blobRoot.AddComponent<ScrapOrbit>(); // kart seçiminde, bir kez
            Haptics.Impact(2);
        }
    }
}
