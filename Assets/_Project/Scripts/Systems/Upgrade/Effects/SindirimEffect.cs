using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Sindirim (pasif): kütle +%10 / +%20 / ★ Ağır Lokma / +%30 / ★ Obur (her 10. lokma ×3).
    [CreateAssetMenu(fileName = "SindirimEffect", menuName = "BlobSurvivor/Effects/Sindirim")]
    public class SindirimEffect : UpgradeEffect
    {
        [SerializeField] private float _gainPerStep = 0.1f;     // Sv.1, 2, 4
        [SerializeField] private float _heavyBiteBonus = 0.25f; // Sv.3: kendi tier'ının en büyük lokmaları
        [SerializeField] private int _gluttonEvery = 10;        // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            if (level == 1 || level == 2 || level == 4)
                blobRoot.GetComponent<BlobGrowth>()?.IncreaseMassGainMultiplier(_gainPerStep);
            BlobSkills skills = BlobSkills.Of(blobRoot);
            if (level >= 3) skills.HeavyBiteBonus = _heavyBiteBonus;
            if (level >= 5) skills.GluttonEvery = _gluttonEvery;
        }
    }
}
