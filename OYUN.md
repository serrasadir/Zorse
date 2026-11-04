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
| **Run süresi** | Hedef 12–15 dk; zaman sınırı yok |
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
5. **Ölüm = ilerleme.** Ölünce run coin'inin %50'si krediye döner.
6. **Sıfır pay-to-win.**
7. **Frame rate > içerik.** Mobilde 30 FPS kaosun zirvesinde de korunur.
8. **Çocuk yaya yok.** Yiyerek büyüme oyununda çocuk yutmak etik ve mağaza riski; eklenmez.

---

## 3. Run Akışı

1. **Lobi:** karakter seç (Topik, Mıknato, Mermo), Market'e gir.
2. Şehirde başlarsın; yerdeki çöpleri, sokak eşyalarını, yayaları yiyerek büyürsün. Senden büyük şeyler seni durdurur (katı), hazard'lar hasar verir.
3. Kütle = XP. Level atlayınca oyun durur, **3 karttan 1** seçilir. Aynı skill tekrar seçilirse seviye atlar (en fazla 8).
4. Polisler gelir, zamanla ve büyüdükçe baskı artar; boss'lar belirli dakikalarda gelir.
5. Run iki şekilde biter:
   - **Ölüm** → "Game Over!" ekranı, coin'in %50'si krediye.
   - **Final boss (helikopter) yutulursa** → "KAZANDIN!" ekranı, coin'in %100'ü krediye.
6. Kredi Market'te kalıcı statlara ve Mıknato'ya harcanır.

### Zaman çizelgesi

| Zaman | Olay |
|---|---|
| 0–1 dk | Sadece yenebilir objeler, yayalar, arabalar |
| 1. dk | Polisler gelmeye başlar |
| Blob **Medium** olunca (en geç 4. dk) | Elit polisler |
| 5. dk | Düşman hasarı ×1.2 |
| **6. dk** | 1. Miniboss (polis tankı) |
| **9. dk** | 2. Miniboss (güçlü tank) |
| 10. dk | Düşman yoğunluğu ×2 |
| **12. dk** | **Final boss: polis helikopteri** |
| 15. dk | Düşman hızı ×1.15 |
| 20. dk | Düşman hasarı ×3 |

Boss'lar sırayla ve tek tek gelir: biri yaşıyorsa sonraki onun ölmesini bekler.

---

## 4. Büyüme, Tier ve Yeme

- **Ölçek:** `0.5 × (1 + kütle × 0.1) ^ 0.45` — Giant (1000 kütle) ≈ 4 birim çap. Yeme anında kısa şişme (punch).
- **Hız:** `1 / tier ^ 0.3` × karakter/skill/meta çarpanları (Giant'ta ~%62).
- **XP eşiği:** `20 + level × 15`.

| Tier | Kütle | Kabaca ne yer |
|---|---|---|
| 1 Tiny | 0 | Çöp, küçük eşyalar |
| 2 Small | 50 | Sokak eşyaları, yayalar |
| 3 Medium | 200 | **Normal polis**, iri yayalar |
| 4 Large | 500 | Arabalar, küçük binalar |
| 5 Giant | 1000 | Büyük binalar, **elit polis** |

- **Yeme temasla olur.** Tier'ın yetmediği obje katıdır (çarparsın, içinden geçemezsin); yetince içinden geçip yersin.
- **Polis yutulurken ısırır:** tier tam yetiyorsa 5, bir tier üstündeysen 2.5, daha büyüksen 0 hasar.
- **Polis kaçar:** blob onu yiyebilecek boydayken görünce kaçar.
- **Hazard'lar** (ör. araba çarpması) yiyemeyeceğin boydayken hasar verir.
- Final boss istisnadır: tier değil **fiziksel uzanma** gerekir (bkz. §6).

---

## 5. Skill'ler ve Karakterler

Ayrıntı ve tüm sayılar **`SKILLS.md`**'de. Özet:

- **9 skill:** Silah Gücü, Zırh (kalkan), Rejenerasyon, Can Artışı, Hız, Attract (Vakum), Sindirim, Yutuş, Yırtıcı Çene.
- **2 evrim:** Attract + Sindirim (max) → **Kara Delik**; Yırtıcı Çene + Silah Gücü (max) → **Avcı Formu**. Evrim kartı ★ ve altın renkle gelir.
- Max seviyedeki skill havuzdan çıkar; birden çok level aynı anda atlanırsa kartlar sırayla gelir; run bittiyse kart açılmaz.
- **3 karakter**, hepsi top formunda, silahlar otomatik ateşler:
  - **Topik** — top (yay çizen, küçük patlamalı); pasif: hız. Baştan açık.
  - **Mıknato** — metal bilye (hedefe bükülen); pasif: Attract ×1.25. Market'te 500 kredi.
  - **Mermo** — tabanca (hızlı, düz); pasif: kendinden 1 tier büyük objeyi vurup 3 yenebilir parçaya böler. Şu an baştan açık (kilit yok — issue #68).
- Silah havadaki hedefe (helikopter) otomatik yukarı nişan alır; menzil yatay ölçülür.

---

## 6. Düşmanlar ve Boss'lar

Sayılar **`SKILLS.md` Bölüm 5**'te.

| Düşman | Nasıl davranır | Nasıl ölür |
|---|---|---|
| **Polis** (sürü) | Koşar, temasta coplar. Binaların, eşyaların, arabaların etrafından dolanır; yiyebileceğin boydaysan kaçar | Silah ya da Medium'dan itibaren yutma |
| **Elit polis** | Durur, nişan lazeriyle takip eder, kilitleyip 3'lü mermi atar. Arada bina varsa ateş etmez | Silah ya da Giant'ta yutma. Sandık düşürür |
| **Miniboss: Polis Tankı** (6. ve 9. dk) | 2 kat büyük; nişan lazeri + kilit + tank topu. Üstünden geçtiği arabaları ve sokak eşyalarını ezer, binaların etrafından dolanır. Uzaktaysa yetişmek için hızlanır | Sadece silahla, yutulamaz |
| **Final Boss: Polis Helikopteri** (12. dk) | Havada uçar, yukarıdan makineliyle seri atar. Vurdukça kademe kademe alçalır (irtifa 8 → 7 → 6 → 5), yerde koyulaşan gölgesi ve dönen pervane gölgesi var. Gelince kamera ikinizi kadraja alır | **Silahla ölmez.** Blob'un tepesi helikoptere değdiği an yutulur, hangi irtifada olursa olsun (irtifa 5 ≈ 1340 kütle). Yutunca run kazanılır |

- **Eşzamanlı:** sahnede en fazla 30 düşman / 10 elit (belgelerde 15 / 4 hedefi var — karar issue #50).
- **Ödüller:** her düşman coin düşürür; elit ve boss'lar sandık (ekstra kart + coin); boss'lar bonus coin. Kalp: polis %5, elit %100 (+20 can).
- Coin 20 sn sonra kaybolur (son 3 sn yanıp söner).

---

## 7. Şehrin Canlıları

- **Yayalar** (en fazla 25): Erkek (11 görünüm), Kadın (8), İri (işçi/kurye/taşıyıcı, 3 görünüm — yavaş, Medium'da yenir, 3× kütle). Rastgele dolaşır, yiyebileceğin boydaysan kaçar, eşyalara ve arabalara çarpar. Animasyonsuz (yukarı-aşağı süzülme). Vakumla çekilmez.
- **Arabalar** (en fazla 6): oyuncunun yakınındaki blokların etrafında sağ şeritte döner, uzaklaşınca havuza döner. Önlerinde araba, yaya ya da polis varsa durur. Large'dan itibaren yenir; küçükken çarpınca hasar verir. Yanından geçtiği küçük çöpleri iter.
- **Sokak eşyaları** (ağaç, lamba, durak, bank, hidrant, çöp kutusu, trafik ışığı) ve **binalar** yenebilir; yenen her şey run sonunda geri gelir.
- Yenebilir küçük objeler (en fazla 80) oyuncunun çevresinde sürekli yenilenir.

---

## 8. Harita

- **Tek harita: Modern Şehir**, 136×136 birim (11×11 blok). Ortası elle tasarlanmış Level01 mahallesi, dışı aynı kitten türetilmiş bloklar ve parklar.
- Kenarlarda görünmez duvar.
- Binalar şehir kitinden (Blender, tek palet dokusu). Uzun binalar oynanış için kısaltılmış (en uzun kule ~6.4).
- Kameranın önüne giren bina saydamlaşır.
- Yol bulma: iki NavMesh — normal ajanlar için ve tank için (geniş). Sokak eşyaları sadece normal ajanın NavMesh'inde engel.

---

## 9. Ekonomi ve Market

- **Coin** run içinde toplanır; run sonunda **krediye** döner (kazanınca %100, ölünce %50).
- **Skor** sadece rekor göstergesi, hiçbir şey satın almaz.

| Market kalemi | Fiyat |
|---|---|
| Mıknato (karakter) | 500 |
| +%5 hız | 200, her alımda +150 |
| +10 max can | 200, her alımda +150 |
| +%5 kütle kazancı | 300, her alımda +200 |
| +%5 coin kazancı | 300, her alımda +200 |
| XP çarpanı +%10 | 1000 (tek sefer) |

Kademeli statlar sınırsız alınabilir. Kayıt yerel JSON'da (`meta_progression.json`); run analitiği de yerel (`analytics.json`, son 100 run).

---

## 10. Kontrol, Kamera, Arayüz

- **Dokunmatik:** görünmez sürükleme — parmağın bastığı yerden sürüklediğin yön ve mesafe hareket olur (80 px = tam hız). UI butonuna basınca hareket başlamaz. **Klavye:** WASD / ok tuşları.
- **Kamera:** üstten 60° eğik; blob büyüdükçe yavaş uzaklaşır (en fazla ×4); hasar alınca kısa sarsıntı; final boss gelince ikisini birden kadraja alır.
- **Geri bildirim:** yemede hafif titreşim (iOS haptic), hasarda kırmızı parlama, kalkan vuruşunda kalkan parlar.
- **HUD:** can barı + altında kalkan barı (Zırh varsa), XP barı, seviye, tier, skor, coin, süre, aktif skill rozetleri, boss can barı.
- **Haber bandı** (üstte, Plague Inc. tarzı): 30 sn'de bir yeni başlık; blob büyüdükçe haberler onunla ilgili olur. Metinler `NewsHeadlines.asset`'te.
- **Duraklatma:** sağ üst buton; uygulama arka plana atılınca otomatik duraklar.
- **Ekranlar:** Lobi, Market, kart seçimi (3 kart, "YENİ" / "Sv. x/8", seviyeye özel açıklama), Game Over / KAZANDIN.
- Ses yok (henüz).

---

## 11. Görsel Stil

- 2.5D stilize 3D, low-poly; şehir, insanlar, araçlar, boss'lar ve kalkanlar tek bir palet dokusunu paylaşır.
- Yumuşak post-processing (tonemapping, hafif kontrast/doygunluk, vinyet, FXAA); bloom/SSAO yok (mobil).
- Karakterler animasyonsuz; saldırı hissi koddan: polis hamlesi, geri tepme, hasar parlaması, kamera sarsıntısı.
- Eksik: karakter ve skill ikonları, kalıcı uygulama ikonu, VFX (issue'larda).

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
