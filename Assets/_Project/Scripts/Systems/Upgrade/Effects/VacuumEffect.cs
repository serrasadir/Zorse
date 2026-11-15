using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Vakum (aktif): menzil 1.6 / 2.0 / ★ Geniş Ağız (yeme alanı büyür) / 2.8 + çekme ×1.5 / ★ coin ve kalpleri de çeker.
    [CreateAssetMenu(fileName = "VacuumEffect", menuName = "BlobSurvivor/Effects/Vacuum")]
    public class VacuumEffect : UpgradeEffect
    {
        [SerializeField] private float[] _reachByLevel = { 1.6f, 2f, 2f, 2.8f, 2.8f };
        [SerializeField] private float _mouthScale = 1.3f;   // Sv.3
        [SerializeField] private float _fastPull = 1.5f;     // Sv.4

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            VacuumComponent vacuum = GetOrAdd<VacuumComponent>(blobRoot);
            vacuum.SetReach(At(_reachByLevel, level));
            if (level >= 3) vacuum.SetMouthScale(_mouthScale);
            if (level >= 4) vacuum.PullSpeedMultiplier = _fastPull;
            if (level >= 5) vacuum.PullPickups = true;
        }
    }
}
