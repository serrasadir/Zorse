using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Kaygan Zemin (aktif): iz 2 sn, düşman −%30 / iz 3 sn / ★ robot köpekler izde 0.5 sn kayar / −%50 /
    // ★ asit iz: yutamadıklarına 5/sn hasar.
    [CreateAssetMenu(fileName = "SlipTrailEffect", menuName = "BlobSurvivor/Effects/Kaygan Zemin")]
    public class SlipTrailEffect : UpgradeEffect
    {
        [SerializeField] private float[] _durationByLevel = { 2f, 3f, 3f, 3f, 3f };
        [SerializeField] private float[] _slowByLevel = { 0.3f, 0.3f, 0.3f, 0.5f, 0.5f };
        [SerializeField] private float _swarmSlip = 0.5f;  // Sv.3
        [SerializeField] private float _acidDps = 5f;      // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            SlipTrail trail = GetOrAdd<SlipTrail>(blobRoot);
            trail.Duration = At(_durationByLevel, level);
            trail.Slow = At(_slowByLevel, level);
            trail.SwarmSlip = level >= 3 ? _swarmSlip : 0f;
            trail.AcidDps = level >= 5 ? _acidDps : 0f;
        }
    }
}
