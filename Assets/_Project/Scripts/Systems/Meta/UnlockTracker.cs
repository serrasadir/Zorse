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
        private const float LowHealthFraction = 0.25f;

        private BlobHealth _health;
        private int _lowHealthSeconds; // bu run'da can %25 altında geçen sn
        private int _carsEaten, _policeEaten, _eaten;
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
            GameEvents.OnCoinsChanged += OnCoinsChanged;
            GameEvents.OnBlobAte += OnBlobAte;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnVehicleEaten -= OnVehicleEaten;
            GameEvents.OnBlobTierChanged -= OnTierChanged;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTime;
            GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
            GameEvents.OnFinalBossConsumed -= OnFinalBossConsumed;
            GameEvents.OnCoinsChanged -= OnCoinsChanged;
            GameEvents.OnBlobAte -= OnBlobAte;
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
            _eaten = 0;
            _lowHealthSeconds = 0;
            _lastReportedSecond = -1;
        }

        private void OnVehicleEaten()
        {
            _carsEaten++;
            Meta?.ReportStat(UnlockStat.CarsEatenInRun, _carsEaten);
        }

        private void OnTierChanged(BlobTier tier)
        {
            if (tier >= BlobTier.Large) Meta?.ReportStat(UnlockStat.ReachedLarge, 1);
        }

        private void OnSurvivalTime(float t)
        {
            int second = Mathf.FloorToInt(t);
            if (second == _lastReportedSecond) return; // olay 0,1 sn'de bir gelir
            _lastReportedSecond = second;
            Meta?.ReportStat(UnlockStat.SurviveSeconds, second);
            if (_health != null && _health.IsAlive && _health.CurrentHealth < _health.MaxHealth * LowHealthFraction)
                Meta?.ReportStat(UnlockStat.LowHealthSeconds, ++_lowHealthSeconds);
        }

        private void OnEnemyDefeated(EnemyData data, bool eaten)
        {
            if (data == null || Meta == null) return;
            if (data.PreventConsumption) // boss
            {
                if (!data.RequiresConsumptionToDie) Meta.AddStat(UnlockStat.TankKills, 1);
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

        // Run'ın coin toplamı (ScoreSystem run başında sıfırlar).
        private void OnCoinsChanged(int total) => Meta?.ReportStat(UnlockStat.CoinsInRun, total);

        private void OnBlobAte(BlobTier _) => Meta?.ReportStat(UnlockStat.EatenInRun, ++_eaten);

        private void OnFinalBossConsumed() => Meta?.ReportStat(UnlockStat.FinalBossEaten, 1);

        private void OnSkillUnlocked(string id)
        {
            UpgradeData skill = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.FindById(id) : null;
            GameEvents.RaiseSkillUnlocked(skill != null ? skill.DisplayName : id);
        }
    }
}
