using UnityEngine;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "ScoreMultiplierEffect", menuName = "BlobSurvivor/Effects/Score Multiplier")]
    public class ScoreMultiplierEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var score = FindAnyObjectByType<ScoreSystem>();
            if (score == null) return;
            score.SetMultiplier(score.ScoreMultiplier + data.EffectValue);
        }
    }
}
