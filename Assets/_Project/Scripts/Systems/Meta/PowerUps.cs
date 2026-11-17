using System;

namespace BlobSurvivor.Systems.Meta
{
    // Market'in kalıcı güçlendiricileri (VS "PowerUp" karşılığı; OYUN.md §9). Her run başında uygulanır
    // (GameManager.ApplyMetaProgression). Seviyeler kayıtta id ile tutulur: id değişirse alım kaybolur.
    // Eski 5 stat (hız, can, kütle, coin, XP) MetaStatType'ta kalır; Market ikisini aynı listede gösterir.
    public sealed class PowerUpDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int MaxLevel;
        public readonly int BaseCost;
        public readonly int CostStep;   // her alımda fiyat artışı
        public readonly float PerLevel;
        public readonly string PerLevelText;          // Market: "seviye başı" metni
        public readonly Func<float, string> TotalText; // Market: toplam etki ("şu an ...")

        public PowerUpDef(string id, string name, int maxLevel, int baseCost, int costStep, float perLevel,
            string perLevelText, Func<float, string> totalText)
        {
            Id = id;
            Name = name;
            MaxLevel = maxLevel;
            BaseCost = baseCost;
            CostStep = costStep;
            PerLevel = perLevel;
            PerLevelText = perLevelText;
            TotalText = totalText;
        }

        public int CostAt(int level) => BaseCost + level * CostStep;
    }

    public static class PowerUps
    {
        // Savunma
        public static readonly PowerUpDef Armor = new PowerUpDef("armor", "Zırh", 3, 400, 300, 1f,
            "Gelen her hasar −1", v => $"−{v:0} hasar");
        public static readonly PowerUpDef Regen = new PowerUpDef("regen", "Rejenerasyon", 3, 300, 250, 0.2f,
            "Saniyede +0,2 can", v => $"saniyede +{v:0.0} can");
        public static readonly PowerUpDef ThickSkin = new PowerUpDef("thick_skin", "Kalın Deri", 2, 300, 300, 0.2f,
            "Polis yutarken ısırık −%20", v => $"ısırık −%{v * 100f:0}");

        // Büyüme ve yeme
        public static readonly PowerUpDef WideMouth = new PowerUpDef("wide_mouth", "Geniş Ağız", 3, 250, 200, 0.05f,
            "Yeme alanı +%5", v => $"+%{v * 100f:0}");
        public static readonly PowerUpDef DigestResist = new PowerUpDef("digest_resist", "Sindirim Direnci", 3, 300, 250, 0.1f,
            "Büyüyünce yavaşlama −%10", v => $"−%{v * 100f:0}");
        public static readonly PowerUpDef Magnet = new PowerUpDef("magnet", "Mıknatıs", 3, 200, 150, 1f,
            "Coin ve kalbi 1 birimden çeker", v => $"{v:0} birim");

        public static readonly PowerUpDef[] All = { Armor, Regen, ThickSkin, WideMouth, DigestResist, Magnet };
    }

    // Oynayarak açılan skiller (VS gibi): koşul gerçekleşince kart havuzuna girer. Listede olmayan skill baştan açık.
    // Karakterin başlangıç ve favori skill'i o karakterle kilitli olsa da çıkar (UpgradeSystem).
    // İlerleme MetaProgression'da stat başına tutulur (run içi en iyi ya da tüm run'lar toplamı); UnlockTracker sayar.
    public enum UnlockStat
    {
        CarsEatenInRun,    // bir run'da yenen hareketli araba (en iyi)
        ReachedDev,        // Dev boyuna ulaşıldı (1)
        SurviveSeconds,    // bir run'da hayatta kalma (en iyi, sn)
        PoliceKillsTotal,  // silahla indirilen polis (toplam)
        BossLowHealth,     // can %20'nin altındayken boss yenildi (1)
        TankKills,         // yenilen tank (toplam)
        PoliceEatenInRun,  // bir run'da yutulan polis (en iyi)
        FinalBossEaten,    // helikopter yutuldu (1)
    }

    public static class SkillUnlocks
    {
        public struct Entry
        {
            public string Id;
            public UnlockStat Stat;
            public int Target;
            public string Text;
        }

        public static readonly Entry[] Locked =
        {
            new Entry { Id = "geri_donusum", Stat = UnlockStat.CarsEatenInRun, Target = 10, Text = "Bir run'da 10 araba ye" },
            new Entry { Id = "hizli_metabolizma", Stat = UnlockStat.ReachedDev, Target = 1, Text = "Dev boyuna ulaş" },
            new Entry { Id = "kaygan_zemin", Stat = UnlockStat.SurviveSeconds, Target = 600, Text = "Bir run'da 10 dakika hayatta kal" },
            new Entry { Id = "tetikci", Stat = UnlockStat.PoliceKillsTotal, Target = 300, Text = "Toplam 300 polis indir" },
            new Entry { Id = "yara_ofkesi", Stat = UnlockStat.BossLowHealth, Target = 1, Text = "Canın %20'nin altındayken bir boss yen" },
            new Entry { Id = "yer_sarsintisi", Stat = UnlockStat.TankKills, Target = 1, Text = "Bir tank yen" },
            new Entry { Id = "sindirim_gazi", Stat = UnlockStat.PoliceEatenInRun, Target = 30, Text = "Bir run'da 30 polis yut" },
            new Entry { Id = "bolunme", Stat = UnlockStat.FinalBossEaten, Target = 1, Text = "Helikopteri yut" },
        };

        public static bool IsLocked(string id) => TryGet(id, out _);

        public static bool TryGet(string id, out Entry entry)
        {
            foreach (Entry e in Locked)
                if (e.Id == id) { entry = e; return true; }
            entry = default;
            return false;
        }

        // Market'te ilerleme metni: "142/300", süre için "6:20/10:00"; tek seferlikler boş.
        public static string ProgressText(Entry e, int value)
        {
            if (e.Target <= 1) return "";
            value = System.Math.Min(value, e.Target);
            if (e.Stat == UnlockStat.SurviveSeconds) return $"{value / 60}:{value % 60:00}/{e.Target / 60}:{e.Target % 60:00}";
            return $"{value}/{e.Target}";
        }
    }
}
