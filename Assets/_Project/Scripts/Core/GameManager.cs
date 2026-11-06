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

    // A17 (GDD_v2.md Karar 1): run'ın GameOver'dan (oyuncu ölümü) ayrı, "başarıyla" kapandığı yol.
    // 2026-11-04 (Serra): 15. dk zaman sınırı (TimeSurvived) kaldırıldı — boss savaşı uzayabilir;
    // run sadece ölümle ya da final boss yutulunca biter.
    public enum RunEndReason
    {
        FinalBossConsumed
    }

    public class GameManager : MonoBehaviour
    {
        private const float TopikSlowdownReduction = 0.03f;

        public static GameManager Instance { get; private set; }

        [SerializeField] private CharacterData _defaultCharacter;

        private CharacterData _lastCharacter;
        // 2026-09-06 (Serra): RaiseSurvivalTimeUpdated her frame ateşleniyordu — HUD (her frame string
        // alloc, CLAUDE.md kuralına aykırı), WaveController ve BossSpawner'a gereksiz sıklıkta yayılıyordu.
        // Hiçbir tüketicisi frame-hassas çözünürlük istemiyor (en dar eşik dakika bazlı); 0.1s'e throttle
        // edildi. `SurvivalTime` property'si hâlâ her frame doğru güncelleniyor, sadece event daha seyrek.
        private const float SurvivalTimeEventInterval = 0.1f;
        private float _survivalTimeEventTimer;

        private Vector3 _blobSpawnPosition;
        private bool _hasBlobSpawnPosition;

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
            if (transform.parent == null) DontDestroyOnLoad(gameObject); // child ise uyarı basar; tek sahne, gerekmiyor
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

            _survivalTimeEventTimer -= Time.deltaTime;
            if (_survivalTimeEventTimer <= 0f)
            {
                _survivalTimeEventTimer = SurvivalTimeEventInterval;
                GameEvents.RaiseSurvivalTimeUpdated(SurvivalTime);
            }
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
            _survivalTimeEventTimer = 0f;
            Time.timeScale = 1f; // GameOver/RunComplete 0'da bırakır, restart bunu açmazsa oyun donuk başlar
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
            // silah çiftlenir ve pasifler (örn. VacuumComponent.Reach) stack'lenir.
            ClearPreviousRunState(blob);
            ApplyMetaProgression(blob, character);

            // 2026-11-04: karaktere özel görünüm (Topik = yüzlü turuncu top); boşsa varsayılan küre.
            BlobVisual visual = blob.GetComponent<BlobVisual>();
            if (visual == null) visual = blob.AddComponent<BlobVisual>();
            visual.Apply(character.BlobVisualPrefab);
            ArmorShield.OnHead = character.ArmorOnHead; // Zırh kalkanı kafada mı (Mermo)
            if (character.SpeedTrail)
                blob.AddComponent<SpeedTrail>(); // Topik: rüzgar izi (eskisi ClearPreviousRunState'te kare sonunda silinir)
            WeaponVisual weaponVisual = blob.GetComponent<WeaponVisual>();
            if (weaponVisual == null) weaponVisual = blob.AddComponent<WeaponVisual>();
            weaponVisual.Setup(visual.ModelRenderer, character.WeaponModel, character.WeaponVisibleFromStart);

            switch (character.PassiveType)
            {
                case CharacterPassiveType.MoveSpeed:
                    // M22: hız artık ApplyMetaProgression'da meta bonusuyla birlikte tek seferde set ediliyor
                    // (aksi halde burada overwrite edilip meta bonusu kaybolurdu).
                    // 2026-11-03: Topik ayrıca büyüdükçe daha az yavaşlar (üs 0.3 → 0.27, ~%10) — "hantallaşmayan" karakter.
                    blob.GetComponent<BlobController>()?.ReduceTierSlowdownExponent(TopikSlowdownReduction);
                    break;
                case CharacterPassiveType.MagnetPull:
                    // 2026-11-03: eskiden +PassiveValue yarıçap ekliyordu (0.1 birim, hissedilmiyordu).
                    // GDD Karar 9 "Attract hattına +%25": menzil ve çekme hızı çarpanı. Vakum skili de bunun üstüne biner.
                    VacuumComponent vacuum = blob.AddComponent<VacuumComponent>();
                    vacuum.SetEffectMultiplier(1f + character.PassiveValue);
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

            // Skill efektleri blob'a component olarak eklenir; kalırsa bonuslar yeni run'a sızar.
            RemoveComponent<BlackHoleComponent>(blob);
            RemoveComponent<LifestealComponent>(blob);
            RemoveComponent<SpeedTrail>(blob);
            RemoveComponent<ArmorShield>(blob); // Zırh baloncuğu + dikenler (kalkan havuzunu ResetHealth sıfırlar)

            blob.GetComponent<BlobGrowth>()?.ResetGrowth();
            blob.GetComponent<BlobHealth>()?.ResetHealth();
            blob.GetComponent<BlobController>()?.ResetTierSlowdown();

            ResetBlobPosition(blob);
        }

        private static void RemoveComponent<T>(GameObject blob) where T : Component
        {
            T component = blob.GetComponent<T>();
            if (component != null) Destroy(component);
        }

        // İlk StartGame'deki konum spawn noktasıdır; sonraki run'lar oraya döner.
        private void ResetBlobPosition(GameObject blob)
        {
            if (!_hasBlobSpawnPosition)
            {
                _blobSpawnPosition = blob.transform.position;
                _hasBlobSpawnPosition = true;
                return;
            }

            blob.transform.position = _blobSpawnPosition;
            Rigidbody rigidbody = blob.GetComponent<Rigidbody>();
            if (rigidbody == null) return;
            rigidbody.position = _blobSpawnPosition;
            rigidbody.linearVelocity = Vector3.zero;
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

        // Duraklatma menüsünün "Devam" butonu: seviye atlama ekranı açıkken oyunu kart seçmeden sürdürmesin.
        public void ResumeFromPause()
        {
            if (CurrentState == GameState.Paused) ResumeGame();
        }

        // Duraklatma menüsünden "Ana Menü": run terk edilir (ödül/coin kredisi yok, ölüm sayılmaz). Dünya donuk kalır
        // (timeScale 0) — lobi arkasında düşman vurmasın; karakter seçilince StartGame her şeyi sıfırlar.
        public void ReturnToMenu()
        {
            if (CurrentState != GameState.Paused) return;
            ChangeState(GameState.Menu);
            Time.timeScale = 0f;
        }

        // Oyun sonu ekranından "Ana Menü": run zaten bitti (kredi MetaProgression'da verildi), sadece lobiye dönülür.
        public void ReturnToMenuFromRunEnd()
        {
            if (CurrentState != GameState.GameOver && CurrentState != GameState.RunComplete) return;
            ChangeState(GameState.Menu);
            Time.timeScale = 0f;
        }

        // Giriş ekranı (TitleScreen): run'ın her şeyi sıfırlanır (OnCharacterSelected dinleyicileri: şehir, düşman,
        // boss, coin...), blob varsayılan karakterle (Topik) başlangıçta durur. Menü durumunda kalır: blob hareket
        // etmez, düşman doğmaz, ama dünya akar (arabalar/yayalar) — arka plan canlı.
        public CharacterData DefaultCharacter => _defaultCharacter;

        public void EnterTitle()
        {
            if (CurrentState == GameState.Playing || CurrentState == GameState.LevelUp) return;
            ChangeState(GameState.Menu);
            Time.timeScale = 1f;
            FindAnyObjectByType<Systems.ScoreSystem>()?.ResetScore();
            SurvivalTime = 0f;
            if (_defaultCharacter != null) ApplyCharacter(_defaultCharacter);
        }

        // 2026-11-02: seviye atlama eskiden PauseGame kullanıyordu; artık duraklatma menüsü OnGamePaused'u
        // dinlediği için kart ekranının üstüne açılmasın diye ayrı bir state. OnGamePaused ateşlenmez.
        public void BeginLevelUp()
        {
            if (CurrentState != GameState.Playing) return;
            ChangeState(GameState.LevelUp);
            Time.timeScale = 0f;
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

        // Arka plana atılınca (veya iOS'ta bildirim/kontrol merkezi çekilince) oyun duraklar.
        // 2026-11-02: eskiden de duruyordu ama devam ettirecek hiçbir UI yoktu — oyun dönüşte donuk kalıyordu.
        // Artık PauseMenu OnGamePaused'u dinleyip "Devam" butonunu gösteriyor; otomatik devam bilinçli olarak yok.
        private void OnApplicationPause(bool paused)
        {
            if (paused) PauseGame();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_EDITOR
            if (!hasFocus) PauseGame();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
