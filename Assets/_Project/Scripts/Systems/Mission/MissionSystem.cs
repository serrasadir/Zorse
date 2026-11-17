using System;
using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Loot;
using BlobSurvivor.UI;

namespace BlobSurvivor.Systems
{
    public enum MissionType { KillPolice, EatPolice, EatAny, BigBites, NoDamage, GrabChest }
    public enum MissionResult { None, Completed, Failed }

    // Anlık görevler (OYUN.md §5): run içinde kısa süreli hedef. Başarınca coin + run içi bonus
    // (can, kalkan ya da ekstra kart); süre dolarsa cezasız kaybolur. Ekranda hep en fazla bir görev.
    // Sahne kurulumu gerekmez: HUD olan sahne yüklenince kendini kurar.
    public class MissionSystem : MonoBehaviour
    {
        public static MissionSystem Instance { get; private set; }

        private const float FirstMissionTime = 90f;
        private const float Gap = 60f;               // görev bittikten sonra sıradaki
        private const float RetryDelay = 5f;         // boss/kuşatma yüzünden ertelenince
        private const float EncirclementQuiet = 20f; // kuşatmadan sonra bu kadar sn görev gelmez
        private const float CoinBase = 15f, CoinPerMinute = 3f;
        private const float HealFraction = 0.3f, ShieldAmount = 25f;
        private const float CardChance = 0.25f;
        private const float GoToMin = 22f, GoToMax = 38f;

        // Denge: görev coin'inin çarpanı (ölçümden sonra ayarlanır).
        public static float CoinScale = 1f;

        public bool Active { get; private set; }
        public MissionType Type { get; private set; }
        public string Label { get; private set; }    // HUD satırı, ör. "Polis indir"
        public int Progress { get; private set; }
        public int Target { get; private set; }      // 0 = sayaçsız (süre/mesafe görevi)
        public float TimeLeft { get; private set; }
        public Vector3 TargetPoint { get; private set; }
        public MissionResult LastResult { get; private set; }
        public float LastResultTime { get; private set; } = -100f;

        public int Offered { get; private set; }
        public int Completed { get; private set; }

        public float DistanceToTarget =>
            _blob != null ? Vector3.Distance(Flat(_blob.position), Flat(TargetPoint)) : 0f;

        private Transform _blob;
        private BlobGrowth _growth;
        private BlobHealth _health;
        private ScoreSystem _score;

        private float _survivalTime;
        private float _nextTime = FirstMissionTime;
        private float _lastEncirclement = -100f;
        private bool _bossActive;
        private bool _hasLast;
        private MissionType _last;
        private BlobTier _bigBiteTier;
        private ChestPickup _chest; // Sandığı kap görevinin sandığı
        private readonly List<MissionType> _candidates = new List<MissionType>(6);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;
            if (FindAnyObjectByType<HUDController>(FindObjectsInactive.Include) == null) return;
            new GameObject("MissionSystem").AddComponent<MissionSystem>();
        }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob != null)
            {
                _blob = blob.transform;
                _growth = blob.GetComponent<BlobGrowth>();
                _health = blob.GetComponent<BlobHealth>();
                if (_health != null) _health.OnDamaged += OnDamaged;
            }
            _score = FindAnyObjectByType<ScoreSystem>();
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterSelected += OnRunStarted;
            GameEvents.OnSurvivalTimeUpdated += OnSurvivalTime;
            GameEvents.OnBossHealthChanged += OnBossHealth;
            GameEvents.OnEncirclement += OnEncirclement;
            GameEvents.OnEnemyDefeated += OnEnemyDefeated;
            GameEvents.OnBlobAte += OnBlobAte;
            GameEvents.OnGameOver += EndSilently;
            GameEvents.OnRunComplete += OnRunComplete;
            ChestPickup.AnyCollected += OnChestCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTime;
            GameEvents.OnBossHealthChanged -= OnBossHealth;
            GameEvents.OnEncirclement -= OnEncirclement;
            GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
            GameEvents.OnBlobAte -= OnBlobAte;
            GameEvents.OnGameOver -= EndSilently;
            GameEvents.OnRunComplete -= OnRunComplete;
            ChestPickup.AnyCollected -= OnChestCollected;
            if (_health != null) _health.OnDamaged -= OnDamaged;
        }

        // Restart sahneyi yeniden yüklemez: run durumu burada sıfırlanır.
        private void OnRunStarted(CharacterData _)
        {
            _chest = null; // ChestSpawner restart'ta kendi sandıklarını havuza döndürür
            Active = false;
            LastResult = MissionResult.None;
            _survivalTime = 0f;
            _nextTime = FirstMissionTime;
            _lastEncirclement = -100f;
            _bossActive = false;
            _hasLast = false;
            Offered = Completed = 0;
        }

        private void OnSurvivalTime(float t) => _survivalTime = t;
        private void OnBossHealth(float current, float max) => _bossActive = max > 0f;
        private void OnEncirclement() => _lastEncirclement = _survivalTime;
        private void OnRunComplete(RunEndReason _) => EndSilently();

        private void EndSilently()
        {
            RemoveChest();
            Active = false;
            LastResult = MissionResult.None;
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            if (!Active)
            {
                if (_survivalTime >= _nextTime) TryStart(false);
                return;
            }

            TimeLeft -= Time.deltaTime;
            if (TimeLeft > 0f) return;
            if (Type == MissionType.NoDamage) Complete();
            else Finish(MissionResult.Failed);
        }

        private void TryStart(bool force)
        {
            if (!force && (_bossActive || _survivalTime - _lastEncirclement < EncirclementQuiet))
            {
                _nextTime = _survivalTime + RetryDelay;
                return;
            }

            BlobTier tier = _growth != null ? _growth.CurrentTier : GameEvents.CurrentBlobTier;
            _candidates.Clear();
            _candidates.Add(MissionType.KillPolice);
            _candidates.Add(MissionType.EatAny);
            _candidates.Add(MissionType.NoDamage);
            if (ChestSpawner.Instance != null) _candidates.Add(MissionType.GrabChest);
            if (tier >= BlobTier.Medium) _candidates.Add(MissionType.EatPolice); // normal polis Medium'da yenir
            if (tier >= BlobTier.Small) _candidates.Add(MissionType.BigBites);
            if (_hasLast && _candidates.Count > 1) _candidates.Remove(_last);

            for (int attempt = 0; attempt < 3 && _candidates.Count > 0; attempt++)
            {
                MissionType type = _candidates[UnityEngine.Random.Range(0, _candidates.Count)];
                if (Begin(type, tier)) return;
                _candidates.Remove(type); // ör. hedef nokta bulunamadı
            }
            _nextTime = _survivalTime + RetryDelay;
        }

        private bool Begin(MissionType type, BlobTier tier)
        {
            int level = _growth != null ? _growth.CurrentLevel : 0;
            string title = "GÖREV!";
            string sub;
            float duration;
            Progress = 0;
            Target = 0;

            switch (type)
            {
                case MissionType.KillPolice:
                    Target = 5 + level / 2;
                    duration = 45f;
                    Label = "Polis indir";
                    sub = $"{duration:0} sn'de {Target} polis indir";
                    break;
                case MissionType.EatPolice:
                    Target = 3;
                    duration = 45f;
                    Label = "Polis yut";
                    sub = $"{duration:0} sn'de {Target} polis yut";
                    break;
                case MissionType.EatAny:
                    Target = 15 + level * 2;
                    duration = 45f;
                    Label = "Bir şeyler ye";
                    sub = $"{duration:0} sn'de {Target} şey ye";
                    break;
                case MissionType.BigBites:
                    _bigBiteTier = (BlobTier)Mathf.Max((int)BlobTier.Tiny, (int)tier - 1);
                    Target = 3;
                    duration = 60f;
                    Label = TierNames.Get(_bigBiteTier) + "+ lokma";
                    sub = $"{duration:0} sn'de {Target} {TierNames.Get(_bigBiteTier)} ya da daha büyük şey ye";
                    break;
                case MissionType.NoDamage:
                    duration = 30f;
                    Label = "Hasar alma";
                    sub = $"{duration:0} sn hiç hasar alma";
                    break;
                case MissionType.GrabChest:
                    if (!TryFindTargetPoint(out Vector3 point)) return false;
                    _chest = ChestSpawner.Instance.SpawnChest(point);
                    if (_chest == null) return false;
                    TargetPoint = point;
                    duration = 30f;
                    Label = "Sandığı kap";
                    sub = $"{duration:0} sn'de sandığa yetiş";
                    break;
                default:
                    return false;
            }

            Type = type;
            TimeLeft = duration;
            Active = true;
            LastResult = MissionResult.None;
            _last = type;
            _hasLast = true;
            Offered++;
            GameEvents.RaiseMissionStarted(title, sub);
            return true;
        }

        private bool TryFindTargetPoint(out Vector3 point)
        {
            point = Vector3.zero;
            if (_blob == null) return false;
            Vector3 blobPos = Flat(_blob.position);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                Vector3 candidate = blobPos + new Vector3(dir.x, 0f, dir.y) * UnityEngine.Random.Range(GoToMin, GoToMax);
                candidate = MapBounds.Clamp(candidate, 4f);
                if (!SpawnPositionUtility.TryFindNavMeshPosition(candidate, 0f, 4f, out Vector3 snapped)) continue;
                if ((Flat(snapped) - blobPos).sqrMagnitude < GoToMin * GoToMin * 0.5f) continue; // harita kenarında çok yakına düştü
                point = snapped;
                return true;
            }
            return false;
        }

        private void OnEnemyDefeated(EnemyData data, bool eaten)
        {
            if (!Active || data == null || data.PreventConsumption || data.IsSwarm) return; // boss ve robot köpek sayılmaz
            if ((Type == MissionType.KillPolice && !eaten) || (Type == MissionType.EatPolice && eaten))
                Advance();
        }

        private void OnBlobAte(BlobTier foodTier)
        {
            if (!Active) return;
            if (Type == MissionType.EatAny || (Type == MissionType.BigBites && foodTier >= _bigBiteTier))
                Advance();
        }

        // Sandığın kendi ödülü (coin + ekstra kart) zaten verildi: görev ayrıca ödül vermez.
        private void OnChestCollected(ChestPickup chest)
        {
            if (!Active || Type != MissionType.GrabChest || chest != _chest) return;
            _chest = null;
            Finish(MissionResult.Completed);
            Completed++;
            GameEvents.RaiseMissionCompleted("Sandık senin!");
        }

        private void RemoveChest()
        {
            if (_chest != null && _chest.gameObject.activeInHierarchy) ChestSpawner.Instance?.Remove(_chest);
            _chest = null;
        }

        private void OnDamaged(float amount)
        {
            if (Active && Type == MissionType.NoDamage && amount > 0f)
                Finish(MissionResult.Failed);
        }

        private void Advance()
        {
            Progress++;
            if (Progress >= Target) Complete();
        }

        private void Complete()
        {
            if (!Active) return;
            if (Type == MissionType.GrabChest) return; // sandık toplanınca biter (OnChestCollected)
            Finish(MissionResult.Completed);
            Completed++;

            int minutes = Mathf.FloorToInt(_survivalTime / 60f);
            int coins = Mathf.Max(1, Mathf.RoundToInt((CoinBase + CoinPerMinute * minutes) * CoinScale));
            _score?.AddCoin(coins, CoinSource.Mission);

            string bonus = GrantBonus();
            GameEvents.RaiseMissionCompleted($"+{coins} coin · {bonus}");
        }

        // Can doluysa can verilmez; kart en nadir olanı.
        private string GrantBonus()
        {
            bool canHeal = _health != null && _health.CurrentHealth < _health.MaxHealth * 0.9f;
            if (UnityEngine.Random.value < CardChance)
            {
                GameEvents.RaiseLevelUp(_growth != null ? _growth.CurrentLevel : 0); // sandıktaki gibi ekstra kart
                return "Ekstra kart";
            }
            if (canHeal && UnityEngine.Random.value < 0.5f)
            {
                _health.Heal(_health.MaxHealth * HealFraction);
                return "Can +%30";
            }
            if (_health != null)
            {
                _health.AddBonusShield(ShieldAmount, ShieldAmount);
                return $"Kalkan +{ShieldAmount:0}";
            }
            return "";
        }

        private void Finish(MissionResult result)
        {
            if (result == MissionResult.Failed) RemoveChest();
            Active = false;
            LastResult = result;
            LastResultTime = Time.unscaledTime;
            _nextTime = _survivalTime + Gap;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // DevCheats (O / P).
        public string DebugStartNext()
        {
            RemoveChest();
            Active = false;
            TryStart(true);
            return Active ? $"Görev: {Label} ({Type})" : "Görev başlatılamadı";
        }

        public string DebugComplete()
        {
            if (!Active) return "Aktif görev yok";
            if (Type == MissionType.GrabChest) { RemoveChest(); Finish(MissionResult.Completed); Completed++; return "Sandık görevi kapatıldı"; }
            Complete();
            return "Görev tamamlandı";
        }
    }
}
