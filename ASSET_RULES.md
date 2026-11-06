# Left No Crumbs: 3B Asset Üretim Kuralları

> Bütün 3B modeller **Blender script'leriyle** üretilir (`~/Developer/zorse-blender/build_*.py`, Claude yazar). Elle modelleme ya da dış araç (Meshy vb.) yok.
> Bu dosya yeni bir model yazarken uyulacak kuralları anlatır. Ortak kütüphane `kit2.py`; kurallar oradaki kontrollerle (`collision_report`) otomatik denetlenir.

---

## 1. Akış

1. İstenen obje için ilgili script'e bir `def <obje>()` fonksiyonu eklenir (eşya/zemin `build_props2.py`, bina `build_kit2.py`, insan `build_people.py`, yerdeki yiyecek `build_litter.py`, karakter `build_blobs.py`, tekil modeller kendi script'inde: `build_chest.py`, `build_police_heli.py`, `build_armor_shields.py`, `build_bullets.py`).
2. Önce **önizleme render'ı** alınır (oyun açısı 60° + yakın plan) ve görsel kontrol edilir. Script'i tek eşyayla çalıştırmak `props2.blend`'i o eşyayla ezer; tek modelin önizlemesi için geçici bir script kullanılır.
3. Beğenilince `--export` ile FBX `Assets/_Project/Art/City/...` altına yazılır.
4. Şehirde yer alacaksa yerleşim `build_city2.py`'a eklenir → `city_v2.json`. **Rastgele sayı tüketmeden** ekle (sabit konum, sayaç); aksi hâlde seed kayar ve bütün şehir değişir.
5. Unity: `Tools → Blob.io → Şehir Kur (v2)` → `Şehir Görsel Düzeltmeleri` → Cmd+S. Tier ve kütle boydan otomatik gelir (Bölüm 5).
6. Unity'de görsel kontrol: `Temp/snapshot.request` ile sahne görüntüsü (bkz. CLAUDE.md).

Çalıştırma: `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python build_X.py -- [--export]`

---

## 2. Çarpışma kuralı (EN ÖNEMLİ)

Unity'de çarpışma kutusu otomatik hesaplanır: **modelin alttan 0.6 birimlik kısmının yerdeki izi** engeldir. Daha yüksekteki her şeyin altından blob geçebilir.

- ✅ Çıkıntılar **havada**: ağaç yaprakları, tente, lamba kolu, saçak. En alt noktaları **0.8'in üstünde** (ağaç yaprakları 0.85+). Yemek için çıkıntıya değmek yeter: Unity görünür boy kadar tetikleyici yeme alanı (`EatZone`) ekler.
- ✅ Yerdeki kısım objenin görünür, katı kısmı olsun: gövde, duvar, direk.
- ❌ Yere yakın geniş ama ince parçalar (kök, kaide, etek, zemin plakası). Kaide gövdeden taşarsa çarpışma kutusu büyür.
- ❌ Yerde şeffaf parça (cam duvar): görünmez duvar hissi verir.
- `kit2.collision_report` 0.6–0.8 arasında ayak izinden sarkan parçayı ve bütçe aşımını uyarır; uyarı kalmamalı.

---

## 3. Teknik kurallar

| Konu | Kural |
|---|---|
| Birim | 1 birim = Unity birimi (≈ 1 m). Blender z yukarı |
| Pivot | Tabanın tam ortası, z=0 (istisna: havada dönen sandık = merkez; karakter = küre merkezi) |
| Yön | Şehir kiti (B_/P_/L_/V_): ön cephe **-Y**; Unity'de 180° düzeltilir, Blender (x, y) = Unity (x, z). İnsan/karakter: yüz +Y (kendi script notlarına bak) |
| Mesh | Her obje **tek mesh**, transform uygulanmış (`M` sınıfı doğrudan dünya koordinatında kurar). Ayrı dönen parça gerekiyorsa (helikopter rotoru) ayrı mesh, pivot = göbek |
| Desen dokusu | Tekrar eden zemin desenleri (kaldırım karosu, su) palet dışı: `build_textures.py` (Python/PIL) → `Art/City/Textures/` (Repeat + mipmap ön işlemcide) |
| Doku | **Tek palet dokusu** `CityPaletteV2.png` (8×8 = 64 renk, **dolu**). Yeni renk eklemek için önce bir rengi değiştir; satır/sütun sayısı değişirse bütün UV'ler kayar. Renkler `kit2.PAL` adlarıyla kullanılır |
| Malzeme | Normal/roughness/metallic haritası yok, tek materyal (şehirle aynı batch'e girer) |
| Animasyon | Yok. Hareket hissi koddan (yaya süzülme, rotor dönüşü) |

### Üçgen bütçesi (`collision_report` denetler)
| Tür | Üst sınır | Örnek |
|---|---|---|
| Sokak eşyası `P_` | 800 | dondurma kamyoneti 326, bank ~150 |
| Araç `V_` | 1500 | |
| İnsan / polis `H_` | 2000 | |
| Bina `B_` | 3000 | |

---

## 4. Dosya adlandırma

| Önek | Tür | Örnek |
|---|---|---|
| `B_` | Bina | `B_Bakery` |
| `P_` | Sabit sokak eşyası (park etmiş araç dahil) | `P_IceCreamTruck`, `P_ParkedCarRed` |
| `L_` | Blok zemini (8×8, çarpışma yok) | `L_Park` |
| `V_` | Hareketli araç / boss aracı | `V_PoliceHeli` |
| `H_` | İnsan / polis | `H_Man` |
| `J_` | Yerdeki yiyecek (Tiny) | `J_Simit` |
| `CH_` | Karakter görünümü | `CH_Topik_T3` |
| `C_` | Toplanabilir (sandık, coin) | `C_Chest` |
| `W_` | Yenmeyen çevre (sur, kule; elle yerleştirilir) | `W_SeaWall` |
| `X_` | Oyuncu mermisi (pivot merkez, burun +Y) | `X_BulletMermo` |

PascalCase, Türkçe karakter ve boşluk yok. Eski kitle aynı adı kullanma: `CityVisualFixes` prefab'ları adla eşleştirir.

---

## 5. Boy = tier (yenme sırası)

Tier elle verilmez, Unity boydan hesaplar (`Editor/TierRules.cs`): obje, blob o tier'a girdiğindeki çapından en fazla %5 büyükse o tier'da yenir. Boy = kenarların geometrik ortalaması; ince uzun objelerde en uzun kenarın yarısı daha büyükse o. Yeni objenin hangi tier'a düşeceğini önceden bilmek için hedef boyu buna göre seç.

| Tier | Blob çapı | Boy (≈) | Mevcut örnekler |
|---|---|---|---|
| Tiny | 0.5 | ≤ 0.52 | yerdeki yiyecek 0.27–0.49, hidrant 0.40, çöp kutusu 0.50 |
| Little | 0.76 | ≤ 0.80 | zayıf yaya 0.69–0.77, bank 0.65, çalı 0.64 |
| Small | 1.1 | ≤ 1.18 | iri yaya 1.01, telefon kulübesi 0.99, seyyar araba 1.17, trafik ışığı 1.10 |
| Medium | 2.0 | ≤ 2.07 | ağaç 1.72, araba 1.21, lamba 1.33, kamyon 1.94, dondurma kamyoneti 1.93 |
| Large | 3.3 | ≤ 3.45 | otobüs 2.32, fıskiye 2.49, berber 3.42, atlıkarınca 3.11 |
| Giant | 4.1 | ≤ 4.36 | kafe 3.99, kitapçı 4.06, apartmanlar 4.16–4.33 |
| Dev | 5.0 | ≤ 5.45 | büyük apartmanlar 4.5, hastane 4.87, sinema 5.16, otel 5.20 |

Ölçüleri görmek için `Tools → Blob.io → Yenebilir Boyut Raporu` (`Temp/tier_sizes.txt`). Bina tabanı en fazla 6×6 (blok 8×8, kaldırım payı).

---

## 6. Görsel stil

- **Stilize low-poly**, oyuncak gibi, tıknaz formlar; düz renkler, pastel ağırlıklı palet.
- **Okunabilir silüet:** 60° tepeden bir bakışta tanınmalı; detay çatıya ve ön cepheye.
- **Boy = tehlike/ödül:** oyuncu ne zaman yiyebileceğini silüetten anlamalı; aynı kategori benzer boyda.
- Yazı, logo, marka yok.
- Çocuk yaya yok (OYUN.md İlkeler).

---

## 7. Kontrol listesi

- [ ] `collision_report` uyarısız (sarkan parça yok, bütçe içinde)
- [ ] Pivot taban ortası, model z=0'a oturuyor
- [ ] Ön cephe doğru yöne bakıyor (önizlemede ve Unity görüntüsünde)
- [ ] Sadece palet renkleri, yeni renk yok
- [ ] Boy beklenen tier'a düşüyor (Bölüm 5)
- [ ] Şehre eklendiyse yerleşim rastgele sayı tüketmiyor, `city_v2.json` farkı sadece yeni obje
- [ ] Unity'de diğer objelerle iç içe geçmiyor (ağaç yaprağı, bina)

---

## 8. Unity tarafı (bilgi için)

- Ön ekler davranışı belirler: `B_` herkesi engeller (Environment engeli), `P_` yaya/polisi engeller, tank ezer; `L_` zemin.
- Şehir objeleri static batching'de: oyun sırasında taşınamaz (görüntü kalır, collider kayar). Hareket etmesi gereken obje şehir kiti dışında ayrı prefab olur (hareketli arabalar gibi).
- Saydam/özel materyaller build'de ayıklanmasın diye prefab'da referanslı ya da `Resources/` altında olmalı.
