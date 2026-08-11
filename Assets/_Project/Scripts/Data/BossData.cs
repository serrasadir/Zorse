using UnityEngine;

namespace BlobSurvivor.Data
{
    [System.Serializable]
    public struct BossStage
    {
        public EnemyData EnemyData;
        public float SpawnTime; // saniye — GameManager.SurvivalTime ile aynı birim
        public int BonusCoinMin;
        public int BonusCoinMax;
    }

    [CreateAssetMenu(fileName = "BossData", menuName = "BlobSurvivor/Boss Data")]
    public class BossData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private BossStage[] _stages;

        public string DisplayName => _displayName;
        public BossStage[] Stages => _stages;
    }
}
