using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Slider _healthBar;

        [Header("Boss Health (B18)")]
        [SerializeField] private GameObject _bossHealthContainer;
        [SerializeField] private Slider _bossHealthBar;

        [Header("Score & Timer")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _tierText;

        [Header("XP")]
        [SerializeField] private Slider _xpBar;
        [SerializeField] private TMP_Text _levelText;

        // Eski rozet kabı sahnede duruyor; rozetler kaldırıldı, kap gizlenir.
        [SerializeField] private Transform _skillBadgeContainer;
        private float _survivalTime;
        // 2026-09-06 (Serra): timer sadece görüntülenen saniye değeri değişince string oluştursun diye
        // (CLAUDE.md: "her frame string allocation... sadece değer değişince güncelle").
        private int _lastDisplayedSeconds = -1;
        // 2026-09-06 (Serra): OnXPChanged her yemede ateşleniyor (mass=XP) — FindAnyObjectByType'ı bu
        // sıklıkta çağırmak CLAUDE.md'nin kendi kuralına aykırıydı (Temmuz 2026'daki bir kod incelemesinde
        // de flag'lenip hiç düzeltilmemişti). OnCharacterSelected'da (run başına bir kere) cache'leniyor.
        private Entities.Blob.BlobGrowth _blobGrowth;

        private static readonly Color ShieldFillColor = new Color(0.55f, 0.82f, 1f, 1f); // geçici kalkan uzantısı
        private Entities.Blob.BlobHealth _blobHealth;

        // Yerleşim (hud.md §3.1, safe area üstünden ref px): A boss adı + barı, B süre/skor, C can/kalkan/XP, D slot şeridi.
        private const float BossNameHeight = 44f, BossBarTop = 50f, BossBarHeight = 24f;
        private const float HealthLeft = 84f;   // solda düşük can işaretine (48 + 12) yer
        private const float HealthTop = 216f, HealthHeight = 28f;
        // XP barı sabit boy: dik ekranda 1080 genişlikte eski hâli (sağdan 244 içeride), yan ekranda uzamaz
        // (can barının en uzun hâlinden biraz uzun). Tier adı barın hemen sağında.
        private const float XpTop = 296f, XpHeight = 20f, XpLeft = 172f, XpWidth = 1080f - XpLeft - 244f, XpTierGap = 20f;
        private const float RowTextTop = 286f, RowTextHeight = 40f, LevelTextWidth = 140f, TierTextWidth = 200f;
        private const float StripTop = 338f, StripSlot = 72f;

        private void Awake()
        {
            LayoutHud();
            LayoutBars();
            CreateBonusShield();
            CreateBossName();
            CreateSlotStrip();
            if (_bossHealthContainer != null)
                _bossHealthContainer.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += OnHealthChanged;
            GameEvents.OnScoreChanged += OnScoreChanged;
            GameEvents.OnCoinsChanged += OnCoinsChanged;
            GameEvents.OnSurvivalTimeUpdated += OnSurvivalTimeUpdated;
            GameEvents.OnBlobTierChanged += OnTierChanged;
            GameEvents.OnXPChanged += OnXPChanged;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnCharacterSelected += OnCharacterSelected;
            GameEvents.OnBossHealthChanged += OnBossHealthChanged;
            GameEvents.OnBossSpawned += OnBossSpawned;
            GameEvents.OnUpgradeApplied += OnUpgradeApplied;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= OnHealthChanged;
            GameEvents.OnScoreChanged -= OnScoreChanged;
            GameEvents.OnCoinsChanged -= OnCoinsChanged;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTimeUpdated;
            GameEvents.OnBlobTierChanged -= OnTierChanged;
            GameEvents.OnXPChanged -= OnXPChanged;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnCharacterSelected -= OnCharacterSelected;
            GameEvents.OnBossHealthChanged -= OnBossHealthChanged;
            GameEvents.OnBossSpawned -= OnBossSpawned;
            GameEvents.OnUpgradeApplied -= OnUpgradeApplied;
        }

        // --- Evrim yazısı: "★ KARA DELİK" ekranın üst ortasında belirir (pop), durur, söner. Ölçeksiz zaman. ---
        private static readonly Color EvolutionGold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color EvolutionShadow = new Color(0.12f, 0.02f, 0.2f, 0.9f);
        private const float BannerPop = 0.22f, BannerHold = 1.6f, BannerFade = 0.5f;
        private RectTransform _banner;
        private TMP_Text _bannerText, _bannerShadow;
        private float _bannerTime = -1f;

        private void OnUpgradeApplied(UpgradeData data, int level)
        {
            if (data == null || !data.IsEvolution) return;
            if (_banner == null) CreateBanner();
            if (_banner == null) return;
            string text = UiGlyphs.Star + " " + data.DisplayName.ToUpper(new System.Globalization.CultureInfo("tr-TR"));
            _bannerText.text = text;
            _bannerShadow.text = text;
            _banner.gameObject.SetActive(true);
            _bannerTime = 0f;
        }

        private void CreateBanner()
        {
            RectTransform root = CreateLayoutRoot();
            if (root == null) return;
            var go = new GameObject("EvolutionBanner", typeof(RectTransform));
            go.layer = gameObject.layer;
            _banner = (RectTransform)go.transform;
            _banner.SetParent(root, false);
            _banner.anchorMin = _banner.anchorMax = new Vector2(0.5f, 0.72f);
            _banner.sizeDelta = new Vector2(1000f, 140f);
            _bannerShadow = CreateText("Shadow", _banner, "");
            _bannerText = CreateText("Text", _banner, "");
            UiGlyphs.Apply(_bannerShadow);
            UiGlyphs.Apply(_bannerText);
            PlaceText(_bannerShadow, _banner, Vector2.zero, Vector2.one, new Vector2(0f, -6f), new Vector2(0f, -6f), 92f, TextAlignmentOptions.Center, EvolutionShadow);
            PlaceText(_bannerText, _banner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 92f, TextAlignmentOptions.Center, EvolutionGold);
            go.SetActive(false);
        }

        private void Update()
        {
            UpdateHealthWidth();
            UpdateBonusShield();
            if (_bannerTime < 0f || _banner == null) return;
            _bannerTime += Time.unscaledDeltaTime;
            float t = _bannerTime;
            float scale = t < BannerPop ? Mathf.Lerp(1.6f, 1f, t / BannerPop) : 1f;
            float alpha = t < BannerPop + BannerHold ? 1f : 1f - (t - BannerPop - BannerHold) / BannerFade;
            if (alpha <= 0f)
            {
                _bannerTime = -1f;
                _banner.gameObject.SetActive(false);
                return;
            }
            _banner.localScale = Vector3.one * scale;
            _bannerText.alpha = alpha;
            _bannerShadow.alpha = alpha * EvolutionShadow.a;
        }

        // --- Barlar: sahnedeki konum/boy yerine üst bloğa taşınır (bir kez). ---
        private void LayoutBars()
        {
            RectTransform layout = CreateLayoutRoot();
            if (layout == null) return;

            if (_healthBar != null)
            {
                _healthRect = (RectTransform)_healthBar.transform;
                _healthRect.SetParent(layout, false);
                _healthRect.anchorMin = _healthRect.anchorMax = _healthRect.pivot = new Vector2(0f, 1f); // sol kenar sabit, sağa uzar
                _healthRect.anchoredPosition = new Vector2(HealthLeft, -HealthTop);
                _healthRect.sizeDelta = new Vector2(HealthBaseWidth, HealthHeight);
                _healthRect.localScale = Vector3.one;
                _healthWidthTarget = HealthBaseWidth;
                NormalizeSlider(_healthBar);
            }

            if (_xpBar != null)
            {
                var xp = (RectTransform)_xpBar.transform;
                xp.SetParent(layout, false);
                xp.anchorMin = xp.anchorMax = xp.pivot = new Vector2(0f, 1f);
                xp.anchoredPosition = new Vector2(XpLeft, -XpTop);
                xp.sizeDelta = new Vector2(XpWidth, XpHeight);
                xp.localScale = Vector3.one;
                NormalizeSlider(_xpBar);
            }

            RectTransform boss = _bossHealthContainer != null ? _bossHealthContainer.transform as RectTransform
                : _bossHealthBar != null ? (RectTransform)_bossHealthBar.transform : null;
            if (boss != null)
            {
                boss.SetParent(layout, false);
                boss.anchorMin = new Vector2(0f, 1f);
                boss.anchorMax = new Vector2(1f, 1f);
                boss.pivot = new Vector2(0.5f, 1f);
                boss.offsetMin = new Vector2(Edge, -(BossBarTop + BossBarHeight));
                boss.offsetMax = new Vector2(-Edge, -BossBarTop);
                boss.localScale = Vector3.one;
            }
            if (_bossHealthBar != null) NormalizeSlider(_bossHealthBar);
        }

        // Varsayılan Slider'da zemin/dolgu barın orta yarısını kaplıyor, dolgu tutamak payı bırakıyor: bar tam boyu kullansın.
        private static void NormalizeSlider(Slider slider)
        {
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            if (slider.handleRect != null && slider.handleRect.parent != null && slider.handleRect.parent != slider.transform)
                slider.handleRect.parent.gameObject.SetActive(false);
            Transform background = slider.transform.Find("Background");
            if (background != null) StretchFull((RectTransform)background);
            RectTransform fill = slider.fillRect;
            if (fill != null)
            {
                if (fill.parent != slider.transform) StretchFull((RectTransform)fill.parent);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
            foreach (Graphic g in slider.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // Geçici kalkan (görev ödülü, Geri Dönüşüm): can barının sağ ucunda, can ölçeğinde mavi uzantı.
        // Olayı yok; değer değişince boyu yeniden hesaplanır (float karşılaştırması, ucuz).
        private RectTransform _bonusShield;
        private float _shownBonus = -1f, _shownBonusPx = -1f;

        private void CreateBonusShield()
        {
            if (_healthRect == null) return;
            var go = new GameObject("BonusShield", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            _bonusShield = (RectTransform)go.transform;
            _bonusShield.SetParent(_healthRect, false);
            _bonusShield.anchorMin = new Vector2(1f, 0f);
            _bonusShield.anchorMax = new Vector2(1f, 1f);
            _bonusShield.pivot = new Vector2(0f, 0.5f);
            _bonusShield.anchoredPosition = new Vector2(4f, 0f);
            _bonusShield.sizeDelta = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = ShieldFillColor;
            image.raycastTarget = false;
            go.SetActive(false);
        }

        private void UpdateBonusShield()
        {
            if (_bonusShield == null || _blobHealth == null) return;
            float bonus = _blobHealth.BonusShield;
            float pxPerHp = _healthRect.sizeDelta.x / Mathf.Max(1f, _blobHealth.MaxHealth);
            float px = Mathf.Round(bonus * pxPerHp);
            if (Mathf.Approximately(bonus, _shownBonus) && Mathf.Approximately(px, _shownBonusPx)) return;
            _shownBonus = bonus;
            _shownBonusPx = px;
            bool show = bonus > 0.01f;
            if (_bonusShield.gameObject.activeSelf != show) _bonusShield.gameObject.SetActive(show);
            if (show) _bonusShield.sizeDelta = new Vector2(Mathf.Max(6f, px), 0f);
        }

        // --- Boss: ad bar üstünde (A bölgesi); boss varken haber bandı gizlenir (bar onun yerini alır). ---
        private TMP_Text _bossName;
        private Transform _ticker;

        private void CreateBossName()
        {
            RectTransform layout = CreateLayoutRoot();
            if (layout == null) return;
            _bossName = CreateText("BossName", layout, "");
            PlaceText(_bossName, layout, new Vector2(0f, 1f), Vector2.one, new Vector2(Edge, -BossNameHeight), new Vector2(-Edge, 0f), 34f,
                TextAlignmentOptions.Center, Color.white);
            _bossName.gameObject.SetActive(false);
            // NewsTicker kendi CanvasGroup'unu kullanıyor: ona dokunmadan ölçekle gizlenir (geçici; asıl yeri NewsTicker).
            NewsTicker ticker = FindAnyObjectByType<NewsTicker>(FindObjectsInactive.Include);
            _ticker = ticker != null ? ticker.transform : null;
        }

        private void OnBossSpawned(string hudName, bool isFinal)
        {
            if (_bossName != null) _bossName.SetText(hudName ?? "");
        }

        // B18: max<=0 => aktif boss yok (A15/A16 bu sözleşmeyle RaiseBossHealthChanged çağırıyor).
        private void OnBossHealthChanged(float current, float max)
        {
            bool hasActiveBoss = max > 0f;

            if (_bossHealthContainer != null && _bossHealthContainer.activeSelf != hasActiveBoss)
            {
                _bossHealthContainer.SetActive(hasActiveBoss);
                if (_bossName != null) _bossName.gameObject.SetActive(hasActiveBoss);
                if (_ticker != null) _ticker.localScale = hasActiveBoss ? Vector3.zero : Vector3.one;
            }

            if (_bossHealthBar != null && hasActiveBoss)
            {
                _bossHealthBar.maxValue = max;
                _bossHealthBar.value = current;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (_healthBar != null)
            {
                _healthBar.maxValue = max;
                _healthBar.value = current;
            }
            // Max can arttıkça bar uzar (Can Artışı, Market): 100 can = temel boy, üst sınır ekran payı.
            if (_healthRect != null)
                _healthWidthTarget = Mathf.Clamp(HealthBaseWidth * max / HealthReferenceMax, HealthBaseWidth, HealthMaxWidth);
        }

        // --- Can barı boyu: sol kenar sabit, sağa uzar. ---
        private const float HealthReferenceMax = 100f;
        private const float HealthBaseWidth = 400f, HealthMaxWidth = 720f;
        private const float HealthGrowSpeed = 6f;
        private RectTransform _healthRect;
        private float _healthWidthTarget;

        private void UpdateHealthWidth()
        {
            if (_healthRect == null) return;
            Vector2 size = _healthRect.sizeDelta;
            if (Mathf.Abs(size.x - _healthWidthTarget) < 0.5f) return;
            size.x = Mathf.Lerp(size.x, _healthWidthTarget, 1f - Mathf.Exp(-HealthGrowSpeed * Time.unscaledDeltaTime));
            _healthRect.sizeDelta = size;
        }

        private void OnScoreChanged(int score)
        {
            if (_scoreText == null) return;
            _scoreText.text = score.ToString("N0");
            if (_scoreText.text.Length != _scoreLength) { _scoreLength = _scoreText.text.Length; FitScorePanel(); }
        }

        private void OnCoinsChanged(int coins)
        {
            if (_coinText == null) return;
            _coinText.text = $"{coins:N0} coin";
            if (_coinText.text.Length != _coinLength) { _coinLength = _coinText.text.Length; FitScorePanel(); }
        }

        // Skor/coin kutusu metne göre genişler (sağ kenar sabit); yalnız hane sayısı değişince ölçülür.
        private const float ScorePanelMinWidth = 240f, ScorePanelPadding = 16f;
        private RectTransform _scorePanel;
        private int _scoreLength = -1, _coinLength = -1;

        private void FitScorePanel()
        {
            if (_scorePanel == null) return;
            float width = 0f;
            if (_scoreText != null) width = Mathf.Max(width, _scoreText.preferredWidth);
            if (_coinText != null) width = Mathf.Max(width, _coinText.preferredWidth);
            Vector2 size = _scorePanel.sizeDelta;
            size.x = Mathf.Max(ScorePanelMinWidth, width + ScorePanelPadding * 2f);
            _scorePanel.sizeDelta = size;
        }

        private void OnSurvivalTimeUpdated(float time)
        {
            _survivalTime = time;
            if (_timerText == null) return;

            int totalSeconds = (int)time;
            if (totalSeconds == _lastDisplayedSeconds) return;
            _lastDisplayedSeconds = totalSeconds;

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void OnTierChanged(BlobTier tier)
        {
            if (_tierText != null)
                _tierText.text = TierNames.Get(tier);
        }

        private void OnXPChanged(int xp)
        {
            if (_xpBar == null || _blobGrowth == null) return;
            _xpBar.maxValue = _blobGrowth.XPThreshold;
            _xpBar.value = _blobGrowth.CurrentXP;
        }

        private void OnLevelUp(int level)
        {
            if (_levelText != null)
                _levelText.SetText("Sv. {0}", level);
        }

        private void OnCharacterSelected(CharacterData data)
        {
            _blobGrowth = FindAnyObjectByType<Entities.Blob.BlobGrowth>();

            _blobHealth = _blobGrowth != null ? _blobGrowth.GetComponent<Entities.Blob.BlobHealth>() : null;

            // Restart sahneyi yüklemez: seviye/tier/XP yazıları yeni run'ın değerleriyle başlar.
            OnLevelUp(_blobGrowth != null ? _blobGrowth.CurrentLevel : 0);
            OnTierChanged(GameEvents.CurrentBlobTier);
            OnXPChanged(0);
            if (_banner != null) { _banner.gameObject.SetActive(false); _bannerTime = -1f; } // yarıda kalan evrim yazısı
        }

        // --- Slot şeridi (K19): D bölgesi, sol hizalı. Kendi olaylarını dinler. ---
        private SkillSlotStrip _slotStrip;

        private void CreateSlotStrip()
        {
            RectTransform layout = CreateLayoutRoot();
            if (layout == null) return;
            Sprite rounded = null;
            Transform background = _healthBar != null ? _healthBar.transform.Find("Background") : null;
            Image bgImage = background != null ? background.GetComponent<Image>() : null;
            if (bgImage != null) rounded = bgImage.sprite;
            _slotStrip = SkillSlotStrip.Create(layout, "SlotStrip", StripSlot, rounded);
            var rect = (RectTransform)_slotStrip.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Edge, -StripTop);
        }

        // Yazılar ve barlar çalışma zamanında yerleşir (sahne kurulumu yok). Hepsi HUD'un altında: lobi HUD'u kapatınca onlar da kapanır.
        private static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.17f, 0.55f);
        private static readonly Color CoinColor = new Color(1f, 0.82f, 0.25f, 1f);
        private const float TopOffset = 104f; // A bölgesinin (haber bandı / boss barı) altı
        private const float Edge = 24f;
        // Sağ üst köşe duraklatma tuşunun (PauseMenu, 88 + 24 boşluk): skor kutusu onun solunda.
        public const float PauseReserve = 88f + 24f;
        public const float ScorePanelTop = TopOffset, ScorePanelHeight = 104f;

        private void LayoutHud()
        {
            if (_skillBadgeContainer != null) _skillBadgeContainer.gameObject.SetActive(false);

            RectTransform layout = CreateLayoutRoot();
            if (layout == null) return;

            RectTransform timerPanel = CreatePanel("TimerPanel", layout, new Vector2(0f, 1f), new Vector2(Edge, -TopOffset), new Vector2(190f, 72f));
            PlaceText(_timerText, timerPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 50f, TextAlignmentOptions.Center, Color.white);

            RectTransform scorePanel = CreatePanel("ScorePanel", layout, new Vector2(1f, 1f), new Vector2(-Edge - PauseReserve, -ScorePanelTop),
                new Vector2(ScorePanelMinWidth, ScorePanelHeight));
            _scorePanel = scorePanel;
            PlaceText(_scoreText, scorePanel, new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -60f), new Vector2(-16f, -6f), 44f,
                TextAlignmentOptions.Right, Color.white);
            if (_coinText == null) _coinText = CreateText("CoinText", scorePanel, "0 coin");
            PlaceText(_coinText, scorePanel, Vector2.zero, new Vector2(1f, 0f), new Vector2(16f, 6f), new Vector2(-16f, 46f), 32f,
                TextAlignmentOptions.Right, CoinColor);

            // C bölgesi XP satırı: solda "Sv. N", sağda tier adı (XP barı aralarında).
            PlaceText(_levelText, layout, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(Edge, -(RowTextTop + RowTextHeight)), new Vector2(Edge + LevelTextWidth, -RowTextTop), 32f,
                TextAlignmentOptions.Left, Color.white);
            float tierLeft = XpLeft + XpWidth + XpTierGap;
            PlaceText(_tierText, layout, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(tierLeft, -(RowTextTop + RowTextHeight)), new Vector2(tierLeft + TierTextWidth, -RowTextTop), 32f,
                TextAlignmentOptions.Right, Color.white);
        }

        // HUD'un kendi rect'i sahnede daraltılmış; yazılar ve barlar için güvenli alanı kaplayan bir child açılır —
        // HUD'un daralma/kaymasının tersi uygulanır.
        private RectTransform CreateLayoutRoot()
        {
            RectTransform hud = transform as RectTransform;
            if (hud == null) return null;

            Transform existing = hud.Find("HUDLayout");
            if (existing != null) return existing as RectTransform;

            GameObject go = new GameObject("HUDLayout", typeof(RectTransform));
            go.layer = gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(hud, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = -hud.sizeDelta;
            rect.anchoredPosition = -hud.anchoredPosition;
            return rect;
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 corner, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = PanelColor;
            image.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, string text)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            TMP_Text label = go.GetComponent<TMP_Text>();
            label.text = text;
            return label;
        }

        private static void PlaceText(TMP_Text text, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            if (text == null) return;
            RectTransform rect = text.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
        }
    }
}
