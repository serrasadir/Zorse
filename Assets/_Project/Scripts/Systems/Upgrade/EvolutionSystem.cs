using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    // B17 (GDD_v2.md §5 "Evrim", devamı #16/B7): UpgradeSystem'in level-up/seçim akışını dinler —
    // iki input skill de max level'a ulaşınca output skill'i UpgradeSystem.RegisterDynamicUpgrade
    // ile kart havuzuna ekler. UpgradeSystem.cs'e dokunmadan yapılamadığı için oraya küçük,
    // tek satırlık bir genişletme noktası (_dynamicUpgrades) eklendi — asıl seçim/level mantığı
    // hâlâ UpgradeSystem'de.
    public class EvolutionSystem : MonoBehaviour
    {
        [SerializeField] private SkillEvolutionData[] _evolutions;

        private readonly HashSet<SkillEvolutionData> _unlocked = new HashSet<SkillEvolutionData>();

        private void OnEnable()
        {
            GameEvents.OnUpgradeSelected += CheckEvolutions;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeSelected -= CheckEvolutions;
            GameEvents.OnCharacterSelected -= HandleRestart;
        }

        private void HandleRestart(CharacterData _) => _unlocked.Clear();

        private void CheckEvolutions(UpgradeData _)
        {
            if (_evolutions == null || UpgradeSystem.Instance == null) return;

            foreach (var evo in _evolutions)
            {
                if (evo == null || evo.Output == null || evo.InputA == null || evo.InputB == null) continue;
                if (_unlocked.Contains(evo)) continue;

                bool ready = UpgradeSystem.Instance.GetLevel(evo.InputA) >= evo.InputA.MaxLevel
                          && UpgradeSystem.Instance.GetLevel(evo.InputB) >= evo.InputB.MaxLevel;

                if (ready)
                {
                    _unlocked.Add(evo);
                    UpgradeSystem.Instance.RegisterDynamicUpgrade(evo.Output);
                }
            }
        }
    }
}
