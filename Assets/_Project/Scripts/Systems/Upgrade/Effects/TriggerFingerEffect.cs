using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Tetikçi (pasif): bekleme süreli aktifler (Sopa, Havai Fişek; Yer Sarsıntısı'nın gereken kütlesi) daha sık.
    // −%8 / −%16 / ★ her 5. atış hemen tekrarlanır / −%24 / ★ her öldürmede bekleme −0.2 sn.
    [CreateAssetMenu(fileName = "TriggerFingerEffect", menuName = "BlobSurvivor/Effects/Tetikci")]
    public class TriggerFingerEffect : UpgradeEffect
    {
        [SerializeField] private float[] _reductionByLevel = { 0.08f, 0.16f, 0.16f, 0.24f, 0.24f };
        [SerializeField] private int _repeatEvery = 5;          // Sv.3
        [SerializeField] private float _killRefund = 0.2f;      // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            BlobSkills.Of(blobRoot); // statikleri run sonunda sıfırlayan bileşen hazır olsun
            BlobSkills.CooldownScale = 1f - At(_reductionByLevel, level);
            BlobSkills.RepeatEvery = level >= 3 ? _repeatEvery : 0;
            BlobSkills.KillCooldownRefund = level >= 5 ? _killRefund : 0f;
        }
    }
}
