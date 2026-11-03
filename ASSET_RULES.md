# Left No Crumbs: 3B Asset Üretim Kuralları (AI için)

> Bu dosya bir AI asistanına (Claude vb.) bağlam olarak verilir. Görevi, Meshy AI için tutarlı prompt'lar yazmak ve üretilen modelleri bu kurallara göre kontrol etmektir.
> Son güncelleme: 2026-11-03. Kaynak: Unity projesindeki mevcut şehir kiti ve çarpışma/import pipeline'ı.

---

## 0. Görevin

Kullanıcı bir asset ister ("çöp kutusu", "kafe binası" gibi). Sen:
1. Aşağıdaki kurallara uyan bir **Meshy prompt'u** ve **negatif prompt** yazarsın (İngilizce).
2. Hedef ölçü, üçgen sayısı, doku boyutu ve dosya adını verirsin.
3. Kurallarla çelişen bir istek varsa (yerde geniş kaide, cam duvar, aşırı detay gibi) uyarırsın ve alternatif önerirsin.
4. Kullanıcı üretilen modelin görselini ya da bilgisini paylaşırsa **Bölüm 7'deki kontrol listesiyle** denetlersin.

Çıktı formatı her asset için **Bölüm 8**'deki gibidir.

---

## 1. Oyun bağlamı

- **Oyun:** "Left No Crumbs". Mobil (iOS/Android), top-down 2.5D, stilize low-poly 3B.
- **Oynanış:** Oyuncu yerde yuvarlanan bir **blob**'dur. Kendinden küçük her şeyi yiyerek büyür (Katamari / Hole.io benzeri). Şehirdeki her obje ileride yenebilir bir lokmadır.
- **Kamera:** Yukarıdan, 60° eğik bakar. Oyuncu objelerin en çok **üst yüzeyini ve ön cephesini** görür.
- **Harita:** ~136×136 birimlik bir şehir. 8×8 birimlik bloklar, aralarında 4 birim genişliğinde sokaklar.
- **Birim:** 1 birim ≈ 1 metre gibi düşün. Blob en küçükken 0.5 birim çapında, en büyükken ~4 birim.
- **Performans:** Hedef 30 FPS, orta seviye telefonlar. Her model hafif olmalı.

---

## 2. Çarpışma kuralı (EN ÖNEMLİ)

Unity'de çarpışma kutusu otomatik hesaplanır: **modelin alttan 0.6 birimlik kısmının yerdeki izi** engeldir. Daha yüksekteki her şeyin altından blob geçebilir.

Bu yüzden:
- ✅ Çıkıntılar **havada** olsun: ağaç yaprakları, tente, lamba kolu, durak çatısı, balkon. En alt noktaları **0.8 birimin üstünde** başlasın.
- ✅ Yerdeki kısım üstten bakınca da görünsün ve objenin "katı" kısmı olsun: ağaç gövdesi, bina duvarı, direk.
- ❌ Yere yakın geniş ama ince ya da üstten görünmeyen parçalar: ağaç kökleri, geniş kaide, etek, zemin plakası, çim tabanı.
- ❌ Yerde **şeffaf** parçalar (cam duvar, cam vitrin tabanı): blob görünmez bir duvara çarpmış gibi hisseder.
- ❌ Yere yayılan ince detaylar (kablolar, zincirler, ayaklık).

---

## 3. Teknik kurallar

| Konu | Kural |
|---|---|
| Pivot / orijin | Tabanın tam ortası. Model y=0'da yere oturur |
| Eksen | Y yukarı |
| Yön | Binaların girişi, araçların önü, insanların yüzü **hep aynı yöne** (kameraya bakan ön cephe). Hangi yön olduğu bir kez belirlenir, hepsinde aynı kalır |
| Dosya | Her FBX'te **tek obje**. Zemin, plaka, arka plan, sahne yok |
| Format | FBX, tek mesh tercih edilir |
| Doku | Sadece **base color** (albedo). Normal, roughness, metallic, AO haritası **yok** |
| Doku boyutu | Sokak eşyası / insan: 512 px · Araç: 512–1024 px · Bina: 1024 px |
| Renk | Az sayıda düz renk, gradyan ya da gürültü yok. Böylece ileride tek bir renk atlasına taşınabilir |
| Animasyon | Yok. İnsanlar da animasyonsuz (kodla yukarı-aşağı sallanıyorlar) |

### Üçgen bütçesi (Remesh hedefi)
| Tür | Üçgen |
|---|---|
| Küçük sokak eşyası (musluk, çöp kutusu, bank) | 200–500 |
| Büyük sokak eşyası (ağaç, durak, lamba) | 400–800 |
| Araç | 800–1500 |
| İnsan / polis | 1000–2000 |
| Bina | 1500–3000 |

⚠️ Meshy'nin "low poly" çıktısı bile çok ağır olabiliyor (önceki bir binada 210.000 üçgen). Her modelde **Remesh (triangle topology)** ile yukarıdaki hedefe indirilmeli.

---

## 4. Dosya adlandırma

| Önek | Tür | Örnek |
|---|---|---|
| `B_` | Bina | `B_Bakery` |
| `P_` | Sokak eşyası (prop) | `P_Mailbox` |
| `V_` | Araç | `V_IceCreamTruck` |
| `H_` | İnsan / polis | `H_OldLady` |
| `C_` | Toplanabilir (coin, kalp) | `C_Gem` |

PascalCase, Türkçe karakter yok, boşluk yok.

---

## 5. Hedef ölçüler (en × yükseklik × derinlik, birim)

Mevcut şehirdeki karşılıkları. Yeni asset'ler bu aralıklarda kalmalı.

**Binalar** (taban **en fazla 6 × 6**, çünkü blok 8×8 ve kaldırım payı var)
| Örnek | Ölçü |
|---|---|
| Küçük dükkan / kafe | 3.6–3.8 × 3.1 × 3.65 |
| Polis merkezi | 3.8 × 5.0 × 3.9 |
| Restoran | 5.4 × 4.7 × 4.4 |
| Banka | 5.4 × 6.0 × 5.2 |
| Apartman | 3.6–4.0 × 6.2–6.7 × 3.6 |
| Otel / ofis | 4.8–5.0 × 7.8–9.0 × 4.8–4.9 |
| Kule | 3.6 × 10.9 × 3.6 |
| Süpermarket | 7.0 × 3.4 × 5.1 (en geniş) |

**Sokak eşyaları**
| Obje | Ölçü |
|---|---|
| Ağaç | 1.8 × 2.3 × 1.3 · gövde ~0.25 kalınlık, yapraklar 0.8 üstünde başlar |
| Sokak lambası | direk ~0.1 · boy 2.3 · kol tepede |
| Trafik lambası | 0.2 × 2.05 × 0.16 |
| Otobüs durağı | 2.0 × 1.6 × 0.8 · çatı tepede, yerde sadece direkler ve bank |
| Bank | 1.0 × 0.55 × 0.3 |
| Çöp kutusu | 0.45 × 0.56 × 0.4 |
| Yangın musluğu | 0.2 × 0.5 × 0.2 |

**Araçlar**
| Obje | Ölçü |
|---|---|
| Otomobil / taksi / polis arabası | 1.0 × 0.9–1.0 × 2.1 |
| Minibüs | 1.15 × 1.45 × 2.7 |
| Kamyon | 1.2 × 1.85 × 3.3 |
| Otobüs | 1.3 × 1.55 × 4.6 |

**İnsanlar:** ~1.1 boy, tıknaz, sevimli oranlar (büyük kafa, kısa bacak).

---

## 6. Görsel stil

- **Stilize low-poly**, oyuncak gibi, yuvarlatılmış tıknaz formlar. Gerçekçi değil.
- **Düz renkler**, yumuşak pastel ağırlıklı şehir paleti: lavanta/mor asfalt, krem kaldırımlar; binalarda kırmızı, teal, mavi, sarı vurgular.
- **Okunabilir silüet:** Obje yukarıdan bir bakışta tanınmalı. Detay çatıya ve ön cepheye.
- **Boyut = tehlike/ödül:** Oyuncu objenin ne kadar büyük olduğunu, yani ne zaman yiyebileceğini, siluetinden anlamalı. Aynı kategorideki objeler benzer boyutta kalmalı.
- Yazı, logo, marka yok (tabelalar düz renk ya da basit ikon).

### Tutarlılık stratejisi
1. Önce tek bir "referans" asset üret, beğenilene kadar düzelt.
2. Sonraki asset'lerde Meshy'nin **image-to-3D** modunu bu referansın görüntüsüyle kullan, ya da prompt'a hep aynı stil cümlesini ekle (Bölüm 8).
3. Yalnızca metinle üretim her seferinde farklı bir tarz çıkarabilir.

---

## 7. Kontrol listesi (üretilen modeli denetlerken)

- [ ] Tek obje, zemin/plaka/kaide yok
- [ ] Pivot tabanın ortasında, model yere oturuyor
- [ ] Ön cephe belirlenen yöne bakıyor
- [ ] Ölçü Bölüm 5'teki aralıkta (gerekirse Unity'de ölçeklenir ama oranlar doğru olmalı)
- [ ] Alttan 0.6 birimlik kısım = gerçekten katı ve görünür kısım (Bölüm 2)
- [ ] Yerde şeffaf parça yok
- [ ] Üçgen sayısı bütçede (Remesh yapıldı)
- [ ] Sadece base color dokusu, boyutu doğru
- [ ] Renkler düz, az sayıda
- [ ] Dosya adı doğru önekle

---

## 8. Çıktı formatı (her asset için)

```
### <Dosya adı>   (örn. P_Mailbox)
Tür: Sokak eşyası · Hedef ölçü: 0.4 × 1.1 × 0.35 · Üçgen: 200–400 · Doku: 512 px

Meshy prompt:
low poly stylized <obje ve 3-6 kelimelik tarif>, mobile game asset, chunky simple toy-like shapes,
flat pastel colors, clean silhouette readable from a top-down camera, single isolated object,
centered, standing upright, front facing, no ground, no base plate, no background

Negatif prompt:
ground plane, pedestal, base, platform, grass base, roots, text, logo, realistic, photorealistic,
high detail, noise, gradient texture, multiple objects, transparent parts

Ayarlar: Remesh → triangle, hedef <üçgen>; PBR haritaları kapalı; simetri açık (binalar, araçlar)

Çarpışma notu: <yerde ne katı, havada ne var; Bölüm 2'ye uyuyor mu>
```

### Örnekler

```
### P_TreeRound
Tür: Sokak eşyası · Hedef ölçü: 1.8 × 2.3 × 1.3 · Üçgen: 400–700 · Doku: 512 px

Meshy prompt:
low poly stylized round tree, thin straight brown trunk, fluffy rounded green canopy starting high
above the ground, mobile game asset, chunky simple toy-like shapes, flat pastel colors, clean
silhouette readable from a top-down camera, single isolated object, centered, standing upright,
no ground, no base plate, no background

Negatif prompt: (standart) + exposed roots, grass patch, flower bed

Çarpışma notu: Yerde sadece gövde (~0.25) katı; yapraklar 0.8 üstünde başlamalı, altından geçilebilir.
```

```
### B_Bakery
Tür: Bina · Hedef ölçü: 4.0 × 3.5 × 3.8 · Üçgen: 1500–2500 · Doku: 1024 px

Meshy prompt:
low poly stylized small bakery shop building, two floors, cream walls, red striped awning above the
door, big front window, simple flat roof with a chimney, mobile game asset, chunky simple toy-like
shapes, flat pastel colors, clean silhouette readable from a top-down camera, single isolated
building, centered, front facing, no ground, no sidewalk, no base plate, no background

Negatif prompt: (standart) + sidewalk, street, people, signs with text

Çarpışma notu: Duvarlar katı; tente kapı üstünde (0.8+ yükseklikte) olmalı. Vitrin camı yerden yükseltilmiş, altı duvar.
```

---

## 9. Unity tarafı (bilgi için)

- Modeller `Assets/_Project/Art/City/<DosyaAdı>.fbx` altına konur. Geliştirici import ayarlarını (doku sıkıştırma, ölçek, yön) ve prefab'ı pipeline ile otomatik kurar.
- Çarpışma kutusu, Bölüm 2'deki kurala göre otomatik hesaplanır; elle collider gerekmez.
- Yön veya ölçek tutarsızsa düzeltilebilir, ama **bütün set içinde tutarlı** olması gerekir.
