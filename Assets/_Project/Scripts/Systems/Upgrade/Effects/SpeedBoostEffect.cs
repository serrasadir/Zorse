using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "SpeedBoostEffect", menuName = "BlobSurvivor/Effects/Speed Boost")]
    public class SpeedBoostEffect : UpgradeEffect
    {
        // Sadece düz hız çarpanı (seviye başına _perLevelValue). Büyüme yavaşlamasına dokunmaz: o etki görünmüyordu.
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var controller = blobRoot.GetComponent<BlobController>();
            if (controller == null) return;
            controller.SetSpeedMultiplier(controller.GetSpeedMultiplier() + data.PerLevelValue);

            // Görsel: kaykay → alevli kaykay (Sv.4) → + popoda roket (Sv.8). UpgradeSystem seviyeyi Apply'dan önce artırır.
            SpeedVisual visual = blobRoot.GetComponent<SpeedVisual>();
            if (visual == null) visual = blobRoot.AddComponent<SpeedVisual>();
            visual.SetLevel(UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 1);
        }
    }
}
