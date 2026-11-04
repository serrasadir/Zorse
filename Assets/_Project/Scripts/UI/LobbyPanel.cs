using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems.Meta;

namespace BlobSurvivor.UI
{
    public class LobbyPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _hud;
        [SerializeField] private CharacterData[] _characters;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Image[] _icons;
        [SerializeField] private TMP_Text[] _nameTexts;

        // M23: Market'e giriş — MarketPanel'in kendi "geri" butonu buraya değil, bu script'e bağımlı
        // olmadan LobbyPanel.Show + MarketPanel.Hide'ı Inspector'da iki ayrı OnClick listener olarak
        // wire ediyor (döngüsel script referansına gerek kalmadı).
        [SerializeField] private Button _marketButton;
        [SerializeField] private MarketPanel _marketPanel;

        private void Start()
        {
            if (_panel != null) _panel.SetActive(true);
            if (_hud != null) _hud.SetActive(false);

            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                if (index >= _characters.Length || _characters[index] == null) continue;

                _buttons[i].onClick.AddListener(() => OnCharacterSelected(_characters[index]));
            }

            if (_marketButton != null)
                _marketButton.onClick.AddListener(OpenMarket);

            RefreshCharacterButtons();
        }

        // M23: MarketPanel'in "geri" butonu Inspector'dan bunu çağırır. Market'te Mıknato
        // satın alınmış olabileceğinden kilit durumu her Show()'da yeniden değerlendirilir.
        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            RefreshCharacterButtons();
        }

        // Kilitler: Mıknato Market'te 500 krediyle (RequiresMarketUnlock), Mermo 3 run bitirince
        // (RequiredCompletedRuns, ölüm de sayılır). Kilitliyken buton kapalı, isim altında koşul yazar.
        private void RefreshCharacterButtons()
        {
            MetaProgression meta = MetaProgression.Instance;
            bool miknatoUnlocked = meta == null || meta.MiknatoUnlocked;
            int completedRuns = meta != null ? meta.CompletedRuns : int.MaxValue;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (i >= _characters.Length || _characters[i] == null) continue;
                CharacterData data = _characters[i];
                bool marketLocked = data.RequiresMarketUnlock && !miknatoUnlocked;
                int runsLeft = Mathf.Max(0, data.RequiredCompletedRuns - completedRuns);
                bool locked = marketLocked || runsLeft > 0;
                string lockReason = marketLocked ? "(Market'te açılır)" : $"({runsLeft} run sonra açılır)";

                if (_buttons[i] != null)
                    _buttons[i].interactable = !locked;

                if (_icons != null && i < _icons.Length && _icons[i] != null)
                    _icons[i].sprite = data.Icon;

                if (_nameTexts != null && i < _nameTexts.Length && _nameTexts[i] != null)
                    _nameTexts[i].text = locked ? $"{data.DisplayName}\n{lockReason}" : data.DisplayName;
            }
        }

        private void OpenMarket()
        {
            if (_panel != null) _panel.SetActive(false);
            _marketPanel?.Show();
        }

        private void OnCharacterSelected(CharacterData data)
        {
            if (_panel != null) _panel.SetActive(false);
            if (_hud != null) _hud.SetActive(true);
            GameManager.Instance.StartGame(data);
        }
    }
}
