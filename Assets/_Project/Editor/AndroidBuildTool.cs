using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Android test APK'sı (Play Store değil, telefona elle kurmak için). Paket adı iOS'la aynı.
    // Gerekli: Unity Hub → Android Build Support (+ OpenJDK, SDK & NDK). İlk derleme platform değiştirdiği için uzun sürer.
    // "Telefona Kur (USB)": USB hata ayıklaması açık, kabloyla bağlı telefona kurup açar.
    public static class AndroidBuildTool
    {
        private const string OutputDir = "../zorse-android-build";
        private const string ApkName = "LeftNoCrumbs.apk";
        private const string PackageName = "com.zorsestudio.leftnocrumbs";
        private const string IconPath = "Assets/_Project/Art/AppIcon/AppIcon_Placeholder.png";

        [MenuItem("Tools/Blob.io/Android APK Üret")]
        private static void BuildApk() => Debug.Log("[AndroidBuildTool] " + Build(false));

        [MenuItem("Tools/Blob.io/Android: Telefona Kur (USB)")]
        private static void BuildAndRun() => Debug.Log("[AndroidBuildTool] " + Build(true));

        private static string Build(bool run)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                return "HATA: Android Build Support yüklü değil (Unity Hub → Add modules → Android Build Support).";

            EditorSceneManager.SaveOpenScenes();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();

            Directory.CreateDirectory(OutputDir);
            string path = Path.Combine(OutputDir, ApkName);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None
            });

            return $"{report.summary.result} errors={report.summary.totalErrors} apk={Path.GetFullPath(path)} " +
                   $"version={PlayerSettings.bundleVersion} code={PlayerSettings.Android.bundleVersionCode}";
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Zorse Studio";
            PlayerSettings.productName = "Left No Crumbs";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; // güncel telefonların hepsi
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.bundleVersionCode += 1; // eski kurulumun üstüne güncelleme olarak kurulsun
            EditorUserBuildSettings.buildAppBundle = false; // .aab değil, doğrudan kurulabilen .apk

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }
}
