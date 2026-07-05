using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    public class UpgradeSystem : MonoBehaviour
    {
        [SerializeField] private UpgradeData[] _allUpgrades;
        [SerializeField] private int _choiceCount = 3;

        private GameObject _blobRoot;

        private void Start()
        {
            _blobRoot = GameObject.FindWithTag("Blob");
            ResetUpgradeLevels();
        }

        private void ResetUpgradeLevels()
        {
            if (_allUpgrades == null) return;
            foreach (var u in _allUpgrades)
            {
                if (u != null) u.CurrentLevel = 0;
            }
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
            if (data == null || _blobRoot == null) return;
            data.CurrentLevel++;
            if (data.Effect != null) data.Effect.Apply(_blobRoot, data);
            GameManager.Instance?.ResumeGame();
        }

        private UpgradeData[] SelectUpgrades()
        {
            var pool = new List<UpgradeData>();
            foreach (var u in _allUpgrades)
            {
                if (u != null && u.CurrentLevel < u.MaxLevel) pool.Add(u);
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
