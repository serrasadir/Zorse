using System.Collections;
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
        [SerializeField] private Slider _shieldBar;

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
        [SerializeField] private UpgradeData[] _allUpgrades;

        [Header("Character")]
        [SerializeField] private Image _characterIcon;

        private readonly Dictionary<UpgradeData, GameObject> _badges = new Dictionary<UpgradeData, GameObject>();
        private float _survivalTime;

        private void Awake()
        {
            EnsureRuntimeHudElements();
            OnShieldChanged(0f, 0f);
            if (_characterIcon != null)
                _characterIcon.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += OnHealthChanged;
            GameEvents.OnShieldChanged += OnShieldChanged;
            GameEvents.OnScoreChanged += OnScoreChanged;
            GameEvents.OnCoinsChanged += OnCoinsChanged;
            GameEvents.OnSurvivalTimeUpdated += OnSurvivalTimeUpdated;
            GameEvents.OnBlobTierChanged += OnTierChanged;
            GameEvents.OnXPChanged += OnXPChanged;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnUpgradeSelected += OnUpgradeSelected;
            GameEvents.OnCharacterSelected += OnCharacterSelected;
            GameEvents.OnGameOver += ClearBadges;
            GameEvents.OnGameOver += ClearCharacterIcon;
            GameEvents.OnGameOver += ClearShieldBar;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= OnHealthChanged;
            GameEvents.OnShieldChanged -= OnShieldChanged;
            GameEvents.OnScoreChanged -= OnScoreChanged;
            GameEvents.OnCoinsChanged -= OnCoinsChanged;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTimeUpdated;
            GameEvents.OnBlobTierChanged -= OnTierChanged;
            GameEvents.OnXPChanged -= OnXPChanged;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnUpgradeSelected -= OnUpgradeSelected;
            GameEvents.OnCharacterSelected -= OnCharacterSelected;
            GameEvents.OnGameOver -= ClearBadges;
            GameEvents.OnGameOver -= ClearCharacterIcon;
            GameEvents.OnGameOver -= ClearShieldBar;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (_healthBar != null)
            {
                _healthBar.maxValue = max;
                _healthBar.value = current;
            }
        }

        private void OnShieldChanged(float current, float max)
        {
            if (_shieldBar == null) return;

            bool hasShield = max > 0f;
            _shieldBar.gameObject.SetActive(hasShield);
            if (!hasShield) return;

            _shieldBar.maxValue = max;
            _shieldBar.value = Mathf.Clamp(current, 0f, max);
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
            if (_timerText != null)
            {
                int minutes = (int)time / 60;
                int seconds = (int)time % 60;
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void OnTierChanged(BlobTier tier)
        {
            if (_tierText != null)
                _tierText.text = tier.ToString().ToUpper();
        }

        private void OnXPChanged(int xp)
        {
            if (_xpBar == null) return;
            var blob = FindAnyObjectByType<Entities.Blob.BlobGrowth>();
            if (blob == null) return;
            _xpBar.maxValue = blob.XPThreshold;
            _xpBar.value = blob.CurrentXP;
        }

        private void OnLevelUp(int level)
        {
            if (_levelText != null)
                _levelText.text = $"Lv.{level}";
        }

        private void OnUpgradeSelected(UpgradeData data)
        {
            if (_skillBadgeContainer == null || _skillBadgePrefab == null) return;
            StartCoroutine(RenderSkillBadgesNextFrame(data));
        }

        private IEnumerator RenderSkillBadgesNextFrame(UpgradeData selected)
        {
            yield return null;
            RenderSkillBadges(selected);
        }

        private void RenderSkillBadges(UpgradeData selected)
        {
            if (_skillBadgeContainer == null || _skillBadgePrefab == null) return;

            if (_allUpgrades != null && _allUpgrades.Length > 0)
            {
                bool renderedSelected = false;
                foreach (var upgrade in _allUpgrades)
                {
                    if (upgrade != null && GetUpgradeLevel(upgrade) > 0)
                    {
                        RenderSkillBadge(upgrade);
                        if (upgrade == selected)
                            renderedSelected = true;
                    }
                }
                if (selected != null && !renderedSelected)
                    RenderSkillBadge(selected, 1);
                RemoveInactiveBadges(selected);
                return;
            }

            if (selected != null && GetUpgradeLevel(selected) > 0)
                RenderSkillBadge(selected);
            else if (selected != null)
                RenderSkillBadge(selected, 1);
        }

        private static int GetUpgradeLevel(UpgradeData data)
        {
            return UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 0;
        }

        private void RenderSkillBadge(UpgradeData data)
        {
            RenderSkillBadge(data, GetUpgradeLevel(data));
        }

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
                levelText.text = Mathf.Max(1, level).ToString();
        }

        private void RemoveInactiveBadges(UpgradeData selected)
        {
            var inactive = new List<UpgradeData>();
            foreach (var kv in _badges)
            {
                if (kv.Key == null || (GetUpgradeLevel(kv.Key) <= 0 && kv.Key != selected))
                    inactive.Add(kv.Key);
            }

            foreach (var data in inactive)
            {
                if (_badges.TryGetValue(data, out GameObject badge) && badge != null)
                    Destroy(badge);
                _badges.Remove(data);
            }
        }

        private void OnCharacterSelected(CharacterData data)
        {
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

        private void ClearShieldBar()
        {
            OnShieldChanged(0f, 0f);
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

            if (_shieldBar == null)
            {
                GameObject shieldObject = new GameObject("ShieldBar", typeof(RectTransform), typeof(Slider));
                Transform shieldParent = _healthBar != null ? _healthBar.transform.parent : overlay;
                shieldObject.transform.SetParent(shieldParent, false);
                _shieldBar = shieldObject.GetComponent<Slider>();
                _shieldBar.interactable = false;
                _shieldBar.transition = Selectable.Transition.None;

                RectTransform shieldRect = shieldObject.GetComponent<RectTransform>();
                if (_healthBar != null)
                {
                    RectTransform healthRect = _healthBar.GetComponent<RectTransform>();
                    shieldRect.anchorMin = healthRect.anchorMin;
                    shieldRect.anchorMax = healthRect.anchorMax;
                    shieldRect.pivot = healthRect.pivot;
                    shieldRect.anchoredPosition = healthRect.anchoredPosition + new Vector2(0f, 28f);
                    shieldRect.sizeDelta = new Vector2(healthRect.sizeDelta.x, 10f);
                }
                else
                {
                    shieldRect.anchorMin = new Vector2(1f, 0f);
                    shieldRect.anchorMax = new Vector2(1f, 0f);
                    shieldRect.pivot = new Vector2(0.5f, 0.5f);
                    shieldRect.anchoredPosition = new Vector2(-116f, 142f);
                    shieldRect.sizeDelta = new Vector2(200f, 10f);
                }

                Image fill = CreateSliderFill(shieldObject.transform, new Color(0.18f, 0.65f, 1f, 0.85f));
                _shieldBar.fillRect = fill.rectTransform;
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

        private static Image CreateSliderFill(Transform parent, Color color)
        {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(parent, false);

            Image fill = fillObject.GetComponent<Image>();
            fill.color = color;

            RectTransform rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return fill;
        }
    }
}
