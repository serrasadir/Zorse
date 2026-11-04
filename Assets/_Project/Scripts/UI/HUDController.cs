using System.Collections.Generic;
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

        [Header("Skill Badges")]
        [SerializeField] private Transform _skillBadgeContainer;
        [SerializeField] private GameObject _skillBadgePrefab;

        [Header("Character")]
        [SerializeField] private Image _characterIcon;

        private readonly Dictionary<UpgradeData, GameObject> _badges = new Dictionary<UpgradeData, GameObject>();
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
            EnsureRuntimeHudElements();
            CreateShieldBar();
            if (_characterIcon != null)
                _characterIcon.gameObject.SetActive(false);
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
            GameEvents.OnUpgradeApplied += OnUpgradeApplied;
            GameEvents.OnCharacterSelected += OnCharacterSelected;
            GameEvents.OnGameOver += ClearBadges;
            GameEvents.OnGameOver += ClearCharacterIcon;
            GameEvents.OnBossHealthChanged += OnBossHealthChanged;
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
            GameEvents.OnUpgradeApplied -= OnUpgradeApplied;
            GameEvents.OnCharacterSelected -= OnCharacterSelected;
            GameEvents.OnGameOver -= ClearBadges;
            GameEvents.OnGameOver -= ClearCharacterIcon;
            GameEvents.OnBossHealthChanged -= OnBossHealthChanged;
            if (_blobHealth != null) _blobHealth.OnShieldChanged -= OnShieldChanged;
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
                _coinText.text = $"Coins: {coins:N0}";
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

        // 2026-11-03: rozet sadece seviyesi değişen skill için güncellenir. Eskiden sahnedeki ikinci bir skill
        // listesini (_allUpgrades, UpgradeSystem'deki listenin kopyası) her seçimde baştan tarıyordu.
        private void OnUpgradeApplied(UpgradeData data, int level) => RenderSkillBadge(data, level);

        private void RenderSkillBadge(UpgradeData data, int level)
        {
            if (data == null) return;

            if (!_badges.TryGetValue(data, out GameObject badge))
            {
                badge = Instantiate(_skillBadgePrefab, _skillBadgeContainer);
                _badges[data] = badge;
                ConfigureSkillBadge(badge);

                var iconImage = badge.GetComponentInChildren<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = data.Icon;
                    iconImage.color = data.Icon != null ? Color.white : new Color(0.18f, 0.65f, 1f, 0.85f);
                }
            }

            var levelText = badge.GetComponentInChildren<TMP_Text>();
            if (levelText != null)
                levelText.text = level.ToString();
        }

        private void OnCharacterSelected(CharacterData data)
        {
            ClearBadges(); // skill seviyeleri run başında sıfırlanıyor (RunComplete sonrası restart'ta da)
            _blobGrowth = FindAnyObjectByType<Entities.Blob.BlobGrowth>();

            // Run başına bir kez: kalkan event'ine bağlan (restart'ta aynı blob — çift abonelik olmasın).
            if (_blobHealth != null) _blobHealth.OnShieldChanged -= OnShieldChanged;
            _blobHealth = _blobGrowth != null ? _blobGrowth.GetComponent<Entities.Blob.BlobHealth>() : null;
            if (_blobHealth != null)
            {
                _blobHealth.OnShieldChanged += OnShieldChanged;
                OnShieldChanged(_blobHealth.ShieldCurrent, _blobHealth.ShieldMax);
            }

            if (_characterIcon == null) return;

            _characterIcon.sprite = data != null ? data.Icon : null;
            _characterIcon.color = data != null && data.Icon == null ? new Color(0.25f, 0.75f, 1f, 1f) : Color.white;
            _characterIcon.gameObject.SetActive(data != null);
        }

        private void ClearBadges()
        {
            foreach (var kv in _badges)
                if (kv.Value != null) Destroy(kv.Value);
            _badges.Clear();
        }

        private void ClearCharacterIcon()
        {
            if (_characterIcon == null) return;
            _characterIcon.sprite = null;
            _characterIcon.gameObject.SetActive(false);
        }

        private void EnsureRuntimeHudElements()
        {
            RectTransform overlay = GetRuntimeOverlay();
            if (overlay == null) return;

            ConfigureSkillBadgeContainer(overlay);
            EnsureCoinText(overlay);

            if (_characterIcon == null)
            {
                GameObject iconObject = new GameObject("CharacterIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(overlay, false);
                _characterIcon = iconObject.GetComponent<Image>();
                _characterIcon.preserveAspect = true;

                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(16f, -16f);
                iconRect.sizeDelta = new Vector2(42f, 42f);
            }
        }

        private RectTransform GetRuntimeOverlay()
        {
            Transform parent = transform.parent != null ? transform.parent : transform;
            Transform existing = parent.Find("HUDRuntimeOverlay");
            if (existing != null)
                return existing as RectTransform;

            GameObject overlayObject = new GameObject("HUDRuntimeOverlay", typeof(RectTransform));
            overlayObject.transform.SetParent(parent, false);
            overlayObject.transform.SetAsLastSibling();

            RectTransform overlay = overlayObject.GetComponent<RectTransform>();
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.pivot = new Vector2(0.5f, 0.5f);
            return overlay;
        }

        private void EnsureCoinText(RectTransform overlay)
        {
            if (_coinText != null) return;

            GameObject coinObject = new GameObject("CoinText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            coinObject.transform.SetParent(overlay, false);

            _coinText = coinObject.GetComponent<TMP_Text>();
            _coinText.text = "Coins: 0";
            _coinText.fontSize = 18f;
            _coinText.alignment = TextAlignmentOptions.TopRight;
            _coinText.color = new Color(1f, 0.82f, 0.25f, 1f);

            RectTransform rect = coinObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -96f);
            rect.sizeDelta = new Vector2(180f, 30f);
        }

        private void ConfigureSkillBadgeContainer(RectTransform overlay)
        {
            if (_skillBadgeContainer == null) return;

            _skillBadgeContainer.SetParent(overlay, false);

            RectTransform rect = _skillBadgeContainer as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-16f, -16f);
                rect.sizeDelta = new Vector2(220f, 44f);
            }

            HorizontalLayoutGroup layout = _skillBadgeContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.UpperRight;
                layout.spacing = 6f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }

        private static void ConfigureSkillBadge(GameObject badge)
        {
            RectTransform badgeRect = badge.GetComponent<RectTransform>();
            if (badgeRect != null)
                badgeRect.sizeDelta = new Vector2(34f, 34f);

            LayoutElement layoutElement = badge.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = badge.AddComponent<LayoutElement>();

            layoutElement.minWidth = 34f;
            layoutElement.minHeight = 34f;
            layoutElement.preferredWidth = 34f;
            layoutElement.preferredHeight = 34f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            Image[] images = badge.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                RectTransform rect = image.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                image.preserveAspect = true;
            }

            TMP_Text levelText = badge.GetComponentInChildren<TMP_Text>(true);
            if (levelText != null)
            {
                levelText.fontSize = 16f;
                levelText.alignment = TextAlignmentOptions.BottomRight;
                RectTransform textRect = levelText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(2f, 1f);
                textRect.offsetMax = new Vector2(-2f, -1f);
            }
        }
    }
}
