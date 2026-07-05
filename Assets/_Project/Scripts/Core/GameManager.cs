using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        LevelUp,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private CharacterData _defaultCharacter;

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

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            SurvivalTime += Time.deltaTime;
            GameEvents.RaiseSurvivalTimeUpdated(SurvivalTime);
        }

        public void StartGame()
        {
            StartGame(_defaultCharacter);
        }

        public void StartGame(CharacterData character)
        {
            SurvivalTime = 0f;
            ChangeState(GameState.Playing);

            if (character != null)
                ApplyCharacter(character);
        }

        private void ApplyCharacter(CharacterData character)
        {
            GameObject blob = GameObject.FindWithTag("Blob");
            if (blob == null) return;

            switch (character.PassiveType)
            {
                case CharacterPassiveType.MoveSpeed:
                    blob.GetComponent<BlobController>()?.SetSpeedMultiplier(1f + character.PassiveValue);
                    break;
                case CharacterPassiveType.MagnetPull:
                    MagnetComponent magnet = blob.GetComponent<MagnetComponent>() ?? blob.AddComponent<MagnetComponent>();
                    magnet.IncreaseRadius(character.PassiveValue);
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
