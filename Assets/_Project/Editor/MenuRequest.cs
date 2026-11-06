using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Claude, Temp/menu.request'e (satır başına bir menü yolu) yazarak Tools menülerini sırayla çalıştırır.
    // Sonuç Temp/menu.done'a yazılır. Play Mode'da beklenir.
    [InitializeOnLoad]
    internal static class MenuRequest
    {
        // "set <BileşenTipi> <_alan> <değer>": sahnedeki ilk o tipteki bileşenin sayısal/bool alanını yazar
        // (Unity açıkken sahne dosyası dışarıdan düzenlenmez). Sonra Cmd+S.
        private static string SetField(string args)
        {
            string[] p = args.Split(' ');
            if (p.Length < 3) return "HATA\tset <Tip> <_alan> <değer>";
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb.GetType().Name != p[0]) continue;
                var so = new SerializedObject(mb);
                var prop = so.FindProperty(p[1]);
                if (prop == null) return $"YOK\t{p[0]}.{p[1]}";
                string old;
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Float:
                        old = prop.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        prop.floatValue = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture); break;
                    case SerializedPropertyType.Integer:
                        old = prop.intValue.ToString(); prop.intValue = int.Parse(p[2]); break;
                    case SerializedPropertyType.Boolean:
                        old = prop.boolValue.ToString(); prop.boolValue = bool.Parse(p[2]); break;
                    default: return $"DESTEKLENMİYOR\t{prop.propertyType}";
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mb.gameObject.scene);
                return $"OK\t{p[0]}.{p[1]}: {old} → {p[2]}";
            }
            return $"YOK\t{p[0]}";
        }

        // "prune <BileşenTipi> <_dizi>": sahnedeki ilk o tipteki bileşenin obje dizisinden boş/kayıp referansları siler.
        private static string PruneNulls(string args)
        {
            string[] p = args.Split(' ');
            if (p.Length < 2) return "HATA\tprune <Tip> <_dizi>";
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb.GetType().Name != p[0]) continue;
                var so = new SerializedObject(mb);
                var prop = so.FindProperty(p[1]);
                if (prop == null || !prop.isArray) return $"YOK\t{p[0]}.{p[1]}";
                int removed = 0;
                for (int i = prop.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty e = prop.GetArrayElementAtIndex(i);
                    if (e.propertyType != SerializedPropertyType.ObjectReference || e.objectReferenceValue != null) continue;
                    e.objectReferenceValue = null;
                    prop.DeleteArrayElementAtIndex(i);
                    removed++;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                if (removed > 0) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mb.gameObject.scene);
                return $"OK\t{p[0]}.{p[1]}: {removed} boş referans silindi, kalan {prop.arraySize}";
            }
            return $"YOK\t{p[0]}";
        }

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
                if (menu.StartsWith("set ")) { log.AppendLine(SetField(menu.Substring(4).Trim())); continue; }
                if (menu.StartsWith("prune ")) { log.AppendLine(PruneNulls(menu.Substring(6).Trim())); continue; }
                bool ok = EditorApplication.ExecuteMenuItem(menu);
                log.AppendLine($"{(ok ? "OK" : "YOK")}\t{menu}");
            }
            File.WriteAllText(DonePath, log.ToString());
        }
    }
}
