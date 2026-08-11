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
        }

        private void OnEnable()
        {
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnUpgradeSelected += OnUpgradeSelected;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnUpgradeSelected -= OnUpgradeSelected;
        }

        public int GetLevel(UpgradeData data)
        {
            return data != null && _levels.TryGetValue(data, out int level) ? level : 0;
        }

        private void OnLevelUp(int level)
        {
            if (_allUpgrades == null || _allUpgrades.Length == 0) return;

            UpgradeData[] choices = SelectUpgrades();
            if (choices == null || choices.Length == 0) return;

            GameEvents.RaiseUpgradeChoicesReady(choices);
            GameManager.Instance?.PauseGame();
        }

        private void OnUpgradeSelected(UpgradeData data)
        {
            if (data == null) return;

            _levels[data] = GetLevel(data) + 1;

            // _blobRoot bulunamasa bile oyunu duraklamış bırakma — soft-lock fix (B12).
            if (_blobRoot != null && data.Effect != null)
                data.Effect.Apply(_blobRoot, data);

            GameManager.Instance?.ResumeGame();
        }

        private UpgradeData[] SelectUpgrades()
        {
            var pool = new List<UpgradeData>();
            foreach (var u in _allUpgrades)
            {
                if (u != null && GetLevel(u) < u.MaxLevel) pool.Add(u);
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
