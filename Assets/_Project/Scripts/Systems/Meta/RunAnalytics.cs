using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems.Meta
{
    // B19 (GDD_v2.md §13): run içi event toplayan hafif katman — level-up seçimlerini ve
    // run'ın nasıl bittiğini (ölüm/final boss/timeout) toplar, run bitince SaveSystem'e devreder.
    public class RunAnalytics : MonoBehaviour
    {
        public static RunAnalytics Instance { get; private set; }

        private readonly List<string> _upgradeChoiceIds = new List<string>();
        private bool _runEnded;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnUpgradeSelected += HandleUpgradeSelected;
            GameEvents.OnGameOver += HandlePlayerDied;
            GameEvents.OnRunComplete += HandleRunComplete;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeSelected -= HandleUpgradeSelected;
            GameEvents.OnGameOver -= HandlePlayerDied;
            GameEvents.OnRunComplete -= HandleRunComplete;
            GameEvents.OnCharacterSelected -= HandleRestart;
        }

        private void HandleRestart(CharacterData _)
        {
            _upgradeChoiceIds.Clear();
            _runEnded = false;
        }

        private void HandleUpgradeSelected(UpgradeData data)
        {
            if (data != null && !string.IsNullOrEmpty(data.Id))
                _upgradeChoiceIds.Add(data.Id);
        }

        private void HandlePlayerDied() => FinishRun("PlayerDied");

        private void HandleRunComplete(RunEndReason reason) => FinishRun(reason.ToString());

        private void FinishRun(string reason)
        {
            if (_runEnded) return;
            _runEnded = true;

            ScoreSystem scoreSystem = FindAnyObjectByType<ScoreSystem>();

            var record = new RunRecord
            {
                endReason = reason,
                durationSeconds = GameManager.Instance != null ? GameManager.Instance.SurvivalTime : 0f,
                finalScore = scoreSystem != null ? scoreSystem.CurrentScore : 0,
                coinsEarned = scoreSystem != null ? scoreSystem.Coins : 0,
                upgradeChoiceIds = new List<string>(_upgradeChoiceIds)
            };

            SaveSystem.Instance?.SaveRun(record);
        }
    }
}
