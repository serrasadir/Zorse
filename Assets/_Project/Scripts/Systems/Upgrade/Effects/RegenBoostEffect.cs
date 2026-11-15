using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Rejenerasyon (pasif): 0.5/sn / 1/sn / ★ 3 sn hasar almazsan ×2 / 1.5/sn / ★ 2/sn + tier atlayınca %20 can.
    // (Eski Can Emme bu asset'i kullanıyordu, sınıf/asset adı guid için aynı; Can Emme yeni sette yok.)
    [CreateAssetMenu(fileName = "RegenBoostEffect", menuName = "BlobSurvivor/Effects/Regen Boost")]
    public class RegenBoostEffect : UpgradeEffect
    {
        [SerializeField] private float[] _perSecondByLevel = { 0.5f, 1f, 1f, 1.5f, 2f };
        [SerializeField] private float _idleDelay = 3f;        // Sv.3
        [SerializeField] private float _tierUpHeal = 0.2f;     // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            BlobSkills skills = BlobSkills.Of(blobRoot);
            skills.RegenPerSecond = At(_perSecondByLevel, level);
            if (level >= 3) skills.RegenIdleDelay = _idleDelay;
            if (level >= 5) skills.TierUpHealFraction = _tierUpHeal;
        }
    }
}
