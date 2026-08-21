using System;
using System.IO;
using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Systems.Meta
{
    public enum MetaStatType
    {
        MiknatoCharacter,
        Speed,
        MaxHealth,
        MassGain,
        CoinGain,
        XpMultiplier
    }

    [Serializable]
    public class MetaProgressionSaveData
    {
        public int credit;
        public bool miknatoUnlocked;
        public int speedLevel;
        public int maxHealthLevel;
        public int massGainLevel;
        public int coinGainLevel;
        public bool xpMultiplierPurchased;
    }

    // M20 (GDD_v2.md §8, Karar 7): run içi coin'in kalıcı Market kredisine dönüşmesi + satın alınan
    // kalıcı statların/karakterin diske kaydı. Market UI (M21) ve statların run'a uygulanması (M22)
    // bu sınıfın public API'sini (Credit, Get*Bonus, TryPurchase) tüketecek, ayrı issue'lar.
    public class MetaProgression : MonoBehaviour
    {
        public static MetaProgression Instance { get; private set; }

        [Header("Maliyetler (Market kalemi başına, GDD §8)")]
        [SerializeField] private int _miknatoCost = 500;
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
        [SerializeField] private float _xpMultiplierBonus = 0.1f;

        private const string FileName = "meta_progression.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private MetaProgressionSaveData _data = new MetaProgressionSaveData();

        public int Credit => _data.credit;
        public bool MiknatoUnlocked => _data.miknatoUnlocked;
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        private void HandleGameOver() => ConvertCoinsToCredit(0.5f);
        private void HandleRunComplete(RunEndReason reason) => ConvertCoinsToCredit(1f);

        private void ConvertCoinsToCredit(float fraction)
        {
            ScoreSystem scoreSystem = FindAnyObjectByType<ScoreSystem>();
            if (scoreSystem == null || scoreSystem.Coins <= 0) return;

            _data.credit += Mathf.RoundToInt(scoreSystem.Coins * fraction);
            Save();
        }

        public int GetCost(MetaStatType type)
        {
            switch (type)
            {
                case MetaStatType.MiknatoCharacter: return _miknatoCost;
                case MetaStatType.Speed: return _speedBaseCost + SpeedLevel * _speedCostPerLevel;
                case MetaStatType.MaxHealth: return _maxHealthBaseCost + MaxHealthLevel * _maxHealthCostPerLevel;
                case MetaStatType.MassGain: return _massGainBaseCost + MassGainLevel * _massGainCostPerLevel;
                case MetaStatType.CoinGain: return _coinGainBaseCost + CoinGainLevel * _coinGainCostPerLevel;
                case MetaStatType.XpMultiplier: return _xpMultiplierCost;
                default: return int.MaxValue;
            }
        }

        // Sadece tek seferlik kalemler için anlamlı (karakter unlock, XP çarpanı) — kademeli statlar
        // (Hız/MaxHP/Mass/Coin kazancı) sınırsız tekrar satın alınabilir, GDD'de üst sınır belirtilmemiş.
        public bool IsPurchased(MetaStatType type)
        {
            switch (type)
            {
                case MetaStatType.MiknatoCharacter: return MiknatoUnlocked;
                case MetaStatType.XpMultiplier: return XpMultiplierPurchased;
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
                case MetaStatType.MiknatoCharacter: _data.miknatoUnlocked = true; break;
                case MetaStatType.Speed: _data.speedLevel++; break;
                case MetaStatType.MaxHealth: _data.maxHealthLevel++; break;
                case MetaStatType.MassGain: _data.massGainLevel++; break;
                case MetaStatType.CoinGain: _data.coinGainLevel++; break;
                case MetaStatType.XpMultiplier: _data.xpMultiplierPurchased = true; break;
            }

            Save();
            return true;
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[MetaProgression] Kayıt yazılamadı: {e.Message}");
#endif
            }
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;

            try
            {
                string json = File.ReadAllText(FilePath);
                MetaProgressionSaveData loaded = JsonUtility.FromJson<MetaProgressionSaveData>(json);
                if (loaded != null) _data = loaded;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[MetaProgression] Kayıt okunamadı: {e.Message}");
#endif
            }
        }
    }
}
