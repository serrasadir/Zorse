using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "SpeedBoostEffect", menuName = "BlobSurvivor/Effects/Speed Boost")]
    public class SpeedBoostEffect : UpgradeEffect
    {
        // 2026-09-06 (Serra): "yavaşlama katsayımı da birazcık azaltabilir" — Hızlanma skili artık
        // hem düz hız çarpanını artırıyor hem de büyüme-kaynaklı yavaşlama üssünü (BlobController)
        // her seviyede biraz daha hafifletiyor.
        private const float TierSlowdownReductionPerLevel = 0.015f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var controller = blobRoot.GetComponent<BlobController>();
            if (controller == null) return;
            controller.SetSpeedMultiplier(controller.GetSpeedMultiplier() + data.PerLevelValue);
            controller.ReduceTierSlowdownExponent(TierSlowdownReductionPerLevel);

            // Görsel: kaykay → alevli kaykay (Sv.4) → + popoda roket (Sv.8). UpgradeSystem seviyeyi Apply'dan önce artırır.
            SpeedVisual visual = blobRoot.GetComponent<SpeedVisual>();
            if (visual == null) visual = blobRoot.AddComponent<SpeedVisual>();
            visual.SetLevel(UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 1);
        }
    }
}
