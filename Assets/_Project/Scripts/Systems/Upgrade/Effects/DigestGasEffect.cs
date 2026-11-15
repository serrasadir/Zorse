using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Sindirim Gazı (aktif aura, sadece yutamadıklarına): 4/sn, gövde +1 / 6/sn / ★ gazdakiler %20 yavaşlar /
    // 8/sn, alan +0.5 / ★ gazda ölenin kütlesinin yarısı sana.
    [CreateAssetMenu(fileName = "DigestGasEffect", menuName = "BlobSurvivor/Effects/Sindirim Gazi")]
    public class DigestGasEffect : UpgradeEffect
    {
        [SerializeField] private float[] _dpsByLevel = { 4f, 6f, 6f, 8f, 8f };
        [SerializeField] private float[] _radiusByLevel = { 1f, 1f, 1f, 1.5f, 1.5f };
        [SerializeField] private float _slow = 0.2f;          // Sv.3
        [SerializeField] private float _feedFraction = 0.5f;  // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            DigestGas gas = GetOrAdd<DigestGas>(blobRoot);
            gas.Dps = At(_dpsByLevel, level);
            gas.Radius = At(_radiusByLevel, level);
            gas.Slow = level >= 3 ? _slow : 0f;
            gas.FeedFraction = level >= 5 ? _feedFraction : 0f;
        }
    }
}
