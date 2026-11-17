using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems.Meta
{
    public enum MetaStatType
    {
        Speed,
        MaxHealth,
        MassGain,
        CoinGain,
        XpMultiplier
    }

    [Serializable]
    public class MetaProgressionSaveData
    {
        public int version;
        public int credit;
        public bool miknatoUnlocked; // eski kayıt (version < 2); yüklenince unlockedCharacters'a taşınır
        public List<string> unlockedCharacters = new List<string>(); // Market'te alınan karakterler (CharacterData.SaveId)
        public bool devAllCharacters; // DEV paneli: tüm karakterler açık
        public int speedLevel;
        public int maxHealthLevel;
        public int massGainLevel;
        public int coinGainLevel;
        public bool xpMultiplierPurchased;
        public int completedRuns; // ölümle ya da kazanarak biten run sayısı (Dalcık kilidi)
        public List<PowerUpLevel> powerUps = new List<PowerUpLevel>(); // PowerUps kataloğu (id → seviye)
        public List<string> unlockedSkills = new List<string>();       // oynayarak açılan skiller (UpgradeData.Id)
        public List<UnlockStatValue> unlockStats = new List<UnlockStatValue>(); // açılma koşullarının ilerlemesi
    }

    [Serializable]
    public class UnlockStatValue
    {
        public string stat; // UnlockStat adı
        public int value;
    }

    [Serializable]
    public class PowerUpLevel
    {
        public string id;
        public int level;
    }

    // M20 (GDD_v2.md §8, Karar 7): run içi coin'in kalıcı Market kredisine dönüşmesi + satın alınan
    // kalıcı statların/karakterin diske kaydı. Market UI (M21) ve statların run'a uygulanması (M22)
    // bu sınıfın public API'sini (Credit, Get*Bonus, TryPurchase) tüketecek, ayrı issue'lar.
    public class MetaProgression : MonoBehaviour
    {
        public static MetaProgression Instance { get; private set; }

        [Header("Maliyetler (Market kalemi başına, GDD §8)")]
        [SerializeField] private int _speedBaseCost = 200;
        [SerializeField] private int _speedCostPerLevel = 150;
        [SerializeField] private int _maxHealthBaseCost = 200;
        [SerializeField] private int _maxHealthCostPerLevel = 150;
        [SerializeField] private int _massGainBaseCost = 300;
        [SerializeField] private int _massGainCostPerLevel = 200;
        [SerializeField] private int _coinGainBaseCost = 300;
        [SerializeField] private int _coinGainCostPerLevel = 200;
        [SerializeField] private int _xpMultiplierCost = 1000;

        [Header("Kademe başına etki")]
        [SerializeField] private float _speedBonusPerLevel = 0.05f;
        [SerializeField] private float _maxHealthBonusPerLevel = 10f;
        [SerializeField] private float _massGainBonusPerLevel = 0.05f;
        [SerializeField] private float _coinGainBonusPerLevel = 0.05f;
        [SerializeField] private float _xpMultiplierBonus = 0.25f;

        // Kademeli statların (Hız/Can/Kütle/Coin) üst sınırı: Market'in bitecek bir hedefi olsun (OYUN.md Hedefler).
        public const int MaxStatLevel = 5;

        private const int SaveVersion = 2;
        private const string FileName = "meta_progression.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private MetaProgressionSaveData _data = new MetaProgressionSaveData { version = SaveVersion };

        // Kredi ya da satın alım değişince (Market/Lobi tazelenir).
        public event Action Changed;

        public int Credit => _data.credit;
        public int CompletedRuns => _data.completedRuns;
        public int SpeedLevel => _data.speedLevel;
        public int MaxHealthLevel => _data.maxHealthLevel;
        public int MassGainLevel => _data.massGainLevel;
        public int CoinGainLevel => _data.coinGainLevel;
        public bool XpMultiplierPurchased => _data.xpMultiplierPurchased;

        public float SpeedBonus => SpeedLevel * _speedBonusPerLevel;
        public float MaxHealthBonus => MaxHealthLevel * _maxHealthBonusPerLevel;
        public float MassGainBonus => MassGainLevel * _massGainBonusPerLevel;
        public float CoinGainBonus => CoinGainLevel * _coinGainBonusPerLevel;
        public float XpMultiplierBonus => XpMultiplierPurchased ? _xpMultiplierBonus : 0f;
        public float XpMultiplierBonusValue => _xpMultiplierBonus; // Market metni (alınmamışken de)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject); // child ise uyarı basar; tek sahne, gerekmiyor
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnRunComplete += HandleRunComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnRunComplete -= HandleRunComplete;
        }

        // Ölümde %50, tamamlanan run'da %100 (GDD §8).
        // Her biten run (ölüm de) sayılır — Dalcık 5 run sonra açılır (CharacterData.RequiredCompletedRuns).
        private void HandleGameOver() => FinishRun(0.5f);
        private void HandleRunComplete(RunEndReason reason) => FinishRun(1f);

        private void FinishRun(float creditFraction)
        {
            _data.completedRuns++;
            ScoreSystem scoreSystem = FindAnyObjectByType<ScoreSystem>();
            if (scoreSystem != null && scoreSystem.Coins > 0)
                _data.credit += Mathf.RoundToInt(scoreSystem.Coins * creditFraction);
            Save();
        }

        public int GetCost(MetaStatType type)
        {
            switch (type)
            {
                case MetaStatType.Speed: return _speedBaseCost + SpeedLevel * _speedCostPerLevel;
                case MetaStatType.MaxHealth: return _maxHealthBaseCost + MaxHealthLevel * _maxHealthCostPerLevel;
                case MetaStatType.MassGain: return _massGainBaseCost + MassGainLevel * _massGainCostPerLevel;
                case MetaStatType.CoinGain: return _coinGainBaseCost + CoinGainLevel * _coinGainCostPerLevel;
                case MetaStatType.XpMultiplier: return _xpMultiplierCost;
                default: return int.MaxValue;
            }
        }

        // Tek seferlikler alınınca, kademeli statlar MaxStatLevel'da biter.
        public bool IsPurchased(MetaStatType type)
        {
            switch (type)
            {
                case MetaStatType.XpMultiplier: return XpMultiplierPurchased;
                case MetaStatType.Speed: return SpeedLevel >= MaxStatLevel;
                case MetaStatType.MaxHealth: return MaxHealthLevel >= MaxStatLevel;
                case MetaStatType.MassGain: return MassGainLevel >= MaxStatLevel;
                case MetaStatType.CoinGain: return CoinGainLevel >= MaxStatLevel;
                default: return false;
            }
        }

        public bool TryPurchase(MetaStatType type)
        {
            if (IsPurchased(type)) return false;

            int cost = GetCost(type);
            if (_data.credit < cost) return false;

            _data.credit -= cost;

            switch (type)
            {
                case MetaStatType.Speed: _data.speedLevel++; break;
                case MetaStatType.MaxHealth: _data.maxHealthLevel++; break;
                case MetaStatType.MassGain: _data.massGainLevel++; break;
                case MetaStatType.CoinGain: _data.coinGainLevel++; break;
                case MetaStatType.XpMultiplier: _data.xpMultiplierPurchased = true; break;
            }

            Save();
            return true;
        }

        // ---------------------------------------------------------------- güçlendiriciler (PowerUps)

        public int GetLevel(PowerUpDef def)
        {
            if (def == null || _data.powerUps == null) return 0;
            foreach (PowerUpLevel p in _data.powerUps)
                if (p.id == def.Id) return Mathf.Min(p.level, def.MaxLevel);
            return 0;
        }

        // Toplam etki (seviye × kademe başı): run başında uygulanır.
        public float GetValue(PowerUpDef def) => GetLevel(def) * def.PerLevel;

        public int GetCost(PowerUpDef def) => def.CostAt(GetLevel(def));

        public bool IsMaxed(PowerUpDef def) => GetLevel(def) >= def.MaxLevel;

        public bool TryPurchase(PowerUpDef def)
        {
            if (def == null || IsMaxed(def)) return false;
            int cost = GetCost(def);
            if (_data.credit < cost) return false;
            _data.credit -= cost;
            _data.powerUps ??= new List<PowerUpLevel>();
            PowerUpLevel entry = _data.powerUps.Find(p => p.id == def.Id);
            if (entry == null) _data.powerUps.Add(new PowerUpLevel { id = def.Id, level = 1 });
            else entry.level++;
            Save();
            return true;
        }

        // ---------------------------------------------------------------- skill kilitleri

        public bool IsSkillUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id) || !SkillUnlocks.IsLocked(id)) return true;
            return _data.devAllCharacters || (_data.unlockedSkills != null && _data.unlockedSkills.Contains(id));
        }

        // Koşul gerçekleşip skill açıldığında (id). UnlockTracker bandı gösterir.
        public event Action<string> SkillUnlocked;

        public int GetStat(UnlockStat stat)
        {
            if (_data.unlockStats == null) return 0;
            string key = stat.ToString();
            foreach (UnlockStatValue s in _data.unlockStats)
                if (s.stat == key) return s.value;
            return 0;
        }

        // Run içi en iyi değer: büyükse yazılır. Diske her seferinde yazılmaz (run sonu ya da açılışta kaydedilir).
        public void ReportStat(UnlockStat stat, int value)
        {
            if (value <= GetStat(stat)) return;
            SetStat(stat, value);
            CheckUnlocks();
        }

        // Tüm run'lar toplamı.
        public void AddStat(UnlockStat stat, int delta)
        {
            if (delta <= 0) return;
            SetStat(stat, GetStat(stat) + delta);
            CheckUnlocks();
        }

        private void SetStat(UnlockStat stat, int value)
        {
            _data.unlockStats ??= new List<UnlockStatValue>();
            string key = stat.ToString();
            UnlockStatValue entry = _data.unlockStats.Find(s => s.stat == key);
            if (entry == null) _data.unlockStats.Add(new UnlockStatValue { stat = key, value = value });
            else entry.value = value;
        }

        private void CheckUnlocks()
        {
            foreach (SkillUnlocks.Entry e in SkillUnlocks.Locked)
            {
                if (IsSkillUnlocked(e.Id) || GetStat(e.Stat) < e.Target) continue;
                _data.unlockedSkills ??= new List<string>();
                _data.unlockedSkills.Add(e.Id);
                Save();
                SkillUnlocked?.Invoke(e.Id);
            }
        }

        // ---------------------------------------------------------------- karakter kilitleri

        // Market fiyatı (MarketPrice > 0) satın alınınca, run koşulu (RequiredCompletedRuns) o kadar run bitince açılır.
        public bool IsCharacterUnlocked(CharacterData c)
        {
            if (c == null) return false;
            if (_data.devAllCharacters) return true;
            if (c.MarketPrice > 0 && !_data.unlockedCharacters.Contains(c.SaveId)) return false;
            return CharacterRunsLeft(c) == 0;
        }

        public bool IsCharacterPurchased(CharacterData c) =>
            c != null && (c.MarketPrice <= 0 || _data.devAllCharacters || _data.unlockedCharacters.Contains(c.SaveId));

        public int CharacterRunsLeft(CharacterData c) =>
            c == null || _data.devAllCharacters ? 0 : Mathf.Max(0, c.RequiredCompletedRuns - _data.completedRuns);

        public bool TryPurchaseCharacter(CharacterData c)
        {
            if (c == null || c.MarketPrice <= 0 || IsCharacterPurchased(c)) return false;
            if (_data.credit < c.MarketPrice) return false;
            _data.credit -= c.MarketPrice;
            _data.unlockedCharacters.Add(c.SaveId);
            Save();
            return true;
        }

        // Test araçları (DevCheats, PerfOverlay dev paneli).
        public void DevAddCredit(int amount)
        {
            _data.credit += amount;
            Save();
        }

        public void DevUnlockAllCharacters()
        {
            _data.devAllCharacters = true;
            Save();
        }

        // Satın alınan karakterler ve biten run sayısı sıfırlanır (coin ve kalıcı güçler kalır).
        public void DevLockCharacters()
        {
            _data.devAllCharacters = false;
            _data.unlockedCharacters.Clear();
            _data.unlockedSkills?.Clear();
            _data.unlockStats?.Clear();
            _data.completedRuns = 0;
            Save();
        }

        public bool DevAllCharactersUnlocked => _data.devAllCharacters;

        public void DevReset()
        {
            _data = new MetaProgressionSaveData { version = SaveVersion };
            Save();
        }

        private void Save()
        {
            Changed?.Invoke();
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
            }
            catch (Exception e)
            {
                EditorWarning($"Kayıt yazılamadı: {e.Message}");
            }
        }

        // v1 → v2: Mıknato bayrağı listeye; 3 run ile açılmış Mermo (artık Market'te satılan Fişek) korunur.
        private void Migrate()
        {
            if (_data.version >= SaveVersion) return;
            if (_data.miknatoUnlocked && !_data.unlockedCharacters.Contains("Miknato")) _data.unlockedCharacters.Add("Miknato");
            if (_data.completedRuns >= 3 && !_data.unlockedCharacters.Contains("Fisek")) _data.unlockedCharacters.Add("Fisek");
            _data.miknatoUnlocked = false;
            _data.version = SaveVersion;
            Save();
        }

        // Build'de çağrı (argümanı dahil) derlenmez.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void EditorWarning(string message) => Debug.LogWarning($"[MetaProgression] {message}");

        private void Load()
        {
            if (!File.Exists(FilePath)) return;

            try
            {
                string json = File.ReadAllText(FilePath);
                MetaProgressionSaveData loaded = JsonUtility.FromJson<MetaProgressionSaveData>(json);
                if (loaded != null) _data = loaded;
                if (_data.unlockedCharacters == null) _data.unlockedCharacters = new List<string>();
                Migrate();
            }
            catch (Exception e)
            {
                EditorWarning($"Kayıt okunamadı: {e.Message}");
            }
        }
    }
}
