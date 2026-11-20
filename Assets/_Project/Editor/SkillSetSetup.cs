using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Systems;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → Skill Setini Kur: yeni skill seti (design/gdd/skill-sistemi.md). Efekt ve skill asset'lerini
    // oluşturur/günceller (tür, 5 seviye, kart metinleri) ve açık sahnedeki UpgradeSystem'in kart havuzuna yazar.
    // GameScene açıkken çalıştır; sahneyi kaydeder. Tekrar çalıştırmak güvenli.
    public static class SkillSetSetup
    {
        private const string Folder = "Assets/_Project/Data/Upgrades/";

        private struct Skill
        {
            public string Asset, Fx, Id, Name;
            public SkillKind Kind;
            public Type Effect;
            public int Weight, MaxLevel;
            public float PerLevel;
            public string[] Levels;
        }

        private static Skill S(string asset, string fx, Type effect, string id, string name, SkillKind kind, string[] levels,
            float perLevel = 0f, int weight = 10) =>
            new Skill { Asset = asset, Fx = fx, Effect = effect, Id = id, Name = name, Kind = kind, Levels = levels,
                        PerLevel = perLevel, Weight = weight, MaxLevel = levels.Length };

        private static readonly Skill[] Skills =
        {
            // 1. Sindirim ve Büyüme
            S("Upgrade_Sindirim", "FX_Sindirim", typeof(SindirimEffect), "Sindirim", "Sindirim", SkillKind.Passive, new[] {
                "Yediğin her şeyden kütle +%10.", "Kütle +%20.", "AĞIR LOKMA: kendi boyundaki lokmalar +%25 kütle.",
                "Kütle +%30.", "OBUR: her 10. lokma ×3 kütle." }),
            S("Upgrade_Vacuum", "FX_VacuumEffect", typeof(VacuumEffect), "vacuum", "Vakum", SkillKind.Active, new[] {
                "Yiyebileceklerini kendine çeker.", "Çekme menzili artar.", "GENİŞ AĞIZ: yutma alanın da büyür.",
                "Menzil artar, çekme hızı ×1.5.", "Coin ve kalpleri de çeker." }),
            S("Upgrade_GeriDonusum", "FX_GeriDonusum", typeof(RecycleEffect), "geri_donusum", "Geri Dönüşüm", SkillKind.Passive, new[] {
                "Eşya yedikçe can: lokma başına +1 (yayalar sayılmaz).", "Lokma başına +2 can.", "HURDACI: araba yutunca +25 can.",
                "Lokma başına +3 can.", "Can doluyken fazlası 20'lik geçici kalkan olur." }),
            S("Upgrade_HizliMetabolizma", "FX_HizliMetabolizma", typeof(MetabolismEffect), "hizli_metabolizma", "Hızlı Metabolizma", SkillKind.Passive, new[] {
                "Büyüdükçe gelen yavaşlama −%15.", "Yavaşlama −%30.", "Tier atlayınca 3 sn +%30 hız.",
                "Yavaşlama −%45.", "Yavaşlama −%60, büyük lokmadan sonra 1 sn hız." }),
            // 2. Hareket
            S("Upgrade_SpeedBoost", "FX_SpeedBoost", typeof(SpeedBoostEffect), "speed_boost", "Hız", SkillKind.Passive, new[] {
                "Hareket hızı +%20. Kaykay!", "Hız +%30.", "ALEVLİ KAYKAY: hız +%38.",
                "Hız +%45.", "ROKET: 2 sn durmadan gidince +%20 hız." }),
            S("Upgrade_KayganZemin", "FX_KayganZemin", typeof(SlipTrailEffect), "kaygan_zemin", "Kaygan Zemin", SkillKind.Active, new[] {
                "Arkanda kaygan iz: içindeki düşmanlar %30 yavaşlar.", "İz daha uzun kalır (3 sn).", "Robot köpekler izde kayar.",
                "İzdekiler %50 yavaşlar.", "ASİT İZ: yutamadıklarına saniyede 5 hasar." }),
            // 3. Saldırı
            S("Upgrade_Weapon", "FX_WeaponUpgrade", typeof(WeaponUpgradeEffect), "weapon", "Sopa", SkillKind.Active, new[] {
                "Vuruş hasarı +5, menzil +0.5.", "Vuruş yayı 180°.", "Arkaya da vurur, hasar +5.",
                "Vuruş hızı %30 artar.", "SON FORM: tam tur vuruş (360°), hasar ×1.5." }, perLevel: 5f),
            S("Upgrade_Firework", "FX_Firework", typeof(FireworkEffect), "firework", "Havai Fişek", SkillKind.Active, new[] {
                "Uzaktaki hedefe ağır roket: 2 sn'de bir, 25 hasar. Önce boss ve elit.", "Roket hasarı +8.", "Atış hızı %30 artar.",
                "Boss ve elite ×1.5 hasar, roket hasarı +8.", "SON FORM: her atışta 2 roket, ayrı düşmanlara." }, perLevel: 8f),
            S("Upgrade_YerSarsintisi", "FX_YerSarsintisi", typeof(GroundQuakeEffect), "yer_sarsintisi", "Yer Sarsıntısı", SkillKind.Active, new[] {
                "Darbe aldıkça dolar: her 15 hasarda yere vurur, çevreye 15 hasar.", "Sarsıntı hasarı 22.", "Sarsıntı 0.5 sn sersemletir.",
                "Her 12 hasarda bir, alan genişler.", "DEPREM: her 3. sarsıntı 3 kat hasar, 2 kat alan." }),
            S("Upgrade_SindirimGazi", "FX_SindirimGazi", typeof(DigestGasEffect), "sindirim_gazi", "Sindirim Gazı", SkillKind.Active, new[] {
                "Çevrende gaz: içindeki tüm düşmanlara saniyede 4 hasar.", "Gaz hasarı saniyede 6.", "Gazdakiler %20 yavaşlar.",
                "Saniyede 8 hasar, gaz alanı büyür.", "Gazda ölenin kütlesinin yarısı sana gelir." }),
            S("Upgrade_Tetikci", "FX_Tetikci", typeof(TriggerFingerEffect), "tetikci", "Tetikçi", SkillKind.Passive, new[] {
                "Sopa ve Havai Fişek %8 daha sık, Yer Sarsıntısı daha az darbede dolar.", "Bekleme −%16.", "Her 5. atış hemen tekrarlanır.",
                "Bekleme −%24.", "Her öldürmede bekleme 0.2 sn kısalır." }),
            // 4. Savunma
            S("Upgrade_Armor", "FX_DamageReduction", typeof(ArmorEffect), "damage_reduction", "Zırh", SkillKind.Passive, new[] {
                "Her darbeden 1 hasar düşer.", "Her darbeden 2 hasar düşer.", "Her darbeden 3 düşer. Darbe sonrası dokunulmazlık uzar.",
                "Her darbeden 4 hasar düşer.", "Her darbeden 5 düşer. Darbe alınca etraftakiler 2 kat uzağa savrulur." }),
            S("Upgrade_MaxHealth", "FX_HealthBoost", typeof(HealthBoostEffect), "health_boost", "Can Artışı", SkillKind.Passive, new[] {
                "Max can +15.", "Max can +30.", "Kalpler %50 daha fazla can verir.",
                "Max can +45.", "Max can +60, kalpler 2 kat can verir." }),
            S("Upgrade_Regen", "FX_RegenBoost", typeof(RegenBoostEffect), "regen_boost", "Rejenerasyon", SkillKind.Passive, new[] {
                "Saniyede 0.5 can yenilenir.", "Saniyede 1 can.", "3 sn hasar almazsan yenilenme 2 katı.",
                "Saniyede 1.5 can.", "Saniyede 2 can, tier atlayınca canın %20'si dolar." }),
            S("Upgrade_YaraOfkesi", "FX_YaraOfkesi", typeof(RageEffect), "yara_ofkesi", "Yara Öfkesi", SkillKind.Passive, new[] {
                "Can %40'ın altındayken hız +%15.", "Öfkeliyken hasar da +%15.", "Öfke eşiği %50.",
                "Öfkeliyken hız +%25, hasar +%25.", "Öfkeliyken ısırık hasarı almazsın." }),
            // 5. Nadir / Epik
            S("Upgrade_Bolunme", "FX_Bolunme", typeof(SplitEffect), "bolunme", "Bölünme", SkillKind.Rare, new[] {
                "Ölümcül darbede ölmek yerine bölünürsün: kütlenin %30'u gider, bir tier düşersin, 2 sn dokunulmazsın. Run'da bir kez." },
                weight: 3),
        };

        // HUD kısaltmaları (hud.md §4.3), skill id'sine göre. Kara Delik evrim asset'i ayrıca yazılır.
        private static readonly Dictionary<string, string> Abbr = new Dictionary<string, string>
        {
            { "vacuum", "VK" }, { "kaygan_zemin", "KZ" }, { "weapon", "SP" }, { "firework", "HF" },
            { "yer_sarsintisi", "YS" }, { "sindirim_gazi", "SG" }, { "damage_reduction", "ZR" },
            { "Sindirim", "SN" }, { "geri_donusum", "GD" }, { "hizli_metabolizma", "HM" }, { "speed_boost", "HZ" },
            { "tetikci", "TT" }, { "health_boost", "CA" }, { "regen_boost", "RJ" }, { "yara_ofkesi", "YÖ" },
            { "bolunme", "BÖ" },
        };
        private const string BaseWeaponId = "weapon"; // Sopa
        private const string KaraDelikAsset = "Upgrade_KaraDelik";

        [MenuItem("Tools/Blob.io/Skill Setini Kur")]
        public static void Run()
        {
            var pool = new List<UpgradeData>();
            foreach (Skill s in Skills)
            {
                UpgradeEffect fx = LoadOrCreate(Folder + s.Fx + ".asset", s.Effect) as UpgradeEffect;
                var data = LoadOrCreate(Folder + s.Asset + ".asset", typeof(UpgradeData)) as UpgradeData;
                if (fx == null || data == null) { Debug.LogError($"[SkillSetSetup] {s.Name} kurulamadı."); continue; }
                if (!Abbr.TryGetValue(s.Id, out string abbr)) Debug.LogWarning($"[SkillSetSetup] {s.Name} için kısaltma yok.");
                data.EditorSetup(s.Id, s.Name, s.Kind, s.Weight, fx, s.MaxLevel, s.PerLevel, s.Levels, abbr, s.Id == BaseWeaponId);
                EditorUtility.SetDirty(data);
                pool.Add(data);
            }
            var karaDelik = AssetDatabase.LoadAssetAtPath<UpgradeData>(Folder + KaraDelikAsset + ".asset");
            if (karaDelik == null) Debug.LogWarning("[SkillSetSetup] " + KaraDelikAsset + " bulunamadı.");
            List<SkillEvolutionData> evolutions = SetupEvolutions(pool, karaDelik);
            AssetDatabase.SaveAssets();

            UpgradeSystem system = UnityEngine.Object.FindAnyObjectByType<UpgradeSystem>(FindObjectsInactive.Include);
            if (system == null)
            {
                Debug.LogWarning("[SkillSetSetup] Asset'ler hazır ama sahnede UpgradeSystem yok: GameScene'i açıp tekrar çalıştır.");
                return;
            }
            var so = new SerializedObject(system);
            SerializedProperty list = so.FindProperty("_allUpgrades");
            list.arraySize = pool.Count;
            for (int i = 0; i < pool.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            so.ApplyModifiedProperties();

            EvolutionSystem evoSystem = UnityEngine.Object.FindAnyObjectByType<EvolutionSystem>(FindObjectsInactive.Include);
            if (evoSystem != null)
            {
                var evoSo = new SerializedObject(evoSystem);
                SerializedProperty evoList = evoSo.FindProperty("_evolutions");
                evoList.arraySize = evolutions.Count;
                for (int i = 0; i < evolutions.Count; i++) evoList.GetArrayElementAtIndex(i).objectReferenceValue = evolutions[i];
                evoSo.ApplyModifiedProperties();
            }
            else Debug.LogWarning("[SkillSetSetup] Sahnede EvolutionSystem yok, evrimler bağlanmadı.");
            EditorSceneManager.MarkSceneDirty(system.gameObject.scene);
            EditorSceneManager.SaveScene(system.gameObject.scene);
            Debug.Log($"[SkillSetSetup] {pool.Count} skill kart havuzunda (3 aktif + 3 pasif slot). Sahne kaydedildi.");
        }

        // Evrimler (SKILLS.md §3): A max + B en az EvoPartnerLevel. Kara Delik'in efekt asset'i elle kuruldu, metni ve koşulu burada yazılır.
        private const int EvoPartnerLevel = 1;

        private static List<SkillEvolutionData> SetupEvolutions(List<UpgradeData> pool, UpgradeData karaDelik)
        {
            var list = new List<SkillEvolutionData>();
            // Kara Delik: asset'leri elle kuruldu, kendi evrim sahnesi var (ortak an oynamaz).
            if (karaDelik != null)
            {
                karaDelik.EditorSetupEvolution("kara_delik", "Kara Delik",
                    "Değmeden yutarsın: çevrendeki yiyebildiğin her şey sana çekilir. Polisler hariç.", karaDelik.Effect, "KD");
                EditorUtility.SetDirty(karaDelik);
            }
            AddLink(list, pool, "Evolution_Karadelik", "vacuum", "Sindirim", karaDelik, new Color(0.35f, 0.25f, 0.9f), true);

            Evolution(list, pool, "LambaDiregi", typeof(LampPostEffect), "lamba_diregi", "Lamba Direği", "LD",
                "Dalı bırak, direği sök. Tam tur, sert itme; vurup öldürdüğün düşman ağzına uçar.",
                "weapon", "yara_ofkesi", new Color(1f, 0.85f, 0.35f));
            Evolution(list, pool, "BayramGecesi", typeof(FestivalNightEffect), "bayram_gecesi", "Bayram Gecesi", "BG",
                "Her roket isabette 3 misket rokete bölünür, misketler yakındaki düşmanlara saçılır.",
                "firework", "tetikci", new Color(1f, 0.45f, 0.85f));
            Evolution(list, pool, "SabunSorfu", typeof(SoapSurfEffect), "sabun_sorfu", "Sabun Sörfü", "SS",
                "Kendi izinde %30 hızlı kayarsın. İzdeki düşman kayar, savrulur, 10 hasar alır ve sersemler.",
                "kaygan_zemin", "speed_boost", new Color(0.35f, 0.9f, 0.95f));
            Evolution(list, pool, "HurdaKalkani", typeof(ScrapShieldEffect), "hurda_kalkani", "Hurda Kalkanı", "HK",
                "Etrafında hurda parçaları döner: değdiği düşmana vurur, bir darbeyi tamamen yutup kırılır. Her araba yeni parça (en fazla 4).",
                "geri_donusum", "damage_reduction", new Color(1f, 0.6f, 0.3f));
            Evolution(list, pool, "Gegirme", typeof(BurpEffect), "gegirme", "Geğirme", "GĞ",
                "6 sn'de bir koca geğirme: gaz alanı bir anlığına 2 katına çıkar, içindekilere 30 hasar, geri itme ve sersemletme.",
                "sindirim_gazi", "hizli_metabolizma", new Color(0.55f, 0.9f, 0.35f));
            return list;
        }

        // Evrim çıktısı asset'leri (Upgrade_<ad>, FX_<ad>) + koşul (Evolution_<ad>).
        private static void Evolution(List<SkillEvolutionData> list, List<UpgradeData> pool, string asset, Type effectType,
            string id, string name, string abbr, string description, string inputA, string inputB, Color color)
        {
            var fx = LoadOrCreate(Folder + "FX_" + asset + ".asset", effectType) as UpgradeEffect;
            var output = LoadOrCreate(Folder + "Upgrade_" + asset + ".asset", typeof(UpgradeData)) as UpgradeData;
            if (output != null && fx != null)
            {
                output.EditorSetupEvolution(id, name, description, fx, abbr);
                EditorUtility.SetDirty(output);
            }
            AddLink(list, pool, "Evolution_" + asset, inputA, inputB, output, color, false);
        }

        private static void AddLink(List<SkillEvolutionData> list, List<UpgradeData> pool, string asset, string a, string b,
            UpgradeData output, Color color, bool ownMoment)
        {
            UpgradeData inA = pool.Find(u => u.Id == a), inB = pool.Find(u => u.Id == b);
            var evo = LoadOrCreate(Folder + asset + ".asset", typeof(SkillEvolutionData)) as SkillEvolutionData;
            if (evo == null || inA == null || inB == null || output == null)
            {
                Debug.LogError($"[SkillSetSetup] Evrim {asset} kurulamadı ({a} + {b}).");
                return;
            }
            evo.EditorSetup(inA, inB, EvoPartnerLevel, output, color, ownMoment);
            EditorUtility.SetDirty(evo);
            list.Add(evo);
        }

        // Var olanı (tip uyuyorsa) korur; referanslar ve diğer alanlar (Havai Fişek mermisi vb.) kalır.
        private static ScriptableObject LoadOrCreate(string path, Type type)
        {
            ScriptableObject existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null && type.IsInstanceOfType(existing)) return existing;
            if (existing != null)
            {
                Debug.LogError($"[SkillSetSetup] {path} farklı tipte ({existing.GetType().Name}), dokunulmadı.");
                return null;
            }
            ScriptableObject created = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
