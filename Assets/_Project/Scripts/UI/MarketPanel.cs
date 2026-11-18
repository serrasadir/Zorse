using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;
using BlobSurvivor.Systems.Meta;

namespace BlobSurvivor.UI
{
    // Lobiden açılan Market. Lobi/HUD diliyle: yarı saydam gece mavisi kartlar, kalın beyaz yazı, kredi altın.
    // Üç sekme: KARAKTERLER (coin'le satılanlar + run'la açılan Dalcık), GÜÇLER (kalıcı güçlendiriciler, kategorili,
    // kaydırmalı) ve KİLİTLER (oynayarak açılan skiller: koşul + ilerleme; satılmaz). Satıra dokunmak satın alır;
    // kredi yetmezse kredi rozeti sallanır. Düzen çalışma zamanında bir kez kurulur; sahnedeki eski buton/yazılar gizlenir.
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
        private static readonly Color ActiveColor = new Color(1f, 0.6f, 0.25f, 1f);
        private static readonly Color PassiveColor = new Color(0.45f, 0.75f, 1f, 1f);
        private static readonly Color RareColor = new Color(0.78f, 0.52f, 1f, 1f);

        private const float RowWidth = 900f, RowHeight = 152f, SkillRowHeight = 176f, RowGap = 18f, HeaderHeight = 64f;
        private const float HeaderTop = 500f, ListBottom = 60f, ListWidth = 960f;

        // GÜÇLER sekmesi: kategori başlığı + o kategorinin satırları (eski statlar ve yeni güçlendiriciler bir arada).
        private struct PowerItem
        {
            public string Header;
            public MetaStatType? Stat;
            public PowerUpDef PowerUp;
        }

        private static readonly PowerItem[] PowerLayout =
        {
            new PowerItem { Header = "SAVUNMA" },
            new PowerItem { Stat = MetaStatType.MaxHealth },
            new PowerItem { PowerUp = PowerUps.Armor },
            new PowerItem { PowerUp = PowerUps.Regen },
            new PowerItem { PowerUp = PowerUps.ThickSkin },
            new PowerItem { Header = "BÜYÜME VE YEME" },
            new PowerItem { Stat = MetaStatType.Speed },
            new PowerItem { Stat = MetaStatType.MassGain },
            new PowerItem { PowerUp = PowerUps.WideMouth },
            new PowerItem { PowerUp = PowerUps.DigestResist },
            new PowerItem { PowerUp = PowerUps.Magnet },
            new PowerItem { Header = "EKONOMİ" },
            new PowerItem { Stat = MetaStatType.CoinGain },
            new PowerItem { Stat = MetaStatType.XpMultiplier },
            new PowerItem { Header = "ZORLUK" },
            new PowerItem { PowerUp = PowerUps.Curse },
        };

        private class Row
        {
            public MetaStatType Type;
            public CharacterData Character; // karakter kartı
            public PowerUpDef PowerUp;      // güçlendirici
            public UpgradeData Skill;       // skill kilidi
            public RectTransform Rect;
            public TMP_Text Name, Detail, Level, Price;
            public Image Pill;
            public RectTransform Progress, ProgressFill; // run'la açılan karakter, skill kilidi
        }

        private LobbyPanel _lobby;
        private readonly List<Row> _characterRows = new List<Row>();
        private readonly List<Row> _rows = new List<Row>(); // güç + skill satırları
        private RectTransform[] _pages;
        private Image[] _tabs;
        private TMP_Text[] _tabTexts;
        private int _tab;
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
            if (meta == null) return;
            bool bought;
            if (row.Character != null)
            {
                // Run'la açılan karakter satılmaz; dokununca ilerleme çubuğu zıplar.
                if (row.Character.MarketPrice <= 0 || meta.IsCharacterPurchased(row.Character))
                {
                    if (row.Progress != null && row.Progress.gameObject.activeSelf) StartCoroutine(Punch(row.Progress, 1.08f));
                    return;
                }
                bought = meta.TryPurchaseCharacter(row.Character);
            }
            else if (row.PowerUp != null)
            {
                if (meta.IsMaxed(row.PowerUp)) return;
                bought = meta.TryPurchase(row.PowerUp);
            }
            else if (row.Skill != null)
            {
                // Skill satılmaz, oynayarak açılır: dokununca satır zıplar.
                StartCoroutine(Punch(row.Rect, 1.04f));
                return;
            }
            else
            {
                if (meta.IsPurchased(row.Type)) return;
                bought = meta.TryPurchase(row.Type);
            }

            if (bought)
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
            RefreshResetRow(meta);

            _credit.text = $"{meta.Credit:N0} coin";
            foreach (Row row in _characterRows) RefreshCharacterRow(row, meta);
            foreach (Row row in _rows)
            {
                if (row.PowerUp != null) RefreshPowerUpRow(row, meta);
                else if (row.Skill != null) RefreshSkillRow(row, meta);
                else RefreshStatRow(row, meta);
            }
        }

        private void RefreshCharacterRow(Row row, MetaProgression meta)
        {
            CharacterData c = row.Character;
            bool unlocked = meta.IsCharacterUnlocked(c);
            int runsLeft = meta.CharacterRunsLeft(c);
            row.Progress.gameObject.SetActive(!unlocked && c.RequiredCompletedRuns > 0);

            if (unlocked)
            {
                SetPill(row, Owned, GoldInk, "AÇIK");
                row.Detail.text = $"{c.Description}\n<color=#59D173>Lobide seçebilirsin.</color>";
            }
            else if (c.MarketPrice > 0 && !meta.IsCharacterPurchased(c))
            {
                SetPrice(row, c.MarketPrice, meta.Credit);
                row.Detail.text = c.Description;
            }
            else
            {
                int done = c.RequiredCompletedRuns - runsLeft;
                SetPill(row, PillLocked, Color.white, $"{done}/{c.RequiredCompletedRuns} run");
                row.Detail.text = $"{c.Description}\n<color=#FFD140>{c.RequiredCompletedRuns} run oyna, açılsın.</color>\nÖlüm de sayılır.";
                row.ProgressFill.anchorMax = new Vector2(Mathf.Clamp01((float)done / c.RequiredCompletedRuns), 1f);
            }
        }

        private void RefreshStatRow(Row row, MetaProgression meta)
        {
            if (meta.IsPurchased(row.Type))
                SetPill(row, Owned, GoldInk, row.Type == MetaStatType.XpMultiplier ? "ALINDI" : "MAX");
            else
                SetPrice(row, meta.GetCost(row.Type), meta.Credit);

            int level = StatLevelOf(row.Type, meta);
            row.Level.text = level > 0 ? $"Sv. {level}/{MetaProgression.MaxStatLevel}" : "";
            row.Detail.text = StatDetailOf(row.Type, meta);
        }

        private void RefreshPowerUpRow(Row row, MetaProgression meta)
        {
            PowerUpDef def = row.PowerUp;
            int level = meta.GetLevel(def);
            if (meta.IsMaxed(def)) SetPill(row, Owned, GoldInk, "MAX");
            else SetPrice(row, meta.GetCost(def), meta.Credit);
            row.Level.text = level > 0 ? $"Sv. {level}/{def.MaxLevel}" : "";
            row.Detail.text = level > 0 ? $"{def.PerLevelText} · şu an {def.TotalText(meta.GetValue(def))}" : def.PerLevelText;
        }

        private void RefreshSkillRow(Row row, MetaProgression meta)
        {
            bool locked = SkillUnlocks.TryGet(row.Skill.Id, out SkillUnlocks.Entry entry);
            bool unlocked = meta.IsSkillUnlocked(row.Skill.Id);
            if (!locked)
            {
                SetPill(row, Owned, GoldInk, "AÇIK");
                row.Progress.gameObject.SetActive(false);
                return;
            }
            int value = meta.GetStat(entry.Stat);
            if (unlocked)
            {
                SetPill(row, Owned, GoldInk, "AÇILDI");
                row.Detail.text = $"<color=#59D173>{entry.Text}</color>";
                row.Progress.gameObject.SetActive(false);
            }
            else
            {
                string progress = SkillUnlocks.ProgressText(entry, value);
                SetPill(row, PillLocked, Color.white, string.IsNullOrEmpty(progress) ? "KİLİTLİ" : progress);
                row.Detail.text = $"<color=#FFD140>{entry.Text}</color>";
                row.Progress.gameObject.SetActive(entry.Target > 1);
                row.ProgressFill.anchorMax = new Vector2(Mathf.Clamp01((float)value / entry.Target), 1f);
            }
        }

        private static void SetPill(Row row, Color pill, Color ink, string text)
        {
            row.Pill.color = pill;
            row.Price.color = ink;
            row.Price.text = text;
        }

        private static void SetPrice(Row row, int cost, int credit)
        {
            bool affordable = credit >= cost;
            SetPill(row, affordable ? Gold : PillLocked, affordable ? GoldInk : Faint, $"{cost:N0}");
        }

        private void ShowTab(int tab)
        {
            _tab = tab;
            for (int i = 0; i < _pages.Length; i++)
            {
                bool on = i == tab;
                _pages[i].gameObject.SetActive(on);
                _tabs[i].color = on ? Gold : CardColor;
                _tabTexts[i].color = on ? GoldInk : Color.white;
            }
        }

        private static int StatLevelOf(MetaStatType type, MetaProgression meta)
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

        private static string StatNameOf(MetaStatType type)
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

        private static string StatDetailOf(MetaStatType type, MetaProgression meta)
        {
            switch (type)
            {
                case MetaStatType.Speed: return Stacked("+%5 hareket hızı", meta.SpeedBonus * 100f, "%");
                case MetaStatType.MaxHealth: return Stacked("+10 max can", meta.MaxHealthBonus, "");
                case MetaStatType.MassGain: return Stacked("+%5 kütle", meta.MassGainBonus * 100f, "%");
                case MetaStatType.CoinGain: return Stacked("+%5 coin", meta.CoinGainBonus * 100f, "%");
                case MetaStatType.XpMultiplier:
                    string xp = $"Kalıcı +%{meta.XpMultiplierBonusValue * 100f:0} XP";
                    return meta.XpMultiplierPurchased ? xp : $"{xp} · tek sefer";
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

            // Sekmeler: başlık bloğuyla birlikte üste yapışır.
            string[] tabNames = { "KARAKTERLER", "GÜÇLER", "KİLİTLER" };
            RectTransform tabs = NewRect("Tabs", panel, new Vector2(0f, 540f), new Vector2(900f, 96f));
            _tabs = new Image[tabNames.Length];
            _tabTexts = new TMP_Text[tabNames.Length];
            const float tabWidth = 292f;
            for (int i = 0; i < tabNames.Length; i++)
            {
                int index = i;
                RectTransform tab = Box("Tab_" + i, tabs, new Vector2((i - 1) * (tabWidth + 12f), 0f), new Vector2(tabWidth, 96f), rounded, CardColor);
                MakeButton(tab, () => ShowTab(index));
                _tabs[i] = tab.GetComponent<Image>();
                _tabTexts[i] = Text("Label", tab, Vector2.zero, tab.sizeDelta, 36f, TextAlignmentOptions.Center, Color.white);
                _tabTexts[i].text = tabNames[i];
            }

            RectTransform charactersPage = NewRect("CharactersPage", panel, Vector2.zero, new Vector2(1080f, 1920f));
            _characterRows.Clear();
            if (_lobby != null && _lobby.Characters != null)
                foreach (CharacterData c in _lobby.Characters)
                    if (c != null && c.ShownInMarket)
                        _characterRows.Add(BuildCharacterCard(c, charactersPage, new Vector2(0f, 340f - _characterRows.Count * 285f), rounded));

            _rows.Clear();
            RectTransform powerPage = BuildScrollPage("PowersPage", panel, out RectTransform powerContent);
            var powerLayout = new ListLayout(powerContent);
            foreach (PowerItem item in PowerLayout)
            {
                if (item.Header != null) powerLayout.Add(SectionLabel(powerContent, item.Header), HeaderHeight);
                else if (item.PowerUp != null) powerLayout.Add(BuildPowerUpRow(item.PowerUp, powerContent, rounded).Rect, RowHeight);
                else powerLayout.Add(BuildStatRow(item.Stat.Value, powerContent, rounded).Rect, RowHeight);
            }
            powerLayout.Add(BuildResetRow(powerContent, rounded), RowHeight);
            powerLayout.Finish();

            RectTransform skillsPage = BuildScrollPage("SkillsPage", panel, out RectTransform skillContent);
            var skillLayout = new ListLayout(skillContent);
            skillLayout.Add(SectionLabel(skillContent, "OYNAYARAK AÇILAN SKİLLER"), HeaderHeight);
            List<UpgradeData> skills = SkillsForMarket();
            foreach (UpgradeData u in skills)
                if (SkillUnlocks.IsLocked(u.Id)) skillLayout.Add(BuildSkillRow(u, skillContent, rounded).Rect, SkillRowHeight);
            skillLayout.Add(SectionLabel(skillContent, "BAŞLANGIÇ SKİLLERİ"), HeaderHeight);
            foreach (UpgradeData u in skills)
                if (!SkillUnlocks.IsLocked(u.Id)) skillLayout.Add(BuildSkillRow(u, skillContent, rounded).Rect, SkillRowHeight);
            skillLayout.Finish();

            _pages = new[] { charactersPage, powerPage, skillsPage };

            LobbyPanel.PinToEdge(panel, 1f, "Title", "TitleShadow", "CreditChip", "BackButton", "Tabs");
            ShowTab(_tab);
        }

        // Kart havuzundaki skiller (evrim ve herkeste olan sopa hariç); kilitliler koşul sırasıyla.
        private static List<UpgradeData> SkillsForMarket()
        {
            var list = new List<UpgradeData>();
            UpgradeData[] all = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.AllUpgrades : null;
            if (all == null) return list;
            foreach (UpgradeData u in all)
                if (u != null && !u.IsEvolution && !u.IsBaseWeapon) list.Add(u);
            list.Sort((a, b) => LockOrder(a.Id).CompareTo(LockOrder(b.Id)));
            return list;
        }

        private static int LockOrder(string id)
        {
            for (int i = 0; i < SkillUnlocks.Locked.Length; i++)
                if (SkillUnlocks.Locked[i].Id == id) return i;
            return SkillUnlocks.Locked.Length;
        }

        // Başlıkların altından ekranın altına uzanan dikey kaydırma alanı (ekran boyuna uyar).
        private static RectTransform BuildScrollPage(string name, RectTransform panel, out RectTransform content)
        {
            RectTransform page = NewRect(name, panel, Vector2.zero, Vector2.zero);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = page.offsetMax = Vector2.zero;

            RectTransform viewport = NewRect("Viewport", page, Vector2.zero, Vector2.zero);
            viewport.anchorMin = new Vector2(0.5f, 0f);
            viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.sizeDelta = new Vector2(ListWidth, 0f);
            viewport.offsetMin = new Vector2(-ListWidth * 0.5f, ListBottom);
            viewport.offsetMax = new Vector2(ListWidth * 0.5f, -HeaderTop);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear; // satır aralarından da sürüklenebilsin
            viewport.gameObject.AddComponent<RectMask2D>();

            content = NewRect("Content", viewport, Vector2.zero, new Vector2(ListWidth, 0f));
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            var scroll = page.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            return page;
        }

        // Liste satırlarını yukarıdan aşağı dizer, içerik boyunu ayarlar.
        private class ListLayout
        {
            private readonly RectTransform _content;
            private float _y;

            public ListLayout(RectTransform content) => _content = content;

            public void Add(RectTransform rect, float height)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -_y);
                _y += height + RowGap;
            }

            public void Finish() => _content.sizeDelta = new Vector2(_content.sizeDelta.x, _y + RowGap);
        }

        private static RectTransform SectionLabel(RectTransform parent, string text)
        {
            TMP_Text label = Text("Section", parent, Vector2.zero, new Vector2(RowWidth, HeaderHeight), 34f, TextAlignmentOptions.BottomLeft, Faint);
            label.text = text;
            return label.rectTransform;
        }

        private Row BuildCharacterCard(CharacterData character, RectTransform parent, Vector2 pos, Sprite rounded)
        {
            var row = new Row { Character = character };
            row.Rect = Box("Character_" + character.SaveId, parent, pos, new Vector2(900f, 260f), rounded, CardColor);
            MakeButton(row.Rect, () => Purchase(row));

            RectTransform frame = Box("PortraitFrame", row.Rect, new Vector2(-325f, 0f), new Vector2(210f, 210f), rounded, new Color(1f, 1f, 1f, 0.06f));
            var portrait = NewRect("Portrait", frame, new Vector2(0f, 5f), new Vector2(195f, 195f)).gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.sprite = character.Icon;
            portrait.enabled = character.Icon != null;

            row.Name = Text("Name", row.Rect, new Vector2(45f, 70f), new Vector2(350f, 76f), 58f, TextAlignmentOptions.Left, Color.white);
            row.Name.text = character.DisplayName;
            row.Detail = Text("Description", row.Rect, new Vector2(45f, -25f), new Vector2(350f, 110f), 26f, TextAlignmentOptions.TopLeft, Subtle);
            row.Detail.fontStyle = FontStyles.Normal;
            row.Detail.textWrappingMode = TextWrappingModes.Normal;

            row.Progress = Box("Progress", row.Rect, new Vector2(45f, -100f), new Vector2(350f, 18f), rounded, new Color(1f, 1f, 1f, 0.12f));
            row.ProgressFill = Box("Fill", row.Progress, Vector2.zero, Vector2.zero, rounded, Gold);
            row.ProgressFill.anchorMin = Vector2.zero;
            row.ProgressFill.anchorMax = new Vector2(0f, 1f);
            row.ProgressFill.pivot = new Vector2(0f, 0.5f);
            row.ProgressFill.offsetMin = row.ProgressFill.offsetMax = Vector2.zero;

            row.Pill = Box("Price", row.Rect, new Vector2(330f, 0f), new Vector2(200f, 84f), rounded, Gold).GetComponent<Image>();
            row.Price = Text("Text", (RectTransform)row.Pill.transform, Vector2.zero, new Vector2(200f, 84f), 38f, TextAlignmentOptions.Center, GoldInk);
            return row;
        }

        // GÜÇLER sonunda: bütün güçlendiricileri sıfırla, coin'in tamamı geri (VS "Refund"). Yanlışlıkla basılmasın
        // diye iki dokunuş: ilki onay sorar, 3 sn içinde ikincisi sıfırlar.
        private RectTransform _resetRow;
        private TMP_Text _resetText;
        private float _resetArmedUntil = -1f;

        private RectTransform BuildResetRow(RectTransform parent, Sprite rounded)
        {
            _resetRow = Box("Reset", parent, Vector2.zero, new Vector2(RowWidth, RowHeight), rounded, new Color(0.35f, 0.08f, 0.1f, 0.85f));
            MakeButton(_resetRow, ResetPowerUps);
            _resetText = Text("Label", _resetRow, Vector2.zero, _resetRow.sizeDelta, 38f, TextAlignmentOptions.Center, Color.white);
            return _resetRow;
        }

        private void RefreshResetRow(MetaProgression meta)
        {
            if (_resetText == null) return;
            int spent = meta.PowerUpSpent();
            bool armed = Time.unscaledTime < _resetArmedUntil;
            _resetText.text = spent <= 0 ? "SIFIRLA · alınmış güç yok"
                : armed ? $"EMİN MİSİN? Dokun: +{spent:N0} coin geri"
                : $"GÜÇLERİ SIFIRLA · +{spent:N0} coin geri";
            _resetText.color = spent <= 0 ? Faint : armed ? Gold : Color.white;
        }

        private void ResetPowerUps()
        {
            MetaProgression meta = MetaProgression.Instance;
            if (meta == null || meta.PowerUpSpent() <= 0) return;
            if (Time.unscaledTime >= _resetArmedUntil)
            {
                _resetArmedUntil = Time.unscaledTime + 3f;
                StartCoroutine(Punch(_resetRow, 1.04f));
                StartCoroutine(DisarmReset());
                Refresh();
                return;
            }
            _resetArmedUntil = -1f;
            meta.RefundPowerUps();
            StartCoroutine(Punch(_creditChip, 1.1f));
            Refresh();
        }

        private IEnumerator DisarmReset()
        {
            yield return new WaitForSecondsRealtime(3.05f);
            Refresh();
        }

        private Row BuildStatRow(MetaStatType type, RectTransform parent, Sprite rounded)
        {
            Row row = BuildPowerRowShell("Stat_" + type, StatNameOf(type), parent, rounded);
            row.Type = type;
            return row;
        }

        private Row BuildPowerUpRow(PowerUpDef def, RectTransform parent, Sprite rounded)
        {
            Row row = BuildPowerRowShell("PowerUp_" + def.Id, def.Name, parent, rounded);
            row.PowerUp = def;
            return row;
        }

        private Row BuildPowerRowShell(string id, string name, RectTransform parent, Sprite rounded)
        {
            var row = new Row();
            row.Rect = Box(id, parent, Vector2.zero, new Vector2(RowWidth, RowHeight), rounded, CardColor);
            MakeButton(row.Rect, () => Purchase(row));

            row.Name = Text("Name", row.Rect, new Vector2(-150f, 26f), new Vector2(520f, 60f), 46f, TextAlignmentOptions.Left, Color.white);
            row.Name.text = name;
            row.Level = Text("Level", row.Rect, new Vector2(110f, 26f), new Vector2(160f, 60f), 32f, TextAlignmentOptions.Right, Gold);
            row.Detail = Text("Detail", row.Rect, new Vector2(-110f, -34f), new Vector2(600f, 50f), 30f, TextAlignmentOptions.Left, Subtle);
            row.Detail.fontStyle = FontStyles.Normal;

            row.Pill = Box("Price", row.Rect, new Vector2(320f, 0f), new Vector2(200f, 84f), rounded, Gold).GetComponent<Image>();
            row.Price = Text("Text", (RectTransform)row.Pill.transform, Vector2.zero, new Vector2(200f, 84f), 40f, TextAlignmentOptions.Center, GoldInk);
            _rows.Add(row);
            return row;
        }

        private Row BuildSkillRow(UpgradeData skill, RectTransform parent, Sprite rounded)
        {
            var row = new Row { Skill = skill };
            row.Rect = Box("Skill_" + skill.Id, parent, Vector2.zero, new Vector2(RowWidth, SkillRowHeight), rounded, CardColor);
            MakeButton(row.Rect, () => Purchase(row));

            row.Name = Text("Name", row.Rect, new Vector2(-150f, 42f), new Vector2(520f, 56f), 44f, TextAlignmentOptions.Left, Color.white);
            row.Name.text = skill.DisplayName;

            Color kindColor = skill.Kind == SkillKind.Active ? ActiveColor : skill.Kind == SkillKind.Rare ? RareColor : PassiveColor;
            string kindName = skill.Kind == SkillKind.Active ? "AKTİF" : skill.Kind == SkillKind.Rare ? "NADİR" : "PASİF";
            row.Level = Text("Kind", row.Rect, new Vector2(110f, 42f), new Vector2(160f, 56f), 30f, TextAlignmentOptions.Right, kindColor);
            row.Level.text = kindName;

            // Koşul (kilitliyse) ya da açıklama; altında ilerleme çubuğu.
            row.Detail = Text("Detail", row.Rect, new Vector2(-110f, -14f), new Vector2(600f, 40f), 28f, TextAlignmentOptions.Left, Subtle);
            row.Detail.fontStyle = FontStyles.Normal;
            row.Detail.overflowMode = TextOverflowModes.Ellipsis;
            row.Detail.text = skill.Description;

            row.Progress = Box("Progress", row.Rect, new Vector2(-110f, -58f), new Vector2(600f, 16f), rounded, new Color(1f, 1f, 1f, 0.12f));
            row.ProgressFill = Box("Fill", row.Progress, Vector2.zero, Vector2.zero, rounded, Gold);
            row.ProgressFill.anchorMin = Vector2.zero;
            row.ProgressFill.anchorMax = new Vector2(0f, 1f);
            row.ProgressFill.pivot = new Vector2(0f, 0.5f);
            row.ProgressFill.offsetMin = row.ProgressFill.offsetMax = Vector2.zero;

            row.Pill = Box("Price", row.Rect, new Vector2(320f, 0f), new Vector2(200f, 84f), rounded, Gold).GetComponent<Image>();
            row.Price = Text("Text", (RectTransform)row.Pill.transform, Vector2.zero, new Vector2(200f, 84f), 32f, TextAlignmentOptions.Center, GoldInk);
            _rows.Add(row);
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
