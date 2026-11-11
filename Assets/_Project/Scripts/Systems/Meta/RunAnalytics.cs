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
        private readonly List<float> _levelTimes = new List<float>();
        private string _character;
        private int _policeKills, _eliteKills, _bossKills, _level = 1;
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
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeSelected -= HandleUpgradeSelected;
            GameEvents.OnGameOver -= HandlePlayerDied;
            GameEvents.OnRunComplete -= HandleRunComplete;
            GameEvents.OnCharacterSelected -= HandleRestart;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnLevelUp -= HandleLevelUp;
        }

        private void HandleRestart(CharacterData character)
        {
            _upgradeChoiceIds.Clear();
            _levelTimes.Clear();
            _character = character != null ? character.DisplayName : "";
            _policeKills = _eliteKills = _bossKills = 0;
            _level = 1;
            _runEnded = false;
        }

        private void HandleEnemyKilled(EnemyData data)
        {
            if (data == null) return;
            if (data.PreventConsumption) _bossKills++;
            else if (data.IsElite) _eliteKills++;
            else _policeKills++;
        }

        private void HandleLevelUp(int level)
        {
            _level = level;
            _levelTimes.Add(GameManager.Instance != null ? GameManager.Instance.SurvivalTime : 0f);
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
                upgradeChoiceIds = new List<string>(_upgradeChoiceIds),
                character = _character,
                coinsPolice = scoreSystem != null ? scoreSystem.CoinsFrom(CoinSource.Police) : 0,
                coinsElite = scoreSystem != null ? scoreSystem.CoinsFrom(CoinSource.Elite) : 0,
                coinsBoss = scoreSystem != null ? scoreSystem.CoinsFrom(CoinSource.Boss) : 0,
                coinsChest = scoreSystem != null ? scoreSystem.CoinsFrom(CoinSource.Chest) : 0,
                coinsExpired = scoreSystem != null ? scoreSystem.ExpiredCoins : 0,
                policeKills = _policeKills,
                eliteKills = _eliteKills,
                bossKills = _bossKills,
                finalLevel = _level,
                levelTimes = new List<float>(_levelTimes)
            };
            record.coinsBanked = Mathf.RoundToInt(record.coinsEarned * (reason == "PlayerDied" ? 0.5f : 1f)); // MetaProgression ile aynı oran

            SaveSystem.Instance?.SaveRun(record);
        }
    }
}
