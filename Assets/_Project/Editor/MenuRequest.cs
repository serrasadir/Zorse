using System.IO;
using System.Text;
using UnityEditor;

namespace BlobSurvivor.EditorTools
{
    // Claude, Temp/menu.request'e (satır başına bir menü yolu) yazarak Tools menülerini sırayla çalıştırır.
    // Sonuç Temp/menu.done'a yazılır. Play Mode'da beklenir.
    [InitializeOnLoad]
    internal static class MenuRequest
    {
        private const string RequestPath = "Temp/menu.request";
        private const string DonePath = "Temp/menu.done";
        private static double s_nextPoll;

        static MenuRequest() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_nextPoll) return;
            s_nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            string[] menus = File.ReadAllLines(RequestPath);
            File.Delete(RequestPath);
            var log = new StringBuilder();
            foreach (string raw in menus)
            {
                string menu = raw.Trim();
                if (menu.Length == 0) continue;
                bool ok = EditorApplication.ExecuteMenuItem(menu);
                log.AppendLine($"{(ok ? "OK" : "YOK")}\t{menu}");
            }
            File.WriteAllText(DonePath, log.ToString());
        }
    }
}
