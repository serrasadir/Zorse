using System;
using UnityEngine;

namespace BlobSurvivor.Systems.Social
{
    // Platform sosyal katmanı: oyuncu profili, arkadaşlar, sıralama + rozet. iOS'ta Game Center;
    // Google Play Games ve Steamworks aynı arayüze eklenir. Ad, fotoğraf ve arkadaş listesi platformun kendi profilinde.
    public interface ISocialService
    {
        bool IsAuthenticated { get; }
        string PlayerName { get; }   // platformdaki görünen ad
        string PlayerId { get; }     // platformun oyuncu kimliği
        Texture2D Avatar { get; }    // yüklenene kadar null
        void Authenticate(Action<bool> done);
        void UnlockAchievement(string id);
        void ReportProgress(string id, double percent); // 0..100; 100 = açılır
        void ReportScore(string leaderboardId, long value);
        void ShowAchievements();
        void ShowLeaderboards();
        void ShowProfile();          // ad + fotoğraf düzenleme
        void ShowAddFriends();
    }

#if UNITY_IOS && !UNITY_EDITOR
    // Unity'nin yerleşik Social API'si (GameCenterModule). Unity 6'da deprecated işaretli ama çalışıyor;
    // kaldırılırsa sadece bu sınıf Apple'ın GameKit eklentisine geçer. Profil/arkadaş ekranları Plugins/iOS/GameCenterBridge.mm.
#pragma warning disable CS0618
    public class GameCenterService : ISocialService
    {
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void LNC_ShowProfile();
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void LNC_AddFriends();

        public bool IsAuthenticated => UnityEngine.Social.localUser.authenticated;
        public string PlayerName => IsAuthenticated ? UnityEngine.Social.localUser.userName : null;
        public string PlayerId => IsAuthenticated ? UnityEngine.Social.localUser.id : null;
        public Texture2D Avatar => IsAuthenticated ? UnityEngine.Social.localUser.image : null;

        public void Authenticate(Action<bool> done)
        {
            UnityEngine.Social.localUser.Authenticate(ok =>
            {
                if (ok) UnityEngine.SocialPlatforms.GameCenter.GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
                done?.Invoke(ok);
            });
        }

        public void UnlockAchievement(string id) => ReportProgress(id, 100.0);

        public void ReportProgress(string id, double percent)
        {
            if (IsAuthenticated) UnityEngine.Social.ReportProgress(id, Math.Min(100.0, percent), null);
        }

        public void ReportScore(string leaderboardId, long value)
        {
            if (IsAuthenticated && value > 0) UnityEngine.Social.ReportScore(value, leaderboardId, null);
        }

        public void ShowAchievements() => UnityEngine.Social.ShowAchievementsUI();
        public void ShowLeaderboards() => UnityEngine.Social.ShowLeaderboardUI();
        public void ShowProfile() => LNC_ShowProfile();
        public void ShowAddFriends() => LNC_AddFriends();
    }
#pragma warning restore CS0618
#endif

    // Editor ve henüz bağlanmamış platformlar: hiçbir yere göndermez, Editor'da konsola yazar.
    public class OfflineSocialService : ISocialService
    {
        public bool IsAuthenticated => false;
        public string PlayerName => null;
        public string PlayerId => null;
        public Texture2D Avatar => null;
        public void Authenticate(Action<bool> done) => done?.Invoke(false);
        public void UnlockAchievement(string id) => Log($"Rozet açıldı: {id}");
        public void ReportProgress(string id, double percent) { }
        public void ReportScore(string leaderboardId, long value) => Log($"Skor: {leaderboardId} = {value}");
        public void ShowAchievements() => Log("Rozetler ekranı (Game Center sadece iOS cihazda)");
        public void ShowLeaderboards() => Log("Sıralama ekranı (Game Center sadece iOS cihazda)");
        public void ShowProfile() => Log("Profil ekranı (Game Center sadece iOS cihazda)");
        public void ShowAddFriends() => Log("Arkadaş ekleme (Game Center sadece iOS cihazda)");

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void Log(string message) => Debug.Log($"[Social] {message}");
    }
}
