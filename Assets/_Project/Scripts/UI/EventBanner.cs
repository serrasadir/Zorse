using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // K20 olay bandı, P0-d sade (hud.md §4.2, §8): tek band, başlık + alt satır, pop 0.22 + tutma + 0.4 sönme.
    // Yeni olay mevcudun yerine geçer (kuyruk/birleştirme P1). Playing dışında donar. Zemin yok, yalnız metin + gölge.
    public class EventBanner : MonoBehaviour
    {
        private const float Pop = 0.22f, PopScale = 1.4f, FadeTime = 0.4f;
        private const float HoldTier = 1.6f, HoldBoss = 1.6f, HoldFinal = 2.0f;
        private const float TitleSize = 72f, TitleMin = 56f, SubSize = 36f, SubMin = 32f, SubOffset = 70f;
        private const float SubAlpha = 0.9f;

        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);
        private static readonly Color TierColor = Color.white; // spec renk vermiyor
        private static readonly Color BossColor = new Color(1f, 0.42f, 0.38f);
        private static readonly Color FinalColor = new Color(1f, 0.82f, 0.25f);
        private static readonly Vector2 ShadowOffset = new Vector2(0f, -6f);

        private const string FinalTitle = "HELİKOPTER GELDİ!";
        private const string FinalSub = "Uzan ve yut!";
        private const string BossSub = "Silahla indir";
        private const string EncircleTitle = "KUŞATILDIN!";
        private const string EncircleSub = "Yararak çık!";

        // (int)BlobTier ile indekslenir (1..7); metinler bir kez kurulur.
        private static readonly string[] TierTitles = BuildTierTitles();
        private static readonly string[] TierSubs =
        {
            "", "",
            "Artık yayaları yiyebilirsin",
            "İri yayalar ve seyyar arabalar menüde",
            "Polisler ve arabalar menüde",
            "Otobüsler menüde",
            "Elit polis ve kafeler menüde",
            "Bütün şehir menüde",
        };

        // Arayüz kiti: başlığın arkasında düz kurdele/hap (başlık boyuna göre, yalnız Show'da ölçülür), konturlu beyaz yazı.
        private const string RibbonGold = "Banners/ribbon_gold", RibbonRed = "Banners/ribbon_red";
        private const string PillMission = "Pills/pill_blue", PillDone = "Pills/pill_green";
        private const float RibbonHeight = 150f, RibbonMin = 480f, RibbonMax = 1060f, RibbonPad = 40f;
        private const float KitSubOffset = 104f;
        private bool _k;
        private Image _ribbon;

        private GameObject _hud;
        private RectTransform _band;
        private TMP_Text _title, _titleShadow, _sub;
        private float _time = -1f, _hold;
        private BlobTier _prevTier = BlobTier.Tiny;

        private static string[] BuildTierTitles()
        {
            var titles = new string[8];
            for (int i = 1; i < titles.Length; i++)
                titles[i] = "<sprite name=\"yukari\" tint=1> " + TierNames.Get((BlobTier)i) + "!";
            return titles;
        }

        internal void Init(GameObject hud, RectTransform layer)
        {
            _hud = hud;
            var safeGo = new GameObject("BannerSafeArea", typeof(RectTransform));
            safeGo.layer = layer.gameObject.layer;
            var safe = (RectTransform)safeGo.transform;
            safe.SetParent(layer, false);
            HudFeedbackLayer.Stretch(safe);
            safeGo.AddComponent<SafeAreaHandler>();

            var bandGo = new GameObject("EventBanner", typeof(RectTransform));
            bandGo.layer = safeGo.layer;
            _band = (RectTransform)bandGo.transform;
            _band.SetParent(safe, false);
            _band.anchorMin = _band.anchorMax = new Vector2(0.5f, 0.72f);
            _band.pivot = new Vector2(0.5f, 0.5f);
            _band.sizeDelta = new Vector2(1000f, 140f);

            TMP_SpriteAsset glyphs = Resources.Load<TMP_SpriteAsset>("Sprite Assets/UI_Glyphs");
            _titleShadow = MakeText("TitleShadow", ShadowOffset, 120f, TitleSize, TitleMin, FontStyles.Bold, ShadowColor);
            _title = MakeText("Title", Vector2.zero, 120f, TitleSize, TitleMin, FontStyles.Bold, TierColor);
            _sub = MakeText("Subtitle", new Vector2(0f, -SubOffset), 56f, SubSize, SubMin, FontStyles.Normal, Color.white);
            _titleShadow.spriteAsset = glyphs;
            _title.spriteAsset = glyphs;

            _k = UiKit.Ready;
            if (_k)
            {
                _ribbon = UiKit.NewImage("Ribbon", _band, Vector2.zero, new Vector2(RibbonMin, RibbonHeight), RibbonGold);
                _ribbon.rectTransform.SetAsFirstSibling();
                _titleShadow.gameObject.SetActive(false);
                UiKit.Outlined(_title, Color.white, true);
                UiKit.Outlined(_sub, Color.white, false, false);
                _sub.rectTransform.anchoredPosition = new Vector2(0f, -KitSubOffset);
            }

            bandGo.SetActive(false);
        }

        private TMP_Text MakeText(string name, Vector2 pos, float height, float size, float min, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = _band.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(_band, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(1000f, height);
            var text = go.GetComponent<TMP_Text>();
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = min;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void OnEnable()
        {
            GameEvents.OnBlobTierChanged += OnTierChanged;
            GameEvents.OnBossSpawned += OnBossSpawned;
            GameEvents.OnEncirclement += OnEncirclement;
            GameEvents.OnWaveStarted += OnWaveStarted;
            GameEvents.OnMissionStarted += OnMissionStarted;
            GameEvents.OnMissionCompleted += OnMissionCompleted;
            GameEvents.OnSkillUnlocked += OnSkillUnlocked;
            GameEvents.OnTransformation += OnTransformation;
            GameEvents.OnCharacterSelected += OnRunStarted;
            GameEvents.OnGameOver += Hide;
            GameEvents.OnRunComplete += OnRunComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnBlobTierChanged -= OnTierChanged;
            GameEvents.OnBossSpawned -= OnBossSpawned;
            GameEvents.OnEncirclement -= OnEncirclement;
            GameEvents.OnWaveStarted -= OnWaveStarted;
            GameEvents.OnMissionStarted -= OnMissionStarted;
            GameEvents.OnMissionCompleted -= OnMissionCompleted;
            GameEvents.OnSkillUnlocked -= OnSkillUnlocked;
            GameEvents.OnTransformation -= OnTransformation;
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnGameOver -= Hide;
            GameEvents.OnRunComplete -= OnRunComplete;
        }

        private void OnRunStarted(CharacterData _)
        {
            Hide();
            _prevTier = GameEvents.CurrentBlobTier;
        }

        private void OnRunComplete(RunEndReason _) => Hide();

        private void OnTierChanged(BlobTier tier)
        {
            BlobTier prev = _prevTier;
            _prevTier = tier;
            if (TierLayers.SlowdownRank(tier) <= TierLayers.SlowdownRank(prev)) return; // düşme bandı P1
            if (!IsPlaying()) return;
            int i = (int)tier;
            if (i <= 0 || i >= TierTitles.Length) return;
            Show(TierTitles[i], TierSubs[i], TierColor, HoldTier);
            Haptics.Impact(1);
        }

        private void OnBossSpawned(string hudName, bool isFinal)
        {
            if (isFinal) Show(FinalTitle, FinalSub, FinalColor, HoldFinal);
            else if (!string.IsNullOrEmpty(hudName)) Show(hudName + " GELDİ!", BossSub, BossColor, HoldBoss, RibbonRed);
            else return;
            Haptics.Impact(2);
        }

        // İlk düşmanlar (bekçi) geldiğinde ne olduklarını söyle.
        private void OnWaveStarted(string waveName)
        {
            if (!IsPlaying() || waveName != "Guard") return;
            Show("BEKÇİLER GELDİ!", "Seni durdurmak istiyorlar · yakala, yut!", BossColor, HoldBoss, RibbonRed);
        }

        private void OnEncirclement()
        {
            if (!IsPlaying()) return;
            Show(EncircleTitle, EncircleSub, BossColor, HoldBoss, RibbonRed);
            Haptics.Impact(2);
        }

        private static readonly Color MissionColor = new Color(0.45f, 0.9f, 1f);
        private static readonly Color MissionDoneColor = new Color(0.55f, 1f, 0.55f);

        private void OnMissionStarted(string title, string sub)
        {
            if (!IsPlaying()) return;
            Show(title, sub, MissionColor, HoldBoss, PillMission);
            Haptics.Impact(1);
        }

        private static readonly Color UnlockColor = new Color(0.78f, 0.52f, 1f);

        private void OnTransformation(string title, string sub)
        {
            if (!IsPlaying()) return;
            Show(title, sub, FinalColor, HoldFinal);
        }

        private void OnSkillUnlocked(string name)
        {
            if (!IsPlaying()) return;
            Show("YENİ SKİLL!", $"{name} açıldı · artık kartlarda", UnlockColor, HoldFinal);
            Haptics.Impact(2);
        }

        private void OnMissionCompleted(string reward)
        {
            if (!IsPlaying()) return;
            Show("GÖREV TAMAM!", reward, MissionDoneColor, HoldBoss, PillDone);
            Haptics.Impact(2);
        }

        private void Show(string title, string sub, Color color, float hold, string ribbon = RibbonGold)
        {
            if (_band == null) return;
            _title.text = title;
            _titleShadow.text = title;
            _title.color = _k ? Color.white : color;
            if (_k) FitRibbon(title, ribbon);
            bool hasSub = !string.IsNullOrEmpty(sub);
            _sub.gameObject.SetActive(hasSub);
            if (hasSub) _sub.text = sub;
            _band.gameObject.SetActive(true);
            _time = 0f;
            _hold = hold;
            Apply();
        }

        // Kurdele başlığın tercih edilen genişliğine göre; başlık kurdelenin yüzüne sığacak kadar daralır (otomatik boy).
        private void FitRibbon(string title, string key)
        {
            if (_ribbon == null) return;
            UiKit.Paint(_ribbon, key);
            Sprite s = _ribbon.sprite;
            float ends = s != null ? s.border.x * RibbonHeight / s.rect.height : 0f;
            _title.fontSize = TitleSize;
            float pref = _title.GetPreferredValues(title, 2000f, 120f).x;
            float width = Mathf.Clamp(pref + ends * 1.6f + RibbonPad, RibbonMin, RibbonMax);
            bool isRibbon = key.StartsWith("Banners/", System.StringComparison.Ordinal);
            RectTransform r = _ribbon.rectTransform;
            r.sizeDelta = new Vector2(width, RibbonHeight);
            r.anchoredPosition = new Vector2(0f, isRibbon ? -RibbonHeight * 0.075f : 0f); // kurdelenin yüzü merkezden biraz yukarıda
            if (s != null) _ribbon.pixelsPerUnitMultiplier = s.rect.height / RibbonHeight;
            _title.rectTransform.sizeDelta = new Vector2(Mathf.Min(1000f, width - ends * 1.4f), 120f);
        }

        private void Hide()
        {
            _time = -1f;
            if (_band != null) _band.gameObject.SetActive(false);
        }

        private static bool IsPlaying() => GameManager.Instance != null && GameManager.Instance.IsPlaying;

        private void Update()
        {
            if (_time < 0f) return;
            GameManager gm = GameManager.Instance;
            if ((_hud != null && !_hud.activeInHierarchy) || (gm != null && gm.CurrentState == GameState.Menu)) { Hide(); return; }
            // Kart ekranında bant gizlenir (kurdelesi kart başlığıyla üst üste binmesin), seçimden sonra kaldığı yerden sürer.
            bool levelUp = gm != null && gm.CurrentState == GameState.LevelUp;
            if (_band.gameObject.activeSelf == levelUp) _band.gameObject.SetActive(!levelUp);
            if (gm != null && gm.CurrentState != GameState.Playing) return; // kart ekranı/duraklatmada donar
            _time += Time.unscaledDeltaTime;
            if (_time >= Pop + _hold + FadeTime) { Hide(); return; }
            Apply();
        }

        private void Apply()
        {
            float t = _time;
            float scale = t < Pop ? Mathf.Lerp(PopScale, 1f, t / Pop) : 1f;
            float alpha = t < Pop + _hold ? 1f : 1f - (t - Pop - _hold) / FadeTime;
            _band.localScale = new Vector3(scale, scale, 1f);
            _title.alpha = alpha;
            _titleShadow.alpha = alpha * ShadowColor.a;
            if (_ribbon != null) _ribbon.color = new Color(1f, 1f, 1f, alpha);
            _sub.alpha = alpha * SubAlpha;
        }
    }
}
