using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Weapons;
using BlobSurvivor.Systems.Meta;

namespace BlobSurvivor.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        LevelUp,
        GameOver,
        RunComplete
    }

    // A17 (GDD_v2.md Karar 1): run'ın GameOver'dan (oyuncu ölümü) ayrı, "başarıyla" kapandığı iki yol.
    public enum RunEndReason
    {
        FinalBossConsumed,
        TimeSurvived
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private CharacterData _defaultCharacter;

        // A17: final boss (12. dk) yenilemese bile run'ın sonsuza kadar sürmemesi için güvenlik eşiği —
        // GDD'nin "12-15 dk" run vizyonunun üst sınırı. WaveController'a dokunmadan (dev-a domaini,
        // ayrı dosya) sadece burada, run'ın ne zaman kapanacağına dair kararı tutar.
        [SerializeField] private float _runTimeoutSeconds = 900f;

        private CharacterData _lastCharacter;

        public GameState CurrentState { get; private set; }
        public float SurvivalTime { get; private set; }
        public bool IsPlaying => CurrentState == GameState.Playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void OnEnable()
        {
            GameEvents.OnFinalBossConsumed += HandleFinalBossConsumed;
        }

        private void OnDisable()
        {
            GameEvents.OnFinalBossConsumed -= HandleFinalBossConsumed;
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            SurvivalTime += Time.deltaTime;
            GameEvents.RaiseSurvivalTimeUpdated(SurvivalTime);

            if (SurvivalTime >= _runTimeoutSeconds)
                TriggerRunComplete(RunEndReason.TimeSurvived);
        }

        private void HandleFinalBossConsumed() => TriggerRunComplete(RunEndReason.FinalBossConsumed);

        public void StartGame()
        {
            StartGame(_lastCharacter != null ? _lastCharacter : _defaultCharacter);
        }

        public void StartGame(CharacterData character)
        {
            FindAnyObjectByType<Systems.ScoreSystem>()?.ResetScore();
            SurvivalTime = 0f;
            ChangeState(GameState.Playing);

            if (character != null)
            {
                _lastCharacter = character;
                ApplyCharacter(character);
            }
        }

        private void ApplyCharacter(CharacterData character)
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob == null) return;

            // Restart aynı blob instance'ını yeniden kullanır (sahne reload yok) — önceki run'dan
            // kalan silah/pasif bileşenlerini temizlemeden ApplyCharacter tekrar çağrılırsa
            // silah çiftlenir ve pasifler (örn. VacuumComponent.Radius) stack'lenir.
            ClearPreviousRunState(blob);
            ApplyMetaProgression(blob, character);

            switch (character.PassiveType)
            {
                case CharacterPassiveType.MoveSpeed:
                    // M22: hız artık ApplyMetaProgression'da meta bonusuyla birlikte tek seferde set ediliyor
                    // (aksi halde burada overwrite edilip meta bonusu kaybolurdu).
                    break;
                case CharacterPassiveType.MagnetPull:
                    VacuumComponent vacuum = blob.AddComponent<VacuumComponent>();
                    vacuum.IncreaseRadius(character.PassiveValue);
                    break;
                case CharacterPassiveType.ConsumableSplit:
                    // PistolWeapon zaten hedef tier'ı kontrol edip parçalıyor — ek kurulum gerekmiyor.
                    break;
            }

            if (character.StartingWeaponPrefab != null)
            {
                GameObject weapon = Instantiate(character.StartingWeaponPrefab, blob.transform);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
            }

            GameEvents.RaiseCharacterSelected(character);
        }

        private void ClearPreviousRunState(GameObject blob)
        {
            VacuumComponent oldVacuum = blob.GetComponent<VacuumComponent>();
            if (oldVacuum != null) Destroy(oldVacuum);

            WeaponBase oldWeapon = blob.GetComponentInChildren<WeaponBase>();
            if (oldWeapon != null) Destroy(oldWeapon.gameObject);

            blob.GetComponent<BlobGrowth>()?.ResetGrowth();
            blob.GetComponent<BlobHealth>()?.ResetHealth();
        }

        // M22 (GDD §8): Market'te satın alınan kalıcı statlar her run başında (fresh start + restart)
        // uygulanır. ClearPreviousRunState'ten SONRA çağrılmalı — Reset*() metodları önce temiz tabana
        // dönmeli, meta bonusu ondan sonra üzerine binmeli (aksi halde restart'ta katlanır).
        private void ApplyMetaProgression(GameObject blob, CharacterData character)
        {
            MetaProgression meta = MetaProgression.Instance;
            float speedBonus = meta != null ? meta.SpeedBonus : 0f;
            float maxHealthBonus = meta != null ? meta.MaxHealthBonus : 0f;
            float massGainBonus = meta != null ? meta.MassGainBonus : 0f;
            float coinGainBonus = meta != null ? meta.CoinGainBonus : 0f;
            float xpBonus = meta != null ? meta.XpMultiplierBonus : 0f;

            float characterSpeedBonus = character.PassiveType == CharacterPassiveType.MoveSpeed ? character.PassiveValue : 0f;
            blob.GetComponent<BlobController>()?.SetSpeedMultiplier(1f + characterSpeedBonus + speedBonus);

            if (maxHealthBonus > 0f)
                blob.GetComponent<BlobHealth>()?.IncreaseMaxHealth(maxHealthBonus);

            blob.GetComponent<BlobGrowth>()?.ApplyMetaBonuses(massGainBonus, xpBonus);

            FindAnyObjectByType<Systems.ScoreSystem>()?.SetCoinGainMultiplier(1f + coinGainBonus);
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
            GameEvents.RaiseGamePaused();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused && CurrentState != GameState.LevelUp) return;
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
            GameEvents.RaiseGameResumed();
        }

        public void TriggerLevelUp(Data.UpgradeData[] choices)
        {
            if (CurrentState != GameState.Playing) return;
            ChangeState(GameState.LevelUp);
            Time.timeScale = 0f;
            GameEvents.RaiseUpgradeChoicesReady(choices);
        }

        public void TriggerGameOver()
        {
            ChangeState(GameState.GameOver);
            Time.timeScale = 0f;
            GameEvents.RaiseGameOver();
        }

        // A17: final boss yutulunca ya da run-timeout'a ulaşılınca — normal GameOver'dan (ölüm)
        // bilinçli olarak ayrı bir state. UI tarafı (B18/Sprint 5) OnRunComplete'i dinleyip
        // kendi "run tamamlandı" ekranını gösterecek; bu issue sadece state/event kontratını kurar.
        public void TriggerRunComplete(RunEndReason reason)
        {
            if (CurrentState != GameState.Playing) return;
            ChangeState(GameState.RunComplete);
            Time.timeScale = 0f;
            GameEvents.RaiseRunComplete(reason);
        }

        private void ChangeState(GameState newState)
        {
            CurrentState = newState;

#if UNITY_EDITOR
            Debug.Log($"[GameManager] State: {newState}");
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && CurrentState == GameState.Playing)
                PauseGame();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
