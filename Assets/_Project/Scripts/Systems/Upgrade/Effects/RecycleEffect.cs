using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Geri Dönüşüm (pasif): cansız lokma (eşya, araba; yaya/polis değil) can verir.
    // +1 / +2 / ★ Hurdacı (araba +25) / +3 / ★ can doluyken fazlası 20'lik geçici kalkan.
    [CreateAssetMenu(fileName = "RecycleEffect", menuName = "BlobSurvivor/Effects/Geri Donusum")]
    public class RecycleEffect : UpgradeEffect
    {
        [SerializeField] private float[] _healByLevel = { 1f, 2f, 2f, 3f, 3f };
        [SerializeField] private float _carHeal = 25f;          // Sv.3
        [SerializeField] private float _overflowShieldCap = 20f; // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            BlobSkills skills = BlobSkills.Of(blobRoot);
            skills.RecycleHeal = At(_healByLevel, level);
            if (level >= 3) skills.CarHeal = _carHeal;
            if (level >= 5) skills.OverflowShieldCap = _overflowShieldCap;
        }
    }
}
