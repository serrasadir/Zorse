using UnityEngine;

namespace BlobSurvivor.Data
{
    // B17 (GDD_v2.md §5 "Evrim"): iki input skill max level'a ulaşınca output skill'in
    // UpgradeSystem'in kart havuzuna dinamik olarak eklenmesini tanımlayan veri.
    [CreateAssetMenu(fileName = "SkillEvolutionData", menuName = "BlobSurvivor/Skill Evolution Data")]
    public class SkillEvolutionData : ScriptableObject
    {
        [SerializeField] private UpgradeData _inputA;
        [SerializeField] private UpgradeData _inputB;
        [SerializeField] private UpgradeData _output;

        public UpgradeData InputA => _inputA;
        public UpgradeData InputB => _inputB;
        public UpgradeData Output => _output;
    }
}
