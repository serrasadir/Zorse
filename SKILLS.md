# Left No Crumbs: Skill, Karakter ve Boss Rehberi

> Skill, karakter, düşman ve boss'ların **güncel** değerleri. Değerler koddan ve asset'lerden okunur (`Assets/_Project/Data/Upgrades/`, `Data/Characters/`, `ScriptableObjects/Enemies/`); değişince burası da güncellenir, eski hali yazılmaz.
> Skill değeri değiştirmek için ilgili `Upgrade_*.asset` dosyasında `Max Level` / `Per Level Value` alanlarını Inspector'dan düzenle.
> Oyunun genel hali `OYUN.md`'de; açık denge soruları GitHub issue'larında.

## 1. Genel kurallar

**Seviye atlama.** Yediğin her şeyin mass'ı aynı zamanda XP'dir. Bir sonraki level için gereken XP `20 + level × 15 + level² × 2`: 1. level 20 XP, 2. level 37, 3. level 58, … 10. level 317. Karesel terim geç oyunda (bina = 25–50 XP) seviye atlamanın çok hızlanmasını önler.

**Kart ekranı.** Level atlayınca oyun durur ve 3 kart gelir. Biri seçilir.
- Kartlar ağırlıklı rastgele çekilir: çoğu skill ağırlık **10**, Rejenerasyon **8**, Can Artışı **4**. Ağırlığı düşük olan daha seyrek gelir.
- Max seviyeye ulaşan skill havuzdan çıkar, bir daha gelmez.
- Havuzda 3'ten az skill kalırsa daha az kart gelir. Hepsi maxlıysa level-up ödülsüz geçer. Bunun için 57 seçim gerekir, pratikte ulaşılamaz.
- Kartın adının yanında **YENİ** (ilk kez alıyorsun) ya da **Sv. 3/8** (bu seçimle ulaşacağın seviye) yazar.
- Tek lokmada birden fazla level atlarsan kart ekranları arka arkaya gelir, seviye kaybolmaz.
- Elit sandığı da bir kart ekranı açar.

**Run sonu.** Skiller run'a özeldir. Ölüm ya da restart hepsini sıfırlar.

**Kalıcı bonuslarla etkileşim.** Market'ten alınan kalıcı statlar (Hız, Max Can, Mass/Coin kazancı, XP çarpanı) her run başında uygulanır. Skiller bunların üstüne eklenir.

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

## 2. Skiller (7 adet)

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
- **Görsel:** Blob'un sağ yanında süzülen, yüzü dışa bakan, gidiş yönüyle dönen **sokak eşyası kalkan**. **Mermo'da kafada** şapka gibi durur (yanlarında tabancaları var; `CharacterData._armorOnHead`): tepede yatay, ön kenarı gidiş yönünde, hafif geriye yatık, %80 boy; kırılınca fırıldak gibi döner. Form seviyeyle, yeni mekanikle aynı anda değişir: **Sv.1-2** çöp kutusu kapağı → **Sv.3-5** dikenli "girilmez" tabelası (dikenler) → **Sv.6-7** dikenli menhol kapağı (güçlü dikenler) → **Sv.8** altın menhol kapağı + kızıl mücevher (şok dalgası). Kalkan azaldıkça koyulaşır, vuruşta parlar ve şişer, kırılınca dönerek küçülüp kaybolur, dolmaya başlayınca geri "pop" eder; yeni form da pop ile gelir. Modeller `zorse-blender/build_armor_shields.py` → `Resources/FX/Shield` (148–220 üçgen).

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

### 💚 Rejenerasyon
- **Ne yapar:** Her saniye can yeniler.
- **Seviye başına:** +0.5 can/sn · **Max:** 8 · **Ağırlık:** 8
- **Kod:** `RegenBoostEffect` → `BlobHealth.EnableRegen`

| Seviye | 1 | 4 | 8 |
|---|---|---|---|
| Can/sn | 0.5 | 2 | **4** |
| Dakikada | 30 | 120 | 240 |

**Örnek:** Seviye 4'te 50 hasar aldıysan yaklaşık 25 saniyede toparlarsın.

---

### 💨 Hız Artışı
- **Ne yapar:** İki şey yapar. (1) Hız çarpanını artırır. (2) Büyüdükçe gelen yavaşlamayı biraz hafifletir.
- **Seviye başına:** +%10 hız, yavaşlama üssü −0.015 · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `SpeedBoostEffect` → `BlobController.SetSpeedMultiplier` + `ReduceTierSlowdownExponent`
- **Formül:** `hız = 5 × çarpan / sıra^üs` (sıra: Tiny 1 … Giant 5, Dev 6). Başlangıç üssü **0.24** (sahnede `BlobController._tierSlowdownExponent`).

| Seviye | Çarpan | Üs | Tiny hızı | Giant hızı | Dev hızı |
|---|---|---|---|---|---|
| 0 | 1.0 | 0.24 | 5.0 | 3.4 | 3.25 |
| 4 | 1.4 | 0.18 | 7.0 | 5.2 | 5.1 |
| 8 | **1.8** | **0.12** | 9.0 | **7.4** | 7.3 |

**Örnek:** Topik'in pasifi +%25 hız ve yavaşlama üssü −0.03 (0.21). Topik ile Hız 8 = çarpan 2.05, üs 0.09; Giant hızı ~8.9.

---

### 🌀 Vakum
- **Ne yapar:** Çevrendeki consumable'ları kendine doğru çeker. Sadece yiyebileceğin tier'dakileri çeker.
  **Çekmedikleri:** coin, yaya, polis, şehir binaları, hazard'lar.
- **Temel menzil:** 1.2 · **Seviye başına:** +0.35 · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `VacuumEffect` → `VacuumComponent` (0.15 sn'de bir tarama, çekme hızı 6 birim/sn)
- **Menzil gövde yüzeyinden ölçülür:** blob büyüdükçe çekme alanı da büyür. Toplam yarıçap = gövde yarıçapı + menzil.
- **Görsel:** çekim alanı boyunca zeminde sürekli dönen hafif bir kasırga: iki katman açık mavi, kesik kesik spiral rüzgar çizgisi, içe doğru akar. Menzil arttıkça büyür. Doku/materyal: `Tools → Blob.io → Vakum Efektini Kur` (`Resources/FX/Vacuum`).

| Seviye | 1 | 4 | 8 |
|---|---|---|---|
| Menzil (gövde dışına) | 1.55 | 2.6 | **4.0** |
| Mıknato ile (×1.25) | 1.94 | 3.25 | **5.0** |
| Toplam yarıçap, Giant blob (gövde ~2) | 3.55 | 4.6 | 6.0 |

**Örnek:** Seviye 4'te, gövdenin 2.6 birim dışına kadar yiyebileceğin objeler sana akar. Blob Giant olunca alan da onunla büyür.

---

### 🔫 Silah Gücü
- **Ne yapar:** Silahını güçlendirir. Her seviye "+hasar" değil: kilometre taşı seviyelerinde yeni bir yetenek açılır. Kart, o seviyede ne kazanacağını yazar.
- **Max:** 8 · **Ağırlık:** 10
- **Kod:** `WeaponUpgradeEffect` (kilometre taşı seviyeleri burada, Inspector'dan değiştirilebilir) → `WeaponBase`. Kart metinleri `Upgrade_Weapon.asset` → Level Descriptions.
- **Temel silah (3 karakter aynı):** saniyede **2.5 atış × 4 hasar** (saniye başına 10), menzil 8. Mermiler küçük ve hızlı.

| Sv. | Kazanılan | Mermi hasarı | Hedef başına sn. hasar |
|---|---|---|---|
| 0 | – | 4 | 10 |
| 1 | +2 hasar | 6 | 15 |
| 2 | +2 hasar | 8 | 20 |
| 3 | ⭐ **Çift hedef:** en yakın 2 düşmana aynı anda ateş | 8 | 20 (×2 hedef) |
| 4 | +2 hasar | 10 | 25 |
| 5 | ⭐ **Delip geçme:** mermi ilk düşmanı delip arkasındakine de vurur | 10 | 25 |
| 6 | +2 hasar | 12 | 30 |
| 7 | ⭐ **Üçlü hedef** | 12 | 30 (×3 hedef) |
| 8 | ⭐ **Son form** (karaktere özel, aşağıda) | 12 | – |

**Son formlar (Sv. 8):**
| Karakter | Silah | Son form |
|---|---|---|
| Topik | Top (çarptığı yerde 2 birimlik patlama) | **Büyük Patlama:** patlama alanı 2.5 katı (5 birim). Polis kümesini tek atışta temizler |
| Mıknato | Metal Bilye (güdümlü) | **Sekme:** isabet eden bilye 6 birim içindeki en yakın 2 düşmana daha seker. Her mermi 3 düşmana vurur |
| Mermo | Pistol (düz, en hızlı) | **Pompalı:** her atış 40°'lik yelpazede 5 saçma (saçma başına hasarın %60'ı). Yakında 3 kat hasar |

**Örnek:** Sv. 7'de 3 polis sana koşuyor: her biri saniyede 2.5 mermi yer; delme sayesinde arkalarındakiler de vurulur. Polis (50 can) tek hedefe 12 hasarla ~2 saniyede düşer.
**Not:** Tek bir elite (250 can) karşı Sv. 8'de saniyede 30 hasar = ~8 saniye. Güç kalabalığa karşı; tek güçlü düşmanda daha yavaş.

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
Polis **Medium**, elit polis **Giant** blob ister.

**Isırık bedeli:** Polis yutulurken ısırır: tier tam eşikteyse 5, 1 tier üstteyse 2.5, 2+ tier üstteyse 0 hasar.

**Kaçış:** Polisler, blob onları yiyebilecek boydayken blob'u görünce kaçar.

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

## 4. Karakterler (3 adet)

**Açılma koşulları:**
- **Topik:** baştan açık.
- **Mıknato:** Market'te **500 kredi** ile satın alınır (kalıcı). Kredi run sonunda coin'den gelir: kazanınca coin'in %100'ü, ölünce %50'si.
- **Mermo:** toplam **3 run** bitirince kendiliğinden açılır. Ölümle biten run da, kazanılan run da sayılır. Sayı kalıcı kayıtta tutulur (`MetaProgression.CompletedRuns`), koşul `Char_Mermo.asset` → `Required Completed Runs`.
- Kilitli karakter Lobby'de seçilemez; isminin altında koşul yazar: "(Market'te açılır)" ya da "(2 run sonra açılır)".

Üç karakter de aynı blob'dur; farkları **başlangıç silahı** ve **pasif yetenektir**. Temel silah değerleri üçünde aynı: saniyede 2.5 atış × 4 hasar, menzil 8. Silah Gücü yolu (Bölüm 2) üçünde de aynı ilerler, sadece **Sv. 8 son formu** karaktere özeldir.

| | 🟠 Topik | 🧲 Mıknato | 🔫 Mermo |
|---|---|---|---|
| **Kilit** | Başlangıçta açık | Market'te **500 kredi** ile satın alınır | **3 run bitirince** açılır (ölümle biten run da sayılır) |
| **Silah** | Top: yay çizerek uçar, çarptığı yerde patlar | Metal bilye: güdümlü, hedefe kıvrılır | Tabanca: düz ve en hızlı mermi |
| **Mermi hızı / ömrü** | 14 / 0.9 sn | 12 / 1.5 sn | 18 / 0.7 sn |
| **Silah özelliği** | 2 birim yarıçapta **alan hasarı** (vurulan dışındakilere de tam hasar) | **Iskalamaz** (sürekli hedefe döner) | Bir boy büyük objeyi **parçalar** (pasif) |
| **Pasif** | **+%25 hız** ve büyüdükçe ~%10 daha az yavaşlama | **Vakum baştan açık** (1.2 menzil) + çekim etkisi **×1.25** | Mermileri, blob'un **tam 1 tier üstündeki** objeyi 3 yenebilir parçaya böler |
| **Sv. 8 son form** | **Büyük Patlama:** alan 2.5 kat (5 birim) | **Sekme:** isabetten sonra 6 birim içindeki 2 düşmana daha seker | **Pompalı:** 40°'lik yelpazede 5 saçma (her biri %60 hasar) |
| **Oyun tarzı** | Hızlı, hantallaşmayan; kalabalığı patlamayla dağıtır | Toplayıcı; yemi kendine çeker, hızlı büyür | Fırsatçı; büyük lokmaları bir boy erken yer |

### 🟠 Topik
- **Pasif:** Hız çarpanı +0.25 (Hız Artışı skiliyle toplanır: Hız 8 ile ×2.05). Ayrıca büyüme yavaşlaması üssü 0.24 → 0.21. Giant'ta bile çevik kalır.
- **Silah:** Top yay çizerek uçar (0.4 birim yükseklik), ilk düşmana çarpınca patlar ve 2 birim içindeki diğer düşmanlara da tam hasar verir. Kalabalığa en etkili silah.
- **Örnek:** Sv. 7'de 3 hedefe ateş eden, her biri 2 birimlik patlama yapan Topik, sürüyü saniyeler içinde eritir. Sv. 8'de patlama 5 birim olur.
- **Zayıf yanı:** Top binaya çarpınca durur; patlama ince bir duvarın arkasına sızabilir.

### 🧲 Mıknato
- **Pasif:** Oyuna Vakum skili açık başlar (1.2 menzil, gövde yüzeyinden; ×1.25 ile 1.5). Çekim menzili ve çekme hızı ×1.25. Vakum skilini alırsa çarpan onu da büyütür (Vakum 8 → 5.0 menzil).
- **Silah:** Metal bilye hedefe sürekli kıvrılır, pratikte ıskalamaz. Hedef ölürse düz gider.
- **Örnek:** Diğer karakterler Vakum'u kart olarak beklerken Mıknato ilk saniyeden çevresini toplar. Erken büyüme ve erken level demek. Kara Delik evrimine (Vakum 8 + Sindirim 8) en doğal giden karakter.
- **Not:** En güçlü pasif. Bu yüzden 500 kredilik Market kilidi var.

### 🔫 Mermo
- **Pasif:** Mermisi blob'un **tam bir boy büyüğü** olan objeye (şehir binaları dahil) çarparsa onu 3 parçaya böler. Parçalar blob'un yiyebileceği boyda ve toplamda objenin mass'ı kadar verir. Daha büyük objelerde mermi durur; dev binaları silemez. Yayalar ve hazard'lar parçalanmaz.
- **Silah:** En hızlı, düz giden mermi. Nişan, polisin hareketine göre önden kesilir.
- **Örnek:** Large blob, Giant bir binaya (50 mass) ateş eder → 3 × 16.7 mass'lık parça. Normalde Giant olmadan yenemeyeceği lokmayı erken yer.
- **Son form:** Pompalı yakın mesafede çok güçlü (5 saçma × %60 = 3 kat hasar).

---

## 5. Düşmanlar ve Bosslar

### Normal düşmanlar (karşılaştırma için)
| | Polis | Elit Polis |
|---|---|---|
| **Ne zaman** | 1. dk'dan itibaren | Blob **Medium** olunca (en geç 4. dk) |
| **Can** | 50 | 250 |
| **Saldırı** | Yakın dövüş, 10 hasar, saniyede 1 | Tabanca: 0.5 sn nişan lazeri → **3'lü seri**, mermi başına 10 hasar, menzil 7.5. Arada bina varsa ateş etmez |
| **Hız** | 3 | 2.1 |
| **Yutulma** | Medium blob | Giant blob |
| **Davranış** | Blob onu yiyebiliyorsa **kaçar** | Kaçmaz |
| **Ödül** | 3 mass, 1 coin, %5 kalp | 20 mass, 5–10 coin, **sandık**, %100 kalp |

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
| **Ödül** | 150 skor + 5–10 coin + sandık + **15–25 bonus coin** | 250 skor + 5–10 coin + sandık + **25–40 bonus coin** |

- **Silahla öldürme süresi:** 1. Miniboss: temel silahla ~60 sn, Silah Gücü 8 ile ~20 sn. 2. Miniboss: ~120 sn / ~40 sn.
- **Ekranda:** Boss can barı HUD'da görünür.
- **Görsel:** Polis tankı modeli, **2.5 kat büyük** (en dar sokağa sığan en büyük boy) (`CityKitPipeline.MinibossScale`). Ateş edince geri teper.
- **Kaçış:** Kilit anında (lazer kalınlaşınca) yana kaçan oyuncu mermiyi atlatır. Arada bina varsa ateş etmez, yürür. Menzil oyuncu silahıyla eşit (8): o sana ateş edebiliyorsa sen de ona edebilirsin.
- **Ölçek:** 1. miniboss geldiğinde can ~120-150 iken tek isabet canın ~1/5'i; 5-6 isabet öldürür.

### 🚁 Final Boss: "Polis Helikopteri"
**"Vur, alçalt, büyü, yut."** Helikopter havada uçar; vurdukça alçalır, sen de ona uzanacak kadar büyüyüp yutarsın.

| | Final Boss |
|---|---|
| **Geliş** | **11. seviye** (en geç 21. dk). Bir miniboss yaşıyorsa beklemez, miniboss çekilir |
| **Can** | 9000. **Silahla ölmez** (can 1'in altına inmez) |
| **İrtifa** (kızakların yerden yüksekliği) | Başta **8** → can %75'te **7** → %50'de **6** → %25'te **5**. Yumuşak iner, geri yükselmez |
| **Saldırı** | Mini roket serisi, yukarıdan: 1 sn nişan lazeri (son 0.4 sn kilit) → **6 roket × 6 hasar** (0.12 sn arayla), 2 sn bekleme. Menzil 10 (yatay). Arada bina varsa ateş etmez. Roketler blob'a, yere ya da duvara değince patlar: blob yüzeyi patlamaya **1.2 birim** yakınsa hasar alır (ıskalayıp yanına düşen de vurur). Mermi: `HeliRocketBullet` (Helikopter Mermi Roketini Kur) |
| **Roket (alan)** | Her **7 sn**'de (ilk 6. sn'de) blob'un gideceği yere **kırmızı daire**; halka atar, iç disk dolar, **1.6 sn** sonra roket düşer: **20 hasar**. Can %75'in üstünde 1, %50'ye kadar 2, sonrası **3 roket** (ekler hedefin iki yanına, 0.3 sn arayla). Daire yarıçapı 1.8 + blob yarıçapı × 0.6; daireden çıkan (kenarı sıyıran) kurtulur. Menzil 16 (yatay) |
| **Hız** | 3.5. Ekran dışında ve uzaktaysa ×2 (yetişme) |
| **Algı** | Tüm harita |
| **Yutulma** | Blob'un tepesi helikoptere **değdiği an**, hangi irtifada olursa olsun. Çok büyüyen oyuncu daha erken bitirir |
| **Ödül** | Yutulunca **run "tamamlandı" olarak biter** (ölümden ayrı ekran), 300 mass, 600 skor, **60–100 bonus coin**, tam kredi (ölümde %50) |

- **Ne kadar büyümek gerekir** (blob tepesi ≈ 1.1 × ölçek): irtifa 5 ≈ **1340 mass** (Giant ile Dev arası; Dev olmak gerekmez), 6 ≈ 1950, 7 ≈ 2700, 8 ≈ 3600.
- **Oyuncu silahı yukarı ateş eder:** Havadaki hedefe nişan otomatik 3D (skill gerekmez). Menzil yatay ölçülür; helikopterin altına yaklaşman yeter.
- **Kamera:** Helikopter gelince hızlıca uzaklaşıp ikinizi birden kadraja alır; boss ölünce normale döner.
- **Görsel:** Rotorlar döner, havada süzülür, gittiği yöne eğilir. Son irtifada rotor yavaşlar ("hasarlı"). Burnun altındaki projektör yere sarı ışık düşürür, yavaşça sağa sola tarar.
- **Zaman sınırı yok:** Run sadece ölümle ya da helikopter yutulunca biter.

### Zorluk zaman çizelgesi
Boss'lar süreye değil seviyeye bağlı (1. Miniboss Sv.5, 2. Miniboss Sv.8, Final Boss Sv.11; en geç 9 / 15 / 21. dk).

| Dakika | Olay |
|---|---|
| 0–1 | Sadece yenebilir objeler, yayalar, arabalar |
| 1 | Polisler gelmeye başlar (aynı anda en fazla 30 düşman) |
| **Medium** (en geç 4) | Elit polis dalgası (aynı anda en fazla 15 elit, toplam 30'un içinde) |
| 5 | Düşman hasarı ×1.2 |
| 10 | Düşman yoğunluğu ×2 (aynı anda en fazla 60) |
| 15 | Düşman hızı ×1.15 |
| 20 | Düşman hasarı ×3 |
