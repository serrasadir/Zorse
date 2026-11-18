using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Systems.Meta;
using BlobSurvivor.UI;

namespace BlobSurvivor.Systems.Social
{
    [Serializable]
    public class AchievementSaveData
    {
        public List<string> unlocked = new List<string>();
        public List<string> wonWith = new List<string>(); // kazanılan karakterler (CharacterData.SaveId)
        public int pedestrians, cars, coins, gasKills, slips;
        public long bestWinCentis; // en hızlı kazanma (salise), 0 = yok
    }

    // Rozet koşullarını oyun olaylarından sayar, Game Center'a (ISocialService) rozet ve skor gönderir.
    // Açılan rozetler ve toplam sayaçlar yerelde (achievements.json) tutulur; Game Center'a girişte hepsi
    // tekrar gönderilir (çevrimdışı açılanlar kaybolmaz). Sahne kurulumu gerekmez: HUD olan sahnede kendini kurar.
    public class AchievementTracker : MonoBehaviour
    {
        public static AchievementTracker Instance { get; private set; }

        private const string FileName = "achievements.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private ISocialService _service;
        private AchievementSaveData _data = new AchievementSaveData();
        private bool _dirty;

        private ScoreSystem _score;
        private CharacterData[] _characters;

        // Run içi
        private string _character;
        private int _policeEaten;
        private float _lastHealth = -1f, _lastDamageTime;
        private bool _runEnded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<AchievementTracker>() != null) return;
            if (FindAnyObjectByType<HUDController>(FindObjectsInactive.Include) == null) return;
            new GameObject("AchievementTracker").AddComponent<AchievementTracker>();
        }

        private void Awake()
        {
            Instance = this;
#if UNITY_IOS && !UNITY_EDITOR
            _service = new GameCenterService();
#else
            _service = new OfflineSocialService();
#endif
            Load();
        }

        private void Start()
        {
            _score = FindAnyObjectByType<ScoreSystem>();
            LobbyPanel lobby = FindAnyObjectByType<LobbyPanel>(FindObjectsInactive.Include);
            _characters = lobby != null ? lobby.Characters : null;
            if (MetaProgression.Instance != null) MetaProgression.Instance.Changed += CheckAllCharacters;
            CheckAllCharacters();
            _service.Authenticate(OnAuthenticated);
        }

        private void OnDestroy()
        {
            if (MetaProgression.Instance != null) MetaProgression.Instance.Changed -= CheckAllCharacters;
            Save();
            if (Instance == this) Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterSelected += OnRunStarted;
            GameEvents.OnHealthChanged += OnHealthChanged;
            GameEvents.OnSurvivalTimeUpdated += OnSurvivalTime;
            GameEvents.OnBlobTierChanged += OnTierChanged;
            GameEvents.OnEnemyDefeated += OnEnemyDefeated;
            GameEvents.OnFinalBossConsumed += OnFinalBossConsumed;
            GameEvents.OnGasKill += OnGasKill;
            GameEvents.OnEnemySlipped += OnEnemySlipped;
            GameEvents.OnUpgradeApplied += OnUpgradeApplied;
            GameEvents.OnTransformation += OnTransformation;
            GameEvents.OnVehicleEaten += OnVehicleEaten;
            GameEvents.OnPedestrianEaten += OnPedestrianEaten;
            GameEvents.OnGameOver += OnGameOver;
            GameEvents.OnRunComplete += OnRunComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnHealthChanged -= OnHealthChanged;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTime;
            GameEvents.OnBlobTierChanged -= OnTierChanged;
            GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
            GameEvents.OnFinalBossConsumed -= OnFinalBossConsumed;
            GameEvents.OnGasKill -= OnGasKill;
            GameEvents.OnEnemySlipped -= OnEnemySlipped;
            GameEvents.OnUpgradeApplied -= OnUpgradeApplied;
            GameEvents.OnTransformation -= OnTransformation;
            GameEvents.OnVehicleEaten -= OnVehicleEaten;
            GameEvents.OnPedestrianEaten -= OnPedestrianEaten;
            GameEvents.OnGameOver -= OnGameOver;
            GameEvents.OnRunComplete -= OnRunComplete;
        }

        // ---------------------------------------------------------------- lobi ve profil

        // Giriş durumu değişince (lobideki profil çipi ve profil paneli tazelenir).
        public event Action SignInChanged;

        public bool IsSignedIn => _service.IsAuthenticated;
        public string PlayerName => _service.PlayerName;
        public string PlayerId => _service.PlayerId;
        public Texture2D Avatar => _service.Avatar;
        public int UnlockedCount => _data.unlocked.Count;
        public int BestScore => _score != null ? _score.HighScore : 0;
        public float BestWinSeconds => _data.bestWinCentis / 100f; // 0 = henüz kazanılmadı

        public void ShowAchievements() => WithLogin(_service.ShowAchievements);
        public void ShowLeaderboards() => WithLogin(_service.ShowLeaderboards);
        public void ShowProfile() => WithLogin(_service.ShowProfile);
        public void ShowAddFriends() => WithLogin(_service.ShowAddFriends);
        public void SignIn() => WithLogin(() => { });

        // Game Center ekranı açılamadı (giriş yok ya da reddedildi; Editor'da hep): lobi profil panelini açar.
        public event Action Unavailable;

        // Açılışta girişi atlayan oyuncu düğmeye basınca tekrar sorulur. iOS reddedilen girişi aynı oturumda
        // bir daha göstermez; o zaman Unavailable (profil paneli Ayarlar → Game Center yolunu yazar).
        private void WithLogin(Action show)
        {
            if (_service.IsAuthenticated) { show(); return; }
            _service.Authenticate(ok =>
            {
                OnAuthenticated(ok);
                if (ok) show();
                else Unavailable?.Invoke();
            });
        }

        private void OnAuthenticated(bool ok)
        {
            SignInChanged?.Invoke();
            if (!ok) return;
            foreach (string id in _data.unlocked) _service.UnlockAchievement(id);
            ReportTotals();
            if (_score != null) _service.ReportScore(Leaderboards.HighScore, _score.HighScore);
            if (_data.bestWinCentis > 0) _service.ReportScore(Leaderboards.FastestWin, _data.bestWinCentis);
        }

        // ---------------------------------------------------------------- olaylar

        private static float SurvivalTime => GameManager.Instance != null ? GameManager.Instance.SurvivalTime : 0f;

        private void OnRunStarted(CharacterData character)
        {
            _character = character != null ? character.SaveId : null;
            _policeEaten = 0;
            _lastHealth = -1f;
            _lastDamageTime = 0f;
            _runEnded = false;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (_lastHealth >= 0f && current < _lastHealth - 0.01f) _lastDamageTime = SurvivalTime;
            _lastHealth = current;
        }

        private void OnSurvivalTime(float t)
        {
            if (t - _lastDamageTime >= Achievements.NoDamageSeconds) Unlock(Achievements.NoDamage);
        }

        private void OnTierChanged(BlobTier tier)
        {
            if (tier >= BlobTier.Colossal) Unlock(Achievements.ReachDev);
        }

        private void OnEnemyDefeated(EnemyData data, bool eaten)
        {
            if (data == null) return;
            if (data.PreventConsumption) // boss; helikopter OnFinalBossConsumed'da
            {
                if (!data.RequiresConsumptionToDie) Unlock(Achievements.Tank);
                return;
            }
            if (!eaten) return;
            if (data.IsSwarm) { Unlock(Achievements.RobotDog); return; }

            if (++_policeEaten >= Achievements.PoliceFeastTarget) Unlock(Achievements.PoliceFeast);
            if (data.FrontShieldHalfAngle > 0f) Unlock(Achievements.RiotPolice);
            if (data.DashRange > 0f) Unlock(Achievements.MotorPolice);
        }

        private void OnFinalBossConsumed() => Unlock(Achievements.Helicopter);

        private void OnGasKill() => Count(ref _data.gasKills, Achievements.GasKillsTarget, Achievements.GasKills);
        private void OnEnemySlipped() => Count(ref _data.slips, Achievements.SlipDogsTarget, Achievements.SlipDogs);
        private void OnVehicleEaten() => Count(ref _data.cars, Achievements.CarsTarget, Achievements.Cars);
        private void OnPedestrianEaten() => Count(ref _data.pedestrians, Achievements.PedestriansTarget, Achievements.Pedestrians);

        private void OnUpgradeApplied(UpgradeData data, int level)
        {
            if (data == null) return;
            if (data.Id == "kara_delik") Unlock(Achievements.BlackHole);
            else if (data.Id == "bolunme") Unlock(Achievements.Split);
            if (IsFullyLoaded()) Unlock(Achievements.FullyLoaded);
        }

        private static bool IsFullyLoaded()
        {
            UpgradeSystem upgrades = UpgradeSystem.Instance;
            if (upgrades == null) return false;
            if (upgrades.OwnedActives.Count < upgrades.ActiveSlots || upgrades.OwnedPassives.Count < upgrades.PassiveSlots) return false;
            foreach (UpgradeData u in upgrades.OwnedActives) if (upgrades.GetLevel(u) < u.MaxLevel) return false;
            foreach (UpgradeData u in upgrades.OwnedPassives) if (upgrades.GetLevel(u) < u.MaxLevel) return false;
            return true;
        }

        private void OnTransformation(string title, string sub)
        {
            if (title == FerrisWheelForm.BannerTitle) Unlock(Achievements.FerrisWheel);
        }

        private void OnGameOver() => EndRun(false);
        private void OnRunComplete(RunEndReason reason) => EndRun(true);

        private void EndRun(bool won)
        {
            if (_runEnded) return;
            _runEnded = true;
            float time = SurvivalTime;

            Unlock(Achievements.FirstRun);
            if (!won && time < Achievements.QuickDeathSeconds) Unlock(Achievements.QuickDeath);

            if (_score != null)
            {
                _service.ReportScore(Leaderboards.HighScore, _score.CurrentScore);
                Count(ref _data.coins, Achievements.CoinsTarget, Achievements.Coins, _score.Coins);
            }

            if (won)
            {
                Unlock(Achievements.FirstWin);
                long centis = (long)Math.Round(time * 100.0);
                if (centis > 0 && (_data.bestWinCentis == 0 || centis < _data.bestWinCentis)) _data.bestWinCentis = centis;
                _service.ReportScore(Leaderboards.FastestWin, centis);

                if (!string.IsNullOrEmpty(_character))
                {
                    if (!_data.wonWith.Contains(_character)) _data.wonWith.Add(_character);
                    Unlock(Achievements.WinWith(_character));
                }
                if (WonWithAll()) Unlock(Achievements.WinAll);
            }

            CheckAllCharacters();
            ReportTotals();
            _dirty = true;
            Save();
        }

        private bool WonWithAll()
        {
            if (_characters == null || _characters.Length == 0) return false;
            foreach (CharacterData c in _characters)
                if (c != null && !_data.wonWith.Contains(c.SaveId)) return false;
            return true;
        }

        private void CheckAllCharacters()
        {
            MetaProgression meta = MetaProgression.Instance;
            if (meta == null || _characters == null || _characters.Length == 0) return;
            foreach (CharacterData c in _characters)
                if (c != null && !meta.IsCharacterUnlocked(c)) return;
            Unlock(Achievements.AllCharacters);
        }

        // Toplam sayaçların ilerlemesi (Game Center'da çubuk olarak görünür).
        private void ReportTotals()
        {
            if (!_service.IsAuthenticated) return;
            Progress(Achievements.Pedestrians, _data.pedestrians, Achievements.PedestriansTarget);
            Progress(Achievements.Cars, _data.cars, Achievements.CarsTarget);
            Progress(Achievements.Coins, _data.coins, Achievements.CoinsTarget);
            Progress(Achievements.GasKills, _data.gasKills, Achievements.GasKillsTarget);
            Progress(Achievements.SlipDogs, _data.slips, Achievements.SlipDogsTarget);
            if (_characters != null && _characters.Length > 0)
                Progress(Achievements.WinAll, _data.wonWith.Count, _characters.Length);
        }

        private void Progress(string id, int value, int target)
        {
            if (!_data.unlocked.Contains(id) && value > 0) _service.ReportProgress(id, 100.0 * value / target);
        }

        // ---------------------------------------------------------------- kayıt

        private void Count(ref int counter, int target, string id, int delta = 1)
        {
            if (delta <= 0) return;
            counter += delta;
            _dirty = true;
            if (counter >= target) Unlock(id);
        }

        private void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || _data.unlocked.Contains(id)) return;
            _data.unlocked.Add(id);
            _dirty = true;
            _service.UnlockAchievement(id);
        }

        private void Save()
        {
            if (!_dirty) return;
            _dirty = false;
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data));
            }
            catch (Exception e)
            {
                EditorWarning($"Kayıt yazılamadı: {e.Message}");
            }
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;
            try
            {
                AchievementSaveData loaded = JsonUtility.FromJson<AchievementSaveData>(File.ReadAllText(FilePath));
                if (loaded != null) _data = loaded;
                _data.unlocked ??= new List<string>();
                _data.wonWith ??= new List<string>();
            }
            catch (Exception e)
            {
                EditorWarning($"Kayıt okunamadı: {e.Message}");
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void EditorWarning(string message) => Debug.LogWarning($"[AchievementTracker] {message}");
    }
}
