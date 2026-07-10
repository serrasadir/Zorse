# Blob.io — GDD v2.0 (Tek Kaynak Doküman)

> **Bu doküman projenin tek kaynak (single source of truth) tasarım belgesidir.**
> GDD v1.0 PDF'ini (Haziran 2025) ve kök `GDD.md` vizyon taslağını **geçersiz kılar**.
> Temmuz 2026'da, `ADVISORY_BOARD.md` ve `BOARD_EVALUATION.md` bulgularının üzerinden yapılan 13 kararlık soru-cevap oturumuyla yazılmıştır. Çelişki kalmamıştır; bir çelişki bulursan bu doküman kazanır ve diğeri güncellenir.
>
> Balance değerleri ScriptableObject'lerde yaşar; buradaki sayılar başlangıç/hedef değerleridir.

**Slogan:** _"Hayatta kal. Seviyeleri atla. Karanlığı yut."_

---

## 0. Karar Günlüğü (Temmuz 2026 oturumu)

Bu bölüm dokümanın anayasasıdır. Her karar kurulun raporlarındaki bir bulguya karşılık gelir.

| # | Karar | Sonuç |
|---|-------|-------|
| 1 | Run süresi | **12–15 dakika** (20–30 dk ve 5–10 dk vizyonları iptal) |
| 2 | Düşman ölçeği | **150–200 eşzamanlı**; sürü = basit steering, NavMesh sadece elit/boss |
| 3 | Platform | **Mobil önce, F2P** (kurul önerisinin tersi — bilinçli tercih). PC/Steam premium = post-launch port |
| 4 | Sanat yönü | **2.5D stilize 3D** (16-bit pixel art vizyonu iptal); gotik palet korunur |
| 5 | Ana fiil | **Yeme birincil, silah destek** — tier büyüdükçe sürü düşmanları yutulur |
| 6 | Hazard kuralı | **Tam commit** — senden büyük şey sana zarar verir |
| 7 | Ekonomi | **2 sisteme iner**: Coin + Skor (mass=XP zaten birleşik) |
| 8 | Boss kadrosu | **1 miniboss (tekrarlı, artan stat) + 1 final boss (tek faz geçişi, sonunda yenir)** |
| 9 | Çekme mekanikleri | **Tek "Attract" skill hattı**; Mıknato pasifi bunu güçlendirir |
| 10 | Mermo pasifi | ~~Sistemik: can bedeliyle yeme~~ → **REVİZE (10 Tem):** uygulanmış parçalama korunur — `ConsumeAndSplit` mevcut alt-tier havuzundan saçar, **sıfır asset vergisi** (kurulun maliyet itirazının öncülü koddaki çözümle ortadan kalktı) |
| 11 | Ertelemeler | Hava durumu, NG+, Grimoire UI, karakter meta ağaçları → **post-launch** |
| 12 | Harita | **Lansmanda 1 cilalı harita** (Modern Şehir); Medieval = ilk içerik güncellemesi |
| 13 | Go-to-market | **Self-publish + organik**; test pazarında soft launch, creator odaklı büyüme |

**⏸️ Açık kararlar** (bkz. §16): Score Multiplier kartın kaderi · nihai oyun adı.

---

## 1. Künye

| | |
|---|---|
| **Oyun adı** | Blob.io _(çalışma adı; nihai isim açık karar)_ |
| **Kod adı** | Zorse |
| **Tür** | Roguelite / Bullet-Heaven / Survival |
| **Platform** | **Mobil (iOS/Android), F2P** — PC/Steam premium post-launch port |
| **Run süresi** | **12–15 dakika** |
| **Motor** | Unity 6, URP |
| **Ekip** | Hüma (Art/UI-UX) · Serra (Dev/Design) · Bahar (Dev/Design) |
| **Referanslar** | Vampire Survivors · Katamari Damacy · Hole.io · Brotato |
| **İş modeli** | F2P + kozmetik IAP + rewarded reklam; **sıfır pay-to-win** |

### Tek cümlelik pitch
Bir blob'u kontrol et, kendinden küçük her şeyi yutarak büyü, büyüdükçe yavaşla ve güçlen — seni avlayanlar sonunda senin yemeğin olsun. **Saldırı tuşu yok; tek strateji hareket.**

---

## 2. Vizyon & Pillar'lar

### 3 Pillar
1. **Smooth büyüme hissi** — tier atlama ani değil, akıcı (`Pow` formülü). Avatar, ilerleme çubuğunun kendisidir.
2. **Büyüdükçe yavaşla** — `hız = 1/√tier`. Güç ≠ rahatlık; ustalık anında kırılganlık geri gelir.
3. **Her run farklı** — upgrade kombinasyonları + meta unlock'lar.

### Oyunun kalbi: Statü Tersine Dönüşü
Oyunun tasarım merkezi ve pazarlama varlığı tek bir andır: **seni kovalayan şeyin yemeğe dönüşmesi.** Küçükken polisten kaçarsın; Tier 4'te aynı polisi yutarsın. Her sistem bu anı tekrar tekrar üretmek için vardır. Trailer, ilk 10 saniyesinde bu anı gösterir: _minik blob polisten kaçar → kesme → dev blob polisi yutar._

### Anti-vizyon (bu oyun NE değildir)
- Hikâye odaklı değil (environmental storytelling yalnızca).
- PvP değil. Realistik değil — stilize, komik, absürt-gotik.
- Sonsuz run değil — her run final boss'la ya da ölümle 15 dakikada kapanır.
- **Silah vitrini değil** — silahlar destektir; yıldız yemektir (Karar 5).

---

## 3. Core Gameplay Loop

### 30 saniyelik loop
`Hareket et → consumable/düşman gör → tier kontrol → ye veya kaç → smooth büyü → hazard'dan sakın`
Anlık tatmin: squash animasyonu, ses, skor pop-up, haptic.

### 2–4 dakikalık loop
`Mass biriktir → tier atla → level-up → 3 karttan 1 seç → daha büyük şeyleri (ve artık DÜŞMANLARI) yiyebil → miniboss → yut → sonraki tier`

### Run loop (12–15 dk)
`Karakter seç → tier 1→5 yolculuğu → dalga baskısı artar → 4/8. dk miniboss → 12. dk final boss → boss'u YE ya da öl → coin → Market`

### Meta loop
`Coin → Market kredisi → kalıcı stat + karakter unlock → sonraki run daha çeşitli → "bir run daha"`

### Oturum yapısı
1. Lobide karakter seç (tek harita lansmanda otomatik).
2. Alanda başla — consumable'lar spawn olur, yiyerek büyürsün. Büyük objeler **tehlikelidir** (hazard).
3. Level atladıkça 3 kart → 1 seçim. Aynı skill tekrar seçilirse seviye atlar (1–8).
4. Düşman dalgaları başlar (60. sn) ve zamanla yoğunlaşır.
5. Düşman öldür/yut → coin düşer; **elit** düşman → **sandık** (skill + yüksek coin).
6. 4. ve 8. dakikada miniboss (aynı tasarım, artan stat). 12. dakikada **final boss**.
7. Final boss'un son fazında yeterince büyüdüysen onu **yutarsın** — run'ın zirve anı.
8. Run biter → coin Market kredisine dönüşür (ölümde %50) → Market → tekrar başla.

**Fail döngüsü:** Ölüm oturumu bitirir; kredinin %50'si korunur. "Sıfır kayıp yoktur — denemek her zaman değer." Bu oran korunacak çekirdek değerdir (kurulun "death is progress" bulgusu).

---

## 4. Büyüme, Tier & Yeme (Birincil Sistem)

### Büyüme formülü
```
scale = baseScale × (1 + mass × growthFactor) ^ growthExponent
hız   = 1 / √tier
```
Varsayılanlar: `baseScale=0.5, growthFactor=0.5, growthExponent=0.4`

### Tier eşikleri
| Tier | Mass | Yiyebildiği |
|---|---|---|
| 1 Tiny | 0 | Tier-1 consumable |
| 2 Small | 10 | Tier-2'ye kadar |
| 3 Medium | 30 | Tier-3'e kadar + **sürü düşmanı yutabilir** |
| 4 Large | 60 | Tier-4'e kadar + sürü düşmanı kolay yutar |
| 5 Giant | 100 | Her şey + **elit yutabilir**; final boss'un son fazı |

### Yeme birincildir (Karar 5)
- **Tier 3'ten itibaren** blob, sürü düşmanlarını temasla yutabilir (silahla öldürmeye gerek kalmaz). Yutulan düşman: mass + coin + XP verir.
- **Tier 5'te** elit düşmanlar yutulabilir.
- **Silahların rolü:** henüz yutamadığın düşmanı yavaşlatmak/öldürmek ve boss'u "yenebilir faza" getirmek. Silahlar DPS yarışının değil, erişim ekonomisinin aracıdır.
- **Final boss'un ölümü = yutulması.** Vurarak son faza getirirsin, yutarak bitirirsin.

### Hazard kuralı (Karar 6 — tam commit)
- **Senden büyük tier'daki her obje ve düşman temas hasarı verir.** Hasar `HazardAmount` alanından okunur (hardcoded `MassValue × 0.5` kaldırılır).
- Sonuç: erken oyunda harita bir mayın tarlasıdır; büyüdükçe aynı harita güvenli bir büfeye dönüşür. **Büyüme, hissedilir güçtür.**
- Okunabilirlik: hazard objeleri sana zarar verebilecekken hafif kızıl outline alır (renk körü desteği: outline + ikon).

### XP & Level
Mass kazanmak = XP kazanmak (tek sistem, ayrı XP kaynağı yok — Karar 7'nin temeli).
`xpThreshold = 20 + level × 15` → eşik geçilince level-up → 3 kart.

---

## 5. Skill & Upgrade Sistemi

### Kurallar
- Level-up'ta oyun durur, **3 kart** sunulur, 1 seçilir. Aynı skill 8. seviyeye kadar gelişir.
- **Yeniden Çek:** oturum başına 1 ücretsiz; sonrası **rewarded reklam** (mobil ana monetizasyon yüzeyi) veya 50 coin.
- Kartlar renk + sembol taşır (renk körü desteği: yıldız/elmas/daire).
- Max seviyeye ulaşan skill havuzdan çıkar.

### Lansman skill havuzu (11 skill)
| Kategori | Skill | Etki (seviye başına) |
|---|---|---|
| Saldırı | **Silah Gücü** | Karakter silahının hasar/atış hızı ↑ |
| Savunma | **Rejenerasyon** | +0.5 HP/sn |
| Savunma | **Zırh** | Alınan hasar ↓ (Kalkan iptal — aynı iş) |
| Pasif | **Maksimum Can** | Base 100, +10 |
| Hareket | **Hız** | Kalıcı hareket hızı ↑ |
| Hareket | **Hızlanma (Dash)** | Kısa hız patlaması; seviye → cooldown ↓, süre ↑ |
| Destek | **Attract** | Çekim yarıçapı + çekim hızı ↑ (Magnet+Vakum birleşti — Karar 9) |
| 🆕 Yeme | **Sindirim** | Yediklerinden mass kazancı +% |
| 🆕 Yeme | **Yutuş** | Her yeme küçük miktar HP yeniler |
| 🆕 Yeme | **Yırtıcı Çene** | Düşman yutma tier eşiği kolaylaşır (yutma menzili/boyut toleransı ↑) |
| ⏸️ | Score Multiplier | **Açık karar** — kart mı, meta upgrade mi (bkz. §16) |

> 🆕 Yeme skilleri Karar 5'in doğrudan sonucudur: farklılaştırıcı fiil, skill havuzunda da yıldız olmalı. Üçü de mevcut sistemlerin (mass kazancı, BlobHealth, yutma eşiği) parametreleridir — yeni sistem gerektirmez.

### Evrim (lansmanda 2 adet)
İki skill max seviyeye ulaşınca birleşir. Lansman önerileri (playtest'te doğrulanacak):
- **Attract (max) + Sindirim (max) → Kara Delik:** çekilen tier-1/2 consumable'lar temassız, otomatik yutulur.
- **Yırtıcı Çene (max) + Silah Gücü (max) → Avcı Formu:** silahla vurulan sürü düşmanları kısa süreliğine "yutulabilir" işaretlenir (tier şartı olmadan).

Evrim havuzu post-launch büyür (bkz. §14).

---

## 6. Karakterler & Silahlar

Her karakter top formundadır, elinde silahı vardır. Silahlar otomatik ateşler (saldırı tuşu yok).

| Karakter | Pasif | Silah | Unlock |
|---|---|---|---|
| **Topik** | +%20 hareket hızı | Top | Baştan açık |
| **Mıknato** | **Attract hattına +%25 etki** (Karar 9 — pasif, skill hattını güçlendirir) | Metal bilye | Market'te 500 kredi |
| **Mermo** | **Mermileri büyük consumable'ları yenebilir parçalara ayırır** — parçalar mevcut alt-tier havuzundan saçılır (özel fragman asset'i YOK; Karar 10 revizesi) | Pistol | **Toplam 3 oturum tamamla** (eski "3 farklı harita" koşulu tek-harita lansmanıyla güncellendi) |

- Karakter pasifleri run başında otomatik uygulanır.
- Karakter başı 5 kademeli meta ağaçlar **post-launch** (Karar 11). Lansmanda meta güçlendirme, karakterden bağımsız düz statlardır (bkz. §8).
- Mermo'nun pasifi "büyük şeylere erken erişim" fantezisidir: silah, yiyemediğini yenebilir hale getirir — yeme-birincil kimliğe silah üzerinden hizmet eden tek karakter. Uygulama: `PistolProjectile` → `ConsumableSpawner.ConsumeAndSplit(target, 3)`; büyük obje silinir, yerine bir alt tier'ın mevcut consumable'larından 3 adet saçılır. Yeni asset üretimi gerektirmez — içerik büyüdükçe maliyeti sabittir.

---

## 7. Düşman Sistemi

### Tipler (lansman)
| Tip | Davranış | Yutulma | Ödül |
|---|---|---|---|
| **Normal polis** (sürü) | Kalabalık koşar, basit steering | Tier 3+ | Coin (düşük) |
| **Elit polis** | Yavaş, güçlü, özel saldırı deseni; NavMesh | Tier 5 | **Sandık** (skill + yüksek coin) |
| **Miniboss** | 4. ve 8. dk; aynı tasarım, artan stat; alan saldırısı | Hayır (silahla) | Yüksek coin + garanti sandık |
| **Final Boss** | 12. dk; tek faz geçişi; son fazda **yutulur** | Son fazda | Run tamamlama ödülü |

### Ölçekleme eğrisi (12–15 dk'ya sıkıştırılmış — Karar 1)
| Dakika | Baskı |
|---|---|
| 0–1 | Sadece consumable; hazard öğrenimi |
| 1–3 | Temel sürü, düşük sağlık |
| 3–5 | Elit eklenir; hasar +%20; **4. dk miniboss** |
| 5–8 | Yoğunluk ×2; **8. dk miniboss** (güçlenmiş) |
| 8–12 | Çoklu elit + hız bonusu; hasar ×2 |
| 12+ | **Final boss** + kıyamet yoğunluğu; run 15. dk'da ölümle ya da boss'un yutulmasıyla biter |

### Ödül kuralı (çekirdek döngü — asla ödülsüz ölüm yok)
Her düşman ölümü/yutulması **mutlaka** coin düşürür ve `OnEnemyDied` event'i ateşler. `ScoreValue` alanı skora işler. (Kurulun "core loop kopuk" bulgusunun kapanışı — Sprint 2 A7 + orb drop.)

### Mimari (Karar 2)
- **Hedef: 150–200 eşzamanlı düşman, mobilde 30 FPS.**
- Sürü düşmanları: basit steering (blob'a yönel + komşudan ayrıl, spatial hash). **NavMeshAgent kullanmaz.**
- Elit + boss: NavMeshAgent (aynı anda ≤8–10 agent).
- Sprint öncesi 1 haftalık steering spike zorunlu iş olarak roadmap'tedir.

---

## 8. Ekonomi & Meta İlerleme (Karar 7 — 2 sistem)

### Run içi: COIN (tek para)
- Düşman ölümünden/yutulmasından düşer; Attract ile toplanır.
- Run içinde harcama: **Yeniden Çek** (50 coin, ücretsiz hak bittiyse).
- Run sonunda: kalan coin **Market kredisine** dönüşür — tamamlanan run'da %100, ölümde **%50**.

### Gösterge: SKOR
- Sadece leaderboard/highscore göstergesi. Hiçbir şey satın almaz. HUD'da yaşar, ekonomiye karışmaz.

### Kaldırılanlar
- ~~Ayrı XP puanı~~ (mass=XP), ~~ayrı re-roll altını~~ (coin öder), ~~ayrı kredi kazanım sistemi~~ (coin dönüşür).
- ~~"Başlangıç Silahı Kilidi" Market kalemi~~ — karakterler zaten silah tanımlar; kalem hem gereksiz hem karakter kimliğini sulandırıyordu.

### Market (lansman kalemleri)
| Kalem | Maliyet | Etki |
|---|---|---|
| Mıknato (karakter) | 500 | Kalıcı unlock |
| Kalıcı stat: +%5 hız | 200 → artan | Düz, karakterden bağımsız |
| Kalıcı stat: +10 max HP | 200 → artan | Düz |
| Kalıcı stat: +%5 mass kazancı | 300 → artan | Düz |
| Kalıcı stat: +%5 coin kazancı | 300 → artan | Düz |
| XP Çarpanı +%10 | 1000 | Tüm oturumlar |

4–6 düz stat + 1 karakter: lansman meta'sı budur. Karakter ağaçları, harita satın alma (tek harita), Grimoire post-launch.

---

## 9. Harita (Karar 12 — lansmanda 1)

**Modern Şehir** — tam işlenmiş tek harita:
- Sonsuz kaydırmalı (Vampire Survivors modeli).
- Kendi consumable seti (çöp → eşya → araç → yapı ölçeğinde), düşman havuzu, gotik-şehir paleti.
- Toplanabilirler: Coin, Kalp (can), Altın Kasa (bonus güçlenme).
- 2–3 easter egg noktası (mağara/garaj tipi yapılar) — Grimoire tracking'i şimdiden loglar (UI'sız).

**Medieval** = lansman sonrası ilk büyük içerik güncellemesi (canlı oyun anlatısının ilk başlığı).

---

## 10. Sanat & Ses (Karar 4)

### Sanat: 2.5D stilize 3D
- Unity URP, top-down 2.5D sahne; **16-bit pixel art vizyonu iptal** — büyüme fantezisi gerçek ölçekle anlatılır (blob'un dünyaya oranı her an okunur).
- Ton: komik-gotik, absürt. Blob sevimli ve **karizmatik** olmalı (gözler/yüz animasyonu cila fazında) — pazarlama kliplerinin taşıyıcısı blob'un kendisidir.
- Gotik palet korunur:

| Kullanım | Renk | Hex |
|---|---|---|
| Oyuncu | Kızıl | `#8B0000` |
| UI | Kemik beyazı | `#F5F0E0` |
| Arka plan | Gece mavisi | `#0D0D2B` |
| Sürü düşman | Bataklık yeşili | `#3D5C3A` |
| Elit düşman | Mor gecesi | `#4A0E6B` |
| XP/coin | Kehribar | `#FFC300` |

### Ses
- Synthwave + gotik orkestra; run'ın son 3 dakikasında tempo yükselir.
- **Unity Audio + mixer yeterli** — FMOD/WWise iptal (kurul: "aspiration, not need").
- Kritik SFX önceliği: yeme sesi (en sık duyulan ses — en çok itina), yutma (büyük), level-up jingle, boss girişi.

---

## 11. Kontrol, UI & Erişilebilirlik

| Platform | Hareket | Duraklat | Kart seçimi |
|---|---|---|---|
| Mobil (birincil) | Sanal joystick (dinamik, sol yarı) | Üst menü butonu | Dokunuş |
| PC (port) | WASD/ok | ESC | Fare/1-2-3 |

**HUD:** Sol üst can barı · orta üst timer + dalga · sağ üst aktif skill rozetleri + seviye · alt orta XP barı + level · coin sayacı · portrait, tek elle erişilebilir, safe-area uyumlu.

**Erişilebilirlik (değişmez):** tek el · renk + sembol (renk körü) · hazard outline + ikon · oturum kaydı.

---

## 12. Monetizasyon (Mobil F2P — sıfır pay-to-win)

**Değişmez kural: para hiçbir mekanik avantaj satın alamaz.**

| Yüzey | Ne | Not |
|---|---|---|
| **Rewarded reklam** | Yeniden Çek hakkı · run sonu bonus kredi | Ana erken gelir; oyuncu isteğiyle, asla zorunlu |
| **Kozmetik IAP** | Blob renk paletleri, trail efektleri (lansmanda küçük set: 4–6 kalem) | Genişleme post-launch |
| **PC premium (port)** | $4.99, reklamsız, tüm kozmetik dahil | Post-launch |

LTV beklentisi mütevazı tutulur; self-publish modelinde (Karar 13) erken gelir hedefi karlılık değil, **soft launch verisini fonlamaktır**.

---

## 13. Go-to-Market: Self-Publish + Organik (Karar 13)

UA bütçesi yok; görünürlük **kazanılacak**. Plan:

1. **Ana pazarlama varlığı:** size-flip klibi (_minik blob polisten kaçar → dev blob polisi yutar_). 10 saniyelik, sessiz anlaşılır, remix'lenebilir. Tüm kanal içeriği bu asset şablonundan türetilir (farklı objeler/düşmanlarla varyasyon).
2. **Kanal:** TikTok/Reels/Shorts — Hole.io'nun kanıtladığı format. Geliştirme sürecinin kendisi içerik (devlog klipleri).
3. **Soft launch:** küçük test pazarı (öneri: TR + 1 SEA ülkesi), Google Play açık beta.
   **Metrik kapıları:** D1 ≥ %35 · D7 ≥ %12 · oturum/DAU ≥ 3 run · median run süresi 10+ dk. Kapılar geçilmeden global lansman yok.
4. **Analytics + kayıt sistemi ilk günden** (mobil-önce kararının zorunlu bedeli): JSON tabanlı save + GameAnalytics/Unity Analytics ücretsiz katman. Ölçülenler: run sayısı, ölüm dakikası/nedeni, kart seçim oranları, D1/D7.
5. **Publisher opsiyonu saklı:** soft launch verisi güçlü çıkarsa publisher pitch'i (veri + trailer + deck) her zaman masadadır; self-publish bunu dışlamaz.

---

## 14. Yol Haritası

### Lansman kapsamı (MVP tanımı)
**MVP = oynanabilir + bir kez bitirilebilir + tekrar oynanabilir.**
1 harita · 3 karakter (1 açık + 2 unlock) · 11 skill + 2 evrim · 3 düşman tipi + miniboss + final boss · hazard aktif · coin ekonomisi + 6 kalemlik Market · yeme juice'u (squash, ses, haptic) · analytics + save.

### Fazlar
| Faz | Süre | İçerik | Kapı |
|---|---|---|---|
| **0 — Temel onarım** | 1–2 hafta | Aşağıdaki Faz 0 paketi | Düşman öldürmek ödül veriyor |
| **Prototip — "Eğlenceli mi?"** | 4–6 hafta | Yeme-birincil + hazard + yutma; greybox; 6 skill | **5 yabancıdan ≥3'ü kendiliğinden yeni run başlatıyor** — geçilmeden ileri gidilmez |
| **MVP / dikey dilim** | 2–3 ay | Tam lansman kapsamı, gerçek art kritik yolda, save+analytics | İç playtest: median ≥3 run/oturum |
| **Soft launch** | 1–2 ay | Test pazarı, metrik kapıları (§13), denge iterasyonu | D1 ≥ %35, D7 ≥ %12 |
| **Global lansman** | — | iOS + Android, kozmetik set, size-flip kampanyası | — |
| **Post-launch** | sürekli | Aşağıdaki içerik yol haritası | — |

### Faz 0 — Onarım Paketi (somut işler, 10 Tem 2026 kod incelemesinden)
1. **Ödül döngüsü:** `EnemyBase.Die()` → coin drop + `GameEvents.RaiseEnemyDied` + `ScoreValue` işlenmesi ("asla ödülsüz ölüm yok" kuralının kod karşılığı)
2. **Pool iadeleri:** düşman + consumable `Return()` çağrıları, `IPoolable` state-reset hook'u, NavMesh spawn'da `agent.Warp()`
3. **Hazard dalının canlandırılması:** `BlobConsumption`'daki ölü dal → `TakeDamage(HazardAmount)`; hardcoded `MassValue × 0.5` silinir
4. **UpgradeSystem düzeltmeleri:** blob bulunamayınca oyunun `timeScale=0`'da kilitli kalması (soft-lock) + `CurrentLevel`'ın paylaşılan SO'dan runtime wrapper'a taşınması
5. **Pull sonrası bulunan buglar:** `LobbyPanel`'de listener'ın bounds check'ten önce eklenmesi (fazla buton → `IndexOutOfRange`); restart akışında `ApplyCharacter`'ın silahı yeniden Instantiate edip pasifleri stack'lemesi
6. **Steering spike:** 150–200 sürü düşmanı için steering mimarisi doğrulaması (1 hafta, Karar 2'nin ön şartı)

### Post-Launch İçerik Yol Haritası (Karar 11 — ertelenenler, silinmedi)
Her ertelenen özellik bir güncelleme manşetidir:
1. **Medieval haritası** (+ harita satın alma Market'e döner)
2. **Hava durumu sistemi** (Ay Tutulması, Kızıl Yağmur, Sis, Şimşek Fırtınası)
3. **Grimoire UI** (tracking verisi lansmandan beri birikiyor)
4. **NG+ zorlukları** (Kızıl Ay → Kan Krizi → Apokalips)
5. **Karakter meta ağaçları** (5 kademe/karakter)
6. **Yeni bosslar** (SWAT aracı, helikopter, drone)
7. **Evrim havuzu genişlemesi** + yeni skiller
8. **PC/Steam premium port** ($4.99 + "Kıyamet Genişlemesi" DLC yolu)

---

## 15. Performans Hedefleri (revize)

| Hedef | Değer |
|---|---|
| Mobil FPS | 30 @ 720p, **200 eşzamanlı düşman** (500 hedefi revize — Karar 2) |
| Batarya | 15 dk run'da ≤ %8 |
| Yükleme | < 3 sn |
| PC (port) | 60 FPS @ 1080p |

Mühendislik kuralları (throttle, `sqrMagnitude`, object pool zorunluluğu, LayerMask'li sorgular) `CLAUDE.md`'de yaşamaya devam eder.

---

## 16. Açık Kararlar

> Cevaplanınca silinmez; altına **✅ Karar:** notu düşülür.

- 🔲 **Score Multiplier:** run içi kart mı kalsın, Market meta upgrade'ine mi taşınsın? (Kurul metayı öneriyor; karar bilinçli olarak ertelendi.)
- 🔲 **Nihai oyun adı:** Blob.io çalışma adı — store'da "io" eki algı riski taşır; global lansmandan önce kesinleşmeli.
- 🔲 **Soft launch pazarları:** TR + hangi ikinci pazar?

---

## 17. Korunacaklar (asla taviz verilmez)

1. **Büyüme–hız takası** (`1/√tier`) — playtester'lar yavaşlıktan şikâyet edecek; ayarlanır, kaldırılmaz.
2. **Yeme feedback'inin kalitesi** — squash/ses/haptic; buradaki her kesinti üründeki en pahalı hasardır.
3. **Tek input** — ikinci tuş isteyen her fikir varsayılan olarak reddedilir.
4. **Ölümde %50 kredi** — "ölüm = ilerleme."
5. **Sıfır pay-to-win.**
6. **Frame rate > içerik** — kaosun zirvesinde takılan bullet-heaven, silinen bullet-heaven'dır.

---

## Ek A — GDD v1.0 → v2.0 Değişiklik Özeti

Neyin neden değiştiğinin hızlı referansı. Gerekçelerin tamamı `ADVISORY_BOARD.md` ve `BOARD_EVALUATION.md`'de.

| Konu | v1.0 (Haziran 2025) | v2.0 (Temmuz 2026) |
|---|---|---|
| **Run süresi** | 20–30 dk (GDD.md'de ayrıca 5–10 dk — çelişkili) | **12–15 dk** — tek cevap |
| **Düşman ölçeği** | 500 eşzamanlı @30fps | **150–200**; sürü=steering, NavMesh sadece elit/boss |
| **Platform** | Mobil-first deniyor ama takvim Steam EA'yı önce koyuyordu — çelişkili | **Mobil F2P önce**; PC/Steam premium = post-launch port |
| **Motor** | Godot 4.x yazıyordu (proje Unity'de) | **Unity 6 / URP** — doküman gerçeğe uyduruldu |
| **Sanat** | 16-bit pixel art, 32×32 sprite, parallax | **2.5D stilize 3D**; gotik palet korundu |
| **Ana fiil** | Örtük olarak silah (skill listesi jenerik VS) | **Yeme birincil, silah destek**: Tier 3+ düşman yutma, Tier 5 elit yutma, final boss yutularak ölür, 3 yeni yeme skilli |
| **Hazard** | Yarım tanımlı (alan var, kural yok) | **Tam commit**: büyük şey zarar verir, `HazardAmount` gerçek, kızıl outline |
| **Ekonomi** | 6 sayısal sistem (mass, XP, skor, coin, kredi, re-roll altını) | **2 sistem**: Coin + Skor; re-roll coin'le/reklamla |
| **Boss kadrosu** | SWAT + helikopter + drone + fazlı Kıyamet Bossu (4+ özgün) | **1 miniboss (4/8. dk, artan stat) + 1 final boss (12. dk)**; kalanlar post-launch |
| **Boss dakikaları** | 5/10/15/20/25+ | **4/8/12** (sıkıştırılmış eğri) |
| **Çekme mekanikleri** | Mıknatıs + Vakum ayrı skiller + Mıknato pasifi (3 örtüşen sistem) | **Tek "Attract" hattı**; Mıknato pasifi onu güçlendirir |
| **Mermo pasifi** | Mermiyle parçalama (fragman maliyeti belirsiz) | **Korundu ve netleşti**: `ConsumeAndSplit` alt-tier havuzundan saçar, sıfır asset vergisi (10 Tem revizesi) |
| **Mermo unlock** | 3 farklı haritada oturum tamamla | **Toplam 3 oturum tamamla** (lansmanda 1 harita var) |
| **Kalkan skilli** | Listede | **İptal** (Zırh ile aynı iş — Sprint 1 kararı) |
| **Skill havuzu** | 8 jenerik skill | **11 skill**: +Sindirim, +Yutuş, +Yırtıcı Çene (yeme-temalı); Score Multiplier askıda |
| **Evrim** | Bahsedilmiş, tanımsız (GDD'de boş satır) | **Lansmanda 2 tanımlı evrim** (Kara Delik, Avcı Formu — playtest'te doğrulanacak) |
| **Haritalar** | Modern Şehir + Medieval (+ Park/Liman/Endüstri) | **Lansmanda 1** (Modern Şehir); Medieval ilk içerik güncellemesi |
| **Hava durumu** | 4 efekt lansman özelliği | **Post-launch** |
| **NG+ (4 zorluk)** | Lansman özelliği | **Post-launch** |
| **Grimoire** | Lansman özelliği | **UI post-launch**; tracking hook'ları lansmanda kodda |
| **Karakter meta ağaçları** | 5 kademe/karakter (tabloda var olmayan karakterler: Mira/Dorian/Lyra/Bastian) | **Post-launch**; lansmanda 4–6 düz kalıcı stat |
| **Market kalemleri** | 5 kategori (karakter, pasif, harita, silah kilidi, XP çarpanı) | **Sadeleşti**: "Başlangıç Silahı Kilidi" silindi (karakterler zaten silah tanımlar), harita satın alma post-launch'a |
| **Ses motoru** | FMOD / WWise | **Unity Audio + mixer** |
| **Go-to-market** | Yok | **Yeni bölüm (§13)**: self-publish + organik, size-flip klibi, soft launch metrik kapıları (D1 ≥ %35, D7 ≥ %12) |
| **Analytics & save** | Bahsedilmemiş | **Zorunlu, ilk günden**: JSON save + analytics (mobil-önce kararının bedeli) |
| **Performans/batarya** | 30 dk'da ≤%15 | **15 dk run'da ≤%8**, 200 düşman @30fps |
| **Proje takvimi** | Pre-Alpha→1.0, 18 ay, Steam odaklı | **Kapılı fazlar** (§14): her faz metrik kapısıyla geçilir; prototip "eğlence kapısı" en öne alındı |
| **Doküman disiplini** | 3 doküman, 3 farklı cevap | **Tek kaynak kural**: çelişkide GDD v2 kazanır; Karar Günlüğü (§0) anayasa |

---

_GDD v2.0 — Temmuz 2026 · GDD v1.0 (PDF) ve GDD.md'yi geçersiz kılar · Karar oturumu: ADVISORY_BOARD.md + BOARD_EVALUATION.md üzerinden 13 karar_
