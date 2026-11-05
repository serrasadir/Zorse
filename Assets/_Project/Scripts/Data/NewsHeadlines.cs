using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Data
{
    // 2026-11-03 (Serra, Plague Inc.'teki haber bandı gibi): üstte geçen haber başlıkları.
    // Blob küçükken dünya umursamaz, saçma sapan dünya haberleri döner; büyüdükçe blob'la ilgili haber oranı artar.
    // Metinler sadece burada — yeni haber eklemek kod gerektirmez.
    [CreateAssetMenu(fileName = "NewsHeadlines", menuName = "BlobSurvivor/News Headlines")]
    public class NewsHeadlines : ScriptableObject
    {
        [System.Serializable]
        public class TierHeadlines
        {
            public BlobTier Tier;
            [TextArea(1, 2)] public string[] Headlines;
        }

        [Tooltip("Blob'la ilgisi olmayan komik dünya haberleri.")]
        [SerializeField, TextArea(1, 2)] private string[] _worldNews;

        [Tooltip("Blob'la ilgili haberler, tier'a göre. Tier'ın listesi yoksa bir alttakinden seçilir.")]
        [SerializeField] private TierHeadlines[] _blobNews;

        [Tooltip("Sıradaki haberin blob'la ilgili olma ihtimali — sıra: Tiny, Small, Medium, Large, Giant.")]
        [SerializeField] private float[] _blobNewsChanceByTier = { 0f, 0.2f, 0.45f, 0.7f, 0.9f, 0.95f };

        public string[] WorldNews => _worldNews;

        public float BlobNewsChance(BlobTier tier)
        {
            int index = (int)tier - 1;
            if (_blobNewsChanceByTier == null || index < 0 || index >= _blobNewsChanceByTier.Length) return 0f;
            return _blobNewsChanceByTier[index];
        }

        // Tam o tier'ın listesi; yoksa en yakın alt tier'ınki. Hiçbiri yoksa null.
        public string[] BlobNewsFor(BlobTier tier)
        {
            if (_blobNews == null) return null;
            string[] best = null;
            int bestTier = 0;
            foreach (TierHeadlines entry in _blobNews)
            {
                if (entry == null || entry.Headlines == null || entry.Headlines.Length == 0) continue;
                int t = (int)entry.Tier;
                if (t <= (int)tier && t > bestTier)
                {
                    best = entry.Headlines;
                    bestTier = t;
                }
            }
            return best;
        }

#if UNITY_EDITOR
        // Sadece kurulum aracı (NewsTickerBuilder) kullanır.
        public void EditorSetContent(string[] worldNews, TierHeadlines[] blobNews)
        {
            _worldNews = worldNews;
            _blobNews = blobNews;
        }
#endif
    }
}
