using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Bölünme (nadir, tek seviye): ölümcül hasarda ölmek yerine kütlenin %30'unu kaybedip bir tier düşer,
    // %30 canla ve 2 sn dokunulmaz devam eder. Run'da bir kez. Ölünce devam et teklifi bundan sonra gelir.
    [CreateAssetMenu(fileName = "SplitEffect", menuName = "BlobSurvivor/Effects/Bolunme")]
    public class SplitEffect : UpgradeEffect
    {
        [SerializeField] private float _massFraction = 0.3f;
        [SerializeField] private float _healthFraction = 0.3f;
        [SerializeField] private float _invulnerability = 2f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            BlobSkills skills = BlobSkills.Of(blobRoot);
            skills.SplitMassFraction = _massFraction;
            skills.SplitHealthFraction = _healthFraction;
            skills.SplitInvulnerability = _invulnerability;
            skills.EnableSplit();
        }
    }
}
