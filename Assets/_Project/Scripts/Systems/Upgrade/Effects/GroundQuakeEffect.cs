using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Yer Sarsıntısı (aktif, darbe aldıkça tetiklenir): her 15 hasarda 15 hasar / 22 hasar / ★ 0.5 sn sersemletme /
    // her 12 hasarda, alan +0.5 / ★ Deprem: her 3. sarsıntı 3× hasar, 2× alan.
    [CreateAssetMenu(fileName = "GroundQuakeEffect", menuName = "BlobSurvivor/Effects/Yer Sarsintisi")]
    public class GroundQuakeEffect : UpgradeEffect
    {
        [SerializeField] private float[] _takenByLevel = { 15f, 15f, 15f, 12f, 12f };
        [SerializeField] private float[] _damageByLevel = { 15f, 22f, 22f, 22f, 22f };
        [SerializeField] private float[] _radiusByLevel = { 2.5f, 2.5f, 2.5f, 3f, 3f };
        [SerializeField] private float _stunTime = 0.5f;   // Sv.3
        [SerializeField] private int _bigEvery = 3;        // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            GroundQuake quake = GetOrAdd<GroundQuake>(blobRoot);
            quake.DamagePerShake = At(_takenByLevel, level);
            quake.Damage = At(_damageByLevel, level);
            quake.Radius = At(_radiusByLevel, level);
            quake.StunTime = level >= 3 ? _stunTime : 0f;
            quake.BigEvery = level >= 5 ? _bigEvery : 0;
        }
    }
}
