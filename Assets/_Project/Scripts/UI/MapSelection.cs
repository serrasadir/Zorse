using BlobSurvivor.Systems.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlobSurvivor.UI
{
    // Haritalar = sahneler: 1. şehir (GameScene), 2. Crownreach (CrownreachScene). Lobideki HARİTA düğmesi diğerini yükler,
    // yeni sahne giriş ekranını atlayıp lobiyi açar. Crownreach, helikopter bir kez yutulunca açılır (Bölünme skill'iyle aynı
    // koşul, DEV "tüm karakterleri aç" da açar).
    public static class MapSelection
    {
        public const string CityScene = "GameScene";
        public const string CrownreachScene = "CrownreachScene";
        private static readonly string[] Order = { CityScene, CrownreachScene };

        public static bool OpenLobbyOnLoad;

        private static string Active => SceneManager.GetActiveScene().name;
        public static bool IsCity => Active == CityScene;
        public static bool IsCrownreach => Active == CrownreachScene;

        public static bool CrownreachUnlocked =>
            MetaProgression.Instance == null || MetaProgression.Instance.IsSkillUnlocked("bolunme");

        public static void Switch()
        {
            OpenLobbyOnLoad = true;
            int i = System.Array.IndexOf(Order, Active);
            for (int step = 1; step <= Order.Length; step++)
            {
                string next = Order[(i + step + Order.Length) % Order.Length];
                if (Application.CanStreamedLevelBeLoaded(next)) { SceneManager.LoadScene(next); return; } // build'de olmayan atlanır
            }
        }
    }
}
