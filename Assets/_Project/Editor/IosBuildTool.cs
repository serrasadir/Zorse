using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Unity açıkken komut satırından build alınamıyor (proje kilitli). Bu araç Xcode projesini proje
    // dışına üretir; Temp/build_ios.request dosyası varsa bir sonraki script yüklemesinde kendiliğinden
    // başlar, sonucu Temp/build_ios.result dosyasına yazar. Kalan imza/kurulum Xcode tarafında yapılır.
    public static class IosBuildTool
    {
        private const string RequestPath = "Temp/build_ios.request";
        private const string ResultPath = "Temp/build_ios.result";
        private const string OutputPath = "../zorse-ios-build";

        [InitializeOnLoadMethod]
        private static void RunPendingRequest()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    File.WriteAllText(ResultPath, "ABORTED: Play Mode açık");
                    File.Delete(RequestPath);
                    return;
                }

                File.Delete(RequestPath);
                Build();
            };
        }

        [MenuItem("Tools/Blob.io/iOS Xcode Projesi Üret")]
        private static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.Development
            });

            string result = $"{report.summary.result} errors={report.summary.totalErrors}";
            File.WriteAllText(ResultPath, result);
            Debug.Log($"[IosBuildTool] {result}");
        }
    }
}
