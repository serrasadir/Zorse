using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Can Artışı (pasif): max can +15 / +30 / ★ kalpler +%50 / +45 / ★ +60, kalpler 2×.
    [CreateAssetMenu(fileName = "HealthBoostEffect", menuName = "BlobSurvivor/Effects/Health Boost")]
    public class HealthBoostEffect : UpgradeEffect
    {
        [SerializeField] private float[] _addByLevel = { 15f, 15f, 0f, 15f, 15f }; // seviye başına eklenen max can
        [SerializeField] private float _heartBonus = 1.5f;   // Sv.3
        [SerializeField] private float _heartBonusMax = 2f;  // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;
            int level = LevelOf(data);
            float add = At(_addByLevel, level);
            if (add > 0f) health.IncreaseMaxHealth(add);
            BlobSkills.Of(blobRoot);
            if (level >= 5) BlobSkills.HeartMultiplier = _heartBonusMax;
            else if (level >= 3) BlobSkills.HeartMultiplier = _heartBonus;
        }
    }
}
