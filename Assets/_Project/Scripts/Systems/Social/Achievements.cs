namespace BlobSurvivor.Systems.Social
{
    // Game Center (App Store Connect) kimlikleri. Rozet adları, koşulları ve puanları OYUN.md "Rozetler ve sıralama"da;
    // id değişirse App Store Connect'teki kayıt da değişmeli (yayınlanmış id silinemez).
    public static class Achievements
    {
        public const string FirstRun = "lnc_first_run";             // Afiyet Olsun
        public const string QuickDeath = "lnc_quick_death";         // Aperatif
        public const string ReachDev = "lnc_reach_dev";             // Şehir Bana Dar
        public const string FirstWin = "lnc_first_win";             // Kırıntı Bırakmadın
        public const string NoDamage = "lnc_no_damage";             // Kılı Bile Kıpırdamadı

        public const string Tank = "lnc_tank";                      // Tank Tatlısı
        public const string Helicopter = "lnc_helicopter";          // Pervaneli Dürüm
        public const string PoliceFeast = "lnc_police_feast";       // Karakol Kahvaltısı
        public const string RiotPolice = "lnc_riot_police";         // Kalkan mı? Çatal Bıçak!
        public const string MotorPolice = "lnc_motor_police";       // Kask Zorunluymuş
        public const string RobotDog = "lnc_robot_dog";             // Mama Saati

        public const string BlackHole = "lnc_black_hole";           // Midede Olay Ufku
        public const string GasKills = "lnc_gas_kills";             // Kimse Görmedi
        public const string SlipDogs = "lnc_slip_dogs";             // Dikkat Islak Zemin
        public const string Split = "lnc_split";                    // Ben, Ben ve Yine Ben
        public const string FullyLoaded = "lnc_fully_loaded";       // Full Paket
        public const string FerrisWheel = "lnc_ferris_wheel";       // Çark-ı Felek

        public const string AllCharacters = "lnc_all_characters";   // Aile Sofrası
        public const string WinAll = "lnc_win_all";                 // Beşi Bir Yerde

        public const string Pedestrians = "lnc_pedestrians";        // Nüfus Sayımı Bozuldu
        public const string Cars = "lnc_cars";                      // Trafik Rahatladı
        public const string Coins = "lnc_coins";                    // Kumbara Patladı

        public const int Count = 27; // yukarıdakiler + 5 karakter rozeti; App Store Connect'teki sayı

        // Karakterle kazanma rozetleri: CharacterData.SaveId → id.
        public static string WinWith(string saveId) => string.IsNullOrEmpty(saveId) ? null : "lnc_win_" + saveId.ToLowerInvariant();

        // Hedefler
        public const int QuickDeathSeconds = 60;
        public const int NoDamageSeconds = 600;
        public const int PoliceFeastTarget = 100;
        public const int GasKillsTarget = 50;
        public const int SlipDogsTarget = 20;
        public const int PedestriansTarget = 1000;
        public const int CarsTarget = 500;
        public const int CoinsTarget = 100000;
    }

    public static class Leaderboards
    {
        public const string HighScore = "lnc_high_score";   // tamsayı, büyük olan iyi
        public const string FastestWin = "lnc_fastest_win"; // geçen süre, salise (1/100 sn), küçük olan iyi
    }
}
