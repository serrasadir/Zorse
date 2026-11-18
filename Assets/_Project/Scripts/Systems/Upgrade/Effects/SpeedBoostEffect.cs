using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Hız (pasif, kaykay): +%20 / +%30 / ★ alevli kaykay +%38 / +%45 / ★ roket: 2 sn durmadan gidince +%20.
    // Görsel: kaykay → alevli kaykay (Sv.3) → popoda roket (Sv.5). Büyüme yavaşlamasına dokunmaz (o Hızlı Metabolizma).
    [CreateAssetMenu(fileName = "SpeedBoostEffect", menuName = "BlobSurvivor/Effects/Speed Boost")]
    public class SpeedBoostEffect : UpgradeEffect
    {
        [SerializeField] private float[] _bonusByLevel = { 0.20f, 0.10f, 0.08f, 0.07f, 0f }; // seviye başına eklenen
        [SerializeField] private float _rocketBonus = 0.2f;   // Sv.5
        [SerializeField] private float _rocketDelay = 2f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            var controller = blobRoot.GetComponent<BlobController>();
            if (controller != null)
                controller.SetSpeedMultiplier(controller.GetSpeedMultiplier() + At(_bonusByLevel, level));

            if (level >= 5)
            {
                BlobSkills skills = BlobSkills.Of(blobRoot);
                skills.RocketBonus = _rocketBonus;
                skills.RocketDelay = _rocketDelay;
            }

            // SpeedVisual eski 8 seviyeli eşikleri kullanıyor (alevli 4, roket 8): 5 seviyeye eşlenir.
            SpeedVisual visual = GetOrAdd<SpeedVisual>(blobRoot);
            visual.SetLevel(level >= 5 ? 8 : level >= 3 ? 4 : 1);
        }
    }
}
