using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button[] _upgradeButtons;
        [SerializeField] private TMP_Text[] _upgradeNameTexts;
        [SerializeField] private TMP_Text[] _upgradeDescTexts;

        private UpgradeData[] _currentChoices;

        private void OnEnable()
        {
            GameEvents.OnUpgradeChoicesReady += ShowChoices;
            GameEvents.OnGameOver += Hide;
        }

        private void OnDisable()
        {
            GameEvents.OnUpgradeChoicesReady -= ShowChoices;
            GameEvents.OnGameOver -= Hide;
        }

        private void Start()
        {
            if (_panel != null) _panel.SetActive(false);

            for (int i = 0; i < _upgradeButtons.Length; i++)
            {
                int index = i;
                _upgradeButtons[i].onClick.AddListener(() => SelectUpgrade(index));
            }
        }

        private void ShowChoices(UpgradeData[] choices)
        {
            _currentChoices = choices;
            if (_panel != null) _panel.SetActive(true);

            for (int i = 0; i < _upgradeButtons.Length; i++)
            {
                bool hasChoice = i < choices.Length && choices[i] != null;
                _upgradeButtons[i].gameObject.SetActive(hasChoice);

                if (!hasChoice) continue;

                if (_upgradeNameTexts != null && i < _upgradeNameTexts.Length && _upgradeNameTexts[i] != null)
                    _upgradeNameTexts[i].text = choices[i].DisplayName;

                if (_upgradeDescTexts != null && i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                    _upgradeDescTexts[i].text = choices[i].Description;
            }
        }

        private void SelectUpgrade(int index)
        {
            if (_currentChoices == null || index >= _currentChoices.Length) return;
            GameEvents.RaiseUpgradeSelected(_currentChoices[index]);
            Hide();
        }

        private void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
