# Left No Crumbs: Skill, Karakter ve Boss Rehberi

> Son güncelleme: 2026-11-03. Değerler koddan ve asset'lerden okundu (`Assets/_Project/Data/Upgrades/`, `Data/Characters/`, `ScriptableObjects/Enemies/`).
> Değer değiştirmek için ilgili `Upgrade_*.asset` dosyasındaki `Max Level` / `Per Level Value` alanlarını Inspector'dan düzenle; kod gerekmez.
> GDD karşılığı: `GDD_v2.md` §5.

---

## 1. Genel kurallar

**Seviye atlama.** Yediğin her şeyin mass'ı aynı zamanda XP'dir. Bir sonraki level için gereken XP `20 + level × 15`: 1. level 20 XP, 2. level 35, 3. level 50, … 10. level 155.

**Kart ekranı.** Level atlayınca oyun durur ve 3 kart gelir. Biri seçilir.
- Kartlar ağırlıklı rastgele çekilir: çoğu skill ağırlık **10**, Rejenerasyon **8**, Yırtıcı Çene **6**. Ağırlığı düşük olan daha seyrek gelir.
- Max seviyeye ulaşan skill havuzdan çıkar, bir daha gelmez.
- Havuzda 3'ten az skill kalırsa daha az kart gelir. Hepsi maxlıysa level-up ödülsüz geçer. Bunun için yaklaşık 61 seçim gerekir, pratikte ulaşılamaz.
- Kartın adının yanında **YENİ** (ilk kez alıyorsun) ya da **Sv. 3/8** (bu seçimle ulaşacağın seviye) yazar.
- Tek lokmada birden fazla level atlarsan kart ekranları arka arkaya gelir, seviye kaybolmaz.
- Elit sandığı da bir kart ekranı açar.

**Run sonu.** Skiller run'a özeldir. Ölüm ya da restart hepsini sıfırlar.

**Kalıcı bonuslarla etkileşim.** Market'ten alınan kalıcı statlar (Hız, Max Can, Mass/Coin kazancı, XP çarpanı) her run başında uygulanır. Skiller bunların üstüne eklenir.

**Blob boyutu referansı** (örneklerde kullanılıyor, sahne değerleri):

| Tier | Gereken mass | Görsel ölçek | Gövde yarıçapı |
|---|---|---|---|
| Tiny | 0 | 0.5 | 0.25 |
| Small | 50 | ~1.1 | ~0.55 |
| Medium | 200 | ~2.0 | ~1.0 |
| Large | 500 | ~2.9 | ~1.5 |
| Giant | 1000 | ~4.0 | ~2.0 |

---

## 2. Skiller (9 adet)

### 🛡️ Zırh
- **Ne yapar:** Aldığın tüm hasarı yüzdesel azaltır (polis ısırığı, elit mermisi, araba çarpması, hazard).
- **Seviye başına:** −%7 hasar · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `DamageReductionEffect` → `BlobHealth._armorMultiplier`

| Seviye | 1 | 2 | 4 | 6 | 8 |
|---|---|---|---|---|---|
| Alınan hasar | %93 | %86 | %72 | %58 | **%44** |

**Örnek:** Elit mermisi 10 hasar. Zırh 8'de 4.4 hasar alırsın. Polis ısırığı 5'ten 2.2'ye iner.
**Not:** Azalmalar toplanır, çarpılmaz (seviye 8 = 1 − 0.56).

---

### ❤️ Can Artışı
- **Ne yapar:** Max canı artırır. Eklenen miktar kadar da anında iyileştirir.
- **Seviye başına:** +10 max can · **Max:** 8 · **Ağırlık:** 10
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
- **Formül:** `hız = 5 × çarpan / tier^üs`. Başlangıç üssü 0.3.

| Seviye | Çarpan | Üs | Tiny hızı | Giant hızı |
|---|---|---|---|---|
| 0 | 1.0 | 0.30 | 5.0 | 3.1 |
| 4 | 1.4 | 0.24 | 7.0 | 4.7 |
| 8 | **1.8** | **0.18** | 9.0 | **6.7** |

**Örnek:** Topik'in pasifi +%25 hız ve yavaşlama üssü −0.03. Topik ile Hız 8 = çarpan 2.05, üs 0.15; Giant hızı ~8.

---

### 🌀 Vakum
- **Ne yapar:** Çevrendeki consumable'ları kendine doğru çeker. Sadece yiyebileceğin tier'dakileri çeker.
  **Çekmedikleri:** coin, yaya, polis, şehir binaları, hazard'lar.
- **Seviye başına:** +0.5 menzil · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `VacuumEffect` → `VacuumComponent` (0.15 sn'de bir tarama, çekme hızı 6 birim/sn)
- **Menzil gövde yüzeyinden ölçülür:** blob büyüdükçe çekme alanı da büyür. Toplam yarıçap = gövde yarıçapı + menzil.

| Seviye | 1 | 4 | 8 |
|---|---|---|---|
| Menzil (gövde dışına) | 3.5 | 5.0 | **7.0** |
| Mıknato ile (×1.25) | 4.4 | 6.25 | **8.75** |
| Toplam yarıçap, Giant blob (gövde ~2) | 5.5 | 7.0 | 9.0 |

**Örnek:** Seviye 4'te, gövdenin 5 birim dışına kadar yiyebileceğin objeler sana akar. Blob Giant olunca alan da onunla büyür.

---

### 🔫 Silah Gücü
- **Ne yapar:** Silahını güçlendirir. Her seviye "+hasar" değil: kilometre taşı seviyelerinde yeni bir yetenek açılır. Kart, o seviyede ne kazanacağını yazar.
- **Max:** 8 · **Ağırlık:** 10
- **Kod:** `WeaponUpgradeEffect` (kilometre taşı seviyeleri burada, Inspector'dan değiştirilebilir) → `WeaponBase`. Kart metinleri `Upgrade_Weapon.asset` → Level Descriptions.
- **Temel silah (3 karakter aynı):** saniyede **2.5 atış × 4 hasar** (saniye başına 10), menzil 8. Mermiler küçük ve hızlı. (2026-11-03'e kadar saniyede 1 × 10'du.)

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
**Not:** Tek bir elite (250 can) karşı Sv. 8'de saniyede 30 hasar = ~8 saniye. Eski sistemde 50 hasar/sn ile 5 saniyeydi. Güç artık kalabalığa karşı; tek güçlü düşmanda biraz daha yavaş.

---

### 🍖 Sindirim
- **Ne yapar:** Yediğin her şeyden kazandığın mass'ı, dolayısıyla XP'yi artırır. Daha hızlı büyür, daha hızlı level atlarsın.
- **Seviye başına:** +%10 · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `SindirimEffect` → `BlobGrowth._massGainMultiplier`

| Seviye | 2 | 4 | 8 |
|---|---|---|---|
| Kazanç | ×1.2 | ×1.4 | **×1.8** |

**Örnek:** 10 mass'lık bir obje Sindirim 8'de 18 mass verir. Medium'a (200 mass) ~111 mass'lık yemeyle ulaşırsın.

---

### 🩹 Yutuş
- **Ne yapar:** Her yemede can yeniler. Consumable, yaya, polis ve Kara Delik'in yuttukları sayılır. **Araba sayılmaz.**
- **Seviye başına:** +1 can/yeme · **Max:** 8 · **Ağırlık:** 10
- **Kod:** `YutusEffect` → `HealOnConsumeComponent`

**Örnek:** Seviye 8'de, saniyede 2 küçük obje yiyorsan saniyede 16 can. Bu, Rejenerasyon 8'in 4 katı. Sürekli yiyen oyuncu için en güçlü iyileşme.

---

### 🦷 Yırtıcı Çene
- **Ne yapar:** Polis yutarken blob'unu olduğundan seviye başına 1 tier büyük sayar.
  **Consumable'ları, yayaları, arabaları ve binaları etkilemez.** Elitleri de kapsamaz; elit yemenin yolu Avcı Formu.
- **Seviye başına:** +1 tier · **Max:** 2 · **Ağırlık:** 6 (seyrek gelir)
- **Kod:** `YirticiCeneEffect` → `ConsumptionBonusComponent`, kullanıldığı yer `BlobConsumption.OnTriggerEnter`

Polis normalde **Medium** blob ister.

| Blob | Yırtıcı Çene yok | Sv. 1 | Sv. 2 |
|---|---|---|---|
| Tiny | ❌ | ❌ | ✅ |
| Small | ❌ | ✅ | ✅ |
| Medium+ | ✅ | ✅ | ✅ |

**Isırık bedeli:** Polis yutulurken ısırır. Hasar, (efektif) tier'ın eşiğin ne kadar üstünde olduğuna göre değişir: eşitse 5, 1 tier üstteyse 2.5, 2+ tier üstteyse 0. Yırtıcı Çene ile erken yediğin polis hep tam ısırır.

**Kaçış (2026-11-03):** Polisler, blob onları yiyebilecek boydayken (efektif tier ≥ Medium) blob'u görünce kaçar. Yırtıcı Çene efektif tier'ı yükselttiği için polisler **daha erken kaçmaya başlar**, yani skill "erken av" ile birlikte "erken kovalamaca" da getirir.

#### ⚠️ Neden saçma hissettiriyor
1. **Ödülü yok denecek kadar az.** Polis yutunca **3 mass** veriyor; Small'dan Medium'a geçmek 150 mass ister. Bir polis yutmak bir kaç küçük consumable'dan az kazandırıyor. Üstüne 5 can ısırık yiyorsun.
2. **Polisler zaten silahla ölüyor.** Silah 1 sn'de 10 hasar veriyor; polis 50 canla birkaç saniyede ölüyor ve coin düşürüyor. Yutmanın öldürmeye üstünlüğü yok.
3. **Medium'dan sonra hiçbir şey yapmıyor.** Oyunun ikinci yarısında boş bir kart.
4. **Tek değeri evrime giriş.** Avcı Formu'nun girdisi olduğu için alınıyor.

#### Değişiklik fikirleri (karar senin)
- **A) Polisin ödülünü büyüt:** polis mass'ını 3 → 15-20 yap. Skill otomatik anlam kazanır, polis yemek "erken büyüme" stratejisi olur. Tek asset değişikliği (`EnemyData_Police._massReward`).
- **B) Isırığı kaldır:** Yırtıcı Çene ile yutulan polis ısırmasın. Skill güvenli bir avlanma aracı olur.
- **C) Kapsamı genişlet:** consumable/yaya eşiğini de 1 tier düşürsün ("lokman senden büyük olabilir" açıklamasına uygun). En güçlü seçenek, büyüme hızını belirgin artırır; ağırlığı düşük tutmak gerekir.
- **D) Yeniden tasarla:** "Yuttuğun polis X saniye boyunca etrafındakileri korkutur" gibi bambaşka bir etki.
- **Benim önerim:** A + B. En küçük değişiklik, en net "polis avla, büyü" fantezisi.

---

## 3. Evrimler (2 adet)

Evrim kartı, iki girdi skilinin ikisi de **max seviyeye** ulaşınca havuza girer. Kart ★ ve altın renkle işaretlenir. Max seviyesi 1'dir: bir kez seçilince havuzdan çıkar. Ağırlık 10.

### ★ Kara Delik
- **Gereken:** Vakum 8 + Sindirim 8 (toplam 16 seçim)
- **Ne yapar:** Blob'un çevresindeki, **yiyebildiğin tier'daki her şey** temas gerekmeden otomatik yutulur: consumable, yaya, şehir objesi. Hazard'ları yemez.
- **Yarıçap:** gövde yarıçapının 2 katı; blob büyüdükçe büyür. 0.1 sn'de bir tarar.
- **Kod:** `KaraDelikEffect` → `BlackHoleComponent`

| Blob | Gövde yarıçapı | Kara Delik yarıçapı |
|---|---|---|
| Medium | ~1.0 | ~2.0 |
| Giant | ~2.0 | ~4.0 |

**Örnek:** Giant blob'la bir sokakta ilerlerken, gövdenin 2 birim dışındaki her şey (Giant tier binalar dahil) dokunmadan yutulur. Vakum hâlâ çalışır ve daha uzaktakileri çekip Kara Delik'in içine getirir.
**Not:** Polisleri yutmaz; onlar için temas ya da Avcı Formu gerekir.

---

### ★ Avcı Formu
- **Gereken:** Yırtıcı Çene 2 + Silah Gücü 8 (toplam 10 seçim)
- **Ne yapar:** Silahınla vurduğun düşman **3 saniye boyunca, tier'a bakmadan** yutulabilir olur. **Elitler de dahil.** Miniboss ve final boss hariç (onlar zaten yutulamaz; final boss kendi son fazında yutulur).
- **Kod:** `AvciFormuEffect` → `AvciFormuComponent` → `EnemyBase.MarkTemporarilyConsumable`

**Örnek:** Small blob'sun, bir elit polis sana ateş ediyor. Silahın ona bir kez isabet ettiği anda 3 saniyelik pencere açılır. O sürede ona dokunursan yutarsın: 20 mass, 5-10 coin ve sandık. Normalde bunun için Giant (1000 mass) olman gerekirdi.
**Not:** Elit "yutulduğunda" ısırmaz (elit ısırık hasarı 0).

---

## 4. Karakterler (3 adet)

Üç karakter de aynı blob'dur; farkları **başlangıç silahı** ve **pasif yetenektir**. Temel silah değerleri üçünde aynı: saniyede 2.5 atış × 4 hasar, menzil 8. Silah Gücü yolu (Bölüm 2) üçünde de aynı ilerler, sadece **Sv. 8 son formu** karaktere özeldir.

| | 🟠 Topik | 🧲 Mıknato | 🔫 Mermo |
|---|---|---|---|
| **Kilit** | Başlangıçta açık | Market'te **500 kredi** | Kodda açık (GDD: "3 oturum tamamla", bkz. Bölüm 6) |
| **Silah** | Top: yay çizerek uçar, çarptığı yerde patlar | Metal bilye: güdümlü, hedefe kıvrılır | Tabanca: düz ve en hızlı mermi |
| **Mermi hızı / ömrü** | 14 / 0.9 sn | 12 / 1.5 sn | 18 / 0.7 sn |
| **Silah özelliği** | 2 birim yarıçapta **alan hasarı** (vurulan dışındakilere de tam hasar) | **Iskalamaz** (sürekli hedefe döner) | Bir boy büyük objeyi **parçalar** (pasif) |
| **Pasif** | **+%25 hız** ve büyüdükçe ~%10 daha az yavaşlama | **Vakum baştan açık** (3 menzil) + çekim etkisi **×1.25** | Mermileri, blob'un **tam 1 tier üstündeki** objeyi 3 yenebilir parçaya böler |
| **Sv. 8 son form** | **Büyük Patlama:** alan 2.5 kat (5 birim) | **Sekme:** isabetten sonra 6 birim içindeki 2 düşmana daha seker | **Pompalı:** 40°'lik yelpazede 5 saçma (her biri %60 hasar) |
| **Oyun tarzı** | Hızlı, hantallaşmayan; kalabalığı patlamayla dağıtır | Toplayıcı; yemi kendine çeker, hızlı büyür | Fırsatçı; büyük lokmaları bir boy erken yer |

### 🟠 Topik
- **Pasif:** Hız çarpanı +0.25 (Hız Artışı skiliyle toplanır: Hız 8 ile ×2.05). Ayrıca büyüme yavaşlaması üssü 0.30 → 0.27. Giant'ta bile çevik kalır.
- **Silah:** Top yay çizerek uçar (0.4 birim yükseklik), ilk düşmana çarpınca patlar ve 2 birim içindeki diğer düşmanlara da tam hasar verir. Kalabalığa en etkili silah.
- **Örnek:** Sv. 7'de 3 hedefe ateş eden, her biri 2 birimlik patlama yapan Topik, sürüyü saniyeler içinde eritir. Sv. 8'de patlama 5 birim olur.
- **Zayıf yanı:** Top binaya çarpınca durur; patlama ince bir duvarın arkasına sızabilir (bilinçli kabul edildi).

### 🧲 Mıknato
- **Pasif:** Oyuna Vakum skili açık başlar (3 menzil, gövde yüzeyinden). Çekim menzili ve çekme hızı ×1.25. Vakum skilini alırsa çarpan onu da büyütür (Vakum 8 → 8.75 menzil).
- **Silah:** Metal bilye hedefe sürekli kıvrılır, pratikte ıskalamaz. Hedef ölürse düz gider.
- **Örnek:** Diğer karakterler Vakum'u kart olarak beklerken Mıknato ilk saniyeden çevresini toplar. Erken büyüme ve erken level demek. Kara Delik evrimine (Vakum 8 + Sindirim 8) en doğal giden karakter.
- **Not:** En güçlü pasif. Bu yüzden 500 kredilik Market kilidi var.

### 🔫 Mermo
- **Pasif:** Mermisi blob'un **tam bir boy büyüğü** olan objeye (şehir binaları dahil) çarparsa onu 3 parçaya böler. Parçalar blob'un yiyebileceği boyda ve toplamda objenin mass'ı kadar verir. Daha büyük objelerde mermi durur; dev binaları silemez. Yayalar ve hazard'lar parçalanmaz.
- **Silah:** En hızlı, düz giden mermi. Nişan, polisin hareketine göre önden kesilir.
- **Örnek:** Medium blob, Large bir binaya (25 mass) ateş eder → 3 × 8.3 mass'lık parça. Normalde Large olmadan yenemeyeceği lokmayı erken yer.
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
| **Yutulma** | Medium blob (Yırtıcı Çene ile daha erken) | Giant blob ya da Avcı Formu |
| **Davranış** | Blob onu yiyebiliyorsa **kaçar** | Kaçmaz |
| **Ödül** | 3 mass, 1 coin, %5 kalp | 20 mass, 5–10 coin, **sandık**, %100 kalp |

### 👹 Miniboss: "Polis Tankı"
Aynı tasarım, iki kez gelir, ikincisi daha güçlü. Ayrı bir davranış kodu yok; elit makinesini kullanır (yol bulma + yakın saldırı serisi).

| | 1. Miniboss | 2. Miniboss |
|---|---|---|
| **Geliş** | **6. dk** | **8. dk** (ilki hâlâ yaşıyorsa o ölene kadar bekler) |
| **Can** | 300 | 600 |
| **Saldırı** | 3 vuruşluk seri × 15 = **45 hasar**, 2.5 sn'de bir, menzil 3 | 4 vuruşluk seri × 22 = **88 hasar**, 2.2 sn'de bir, menzil 3.5 |
| **Hız** | 1.8 (blob'dan yavaş, kaçılabilir) | 2.0 |
| **Algı** | Tüm harita: oyuncuyu her yerden bulur | Tüm harita |
| **Yutulma** | **Hiçbir şekilde yutulamaz**, silahla ölür | Aynı |
| **Ödül** | 150 skor + 5–10 coin + sandık + **15–25 bonus coin** | 250 skor + 5–10 coin + sandık + **25–40 bonus coin** |

- **Silahla öldürme süresi:** 1. Miniboss: temel silahla ~30 sn, Silah Gücü 8 ile ~10 sn. 2. Miniboss: ~60 sn / ~20 sn.
- **Ekranda:** Boss can barı HUD'da görünür.
- **Görsel:** Polis tankı modeli. Saldırırken ileri atılır (hamle animasyonu).

### 💀 Final Boss
GDD Karar 1+8: **"Vurarak son faza getirirsin, yutarak bitirirsin."**

| | Final Boss |
|---|---|
| **Geliş** | **12. dk** (bir miniboss yaşıyorsa o ölene kadar bekler) |
| **Can** | 3000 |
| **Saldırı** | 5 vuruşluk seri × 28 = **140 hasar**, 2 sn'de bir, menzil 4 |
| **Hız** | 2.2 |
| **Algı** | Tüm harita |
| **Faz 1** | Silahla vurulur ama **silahla ölmez**: can 1'in altına inmez |
| **Faz 2 ("yenebilir")** | Can **%25'in altına** (750) inince yutulabilir hale gelir. Blob **Giant** olmalı (ya da Avcı Formu, bkz. Bölüm 6) |
| **Ödül** | Yutulunca **run "tamamlandı" olarak biter** (ölümden ayrı ekran), 300 mass, 600 skor, **60–100 bonus coin**, tam kredi (ölümde %50) |

- **Faz 2'ye indirme süresi:** 2250 hasar gerekir. Temel silahla ~225 sn (3.75 dk), Silah Gücü 8 ile ~75 sn (tek hedef).
- **Zaman sınırı:** Final boss yutulamazsa run **15. dk'da** "hayatta kaldın" olarak biter (güvenlik eşiği).

### Zorluk zaman çizelgesi
| Dakika | Olay |
|---|---|
| 0–1 | Sadece yenebilir objeler, yayalar, arabalar |
| 1 | Polisler gelmeye başlar (aynı anda en fazla 30 düşman) |
| **Medium** (en geç 4) | Elit polis dalgası (aynı anda en fazla 10 elit, toplam 30'un içinde) |
| 5 | Düşman hasarı ×1.2 |
| 6 | 1. Miniboss |
| 8 | 2. Miniboss |
| 10 | Düşman yoğunluğu ×2 (aynı anda en fazla 60) |
| 12 | Final Boss |
| 15 | Run biter |

---

## 6. Açık sorular / ileride bakılacaklar

**Skiller**
1. **Yırtıcı Çene'nin değeri** (bkz. yukarıdaki A–D).
2. ~~Vakum yarıçapı blob boyutuyla ölçeklenmeli mi?~~ ✅ 2026-11-03: menzil artık gövde yüzeyinden ölçülüyor.
3. ~~Mıknato pasifi hissedilmiyor~~ ✅ 2026-11-03: ×1.25 menzil ve çekme hızı (GDD Karar 9).
4. **Yutuş, Rejenerasyon'dan çok güçlü**, sürekli yiyen oyuncu için 4 katı iyileşme veriyor.
5. **Araba yemek Yutuş'u tetiklemiyor**: araba kendi çarpışma yolunu kullanıyor, bilinçli mi?
6. **Kara Delik'e ulaşmak 16 seçim**: 15 dakikalık run'da ortalama kaç level atlandığına bakılıp gerçekçi mi değerlendirilmeli.
7. Tüm skill ikonları hâlâ boş (sanat TODO'su).

**Karakterler**
8. **Mermo'nun kilidi yok:** GDD "toplam 3 oturum tamamla" diyor, kodda başlangıçta açık.
9. Karakter ikonları boş (#47).

**Bosslar**
10. **Final boss tek seride öldürüyor:** 140 hasar, başlangıç canı 100. Zırh ve Can Artışı almamış oyuncu tek saldırıda ölür. Bilinçli mi?
11. **Final boss'a yetişmek zor olabilir:** 12. dk'da geliyor, run 15. dk'da bitiyor. Silah Gücü almamış oyuncu faz 2'ye ~3.75 dk'da iner, yani yetişemez. Ya boss daha erken gelmeli, ya canı düşmeli, ya da süre uzamalı.
12. **Boss hasarı zamanla artmıyor:** Polislere uygulanan ×1.2 / ×2 zorluk çarpanları bosslara uygulanmıyor (değerler zaten sabit yüksek). Bilinçli mi?
13. **Bosslar sandık da düşürüyor:** Elit sayıldıkları için ölünce bonus coin'in yanında bir sandık (ekstra skill seçimi) ve 5–10 coin daha veriyorlar.
14. **Avcı Formu final boss'u erken yutturabilir:** Avcı Formu'nun "vurulan düşman 3 sn tier'a bakmadan yenir" kuralı, faz 2'deki final boss'a da işliyor; Giant olmadan bitirilebilir. Ödül mü, açık mı?
15. **Miniboss'u ayırt eden bir mekanik yok:** GDD alan saldırısı öngörüyor, şu an elit gibi yakın dövüş serisi (#58).
16. **Düşman tavanları belgelerle uyuşmuyor:** Sahnede aynı anda 30 düşman / 10 elit var (10. dk'dan sonra 60). GDD ve CLAUDE.md 15 polis / 4 elit diyor (#50). Hangisi geçerli?
