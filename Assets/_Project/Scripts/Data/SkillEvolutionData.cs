using UnityEngine;

namespace BlobSurvivor.Data
{
    // Evrim: A (aktif) max seviyede + B (eş) en az _inputBMinLevel'de → Output kendiliğinden verilir, evrim anıyla (EvolutionSystem).
    [CreateAssetMenu(fileName = "SkillEvolutionData", menuName = "BlobSurvivor/Skill Evolution Data")]
    public class SkillEvolutionData : ScriptableObject
    {
        [Tooltip("Max seviyede olmalı (genelde aktif skill).")]
        [SerializeField] private UpgradeData _inputA;
        [Tooltip("Eş skill (genelde pasif); en az Input B Min Level.")]
        [SerializeField] private UpgradeData _inputB;
        [Tooltip("Eşin gereken seviyesi. 0 = max.")]
        [SerializeField] private int _inputBMinLevel = 1;
        [SerializeField] private UpgradeData _output;
        [Tooltip("Evrim anındaki halka ve kırıntı rengi.")]
        [SerializeField] private Color _color = new Color(1f, 0.82f, 0.25f, 1f);
        [Tooltip("Çıktının kendi evrim sahnesi var (Kara Delik): ortak an oynamaz.")]
        [SerializeField] private bool _ownMoment;

        public UpgradeData InputA => _inputA;
        public UpgradeData InputB => _inputB;
        public UpgradeData Output => _output;
        public Color Color => _color;
        public bool HasOwnMoment => _ownMoment;
        public int InputBRequiredLevel => _inputB == null ? 0 : _inputBMinLevel > 0 ? Mathf.Min(_inputBMinLevel, _inputB.MaxLevel) : _inputB.MaxLevel;

#if UNITY_EDITOR
        public void EditorSetup(UpgradeData a, UpgradeData b, int bMinLevel, UpgradeData output, Color color, bool ownMoment)
        {
            _inputA = a; _inputB = b; _inputBMinLevel = bMinLevel; _output = output; _color = color; _ownMoment = ownMoment;
        }
#endif
    }
}
