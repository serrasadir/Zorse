using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Yara Öfkesi (pasif): can eşiğin altındayken öfkeli. Can <%40: hız +%15 / + hasar +%15 / ★ eşik %50 /
    // hız +%25, hasar +%25 / ★ öfkeliyken ısırık hasarı yok.
    [CreateAssetMenu(fileName = "RageEffect", menuName = "BlobSurvivor/Effects/Yara Ofkesi")]
    public class RageEffect : UpgradeEffect
    {
        [SerializeField] private float[] _thresholdByLevel = { 0.4f, 0.4f, 0.5f, 0.5f, 0.5f };
        [SerializeField] private float[] _speedByLevel = { 0.15f, 0.15f, 0.15f, 0.25f, 0.25f };
        [SerializeField] private float[] _damageByLevel = { 0f, 0.15f, 0.15f, 0.25f, 0.25f };

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            BlobSkills skills = BlobSkills.Of(blobRoot);
            skills.RageThreshold = At(_thresholdByLevel, level);
            skills.RageSpeed = At(_speedByLevel, level);
            skills.RageDamage = At(_damageByLevel, level);
            skills.RageNoBite = level >= 5;
        }
    }
}
