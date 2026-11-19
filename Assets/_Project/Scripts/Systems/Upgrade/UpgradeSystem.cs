using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    public class UpgradeSystem : MonoBehaviour
    {
        public static UpgradeSystem Instance { get; private set; }

        [SerializeField] private UpgradeData[] _allUpgrades;
        [SerializeField] private int _choiceCount = 3;

        private GameObject _blobRoot;

        // A12/B12: seviye artık paylaşılan UpgradeData asset'inde değil, burada runtime'da tutulur —
        // asset salt-okunur veri kalır, editor'da yanlışlıkla dirty/save riski ve run'lar arası
        // sızıntı riski ortadan kalkar.
        private readonly Dictionary<UpgradeData, int> _levels = new Dictionary<UpgradeData, int>();


        // 2026-11-03: tek lokma eşiği birden fazla kez geçince (AddXP döngüsü) ikinci level-up ilk kart
        // ekranının üstüne yazıyor, seçilmeyen seviye kayboluyordu. Kart ekranı açıkken gelenler sıraya
        // alınır, seçim sonrası arka arkaya gösterilir.
        private bool _awaitingChoice;
        private int _pendingLevelUps;
        private CharacterData _character; // favori skill'i daha sık çekilir

        // Skill sistemi (design/gdd/skill-sistemi.md): aynı anda en fazla 3 aktif + 3 pasif. Tür slotu dolunca
        // o türden yeni skill çekilmez, sadece eldekilerin seviyesi çıkar. Her şey max ise level-up can yeniler.
        [Header("Slot kuralı")]
        [SerializeField] private int _activeSlots = 3;
        [SerializeField] private int _passiveSlots = 3;
        [Tooltip("Hepsi max iken level atlayınca yenilenen can (max canın oranı).")]
        [SerializeField, Range(0f, 1f)] private float _allMaxHealFraction = 0.3f;

        public int ActiveSlots => _activeSlots;
        public int PassiveSlots => _passiveSlots;

        // Slot tutan sahip olunan skill'ler, alım sırasıyla (HUD slot şeridi). Başlangıç skill'i ilk; Sopa Sv.0'da yok.
        private readonly List<UpgradeData> _ownedActives = new List<UpgradeData>();
        private readonly List<UpgradeData> _ownedPassives = new List<UpgradeData>();
        public IReadOnlyList<UpgradeData> OwnedActives => _ownedActives;
        public IReadOnlyList<UpgradeData> OwnedPassives => _ownedPassives;

        // Seviye yazan tek yer: ilk kez sahip olunan slotlu skill sıralı listeye eklenir.
        private void SetLevel(UpgradeData data, int level)
        {
            _levels[data] = level;
            if (level <= 0 || !data.UsesSlot) return;
            List<UpgradeData> list = data.Kind == SkillKind.Active ? _ownedActives : _ownedPassives;
            if (list.Contains(data)) return;
            list.Add(data);
#if UNITY_EDITOR
            Debug.Log($"[UpgradeSystem] Slotlar: aktif [{Names(_ownedActives)}] pasif [{Names(_ownedPassives)}]");
#endif
        }

        private void ClearOwned()
        {
            _levels.Clear();
            _ownedActives.Clear();
            _ownedPassives.Clear();
        }

#if UNITY_EDITOR
        private static string Names(List<UpgradeData> list)
        {
            var parts = new string[list.Count];
            for (int i = 0; i < list.Count; i++) parts[i] = list[i].Abbreviation;
            return string.Join(", ", parts);
        }
#endif

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            _blobRoot = GameObject.FindWithTag("Blob");
            ClearOwned();
        }

        // Evrim (EvolutionSystem, evrim anının patlamasında): kart ekranı olmadan Sv.1 olarak uygulanır.
        public void GrantEvolution(UpgradeData data)
        {
            if (data == null || GetLevel(data) > 0) return;
            SetLevel(data, 1);
            if (_blobRoot != null && data.Effect != null) data.Effect.Apply(_blobRoot, data);
            GameEvents.RaiseUpgradeApplied(data, 1);
        }

        private void OnEnable()
        {
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnUpgradeSelected += OnUpgradeSelected;
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnUpgradeSelected -= OnUpgradeSelected;
            GameEvents.OnCharacterSelected -= HandleRestart;
        }

        // Skill seviyeleri ve açılan evrimler run'a özeldir.
        private void HandleRestart(CharacterData character)
        {
            _character = character;
            ClearOwned();
            _awaitingChoice = false;
            _pendingLevelUps = 0;
            GrantStartingUpgrade(character);
        }

        // Karakterin başlangıç skill'i (Dalcık: Sopa Sv.2): kart seçimiyle aynı yoldan, seviye seviye uygulanır.
        private void GrantStartingUpgrade(CharacterData character)
        {
            if (character == null || character.StartingUpgrade == null || _blobRoot == null) return;
            UpgradeData data = character.StartingUpgrade;
            int target = Mathf.Min(character.StartingUpgradeLevel, data.MaxLevel);
            for (int level = 1; level <= target; level++)
            {
                SetLevel(data, level);
                if (data.Effect != null) data.Effect.Apply(_blobRoot, data);
                GameEvents.RaiseUpgradeApplied(data, level);
            }
        }

        public int GetLevel(UpgradeData data)
        {
            return data != null && _levels.TryGetValue(data, out int level) ? level : 0;
        }

        private void OnLevelUp(int level)
        {
            // 2026-11-04 (Serra: "final boss'u yiyince üst üste skill aldırdı, oyun bitmedi"): helikopteri yutmak
            // önce run'ı bitiriyor, hemen ardından 300 mass XP olarak eklenip birkaç level atlatıyordu — kartlar
            // run bitmiş ekranın üstünde açılıyordu. Run bittiyse (ölüm ya da tamamlandı) level-up yok sayılır.
            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Playing;
            if (state == GameState.GameOver || state == GameState.RunComplete) return;

            if (_awaitingChoice)
            {
                _pendingLevelUps++;
                return;
            }

            if (TryShowChoices())
                GameManager.Instance?.BeginLevelUp();
        }

        private bool TryShowChoices()
        {
            if (_allUpgrades == null || _allUpgrades.Length == 0) return false;

            UpgradeData[] choices = SelectUpgrades();
            if (choices == null || choices.Length == 0)
            {
                // Her şey max (ya da slotlar dolu ve eldekiler max): kart yok, can yenilenir.
                var health = _blobRoot != null ? _blobRoot.GetComponent<BlobSurvivor.Entities.Blob.BlobHealth>() : null;
                if (health != null) health.Heal(health.MaxHealth * _allMaxHealFraction);
                return false;
            }

            _awaitingChoice = true;
            GameEvents.RaiseUpgradeChoicesReady(choices);
            return true;
        }

        private void OnUpgradeSelected(UpgradeData data)
        {
            if (data == null) return;
            _awaitingChoice = false;

            int newLevel = GetLevel(data) + 1;
            SetLevel(data, newLevel);

            if (_blobRoot != null && data.Effect != null)
                data.Effect.Apply(_blobRoot, data);

            GameEvents.RaiseUpgradeApplied(data, newLevel);

            // Sırada bekleyen level-up varsa oyun duraklamış kalır, sıradaki kartlar gösterilir.
            while (_pendingLevelUps > 0)
            {
                _pendingLevelUps--;
                if (TryShowChoices()) return;
            }

            // _blobRoot bulunamasa bile oyunu duraklamış bırakma — soft-lock fix (B12).
            GameManager.Instance?.ResumeGame();
        }

        // Test listesi (DevCheats, PerfOverlay dev paneli): havuzdaki skiller.
        public List<UpgradeData> DebugAllUpgrades()
        {
            var all = new List<UpgradeData>();
            if (_allUpgrades != null) all.AddRange(_allUpgrades);
            all.RemoveAll(u => u == null);
            return all;
        }

        // DevCheats / dev paneli (test build'lerinde de): kart ekranı açmadan skill'i bir seviye verir (kart seçimiyle aynı uygulama + evrim kontrolü).
        public string DebugGrant(string id)
        {
            if (_allUpgrades == null) return "Skill listesi boş.";
            foreach (var u in _allUpgrades)
            {
                if (u == null || u.Id != id) continue;
                int level = GetLevel(u);
                if (level >= u.MaxLevel) return $"{u.DisplayName} zaten max ({level}/{u.MaxLevel}).";
                SetLevel(u, level + 1);
                if (_blobRoot != null && u.Effect != null) u.Effect.Apply(_blobRoot, u);
                GameEvents.RaiseUpgradeApplied(u, level + 1);
                return $"{u.DisplayName} Sv.{level + 1}/{u.MaxLevel}";
            }
            return $"'{id}' skill'i bulunamadı.";
        }

        // Market kilidi; karakterin başlangıç ve favori skill'i o karakterle hep açık.
        private bool IsUnlocked(UpgradeData u)
        {
            var meta = Meta.MetaProgression.Instance;
            if (meta == null || meta.IsSkillUnlocked(u.Id)) return true;
            return _character != null && (u == _character.StartingUpgrade || u == _character.FavoredUpgrade);
        }

        // Market'in skill sekmesi (kart havuzundaki skiller, sahnedeki sırayla).
        public UpgradeData[] AllUpgrades => _allUpgrades;

        public UpgradeData FindById(string id)
        {
            if (_allUpgrades != null)
                foreach (UpgradeData u in _allUpgrades)
                    if (u != null && u.Id == id) return u;
            return null;
        }

        private float WeightOf(UpgradeData u) =>
            _character != null && u == _character.FavoredUpgrade ? u.Weight * _character.FavoredWeightMultiplier : u.Weight;

        // Bu türden kaç farklı skill alındı (slot tutanlar).
        public int OwnedCount(SkillKind kind)
        {
            int n = 0;
            foreach (var pair in _levels)
                if (pair.Value > 0 && pair.Key != null && pair.Key.UsesSlot && pair.Key.Kind == kind) n++;
            return n;
        }

        private bool SlotFull(SkillKind kind) =>
            kind == SkillKind.Active ? OwnedCount(SkillKind.Active) >= _activeSlots
            : kind == SkillKind.Passive && OwnedCount(SkillKind.Passive) >= _passiveSlots;

        // Kart havuzuna girebilir mi: max değil, yeni skill ise türünün slotu boş.
        private bool Offerable(UpgradeData u)
        {
            if (u == null) return false;
            int level = GetLevel(u);
            if (level >= u.MaxLevel) return false;
            if (level == 0 && !IsUnlocked(u)) return false;
            return level > 0 || !u.UsesSlot || !SlotFull(u.Kind);
        }

        private UpgradeData[] SelectUpgrades()
        {
            var pool = new List<UpgradeData>();
            foreach (var u in _allUpgrades)
                if (Offerable(u)) pool.Add(u);
            if (pool.Count == 0) return null;

            int count = Mathf.Min(_choiceCount, pool.Count);
            var selected = new UpgradeData[count];

            for (int i = 0; i < count; i++)
            {
                float total = 0f;
                foreach (var u in pool) total += WeightOf(u);

                float roll = Random.Range(0f, total);
                float cumulative = 0f;
                int pick = pool.Count - 1; // float yuvarlaması: son eleman
                for (int j = 0; j < pool.Count; j++)
                {
                    cumulative += WeightOf(pool[j]);
                    if (roll <= cumulative) { pick = j; break; }
                }
                selected[i] = pool[pick];
                pool.RemoveAt(pick);
            }

            return selected;
        }
    }
}
