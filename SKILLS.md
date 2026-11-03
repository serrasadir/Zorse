# Left No Crumbs: Skill ve Evrim Rehberi

> Son güncelleme: 2026-11-03. Değerler koddan ve asset'lerden okundu (`Assets/_Project/Data/Upgrades/`).
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

## 4. Karakter pasifleriyle etkileşim

| Karakter | Pasif | İlgili skill |
|---|---|---|
| Topik | **+%25 hız** ve büyüdükçe ~%10 daha az yavaşlar (üs 0.3 → 0.27) | Hız Artışı ile toplanır (Hız 8 → ×2.05). "Hantallaşmayan" karakter |
| Mıknato | Başlangıçta Vakum açık (3 menzil) + çekme etkisi **×1.25** (menzil ve çekme hızı) | Vakum seçimleri bunun üstüne eklenir ve çarpandan yararlanır (Vakum 8 → 8.75 menzil) |
| Mermo | Mermileri, blob'un **tam 1 tier üstündeki** objeleri (şehir objeleri dahil) 3 parçaya böler. Parçaların toplam mass'ı objeninki kadar ve hemen yenebilir. Daha büyükleri delip geçer (tabanca dev binayı silemez). Yayalar ve hazard'lar parçalanmaz | Sindirim parçaları da çarpar. "Bir boy büyük lokmayı erken ye" karakteri. Örnek: Medium blob, Large bina (25 mass) → 3 × 8.3 mass |

---

## 5. Açık sorular / ileride bakılacaklar

1. **Yırtıcı Çene'nin değeri** (bkz. yukarıdaki A–D).
2. ~~Vakum yarıçapı blob boyutuyla ölçeklenmeli mi?~~ ✅ 2026-11-03: menzil artık gövde yüzeyinden ölçülüyor.
3. ~~Mıknato pasifi hissedilmiyor~~ ✅ 2026-11-03: ×1.25 menzil ve çekme hızı (GDD Karar 9).
4. **Yutuş, Rejenerasyon'dan çok güçlü**, sürekli yiyen oyuncu için 4 katı iyileşme veriyor.
5. **Araba yemek Yutuş'u tetiklemiyor**: araba kendi çarpışma yolunu kullanıyor, bilinçli mi?
6. **Kara Delik'e ulaşmak 16 seçim**: 15 dakikalık run'da ortalama kaç level atlandığına bakılıp gerçekçi mi değerlendirilmeli.
7. Tüm skill ikonları hâlâ boş (sanat TODO'su).
