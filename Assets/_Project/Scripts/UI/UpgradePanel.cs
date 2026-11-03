using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.UI
{
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
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
                {
                    // B17: evrim çıktısı kartları ★ + altın renkle işaretlenir (mevcut kart sistemine ek görsel işaret, ayrı ekran yok).
                    _upgradeNameTexts[i].text = choices[i].IsEvolution ? $"★ {choices[i].DisplayName}" : $"{choices[i].DisplayName}  {LevelLabel(choices[i])}";
                    _upgradeNameTexts[i].color = choices[i].IsEvolution ? new Color(1f, 0.82f, 0.25f, 1f) : Color.white;
                }

                if (_upgradeDescTexts != null && i < _upgradeDescTexts.Length && _upgradeDescTexts[i] != null)
                    _upgradeDescTexts[i].text = DescriptionFor(choices[i]);
            }
        }

        // 2026-11-03: oyuncu yeni skill mi alıyor, var olanı mı yükseltiyor görsün — seçilince ulaşacağı seviye.
        private static string DescriptionFor(UpgradeData data)
        {
            int next = (UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 0) + 1;
            string text = data.GetDescriptionForLevel(next);
            return text.Contains("{ultimate}")
                ? text.Replace("{ultimate}", WeaponBase.CurrentUltimateDescription ?? "Silahın son formuna ulaşır.")
                : text;
        }

        private static string LevelLabel(UpgradeData data)
        {
            int current = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 0;
            return current == 0 ? "<size=70%>YENİ</size>" : $"<size=70%>Sv. {current + 1}/{data.MaxLevel}</size>";
        }

        private void SelectUpgrade(int index)
        {
            if (_currentChoices == null || index >= _currentChoices.Length) return;
            UpgradeData chosen = _currentChoices[index];
            // Önce gizle: sırada bekleyen level-up varsa yeni kartlar bu çağrının içinde açılır.
            Hide();
            GameEvents.RaiseUpgradeSelected(chosen);
        }

        private void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
