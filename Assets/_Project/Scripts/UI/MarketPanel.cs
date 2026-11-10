using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Data;
using BlobSurvivor.Systems.Meta;

namespace BlobSurvivor.UI
{
    // Lobiden açılan Market. Lobi/HUD diliyle: yarı saydam gece mavisi kartlar, kalın beyaz yazı, kredi altın.
    // Üstte öne çıkan karakter kartı (Mıknato), altında kademeli kalıcı güçler. Satıra dokunmak satın alır;
    // kredi yetmezse kredi rozeti sallanır. Düzen çalışma zamanında bir kez kurulur; sahnedeki eski
    // buton/yazılar gizlenir (sadece _panel ve ilk butonun yuvarlak sprite'ı kullanılır).
    public class MarketPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button[] _buttons; // eski düzen; sadece yuvarlak sprite'ı alınır

        private static readonly Color Backdrop = new Color(0.05f, 0.05f, 0.17f, 0.7f);
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.82f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color GoldInk = new Color(0.12f, 0.08f, 0.02f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.78f);
        private static readonly Color Faint = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color Owned = new Color(0.35f, 0.82f, 0.45f, 1f);
        private static readonly Color PillLocked = new Color(0.32f, 0.32f, 0.36f, 0.9f);
        private static readonly Color Warn = new Color(1f, 0.42f, 0.38f, 1f);
        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);

        private static readonly MetaStatType[] StatRows =
        {
            MetaStatType.Speed, MetaStatType.MaxHealth, MetaStatType.MassGain, MetaStatType.CoinGain, MetaStatType.XpMultiplier
        };

        private class Row
        {
            public MetaStatType Type;
            public RectTransform Rect;
            public TMP_Text Name, Detail, Level, Price;
            public Image Pill;
        }

        private LobbyPanel _lobby;
        private CharacterData _character;
        private Row _characterRow;
        private Row[] _rows;
        private RectTransform _creditChip;
        private TMP_Text _credit;
        private bool _built;

        private void Start()
        {
            if (MetaProgression.Instance != null) MetaProgression.Instance.Changed += Refresh;
            if (!_built) Hide();
        }

        private void OnDestroy()
        {
            if (MetaProgression.Instance != null) MetaProgression.Instance.Changed -= Refresh;
        }

        public void Show(LobbyPanel lobby)
        {
            _lobby = lobby;
            Build();
            if (_panel != null) _panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void Back()
        {
            Hide();
            if (_lobby != null) _lobby.Show();
        }

        private void Purchase(Row row)
        {
            MetaProgression meta = MetaProgression.Instance;
            if (meta == null || meta.IsPurchased(row.Type)) return;

            if (meta.TryPurchase(row.Type))
            {
                StartCoroutine(Punch(row.Rect, 1.06f));
                StartCoroutine(Punch(_creditChip, 1.1f));
            }
            else
            {
                StartCoroutine(Shake(_creditChip));
            }
            Refresh();
        }

        private void Refresh()
        {
            MetaProgression meta = MetaProgression.Instance;
            if (!_built || meta == null) return;

            _credit.text = $"{meta.Credit:N0} coin";
            if (_characterRow != null) RefreshRow(_characterRow, meta);
            foreach (Row row in _rows) RefreshRow(row, meta);
        }

        private void RefreshRow(Row row, MetaProgression meta)
        {
            bool owned = meta.IsPurchased(row.Type);
            int cost = meta.GetCost(row.Type);
            bool affordable = meta.Credit >= cost;

            if (owned)
            {
                row.Pill.color = Owned;
                row.Price.color = GoldInk;
                row.Price.text = row.Type == MetaStatType.MiknatoCharacter ? "AÇIK" : "ALINDI";
            }
            else
            {
                row.Pill.color = affordable ? Gold : PillLocked;
                row.Price.color = affordable ? GoldInk : Faint;
                row.Price.text = $"{cost:N0}";
            }

            if (row.Level != null)
            {
                int level = LevelOf(row.Type, meta);
                row.Level.text = level > 0 ? $"Sv. {level}" : "";
            }
            if (row.Type != MetaStatType.MiknatoCharacter) row.Detail.text = DetailOf(row.Type, meta);
            else if (owned) row.Detail.text = "Lobide seçebilirsin.";
        }

        private static int LevelOf(MetaStatType type, MetaProgression meta)
        {
            switch (type)
            {
                case MetaStatType.Speed: return meta.SpeedLevel;
                case MetaStatType.MaxHealth: return meta.MaxHealthLevel;
                case MetaStatType.MassGain: return meta.MassGainLevel;
                case MetaStatType.CoinGain: return meta.CoinGainLevel;
                default: return 0;
            }
        }

        private static string NameOf(MetaStatType type)
        {
            switch (type)
            {
                case MetaStatType.Speed: return "Hız";
                case MetaStatType.MaxHealth: return "Max Can";
                case MetaStatType.MassGain: return "Kütle Kazancı";
                case MetaStatType.CoinGain: return "Coin Kazancı";
                case MetaStatType.XpMultiplier: return "XP Çarpanı";
                default: return type.ToString();
            }
        }

        private static string DetailOf(MetaStatType type, MetaProgression meta)
        {
            switch (type)
            {
                case MetaStatType.Speed: return Stacked("+%5 hareket hızı", meta.SpeedBonus * 100f, "%");
                case MetaStatType.MaxHealth: return Stacked("+10 max can", meta.MaxHealthBonus, "");
                case MetaStatType.MassGain: return Stacked("+%5 kütle", meta.MassGainBonus * 100f, "%");
                case MetaStatType.CoinGain: return Stacked("+%5 coin", meta.CoinGainBonus * 100f, "%");
                case MetaStatType.XpMultiplier: return meta.XpMultiplierPurchased ? "Kalıcı +%10 XP" : "Kalıcı +%10 XP · tek sefer";
                default: return "";
            }
        }

        private static string Stacked(string perLevel, float total, string unit)
        {
            if (total <= 0f) return perLevel;
            string amount = unit == "%" ? $"+%{total:0}" : $"+{total:0}";
            return $"{perLevel} · şu an {amount}";
        }

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build()
        {
            if (_built || _panel == null) return;
            _built = true;
            var panel = (RectTransform)_panel.transform;

            Sprite rounded = null;
            foreach (Button b in _buttons)
                if (b != null && b.targetGraphic is Image bi && bi.sprite != null) { rounded = bi.sprite; break; }

            // Lobiyle aynı ebeveyn (güvenli alan) ve hemen üstünde: koordinatlar birebir aynı olsun.
            if (_lobby != null && _lobby.PanelRect != null && _lobby.PanelRect.parent != null)
            {
                panel.SetParent(_lobby.PanelRect.parent, false);
                panel.SetSiblingIndex(_lobby.PanelRect.GetSiblingIndex() + 1);
            }
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.anchoredPosition = Vector2.zero; panel.sizeDelta = Vector2.zero;
            foreach (Transform c in panel) c.gameObject.SetActive(false); // eski düzen

            Image backdrop = _panel.GetComponent<Image>();
            if (backdrop != null) { backdrop.color = Color.clear; backdrop.sprite = null; backdrop.raycastTarget = true; }
            LobbyPanel.ScreenBackdrop(panel, Backdrop);

            Shadowed("Title", panel, new Vector2(0f, 760f), new Vector2(1000f, 120f), "MARKET", 92f, Gold);

            _creditChip = Box("CreditChip", panel, new Vector2(0f, 650f), new Vector2(360f, 72f), rounded, CardColor);
            _credit = Text("Credit", _creditChip, Vector2.zero, _creditChip.sizeDelta, 38f, TextAlignmentOptions.Center, Gold);

            RectTransform back = Box("BackButton", panel, new Vector2(-420f, 760f), new Vector2(120f, 100f), rounded, CardColor);
            MakeButton(back, Back);
            Text("Label", back, Vector2.zero, back.sizeDelta, 56f, TextAlignmentOptions.Center, Color.white).text = "<";

            _character = _lobby != null ? _lobby.MarketCharacter : null;
            if (_character != null) _characterRow = BuildCharacterCard(panel, new Vector2(0f, 420f), rounded);

            Text("SectionLabel", panel, new Vector2(-450f, 222f), new Vector2(900f, 50f), 34f, TextAlignmentOptions.Left, Faint)
                .text = "KALICI GÜÇLER · her run'da geçerli";

            _rows = new Row[StatRows.Length];
            for (int i = 0; i < StatRows.Length; i++)
                _rows[i] = BuildStatRow(StatRows[i], panel, new Vector2(0f, 110f - i * 172f), rounded);

            LobbyPanel.PinToEdge(panel, 1f, "Title", "TitleShadow", "CreditChip", "BackButton");
        }

        private Row BuildCharacterCard(RectTransform panel, Vector2 pos, Sprite rounded)
        {
            var row = new Row { Type = MetaStatType.MiknatoCharacter };
            row.Rect = Box("CharacterCard", panel, pos, new Vector2(900f, 300f), rounded, CardColor);
            MakeButton(row.Rect, () => Purchase(row));

            Box("Accent", row.Rect, new Vector2(0f, 146f), new Vector2(860f, 8f), rounded, new Color(1f, 0.82f, 0.25f, 0.8f));

            RectTransform frame = Box("PortraitFrame", row.Rect, new Vector2(-310f, 0f), new Vector2(240f, 240f), rounded, new Color(1f, 1f, 1f, 0.06f));
            var portrait = NewRect("Portrait", frame, new Vector2(0f, 6f), new Vector2(220f, 220f)).gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.sprite = _character.Icon;
            portrait.enabled = _character.Icon != null;

            Text("Tag", row.Rect, new Vector2(120f, 96f), new Vector2(580f, 40f), 28f, TextAlignmentOptions.Left, Gold).text = "YENİ KARAKTER";
            row.Name = Text("Name", row.Rect, new Vector2(120f, 46f), new Vector2(580f, 80f), 64f, TextAlignmentOptions.Left, Color.white);
            row.Name.text = _character.DisplayName;
            row.Detail = Text("Description", row.Rect, new Vector2(20f, -46f), new Vector2(380f, 110f), 30f, TextAlignmentOptions.TopLeft, Subtle);
            row.Detail.fontStyle = FontStyles.Normal;
            row.Detail.textWrappingMode = TextWrappingModes.Normal;
            row.Detail.text = _character.Description;

            row.Pill = Box("Price", row.Rect, new Vector2(320f, -70f), new Vector2(200f, 84f), rounded, Gold).GetComponent<Image>();
            row.Price = Text("Text", (RectTransform)row.Pill.transform, Vector2.zero, new Vector2(200f, 84f), 40f, TextAlignmentOptions.Center, GoldInk);
            return row;
        }

        private Row BuildStatRow(MetaStatType type, RectTransform panel, Vector2 pos, Sprite rounded)
        {
            var row = new Row { Type = type };
            row.Rect = Box("Stat_" + type, panel, pos, new Vector2(900f, 152f), rounded, CardColor);
            MakeButton(row.Rect, () => Purchase(row));

            row.Name = Text("Name", row.Rect, new Vector2(-150f, 26f), new Vector2(520f, 60f), 46f, TextAlignmentOptions.Left, Color.white);
            row.Name.text = NameOf(type);
            row.Level = Text("Level", row.Rect, new Vector2(110f, 26f), new Vector2(160f, 60f), 32f, TextAlignmentOptions.Right, Gold);
            row.Detail = Text("Detail", row.Rect, new Vector2(-110f, -34f), new Vector2(600f, 50f), 30f, TextAlignmentOptions.Left, Subtle);
            row.Detail.fontStyle = FontStyles.Normal;

            row.Pill = Box("Price", row.Rect, new Vector2(320f, 0f), new Vector2(200f, 84f), rounded, Gold).GetComponent<Image>();
            row.Price = Text("Text", (RectTransform)row.Pill.transform, Vector2.zero, new Vector2(200f, 84f), 40f, TextAlignmentOptions.Center, GoldInk);
            return row;
        }

        // ---------------------------------------------------------------- geri bildirim

        private static IEnumerator Punch(RectTransform rect, float peak)
        {
            const float duration = 0.22f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, peak, k);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        private IEnumerator Shake(RectTransform rect)
        {
            const float duration = 0.35f;
            Vector2 home = rect.anchoredPosition;
            _credit.color = Warn;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float fade = 1f - t / duration;
                rect.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * 14f * fade, 0f);
                yield return null;
            }
            rect.anchoredPosition = home;
            _credit.color = Gold;
        }

        // ---------------------------------------------------------------- yardımcılar (LobbyPanel ile aynı)

        private static void MakeButton(RectTransform rect, UnityEngine.Events.UnityAction onClick)
        {
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.85f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);
        }

        private static void Place(RectTransform rect, RectTransform parent, Vector2 pos, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            Place(rect, parent, pos, size);
            return rect;
        }

        private static RectTransform Box(string name, RectTransform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            if (sprite != null) { image.sprite = sprite; image.type = Image.Type.Sliced; }
            return rect;
        }

        private static TMP_Text Text(string name, RectTransform parent, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = alignment;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void Shadowed(string name, RectTransform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color)
        {
            Text(name + "Shadow", parent, pos + new Vector2(0f, -fontSize * 0.06f), size, fontSize, TextAlignmentOptions.Center, ShadowColor).text = text;
            Text(name, parent, pos, size, fontSize, TextAlignmentOptions.Center, color).text = text;
        }
    }
}
