using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.UI;

namespace BlobSurvivor.Systems.Meta
{
    // Skill açılma koşullarını (SkillUnlocks) oyun olaylarından sayıp MetaProgression'a bildirir.
    // Açılan skill için ekranda "YENİ SKİLL!" bandı. Sahne kurulumu gerekmez: HUD olan sahnede kendini kurar.
    public class UnlockTracker : MonoBehaviour
    {
        private const float BossLowHealthFraction = 0.2f;

        private BlobHealth _health;
        private int _carsEaten, _policeEaten;
        private int _lastReportedSecond = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<UnlockTracker>() != null) return;
            if (FindAnyObjectByType<HUDController>(FindObjectsInactive.Include) == null) return;
            new GameObject("UnlockTracker").AddComponent<UnlockTracker>();
        }

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null) _health = blob.GetComponent<BlobHealth>();
            if (MetaProgression.Instance != null) MetaProgression.Instance.SkillUnlocked += OnSkillUnlocked;
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterSelected += OnRunStarted;
            GameEvents.OnVehicleEaten += OnVehicleEaten;
            GameEvents.OnBlobTierChanged += OnTierChanged;
            GameEvents.OnSurvivalTimeUpdated += OnSurvivalTime;
            GameEvents.OnEnemyDefeated += OnEnemyDefeated;
            GameEvents.OnFinalBossConsumed += OnFinalBossConsumed;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnVehicleEaten -= OnVehicleEaten;
            GameEvents.OnBlobTierChanged -= OnTierChanged;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTime;
            GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
            GameEvents.OnFinalBossConsumed -= OnFinalBossConsumed;
        }

        private void OnDestroy()
        {
            if (MetaProgression.Instance != null) MetaProgression.Instance.SkillUnlocked -= OnSkillUnlocked;
        }

        private static MetaProgression Meta => MetaProgression.Instance;

        private void OnRunStarted(CharacterData _)
        {
            _carsEaten = 0;
            _policeEaten = 0;
            _lastReportedSecond = -1;
        }

        private void OnVehicleEaten()
        {
            _carsEaten++;
            Meta?.ReportStat(UnlockStat.CarsEatenInRun, _carsEaten);
        }

        private void OnTierChanged(BlobTier tier)
        {
            if (tier >= BlobTier.Colossal) Meta?.ReportStat(UnlockStat.ReachedDev, 1);
        }

        private void OnSurvivalTime(float t)
        {
            int second = Mathf.FloorToInt(t);
            if (second == _lastReportedSecond) return; // olay 0,1 sn'de bir gelir
            _lastReportedSecond = second;
            Meta?.ReportStat(UnlockStat.SurviveSeconds, second);
        }

        private void OnEnemyDefeated(EnemyData data, bool eaten)
        {
            if (data == null || Meta == null) return;
            if (data.PreventConsumption) // boss
            {
                if (!data.RequiresConsumptionToDie) Meta.AddStat(UnlockStat.TankKills, 1);
                if (_health != null && _health.CurrentHealth < _health.MaxHealth * BossLowHealthFraction)
                    Meta.ReportStat(UnlockStat.BossLowHealth, 1);
                return;
            }
            if (data.IsSwarm) return; // robot köpek polis sayılmaz
            if (eaten)
            {
                _policeEaten++;
                Meta.ReportStat(UnlockStat.PoliceEatenInRun, _policeEaten);
            }
            else Meta.AddStat(UnlockStat.PoliceKillsTotal, 1);
        }

        private void OnFinalBossConsumed() => Meta?.ReportStat(UnlockStat.FinalBossEaten, 1);

        private void OnSkillUnlocked(string id)
        {
            string name = id;
            UpgradeData[] all = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.AllUpgrades : null;
            if (all != null)
                foreach (UpgradeData u in all)
                    if (u != null && u.Id == id) { name = u.DisplayName; break; }
            GameEvents.RaiseSkillUnlocked(name);
        }
    }
}
