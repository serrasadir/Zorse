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

        // 2026-11-04: Zırh kalkanı barı — can barının hemen altında ince mavi bar, çalışma zamanında can barından
        // kopyalanır (sahne kurulumu yok). Kalkan yokken (Zırh alınmamış) gizli.
        private static readonly Color ShieldFillColor = new Color(0.55f, 0.82f, 1f, 1f);
        private static readonly Color ShieldBackgroundColor = new Color(0.08f, 0.1f, 0.2f, 0.7f);
        private Slider _shieldBar;
        private Entities.Blob.BlobHealth _blobHealth;

        private void Awake()
        {
            LayoutHud();
            CreateShieldBar();
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
            GameEvents.OnUpgradeApplied -= OnUpgradeApplied;
            if (_blobHealth != null) _blobHealth.OnShieldChanged -= OnShieldChanged;
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
            string text = "★ " + data.DisplayName.ToUpper(new System.Globalization.CultureInfo("tr-TR"));
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
            PlaceText(_bannerShadow, _banner, Vector2.zero, Vector2.one, new Vector2(0f, -6f), new Vector2(0f, -6f), 92f, TextAlignmentOptions.Center, EvolutionShadow);
            PlaceText(_bannerText, _banner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 92f, TextAlignmentOptions.Center, EvolutionGold);
            go.SetActive(false);
        }

        private void Update()
        {
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

        private void CreateShieldBar()
        {
            if (_healthBar == null) return;

            RectTransform healthRect = _healthBar.GetComponent<RectTransform>();
            GameObject copy = Instantiate(_healthBar.gameObject, healthRect);
            copy.name = "ShieldBar";
            _shieldBar = copy.GetComponent<Slider>();
            _shieldBar.interactable = false;
            if (_shieldBar.handleRect != null)
            {
                Destroy(_shieldBar.handleRect.gameObject);
                _shieldBar.handleRect = null;
            }

            // Can barının hemen altına, aynı genişlikte, onun %45'i kalınlığında.
            RectTransform rect = copy.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -3f);
            rect.sizeDelta = new Vector2(0f, Mathf.Max(6f, healthRect.rect.height * 0.45f));
            rect.localScale = Vector3.one;

            foreach (Image image in copy.GetComponentsInChildren<Image>(true))
            {
                bool isFill = _shieldBar.fillRect != null && image.transform == _shieldBar.fillRect;
                image.color = isFill ? ShieldFillColor : ShieldBackgroundColor;
            }

            copy.SetActive(false);
        }

        private void OnShieldChanged(float current, float max)
        {
            if (_shieldBar == null) return;
            bool hasShield = max > 0f;
            if (_shieldBar.gameObject.activeSelf != hasShield) _shieldBar.gameObject.SetActive(hasShield);
            if (!hasShield) return;
            _shieldBar.maxValue = max;
            _shieldBar.value = current;
        }

        // B18: max<=0 => aktif boss yok (A15/A16 bu sözleşmeyle RaiseBossHealthChanged çağırıyor).
        private void OnBossHealthChanged(float current, float max)
        {
            bool hasActiveBoss = max > 0f;

            if (_bossHealthContainer != null)
                _bossHealthContainer.SetActive(hasActiveBoss);

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
        }

        private void OnScoreChanged(int score)
        {
            if (_scoreText != null)
                _scoreText.text = score.ToString("N0");
        }

        private void OnCoinsChanged(int coins)
        {
            if (_coinText != null)
                _coinText.text = $"{coins:N0} coin";
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
                _tierText.text = tier.ToString().ToUpper();
        }

        private void OnXPChanged(int xp)
        {
            if (_xpBar == null) return;
            if (_blobGrowth == null) _blobGrowth = FindAnyObjectByType<Entities.Blob.BlobGrowth>();
            if (_blobGrowth == null) return;
            _xpBar.maxValue = _blobGrowth.XPThreshold;
            _xpBar.value = _blobGrowth.CurrentXP;
        }

        private void OnLevelUp(int level)
        {
            if (_levelText != null)
                _levelText.text = $"Lv.{level}";
        }

        private void OnCharacterSelected(CharacterData data)
        {
            _blobGrowth = FindAnyObjectByType<Entities.Blob.BlobGrowth>();

            // Run başına bir kez: kalkan event'ine bağlan (restart'ta aynı blob — çift abonelik olmasın).
            if (_blobHealth != null) _blobHealth.OnShieldChanged -= OnShieldChanged;
            _blobHealth = _blobGrowth != null ? _blobGrowth.GetComponent<Entities.Blob.BlobHealth>() : null;
            if (_blobHealth != null)
            {
                _blobHealth.OnShieldChanged += OnShieldChanged;
                OnShieldChanged(_blobHealth.ShieldCurrent, _blobHealth.ShieldMax);
            }
        }

        // 2026-11-04 (Serra: "HUD çok karışık, ikon/rozet tasarımı yok, kaldıralım. Barlar kalsın"): barlara dokunulmaz;
        // yazılar çalışma zamanında yerleşir (sahne kurulumu yok). Sol üst süre, sağ üst skor + coin, seviye/tier XP barının yanında.
        // Hepsi HUD'un altında kalır: lobi HUD'u kapatınca onlar da kapanır.
        private static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.17f, 0.55f);
        private static readonly Color CoinColor = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color SubtleColor = new Color(1f, 1f, 1f, 0.85f);
        private const float TopOffset = 104f; // haber bandının (y -30, 58 yüksek) altı
        private const float Edge = 24f;

        private void LayoutHud()
        {
            if (_skillBadgeContainer != null) _skillBadgeContainer.gameObject.SetActive(false);

            RectTransform layout = CreateLayoutRoot();
            if (layout == null) return;

            RectTransform timerPanel = CreatePanel("TimerPanel", layout, new Vector2(0f, 1f), new Vector2(Edge, -TopOffset), new Vector2(190f, 72f));
            PlaceText(_timerText, timerPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 50f, TextAlignmentOptions.Center, Color.white);

            RectTransform scorePanel = CreatePanel("ScorePanel", layout, new Vector2(1f, 1f), new Vector2(-Edge, -TopOffset), new Vector2(240f, 104f));
            PlaceText(_scoreText, scorePanel, new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -60f), new Vector2(-16f, -6f), 44f,
                TextAlignmentOptions.Right, Color.white);
            if (_coinText == null) _coinText = CreateText("CoinText", scorePanel, "0 coin");
            PlaceText(_coinText, scorePanel, Vector2.zero, new Vector2(1f, 0f), new Vector2(16f, 8f), new Vector2(-16f, 44f), 28f,
                TextAlignmentOptions.Right, CoinColor);

            // Seviye XP barının solunda, tier XP barının üstünde: barla birlikte hareket eder.
            RectTransform xp = _xpBar != null ? _xpBar.GetComponent<RectTransform>() : null;
            if (xp != null)
            {
                PlaceText(_levelText, xp, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-150f, -14f), new Vector2(-12f, 14f), 30f,
                    TextAlignmentOptions.Right, Color.white);
                PlaceText(_tierText, xp, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 4f), new Vector2(0f, 38f), 24f,
                    TextAlignmentOptions.Right, SubtleColor);
            }
        }

        // HUD'un kendi rect'i sahnede daraltılmış (barlar onun köşelerine göre konumlu); yazılar için güvenli alanı
        // kaplayan bir child açılır — HUD'un daralma/kaymasının tersi uygulanır.
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
            rect.SetAsFirstSibling(); // barların altında çizilsin
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
