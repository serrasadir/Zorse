using UnityEngine;

namespace BlobSurvivor.Data
{
    [System.Serializable]
    public struct BossStage
    {
        public EnemyData EnemyData;
        public int RequiredLevel; // blob bu seviyeye gelince çıkar (0 = seviyeye bakılmaz)
        [UnityEngine.Serialization.FormerlySerializedAs("SpawnTime")]
        public float LatestTime;  // saniye: seviyeye ulaşılamasa da en geç bu sürede çıkar (0 = yok)
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
