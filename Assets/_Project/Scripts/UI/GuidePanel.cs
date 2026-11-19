using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;
using BlobSurvivor.Systems.Meta;
using M = BlobSurvivor.UI.MarketPanel;

namespace BlobSurvivor.UI
{
    // Giriş ekranındaki REHBER: oyunun minik wikisi. Üç sekme: KARAKTERLER (başlangıç/favori skill, hedef evrim,
    // kilit), SKİLLER (her seviye, kilit koşulu + ilerleme, evrim eşi) ve EVRİMLER (tarifler). Market'in görsel
    // dilini ve yardımcılarını kullanır. Sayfalar her açılışta yeniden yazılır (kilit ilerlemesi run'lar arası değişir).
    public class GuidePanel : MonoBehaviour
    {
        private const float Pad = 34f, IconSize = 150f, TitleHeight = 60f, BodySize = 28f, BarHeight = 16f;
        private static readonly Color StarColor = new Color(1f, 0.82f, 0.25f, 1f);
        private const string GoldHex = "#FFD140", GreenHex = "#59D173", FaintHex = "#FFFFFFAA";

        private RectTransform _panel;
        private RectTransform[] _pages;
        private RectTransform[] _contents;
        private Image[] _tabs;
        private TMP_Text[] _tabTexts;
        private int _tab;
        private Sprite _rounded;
        private CharacterData[] _characters;
        private System.Action _onBack;

        public static GuidePanel Create(RectTransform parent, Sprite rounded, CharacterData[] characters, System.Action onBack)
        {
            var go = new GameObject("GuidePanel", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            GuidePanel guide = go.AddComponent<GuidePanel>(); // menü kurulumu, bir kez
            guide._rounded = rounded;
            guide._characters = characters;
            guide._onBack = onBack;
            guide.Build();
            go.SetActive(false);
            return guide;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            FillCharacters(_contents[0]);
            FillSkills(_contents[1]);
            FillEvolutions(_contents[2]);
            ShowTab(_tab);
        }

        private void Back()
        {
            gameObject.SetActive(false);
            _onBack?.Invoke();
        }

        // ---------------------------------------------------------------- iskelet (bir kez)

        private void Build()
        {
            // Yazılar güvenli alanda; arka plan tüm ekranı kaplar.
            _panel = M.NewRect("Safe", (RectTransform)transform, Vector2.zero, Vector2.zero);
            Rect area = Screen.safeArea;
            _panel.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            _panel.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            _panel.offsetMin = _panel.offsetMax = Vector2.zero;
            var blocker = _panel.gameObject.AddComponent<Image>();
            blocker.color = Color.clear; // arkadaki giriş ekranına dokunulmasın
            LobbyPanel.ScreenBackdrop(_panel, M.Backdrop);

            M.Shadowed("Title", _panel, new Vector2(0f, 760f), new Vector2(1000f, 120f), "REHBER", 92f, M.Gold);
            RectTransform back = M.Box("BackButton", _panel, new Vector2(-420f, 760f), new Vector2(120f, 100f), _rounded, M.CardColor);
            M.MakeButton(back, Back);
            M.Text("Label", back, Vector2.zero, back.sizeDelta, 56f, TextAlignmentOptions.Center, Color.white).text = "<";

            string[] tabNames = { "KARAKTERLER", "SKİLLER", "EVRİMLER" };
            RectTransform tabs = M.NewRect("Tabs", _panel, new Vector2(0f, 540f), new Vector2(900f, 96f));
            _tabs = new Image[tabNames.Length];
            _tabTexts = new TMP_Text[tabNames.Length];
            const float tabWidth = 292f;
            for (int i = 0; i < tabNames.Length; i++)
            {
                int index = i;
                RectTransform tab = M.Box("Tab_" + i, tabs, new Vector2((i - 1) * (tabWidth + 12f), 0f), new Vector2(tabWidth, 96f), _rounded, M.CardColor);
                M.MakeButton(tab, () => ShowTab(index));
                _tabs[i] = tab.GetComponent<Image>();
                _tabTexts[i] = M.Text("Label", tab, Vector2.zero, tab.sizeDelta, 36f, TextAlignmentOptions.Center, Color.white);
                _tabTexts[i].text = tabNames[i];
            }

            _pages = new RectTransform[tabNames.Length];
            _contents = new RectTransform[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
                _pages[i] = M.BuildScrollPage("Page_" + i, _panel, out _contents[i]);

            LobbyPanel.PinToEdge(_panel, 1f, "Title", "TitleShadow", "BackButton", "Tabs");
        }

        private void ShowTab(int tab)
        {
            _tab = tab;
            for (int i = 0; i < _pages.Length; i++)
            {
                bool on = i == tab;
                _pages[i].gameObject.SetActive(on);
                _tabs[i].color = on ? M.Gold : M.CardColor;
                _tabTexts[i].color = on ? M.GoldInk : Color.white;
                if (on) _contents[i].anchoredPosition = Vector2.zero; // sekme değişince başa
            }
        }

        // ---------------------------------------------------------------- sayfalar

        private void FillCharacters(RectTransform content)
        {
            var layout = Begin(content);
            MetaProgression meta = MetaProgression.Instance;
            if (_characters == null) { layout.Finish(); return; }
            var sb = new StringBuilder();
            foreach (CharacterData c in _characters)
            {
                if (c == null) continue;
                bool unlocked = meta == null || meta.IsCharacterUnlocked(c);
                sb.Clear();
                sb.Append(c.Description);
                if (c.StartingUpgrade != null)
                    sb.Append($"\n<color={GoldHex}>Başlangıç:</color> {c.StartingUpgrade.DisplayName} Sv.{c.StartingUpgradeLevel}");
                if (c.FavoredUpgrade != null)
                    sb.Append($"\n<color={GoldHex}>Favori:</color> {c.FavoredUpgrade.DisplayName} (kartta {c.FavoredWeightMultiplier:0} kat sık)");
                SkillEvolutionData evo = EvolutionOf(c.FavoredUpgrade);
                if (evo != null)
                    sb.Append($"\n<color={GoldHex}>{UiGlyphs.Star} Hedef evrim:</color> {evo.Output.DisplayName} ({Recipe(evo)})");

                float progress = -1f;
                if (!unlocked)
                {
                    if (c.MarketPrice > 0 && !meta.IsCharacterPurchased(c))
                        sb.Append($"\n<color={GoldHex}>Market'te {c.MarketPrice:N0} coin</color>");
                    else if (c.RequiredCompletedRuns > 0)
                    {
                        int done = c.RequiredCompletedRuns - meta.CharacterRunsLeft(c);
                        sb.Append($"\n<color={GoldHex}>{c.RequiredCompletedRuns} run bitir ({done}/{c.RequiredCompletedRuns}). Ölüm de sayılır.</color>");
                        progress = (float)done / c.RequiredCompletedRuns;
                    }
                }
                Add(layout, Card(content, c.DisplayName, Color.white, unlocked ? "AÇIK" : "KİLİTLİ",
                    unlocked ? M.Owned : M.Faint, sb.ToString(), c.Icon, progress, !unlocked));
            }
            layout.Finish();
        }

        private void FillSkills(RectTransform content)
        {
            var layout = Begin(content);
            UpgradeData[] all = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.AllUpgrades : null;
            if (all == null) { layout.Finish(); return; }
            Add(layout, Note(content,
                "Seviye atlayınca 3 karttan birini seçersin. En fazla 3 aktif + 3 pasif skill; her biri en fazla Sv.5. Bazı seviyeler yeni yetenek açar."));

            SkillKind[] order = { SkillKind.Active, SkillKind.Passive, SkillKind.Rare };
            string[] headers = { "AKTİF", "PASİF", "NADİR" };
            var sb = new StringBuilder();
            for (int k = 0; k < order.Length; k++)
            {
                layout.Add(M.SectionLabel(content, headers[k]), M.HeaderHeight);
                foreach (UpgradeData u in all)
                {
                    if (u == null || u.IsEvolution || u.Kind != order[k]) continue;
                    sb.Clear();
                    if (u.MaxLevel <= 1) sb.Append(u.Description);
                    else
                        for (int level = 1; level <= u.MaxLevel; level++)
                        {
                            if (level > 1) sb.Append('\n');
                            sb.Append($"<color={GoldHex}>Sv.{level}</color>  {u.GetDescriptionForLevel(level)}");
                        }
                    if (u.IsBaseWeapon) sb.Append($"\n<color={FaintHex}>Herkeste baştan var (Sv.0).</color>");

                    SkillEvolutionData evo = EvolutionOf(u);
                    if (evo != null)
                        sb.Append($"\n<color={GoldHex}>{UiGlyphs.Star} Evrim: {evo.Output.DisplayName}</color> ({Recipe(evo)})");

                    float progress = -1f;
                    bool locked = false;
                    MetaProgression meta = MetaProgression.Instance;
                    if (meta != null && SkillUnlocks.TryGet(u.Id, out SkillUnlocks.Entry entry))
                    {
                        if (meta.IsSkillUnlocked(u.Id)) sb.Append($"\n<color={GreenHex}>Açıldı: {entry.Text}</color>");
                        else
                        {
                            locked = true;
                            int value = meta.GetStat(entry.Stat);
                            string p = SkillUnlocks.ProgressText(entry, value);
                            sb.Append($"\n<color={GoldHex}>Kilitli: {entry.Text}{(p.Length > 0 ? " · " + p : "")}</color>");
                            if (entry.Target > 1) progress = Mathf.Clamp01((float)value / entry.Target);
                        }
                    }
                    Color kindColor = u.Kind == SkillKind.Active ? M.ActiveColor : u.Kind == SkillKind.Rare ? M.RareColor : M.PassiveColor;
                    Add(layout, Card(content, u.DisplayName, Color.white, locked ? "KİLİTLİ" : headers[k], locked ? M.Faint : kindColor,
                        sb.ToString(), null, progress, locked));
                }
            }
            layout.Finish();
        }

        private void FillEvolutions(RectTransform content)
        {
            var layout = Begin(content);
            Add(layout, Note(content,
                "Aktif skill'i MAX yap, eşini al: evrim kendiliğinden gerçekleşir. Slot harcamaz. Kartlarda altın yıldızlı not, bir skill'in evrim eşi olduğunu söyler."));
            IReadOnlyList<SkillEvolutionData> evos = EvolutionSystem.Instance != null ? EvolutionSystem.Instance.Evolutions : null;
            if (evos != null)
                foreach (SkillEvolutionData evo in evos)
                {
                    if (evo == null || evo.Output == null || evo.InputA == null || evo.InputB == null) continue;
                    string body = $"<color={GoldHex}>{Recipe(evo)}</color>\n{evo.Output.Description}";
                    string who = CharacterFor(evo);
                    if (who != null) body += $"\n<color={FaintHex}>En kolay: {who}</color>";
                    Add(layout, Card(content, UiGlyphs.Star + " " + evo.Output.DisplayName, StarColor, "EVRİM", M.Gold, body, null, -1f, false));
                }
            layout.Finish();
        }

        // ---------------------------------------------------------------- yardımcılar

        private static SkillEvolutionData EvolutionOf(UpgradeData skill)
        {
            if (skill == null || EvolutionSystem.Instance == null) return null;
            foreach (SkillEvolutionData evo in EvolutionSystem.Instance.Evolutions)
                if (evo != null && evo.Output != null && (evo.InputA == skill || evo.InputB == skill)) return evo;
            return null;
        }

        private static string Recipe(SkillEvolutionData evo)
        {
            int b = evo.InputBRequiredLevel;
            string bLevel = b >= evo.InputB.MaxLevel ? "MAX" : b <= 1 ? "herhangi seviye" : $"Sv.{b}+";
            return $"{evo.InputA.DisplayName} MAX + {evo.InputB.DisplayName} {bLevel}";
        }

        private string CharacterFor(SkillEvolutionData evo)
        {
            if (_characters == null) return null;
            foreach (CharacterData c in _characters)
                if (c != null && (c.FavoredUpgrade == evo.InputA || c.FavoredUpgrade == evo.InputB)) return c.DisplayName;
            return null;
        }

        private static void Add(M.ListLayout layout, RectTransform rect) => layout.Add(rect, rect.sizeDelta.y);

        private static M.ListLayout Begin(RectTransform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject); // menüde, açılışta
            return new M.ListLayout(content);
        }

        private RectTransform Note(RectTransform content, string text)
        {
            TMP_Text t = M.Text("Note", content, Vector2.zero, new Vector2(M.RowWidth, 10f), BodySize, TextAlignmentOptions.TopLeft, M.Subtle);
            t.fontStyle = FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            UiGlyphs.Apply(t);
            t.text = text;
            float h = t.GetPreferredValues(text, M.RowWidth, 0f).y;
            t.rectTransform.sizeDelta = new Vector2(M.RowWidth, h);
            return t.rectTransform;
        }

        // Yükseklik içeriğe göre: başlık + sarılan gövde (+ ilerleme çubuğu). ListLayout bu kartın boyunu okur.
        private RectTransform Card(RectTransform content, string title, Color titleColor, string tag, Color tagColor,
            string body, Sprite icon, float progress, bool dim)
        {
            float left = icon != null ? Pad + IconSize + 24f : Pad;
            float textWidth = M.RowWidth - left - Pad;

            TMP_Text bodyText = M.Text("Body", content, Vector2.zero, new Vector2(textWidth, 10f), BodySize, TextAlignmentOptions.TopLeft, M.Subtle);
            bodyText.fontStyle = FontStyles.Normal;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.lineSpacing = 4f;
            UiGlyphs.Apply(bodyText);
            bodyText.text = body;
            float bodyHeight = bodyText.GetPreferredValues(body, textWidth, 0f).y;

            float height = Pad + TitleHeight + 8f + bodyHeight + (progress >= 0f ? 18f + BarHeight : 0f) + Pad;
            if (icon != null) height = Mathf.Max(height, IconSize + Pad * 2f);

            RectTransform card = M.Box("Card", content, Vector2.zero, new Vector2(M.RowWidth, height), _rounded, M.CardColor);
            TopLeft(bodyText.rectTransform, card, left, Pad + TitleHeight + 8f, textWidth, bodyHeight);

            TMP_Text titleText = M.Text("Title", card, Vector2.zero, Vector2.zero, 44f, TextAlignmentOptions.Left, titleColor);
            UiGlyphs.Apply(titleText);
            titleText.text = title;
            TopLeft(titleText.rectTransform, card, left, Pad, textWidth - 200f, TitleHeight);

            TMP_Text tagText = M.Text("Tag", card, Vector2.zero, Vector2.zero, 28f, TextAlignmentOptions.Right, tagColor);
            tagText.text = tag;
            TopLeft(tagText.rectTransform, card, M.RowWidth - Pad - 220f, Pad, 220f, TitleHeight);

            if (icon != null)
            {
                RectTransform frame = M.Box("Portrait", card, Vector2.zero, Vector2.zero, _rounded, new Color(1f, 1f, 1f, 0.06f));
                TopLeft(frame, card, Pad, Pad, IconSize, IconSize);
                var img = M.NewRect("Icon", frame, Vector2.zero, new Vector2(IconSize - 12f, IconSize - 12f)).gameObject.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                if (dim) img.color = new Color(1f, 1f, 1f, 0.45f);
            }

            if (progress >= 0f)
            {
                RectTransform bar = M.Box("Progress", card, Vector2.zero, Vector2.zero, _rounded, new Color(1f, 1f, 1f, 0.12f));
                TopLeft(bar, card, left, height - Pad - BarHeight, textWidth, BarHeight);
                RectTransform fill = M.Box("Fill", bar, Vector2.zero, Vector2.zero, _rounded, M.Gold);
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
            return card;
        }

        private static void TopLeft(RectTransform rect, RectTransform parent, float x, float y, float width, float height)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
