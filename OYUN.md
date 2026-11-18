# Left No Crumbs — Oyunun Güncel Hali

> **Bu dosya oyunun şu anki halini anlatır, geçmişini değil.** Bir şey değişince eski hali silinir, yenisi yazılır (üstü çizili satır, "eskiden şöyleydi" notu yok — geçmiş git commit'lerinde).
> **Bekleyen işler GitHub issue'larında.** Bir iş bitince issue kapanır, sonucu buraya işlenir.
> **Skill, karakter ve boss sayıları `SKILLS.md`'de.** 3B asset üretim kuralları `ASSET_RULES.md`'de, pazarlama planı `DISCOVERY_STRATEGY.md`'de.
> Denge değerleri ScriptableObject'lerde ve sahnede yaşar; buradaki sayılar oradan okunmuştur (4 Kasım 2026).

---

## 1. Künye

| | |
|---|---|
| **Ad** | **Left No Crumbs** ("ate and left no crumbs" deyiminden). Telefonda görünen ad "No Crumbs". Kodda iç çalışma adı Blob.io / `BlobSurvivor` |
| **Stüdyo** | Zorse Studio — Serra (Dev/Design), Bahar (Dev/Design), Hüma (Art/UI-UX) |
| **Tür** | Roguelite / bullet-heaven / survival; "yiyerek büyü" |
| **Platform** | iOS (TestFlight'ta, sürüm 0.1.x — canlıya çıkana kadar 0, son hane = build no). Android sonra; PC/Steam port post-launch |
| **Motor** | Unity 6, URP, New Input System, AI Navigation |
| **Run süresi** | Zaman sınırı yok; hedef ~25 dk. Final boss 10. seviyede (en geç 21. dk) gelir, ne zaman yutulursa run o zaman biter |
| **İş modeli** | F2P + kozmetik IAP + rewarded reklam, **sıfır pay-to-win** (henüz kodda yok) |
| **Referanslar** | Vampire Survivors · Katamari Damacy · Hole.io · Brotato |

**Pitch:** Bir blob'u kontrol et, kendinden küçük her şeyi yutarak büyü — seni avlayan polis sonunda senin yemeğin olsun. Saldırı tuşu yok; tek input hareket.

**Oyunun kalbi — statü tersine dönüşü:** küçükken polisten kaçarsın, büyüyünce aynı polisi yutarsın; sonunda seni gökten avlayan helikoptere uzanacak kadar büyüyüp onu da yutarsın.

---

## 2. İlkeler (taviz verilmez)

1. **Yeme birincil, silah destek.** Silahlar henüz yutamadığını öldürmek ve boss'ları indirmek içindir.
2. **Akıcı büyüme.** Tier atlamak "tak" diye sıçramaz; blob sürekli büyür, kamera büyümeden yavaş uzaklaşır (blob ekranda büyür).
3. **Büyüdükçe yavaşla.** Güç ≠ rahatlık. Ayarlanır, kaldırılmaz.
4. **Tek input.** İkinci tuş isteyen fikir varsayılan olarak reddedilir.
5. **Ölüm = ilerleme.** Ölünce run'da toplanan coin'in %50'si kasaya (kalıcı coin) girer.
6. **Sıfır pay-to-win.**
7. **Frame rate > içerik.** Mobilde 30 FPS kaosun zirvesinde de korunur.
8. **Çocuk yaya yok.** Yiyerek büyüme oyununda çocuk yutmak etik ve mağaza riski; eklenmez.

---

## 3. Run Akışı

0. **Açılış (sadece uygulama ilk açılırken):** Zorse Studio giriş videosu oynar (3 sn, dikey, sesli; koyu gri zemin ekranın kalanını doldurur), bitince söner (`StudioSplash`, video `Resources/UI/zorse-intro.mp4`). Video açılamazsa eski hâl: siyahta logo (`Resources/UI/zorse-logo.png`).
1. **Giriş ekranı:** arkada canlı şehir, kamera Topik'e yakın; Topik göz kırpar, sağa sola bakınır. Logo + OYNA. Duraklatma ve oyun sonundaki "Ana Menü" de buraya döner (run sıfırlanır).
2. **Lobi:** OYNA'dan sonra karakter seç (Topik, Mıknato, Fişek, Dalcık, Cano; 4'ten fazla karakterde kartlar alçalır), Market'e gir. HUD diliyle: başlık, coin, her karakter için kart (portre `Art/UI/Characters/Icon_*.png` ← `build_blobs.py`, ad, pasif açıklaması; kilitliyse soluk + koşul rozeti), altın MARKET tuşu ve geri (giriş ekranına) tuşu. Karta dokunmak oyunu başlatır.
3. Şehirde başlarsın; yerdeki çöpleri, sokak eşyalarını, yayaları yiyerek büyürsün. Senden büyük şeyler seni durdurur (katı), hazard'lar hasar verir.
4. Kütle = XP; yenen her şeyin kütlesi genel büyüme hızıyla (×0.35, `BlobGrowth._globalGainScale`) çarpılır. Silahla öldürülen düşman sadece XP verir, kütle vermez (büyüme yemeğe bağlı): robot köpek 0,5, bekçi 0,5, polis 2, motorlu 2, çevik kuvvet 3, elit 7, 1. tank 80, 2. tank 130 (`EnemyData._killXp`). Level atlayınca oyun durur, **3 karttan 1** seçilir. Aynı skill tekrar seçilirse seviye atlar (en fazla 5).
5. Polisler gelir, zamanla ve büyüdükçe baskı artar; boss'lar belirli **seviyelerde** gelir (takılan oyuncuya en geç belirli bir dakikada).
6. Run iki şekilde biter:
   - **Ölüm** → "Game Over!" ekranı, coin'in %50'si kasaya.
   - **Final boss (helikopter) yutulursa** → "KAZANDIN!" ekranı, coin'in %100'ü kasaya.
7. Kasadaki coin Market'te kalıcı statlara ve karakterlere harcanır.

### Zaman çizelgesi

| Zaman | Olay |
|---|---|
| 0–20 sn | Sadece yenebilir objeler, yayalar, arabalar |
| 20. sn | **Bekçiler** gelmeye başlar (zayıf, copsuz; Little'da yenir). Bant: **BEKÇİLER GELDİ!** |
| 1. dk | Robot köpek sürüleri |
| 2. dk → 10. dk | Bekçinin yerini giderek polis alır: 2. dk'da hep bekçi, 3. dk'da ~%12 polis, 6. dk'da yarı yarıya, 10. dk'dan sonra hep polis (`EnemyData_Guard._replacedBy` + `_replaceStart/End`). Toplam düşman tavanı seviyeyle artmaya devam eder |
| 1,5. dk | İlk anlık görev, sonra her görev bittikten 60 sn sonra yenisi (aşağıda) |
| Blob **Medium** olunca (en geç 4. dk) | Elit polisler |
| 2,5. dk (seviye 3+) | İlk kuşatma, sonra ~110 sn'de bir (§6) |
| 5. dk'dan sonra her dakika | Sürü düşmanlarının canı +%8 (10. dk'da ×1,4, 20. dk'da ×2,2; boss'lar hariç; `WaveController._healthPerMinute`, `_healthStartMinute`) |
| 5. dk | Düşman hasarı ×1.2 · **Çevik kuvvet** gelmeye başlar (`Wave_Riot`) |
| 7. dk | **Motorlu polis** gelmeye başlar (`Wave_Motor`) |
| 10. dk | Düşman spawn'ı ×2 hızlı (tavan aynı) |
| 15. dk | Düşman hızı ×1.15 |
| 20. dk | Düşman hasarı ×3 |

Boss'lar seviyeye bağlı ama süre penceresi içinde: seviyeye erken ulaşılsa da en erken süreden önce gelmez, ulaşılamasa da en geç sürede gelir (`BossStage.EarliestTime` / `LatestTime`):

| Boss | Seviye | En erken | En geç |
|---|---|---|---|
| 1. Miniboss (polis tankı) | **5** (~310 kütle, Medium) | 7. dk | 9. dk |
| 2. Miniboss (güçlü tank) | **8** (~1030 kütle, Large) | 13. dk | 15. dk |
| **Final boss: polis helikopteri** | **10** (~2100 kütle, Dev) | 20. dk | 21. dk |

**Boss canı oyuncu seviyesiyle (VS gibi):** boss geldiğinde blob gerekli seviyenin üstündeyse her fazla seviye için +%15 can (1. tank seviye 7'de gelirse ×1,3); Lanet de çarpar (`BossSpawner.BossHealthPerExtraLevel`). Güçlü gelen oyuncuya boss erimesin.

Boss'lar sırayla ve tek tek gelir: biri yaşıyorsa sonraki onun ölmesini bekler; final boss beklemez (miniboss sahnedeyse çekilir). Kart ekranı açıkken boss çıkmaz. Seviye birden ikisini aşarsa sadece sonraki çıkar. Değerler `BossData_Miniboss.asset` → `RequiredLevel` / `LatestTime`.

### Kombo yazısı

0,75 sn içinde art arda gelen öldürmeler (silahla ya da yutarak, boss dahil) zincir sayılır (`UI/ComboPopup`). 3'ten itibaren blob'un üstünde eğik, zıplayan yazı: **ÜÇLÜ / DÖRTLÜ / … / DOKUZLU KOMBO!**, 10'dan sonra **x12 KOMBO!**. Renk sayıyla ısınır: sarı (3–4) → turuncu (5–9) → kırmızı + "MUHTEŞEM!" (10–24) → mor + "DURDURULAMAZ!" (25+), 50+ "EFSANE!". Tek savuruşta 2+ öldürme daha sert zıplar. Titreşim sadece 3, 5 ve 10 eşiklerinde. Zincir bitince 0,55 sn durur, söner. Ödül yok, sadece his.

### Anlık görevler

Run içinde kısa hedef; ekranda hep en fazla bir görev (`MissionSystem`, sahne kurulumu yok, kendini kurar). Başlarken olay bandı (**GÖREV!** + görev), sonra slot şeridinin altında satır: `GÖREV · Polis indir 3/7 · 0:32`. Boss sahnedeyken ve kuşatmadan sonraki 20 sn içinde yeni görev gelmez (5 sn sonra tekrar dener). Aynı görev üst üste gelmez. Süre dolarsa ceza yok: satırda kısa "Süre doldu", görev kaybolur.

| Görev | Hedef | Süre | Ne zaman çıkar |
|---|---|---|---|
| Bekçi / Polis indir (silahla) | 5 + seviye / 2 | 45 sn | her zaman |
| Bir şeyler ye (düşman dahil her şey) | 15 + seviye × 2 | 45 sn | her zaman |
| Büyük lokma: blob'un bir altı tier'dan ya da daha büyük | 3 | 60 sn | Small+ |
| Hasar alma (kalkanın yuttuğu sayılmaz) | — | 30 sn | her zaman |
| Sandığı kap: 22–38 birim öteye sandık düşer, ekranda ok (sandık ekrandaysa üstünde, değilse kenarda). Ödülü sandığın kendisi (35 coin + ekstra kart), görev ayrıca vermez. Süre dolarsa sandık kaybolur | — | 30 sn | her zaman |

Bekçi, polis ve elit sayılır; robot köpek ve boss sayılmaz. Görev metni o anki çoğunluğa göre "bekçi" ya da "polis" der. Düşman yutma görevi yok (silahla öldürme görevine karıştığı için) (sıradan düşmanların yarıdan azı polisken bekçi, ~6. dk'ya kadar; `EnemySpawner.PoliceShare`). **Ödül:** `15 + dakika × 3` coin (`MissionSystem.CoinScale` çarpanıyla; kasaya giden coin gibi Market coin bonusu da işler) + bir bonus: %25 ekstra kart (sandıktaki gibi), yoksa can %90'ın altındaysa yarı yarıya can +%30 ya da kalkan +25, can doluysa kalkan +25. Bant: **GÖREV TAMAM!** + ödül.
- **Geçici kalkan** (görev ödülü, Geri Dönüşüm'ün can taşması): yenilenmez, hasarı candan önce yer. HUD'da can barının sağ ucunda can ölçeğinde açık mavi uzantı; azaldıkça kısalır.

---

## 4. Büyüme, Tier ve Yeme

- **Ölçek:** `0.5 × (1 + kütle × 0.1) ^ 0.45` = blob'un çapı (birim). Yeme anında kısa şişme (punch): yenen şey 1 tier altındaysa tam (%15), 2 tier altındaysa hafif (%4), 3+ tier altındaysa yok. Polis/boss yutunca hep tam.
- **Hız:** `1 / sıra ^ 0.24` (sıra: Tiny 1, Little 1.5, Small 2 … Dev 6; `TierLayers.SlowdownRank`) × karakter/skill/meta çarpanları.
- **XP eşiği:** `20 + seviye × 15 + seviye² × 2 + max(0, seviye − 4)² × 12` (geç oyunda bina yerken seviye yağmuru olmasın diye karesel; son terim ilk 5 seviyeye dokunmadan ileriki seviyelerin aralığını açar; `BlobGrowth._xpQuadraticPerLevel`, `_xpLateStartLevel`, `_xpLateQuadratic`).

| Tier | Kütle | Blob çapı | Ne yer |
|---|---|---|---|
| 1 Tiny | 0 | 0.5 | Çöp, hidrant, çöp kutusu |
| 2 Little | 15 | 0.76 | Zayıf yayalar (erkek, kadın), bank, çalı |
| 3 Small | 50 | 1.1 | İri yaya, telefon kulübesi, benzin pompası, seyyar arabalar, trafik ışığı |
| 4 Medium | 200 | 2.0 | **Normal polis**, ağaçlar, park etmiş ve hareketli arabalar, dondurma kamyoneti, kamyon, minibüs, lamba, kaydırak, pota |
| 5 Large | 650 | 3.3 | Otobüs, fıskiye, berber, benzinlik, atlıkarınca, dönme dolap |
| 6 Giant | 1100 | 4.1 | **Elit polis**, kafe, kitapçı, fırın, pizzacı, mavi/pembe/tuğla apartman |
| 7 Dev (kodda `Colossal`) | 1650 | 5.0 | Cumbalı/adaçayı apartman, karakol, lokanta, hastane, sinema, büyük otel |

- **Tier boydan gelir** (`Editor/TierRules.cs`): obje, blob o tier'a yeni girdiğindeki çapından en fazla %5 büyükse o tier'da yenir. Boy = kenarların geometrik ortalaması; uzun ince objelerde en uzun kenarın yarısı daha büyükse o. Şehir kurulumu bunu otomatik uygular; elle ayar için `Tools → Blob.io → Tier'ları Boya Göre Kur`. Polis ve elit istisna: tier'ları tasarım gereği `EnemyData`'da. Elle istisnalar `TierRules.TierShift`'te (oyun hissi): sokak lambası (`P_Lamp2`) bir tier aşağı (Small), telefon kulübesi bir tier yukarı (Medium).
- **Şehir objesi kütlesi tier'a göre:** 2 / 3 / 4 / 10 / 25 / 50 / 80. Hareketli araba 15 (otobüs 25), yaya 1 (iri 3), yerdeki yiyecek 1–3.
- **Yerdeki yiyecek tier'a göre seyrelir:** tavan 80 × (Tiny 1 · Little 0.85 · Small 0.7 · Medium 0.5 · Large 0.35 · Giant 0.25 · Dev 0.15) — büyüyünce yenen şey zaten büyük (`ConsumableSpawner._tierDensity`). Tier düşerse/restart'ta tekrar dolar.
- **Yeme temasla olur.** Tier'ın yetmediği obje katıdır (çarparsın, içinden geçemezsin); yetince içinden geçip yersin. Katı kısım yerdeki ayak izidir (direk, gövde); havada büyük çıkıntısı olan eşyalarda (pota panosu, ağaç tacı, lamba kolu, durak/benzinlik tentesi) görünür boy kadar bir **yeme alanı** da var: yiyebilecek boydaysan çıkıntıya değince yenir, küçükken altından geçersin.
- **Önle yenir.** Blob'un yüzü gidiş yönüne bakar; arkadaki 120°'lik dilim (popo) yemez. Arkadan değen yenebilir obje içinden geçer, yüz ona dönünce yenir; arkadan çarpan araba yenmez ve hasar da vermez. İstisnalar: vakumla çekilenler, Kara Delik, boss'lar (`BlobConsumption.RearAngle`).
- **Polis yutulurken ısırır:** tier tam yetiyorsa 5, bir tier üstündeysen 2.5, daha büyüksen 0 hasar (robot köpek 2 / 1 / 0). Kırmızı parlama + kamera sarsıntısı + titreşim.
- **Düşmanlar kaçmaz:** blob onları yiyebilecek boyda olsa da saldırmaya devam eder.
- **Yaya tekmesi:** yiyemediğin (senden büyük) yayaya çarparsan seni tekmeler: seke seke yuvarlanarak savrulursun, tepende sersemleme yıldızları, kısa ağır çekim + kamera yakınlaşması. Can gitmez. Ayrıntı `SKILLS.md` §2.
- **Hazard'lar** (ör. araba çarpması) yiyemeyeceğin boydayken hasar verir.
- Final boss istisnadır: tier değil **fiziksel uzanma** gerekir (bkz. §6).

---

### Özel dönüşüm: Dönme Dolap

Haritadaki tek dönme dolabı (`P_FerrisWheel`, Large) yiyince **20 sn dönme dolabı "giyersin"** (`FerrisWheelForm`; Mario Odyssey yakalaması gibi karakter görünür kalır):
- **Yakalama anı (~1 sn, oyun donar):** blob zıplayıp çarkın göbeğine yerleşir, çark etrafında esneyerek açılır ve hızla bir tur döner; parıltı, titreşim, **DÖNME DOLAP! · Ez geç!** bandı.
- **Çark:** ayaksız çark (`X_FerrisWheelRoll`), blob çapının 1,7 katı; blob göbekte dik durur, gözleriyle gidiş yönüne bakar, çark etrafında tekerlek gibi yuvarlanır, arkasında toz. Hız ×1,6, kamera biraz uzaklaşır. Üstünden geçtiği polis/köpek/elit anında ezilip yutulur (kütle verir, yeme görevine sayılır, ısırık hasarı yok; tier'ı bir üstü aşan düşman yine ölür ama yutulmaz), yerdeki boss saniyede 200 hasar alır, bir üst tier'a kadar her şeyi yer (binalar dahil). Hasar alır.
- **Bitiş:** son 3 sn çark yanıp söner; sonra çark dönerek küçülüp kaybolur, blob yere iner. Run başında/ölümde hemen biter.
- Görsel: `Tools → Blob.io → Dönme Dolap Formunu Kur` → `Resources/Transformations/FerrisWheelForm`. Test: Editor'da **T**.

## 5. Skill'ler ve Karakterler

Ayrıntı ve tüm sayılar **`SKILLS.md`**'de. Özet:

- **16 skill, 5 kategori:** Sindirim ve Büyüme (Sindirim, Vakum, Geri Dönüşüm, Hızlı Metabolizma) · Hareket (Hız, Kaygan Zemin) · Saldırı (Sopa, Havai Fişek, Yer Sarsıntısı, Sindirim Gazı, Tetikçi) · Savunma (Zırh, Can Artışı, Rejenerasyon, Yara Öfkesi) · Nadir (Bölünme). Yer Sarsıntısı deneme aşamasında.
- **Slot:** en fazla **3 aktif + 3 pasif**; bir tür dolunca o türden yeni skill gelmez. Nadir ve evrim kartları slot harcamaz. Her skill **en fazla 5 seviye**; ⭐ seviyeler yeni yetenek açar. Garanti kart yok. Kart kalmazsa level atlamak canın %30'unu doldurur.
- **1 evrim:** Vakum 5 + Sindirim 5 → **Kara Delik**. Evrim kartı ★ ve altın renkle gelir. Seçilince evrim anı oynar (ağır çekim, kasırga blob'a çöker, indigo şok dalgası, ekranda ★ KARA DELİK); sonra blob'un altında siyah olay ufku ve siyah-lacivert kasırga kalır.
- Max seviyedeki skill havuzdan çıkar; birden çok level aynı anda atlanırsa kartlar sırayla gelir; run bittiyse kart açılmaz.
- **5 karakter**, hepsi top formunda, silahlar otomatik. Ana silah herkeste aynı (sopa); fark başlangıç skill'inde (o skill favori, kartta ×3; slot kaplar). Ayrıntı `SKILLS.md` §4:
  - **Topik** — pasif: run'a **Hız Sv.2** ile başlar (kaykay baştan var). Baştan açık. Hızlı giderken arkasında hafif rüzgar çizgileri (iki yandan ve tepeden üç ince beyaz iz; `CharacterData._speedTrail`, `SpeedTrail`).
  - **Mıknato** — pasif: **Vakum Sv.2** ile başlar. Market'te 500 coin.
  - **Fişek** — siyah korsan (kırmızı göz bandı); pasif: run'a **Havai Fişek Sv.1** ile başlar, atar baştan omzunda (`CharacterData._startingUpgrade`; favori skili de Havai Fişek). Market'te 1.000 coin.
  - **Dalcık** — fıstık yeşili; pasif: run'a **Sopa Sv.2** ile başlar (15 hasar, menzil 2.5, 180° yay). Satılmaz: **5 run bitirince** açılır (ölüm de sayılır). Oyuncuya "oynamaya devam et" hedefi; Market'te ilerlemesi görünür.
  - **Cano** — ahududu kırmızısı, tepede kalp; pasif: run'a **Geri Dönüşüm Sv.2** ile başlar (yediği her cansız nesne +2 can). Market'te 1.500 coin.
- Kilit koşulu `CharacterData`'da: `_marketPrice` (> 0 = Market'te satılır), `_requiredCompletedRuns`, `_saveId` (kayıtta satın alım kimliği).
- **İki silah:** ana silah **sopa** (ağaç dalı, `StickWeapon`): saniyede bir, yüzün baktığı yöne 120°'lik yayla kısa menzilde (gövde yüzeyinden 2, Sopa skill'iyle 3'e kadar) yaydaki herkese vurur, beyaz hilal izi bırakır. Sopa skill'i: Sv.2 yay 180°, Sv.3 arkaya da, Sv.4 hızlı, Sv.5 tam tur. **Havai Fişek** (kart) uzun menzilli (gövde yüzeyinden 8), yavaş (2 sn'de 1), tek hedefe ağır; **her yöne** ateş eder (kurma/nişan yok), önce boss'a sonra elite.
- Silah havadaki hedefe (helikopter) otomatik yukarı nişan alır; menzil yatay ölçülür.
- **Havai Fişek roketi** model + renkli iz (`X_Firework`, `zorse-blender/build_bullets.py`). Blob büyüdükçe roket de büyür (Tiny 1× → Dev ~3×, kamera uzaklaştığı için).
- **Görünür silah** (Sopa skill'iyle form değiştirir; ayrıntı `SKILLS.md`): bütün karakterlerde ağaç dalı, sadece vururken görünür, yay boyunca savrulur (G_Stick_*). **Havai Fişek atarı** sol omuzda, ana silahın üstünde (sol = saldırı, sağ = kalkan); renkli roketler, isabette renkli kıvılcım.

---

## 6. Düşmanlar ve Boss'lar

Sayılar **`SKILLS.md` Bölüm 5**'te.

| Düşman | Nasıl davranır | Nasıl ölür |
|---|---|---|
| **Robot köpek** (sürü, erken) | 3'lü sürüyle gelir (1. dk'dan), hızlı (3.4; blob'dan yavaş), az canlı (10: ilk sopa vuruşunda ölür), temasta 4 hasar. Açık metalik gri dört bacaklı devriye robotu: polisin mavisinden ve koyu asfalttan ayrılsın diye paletin köpeğe özel kopyası (`RobotDogPalette`, maviler griye; `Tools → Blob.io → Robot Köpek Rengini Uygula`). Model `H_RobotDog` ← `build_robodog.py`; kurulum `Tools → Blob.io → Robot Köpeği Kur`. Görme mesafesi 40: doğduğu andan sana koşar (polis 15). Polis tavanına sayılmaz, kendi tavanı var | Silah ya da **Little**'dan itibaren yutma. %25 olasılıkla 4 coin |
| **Bekçi** (20. sn'den, ilk dakikaların düşmanı) | Copsuz güvenlik görevlisi, yumrukla durdurmaya çalışır. Yavaş (2.0), az canlı (10: temel sopayla tek vuruş), temasta 3 hasar, yutulurken 1 ısırır. Oyuncu ilk dakikada "polisi alt etme"yi tatsın diye. Model `H_Guard` (Blender `build_guard.py`, şimdilik `tools/zorse-blender-pending/`): bej gömlek, sarı reflektörlü yelek, kahve kasket ve pantolon. Polis tavanına ve görevlerde "polis"e sayılır. Blob'u ilk görünce başının üstünde zıplayarak kırmızı **"!"** belirir (~1 sn; `AlertMark`, polislerde de) | Silah ya da **Little**'dan itibaren yutma. %25 olasılıkla 4 coin |
| **Polis** (sürü) | Koşar, temasta coplar (can 30: temel sopayla 3 vuruş; hasar 10). Binaların, eşyaların, arabaların etrafından dolanır; yiyebileceğin boyda olsan da kaçmaz | Silah ya da Medium'dan itibaren yutma |
| **Elit polis** | Durur, nişan lazeriyle takip eder, kilitleyip 3'lü mermi atar. Arada bina varsa ateş etmez | Silah ya da Giant'ta yutma. Sandık düşürür |
| **Çevik kuvvet** (5. dk'dan) | Yavaş (1.8), kalın (can 120, hasar 12). Önünde kalkan: blob önündeki 120°'lik yaydayken (`_frontShieldHalfAngle` 60) gelen hasarın %85'i kesilir; yandan/arkadan tam hasar. Model `H_RiotPolice`: antrasit zırh, beyaz kask + vizör, gövde boyunda açık mavi kalkan | Silah ya da **Large**'dan itibaren yutma. 3 coin |
| **Motorlu polis** (7. dk'dan) | Hızlı (2.6, can 70, hasar 14). 9 birim menzilde ve görüş açıkken durur, 0,6 sn blob'a döner (geri yaslanır), sonra kilitli yönde 8,5 hızla 0,9 sn düz dalar (`DashState`); dalışta bir kez temas hasarı, duvara çarpınca durur; 3 sn bekleme. Model `H_MotorPolice`: beyaz motosiklet (mavi şerit, kırmızı-mavi tepe lambası), mavi üniformalı, beyaz kasklı sürücü | Silah ya da **Large**'dan itibaren yutma. 3 coin |
| **Miniboss: Polis Tankı** (5. ve 8. seviye) | 2.5 kat büyük; nişan lazeri + kilit + tank topu. Üstünden geçtiği arabaları ve sokak eşyalarını ezer (eşya kendi renklerinde kırıntı + tozla dağılır, `CrushFx`), binaların etrafından dolanır. Uzaktaysa yetişmek için hızlanır | Sadece silahla, yutulamaz. Ölünce ~1.3 sn: beyaz parlama, titreme, patlama kırıntıları, yana yatıp yere gömülür |
| **Final Boss: Polis Helikopteri** (10. seviye) | Havada uçar, yukarıdan mini roket serisi atar (alevli, değince patlar: alan hasarı). Vurdukça kademe kademe alçalır (irtifa 8 → 7 → 6 → 5), yerde koyulaşan gölgesi ve dönen pervane gölgesi var. Gelince kamera uzaklaşıp ilk 3 sn ikinizi kadraja alır; sonra uzaklık kalır, kamera blob'u ortalar (boss ölünce normale döner; `CameraController._bossIntroDuration`) | **Silahla ölmez.** Blob'un tepesi helikoptere değdiği an yutulur, hangi irtifada olursa olsun (irtifa 5 ≈ 1340 kütle). **Yutulma sahnesi** (~4 sn, blob hasar almaz): yavaş çekimde helikopter dönerek küçülüp kıvılcım iziyle blob'a emilir, kamera yaklaşır, kocaman yutkunma + patlama + güçlü titreşim; zaman normale döner, ~1.3 sn sonra geğirme (şişme + ağızdan kırıntı bulutu), kısa bekleme; sonra KAZANDIN |

- **Dalgalar** (`WaveController._waves`, sırayla açılır): Guard (20. sn: bekçi 1) → Police (1. dk: bekçi 1, köpek 0.35) → PoliceElite (Medium ya da 4. dk: bekçi 0.85, elit 0.15, köpek 0.25) → Riot (5. dk: bekçi 0.7, elit 0.15, köpek 0.15, çevik kuvvet 0.5) → Motor (7. dk: bekçi 0.55, elit 0.15, köpek 0.1, çevik kuvvet 0.4, motorlu 0.4). Tablodaki "bekçi" spawn anında süreye göre polise döner (yukarıda, 3. → 12. dk). Yeni türler ve dalga tablosu `Tools → Blob.io → Yeni Polis Türlerini Kur` ile kurulur.
- **Eşzamanlı (seviyeye göre):** toplam düşman tavanı `8 + seviye × 2.2`, elit tavanı `2 + (seviye − 5) × 1.5` (en az 1). **Mutlak üst sınır: toplam 30, elit 8** — hiçbir çarpan aşamaz. Robot köpekler bu sayıya dahil değil: ayrı tavan `8 + seviye × 1` (en fazla 18; `EnemySpawner._swarmCap*`). Spawn aralığı seviye başı %10 kısalır (seviye 10'da 2 kat sık). `EnemySpawner._policeCap*`, `_eliteCap*`, `_spawnSpeedupPerLevel`. 25 yaya.
- **Doğuş yeri — kaçış kapanır:** düşmanlar ekranın hemen dışında doğar (blob'dan en az 10 birim), blob hareket ediyorsa %65'i gidiş yönünün ±70°'lik konisinde (`EnemySpawner._aheadChance`, `_aheadSpread`). Kamerasız yedek: blob çevresinde rastgele.
- **Arkada kalan öne taşınır:** gidiş yönünün arkasında ve ekran dışında 3 sn kalan düşman (polis, elit, robot köpek) önündeki ekran kenarına ışınlanır ve 6 sn görüş menzilinden bağımsız kovalar (`_relocateAfter`, `_relocateAlert`). Tavan arkadan gelen kuyrukla dolup önü boş bırakmaz. 50 birimden uzaktaki yine havuza döner.
- **Kuşatma:** 2,5. dk'dan sonra (en az seviye 3, boss yokken) ~110 sn'de bir sıradan polislerden ekran kenarında tam halka: `10 + seviye × 0.6` (en fazla 16). Hepsi 12 sn kovalar; ekranda **KUŞATILDIN! · Yararak çık!** bandı. Polis tavanını en fazla 8 aşabilir (`_encircle*`).
- **Kalabalık kafes olmaz:** düşmanlar (boss hariç) blob'la katı çarpışmaz. Blob içlerine girince yana itilirler, blob itilen düşman başına yavaşlar (`1/(1+0.15×n)`, en az %60 hız; dönme dolap formunda yavaşlamaz). Cana hasar alınca 0,35 sn dokunulmazlık ve blob yüzeyine 1,2 birim yakın düşmanlar 1,1 birim geri sekilir (`BlobHealth.PushEnemiesBack`). Tehlike hasardan gelir, sıkışmaktan değil.
- **Can barı:** polis ve elit vurulunca kafasının üstünde kırmızı can barı belirir (kameraya döner; hiç vurulmamış polisde görünmez). Boss'ların barı HUD'da.
- **Ödüller:** silahla öldürmek XP verir (yukarıda, §3); coin VS gibi her düşmandan değil: bekçi ve robot köpek %25 olasılıkla 4 coin, polis %40 olasılıkla 5 coin (ortalama eskisi gibi 1 / 1 / 2; `EnemyData._coinDropChance` + `_coinOverride`), çevik kuvvet / motorlu / elit / boss her zaman; elit ve boss'lar sandık (ekstra kart + coin); boss'lar bonus coin. Kalp: polis %5, elit %100 (+20 can, max canı aşmaz). Can dolu olsa da değince alınır (boşa gider).
- Coin 20 sn sonra kaybolur (son 3 sn yanıp söner).

---

## 7. Şehrin Canlıları

- **Yayalar** (en fazla 25): Erkek (11 görünüm), Kadın (8), İri (işçi/kurye/taşıyıcı, 3 görünüm — yavaş, 3× kütle). Rastgele dolaşır, yiyebileceğin boydaysan kaçar, eşyalara ve arabalara çarpar. Yürüyüş: bütün görünümler parçalı: bacaklar karşılıklı, kollar karşı bacakla adım ritminde sallanır (gidilen yola bağlı, dururken düzleşir; `PedestrianRig`, `PedestrianData._stepRate/_legSwing/_armSwing`); kolu ayrılamayanlar (kutulu kurye, etekli/paltolu kadınlar) sadece bacakla yürür. Üstüne yukarı-aşağı süzülme. Parçalar `Tools → Blob.io → Yaya Yürüyüş Parçalarını Kur`. Vakumla çekilmez.
- **Arabalar** (en fazla 6): oyuncunun yakınındaki blokların etrafında sağ şeritte döner, uzaklaşınca havuza döner. Önlerinde araba, yaya ya da polis varsa durur. Medium'dan itibaren yenir (otobüs Large); küçükken çarpınca hasar verir. Yanından geçtiği küçük çöpleri iter.
- **Sokak eşyaları** (ağaç, lamba, bank, hidrant, çöp kutusu, trafik ışığı, seyyar arabalar; parkların ortasında ve lunaparkta park etmiş dondurma kamyoneti) ve **binalar** yenebilir; yenen her şey run sonunda geri gelir.
- Yenebilir küçük objeler (en fazla 80) oyuncunun çevresinde sürekli yenilenir.

---

## 8. Harita

- **Tek harita: Modern Şehir**, 172×172 birim (11×11 blok). Blok 8×8 kaldırım adası, aralarda **7 birim** geniş yollar (polis sürüsünden kaçmak için; `CityExpansion.BlockPitch` 15 = 8 + 7, Blender `build_city2.py` PITCH ile aynı olmalı). Blok içleri `build_city2.py` mahalle yerleşiminden.
- Şehir bir ada: kenarlar boyunca mazgallı taş sur/rıhtım, köşelerde kuleler, dışı yavaşça kayan su (sadece görsel, yenmez). Sınırı görünmez duvar tutar.
- Bina adalarının zemini kare karo kaldırım (4×4 birimde tekrar eden doku).
- Binalar şehir kitinden (Blender, tek palet dokusu). Uzun binalar oynanış için kısaltılmış (en uzun kule ~6.4).
- Kameranın önüne giren bina saydamlaşır (alfa 0.18, `CityVisualFixes.GhostAlpha`).
- Yol bulma: iki NavMesh — normal ajanlar için ve tank için (geniş). Sokak eşyaları sadece normal ajanın NavMesh'inde engel.

---

## 9. Ekonomi ve Market

- **Coin** run içinde toplanır; run sonunda **kasaya** girer (kazanınca %100, ölünce %50). Oyuncu tek para görür: coin (kodda kasa `MetaProgression.Credit`).
- **Skor** sadece rekor göstergesi, hiçbir şey satın almaz.

- **Coin kaynakları:** bekçi ve robot köpek ort. 1 (%25 × 4), polis ort. 2 (%40 × 5); anlık görev `15 + dakika × 3` (§3); elit 5–10 + sandık 35 (+ ekstra kart); boss'lar da elit sayılır: 5–10 + sandık 35 + bonus (1. miniboss 15–25, 2. miniboss 25–40, helikopter 60–100). Coin yerde 20 sn kalır. Değerlerin güncel tablosu: `python3 -I .claude/skills/denge/denge_tablosu.py`.

| Market kalemi | Fiyat | Sınır |
|---|---|---|
| Mıknato (karakter) | 500 | tek sefer |
| Fişek (karakter) | 1.000 | tek sefer |
| Cano (karakter) | 1.500 | tek sefer |
| +%5 hız | 200, her alımda +150 | Sv.5 (+%25) |
| +10 max can | 200, her alımda +150 | Sv.5 (+50) |
| +%5 kütle kazancı (XP'yi de artırır) | 300, her alımda +200 | Sv.5 (+%25) |
| +%5 coin kazancı | 300, her alımda +200 | Sv.5 (+%25) |
| XP çarpanı +%25 | 1000 | tek sefer |
| Zırh: gelen her hasar −1 (hasarın en az %30'u geçer) | 400, her alımda +300 | Sv.3 |
| Rejenerasyon: saniyede +0,2 can | 300, +250 | Sv.3 |
| Kalın Deri: polis yutarken ısırık −%20 | 300, +300 | Sv.2 |
| Geniş Ağız: yeme tetikleyicisi +%5 | 250, +200 | Sv.3 |
| Sindirim Direnci: büyüyünce yavaşlama −%10 (Hızlı Metabolizma ile toplanır, en fazla %90) | 300, +250 | Sv.3 |
| Mıknatıs: coin ve kalp gövdenin 1 birim yakınından kendine kayar | 200, +150 | Sv.3 |
| **Lanet** (zorluk, VS "Curse"): düşman sayısı (tavan), spawn sıklığı, canı ve hızı +%10, boss canı +%10, karşılığında coin +%10 (Coin Kazancı'yla toplanır). Mutlak düşman tavanı seviye başı sadece +%5 (30 → en fazla 37; mobil). `WaveController.Curse` | 100, +100 | Sv.5 |

**Sıfırla** (GÜÇLER'in en altında, VS "Refund"): bütün güçlendiriciler (eski statlar dahil, karakterler hariç) sıfırlanır, harcanan coin'in tamamı geri gelir. Yanlışlıkla basılmasın diye iki dokunuş: ilki "EMİN MİSİN?" der, 3 sn içinde ikincisi sıfırlar (`MetaProgression.RefundPowerUps`).

Market üç sekme: **KARAKTERLER**, **GÜÇLER** (kategorili, kaydırmalı: Savunma · Büyüme ve Yeme · Ekonomi · Zorluk, en altta Sıfırla) ve **KİLİTLER** (oynayarak açılan skiller: koşul + ilerleme çubuğu; altında başlangıç skilleri).

**Skill kilitleri (VS gibi, coin'le değil oynayarak):** başlangıçta 8 skill açık (Sopa, Havai Fişek, Vakum, Zırh, Sindirim, Hız, Can Artışı, Rejenerasyon); diğerleri koşul gerçekleşince kart havuzuna girer, o an **YENİ SKİLL! · <ad> açıldı** bandı çıkar. Karakterin başlangıç ve favori skill'i o karakterle kilitli olsa da çıkar. İlerleme kayıtta (`unlockStats`), sayan `UnlockTracker`; koşullar `SkillUnlocks` (`Systems/Meta/PowerUps.cs`).

| Skill | Açılma koşulu |
|---|---|
| Geri Dönüşüm | Bir run'da 10 (hareketli) araba ye |
| Hızlı Metabolizma | Dev boyuna ulaş |
| Kaygan Zemin | Bir run'da 10 dakika hayatta kal |
| Tetikçi | Toplam 300 polis indir (silahla; köpek sayılmaz) |
| Yara Öfkesi | Canın %20'nin altındayken bir boss yen |
| Yer Sarsıntısı | Bir tank yen |
| Sindirim Gazı | Bir run'da 30 polis yut |
| Bölünme | Helikopteri yut | Güçlendiriciler `Systems/Meta/PowerUps.cs` kataloğunda (id, fiyat, etki), kayıtta id ile tutulur; run başında `GameManager.ApplyMetaProgression` uygular. Saldırı (Güç, Alan, Bekleme, Süre, Ek atış) ve kart güçlendiricileri (Şans, Kart yenile/atla/yasakla, Diriliş) sonraki paketlerde.

Sınıra gelen satırda fiyat yerine MAX yazar. Hepsinin toplamı 16.000 coin (karakterler 3.000). Coin kazancı kesirli birikir (1 coin × 1.05 kaybolmaz). Kayıt yerel JSON'da (`meta_progression.json`).

### Denge hedefleri

Fiyatlar ve ödüller bu hedeflere göre ayarlanır; ölçülen değer hedeften saparsa değer değişir, hedef değil.

| Hedef | Değer |
|---|---|
| Run süresi: yeni oyuncu (Market boş) | ilk run'larda 10–15. dk'da ölür |
| Run süresi: güçlendiriciler açıldıkça | 25–30 dk (final boss / run sonu) |
| Mıknato açılışı | 2.–3. run |
| Fişek / Dalcık / Cano açılışı | ~5. / 5. (run sayısı) / ~7. run |
| Market'in yarısı (~8.000 coin) | ~17. run |
| Market'in tamamı (16.000 coin) | ~45. run |
| Coin'in elit + sandıktan gelen payı | %40–55 (kaçan/toplayan oyun da kazansın) |
| Yerde kaybolan coin | toplananın %15'inden az |
| Run başına kart (ölüm / kazanma) | ~12 / ~30 |

**Ölçüm:** her run `analytics.json`'a yazılır (son 100): süre, karakter, son seviye, seviye zamanları, toplanan / kasaya giren / kaybolan coin, coin'in kaynağa göre dağılımı (polis, elit, boss, sandık, görev), çıkan / tamamlanan görev, öldürülen polis/elit/boss, seçilen kartlar. DEV panelinde (COIN / KAYIT) son 10 ölçülen run'ın ortalaması görünür.

---

## 10. Kontrol, Kamera, Arayüz

- **Dokunmatik:** görünmez sürükleme — parmağın bastığı yerden sürüklediğin yön ve mesafe hareket olur (80 px = tam hız). UI butonuna basınca hareket başlamaz. **Klavye:** WASD / ok tuşları.
- **Kamera:** üstten 60° eğik, dar açılı (FOV 45: perspektif bozulması az, minyatür şehir görünümü), başlangıçta 18 yukarıda (`CameraController._baseHeight` / `_baseTilt` / `_fieldOfView`); gölge mesafesi ve boss kadrajı FOV'a göre ayarlanır; blob büyüdükçe uzaklaşır: yükseklik × (ölçek / başlangıç ölçeği)^0.35 (`CameraController._zoomExponent`; Medium ~×1.6, Dev ~×2.2, en fazla ×4). Blob ekranda yine büyür (Dev'de başlangıçtakinin ~4.5 katı); hasar alınca kısa sarsıntı; final boss gelince ikisini birden kadraja alır.
- **Geri bildirim:** yemede hafif titreşim (iOS haptic), hasarda kırmızı parlama, kalkan vuruşunda kalkan parlar. **Ateş:** her atışta blob ateş yönünün tersine kısa geri teper; mermi namludan iri çıkıp normale iner. **Vuruş:** düşman kısa süre kızarır (boss'lar dahil), boss olmayan geri seker (sopa ve Yer Sarsıntısı). **Ölüm (silahla, boss hariç):** 0,09 sn beyaza parlayıp şişer, sonra kendi renklerinde kırıntılara dağılır (`EnemyData._crumbColorA/B`; köpek gümüş, polis mavi, çevik kuvvet antrasit + kalkan mavisi, motorlu beyaz + mavi) ve ayağından toz kalkar (`DeathFx`, yemedeki kırıntı parçacığı). Coin cesetten yay çizerek fırlar. Hafif titreşim (sürüde en fazla 0,12 sn'de bir); elitte güçlü titreşim + 0,05 sn vuruş durması (`HitStop`, sadece oyun normal hızdayken). **Höpleme:** Medium altındaki lokmanın (yerdeki yiyecek, yaya, bank, çalı, hidrant…) görsel kopyası 0,14 sn'de dönüp küçülerek blob'un ortasına çekilir (`SlurpFx`; şehir eşyasında `CityConsumable._slurpMesh`, `Tools → Blob.io → Höpleme Mesh'lerini Kur`). **Medium ve üstü lokmada** obje kaybolduğu an onun renklerinde kırıntı küpleri fışkırıp blob'a çekilir (boyla ölçekli), görünüm bir an "yutkunur"; Large/Giant'ta orta, Dev'de güçlü titreşim ve hafif kamera sarsıntısı. Kırıntı renkleri modelin palet renklerinden (`Yeme Efektini Kur`).
- **HUD:** barlar (can, XP, boss can barı); can barı max canla uzar (100 can = normal boy, sol kenarı sabit, sağa doğru yumuşakça uzar; en fazla ~230 can boyu, Can Artışı/Market); sol üstte süre, sağ üstte skor + coin (en sağda duraklatma tuşu), XP barının solunda seviye, sağında tier. XP barı sabit boyda (yan ekranda uzamaz). Skill rozeti / karakter ikonu yok.
- **Ekran yönü:** dik ve yan ekran. UI dik ekrana göre çizili (1080 genişlik); yan ekranda yükseklikten ölçeklenir (canvas 1500 birim yüksek, UI dik ekranın ~%72'si boyunda; `AdaptiveCanvasScaler`, her 1080×1920 CanvasScaler'a run başında kendiliğinden eklenir). Kart ekranı HUD'un altına sığmazsa o alana ortalanıp küçülür.
- **Ayarlar** (lobide sağ üstte dişli, duraklatma menüsünde Ayarlar): Müzik ve Efekt sesi sürgüsü, Titreşim aç/kapa. Cihazda saklanır (`GameSettings`, PlayerPrefs). Titreşim kapalıyken `Haptics` hiç çalmaz.
- **Ses:** arka planda döngülü yağmur (`Resources/Audio/Ambience/RainLoop`, 60 sn; Müzik sürgüsüne bağlı, müzik gelince aynı kanal). Açılış videosu bitince yavaşça açılır. `AudioManager` kendiliğinden kurulur; efekt sesleri için 8 kaynaklı havuz (`PlaySfx`) hazır, henüz efekt sesi yok. Import kuralı `AudioImportRules`: `Resources/Audio/Ambience|Music` Vorbis + streaming, diğerleri Vorbis + açılışta çöz.
- **Duraklatma:** tuşu sağ üst köşede, skor/coin kutusunun sağında. Panel tüm ekranı kaplar (çentik ve alt şerit dahil): Devam / Yeniden Başla / Ana Menü / Ayarlar. Ana Menü run'ı terk eder (coin kasaya girmez, run sayılmaz).
- **Haber bandı** (üstte, Plague Inc. tarzı): 30 sn'de bir yeni başlık; blob büyüdükçe haberler onunla ilgili olur. Metinler `NewsHeadlines.asset`'te.
- Uygulama arka plana atılınca oyun otomatik duraklar.
- **Ekranlar:** Açılış logosu, giriş, lobi, Market, kart seçimi, Game Over / KAZANDIN.
- **Market ekranı:** lobiden açılır, lobi diliyle (aynı başlık/coin rozeti/geri tuşu yerleşimi, altın "MARKET" başlığı). Başlığın altında iki sekme: **KARAKTERLER** (açılışta; Topik hariç her karakter: portre, ad, pasif, fiyat hapı ya da Dalcık için "x/5 run" + ilerleme çubuğu; açılınca yeşil AÇIK) ve **GÜÇLER** ("KALICI GÜÇLER" satırları: ad, seviye (Sv. x), etki + şu anki toplam, sağda fiyat hapı (yetiyorsa altın, yetmiyorsa gri, alınmışsa yeşil ALINDI/MAX)). Satıra dokunmak satın alır (satır ve coin rozeti zıplar); coin yetmezse rozet kırmızı sallanır. Düzen `MarketPanel` içinde kodla kurulur.
- **Seviye atlama ekranı:** HUD diliyle, tüm ekran. Gölgeli altın "SEVİYE ATLADIN!" + "Bir güç seç"; 3 koyu kart: ad, sağ üstte rozet (YENİ yeşil / mavi Sv. x/5 şu anki seviye / EVRİM altın), altında tür (AKTİF / PASİF / NADİR), seviyeye özel açıklama; evrim kartı altın tonlu. Kartlar sırayla belirir; yanlışlıkla seçilmesin diye ilk 0.4 sn dokunulmaz.
- **Oyun sonu ekranı:** HUD diliyle (yarı saydam gece mavisi panel, kalın beyaz yazı, coin altın), tüm ekran. Başlık GAME OVER (kırmızımsı) ya da KAZANDIN! (altın) + alt satır, rekor kırıldıysa YENİ REKOR! rozeti, kart: Süre / Skor / Rekor / Toplanan coin / Kazanılan coin (+kasaya giren; ölümde toplananın %50'si, kazanınca %100). Kart yumuşakça açılır, skor ve coin'ler sayarak yükselir. Butonlar: Tekrar Oyna, Ana Menü (lobiye döner, coin zaten kasaya girmiştir).
- Ses yok (henüz).

---

## 11. Görsel Stil

- 2.5D stilize 3D, low-poly; şehir, insanlar, araçlar, boss'lar ve kalkanlar tek bir palet dokusunu paylaşır.
- Yumuşak post-processing (tonemapping, hafif kontrast/doygunluk, vinyet, FXAA); bloom/SSAO yok (mobil).
- **Yürüyüş (polis, elit, çevik kuvvet):** model gövde + bacak + kol parçalarına bölünmüş; bacaklar karşılıklı, kollar karşı bacakla adım ritminde sallanır (gidilen yola bağlı, vururken/nişan alırken düzleşir; `LimbWalk`). Çevik kuvvetin kalkan tutan sol kolu sabit. Kurulum `Tools → Blob.io → Polis Yürüyüş Parçalarını Kur`. Motorlu polis ve robot köpek henüz yürümüyor.
- Karakterler ara ara göz kırpar (gözler kapalı mesh'i, `BlobTierMeshes`). Animasyon yok; saldırı hissi koddan: polis hamlesi, geri tepme, hasar parlaması, kamera sarsıntısı.
- **Uygulama ikonu:** Topik'in yakın portresi, gece mavisi zeminde, sağ yanağında kırıntılar (`Art/AppIcon/AppIcon.png` ← `zorse-blender/build_app_icon.py`, varyant A). TestFlight aracı her build'de tüm iOS ikon boyutlarına uygular.
- Eksik: skill ikonları, son form VFX'leri (issue'larda).

---

## 12. Performans ve Teknik

| Hedef | Değer |
|---|---|
| Mobil | 30 FPS @ 720p |
| Batarya | 15 dk'lık run'da ≤ %8 |
| Yükleme | < 3 sn |
| PC (port) | 60 FPS @ 1080p |

- Her şey havuzdan gelir (düşman, yaya, araba, coin, mermi, consumable); runtime'da Instantiate/Destroy yok.
- Sürü polisi ve yayalar basit steering kullanır (NavMeshAgent yok); elit ve boss'lar NavMeshAgent. Sürü hareketi kalabalıkta titremesin diye: hız net kuvvetle orantılı (birbirini iten polis yavaşlar, zikzak yapmaz), ivme sınırlı (7 birim/sn²), birbirini itme yumuşak ve sınırlı (yakınlıkla 0→1, toplam en fazla 1, karelere yayılır), gövde hedefe bakar (yana itilme döndürmez), engelden kaçış yönü engel bitene kadar korunur, köşe dibinde düz yol/dolanma arasında hemen gidip gelmez (`EnemyBase.ApplySteeringMovement`, `ComputeAvoidance`).
- AI sorguları 0.1–0.4 sn aralıkla ve kaydırılarak çalışır.
- Kodlama kuralları ve teknik ayrıntılar `CLAUDE.md`'de.

---

## 13. Yön (karar verilmiş, henüz kodda yok)

- **Gelir:** rewarded reklam (kart yeniden çekme, run sonu bonus) + küçük kozmetik set. Para mekanik avantaj satın alamaz.
- **Yayın:** self-publish + organik (TikTok / Reels / Shorts); ana varlık "minik blob polisten kaçar → dev blob polisi yutar" klibi. Önce küçük bir test pazarında soft launch, metrik kapıları D1 ≥ %35, D7 ≥ %12. Ayrıntı `DISCOVERY_STRATEGY.md`.
- **Post-launch içerik:** Medieval haritası, hava durumu olayları, Grimoire (koleksiyon), NG+ zorlukları, karakter meta ağaçları, yeni boss'lar ve evrimler, PC/Steam premium port.
