using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    public class UpgradeSystem : MonoBehaviour
    {
        public static UpgradeSystem Instance { get; private set; }

        [SerializeField] private UpgradeData[] _allUpgrades;
        [SerializeField] private int _choiceCount = 3;

        private GameObject _blobRoot;

        // A12/B12: seviye artık paylaşılan UpgradeData asset'inde değil, burada runtime'da tutulur —
        // asset salt-okunur veri kalır, editor'da yanlışlıkla dirty/save riski ve run'lar arası
        // sızıntı riski ortadan kalkar.
        private readonly Dictionary<UpgradeData, int> _levels = new Dictionary<UpgradeData, int>();

        // B17: EvolutionSystem, input skiller max level'a ulaşınca output skill'i buraya kaydeder —
        // _allUpgrades sahnede sabit dizildiği için evrim çıktıları ayrı bir runtime listede tutulur.
        private readonly List<UpgradeData> _dynamicUpgrades = new List<UpgradeData>();

        // 2026-11-03: tek lokma eşiği birden fazla kez geçince (AddXP döngüsü) ikinci level-up ilk kart
        // ekranının üstüne yazıyor, seçilmeyen seviye kayboluyordu. Kart ekranı açıkken gelenler sıraya
        // alınır, seçim sonrası arka arkaya gösterilir.
        private bool _awaitingChoice;
        private int _pendingLevelUps;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            _blobRoot = GameObject.FindWithTag("Blob");
            _levels.Clear();
            _dynamicUpgrades.Clear();
        }

        public void RegisterDynamicUpgrade(UpgradeData data)
        {
            if (data != null && !_dynamicUpgrades.Contains(data))
                _dynamicUpgrades.Add(data);
        }

        private void OnEnable()
        {
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnUpgradeSelected += OnUpgradeSelected;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnUpgradeSelected -= OnUpgradeSelected;
            GameEvents.OnCharacterSelected -= HandleRestart;
        }

        // Skill seviyeleri ve açılan evrimler run'a özeldir.
        private void HandleRestart(CharacterData _)
        {
            _levels.Clear();
            _dynamicUpgrades.Clear();
            _awaitingChoice = false;
            _pendingLevelUps = 0;
        }

        public int GetLevel(UpgradeData data)
        {
            return data != null && _levels.TryGetValue(data, out int level) ? level : 0;
        }

        private void OnLevelUp(int level)
        {
            if (_awaitingChoice)
            {
                _pendingLevelUps++;
                return;
            }

            if (TryShowChoices())
                GameManager.Instance?.BeginLevelUp();
        }

        private bool TryShowChoices()
        {
            if (_allUpgrades == null || _allUpgrades.Length == 0) return false;

            UpgradeData[] choices = SelectUpgrades();
            if (choices == null || choices.Length == 0) return false; // her şey max — level-up ödülsüz geçer

            _awaitingChoice = true;
            GameEvents.RaiseUpgradeChoicesReady(choices);
            return true;
        }

        private void OnUpgradeSelected(UpgradeData data)
        {
            if (data == null) return;
            _awaitingChoice = false;

            int newLevel = GetLevel(data) + 1;
            _levels[data] = newLevel;

            if (_blobRoot != null && data.Effect != null)
                data.Effect.Apply(_blobRoot, data);

            GameEvents.RaiseUpgradeApplied(data, newLevel);

            // Sırada bekleyen level-up varsa oyun duraklamış kalır, sıradaki kartlar gösterilir.
            while (_pendingLevelUps > 0)
            {
                _pendingLevelUps--;
                if (TryShowChoices()) return;
            }

            // _blobRoot bulunamasa bile oyunu duraklamış bırakma — soft-lock fix (B12).
            GameManager.Instance?.ResumeGame();
        }

        private UpgradeData[] SelectUpgrades()
        {
            var pool = new List<UpgradeData>();
            foreach (var u in _allUpgrades)
            {
                if (u != null && GetLevel(u) < u.MaxLevel) pool.Add(u);
            }
            foreach (var u in _dynamicUpgrades)
            {
                if (u != null && GetLevel(u) < u.MaxLevel && !pool.Contains(u)) pool.Add(u);
            }
            if (pool.Count == 0) return null;

            int count = Mathf.Min(_choiceCount, pool.Count);
            var selected = new UpgradeData[count];

            for (int i = 0; i < count; i++)
            {
                float total = 0f;
                foreach (var u in pool) total += u.Weight;

                float roll = Random.Range(0f, total);
                float cumulative = 0f;
                for (int j = 0; j < pool.Count; j++)
                {
                    cumulative += pool[j].Weight;
                    if (roll <= cumulative)
                    {
                        selected[i] = pool[j];
                        pool.RemoveAt(j);
                        break;
                    }
                }
            }

            return selected;
        }
    }
}
