# Erişilebilirlik Gereksinimleri — Left No Crumbs

> **Durum**: Taahhüt edildi (Committed)
> **Yazan**: Bahar + ux-designer
> **Son güncelleme**: 2026-11-15
> **Seviye**: **Temel** (Basic) — Bahar onayı 2026-11-15
> **Platform**: iOS, Android (dikey telefon, dokunmatik). PC/Steam portu ileride (OYUN.md §12), o zaman bu belge yeniden ele alınır.
> **Dış standartlar**: WCAG 2.1 AA (yalnız metin kontrastı ve renk kuralı için referans) · Apple HIG / Material dokunma hedefi önerileri · Game Accessibility Guidelines (temel maddeler)
> **Danışman**: Yok
> **Bağlı belgeler**: `design/ux/interaction-patterns.md`, `design/ux/hud.md`, `design/ux/seviye-atlama-kartlari.md`

Bu belge proje geneli taahhüttür. Ekran özel notları UX spec'lerinde. Bir özellik buradaki bir maddeyle çelişirse özellik değişir, madde değil (Bahar onayıyla revizyon hariç).

**Ölçü birimi:** Tüm UI 1080x1920 referans çözünürlükte, genişliğe eşlenmiş (match=0) çiziliyor. Tipik telefon ~390 pt genişlik: **1 referans px ≈ 0.36 pt ≈ 0.06 mm.** Aşağıdaki değerler "ref px" cinsinden.

---

## 1. Seviye tanımı ve taahhüt

**Hedef seviye: Temel.**

**Gerekçe:** Küçük ekip, tek parmakla oynanan, zaman baskılı ama refleks yükü orta bir mobil oyun. Temel seviye esasen tasarım kısıtlarıyla (boyut, kontrast, renk) karşılanabiliyor, ayrı mühendislik bütçesi istemiyor. Oyun zaten tek parmakla oynanıyor ve her an duraklatılabiliyor; motor erişilebilirliğin en pahalı kısımları (yeniden atama, iki el) doğal olarak çözülmüş durumda. Ses olmadığı için oyuncuya bilgi yalnız görsel ve haptik kanaldan gidiyor; bu yüzden "her haptik sinyalin görsel karşılığı olur" kuralı Temel seviyenin parçası sayılıyor.

**Temel seviyenin bu projedeki karşılığı:**
1. Kritik oyuncu metni okunur (§2.1).
2. Hiçbir bilgi yalnız renkle taşınmaz (§2.3).
3. Dokunma hedefleri parmakla rahat basılır (§3.1).
4. Fotosensitivite riski yok (§2.4).
5. Her haptik sinyalin görsel karşılığı var; haptik tek kanal değil (§4).
6. Ses eklendiğinde müzik ve efekt sesi ayrı ayarlanır (§4).

**Seviyenin üstünde, kapsama alınanlar:**
- **Titreşim kapatma** ve **kamera sarsıntısı kapatma** (normalde Standart). Gerekçe: titreşim her yemede çalışıyor (dakikada yüzlerce kez), bazı oyuncular için rahatsız edici; sarsıntı hareket hassasiyetinde ilk tetikleyici. İkisi de tek bir bayrakla çözülüyor, ucuz. **Önkoşul: şu an Ayarlar ekranı yok** (§6).

**Kapsam dışı (bilerek):** renk körü modları, UI/metin ölçekleme ayarı, tam hareket azaltma modu, ekran okuyucu, zorluk ayarları. Bkz. §7.

---

## 2. Görsel erişilebilirlik

### 2.1 Metin boyutu

| Madde | Kural | Öncelik | Durum |
|---|---|---|---|
| Kritik HUD/kart metni | En az **32 ref px** (~11.5 pt), kalın | P0 | İhlal var (aşağıda) |
| İkincil, bilgiyi tekrarlayan etiket | En az 28 ref px, yalnız aynı bilgi başka yolla da veriliyorsa. P0 kapsamında kabul edilen tek istisna: slot şeridi seviye hapı rakamı (K19; seviye kart rozetinde de yazılı) | P0 | — |
| Otomatik boyutlandırma | Uzun metin taşabilecek her kutuda TMP auto-size, **alt sınır yukarıdaki minimum** | P0 (P0-a) | Kapalı |

Bugünkü ihlaller (interaction-patterns K4/K11/K12): tier yazısı 24, kart tür etiketi 26, HUD coin 28, HUD seviye 30, kart rozeti 30. Düzeltme `hud.md` ve `seviye-atlama-kartlari.md`'de.

### 2.2 Kontrast

| Madde | Kural | Öncelik |
|---|---|---|
| Normal metin | Zemine karşı en az 4.5:1 (en açık şehir zemini üstünde, panel dahil ölçülür) | P0 |
| Büyük metin (≥ 64 ref px ya da ≥ 48 ref px kalın) | En az 3:1 | P0 |
| Soluk metin (beyaz alfa 0.45) | Bilgi taşıyan metinde kullanılmaz; yalnız dekoratif | P1 |
| HUD panel zemini | Gece mavisi alfa ≥ 0.55 (mevcut); metin şehrin üstüne çıplak konmaz, ya panel ya K5 gölgesi | P0 |

### 2.3 Renk tek gösterge değil — denetim

| Yer | Renk sinyali | Ne anlatıyor | Renk dışı yedek | Durum |
|---|---|---|---|---|
| Kart tür etiketi | turuncu / açık mavi / pembe | aktif / pasif / nadir | Etiket metni + **çerçeve şekli** (daire / kare / elmas) | Metin var; şekil yeni (kart spec) |
| Kart zemini (nadir, evrim) | pembe / altın ton | özel kart | NADİR etiketi, ★ ve EVRİM rozeti | Var |
| Kart rozeti | yeşil / mavi / altın | yeni / sahip / evrim | Rozet metni | Var |
| HUD skill slotu (yeni) | tür rengi | aktif / pasif | Çerçeve şekli + iki grup arası boşluk | Yeni (hud.md) |
| Can / kalkan / XP / boss barı | kırmızı / mavi / ? / ? | hangi bar | İkon (kalp, kalkan) + konum + boss barında ad | Kısmen; ikon yeni |
| Düşük can | kırmızı kenar nabzı | tehlike | Can barı yanında daire içinde "!" işareti + bir kerelik haptik (P0-d); bar nabzı P1 | Yeni (hud.md) |
| Hasar | kırmızı kenar, blob kızarması | can gitti | Can barının düşmesi + kamera sarsıntısı + haptik; hayalet bar P1 | Yeni |
| Kalkan vuruşu | mavi kenar | kalkan emdi, can gitmedi | Can barı oynamıyor; kalkan barı parlaması P1 | Yeni |
| Market fiyat hapı | altın / gri / yeşil | alınır / alınamaz / alındı | Metin (fiyat, AÇIK/ALINDI) + soluk fiyat | Var |

### 2.4 Fotosensitivite

| Madde | Kural | Öncelik |
|---|---|---|
| Tam ekran ya da geniş alan parlaması | Saniyede en fazla 3 kez (genel tavan) | P0 |
| **Kırmızı flaş dizisi** (hasar kenar uyarısı) | Doygun kırmızı geniş alan flaşı tekrar etmez: flaş yalnız ≥ 1.0 sn sakin süreden sonra (en sık 1/sn); kuşatılmada (1 sn içinde ikinci vuruş) flaş yerine sabit ton (alfa 0.35), vuruşlar bitince 0.3 sn'de söner (K21, `hud.md` §4.2) | P0 (P0-d) |
| Beyaz parlamalar (tank ölümü, evrim anı) | Gözle denetlenir, ekranın büyük kısmını kaplayan doygun beyaz flaş dizisi yok | P1 |
| Düşük can nabzı | ≤ 1.5 Hz, düşük genlik (alfa ≤ 0.2) | P0 |

### 2.5 Hareket efektleri

Mevcut hareket efektleri: kamera sarsıntısı (hasar, boss ölümü, kara delik), PunchZoom (yaya tekmesi), ağır çekim (tekme ×0.4, evrim anı ×0.25), haber bandının kayan metni, UI pop animasyonları.

| Efekt | Taahhüt | Öncelik | Not |
|---|---|---|---|
| Kamera sarsıntısı | Ayarla kapatılabilir | P1 | `CameraController` sarsıntı genliği tek çarpanla sıfırlanır |
| PunchZoom | Sarsıntı ayarına bağlanır | P2 | |
| Ağır çekim (tekme, evrim) | Bu seviyede zorunlu değil; ileride "hareket azaltma" ile kısalabilir | P2 | Oyun hissi parçası, Bahar kararı |
| UI pop/punch | Kalır (kısa, küçük ölçek) | — | |
| Haber bandı kayması | Kalır | — | Bilgi taşımıyor |

---

## 3. Motor erişilebilirlik

### 3.1 Dokunma

| Madde | Kural | Öncelik | Durum |
|---|---|---|---|
| Dokunma hedefi | En az **120x120 ref px** (~44 pt); görsel daha küçük olabilir, tıklama alanı bu kadar | P0 | Duraklat tuşu 72x72: ihlal |
| Hedefler arası boşluk | Yan yana iki buton arası ≥ 24 ref px | P1 | |
| Yıkıcı eylemler | Geri alınamaz eylem (Yeniden Başla, Ana Menü, pahalı satın alma) yanlış dokunmaya karşı korunur (onay ya da basılı tut) | P1 | Yok (K10, K6) |
| Giriş koruması | Oyun akışı içinde aniden açılan ekranlar (kart, oyun sonu) ilk 0.4 sn dokunmayı almaz | P0 | Kartta var; oyun sonu butonları P0-a işi (`hud.md` §16) |
| Tek el | Her şey tek başparmakla, dikey tutuşta | P0 | Var |
| Oyun içi HUD | Duraklat dışında hiçbir HUD öğesi dokunma almaz (`raycastTarget=false`), hareket her yerden başlar | P0 | Var |
| Çoklu dokunma | Oyunun kendisi gerektirmez (3 parmak dokunuşu yalnız geliştirici paneli) | — | Var |

### 3.2 Zamanlama

- Kart seçiminde süre sınırı yok (oyun durur). Korunur.
- Her an duraklatılabilir; uygulama arka plana gidince otomatik durur. Korunur.

---

## 4. İşitsel erişilebilirlik ve haptik

- **Oyunda ses yok.** Altyazı, ses kanalları, mono ses: şu an uygulanamaz (N/A).
- **Ses eklendiğinde (Temel gereği):** müzik ve efekt için ayrı ses ayarı; oyun bilgisi yalnız sesle verilmez.
- **Haptik kuralı (P0):** her haptik sinyalin görsel karşılığı vardır. Titreşimi kapalı oyuncu, iPad ya da titreşimsiz Android aynı bilgiyi ekrandan alır. Haptik eşlemesi `hud.md` §8.
- **Titreşim kapatma (P1):** `Haptics` tek bir açık/kapalı bayrağına bakar, kalıcı kayıtta tutulur.

---

## 5. Bilişsel erişilebilirlik

| Madde | Kural | Öncelik |
|---|---|---|
| Her yerde duraklatma | Oynanış sırasında her an (kart ekranı zaten durdurur) | Var |
| Kendiliğinden kaybolan bilgi | Eyleme dönük bilgi içeren bildirim en az 1.5 sn tam okunur kalır (olay bandı `BannerHold` ≥ 1.5 sn); kalıcı durumlar (skill, slot) HUD'da sürekli görünür | P0 (P0-c, P0-d) |
| Geri alınamaz seçimin bildirilmesi | Skill bırakılamadığı için son slotu dolduran kart bunu söyler | P0 (kart spec) |
| Terim tutarlılığı | Aynı kavram her yerde aynı kelime: "Sv." (seviye), Türkçe tier adları | P0 |

---

## 6. Ayarlar ekranı ihtiyacı (önkoşul)

Şu an Ayarlar ekranı **yok**. Bu belgedeki P1 kapatma ayarları için en küçük çözüm:

- Duraklatma menüsüne iki satır: **Titreşim: Açık/Kapalı**, **Ekran Sarsıntısı: Açık/Kapalı**. Satır 120 ref px yüksek, tüm satır dokunma hedefi, durum metinle yazılır (renkle değil).
- Lobide aynı ayarlara erişim (ilk açılışta oyuncu run'a girmeden kapatabilsin): P2.
- Değerler kalıcı kayıtta (`MetaProgression` kaydı ya da PlayerPrefs; programcı seçer).

---

## 7. Bilinen, bilerek bırakılan sınırlar

| Özellik | Gereken seviye | Neden yok | Risk | Hafifletme |
|---|---|---|---|---|
| Renk körü modu | Standart | Küçük ekip; bilgi zaten renk dışı yedekle veriliyor (§2.3) | Düşük | Şekil + metin kodlaması |
| UI/metin ölçekleme | Standart | Elle kurulan sabit düzen (K18) | Orta: az gören oyuncu | Minimum boyutlar yükseltildi (§2.1) |
| Tam hareket azaltma | Kapsamlı | Ağır çekim oyun hissinin parçası | Orta | Sarsıntı kapatılabilir |
| Ekran okuyucu (VoiceOver/TalkBack) | Standart+ | Oyun görsel ve gerçek zamanlı | Düşük | — |
| Zorluk ayarı | Standart | Denge tek eğri (OYUN.md §9) | Orta | Market kalıcı güçleri dolaylı kolaylaştırır |
| Ses ayarları | Temel | Ses yok | — | Ses gelince zorunlu |

---

## 8. Sistem matrisi

| Sistem | Görsel | Motor | Bilişsel | İşitsel | Ele alındı mı |
|---|---|---|---|---|---|
| HUD | Küçük metin, renk kodlu barlar | Duraklat tuşu küçük | Skill durumu görünmüyor | Ses yok | `hud.md` |
| Seviye atlama kartları | Küçük etiket, renk kodlu tür | — | Slot doluluğu görünmüyor, geri alınamaz seçim | — | `seviye-atlama-kartlari.md` |
| Hasar / geri bildirim | Kırmızıya dayalı | — | Düşük can uyarısı yok | Hasar haptiği yok | `hud.md` §8 |
| Duraklatma menüsü | — | Yıkıcı butonlar korumasız | — | — | Açık (K10) |
| Oyun sonu | — | Giriş koruması yok | — | — | Giriş koruması P0-a (`hud.md` §16); geri kalanı açık (K9) |
| Market / lobi | Soluk fiyat kontrastı | Onaysız satın alma | Kilitli karta tepki yok | — | Açık (K6, K15) |

---

## 9. Test planı

| Ne | Yöntem | Geçme ölçütü | Sorumlu |
|---|---|---|---|
| Metin boyutu | Sahne/kod denetimi: tüm TMP fontSize ≥ 32 (ikincil ≥ 28) | İstisna yok | ux-designer |
| Kontrast | En açık ve en koyu şehir bölgesinde ekran görüntüsü, kontrast aracı | §2.2 eşikleri | ux-designer |
| Renk | Ekran görüntüsünü gri tonlamaya çevir | Tür, bar ve durum ayırt edilebiliyor | ux-designer |
| Dokunma | Gerçek cihazda duraklat tuşu ve kartlar, başparmakla 20 deneme | Iska yok | QA (Serra) |
| Parlama | Üst üste hasar (5 polis) kaydı, kare kare | Kenar sabit tonda, tekrarlayan kırmızı flaş yok; tek vuruş flaşları en sık 1/sn | QA |
| Giriş koruması | Sürüklerken öl, parmağı oyun sonu butonu üstünde kaldır | Buton tetiklenmiyor | QA |
| Haptik kapalı | Titreşim kapalıyken hasar, düşük can, tier, boss | Hepsi ekrandan anlaşılıyor | QA |

---

## 10. Denetim geçmişi

| Tarih | Denetleyen | Tür | Kapsam | Özet | Durum |
|---|---|---|---|---|---|
| 2026-11-15 | ux-designer | Koddan çıkarım | Tüm UI (`interaction-patterns.md`) | Metin boyu, duraklat hedefi, renk/haptik boşlukları | Açık |
| 2026-11-15 | ux-designer (bağımsız) | Spec incelemesi | HUD, kart, bu belge (`production/ui/oyun-ici-ui/ux-review.md`) | Kırmızı flaş dizisi, sahipsiz giriş koruması, 1.5 sn kuralı, düşük can işareti, rozet kontrastı | Spec'lerde kapatıldı; uygulama sonrası test (§9) |

## 11. Açık sorular

| Soru | Sahibi | Çözüm |
|---|---|---|
| Ayarlar duraklatma menüsünde mi, ayrı ekran mı? | Bahar | — |
| Ağır çekim "hareket azaltma" ile kısaltılsın mı (P2)? | Bahar | — |
