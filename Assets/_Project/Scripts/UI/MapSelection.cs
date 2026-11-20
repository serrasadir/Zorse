using BlobSurvivor.Systems.Meta;
using UnityEngine.SceneManagement;

namespace BlobSurvivor.UI
{
    // İki harita = iki sahne: şehir (GameScene) ve kale (CastleScene). Lobideki HARİTA düğmesi diğer sahneyi yükler,
    // yeni sahne giriş ekranını atlayıp lobiyi açar. Kale, helikopter bir kez yutulunca açılır (Bölünme skill'iyle
    // aynı koşul, DEV "tüm karakterleri aç" da açar).
    public static class MapSelection
    {
        public const string CityScene = "GameScene";
        public const string CastleScene = "CastleScene";

        public static bool OpenLobbyOnLoad;

        public static bool IsCastle => SceneManager.GetActiveScene().name == CastleScene;

        public static bool CastleUnlocked =>
            MetaProgression.Instance == null || MetaProgression.Instance.IsSkillUnlocked("bolunme");

        public static void Switch()
        {
            OpenLobbyOnLoad = true;
            SceneManager.LoadScene(IsCastle ? CityScene : CastleScene);
        }
    }
}
