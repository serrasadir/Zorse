using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Evrim (SKILLS.md §3): A max + B en az gereken seviyede olunca evrim kendiliğinden gerçekleşir. Oyun akarken
    // (kart ekranı/duraklatma yok) evrim anı oynar (EvolutionMoment), patlama anında UpgradeSystem.GrantEvolution.
    // Kendi sahnesi olan evrim (Kara Delik) anı atlar, hemen verilir. Birden fazla hazırsa sırayla.
    public class EvolutionSystem : MonoBehaviour
    {
        public static EvolutionSystem Instance { get; private set; }

        [SerializeField] private SkillEvolutionData[] _evolutions;

        public IReadOnlyList<SkillEvolutionData> Evolutions => _evolutions;

        private readonly HashSet<SkillEvolutionData> _done = new HashSet<SkillEvolutionData>();
        private readonly Queue<SkillEvolutionData> _pending = new Queue<SkillEvolutionData>();
        private EvolutionMoment _moment;
        private Transform _blob;

        private void Awake()
        {
            Instance = this;
            _moment = gameObject.AddComponent<EvolutionMoment>(); // sahne başına tek seferlik
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnEnable()
        {
            GameEvents.OnUpgradeApplied += CheckEvolutions;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeApplied -= CheckEvolutions;
            GameEvents.OnCharacterSelected -= HandleRestart;
        }

        private void HandleRestart(CharacterData _)
        {
            _done.Clear();
            _pending.Clear();
            _moment.Cancel();
        }

        private static bool Valid(SkillEvolutionData evo) =>
            evo != null && evo.Output != null && evo.InputA != null && evo.InputB != null;

        private void CheckEvolutions(UpgradeData _, int __)
        {
            UpgradeSystem upgrades = UpgradeSystem.Instance;
            if (_evolutions == null || upgrades == null) return;

            foreach (var evo in _evolutions)
            {
                if (!Valid(evo) || _done.Contains(evo)) continue;
                if (upgrades.GetLevel(evo.InputA) < evo.InputA.MaxLevel) continue;
                if (upgrades.GetLevel(evo.InputB) < evo.InputBRequiredLevel) continue;
                _done.Add(evo);
                _pending.Enqueue(evo);
            }
        }

        private void Update()
        {
            if (_pending.Count == 0 || _moment.Busy) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
            if (_blob == null)
            {
                GameObject blob = GameObject.FindWithTag("Blob"); // ilk evrimde bir kez
                if (blob == null) return;
                _blob = blob.transform;
            }

            SkillEvolutionData evo = _pending.Dequeue();
            if (evo.HasOwnMoment) { UpgradeSystem.Instance?.GrantEvolution(evo.Output); return; }
            _moment.Play(_blob, evo.Color, () => UpgradeSystem.Instance?.GrantEvolution(evo.Output), null);
        }

        // Kart ipucu: bu skill, elindeki bir skill'le bir evrimin eşiyse o evrimin adı (yoksa null).
        public string PartnerHint(UpgradeData card)
        {
            UpgradeSystem upgrades = UpgradeSystem.Instance;
            if (_evolutions == null || upgrades == null || card == null) return null;
            foreach (var evo in _evolutions)
            {
                if (!Valid(evo) || _done.Contains(evo)) continue;
                UpgradeData other = card == evo.InputA ? evo.InputB : card == evo.InputB ? evo.InputA : null;
                if (other != null && upgrades.GetLevel(other) > 0) return evo.Output.DisplayName;
            }
            return null;
        }
    }
}
