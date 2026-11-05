using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // 2026-11-02: TestFlight'a yüklenecek Xcode projesini üretir (Development değil, Release).
    // Her çalıştırmada iOS build numarasını 1 artırır — App Store Connect aynı numarayı ikinci kez kabul etmez —
    // ve sürümü VersionPrefix + build no yapar (0.1.13 = build 13).
    // Arşiv + yükleme Unity dışında xcodebuild ile yapılır (bkz. CLAUDE.md, Editor Araçları).
    // Temp/testflight.request varsa script yüklemesinde kendiliğinden başlar, sonucu Temp/testflight.result'a yazar.
    // Not: istek sadece bir script derlemesinden sonra okunur; derleme yoksa menüden çalıştır.
    public static class TestFlightBuildTool
    {
        private const string RequestPath = "Temp/testflight.request";
        private const string ResultPath = "Temp/testflight.result";
        private const string OutputPath = "../zorse-ios-build";
        private const string BundleId = "com.zorsestudio.leftnocrumbs";
        private const string TeamId = "66ZS86R987";
        private const string IconPath = "Assets/_Project/Art/AppIcon/AppIcon_Placeholder.png";
        // Sürüm = önek + build no (0.1.12 = build 12). Canlıya çıkınca önek değişir (ör. "1.0."); elle girilmez.
        private const string VersionPrefix = "0.1.";

        [InitializeOnLoadMethod]
        private static void RunPendingRequest()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                File.Delete(RequestPath);
                File.WriteAllText(ResultPath, EditorApplication.isPlayingOrWillChangePlaymode ? "ABORTED: Play Mode açık" : Build());
            };
        }

        [MenuItem("Tools/Blob.io/TestFlight Xcode Projesi Üret")]
        private static void BuildFromMenu() => Debug.Log("[TestFlightBuildTool] " + Build());

        private static string Build()
        {
            EditorSceneManager.SaveOpenScenes();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });

            return $"{report.summary.result} errors={report.summary.totalErrors} version={PlayerSettings.bundleVersion} build={PlayerSettings.iOS.buildNumber}";
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Zorse Studio";
            PlayerSettings.productName = "Left No Crumbs"; // 2026-11-02 (Serra): oyunun adı
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            int.TryParse(PlayerSettings.iOS.buildNumber, out int build);
            PlayerSettings.iOS.buildNumber = (build + 1).ToString();
            PlayerSettings.bundleVersion = VersionPrefix + (build + 1); // 2026-11-05: elle güncellemek unutuluyordu

            ApplyIcon();
        }

        // Geçici ikon (gerçek ikon gelene kadar). App Store 1024 "marketing" ikonu olmadan yüklemeyi reddeder;
        // tüm iOS ikon türlerine aynı görsel verilir, Unity boyutları kendisi üretir.
        private static void ApplyIcon()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;

            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (PlatformIcon platformIcon in icons)
                    platformIcon.SetTexture(icon);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
            }
        }

        // Şifreleme beyanı: oyun standart HTTPS dışında şifreleme kullanmıyor. Bu anahtar yoksa App Store Connect
        // her build'de "Export Compliance" sorusunu sorar ve build test için bekler.
        [PostProcessBuild]
        private static void AddExportCompliance(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            // Ana ekran ~12 karakter gösteriyor; "Left No Crumbs" (14) kesilirdi. App Store adı etkilenmez.
            plist.root.SetString("CFBundleDisplayName", "No Crumbs");
            plist.WriteToFile(plistPath);
        }
    }
}
