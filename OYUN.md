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

0. **Açılış (sadece uygulama ilk açılırken):** siyah ekranda Zorse Studio logosu belirir (~2.2 sn), söner (`StudioSplash`, logo `Resources/UI/zorse-logo.png`).
1. **Giriş ekranı:** arkada canlı şehir, kamera Topik'e yakın; Topik göz kırpar, sağa sola bakınır. Logo + OYNA. Duraklatma ve oyun sonundaki "Ana Menü" de buraya döner (run sıfırlanır).
2. **Lobi:** OYNA'dan sonra karakter seç (Topik, Mıknato, Mermo), Market'e gir. HUD diliyle: başlık, coin, her karakter için kart (portre `Art/UI/Characters/Icon_*.png` ← `build_blobs.py`, ad, pasif açıklaması; kilitliyse soluk + koşul rozeti), altın MARKET tuşu ve geri (giriş ekranına) tuşu. Karta dokunmak oyunu başlatır.
3. Şehirde başlarsın; yerdeki çöpleri, sokak eşyalarını, yayaları yiyerek büyürsün. Senden büyük şeyler seni durdurur (katı), hazard'lar hasar verir.
4. Kütle = XP; yenen her şeyin kütlesi genel büyüme hızıyla (×0.35, `BlobGrowth._globalGainScale`) çarpılır. Silahla öldürülen düşman sadece XP verir, kütle vermez (büyüme yemeğe bağlı): polis 1, elit 7, 1. tank 80, 2. tank 130 (`EnemyData._killXp`). Level atlayınca oyun durur, **3 karttan 1** seçilir. Aynı skill tekrar seçilirse seviye atlar (en fazla 8).
5. Polisler gelir, zamanla ve büyüdükçe baskı artar; boss'lar belirli **seviyelerde** gelir (takılan oyuncuya en geç belirli bir dakikada).
6. Run iki şekilde biter:
   - **Ölüm** → "Game Over!" ekranı, coin'in %50'si kasaya.
   - **Final boss (helikopter) yutulursa** → "KAZANDIN!" ekranı, coin'in %100'ü kasaya.
7. Kasadaki coin Market'te kalıcı statlara ve Mıknato'ya harcanır.

### Zaman çizelgesi

| Zaman | Olay |
|---|---|
| 0–1 dk | Sadece yenebilir objeler, yayalar, arabalar |
| 1. dk | Polisler gelmeye başlar |
| Blob **Medium** olunca (en geç 4. dk) | Elit polisler |
| 5. dk | Düşman hasarı ×1.2 |
| 10. dk | Düşman spawn'ı ×2 hızlı (tavan aynı) |
| 15. dk | Düşman hızı ×1.15 |
| 20. dk | Düşman hasarı ×3 |

Boss'lar seviyeye bağlı, en geç süre yedek:

| Boss | Seviye | En geç |
|---|---|---|
| 1. Miniboss (polis tankı) | **5** (~310 kütle, Medium) | 9. dk |
| 2. Miniboss (güçlü tank) | **8** (~1030 kütle, Large) | 15. dk |
| **Final boss: polis helikopteri** | **10** (~2100 kütle, Dev) | 21. dk |

Boss'lar sırayla ve tek tek gelir: biri yaşıyorsa sonraki onun ölmesini bekler; final boss beklemez (miniboss sahnedeyse çekilir). Kart ekranı açıkken boss çıkmaz. Seviye birden ikisini aşarsa sadece sonraki çıkar. Değerler `BossData_Miniboss.asset` → `RequiredLevel` / `LatestTime`.

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
- **Polis yutulurken ısırır:** tier tam yetiyorsa 5, bir tier üstündeysen 2.5, daha büyüksen 0 hasar.
- **Polis kaçar:** blob onu yiyebilecek boydayken görünce kaçar.
- **Hazard'lar** (ör. araba çarpması) yiyemeyeceğin boydayken hasar verir.
- Final boss istisnadır: tier değil **fiziksel uzanma** gerekir (bkz. §6).

---

## 5. Skill'ler ve Karakterler

Ayrıntı ve tüm sayılar **`SKILLS.md`**'de. Özet:

- **8 skill:** Silah Gücü, Havai Fişek (ikinci silah, boss avcısı), Zırh (kalkan), Can Emme (vurduğun hasarın %2/sv'si can), Can Artışı, Hız, Attract (Vakum), Sindirim.
- **1 evrim:** Attract + Sindirim (max) → **Kara Delik**. Evrim kartı ★ ve altın renkle gelir. Seçilince evrim anı oynar (ağır çekim, kasırga blob'a çöker, indigo şok dalgası, ekranda ★ KARA DELİK); sonra blob'un altında siyah olay ufku ve siyah-lacivert kasırga kalır.
- Max seviyedeki skill havuzdan çıkar; birden çok level aynı anda atlanırsa kartlar sırayla gelir; run bittiyse kart açılmaz.
- **3 karakter**, hepsi top formunda, silahlar otomatik ateşler. Ateş tarzı üçünde aynı (düz, hareketi önden kesen hızlı mermi; sadece mermi modeli farklı); fark Silah Gücü Sv.8 son formunda:
  - **Topik** — top mermisi; pasif: run'a **Hız Sv.2** ile başlar (kaykay baştan var). Baştan açık. Hızlı giderken arkasında hafif rüzgar çizgileri (iki yandan ve tepeden üç ince beyaz iz; `CharacterData._speedTrail`, `SpeedTrail`).
  - **Mıknato** — metal bilye; pasif: Vakum Sv.2 ile başlar. Market'te 500 coin.
  - **Mermo** — tabanca mermisi; pasif: run'a **Silah Gücü Sv.2** ile başlar (`CharacterData._startingUpgrade`). **3 run bitirince** açılır (ölüm de sayılır).
- **İki silah:** karakter silahı **her yöne** (360°) ama kısa menzille (gövde yüzeyinden 3, Silah Gücü ile 5'e kadar) ateş eder: kaçarken kovalayanı savuşturur, haritayı temizlemez. **Havai Fişek** (kart, Sv.4'te bir kez garanti; 1. miniboss Sv.5'te) uzun menzilli (gövde yüzeyinden 12), yavaş, tek hedefe ağır; sadece **önündeki 120°'ye**, önce boss'a sonra elite nişan alır. Dönünce 1 sn kurulur (fitil kıvılcımı = hazır). Boss ritmi: dön, dur (yüz son yönde kalır), kurulsun, ateş et; lazer kilitlenince yana kaç. Tam tepedeki hedef (helikopter) her zaman vurulur.
- Silah havadaki hedefe (helikopter) otomatik yukarı nişan alır; menzil yatay ölçülür.
- **Mermiler** karaktere özel model + renkli iz: Topik turuncu şeritli top (döner), Mıknato kırmızı-mavi bantlı çelik bilye (döner), Mermo pirinç kovanlı mermi. Blob büyüdükçe mermi de büyür (Tiny 1× → Dev ~3×, kamera uzaklaştığı için). Modeller `zorse-blender/build_bullets.py`.
- **Görünür silah** (Silah Gücü'yle form değiştirir; ayrıntı `SKILLS.md`): Topik ve Mıknato'da Silah Gücü alınınca sol yanda belirir (kalkan sağda), Mermo'nun iki yanında baştan keleş var (kalkanı kafada). Silah gövdeye sabit, gidiş yönüne bakar. **Havai Fişek atarı** sol omuzda, ana silahın üstünde (sol = saldırı, sağ = kalkan); renkli roketler, isabette renkli kıvılcım.

---

## 6. Düşmanlar ve Boss'lar

Sayılar **`SKILLS.md` Bölüm 5**'te.

| Düşman | Nasıl davranır | Nasıl ölür |
|---|---|---|
| **Polis** (sürü) | Koşar, temasta coplar. Binaların, eşyaların, arabaların etrafından dolanır; yiyebileceğin boydaysan kaçar | Silah ya da Medium'dan itibaren yutma |
| **Elit polis** | Durur, nişan lazeriyle takip eder, kilitleyip 3'lü mermi atar. Arada bina varsa ateş etmez | Silah ya da Giant'ta yutma. Sandık düşürür |
| **Miniboss: Polis Tankı** (5. ve 8. seviye) | 2.5 kat büyük; nişan lazeri + kilit + tank topu. Üstünden geçtiği arabaları ve sokak eşyalarını ezer (eşya kendi renklerinde kırıntı + tozla dağılır, `CrushFx`), binaların etrafından dolanır. Uzaktaysa yetişmek için hızlanır | Sadece silahla, yutulamaz. Ölünce ~1.3 sn: beyaz parlama, titreme, patlama kırıntıları, yana yatıp yere gömülür |
| **Final Boss: Polis Helikopteri** (10. seviye) | Havada uçar, yukarıdan mini roket serisi atar (alevli, değince patlar: alan hasarı). Vurdukça kademe kademe alçalır (irtifa 8 → 7 → 6 → 5), yerde koyulaşan gölgesi ve dönen pervane gölgesi var. Gelince kamera uzaklaşıp ilk 3 sn ikinizi kadraja alır; sonra uzaklık kalır, kamera blob'u ortalar (boss ölünce normale döner; `CameraController._bossIntroDuration`) | **Silahla ölmez.** Blob'un tepesi helikoptere değdiği an yutulur, hangi irtifada olursa olsun (irtifa 5 ≈ 1340 kütle). **Yutulma sahnesi** (~4 sn, blob hasar almaz): yavaş çekimde helikopter dönerek küçülüp kıvılcım iziyle blob'a emilir, kamera yaklaşır, kocaman yutkunma + patlama + güçlü titreşim; zaman normale döner, ~1.3 sn sonra geğirme (şişme + ağızdan kırıntı bulutu), kısa bekleme; sonra KAZANDIN |

- **Eşzamanlı (seviyeye göre):** toplam düşman tavanı `8 + seviye × 2.2`, elit tavanı `2 + (seviye − 5) × 1.5` (en az 1). **Mutlak üst sınır: toplam 30, elit 8** — hiçbir çarpan aşamaz. Spawn aralığı seviye başı %10 kısalır (seviye 10'da 2 kat sık). `EnemySpawner._policeCap*`, `_eliteCap*`, `_spawnSpeedupPerLevel`. 25 yaya.
- **Can barı:** polis ve elit vurulunca kafasının üstünde kırmızı can barı belirir (kameraya döner; hiç vurulmamış polisde görünmez). Boss'ların barı HUD'da.
- **Ödüller:** silahla öldürmek XP verir (yukarıda, §3); her düşman coin düşürür; elit ve boss'lar sandık (ekstra kart + coin); boss'lar bonus coin. Kalp: polis %5, elit %100 (+20 can, max canı aşmaz). Can dolu olsa da değince alınır (boşa gider).
- Coin 20 sn sonra kaybolur (son 3 sn yanıp söner).

---

## 7. Şehrin Canlıları

- **Yayalar** (en fazla 25): Erkek (11 görünüm), Kadın (8), İri (işçi/kurye/taşıyıcı, 3 görünüm — yavaş, 3× kütle). Rastgele dolaşır, yiyebileceğin boydaysan kaçar, eşyalara ve arabalara çarpar. Animasyonsuz (yukarı-aşağı süzülme). Vakumla çekilmez.
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

- **Coin kaynakları:** polis 2; elit 5–10 + sandık 35 (+ ekstra kart); boss'lar da elit sayılır: 5–10 + sandık 35 + bonus (1. miniboss 15–25, 2. miniboss 25–40, helikopter 60–100). Coin yerde 20 sn kalır. Değerlerin güncel tablosu: `python3 -I .claude/skills/denge/denge_tablosu.py`.

| Market kalemi | Fiyat | Sınır |
|---|---|---|
| Mıknato (karakter) | 500 | tek sefer |
| +%5 hız | 200, her alımda +150 | Sv.5 (+%25) |
| +10 max can | 200, her alımda +150 | Sv.5 (+50) |
| +%5 kütle kazancı (XP'yi de artırır) | 300, her alımda +200 | Sv.5 (+%25) |
| +%5 coin kazancı | 300, her alımda +200 | Sv.5 (+%25) |
| XP çarpanı +%25 | 1000 | tek sefer |

Sınıra gelen satırda fiyat yerine MAX yazar. Hepsinin toplamı 13.500 coin. Coin kazancı kesirli birikir (1 coin × 1.05 kaybolmaz). Kayıt yerel JSON'da (`meta_progression.json`).

### Denge hedefleri

Fiyatlar ve ödüller bu hedeflere göre ayarlanır; ölçülen değer hedeften saparsa değer değişir, hedef değil.

| Hedef | Değer |
|---|---|
| Mıknato açılışı | 2.–3. run |
| Market'in yarısı (~6.750 coin) | ~15. run |
| Market'in tamamı (13.500 coin) | ~40. run |
| Coin'in elit + sandıktan gelen payı | %40–55 (kaçan/toplayan oyun da kazansın) |
| Yerde kaybolan coin | toplananın %15'inden az |
| Run başına kart (ölüm / kazanma) | ~12 / ~30 |

**Ölçüm:** her run `analytics.json`'a yazılır (son 100): süre, karakter, son seviye, seviye zamanları, toplanan / kasaya giren / kaybolan coin, coin'in kaynağa göre dağılımı (polis, elit, boss, sandık), öldürülen polis/elit/boss, seçilen kartlar. DEV panelinde (COIN / KAYIT) son 10 ölçülen run'ın ortalaması görünür.

---

## 10. Kontrol, Kamera, Arayüz

- **Dokunmatik:** görünmez sürükleme — parmağın bastığı yerden sürüklediğin yön ve mesafe hareket olur (80 px = tam hız). UI butonuna basınca hareket başlamaz. **Klavye:** WASD / ok tuşları.
- **Kamera:** üstten 60° eğik; blob büyüdükçe uzaklaşır: yükseklik × (ölçek / başlangıç ölçeği)^0.35 (`CameraController._zoomExponent`; Medium ~×1.6, Dev ~×2.2, en fazla ×4). Blob ekranda yine büyür (Dev'de başlangıçtakinin ~4.5 katı); hasar alınca kısa sarsıntı; final boss gelince ikisini birden kadraja alır.
- **Geri bildirim:** yemede hafif titreşim (iOS haptic), hasarda kırmızı parlama, kalkan vuruşunda kalkan parlar. **Ateş:** her atışta blob ateş yönünün tersine kısa geri teper; mermi namludan iri çıkıp normale iner. **Vuruş:** düşman kısa süre kızarır (boss'lar dahil), boss olmayan geri seker (mermi az, Topik son form patlaması orta, Zırh dikenleri güçlü). **Medium ve üstü lokmada** obje kaybolduğu an onun renklerinde kırıntı küpleri fışkırıp blob'a çekilir (boyla ölçekli), görünüm bir an "yutkunur"; Large/Giant'ta orta, Dev'de güçlü titreşim ve hafif kamera sarsıntısı. Kırıntı renkleri modelin palet renklerinden (`Yeme Efektini Kur`).
- **HUD:** barlar (can + altında kalkan barı (Zırh varsa), XP, boss can barı); can barı max canla uzar (100 can = normal boy, sağ kenarı sabit, sola doğru yumuşakça uzar; en fazla ~230 can boyu, Can Artışı/Market); sol üstte süre, sağ üstte skor + coin, XP barının solunda seviye, üstünde tier. Skill rozeti / karakter ikonu yok.
- **Duraklatma:** tuşu üst satırda süre kutusunun yanında. Panel tüm ekranı kaplar (çentik ve alt şerit dahil): Devam / Yeniden Başla / Ana Menü. Ana Menü run'ı terk eder (coin kasaya girmez, run sayılmaz).
- **Haber bandı** (üstte, Plague Inc. tarzı): 30 sn'de bir yeni başlık; blob büyüdükçe haberler onunla ilgili olur. Metinler `NewsHeadlines.asset`'te.
- Uygulama arka plana atılınca oyun otomatik duraklar.
- **Ekranlar:** Açılış logosu, giriş, lobi, Market, kart seçimi, Game Over / KAZANDIN.
- **Market ekranı:** lobiden açılır, lobi diliyle (aynı başlık/coin rozeti/geri tuşu yerleşimi, altın "MARKET" başlığı). Üstte Mıknato kartı (portre, pasif, "YENİ KARAKTER", fiyat), altında "KALICI GÜÇLER" satırları: ad, seviye (Sv. x), etki + şu anki toplam, sağda fiyat hapı (yetiyorsa altın, yetmiyorsa gri, alınmışsa yeşil ALINDI/AÇIK). Satıra dokunmak satın alır (satır ve coin rozeti zıplar); coin yetmezse rozet kırmızı sallanır. Düzen `MarketPanel` içinde kodla kurulur.
- **Seviye atlama ekranı:** HUD diliyle, tüm ekran. Gölgeli altın "SEVİYE ATLADIN!" + "Bir güç seç"; 3 koyu kart: ad, sağ üstte rozet (YENİ yeşil / Sv. x/8 / EVRİM altın), seviyeye özel açıklama; evrim kartı altın tonlu. Kartlar sırayla belirir; yanlışlıkla seçilmesin diye ilk 0.4 sn dokunulmaz.
- **Oyun sonu ekranı:** HUD diliyle (yarı saydam gece mavisi panel, kalın beyaz yazı, coin altın), tüm ekran. Başlık GAME OVER (kırmızımsı) ya da KAZANDIN! (altın) + alt satır, rekor kırıldıysa YENİ REKOR! rozeti, kart: Süre / Skor / Rekor / Toplanan coin / Kazanılan coin (+kasaya giren; ölümde toplananın %50'si, kazanınca %100). Kart yumuşakça açılır, skor ve coin'ler sayarak yükselir. Butonlar: Tekrar Oyna, Ana Menü (lobiye döner, coin zaten kasaya girmiştir).
- Ses yok (henüz).

---

## 11. Görsel Stil

- 2.5D stilize 3D, low-poly; şehir, insanlar, araçlar, boss'lar ve kalkanlar tek bir palet dokusunu paylaşır.
- Yumuşak post-processing (tonemapping, hafif kontrast/doygunluk, vinyet, FXAA); bloom/SSAO yok (mobil).
- Karakterler ara ara göz kırpar (gözler kapalı mesh'i, `BlobTierMeshes`). Animasyon yok; saldırı hissi koddan: polis hamlesi, geri tepme, hasar parlaması, kamera sarsıntısı.
- Eksik: skill ikonları, kalıcı uygulama ikonu, son form VFX'leri (issue'larda).

---

## 12. Performans ve Teknik

| Hedef | Değer |
|---|---|
| Mobil | 30 FPS @ 720p |
| Batarya | 15 dk'lık run'da ≤ %8 |
| Yükleme | < 3 sn |
| PC (port) | 60 FPS @ 1080p |

- Her şey havuzdan gelir (düşman, yaya, araba, coin, mermi, consumable); runtime'da Instantiate/Destroy yok.
- Sürü polisi ve yayalar basit steering kullanır (NavMeshAgent yok); elit ve boss'lar NavMeshAgent.
- AI sorguları 0.1–0.4 sn aralıkla ve kaydırılarak çalışır.
- Kodlama kuralları ve teknik ayrıntılar `CLAUDE.md`'de.

---

## 13. Yön (karar verilmiş, henüz kodda yok)

- **Gelir:** rewarded reklam (kart yeniden çekme, run sonu bonus) + küçük kozmetik set. Para mekanik avantaj satın alamaz.
- **Yayın:** self-publish + organik (TikTok / Reels / Shorts); ana varlık "minik blob polisten kaçar → dev blob polisi yutar" klibi. Önce küçük bir test pazarında soft launch, metrik kapıları D1 ≥ %35, D7 ≥ %12. Ayrıntı `DISCOVERY_STRATEGY.md`.
- **Post-launch içerik:** Medieval haritası, hava durumu olayları, Grimoire (koleksiyon), NG+ zorlukları, karakter meta ağaçları, yeni boss'lar ve evrimler, PC/Steam premium port.
