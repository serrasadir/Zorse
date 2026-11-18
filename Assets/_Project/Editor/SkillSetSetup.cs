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
                "Hareket hızı +%15. Kaykay!", "Hız +%30.", "ALEVLİ KAYKAY: hız +%38.",
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
                "Boss ve elite ×1.5 hasar, roket hasarı +8.", "SON FORM: her atışta 2 roket." }, perLevel: 8f),
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
            if (karaDelik != null)
            {
                karaDelik.EditorSetLabel("KD", false);
                EditorUtility.SetDirty(karaDelik);
            }
            else Debug.LogWarning("[SkillSetSetup] " + KaraDelikAsset + " bulunamadı, kısaltması yazılmadı.");
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
            EditorSceneManager.MarkSceneDirty(system.gameObject.scene);
            EditorSceneManager.SaveScene(system.gameObject.scene);
            Debug.Log($"[SkillSetSetup] {pool.Count} skill kart havuzunda (3 aktif + 3 pasif slot). Sahne kaydedildi.");
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
