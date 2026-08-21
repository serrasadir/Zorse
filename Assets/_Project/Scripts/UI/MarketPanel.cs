using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Systems.Meta;

namespace BlobSurvivor.UI
{
    // M21 (GDD §8): 6 sabit kalemlik Market ekranı — LobbyPanel/UpgradePanel'deki index-aligned
    // serialized array deseniyle aynı (dinamik prefab instantiation yok, sabit sayıda kalem).
    // Panel'in kendisini açıp kapatmak M23'ün (Lobby <-> Market butonu) işi — burada sadece
    // Show()/Hide() public API'si var.
    public class MarketPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _creditText;
        [SerializeField] private MetaStatType[] _statTypes;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private TMP_Text[] _labelTexts;

        private void Start()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                if (_buttons[i] != null)
                    _buttons[i].onClick.AddListener(() => Purchase(index));
            }

            Hide();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void Purchase(int index)
        {
            if (index >= _statTypes.Length) return;
            MetaProgression.Instance?.TryPurchase(_statTypes[index]);
            Refresh();
        }

        private void Refresh()
        {
            MetaProgression meta = MetaProgression.Instance;
            if (meta == null) return;

            if (_creditText != null)
                _creditText.text = $"{meta.Credit} kredi";

            for (int i = 0; i < _statTypes.Length; i++)
            {
                MetaStatType type = _statTypes[i];
                bool purchased = meta.IsPurchased(type);
                int cost = meta.GetCost(type);

                if (i < _buttons.Length && _buttons[i] != null)
                    _buttons[i].interactable = !purchased && meta.Credit >= cost;

                if (i < _labelTexts.Length && _labelTexts[i] != null)
                {
                    _labelTexts[i].text = purchased
                        ? $"{DisplayName(type)}\nSatın Alındı"
                        : $"{DisplayName(type)}{LevelSuffix(type, meta)}\n{cost} kredi";
                }
            }
        }

        private string LevelSuffix(MetaStatType type, MetaProgression meta)
        {
            switch (type)
            {
                case MetaStatType.Speed: return $" (Lv.{meta.SpeedLevel})";
                case MetaStatType.MaxHealth: return $" (Lv.{meta.MaxHealthLevel})";
                case MetaStatType.MassGain: return $" (Lv.{meta.MassGainLevel})";
                case MetaStatType.CoinGain: return $" (Lv.{meta.CoinGainLevel})";
                default: return "";
            }
        }

        private string DisplayName(MetaStatType type)
        {
            switch (type)
            {
                case MetaStatType.MiknatoCharacter: return "Mıknato Karakteri";
                case MetaStatType.Speed: return "Hız +%5";
                case MetaStatType.MaxHealth: return "Max Can +10";
                case MetaStatType.MassGain: return "Mass Kazanımı +%5";
                case MetaStatType.CoinGain: return "Coin Kazanımı +%5";
                case MetaStatType.XpMultiplier: return "XP Çarpanı +%10";
                default: return type.ToString();
            }
        }
    }
}
