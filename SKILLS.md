# Left No Crumbs: Skill, Karakter ve Boss Rehberi

> Skill, karakter, düşman ve boss'ların **güncel** değerleri. Değerler koddan ve asset'lerden okunur (`Assets/_Project/Data/Upgrades/`, `Data/Characters/`, `ScriptableObjects/Enemies/`); değişince burası da güncellenir, eski hali yazılmaz.
> Skill değeri değiştirmek için ilgili `Upgrade_*.asset` dosyasında `Max Level` / `Per Level Value` alanlarını Inspector'dan düzenle.
> Oyunun genel hali `OYUN.md`'de; açık denge soruları GitHub issue'larında.

## 1. Genel kurallar

**Seviye atlama.** Yediğin her şeyin mass'ı aynı zamanda XP'dir. Bir sonraki level için gereken XP `20 + level × 15 + level² × 2 + max(0, level − 4)² × 12`: 1. level 20 XP, 2. level 37, 3. level 58, … 5. level 112, 8. level 331, 10. level 617, 11. level 802 (Giant bina ~17 XP, Dev bina ~28 XP). Karesel terimler geç oyunda (bina = 25–50 XP) seviye atlamanın çok hızlanmasını önler; son terim ilk 5 seviyeyi değiştirmez.

**Kart ekranı.** Level atlayınca oyun durur ve 3 kart gelir. Biri seçilir.
- Kartlar ağırlıklı rastgele çekilir: çoğu skill ağırlık **10**, Can Emme **8**, Can Artışı **4**. Ağırlığı düşük olan daha seyrek gelir.
- **Karakterin favori skill'i (pasifi) ×3 ağırlıkla** çekilir (ağırlığı 10 olan skill kart ekranlarının ~%41'i yerine ~%75'inde çıkar; Cano'nun Can Emme'si 8 ağırlıklı: ~%34 yerine ~%67): Topik → Hız, Mıknato → Vakum, Fişek → Havai Fişek, Dalcık → Sopa, Cano → Can Emme. `CharacterData._favoredUpgrade` / `_favoredWeightMultiplier`.
- Max seviyeye ulaşan skill havuzdan çıkar, bir daha gelmez.
- Havuzda 3'ten az skill kalırsa daha az kart gelir. Hepsi maxlıysa level-up ödülsüz geçer. Bunun için 63 seçim gerekir, pratikte ulaşılamaz.
- Kartın sağ üstünde **YENİ** (yeşil, ilk kez alıyorsun) ya da **mavi Sv. 2/8** (sahip olduğun skill'in şu anki seviyesi) yazar; altındaki açıklama bu seçimle gelecek seviyeyi anlatır.
- Tek lokmada birden fazla level atlarsan kart ekranları arka arkaya gelir, seviye kaybolmaz.
- Elit sandığı da bir kart ekranı açar.
- **Garantili kart:** `UpgradeData._guaranteedAtLevel` > 0 olan skill, oyuncu o seviyeye gelince (henüz almadıysa) ilk kart ekranında bir kez kesin çıkar. Şu an sadece Havai Fişek (Sv.4).
- **Başlangıç skill'i:** `CharacterData._startingUpgrade` + seviye: run o skill o seviyede başlar (Fişek: Havai Fişek Sv.1, Dalcık: Sopa Sv.2, Cano: Can Emme Sv.2).

**Run sonu.** Skiller run'a özeldir. Ölüm ya da restart hepsini sıfırlar.

**Kalıcı bonuslarla etkileşim.** Market'ten alınan kalıcı statlar her run başında uygulanır, skiller bunların üstüne eklenir: Hız en fazla +%25, Max Can +50, Kütle kazancı +%25 (XP'yi de artırır), Coin kazancı +%25 (her biri 5 seviye), XP çarpanı +%25 (tek sefer). Fiyatlar ve denge hedefleri `OYUN.md` §9.

**Blob boyutu referansı** (örneklerde kullanılıyor, sahne değerleri):

| Tier | Gereken mass | Görsel ölçek | Gövde yarıçapı |
|---|---|---|---|
| Tiny | 0 | 0.5 | 0.25 |
| Little | 15 | ~0.76 | ~0.38 |
| Small | 50 | ~1.1 | ~0.55 |
| Medium | 200 | ~2.0 | ~1.0 |
| Large | 650 | ~3.3 | ~1.65 |
| Giant | 1100 | ~4.1 | ~2.05 |
| Dev (`Colossal`) | 1650 | ~5.0 | ~2.5 |

---

## 2. Skiller (8 adet)

### 🛡️ Zırh (kalkan)
- **Ne yapar:** Blob'un yanında bir **kalkan** belirir. Gelen her hasarı (polis vuruşu, elit/tank/helikopter mermisi, araba çarpması, hazard, polis ısırığı) **önce kalkan yer**; kalkan bitince can azalır. Kalkanın yuttuğu hasarda kırmızı parlama ve kamera sarsıntısı olmaz, kalkan parlar.
- **Yenilenme:** Son hasardan **4 sn** sonra (Sv.5'ten itibaren **3 sn**) kalkan **2 sn içinde** tamamen dolar. Her yeni hasar sayacı baştan başlatır.
- **Seviye atlayınca:** Kalkan büyür ve **hemen dolar**.
- **Seviye başına:** +10 kalkan · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `ArmorEffect` (asset `FX_DamageReduction`) → havuz `BlobHealth.SetShield`, kalkan modeli + dikenler `ArmorShield`

| Seviye | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Kalkan | 20 | 30 | 40 | 50 | 60 | 70 | 80 | **100** |
| Yeni | — | — | ⭐ **Dikenler** sn'de 6 | — | ⭐ Yenilenme 3 sn | ⭐ Dikenler sn'de **10** | — | ⭐ **Son form:** kırılınca şok dalgası |

- **Dikenler (Sv.3+):** Kalkan sağlamken blob'a değen her düşman 0.5 sn'de bir hasar alır (Sv.3: sn'de 6, Sv.6: sn'de 10); kızarır ve 0.8 birim dışa seker (şok dalgasında 1.6). Seni kovalayıp yakın dövüşe giren polis kendini yorar. Kalkan kırıkken dikenler çalışmaz.
- **Şok dalgası (Sv.8):** Kalkan kırıldığı an blob'un 2 birim dışına kadar tüm düşmanlara **25 hasar**.
- **HUD:** Can barının hemen altında ince mavi kalkan barı (Zırh alınana kadar gizli).
- **Görsel:** Blob'un sağ yanında süzülen, yüzü dışa bakan, gidiş yönüyle dönen **sokak eşyası kalkan**. İstenirse karakter başına kafada şapka gibi durabilir (`CharacterData._armorOnHead`: tepede yatay, %80 boy; kırılınca fırıldak gibi döner); şu an hiçbir karakterde açık değil (Fişek'in tepesinde fitil var). Form seviyeyle, yeni mekanikle aynı anda değişir: **Sv.1-2** çöp kutusu kapağı → **Sv.3-5** dikenli "girilmez" tabelası (dikenler) → **Sv.6-7** dikenli menhol kapağı (güçlü dikenler) → **Sv.8** altın menhol kapağı + kızıl mücevher (şok dalgası). Kalkan azaldıkça koyulaşır, vuruşta parlar ve şişer, kırılınca dönerek küçülüp kaybolur, dolmaya başlayınca geri "pop" eder; yeni form da pop ile gelir. Modeller `zorse-blender/build_armor_shields.py` → `Resources/FX/Shield` (148–220 üçgen).

**Örnek:** Sv.3, kalkan 40. Bir polis seni 10'ar vuruyor: ilk 4 vuruşu kalkan yer, canın hiç azalmaz. Bu sürede polis sana değdiği için saniyede 6 hasar alır (50 canlı polis ~8 sn'de düşer). 4 sn kaçarsan kalkan 2 sn'de geri dolar.
**Örnek:** Helikopterin 36 hasarlık serisini Sv.4'teki 50'lik kalkan tamamen yer.

---

### ❤️ Can Artışı
- **Ne yapar:** Max canı artırır. Eklenen miktar kadar da anında iyileştirir.
- **Seviye başına:** +10 max can · **Max:** 8 · **Ağırlık:** 4 (seyrek gelir)
- **Kod:** `HealthBoostEffect` → `BlobHealth.IncreaseMaxHealth`

| Seviye | 0 | 2 | 4 | 8 |
|---|---|---|---|---|
| Max can | 100 | 120 | 140 | **180** |

**Örnek:** 30/100 candayken alırsan 40/110 olursun.

---

### 💚 Can Emme
- **Ne yapar:** Düşmana vurduğun hasarın bir yüzdesi can olarak sana döner. Durduk yere can yenilemez; savaştıkça iyileşirsin. Sopa, Havai Fişek, Zırh dikenleri ve şok dalgası hepsi sayılır. Sadece gerçekten işleyen hasar sayılır (canı 1'e kilitli helikoptere boşa giden vuruş can vermez). Can doluyken birikmez.
- **Seviye başına:** vurulan hasarın %2'si · **Max:** 8 · **Ağırlık:** 8
- **Kod:** `RegenBoostEffect` (id `regen_boost`, eski Rejenerasyon) → `LifestealComponent` (`EnemyBase.OnDamageDealt` dinler, 0.2 sn'de bir toplu iyileştirir)

| Seviye | 1 | 4 | 8 |
|---|---|---|---|
| Can olarak dönen | %2 | %8 | **%16** |

**Örnek:** Sv.4, silahın saniyede toplam 40 hasar vuruyor (3 hedef, patlama): saniyede ~3.2 can. Ateş etmediğin sürece iyileşmezsin.

---|---|---|---|
| Can/sn | 0.5 | 2 | **4** |
| Dakikada | 30 | 120 | 240 |

**Örnek:** Seviye 4'te 50 hasar aldıysan yaklaşık 25 saniyede toparlarsın.

---

### 💨 Hız Artışı
- **Ne yapar:** Düz hız verir: hız çarpanını artırır. Büyüdükçe gelen yavaşlamaya dokunmaz.
- **Seviye başına:** +%15 hız · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `SpeedBoostEffect` → `BlobController.SetSpeedMultiplier`
- **Görsel (bütün karakterlerde aynı):** **Sv.1-3** kaykay → **Sv.4-7** alevli kaykay (tablada alev deseni, turuncu tekerlek) → **Sv.8** alevli kaykay + **popoda roket** (alevi hıza göre uzar, dururken söner gibi kısalır, titrer). Yeni parça "pop" ile gelir. Kaykay varken görünüm 0.13 kalkar (tekerlekler yere basar; collider ve yeme aynı). Modeller `zorse-blender/build_speed.py` → `Resources/FX/Speed` (kaykay ~210, roket 260, alev 92 üçgen); `SpeedVisual`. Topik'in pasif rüzgar izi bundan ayrı, baştan var.
- **Formül:** `hız = 5 × çarpan / sıra^üs` (sıra: Tiny 1 … Giant 5, Dev 6). Üs **0.24**, run boyunca sabit (sahnede `BlobController._tierSlowdownExponent`).

| Seviye | Çarpan | Tiny hızı | Giant hızı | Dev hızı |
|---|---|---|---|---|
| 0 | 1.0 | 5.0 | 3.4 | 3.25 |
| 4 | 1.6 | 8.0 | 5.4 | 5.2 |
| 8 | **2.2** | 11.0 | **7.5** | 7.2 |

**Örnek:** Topik run'a Hız Sv.2 ile başlar (çarpan 1.3): ilk saniyeden Tiny hızı 6.5, kaykay görünür.

---

### 🌀 Vakum
- **Ne yapar:** Çevrendeki consumable'ları kendine doğru çeker. Sadece yiyebileceğin tier'dakileri çeker.
  **Çekmedikleri:** coin, yaya, polis, şehir binaları, hazard'lar.
- **Temel menzil:** 1.2 · **Seviye başına:** +0.35 · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `VacuumEffect` → `VacuumComponent` (0.15 sn'de bir tarama, çekme hızı 6 birim/sn)
- **Menzil gövde yüzeyinden ölçülür:** blob büyüdükçe çekme alanı da büyür. Toplam yarıçap = gövde yarıçapı + menzil.
- **Görsel:** çekim alanı boyunca zeminde sürekli dönen hafif bir kasırga: iki katman açık mavi, kesik kesik spiral rüzgar çizgisi, içe doğru akar. Menzil arttıkça büyür. Doku/materyal: `Tools → Blob.io → Vakum Efektini Kur` (`Resources/FX/Vacuum`).

| Seviye | 1 | 2 | 4 | 8 |
|---|---|---|---|---|
| Menzil (gövde dışına) | 1.55 | 1.9 | 2.6 | **4.0** |
| Toplam yarıçap, Giant blob (gövde ~2) | 3.55 | 3.9 | 4.6 | 6.0 |

Mıknato run'a Sv.2 ile başlar; çarpanı yok, değerler herkesle aynı.

**Örnek:** Seviye 4'te, gövdenin 2.6 birim dışına kadar yiyebileceğin objeler sana akar. Blob Giant olunca alan da onunla büyür.

---

### 🪵 Sopa
- **Ne yapar:** Ana silahı (ağaç dalı sopa) güçlendirir. Her seviye "+hasar" değil: kilometre taşı seviyelerinde yeni bir yetenek açılır. Kart, o seviyede ne kazanacağını yazar.
- **Max:** 8 · **Ağırlık:** 10
- **Kod:** `WeaponUpgradeEffect` (kilometre taşı seviyeleri; +hasar `Upgrade_Weapon.asset` → Per Level Value 5) → `StickWeapon` (`Prefabs/Weapons/StickWeapon.prefab`, `GameManager._stickWeaponPrefab`: `_hitDamage`, `_range`, `_interval`, `_arc`, `_wideArc`, `_fastFactor`, `_fullCircleDamage`). Kart metinleri `Upgrade_Weapon.asset` → Level Descriptions.
- **Temel sopa (bütün karakterlerde aynı):** saniyede **1 vuruş × 10 hasar**, yüzün baktığı yöne **120°'lik yay**, menzildeki yaydaki **herkese** vurur (hafif geri itme, bina arkasındakine vurmaz). Menzil **gövde yüzeyinden 2** (+hasar seviyeleri +0.5; Sv.6'da 4). Kimse yoksa sallamaz. Kaçarken arkandakine vurmaz (Sv.3'e kadar). Uzun menzil ve boss işi Havai Fişek'in.
- **Görsel:** **ağaç dalı** (`G_Stick_1..4` ← `zorse-blender/build_stick.py`), **sadece vururken görünür**: blob'un ortasından gidiş yönüne çıkar, yay boyunca soldan sağa savrulur (`WeaponVisual.PlaySwing` / `HideWhenIdle`; Sv.5'te 180°, Sv.8'de tam tur; çift uçlu formların arka ucu arka yayı süpürür), arkasında beyaz hilal izi (`SwingArcFx`, materyal `Resources/FX/SpeedTrail`). Formlar: **Sv.0-2** yapraklı eğri dal → **Sv.3-6** çift uçlu topuzlu sopa, kırmızı bantlı → **Sv.7** topuzlarda çiviler → **Sv.8** altın bantlı, uçlarında altın yaprak ve pembe çiçek. Yeni form "pop" ile gelir.

| Sv. | Kazanılan | Vuruş hasarı | Sn. başı hasar (hedef başına) |
|---|---|---|---|
| 0 | – | 10 | 10 |
| 1 | +5 hasar, +0.5 menzil | 15 | 15 |
| 2 | +5 hasar, +0.5 menzil | 20 | 20 |
| 3 | ⭐ **Arkaya da vurur** (ikinci yay) | 20 | 20 |
| 4 | +5 hasar, +0.5 menzil | 25 | 25 |
| 5 | ⭐ **Yay 180°** | 25 | 25 |
| 6 | +5 hasar, +0.5 menzil | 30 | 30 |
| 7 | ⭐ **Vuruş hızı ×1.3** | 30 | 39 |
| 8 | ⭐ **Son form: tam tur (360°)**, hasar ×1.5 | 45 | 58 |

**Örnek:** Sv.5'te 4 polis önünden geliyor: tek vuruş (25) 180°'lik yaydaki dördüne de değer; 50 canlı polis 2 vuruşta düşer. Robot köpek (10 can) her seviyede tek vuruşta ölür.
**Not:** Tek bir elite (250 can) Sv.8'de saniyede ~58 hasar = ~4.5 sn.


---

### 🎆 Havai Fişek (ikinci silah)
- **Ne yapar:** Karakter silahının yanına ikinci, otomatik bir silah ekler. Uzun menzilli, yavaş, tek hedefe ağır roket. **Her yöne** (360°) ateş eder, nişan/kurma yok; hedef önceliği **boss > elit > en yakın**. Delmez, alan hasarı yok: sürüyü değil boss'u indirir. Herkesin kartı (karakter pasifi değil).
- **Garanti:** Oyuncu 4. seviyeye gelince (1. miniboss 5'te) henüz almadıysa o kart ekranında bir kez kesin çıkar (`UpgradeData._guaranteedAtLevel`). Sonra normal ağırlıkla.
- **Temel:** **2 sn'de 1 atış × 25 hasar** (12.5/sn), menzil **gövde yüzeyinden 8**. **Max:** 6 · **Ağırlık:** 10 · Seviye başına +8 hasar (kilometre taşı olmayan seviyelerde)
- **Kod:** `FireworkEffect` (değerler `FX_Firework.asset`) → `FireworkWeapon` (`WeaponBase` alt sınıfı; blob'un `FireworkWeapon` child'ı). Mermi `Prefabs/Weapons/FireworkRocket.prefab` (`FireworkProjectile`, model `X_Firework` ← `build_bullets.py`, Mermi Modellerini Uygula kurar). Efektler `FireworkBurstFx` (EatCrumbs havuzu).
- **Görünür atar:** **sol omuzda** (kural: sol = saldırı, sağ = kalkan). Alçak ve neredeyse yatay. Fişek'te run başından var. Gövdeye sabit, öne bakar (roket hedefe doğru çıkar). Sokak hurdası karton havai fişek tüpü; form yeni mekanikle değişir: **Sv.1-2** kırmızı-beyaz tek tüp + fitil → **Sv.3-4** kalın mavi-sarı tüp → **Sv.5** üstte trafik konisi nişangahı → **Sv.6** karton kutuda çift tüp, altın bant. İlk alınca "pop" eder. Modeller `zorse-blender/build_firework.py` → `Resources/FX/Weapons/G_Firework_1..4` (192–470 üçgen; önizleme en büyük kalkan + silahla çizilir).
- **Efektler:** Her roket sıradaki renkte (kırmızı, yeşil, mavi, sarı, pembe, camgöbeği): gövde ve iz o renk. Durduğu yerde (düşman, duvar ya da menzil sonunda havada) o renkte kıvılcım patlaması. Atışta atarın arkasından gri duman, blob karakter silahının ~2.6 katı geri teper, hafif titreşim.

| Sv. | Kazanılan | Hasar | Atış aralığı | Saniyede hasar |
|---|---|---|---|---|
| 1 | Havai Fişek açılır | 25 | 2 sn | 12.5 |
| 2 | +8 hasar | 33 | 2 sn | 16.5 |
| 3 | ⭐ Atış hızı ×1.3 | 33 | ~1.5 sn | 21.5 |
| 4 | +8 hasar | 41 | ~1.5 sn | 26.7 |
| 5 | ⭐ Boss ve elite ×1.5 hasar | 41 (boss'a 61.5) | ~1.5 sn | 26.7 (boss'a 40) |
| 6 | ⭐ **Son form:** her atış art arda 2 roket | 41 ×2 | ~1.5 sn | 53 (boss'a 80) |

**Örnek:** 1. tank (600 can) sopa (10/sn) + Havai Fişek Sv.1 (12.5/sn) ile ~27 sn'de düşer; sadece temel silahla ~60 sn. Havai Fişek Sv.6 boss'a ~80/sn.

---

### 🍖 Sindirim
- **Ne yapar:** Yediğin her şeyden kazandığın mass'ı, dolayısıyla XP'yi artırır. Daha hızlı büyür, daha hızlı level atlarsın.
- **Seviye başına:** +%5 · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `SindirimEffect` → `BlobGrowth._massGainMultiplier`

| Seviye | 2 | 4 | 8 |
|---|---|---|---|
| Kazanç | ×1.1 | ×1.2 | **×1.4** |

**Örnek:** 10 mass'lık bir obje Sindirim 8'de 14 mass verir. Medium'a (200 mass) ~143 mass'lık yemeyle ulaşırsın.

---

### 🚓 Polis yutma (skill değil, genel kural)
Robot köpek **Little**, polis **Medium**, elit polis **Giant** blob ister.

**Isırık bedeli:** Polis yutulurken ısırır: tier tam eşikteyse 5, 1 tier üstteyse 2.5, 2+ tier üstteyse 0 hasar (robot köpek 2 / 1 / 0). Isırık normal hasar gibi kırmızı parlama + kamera sarsıntısı + titreşim verir.

**Kaçmazlar:** Düşmanlar blob onları yiyebilecek boyda olsa da kaçmaz, saldırmaya devam eder.

---

## 3. Evrim (1 adet)

Evrim kartı, iki girdi skilinin ikisi de **max seviyeye** ulaşınca havuza girer. Kart ★ ve altın renkle işaretlenir. Max seviyesi 1'dir: bir kez seçilince havuzdan çıkar. Ağırlık 10.

### ★ Kara Delik
- **Gereken:** Vakum 8 + Sindirim 8 (toplam 16 seçim)
- **Ne yapar:** Blob'un çevresindeki, **yiyebildiğin tier'daki her şey** temas gerekmeden otomatik yutulur: consumable, yaya, şehir objesi. Hazard'ları yemez.
- **Yarıçap:** gövde yarıçapının 2 katı; blob büyüdükçe büyür. 0.1 sn'de bir tarar.
- **Kod:** `KaraDelikEffect` → `BlackHoleComponent`
- **Evrim anı** (kart seçilince ~1.8 sn): zaman yavaşlar (×0.25), açık mavi kasırga hızlanıp blob'a çöker → indigo şok dalgası, kamera sarsıntısı, güçlü titreşim, blob şişip yutkunur; kasırga siyah-lacivert olarak geri açılır, olay ufku taşarak belirir; zaman ve kamera normale döner. Ekranın üstünde altın **★ KARA DELİK** yazısı (her evrimde adıyla).
- **Kalıcı görünüm:** vakum kasırgası siyah + lacivert (normal vakumdan 1.5 kat çok çizgi, ayrı dokular), 1.6 kat hızlı döner; blob'un altında yutma yarıçapında siyah disk ve kenarında dönen indigo yığılma halkası (temassız yuttuğu alan).

| Blob | Gövde yarıçapı | Kara Delik yarıçapı |
|---|---|---|
| Medium | ~1.0 | ~2.0 |
| Giant | ~2.0 | ~4.0 |

**Örnek:** Giant blob'la bir sokakta ilerlerken, gövdenin 2 birim dışındaki her şey (Giant tier binalar dahil) dokunmadan yutulur. Vakum hâlâ çalışır ve daha uzaktakileri çekip Kara Delik'in içine getirir.
**Not:** Polisleri yutmaz; onlar için temas gerekir.

---

## 4. Karakterler (5 adet)

**Açılma koşulları** (Market → **KARAKTERLER** sekmesi; Topik orada yok, baştan açık):
- **Topik:** baştan açık.
- **Mıknato:** Market'te **500 coin** (2.–3. run).
- **Fişek:** Market'te **1.000 coin** (~5. run).
- **Cano:** Market'te **1.500 coin** (~7. run).
- **Dalcık:** satılmaz, **5 run bitirince** kendiliğinden açılır. Ölümle biten run da, kazanılan run da sayılır. Market'teki kartında ilerleme yazar ("3/5 run"). Sayı kalıcı kayıtta tutulur (`MetaProgression.CompletedRuns`), koşul `Char_Dalcik.asset` → `Required Completed Runs`.
- Kasaya run sonunda toplanan coin girer: kazanınca %100'ü, ölünce %50'si. Satın alınan karakter kalıcıdır (`CharacterData._saveId` kayda yazılır, `_marketPrice` > 0 = Market'te satılır).
- Kilitli karakter Lobby'de seçilemez; isminin altında koşul yazar: "Market'te açılır" ya da "2 run sonra açılır".

**Neden Dalcık run sayısıyla:** coin'le alınan üç karakter "biriktir ve seç" hedefi; Dalcık "oynamaya devam et" hedefi. Başarılı run şartı yeni oyuncu için uzak ve en çok yardıma ihtiyacı olanı (kazanamayanı) cezalandırır; 5 run her oyuncuya sayılabilir, görünür bir hedef verir ve Mıknato ile Fişek'in arasına düşer.

Beş karakter de aynı blob'dur; farkları **görünüm** ve **başlangıç avantajı** (kendi skill'iyle başlar; o skill favori skili, kart ekranında ×3 ağırlık). Ana silah hepsinde aynı: sopa (yukarıda).

| | 🟠 Topik | 🧲 Mıknato | 🎆 Fişek | 🌿 Dalcık | 💚 Cano |
|---|---|---|---|---|---|
| **Kilit** | Baştan açık | Market **500** | Market **1.000** | **5 run bitirince** | Market **1.500** |
| **Başlangıç avantajı** | **Hız Sv.2** | **Vakum Sv.2** | **Havai Fişek Sv.1** | **Sopa Sv.2** | **Can Emme Sv.2** |
| **Favori skill (×3)** | Hız | Vakum | Havai Fişek | Sopa | Can Emme |
| **Görünüm** | Turuncu → kırmızı | Mavi, tepede mıknatıs | Korsan bomba: siyah, göz bandı, tepede fitil | Fıstık yeşili, tepede minik dal | Ahududu kırmızısı, tepede kalp |
| **Oyun tarzı** | Baştan hızlı; kaçarak büyür | Toplayıcı; yemi kendine çeker | Uzaktan vurur, boss'u erken indirir | Yakın dövüşçü; sürünün içine dalar | Dayanıklı; savaştıkça iyileşir |

### 🟠 Topik
- **Pasif:** Run'a **Hız Artışı Sv.2** ile başlar (çarpan 1.3; kaykay baştan görünür). Kart ekranında sıradaki Sv.3 çıkar, Hız favori skili (×3 ağırlık). `Char_Topik.asset` → `Starting Upgrade` / `Starting Upgrade Level`. Rüzgar izi (`SpeedTrail`) sadece görsel.
- **Görünüm:** turuncu → kırmızı; hızlı giderken arkasında rüzgar izi.

### 🧲 Mıknato
- **Pasif:** Run'a **Vakum Sv.2** ile başlar (menzil gövde yüzeyinden 1.9; kart ekranında sıradaki Sv.3). Vakum favori skili (×3 ağırlık). `Char_Miknato.asset`.
- **Görünüm:** mavi, tepede kırmızı at nalı mıknatıs.
- **Örnek:** Diğer karakterler Vakum'u kart olarak beklerken Mıknato ilk saniyeden çevresini toplar. Erken büyüme ve erken level demek. Kara Delik evrimine (Vakum 8 + Sindirim 8) en doğal giden karakter.

### 🎆 Fişek
- **Pasif:** Run'a **Havai Fişek Sv.1** ile başlar: atar baştan sol omzunda, 2 sn'de bir 25 hasarlık roket (menzil gövde yüzeyinden 8). Havai Fişek favori skili (×3 ağırlık). `Char_Fisek.asset`.
- **Görünüm:** korsan bomba: siyah gövde (kömür → gece siyahı), sol gözde kırmızı göz bandı; bandı kafayı tek düz halka olarak çapraz sarar. Tepede metal ağızlık ve yanan fitil (`build_blobs.py`: `eye_patch`, `fuse_top`; model adları hâlâ `CH_Mermo_*`). Siyahta okunsun diye kaşları açık gri, ağzı koyu kırmızı. Kalkanı diğerleri gibi sağ yanda.

### 🌿 Dalcık
- **Pasif:** Run'a **Sopa Sv.2** ile başlar: sopa ilk saniyeden **20 hasar**, menzil gövde yüzeyinden **3** (diğerlerinde 10 hasar, 2 menzil). Kart ekranında sıradaki Sv.3 (⭐ arkaya da vurur) çıkar; Sopa favori skili (×3 ağırlık), kilometre taşlarına (Sv.5 180°, Sv.8 tam tur) en hızlı giden karakter. `Char_Dalcik.asset`.
- **Örnek:** Polis 50 can: diğerleri 5 vuruşta, Dalcık 3 vuruşta indirir. Elit (250 can) Sv.2'de ~13 sn.
- **Görünüm:** fıstık yeşili (çimden ayrılsın diye sarımsı), tepede minicik kısa dal: ucunda tek koyu yeşil yaprak, yanında kesik budak (sopadan esinlenme). `build_blobs.py` → `CH_Dalcik_T1..T5`, portre `Icon_Dalcik.png`.

### 💚 Cano
- **Pasif:** Run'a **Can Emme Sv.2** ile başlar: vurduğu hasarın **%4**'ü can olarak döner (sopa 10 hasar/sn → saniyede 0.4 can; Havai Fişek gelince artar). Can Emme favori skili (normal ağırlığı 8, Cano'da 24). `Char_Cano.asset`.
- **Örnek:** Sv.6'ya (%12) diğerlerinden çok önce ulaşır; Sv.5 sopa ile 4 polise vururken (4 × 25 = 100 hasar/sn) saniyede 12 can.
- **Görünüm:** ahududu kırmızısı (Topik'in kırmızısından pembeye kayık), tepede kısa sap ucunda pembe kalp (oyun kamerasına dönük); T1-T3'te ağız köşesinde minik vampir dişi. `build_blobs.py` → `CH_Cano_T1..T5`, portre `Icon_Cano.png`.

---

## 5. Düşmanlar ve Bosslar

### Normal düşmanlar (karşılaştırma için)
| | Robot köpek | Polis | Elit Polis |
|---|---|---|---|
| **Ne zaman** | 1. dk'dan itibaren, **3'lü sürü** | 1. dk'dan itibaren | Blob **Medium** olunca (en geç 4. dk) |
| **Sayı** | Ayrı tavan: 8 + seviye × 1 (en fazla 18) | Polis + elit: 8 + seviye × 2.2 (en fazla 30) | Polis tavanı içinde, en fazla 8 |
| **Can** | 10 | 50 | 250 |
| **Saldırı** | Yakın dövüş, 4 hasar, saniyede 1 | Yakın dövüş, 10 hasar, saniyede 1 | Tabanca: 0.5 sn nişan lazeri → **3'lü seri**, mermi başına 10 hasar, menzil 7.5. Arada bina varsa ateş etmez |
| **Hız** | 3.4 | 2.3 | 2.1 |
| **Görme** | 40 (doğduğu andan koşar) | 15 | 23 |
| **Yutulma** | Little blob | Medium blob | Giant blob |
| **Davranış** | Kaçmaz | Kaçmaz | Kaçmaz |
| **Ödül** | 1.5 mass, 1 coin, %2 kalp | 3 mass, 2 coin, %5 kalp | 20 mass, 5–10 coin, **sandık** (35 coin + ekstra kart), %100 kalp |

### 👹 Miniboss: "Polis Tankı"
Aynı tasarım, iki kez gelir, ikincisi daha güçlü. Elit polisin nişan-kilit-ateş akışını top mermisiyle kullanır; üstünden geçtiği arabaları ve sokak eşyalarını ezer.

| | 1. Miniboss | 2. Miniboss |
|---|---|---|
| **Geliş** | **5. seviye** (en geç 9. dk) | **8. seviye** (en geç 15. dk; ilki hâlâ yaşıyorsa o ölene kadar bekler) |
| **Can** | 600 | 1200 |
| **Saldırı** | Tank topu: 1.1 sn nişan lazeri (son 0.4 sn kilit) → **1 mermi × 25 hasar**, sonra 2.5 sn bekleme. Menzil 8, mermi hızı 9 | 0.9 sn nişan (son 0.35 sn kilit) → **2 mermi × 20 = 40 hasar** (0.45 sn arayla), 2.2 sn bekleme. Menzil 8, mermi hızı 10 |
| **Hız** | 2.9. Ekran dışında ve 25 birimden uzaktaysa **×2** (yetişme), ekrana girince normale döner | 3.2, yetişme aynı |
| **Algı** | Tüm harita: oyuncuyu her yerden bulur | Tüm harita |
| **Yutulma** | **Hiçbir şekilde yutulamaz**, silahla ölür | Aynı |
| **Ödül** | 150 skor + 5–10 coin + sandık (35 coin + ekstra kart) + **15–25 bonus coin** | 250 skor + 5–10 coin + sandık (35 coin + ekstra kart) + **25–40 bonus coin** |

- **Silahla öldürme süresi:** 1. Miniboss: temel silahla ~60 sn, + Havai Fişek Sv.1 ~27 sn, Sopa 8 ile ~20 sn. 2. Miniboss: ~120 sn / Havai Fişek Sv.1 ~53 sn / ~40 sn. (Temel silahın menzili yüzeyden 3 olduğu için tanka yaklaşmak gerekir.)
- **Ekranda:** Boss can barı HUD'da görünür.
- **Görsel:** Polis tankı modeli, **2.5 kat büyük** (en dar sokağa sığan en büyük boy) (`CityKitPipeline.MinibossScale`). Ateş edince geri teper.
- **Kaçış:** Kilit anında (lazer kalınlaşınca) yana kaçan oyuncu mermiyi atlatır. Arada bina varsa ateş etmez, yürür. Menzil 8 (tankın merkezinden): sopadan çok uzun; Havai Fişek'le (gövde yüzeyinden 8, yani merkezden 8 + gövde yarıçapı) aşağı yukarı eşit. Blob büyüdükçe Havai Fişek tankın menzilinin hemen dışından vurabilir.
- **Ölçek:** 1. miniboss geldiğinde can ~120-150 iken tek isabet canın ~1/5'i; 5-6 isabet öldürür.

### 🚁 Final Boss: "Polis Helikopteri"
**"Vur, alçalt, büyü, yut."** Helikopter havada uçar; vurdukça alçalır, sen de ona uzanacak kadar büyüyüp yutarsın.

| | Final Boss |
|---|---|
| **Geliş** | **10. seviye** (en geç 21. dk). Bir miniboss yaşıyorsa beklemez, miniboss çekilir |
| **Can** | 9000. **Silahla ölmez** (can 1'in altına inmez) |
| **İrtifa** (kızakların yerden yüksekliği) | Başta **8** → can %75'te **7** → %50'de **6** → %25'te **5**. Yumuşak iner, geri yükselmez |
| **Saldırı** | Mini roket serisi, yukarıdan: 1 sn nişan lazeri (son 0.4 sn kilit) → **6 roket × 6 hasar** (0.12 sn arayla), 2 sn bekleme. Menzil 10 (yatay). Arada bina varsa ateş etmez. Roketler blob'a, yere ya da duvara değince patlar: blob yüzeyi patlamaya **1.2 birim** yakınsa hasar alır (ıskalayıp yanına düşen de vurur). Mermi: `HeliRocketBullet` (Helikopter Mermi Roketini Kur) |
| **Roket (alan)** | Her **7 sn**'de (ilk 6. sn'de) blob'un gideceği yere **kırmızı daire**; halka atar, iç disk dolar, **1.6 sn** sonra roket düşer: **20 hasar**. Can %75'in üstünde 1, %50'ye kadar 2, sonrası **3 roket** (ekler hedefin iki yanına, 0.3 sn arayla). Daire yarıçapı 1.8 + blob yarıçapı × 0.6; daireden çıkan (kenarı sıyıran) kurtulur. Menzil 16 (yatay) |
| **Hız** | 3.5. Ekran dışında ve uzaktaysa ×2 (yetişme) |
| **Algı** | Tüm harita |
| **Yutulma** | Blob'un tepesi helikoptere **değdiği an**, hangi irtifada olursa olsun. Çok büyüyen oyuncu daha erken bitirir |
| **Ödül** | Yutulunca **run "tamamlandı" olarak biter** (ölümden ayrı ekran), 300 mass, 600 skor, 5–10 coin + sandık (35) + **60–100 bonus coin**, coin'in tamamı kasaya (ölümde %50) |

- **Ne kadar büyümek gerekir** (blob tepesi ≈ 1.1 × ölçek): irtifa 5 ≈ **1340 mass** (Giant ile Dev arası; Dev olmak gerekmez), 6 ≈ 1950, 7 ≈ 2700, 8 ≈ 3600.
- **Silahlar yukarıyı da vurur:** Havai Fişek havadaki hedefe otomatik 3D nişan alır; sopa tam tepedeki hedefe de değer. Menzil yatay ölçülür; helikopterin altına yaklaşman yeter.
- **Kamera:** Helikopter gelince hızlıca uzaklaşıp ilk 3 sn ikinizi birden kadraja alır; sonra uzaklık kalır, kamera blob'u ortalar; boss ölünce normale döner.
- **Görsel:** Rotorlar döner, havada süzülür, gittiği yöne eğilir. Son irtifada rotor yavaşlar ("hasarlı"). Burnun altındaki projektör yere sarı ışık düşürür, yavaşça sağa sola tarar.
- **Zaman sınırı yok:** Run sadece ölümle ya da helikopter yutulunca biter.

### Zorluk zaman çizelgesi
Boss'lar süreye değil seviyeye bağlı (1. Miniboss Sv.5, 2. Miniboss Sv.8, Final Boss Sv.10; en geç 9 / 15 / 21. dk).

| Dakika | Olay |
|---|---|
| 0–1 | Sadece yenebilir objeler, yayalar, arabalar |
| 1 | Polisler ve robot köpek sürüleri gelmeye başlar (polis + elit seviyeyle 8 → 30, köpek ayrı 8 → 18) |
| **Medium** (en geç 4) | Elit polis dalgası (aynı anda en fazla 8 elit, polis tavanının içinde) |
| 5 | Düşman hasarı ×1.2 |
| 10 | Düşman spawn'ı ×2 hızlı (tavanlar aynı) |
| 15 | Düşman hızı ×1.15 |
| 20 | Düşman hasarı ×3 |
