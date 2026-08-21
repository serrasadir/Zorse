using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

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

                if (_icons != null && index < _icons.Length && _icons[index] != null)
                    _icons[index].sprite = _characters[index].Icon;

                if (_nameTexts != null && index < _nameTexts.Length && _nameTexts[index] != null)
                    _nameTexts[index].text = _characters[index].DisplayName;
            }

            if (_marketButton != null)
                _marketButton.onClick.AddListener(OpenMarket);
        }

        // M23: MarketPanel'in "geri" butonu Inspector'dan bunu çağırır.
        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
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
