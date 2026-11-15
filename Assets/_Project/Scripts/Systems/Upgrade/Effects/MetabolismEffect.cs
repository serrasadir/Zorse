using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Hızlı Metabolizma (pasif): büyüme yavaşlaması −%15 / −%30 / ★ tier atlayınca 3 sn +%30 hız / −%45 /
    // ★ −%60 + büyük lokma sonrası 1 sn hız. Yavaşlama hiçbir zaman tamamen kalkmaz (ilke 3).
    [CreateAssetMenu(fileName = "MetabolismEffect", menuName = "BlobSurvivor/Effects/Hizli Metabolizma")]
    public class MetabolismEffect : UpgradeEffect
    {
        [SerializeField] private float[] _reductionByLevel = { 0.15f, 0.3f, 0.3f, 0.45f, 0.6f };
        [SerializeField] private float _tierUpBoost = 0.3f;   // Sv.3
        [SerializeField] private float _bigBiteBoost = 0.2f;  // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            BlobController controller = blobRoot.GetComponent<BlobController>();
            if (controller != null) controller.SlowdownReduction = At(_reductionByLevel, level);
            BlobSkills skills = BlobSkills.Of(blobRoot);
            if (level >= 3) skills.TierUpBoost = _tierUpBoost;
            if (level >= 5) skills.BigBiteBoost = _bigBiteBoost;
        }
    }
}
