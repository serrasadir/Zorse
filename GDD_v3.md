# Blob.io — GDD v3.0 (Tek Kaynak Doküman)

> **Bu doküman projenin tek kaynak (single source of truth) tasarım belgesidir.**
> `GDD_v2.md`'yi (Temmuz 2026) **geçersiz kılar**. GDD v1.0 PDF ve kök `GDD.md` zaten v2 tarafından geçersiz kılınmıştı.
> Ağustos 2026'da, kıdemli bir oyun tasarım ekibinin (`DESIGN_REVIEW_v3.md`) GDD v2 üzerinden yaptığı 18 fazlı incelemenin sonucunda yazılmıştır. v2'nin çözdüğü büyük çelişkiler (run süresi, platform, sanat yönü, ekonomi, hazard, boss kadrosu) burada **tekrar açılmadı** — bu doküman onları korur ve üzerine ince ayar yapar. Çelişki kalmamıştır; bulursan bu doküman kazanır ve diğeri güncellenir.
>
> Balance değerleri ScriptableObject'lerde yaşar; buradaki sayılar başlangıç/hedef değerleridir.

**Slogan:** _"Hayatta kal. Seviyeleri atla. Karanlığı yut."_

---

## 0. Karar Günlüğü (Ağustos 2026 oturumu — v2 üzerine)

v2'nin 13 kararı (bkz. Ek A) hâlâ geçerli ve buraya taşındı. Bu oturumun **yeni** kararları:

| # | Karar | Sonuç |
|---|-------|-------|
| 14 | Score Multiplier | **Market'e taşındı** — run içi karttan çıkarıldı (skor zaten sadece vitrin, kart sırasını bir istatistiğe harcamak gereksizdi) |
| 15 | Kalkan | **İptal kararı geri alındı — korunuyor.** Zırh'la aynı iş değil (görünür tükenen tampon ≠ görünmez % azaltma); kod zaten tam çalışır durumda, silmenin maliyeti > tutmanın maliyeti |
| 16 | Hızlanma (Dash) vs. Hız | **İkisi de kalıyor, rolleri ayrıştı.** Hız = sürekli mobilite (tier yavaşlama vergisine karşı ekonomi aracı). Dash = otomatik/periyodik panik butonu — aktifken hazard temas hasarı **%50 azalır** (tam dokunulmazlık değil — kaçışı ödüllendirir, kuralı geçersiz kılmaz) |
| 17 | Evrim eşiği | **Maks/maks yerine maks + orta seviye.** Her evrim: bir "çapa" skill maksimum seviyede + eşleşen skill 4–5. seviyede. Tek run'da erişilebilirlik matematiksel olarak makul değildi (bkz. inceleme Faz 5) |
| 18 | Araç/trafik sistemi | **"Trafik Tehlikesi" olarak resmileştirildi.** Kodda `CarController`/`CarSpawner`/`CarData` var ama ne CLAUDE.md'de ne GDD'de tanımlıydı — düşman değil, AI'sız, ödülsüz, mevcut hazard hasar hattını kullanan çevresel tehlike. **Mühendislik doğrulaması bekliyor** (sistem hâlâ canlı mı, yoksa kalıntı mı) |
| 19 | Mermo unlock koşulu netleştirildi | "3 oturum tamamla" — ölümle biten run da **sayılır** (run sonu ödül ekranına ulaşan her run "tamamlanmış" kabul edilir) |
| 20 | Tier-zaman pacing hedefi | **Yeni.** Giant (Tier 5, mass 100) run'ın **8–10. dakikasında** ulaşılabilir olmalı — final boss'tan (12. dk) önce elit yeme fantezisini yaşamaya yetecek bir pencere bırakır. İçerik/mass tuning bu hedefe göre yapılmalı |
| 21 | Combat Felsefesi | **Yeni, açık ilke olarak yazıldı** (bkz. §9). Sadece yeme mass(=XP) verir; silahla öldürmek coin/skor verir ama büyütmez. Bu kural zaten kodda (`TryConsumeByBlob`) var ama hiçbir dokümanda yazılı değildi — silah gücü kartlarının oyunu jenerik bullet-heaven'a çekmesini önleyen asıl mekanizma budur ve gelecekteki her içerik eklemesinde korunmalı |

**⏸️ Hâlâ açık kararlar** (bkz. §29): nihai oyun adı · soft launch ikinci pazarı.

---

## 1. Oyun Özeti

| | |
|---|---|
| **Oyun adı** | Blob.io _(çalışma adı; nihai isim açık karar — §29)_ |
| **Kod adı** | Zorse |
| **Tür** | Roguelite / Bullet-Heaven / Survival |
| **Platform** | Mobil (iOS/Android), F2P — PC/Steam premium post-launch port |
| **Run süresi** | 12–15 dakika |
| **Motor** | Unity 6, URP |
| **Ekip** | Hüma (Art/UI-UX) · Serra (Dev/Design) · Bahar (Dev/Design) |
| **Referanslar** | Vampire Survivors · Katamari Damacy · Hole.io · Brotato |
| **İş modeli** | F2P + kozmetik IAP + rewarded reklam; sıfır pay-to-win |

### Tek cümlelik pitch
Bir blob'u kontrol et, kendinden küçük her şeyi yutarak büyü, büyüdükçe yavaşla ve güçlen — seni avlayanlar sonunda senin yemeğin olsun. Saldırı tuşu yok; tek strateji hareket.

---

## 2. Üst Konsept & Oyuncu Fantezisi

Oyuncu küçük, yenebilir ve korkuludur. Etraftaki her şey ya tehdit ya atıştırmalıktır ve ikisi arasındaki çizgi sadece **boyutla** okunur. Büyüme soyut bir güç değildir — kameranın gördüğü şeyin ta kendisidir.

Fantezi tek cümlede: **"Küçükken her şeyden korkarak başladım. Sonunda beni kovalayanları yiyordum."**

Bu, oyunun statü tersine dönüşü anıyla somutlaşır: küçükken polisten kaçarsın; Tier 3'te sürü düşmanını, Tier 5'te elit polisi, run'ın sonunda ise final boss'u yutarsın. Her sistem bu anı üç kademeli bir crescendo olarak tekrar tekrar üretmek için var — bu crescendo yapısı (sürü → elit → boss) bilinçli bir ritim olarak korunmalı; yeni düşman tipi eklenirken sorulacak soru hep aynı: *bu tip hangi tier'da yenebilir hale geliyor, ve o tier'a run bitmeden, tadını çıkaracak kadar zaman kala mı ulaşılıyor?*

---

## 3. Tasarım Sütunları

1. **Smooth büyüme hissi** — tier atlama ani değil, akıcı (`Pow` formülü). Avatar, ilerleme çubuğunun kendisidir.
2. **Büyüdükçe yavaşla** — `hız = 1/√tier`. Güç ≠ rahatlık; ustalık anında kırılganlık geri gelir. **Asla kaldırılmaz, sadece ayarlanır.**
3. **Yeme birincildir** — silahlar destektir, yıldız yemektir. Sadece yeme mass/büyüme verir (bkz. §9).
4. **Her run farklı** — upgrade kombinasyonları + meta unlock'lar replay sağlar.
5. **Tek input** — ikinci tuş isteyen her fikir varsayılan olarak reddedilir.

### Anti-vizyon (bu oyun NE değildir)
Hikâye odaklı değil · PvP değil · realistik değil (stilize, komik, absürt-gotik) · sonsuz run değil (her run final boss'la ya da ölümle 12–15 dakikada kapanır) · silah vitrini değil.

---

## 4. Hedef Kitle

Mobil bullet-heaven/roguelite oyuncusu (Vampire Survivors, Brotato, Archero kitlesi) + Agar.io/Hole.io'nun "büyüme" fantezisine aşina, kısa oturumlu (12–15 dk, metro/mola oynanışı), sıfır pay-to-win bekleyen F2P oyuncu. İkincil: Katamari/absürt-gotik estetiğe çekilen PC oyuncusu (post-launch port).

---

## 5. Core Gameplay Loop

**30 saniyelik loop:** Hareket et → consumable/düşman gör → tier kontrol → ye ya da kaç → smooth büyü → hazard'dan sakın. Anlık tatmin: squash animasyonu, ses, skor pop-up, haptic.

**2–4 dakikalık loop:** Mass biriktir → tier atla → level-up → 3 karttan 1 seç → daha büyük şeyleri (ve artık düşmanları) yiyebil → miniboss → sonraki tier.

**Run loop (12–15 dk):** Karakter seç → tier 1→5 yolculuğu → dalga baskısı artar → 4./8. dk miniboss → 12. dk final boss → boss'u ye ya da öl → coin → Market.

**Meta loop:** Coin → Market kredisi → kalıcı stat + karakter unlock + keşif sayacı → sonraki run daha çeşitli → "bir run daha."

**Fail döngüsü:** Ölüm oturumu bitirir; kredinin %50'si korunur (Mermo unlock koşulu dahil her "tamamlanmış oturum" sayımı için de geçerli — bkz. §11). "Sıfır kayıp yoktur." Bu oran çekirdek değerdir, korunur.

---

## 6. Run Yapısı & Pacing

Ölçekleme eğrisi artık **tier hedefleriyle eşleştirilmiş** durumda — önceki versiyonda sadece düşman baskısı vardı, tier ne zaman geçilmeli tanımsızdı:

| Dakika | Tier hedefi | Düşman durumu | Duygusal an |
|---|---|---|---|
| 0–1 | Tiny → erken Small | Sadece consumable | Öğrenme, henüz tehlike yok |
| 1–3 | Small | Temel sürü belirir | İlk korku — her şey senden büyük |
| 3–5 | Medium'a yaklaşma | Elit eklenir; **4. dk miniboss** | **İlk tersine dönüş:** sürü yenebilir hale gelirken dünya aynı anda tehlikelileşiyor — ikisi çakışmalı, aralarında boşluk olmamalı |
| 5–8 | Medium → Large | Yoğunluk ×2; **8. dk miniboss** (güçlenmiş) | Güven inşası, build kimliği belirginleşir |
| 8–10 | Large → **Giant** | Çoklu elit + hız bonusu | **İkinci tersine dönüş penceresi:** Giant'a saatte hâlâ 2–4 dakika varken ulaşılmalı — elit yeme fantezisini sadece "açmak" değil, birkaç dakika **yaşamak** için |
| 10–12 | Giant | Kaos zirvesi, hasar ×2–3 | Kontrollü bunalma |
| 12–15 | Giant | **Final boss** | Zirve — kırılgan-avdan-avcıya yay, boss öldürülerek değil **yenerek** kapanır |

**Kilitlenecek tek sayı:** Giant tier (mass 100), **8–10. dakikada** ulaşılabilir olmalı. Consumable/düşman mass ödülü tuning'i bu hedefe göre yapılır — bu, "oyunun en sonunda büyüdüm" (ödülsüz istatistik) ile "büyük olmayı birkaç dakika **oynadım**" (asıl fantezi) arasındaki farktır.

---

## 7. Büyüme / Mass / Tier Sistemi

```
scale = baseScale × (1 + mass × growthFactor) ^ growthExponent
hız   = 1 / √tier
```
Varsayılanlar: `baseScale=0.5, growthFactor=0.5, growthExponent=0.4`

| Tier | Mass | Yiyebildiği |
|---|---|---|
| 1 Tiny | 0 | Tier-1 consumable |
| 2 Small | 10 | Tier-2'ye kadar |
| 3 Medium | 30 | Tier-3'e kadar + **sürü düşmanı yutabilir** |
| 4 Large | 60 | Tier-4'e kadar + sürü düşmanı kolay yutar |
| 5 Giant | 100 | Her şey + **elit yutabilir**; final boss'un son fazı |

XP & Level: Mass kazanmak = XP kazanmak (tek sistem, ayrı XP kaynağı yok). `xpThreshold = 20 + level × 15` → eşik geçilince level-up → 3 kart.

---

## 8. Yeme & Hazard Sistemi

- **Tier 3'ten itibaren** blob, sürü düşmanlarını temasla yutabilir. Yutulan düşman: mass + coin + XP verir.
- **Tier 5'te** elit düşmanlar yutulabilir.
- **Hazard kuralı (tam commit):** Senden büyük tier'daki her obje ve düşman temas hasarı verir. Hasar `HazardAmount` alanından okunur. Erken oyunda harita bir mayın tarlasıdır; büyüdükçe aynı harita güvenli bir büfeye dönüşür.
- **Trafik tehlikesi (yeni — §18 Karar):** Araçlar düşman değildir, AI'sız çevresel hazard'dır; aynı hazard hasar hattını kullanır, XP/coin ödülü yoktur. Amaç: dalgalardan bağımsız "güvenli sokak / güvensiz sokak" coğrafyası yaratmak (bkz. §14).
- **Okunabilirlik:** hazard objeleri hafif kızıl outline + ikon taşır (renk körü desteği); bu okunabilirlik aynı zamanda pazarlama klibinin küçük ekranda anlaşılırlığını da taşır (bkz. §20).

---

## 9. Combat Felsefesi (Yeni)

**Kural, açıkça yazılı hale getirildi çünkü sadece kod yorumunda yaşıyordu (`EnemyBase.TryConsumeByBlob`) ve gelecekte kırılma riski taşıyordu:**

> **Sadece yeme mass (=XP) verir. Silahla öldürmek coin ve skor verir, ama mass vermez.**

Bu tek kural, silahların oyunu jenerik bir bullet-heaven'a çekmesini engelleyen asıl mekanizmadır: saf "silah build"i coin toplar ama büyümez/seviyelenmez, dolayısıyla eninde sonunda yemeye dönmek zorunda kalır. Silahların rolü — henüz yutamadığın düşmanı yavaşlatmak/öldürmek ve boss'u yenebilir faza getirmek — bu kural sayesinde tutarlı kalır.

**Gelecekte yeni skill/silah/düşman eklerken kontrol listesi:** *Bu ekleme mass ekonomisini bozuyor mu? Silah build'i yeme build'inden daha verimli büyümeye başlıyor mu?* Cevap evetse, tasarım hatalıdır.

---

## 10. Skill & Evrim Sistemi

Level-up'ta oyun durur, 3 kart sunulur, 1 seçilir. Aynı skill 8. seviyeye kadar gelişir. **Yeniden Çek:** oturum başına 1 ücretsiz; sonrası rewarded reklam veya 50 coin. Kartlar renk + sembol taşır (renk körü desteği). Max seviyeye ulaşan skill havuzdan çıkar.

### Lansman skill havuzu (11 skill — Score Multiplier Market'e taşındı, Kalkan geri geldi)
| Kategori | Skill | Etki |
|---|---|---|
| Saldırı | **Silah Gücü** | Karakter silahının hasar/atış hızı ↑ |
| Savunma | **Rejenerasyon** | +0.5 HP/sn, seviye başına +0.5 |
| Savunma | **Zırh** | Alınan hasar % azalır (görünmez, sürekli mitigasyon) |
| Savunma | **Kalkan** | Ayrı, görünür tükenen HP tamponu (Zırh'tan farklı bir enformasyon: "ne kadar dayanabilirim" görünür kalır) — **korundu**, iptal edilmedi |
| Pasif | **Maksimum Can** | Base 100, +10/seviye |
| Hareket | **Hız** | Kalıcı hareket hızı ↑ — tier yavaşlama vergisine karşı ekonomi aracı |
| Hareket | **Hızlanma (Dash)** | Otomatik/periyodik hız patlaması; aktifken hazard temas hasarı **%50 azalır** — panik butonu, Hız'la rolü ayrışık |
| Destek | **Attract** | Çekim yarıçapı + hızı ↑; sadece yenilebilir tier'daki objeleri çeker (tehdit kaçınmayı trivialize etmez) |
| Yeme | **Sindirim** | Yediklerinden mass kazancı +% |
| Yeme | **Yutuş** | Her yeme küçük miktar HP yeniler |
| Yeme | **Yırtıcı Çene** | Düşman yutma tier eşiği kolaylaşır |

> Yeme skilleri farklılaştırıcı fiilin skill havuzundaki karşılığıdır — kart sanatında en belirgin ikonografiyi almalı (markanın görsel imzası).

### Evrim (lansmanda 2 adet — eşik değişti)
İki skill birleşince özel efekt açılır. **Eşik artık maks/maks değil, çapa skill maks + eşleşen skill 4–5. seviye** (tek run içinde erişilebilirlik için — bkz. Karar 17):

- **Attract (maks) + Sindirim (4–5. seviye) → Kara Delik:** çekilen tier-1/2 consumable'lar temassız, otomatik yutulur.
- **Yırtıcı Çene (maks) + Silah Gücü (4–5. seviye) → Avcı Formu:** silahla vurulan sürü düşmanları kısa süreliğine "yutulabilir" işaretlenir.

**Tasarım riski (bkz. §27):** Bu eşikler dahi analytics verisi olmadan tahmini — B19 (analytics) prod olduğunda gerçek level-up sayılarıyla doğrulanmalı.

---

## 11. Karakterler

Her karakter top formundadır, elinde silahı vardır. Silahlar otomatik ateşler.

| Karakter | Pasif | Silah | Fantezi/Kimlik | Unlock |
|---|---|---|---|---|
| **Topik** | +%20 hareket hızı | Top (arcing AoE) | Genelci — tier yavaşlama vergisine karşı en dirençli karakter; AoE ile yiyecek objeye güvenli yaklaşım sağlar | Baştan açık |
| **Mıknato** | Attract hattına +%25 etki | Metal bilye (yavaş homing) | Toplayıcı — hem pasifi hem silahı "yemek bana gelir" hikâyesini anlatır; roster'daki en tutarlı karakter/silah senkronu | Market'te 500 kredi |
| **Mermo** | Mermileri büyük consumable'ları yenebilir parçalara ayırır (`ConsumeAndSplit`, sıfır asset vergisi) | Pistol | Kırıcı — henüz doğal olarak yiyemediği şeye silahla erken erişim kazanır; oyunun temasını karakter düzeyinde en net anlatan pasif | **3 tamamlanmış oturum** (ölümle biten run da sayılır — §0 Karar 19) |

Karakter pasifleri run başında otomatik uygulanır. Karakter başı 5 kademeli meta ağaçlar post-launch. Lansmanda meta güçlendirme karakterden bağımsız düz statlardır (§17).

---

## 12. Düşman Tasarımı

| Tip | Amaç | Yutulma | Ödül |
|---|---|---|---|
| **Sürü (Normal polis)** | Sürekli hareket kararı zorlar; hazard kuralını erken öğretir; ilk "yenilebilir" flip | Tier 3+ | Coin (düşük) |
| **Elit polis** | Dikkatsiz/açgözlü yemeyi cezalandırır; güvenli kalabalığın içinde tehlike cebi yaratır; ikinci, geç flip | Tier 5 | **Sandık** (skill + yüksek coin) |
| **Trafik (araç)** — *yeni, §0 Karar 18* | Dalgadan bağımsız güvenli/güvensiz sokak coğrafyası yaratır | Yenilmez, saf hazard | Yok — çevresel |
| **Miniboss** (4./8. dk, aynı tasarım artan stat) | Ritim işareti — yeni içerik maliyeti olmadan "bu daha büyük bir dövüş" öğretir | Hayır (silahla) | Yüksek coin + garanti sandık |
| **Final Boss** (12. dk) | Tezi teslim eder: oyundaki en büyük, en korkutucu şey yemeğe dönüşür | Son fazda | Run tamamlama ödülü |

**Miniboss spektakl notu:** Aynı modelin iki kez, artan statla kullanılması doğru kapsam disiplinidir (korunur) — ama "aynı düşman, daha çok can" hissi vermemesi için mevcut Elit materyalinin farklı bir tonu + spawn'da isim banner'ı/ekran flaşı eklenmeli. Yeni geometri yok, sadece veri/UI.

**Boss'un yenilmesi — somut mekanik (yeni):** Final boss'un ölüm sekansı, Mermo için zaten inşa edilmiş `ConsumableSpawner.ConsumeAndSplit` deseninin yeniden kullanılmasıyla çözülür: boss yenilince birkaç "dev boyutlu parça"ya ayrılır, Tier-5 oyuncu bunları sırayla fiziksel olarak yer. Tek seferlik özel "yutma" animasyonu gerektirmez — kanıtlanmış, ödenmiş bir deseni yeniden kullanır.

---

## 13. Boss Tasarımı

Bkz. §12 tablosu. Kadro bilinçli olarak dar tutulur: 1 miniboss (tekrarlı, artan stat) + 1 final boss (tek faz geçişi). Bu, önceki tur incelemesinin en pahalı içerik kalemini (4+ özgün boss) kesme kararının devamıdır — **değiştirilmez**, ekip büyüklüğüne göre doğru kapsam.

---

## 14. Harita & Dünya Tasarımı

**Modern Şehir** — lansımda tek, tam işlenmiş harita. Sonsuz kaydırmalı (Vampire Survivors modeli). Kendi consumable seti (çöp → eşya → araç → yapı ölçeğinde), düşman havuzu, gotik-şehir paleti. Toplanabilirler: Coin, Kalp, Altın Kasa. 2–3 easter egg noktası (Grimoire tracking'i şimdiden loglar, UI'sız).

**Aynı sokak, farklı tier'da farklı deneyim (yeni öneri):** Harita coğrafyasını genişletmek yerine, **spawn tablosunu oyuncu tier'ına göre katmanla.** Aynı fiziksel konum (örn. bir ara sokak), oyuncunun o anki tier'ına göre farklı aktif spawn girdileri barındırır — Tiny geçişte çöp/küçük hazard, Giant geçişte (harita döngüsel kaydığı için aynı yere tekrar gelindiğinde) artık tehdit olmayan sürü düşmanları arka plan dokusu, araçlar yiyecek-bitişik sahne öğesi haline gelir. Yeni sanat/coğrafya gerektirmez — hangi spawn tablosunun aktif olduğuna dair bir veri değişikliğidir. Büyüme fantezisini sayısaldan mekânsala taşıyan, projenin elindeki en ucuz kaldıraç.

**Medieval** = lansman sonrası ilk büyük içerik güncellemesi.

---

## 15. Consumable Ekolojisi

Tier'a göre ölçek: çöp (Tier 1) → eşya (Tier 2) → araç parçası (Tier 3) → yapı elemanı (Tier 4-5). Tier geçişleri hem mass eşiği hem de hazard eşiği olarak çift görev yapar — bir consumable/düşman aynı anda "az önce tehlikeliydi, şimdi yenilebilir" okunabilirliğini taşımalı (§8 hazard outline kuralı).

---

## 16. Ödül & Ekonomi

### Run içi: COIN (tek para)
Düşman ölümünden/yutulmasından düşer; Attract ile toplanır. Run içinde harcama: Yeniden Çek (50 coin). Run sonunda: kalan coin Market kredisine dönüşür — tamamlanan run'da %100, ölümde %50.

### Gösterge: SKOR
Sadece leaderboard/highscore göstergesi. Hiçbir şey satın almaz.

### Market (lansman kalemleri — Score Multiplier eklendi)
| Kalem | Maliyet | Etki |
|---|---|---|
| Mıknato (karakter) | 500 | Kalıcı unlock |
| Kalıcı stat: +%5 hız | 200 → artan | Düz |
| Kalıcı stat: +10 max HP | 200 → artan | Düz |
| Kalıcı stat: +%5 mass kazancı | 300 → artan | Düz |
| Kalıcı stat: +%5 coin kazancı | 300 → artan | Düz |
| **Kalıcı stat: +%5 skor** *(yeni — run kartından taşındı)* | 200 → artan | Düz |
| XP Çarpanı +%10 | 1000 | Tüm oturumlar |

---

## 17. Meta İlerleme

2 karakter unlock (Mıknato: kredi, Mermo: oturum sayısı) + 6 düz kalıcı stat = lansman meta'sı. Ekip büyüklüğüne göre doğru ölçek — genişletilmez.

**Eksik olan tek şey kapatıldı:** tüm meta ilerleme ya saf "içerik" (yeni karakter) ya saf "güç" (%5 stat) idi, arada "olasılık ilerlemesi" (koleksiyon güdüsü) yoktu. **Keşif sayacı (yeni):** Grimoire tracking verisi zaten loglanıyor (UI'sız) — Market ekranına "X/Y şey keşfedildi" sayacı eklenir. Yeni tracking işi gerektirmez, sadece mevcut logun okunması.

---

## 18. Unlock & Başarım Yapısı

Lansımda: 2 karakter unlock koşulu (kredi, oturum sayısı) + keşif sayacı (yukarı). Tam başarım/görev sistemi ve Grimoire UI'ı post-launch (§26).

---

## 19. UX / Kontrol / Erişilebilirlik

| Platform | Hareket | Duraklat | Kart seçimi |
|---|---|---|---|
| Mobil (birincil) | Sanal joystick (dinamik, sol yarı) | Üst menü butonu | Dokunuş |
| PC (port) | WASD/ok | ESC | Fare/1-2-3 |

**HUD:** Sol üst can barı (+ Kalkan bar'ı ayrı gösterge) · orta üst timer + dalga · sağ üst aktif skill rozetleri + seviye · alt orta XP barı + level · coin sayacı · portrait, tek elle erişilebilir, safe-area uyumlu.

**Erişilebilirlik (değişmez):** tek el · renk + sembol (renk körü) · hazard outline + ikon · oturum kaydı.

**Restart akışı (korunur):** Ölüm → ödül → Market → tekrar oyna, aralarına zorunlu menü/onay diyaloğu **eklenmez**. Meta ilerleme UI'ı büyürken bu varsayılan korunmalı.

---

## 20. Sanat Yönü

2.5D stilize 3D, Unity URP. 16-bit pixel art vizyonu iptal — büyüme fantezisi gerçek ölçekle anlatılır. Ton: komik-gotik, absürt. Blob sevimli ve karizmatik olmalı (gözler/yüz animasyonu cila fazında).

| Kullanım | Renk | Hex |
|---|---|---|
| Oyuncu | Kızıl | `#8B0000` |
| UI | Kemik beyazı | `#F5F0E0` |
| Arka plan | Gece mavisi | `#0D0D2B` |
| Sürü düşman | Bataklık yeşili | `#3D5C3A` |
| Elit düşman | Mor gecesi | `#4A0E6B` |
| XP/coin | Kehribar | `#FFC300` |

**Pazarlama klibi amaçlı not (yeni):** Hazard outline sadece erişilebilirlik değil, klip okunabilirliği için de tasarlanmalı — telefon ekranında küçük thumbnail'de bile net kalmalı (bkz. §25 size-flip klibi).

---

## 21. Ses & Game Feel

Synthwave + gotik orkestra; run'ın son 3 dakikasında tempo yükselir. Unity Audio + mixer yeterli (FMOD/WWise iptal). Kritik SFX önceliği: yeme sesi, yutma (büyük), level-up jingle, boss girişi.

**Yeni öneri — küçük hitch/slow-mo:** her yeni "sınıf" avın ilk yutulmasında (ilk sürü, ilk elit, boss) birkaç karelik zaman yavaşlaması + kamera nudge — mevcut squash/haptic sistemine binen ucuz bir cila katmanı, yeni VFX gerektirmez. Hem oyun içi tatmini hem klip anını güçlendirir.

---

## 22. FTUE (İlk 10 Dakika)

| An | Hedef dakika |
|---|---|
| İlk yeme | 0:00–0:05 |
| İlk "yenilemeyecek kadar büyük" obje görülmesi | 0:10–0:30 |
| İlk hazard hasarı | 0:30–1:00 (öğretici, cezalandırıcı değil) |
| İlk upgrade kartı | ~1:00 |
| İlk düşman (sürü) | ~1:00 |
| İlk kaçış | 1:00–3:00 |
| İlk düşmanın yenilebilir hale gelmesi (sürü, Tier 3) | ~3:00–4:00 |
| İlk büyük güç sıçraması (Tier 4→5) | ~8:00 |
| İlk elit yeme | 8:00–10:00 |
| Ölüm ya da final boss zaferi | 12:00–15:00 |
| Meta ilerleme ekranı | run bitince hemen |
| Tekrar oynama isteği | aynı dakika içinde — aradaki menü yok |

Hiçbir noktada tutorial duvarı yok — her an dünyanın kendisi tarafından öğretilir (bir obje ya seni yer ya sen onu yersin, hazard outline hangisi olduğunu önceden gösterir).

---

## 23. Monetizasyon İlkeleri

**Değişmez kural: para hiçbir mekanik avantaj satın alamaz.**

| Yüzey | Ne | Not |
|---|---|---|
| Rewarded reklam | Yeniden Çek hakkı · run sonu bonus kredi | Ana erken gelir; oyuncu isteğiyle |
| Kozmetik IAP | Blob renk paletleri, trail efektleri (4–6 kalem) | Genişleme post-launch |
| PC premium (port) | $4.99, reklamsız | Post-launch |

---

## 24. Analytics / Başarı Metrikleri

JSON tabanlı save + GameAnalytics/Unity Analytics ücretsiz katman (B19). Ölçülenler: run sayısı, ölüm dakikası/nedeni, kart seçim oranları, **tier-zaman eğrisi** (yeni — §6 pacing hedefini doğrulamak için: oyuncular Giant'a gerçekten 8–10. dakikada mı ulaşıyor?), evrim erişim oranı (yeni — §10'daki eşik değişikliğini doğrulamak için), D1/D7.

**Soft launch metrik kapıları (değişmez):** D1 ≥ %35 · D7 ≥ %12 · oturum/DAU ≥ 3 run · median run süresi 10+ dk.

---

## 25. Lansman İçeriği

**MVP = oynanabilir + bir kez bitirilebilir + tekrar oynanabilir.** 1 harita · 3 karakter (1 açık + 2 unlock) · **11 skill** + 2 evrim (yumuşatılmış eşik) · 2 düşman tipi + trafik hazard'ı + miniboss + final boss · hazard aktif · coin ekonomisi + **7 kalemlik** Market (Score Multiplier eklendi) · keşif sayacı · yeme juice'u · analytics + save.

**Ana pazarlama varlığı:** size-flip klibi (minik blob polisten kaçar → dev blob polisi yutar). 10 saniyelik, sessiz anlaşılır, remix'lenebilir.

---

## 26. Lansman Sonrası İçerik

Her ertelenen özellik bir güncelleme manşetidir: Medieval haritası · Hava durumu sistemi · Grimoire UI (tracking verisi lansmandan beri birikiyor) · NG+ zorlukları · Karakter meta ağaçları · Yeni bosslar · Evrim havuzu genişlemesi · PC/Steam premium port.

---

## 27. Tasarım Riskleri & Doğrulama Testleri

| Risk | Neden hâlâ açık | Doğrulama |
|---|---|---|
| Yavaşlama mekaniğinin tuning penceresi | Büyürken güç yerine ceza hissi riski — hiçbir dokümanda playtest verisi yok | Prototip fazının "5 yabancıdan ≥3'ü kendiliğinden yeni run başlatıyor" kapısı |
| Evrim erişilebilirliği | Yumuşatılmış eşik bile tahmini | B19 analytics prod olunca gerçek level-up sayılarıyla doğrula |
| Tier-zaman hedefi (Giant @ 8-10dk) | Yeni hedef, mass/tuning verisiyle henüz test edilmedi | Analytics'teki tier-zaman eğrisi metriğiyle izlenir (§24) |
| Trafik sisteminin canlılığı | Kodda var, hiç dokümante edilmemişti | Mühendislik doğrulaması (§0 Karar 18) |

---

## 28. Korunan Tasarım Kararları

1. **Büyüme–hız takası** (`1/√tier`) — asla kaldırılmaz, sadece ayarlanır.
2. **Yeme feedback'inin kalitesi** — squash/ses/haptic; buradaki her kesinti üründeki en pahalı hasardır.
3. **Tek input** — ikinci tuş isteyen her fikir varsayılan olarak reddedilir.
4. **Ölümde %50 kredi** — "ölüm = ilerleme."
5. **Sadece yeme mass verir** (§9) — silahların oyunu jenerik bullet-heaven'a çekmesini önleyen kural.
6. **Sıfır pay-to-win.**
7. **Frame rate > içerik.**
8. **Tek harita, dar boss kadrosu** lansman kapsamı — ekip büyüklüğüne göre doğru, genişletilmez.

---

## 29. Kalan Ürün Sahibi Kararları

- 🔲 **Nihai oyun adı:** Blob.io çalışma adı — global lansmandan önce kesinleşmeli.
- 🔲 **Soft launch ikinci pazarı:** TR + hangi ülke? Tasarım-bitişik not: kısa-form video kültürü güçlü bir pazar tercih edilmeli (go-to-market planı bir klip üzerine kurulu, mağaza listesi üzerine değil).
- 🔲 **Mobil-önce platform kararı** — Ağustos 2026 inceleme ekibinde bir azınlık görüşü olarak not düşüldü (bkz. `DESIGN_REVIEW_v3.md` Faz 17): F2P + UA bütçesi yokluğu riski hâlâ geçerli. Karar bilinçli ve kayıtlı; yeniden açılması soft launch metrik verisine (§24) bağlı, önceye alınmıyor.

---

## Ek A — v2 → v3 Değişiklik Özeti

| Konu | v2.0 (Temmuz 2026) | v3.0 (Ağustos 2026) |
|---|---|---|
| Score Multiplier | Açık karar (kart mı meta mı) | **Market'e taşındı** |
| Kalkan | İptal (Zırh'la aynı iş) | **Korundu** — farklı rol, kod zaten hazır |
| Dash vs. Hız | Açık karar | **Roller ayrıştı** — Dash = hazard hasar azaltmalı panik butonu |
| Evrim eşiği | Maks + maks | **Maks + orta seviye (4–5)** |
| Trafik/araç sistemi | Dokümante edilmemiş | **Resmi hazard sınıfı** (mühendislik doğrulaması bekliyor) |
| Mermo unlock | "3 oturum tamamla" (belirsiz) | **Ölümle biten run da sayılır** |
| Tier-zaman pacing | Sadece düşman baskı tablosu | **Giant @ 8–10dk hedefi eklendi** |
| Combat felsefesi | Kodda implicit | **§9'da açık ilke** |
| Meta ilerleme | Sadece içerik/güç | **Keşif sayacı eklendi** (olasılık ilerlemesi) |
| Harita | Tek harita, statik ekoloji | **Tier'a göre katmanlı spawn tablosu önerisi** |
| Boss'un yenilmesi | Tanımsız mekanik | **`ConsumeAndSplit` yeniden kullanımıyla somutlaştı** |
| Miniboss | Aynı tasarım, artan stat | **+ spektakl katmanı** (tint + banner, sıfır yeni geometri) |

---

_GDD v3.0 — Ağustos 2026 · `GDD_v2.md`'yi geçersiz kılar · İnceleme oturumu: `DESIGN_REVIEW_v3.md` (kıdemli tasarım ekibi, 18 fazlı inceleme)_
