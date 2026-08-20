using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BlobSurvivor.Systems.Meta
{
    [Serializable]
    public class RunRecord
    {
        public string endReason;
        public float durationSeconds;
        public int finalScore;
        public int coinsEarned;
        public List<string> upgradeChoiceIds = new List<string>();
    }

    [Serializable]
    public class AnalyticsSaveData
    {
        public int totalRuns;
        public List<RunRecord> runs = new List<RunRecord>();
    }

    // B19 (GDD_v2.md §13): yerel JSON kayıt — 3. parti analytics servisi (GameAnalytics/Unity Analytics)
    // değil, sadece run geçmişini diske yazan/okuyan minimal iskele. Servis entegrasyonu Sprint 7+ işi.
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private const string FileName = "analytics.json";
        private const int MaxStoredRuns = 100;

        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SaveRun(RunRecord record)
        {
            AnalyticsSaveData data = Load();
            data.runs.Add(record);
            data.totalRuns++;

            if (data.runs.Count > MaxStoredRuns)
                data.runs.RemoveRange(0, data.runs.Count - MaxStoredRuns);

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SaveSystem] Kayıt yazılamadı: {e.Message}");
#endif
            }
        }

        public AnalyticsSaveData Load()
        {
            if (!File.Exists(FilePath))
                return new AnalyticsSaveData();

            try
            {
                string json = File.ReadAllText(FilePath);
                AnalyticsSaveData data = JsonUtility.FromJson<AnalyticsSaveData>(json);
                return data ?? new AnalyticsSaveData();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SaveSystem] Kayıt okunamadı: {e.Message}");
#endif
                return new AnalyticsSaveData();
            }
        }
    }
}
