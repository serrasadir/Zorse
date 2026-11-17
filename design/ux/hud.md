# HUD Tasarımı — Left No Crumbs

> **Durum**: Tasarımda (In Design)
> **Yazan**: Bahar + ux-designer
> **Son güncelleme**: 2026-11-15
> **Oyun**: Left No Crumbs (tek belge, tüm oyun içi HUD)
> **Platform**: iOS / Android, dikey, tek parmak görünmez sürükleme (ekranın her yeri). Editor'da klavye (WASD).
> **İlgili belgeler**: `OYUN.md` §3-6, §10 · `SKILLS.md` §1-3 · `design/ux/interaction-patterns.md` (K1-K18) · `design/ux/seviye-atlama-kartlari.md`
> **Erişilebilirlik seviyesi**: Temel (`design/accessibility-requirements.md`)
> **Stil**: interaction-patterns §3 (gece mavisi yarı saydam panel, altın vurgu, kalın beyaz yazı, tek yuvarlak 9-slice sprite)
> **Kod**: `Assets/_Project/Scripts/UI/HUDController.cs`

**Birim:** ref px = 1080x1920 referans (genişliğe eşli). 1 ref px ≈ 0.36 pt. Minimum metin 32 ref px, minimum dokunma hedefi 120 ref px.

**Öncelik:** **P0** = mutlaka, **P1** = önemli, **P2** = cila. Önce yalnız P0 uygulanır; P0 dört ayrı test edilebilir pakete bölünür (**P0-a…d**, §16).

**Yeni kalıplar** (`interaction-patterns.md` K19-K25): **K19 Slot şeridi**, **K20 Olay bandı** (K13'ün genellenmesi), **K21 Kenar uyarısı**, **K22 Hayalet bar**, **K23 Tür şekli kodlaması**. K20, K21, K22'nin tanımı bu belgede. Diğer her şey mevcut kalıplardan (K5, K7, K11, K12, K13, K16). Metin glifleri: interaction-patterns §3 glif kuralı.

---

## 1. HUD felsefesi

Oyuncunun gözü ekranın ortasında, blob'un üstünde: ne yiyebileceğine ve nereye kaçacağına bakıyor. HUD **kenarlarda sessiz durur, sadece bir şey değişince konuşur.** Sürekli göstergeler hareketsizdir; animasyon yalnız değişim anında oynar ve kısa sürer (0.2-0.4 sn, §3 animasyon karakteri). Büyük anlar (tier, boss, evrim) tek bir ortak "olay bandı"ndan duyurulur; asla iki tanesi üst üste binmez.

**Belirsizlikte:** GİZLE. 2026-11-04 sadeleştirmesinin dersi: kalabalık HUD oyuncuyu yordu. Yeni bir öğe ancak aşağıdaki kuralı geçerse eklenir.

**Gereklilik kuralı:** "Bir HUD öğesi, oyuncunun **şu anki 10 saniyesindeki bir kararını** değiştiriyorsa ya da **geri alınamaz bir seçimin sonucunu** gösteriyorsa yerini hak eder." (Can → kaç/saldır; tier → neyi yiyebilirim; slotlar → hangi kartı alırım.)

---

## 2. Bilgi mimarisi

### 2.1 Bilgi envanteri

Can, max can, kalkan (Zırh), XP / sonraki seviye, seviye, tier, süre, skor, coin, aktif skill'ler + seviyeleri (3), pasif skill'ler + seviyeleri (3), boş slot sayısı, nadir (Bölünme: var / kullanıldı), evrim (Kara Delik), skill süreli durumları (Yara Öfkesi öfkeli, Zırh İkinci Katman bekleme, skill bekleme süreleri), boss var/yok + adı + canı, boss konumu (ekran dışında), hasar anı, kalkan vuruşu / kırılması, düşük can, tier atlama / düşme, seviye atlama, kartsız seviye atlama (+%30 can), evrim anı, Bölünme tetiklendi, haber başlığı, yaya tekmesi (sersemleme), coin düşmesi/toplanması.

### 2.2 Sınıflandırma

| Bilgi | Hep | Bağlamsal | İstekle | Dünyada (diegetik) | Gerekçe |
|---|---|---|---|---|---|
| Can + max can | X | | | blob kızarması | Her saniye kaç/saldır kararı |
| Kalkan | | X (Zırh varsa) | | kalkan küresi parlar | Zırh yoksa anlamsız |
| XP / seviye | X | | | | Sonraki kartın ne kadar yakın olduğu |
| Tier | X | | | blob boyu | "Neyi yiyebilirim" sorusunun cevabı |
| Süre | X | | | | Boss en geç süreleri, run hedefi |
| Skor | X | | | | Rekor takibi |
| Coin | X | | | yerde coin | Market hedefi |
| Skill slotları + seviye | X (küçük, sakin) | | Kart ekranında büyük | skill efektleri | Kart kararı geri alınamaz; Bahar kararı |
| Bölünme / Kara Delik | | X (sahipse) | | | Ölümden kurtulma hakkı kaldı mı |
| Skill bekleme süreleri | | | | Efektin kendisi | Kalabalık; otomatik ateşleniyor, oyuncu karar vermiyor. **Gösterilmez** |
| Yara Öfkesi öfke hali | | X | | hız/hasar | Risk-ödül kararı (canı düşük tut) |
| Zırh İkinci Katman bekleme | | X (Sv.5) | | | Kalkan güveni |
| Boss adı + canı | | X (boss varken) | | boss | Hedef |
| Boss ekran dışı konumu | | X | | | Nereden gelecek |
| Hasar | | X (anlık) | | kızarma, sarsıntı | Tehlike |
| Düşük can | | X (eşik altı) | | | Kaç |
| Tier/seviye/boss/evrim olayları | | X (olay bandı) | | | Kilometre taşı |
| Haber başlığı | X (8 sn sonra) | boss varken gizli | | | Atmosfer (K14) |

---

## 3. Yerleşim bölgeleri

> **Doğrulanmadı:** can/XP/boss barlarının bugünkü sahne konumu (interaction-patterns §5). Aşağıdaki düzen **hedef**dir. P0 maddeleri mevcut konumlarla uygulanabilir; barların bu düzene taşınması **P1 (yerleşim taşıma)**.

### 3.1 Bölge şeması (dikey, safe area içi, 1080 genişlik)

```
 0                                                    1080
 ┌────────────────────────────────────────────────────┐  0
 │ HABER BANDI (58)  ── boss varken yerine BOSS BARI ─│     A: Üst şerit (0-100)
 │                     (ad 44 + 6 + bar 24 = 74)      │
 ├────────────────────────────────────────────────────┤  104
 │ [02:14][ II ]                        [   12.340 ]  │     B: Bilgi satırı
 │                                      [ ● 1.205  ]  │
 ├────────────────────────────────────────────────────┤  ~216
 │ ♥ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░  (can, sola uzar)          │     C: Durum bloğu
 │   ▒▒▒▒▒▒▒▒░░░░░░░░░░░░  (kalkan, Zırh varsa)       │
 │ Sv.7 ▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░  ORTA ●●●●○○○     │
 ├────────────────────────────────────────────────────┤  ~330
 │ (●VK3)(●SP2)( ○ )  [■SN5][■HZ1][ □ ]  ◆  ★        │     D: Slot şeridi
 ├────────────────────────────────────────────────────┤  ~410  (ekranın ~%21'i)
 │                                                    │
 │              OLAY BANDI (y anchor 0.72)            │     E: Olay bandı
 │                                                    │
 │                      (blob)                        │     F: Merkez — boş
 │                                                    │
 │  KENAR UYARISI: ekranın 4 kenarı (vinyet), boss oku│     G: Kenar
 │                                                    │
 │            (başparmak bölgesi — HUD yok)           │     H: Alt
 └────────────────────────────────────────────────────┘  1920
```

`( )` = aktif slot (daire), `[ ]` = pasif slot (yuvarlak kare), `◆` = nadir, `★` = evrim. İçi boş = boş slot.

**Neden slot şeridi üstte, altta değil:** dikey tek parmak oyununda başparmak ekranın alt yarısında durur; alttaki gösterge sürekli parmağın altında kalır. Üstte, durum bloğunun hemen altında oyuncu bara bakarken çevresel görüşle görür. Bedeli: üst HUD ~%21 yükseklik; bütçe §7.

### 3.2 Bölge tablosu

| Bölge | Konum | Safe area | Öğeler | En fazla | Not |
|---|---|---|---|---|---|
| A Üst şerit | 0-100, tam genişlik | Evet (çentik altı) | Haber bandı (58, üstte) **ya da** boss barı (ad 0-44, bar 50-74) | 1 | Boss varken bar bandın yerini alır (P1 yer değişimi); 100-104 boşluk. Yükseklik boss varken de 100, §7 bütçesine dahil |
| B Bilgi satırı | 104-208 | Evet, kenar 24 | Süre, duraklat, skor+coin | 3 | Duraklat tek dokunulabilir öğe |
| C Durum bloğu | ~216-330 | Evet, kenar 24 | Can, kalkan, XP, Sv., tier | 5 | |
| D Slot şeridi | ~338-410 | Evet, kenar 24 | 6 slot + 2 özel rozet | 8 ikon | Sol hizalı, sakin |
| E Olay bandı | y anchor 0.72 | Evet | Tek olay bandı | 1 | Mevcut evrim banner'ı yeri (K13) |
| F Merkez | orta %40 | — | Hiçbir kalıcı öğe | 0 | Blob ve dünya |
| G Kenar | ekran kenarı | Taşar (tam ekran) | Kenar uyarısı, boss oku | 2 | Dokunma almaz |
| H Alt | alt %35 | — | Yok | 0 | Başparmak |

**Safe area:** üstte `Screen.safeArea` (çentik/Dynamic Island), altta ev çubuğu. Kenar uyarısı tek istisna: tam ekran kaplar (K1-A gibi kök Canvas'a).

---

## 4. HUD öğeleri

### 4.1 Genel tablo

| Öğe | Bölge | Hep görünür | Tetik | Veri kaynağı | Güncelleme | Min boyut | Öncelik | Renk dışı yedek |
|---|---|---|---|---|---|---|---|---|
| Can barı | C | Evet | — | `OnHealthChanged` | Olayda | yük. 28 | P0 düşük can işareti, P1 kalp ikonu | Konum; düşük canda "!" işareti |
| Hayalet bar (K22) | C | Hasarda | Can düşüşü | aynı | Olayda + 0.7 sn anim | — | P1 | Beyaz parça = kaybedilen |
| Kalkan barı | C | Zırh varsa | `OnShieldChanged` | `BlobHealth` | Olayda | yük. 12 | P1 | Kalkan ikonu |
| XP barı + Sv. | C | Evet | — | `OnXPChanged`, `OnLevelUp` | Olayda | yük. 20, metin 32 | P0-a | "Sv. N" metni |
| Tier göstergesi | C | Evet | — | `OnBlobTierChanged` | Olayda | 32 | P0 ad, P1 noktalar | Ad (P0) + 7 nokta (P1) |
| Süre | B | Evet | — | `OnSurvivalTimeUpdated` | Saniyede bir string | 50 | — (var) | — |
| Duraklat | B | Oynanışta | — | `PauseMenu` | — | hit 120x120 | P0-a | "II" metni |
| Skor | B | Evet | — | `OnScoreChanged` | Olayda | 44 | P2 sayma | — |
| Coin | B | Evet | — | `OnCoinsChanged` | Olayda | 32 | P0-a boyut, P1 ikon | "coin" kelimesi (P1 ikon) |
| Slot şeridi (K19) | D | Evet | — | `OnUpgradeApplied`, `OnCharacterSelected`, sıralı slot listesi (P0-b) | Olayda | slot 72, metin 32 | P0-c | Şekil + kısaltma |
| Bölünme rozeti | D | Sahipse | — | Bölünme uygulandı / tetiklendi | Olayda | 80 | P1 | Kullanılınca çarpı |
| Kara Delik rozeti | D | Sahipse | — | Evrim uygulandı | Olayda | 80 | P1 | ★ şekli |
| Öfke işareti | D | Öfkeliyken | Can < eşik + Yara Öfkesi | `BlobSkills` (olay gerek) | Durum değişince | — | P1 | Çerçeve nabzı + "!" |
| İkinci Katman bekleme | D | Zırh Sv.5 | Katman kullanıldı | `ArmorShield` (olay gerek) | 0.25 sn'de bir dolum | — | P2 | Radyal dolum |
| Boss barı | A | Boss varken | Boss geliş olayı (P0-b), `OnBossHealthChanged` | Boss | Olayda | yük. 24, ad 34 | P0-d ad, P1 giriş + yer | Boss adı |
| Boss oku | G | Boss ekran dışında | Boss konumu | Boss transform | Her kare konum (string yok) | 72 | P1 | Ok şekli |
| Kenar uyarısı (K21) | G | Hasar / düşük can | Hasar, kalkan, eşik | `BlobHealth.OnDamaged`, `OnShieldHit`, BonusShield emme olayı (P0-b), `OnHealthChanged` | Olayda + anim | — | P0-d | Haptik, sarsıntı, "!" işareti |
| Olay bandı (K20) | E | Olayda | §8 | çeşitli | Olayda | 64 | P0-d sade, P1 kuyruk kuralları | Metin |
| Haber bandı | A | 8 sn sonra | — | `NewsTicker` | K14 | 32 | — | — |

### 4.2 Öğe detayları

---

**Can barı** (mevcut K11 + değişiklikler)

- Görsel: kırmızı dolgu, gece mavisi zemin; sağ kenar sabit, max can ile sola uzar (mevcut). Solunda **kalp ikonu** 48x48 (P1, ikon gelince; o zamana kadar ikonsuz, glif kullanılmaz).
- Veri: değer. **Sayı yok** (Bahar kararı).
- Güncelleme: düşüşte bar anında iner, arkada **hayalet parça** (K22, P1) kalır; artışta (kalp, Geri Dönüşüm, Rejenerasyon) bar 0.25 sn'de yumuşak dolar, üstünde 0.2 sn açık yeşil-beyaz parlama (P2).
- Aciliyet durumları:
  - **Normal** (> %30): sakin.
  - **Düşük** (≤ %30, `LowHealthFraction`), P0-d:
    - **"!" işareti:** can barının sol ucunun 12 px solunda 48x48 `Image` (daire sprite'ı, K23 asset'i), renk (1,0.15,0.15); ortasında TMP "!" 36 ref px kalın beyaz. Yalnız düşük canda görünür (`SetActive`), sabittir (nabız yok). Düşük canın renk dışı yedeği budur. P1'de kalp ikonu gelince aynı daire 32x32'ye küçülüp kalbin sağ üstüne rozet olur.
    - Kenar uyarısı düşük can nabzı (K21).
    - Eşiğe **ilk düşüşte bir kez** güçlü haptik. Yeniden tetiklenmesi için önce `LowHealthRearm` (%40) üstüne çıkmak gerekir (histerezis).
    - Eşik üstüne çıkınca işaret ve nabız anında kalkar.
  - **Düşük, P1:** bar 1.2 Hz ölçek nabzı (y 1→1.15, sadece bar). Bölünme olayı gelince (P1, olay gerekir) sonraki 1 sn düşük can haptiği bastırılır (Bölünme güçlü haptiğinin hemen ardından ikinci güçlü titreşim olmasın).
  - **Sıfır**: ölüm, HUD oyun sonu ekranının altında kalır.
- Etkileşim: yok. Özelleştirme: yok.

---

**Hayalet bar (K22, yeni kalıp)** — P1

- Can barının dolgusunun arkasında ikinci bir Image (beyazımsı, alfa 0.8). Hasarda ana dolgu anında iner, hayalet parça **0.4 sn bekler, 0.3 sn'de** ana dolguya iner (ease-out). Üst üste hasarda bekleme yeniden başlar, hayalet daha yukarıda kalır: "ne kadar yedim" toplamı görünür.
- Kalkan barı için aynısı P2.
- Ölçeksiz zaman.

---

**Kalkan barı** (mevcut, düzeltme) — P1

- Kalınlık: min 6 → **min 12 ref px**, can barının %45'i kuralı kalır.
- Solunda küçük kalkan ikonu 36x36 (P1, ikon gelince; o zamana kadar ikonsuz).
- **Kalkan emdi** (`OnShieldHit`, ya da Geri Dönüşüm geçici kalkanı için BonusShield emme olayı, P0-b): kenar uyarısı **mavi** tonda ve yarı şiddette (K21); haptik hafif. Oyuncu "can gitmedi" diye okur. P0-d. Kalkan barının 0.15 sn beyaz parlaması P1. Aynı vuruş kısmen kalkana kısmen cana geçerse (`OnShieldHit` + `OnDamaged` aynı karede) **kırmızı kazanır**.
- **Kalkan kırıldı** (0'a indi): bar K7 shake (0.35 sn), haptik orta, bar 1.5 sn soluk (alfa 0.4) kalır sonra dolmaya başlayınca normale. P1.
- **Dolarken**: dolum zaten Zırh kuralıyla zaman içinde; bar değeri olayla güncellenir.

---

**XP barı + seviye** — P0-a

- "Lv.N" → **"Sv. N"** (kartlar ve Market ile aynı terim). 30 → **32 ref px**.
- Sv. metni yalnız `OnLevelUp`'ta yazılır (mevcut).
- **Dolma anı (P1):** XP barı dolduğu karede bar 0.15 sn beyaz parlar ve "Sv." metni K7 punch (1.15). Kart ekranı zaten aynı karede açılıyor (oyun durur); parlama kart ekranının arkasında görünmesin diye bkz. kart spec §10 (arka plan 0.2 sn'de kararır, parlama o arada görülür). Oyun akışına gecikme **eklenmez**.
- **Kartsız seviye atlama** (havuz boş, can %30 dolar): olay bandı "Sv. N · +%30 CAN" (§8), can barı artış parlaması. P1.

---

**Tier göstergesi** — P0-a ad · P0-d tier bandı · P1 noktalar ve punch

- Metin: İngilizce enum yerine **Türkçe ad**, 24 → **32 ref px**, beyaz alfa 0.85 → 1.0 (bilgi taşıyor). Adlar (Bahar kararı):

| Enum | Ad |
|---|---|
| Tiny | MİNİK |
| Little | UFAK |
| Small | KÜÇÜK |
| Medium | ORTA |
| Large | BÜYÜK |
| Giant | KOCAMAN |
| Colossal | DEV |

- Ad dizisi tek statik `string[]`, **`SlowdownRank` sırasıyla** indekslenir (enum sayısal değeri sıra değil, CLAUDE.md); her olayda yeni string yok.
- **Tier yönü:** HUD önceki tier'ı tutar (`OnCharacterSelected`'da başlangıç tier'ı); yön `SlowdownRank` karşılaştırmasıyla. Yeni olay gerekmez.
- **Tier yukarı (P0-d):** olay bandı (§8) + haptik orta.
- **P1:** gösterge K7 punch (1.12, 0.22 sn); **7 noktalı ilerleme** (dolu nokta = geçilen tier, 16x16 ref px, 6 px aralık, beyaz/soluk; "Ufak"/"Küçük" yakınlığını sırayla çözer); tier düşme bandı (Bölünme).

---

**Süre** — değişiklik yok (K12). Saniyede bir string, mevcut.

---

**Duraklat tuşu** — P0-a

- Görsel 88x88 (72'den), **dokunma alanı 120x120** (görünmez genişletilmiş `Image` raycast alanı ya da büyük şeffaf ebeveyn). Süre kutusuyla arasında ≥ 24 ref px boşluk; dokunma alanı süre kutusunun üstüne binebilir (süre dokunma almaz).
- Glif "II" 40 ref px kalın.
- Gerekçe: Fitts yasası; ekranın en uzak köşesinde, başparmağın en zor eriştiği yerde küçük hedef = ıska. Temel seviye ≥ 44 pt.

---

**Skor + coin** — P0-a boyut, P1 ikon, P2 sayma

- Coin 28 → **32 ref px**. Kutu 240 → genişliği metne göre (sağ hizalı, sol kenar uzar; değer değişince `preferredWidth` ile bir kez hesaplanır, en az 240). Büyük sayıda taşma biter.
- "N0 coin" → **coin ikonu (40x40) + sayı** (P1, ikon gelince). İkon gelene kadar mevcut "coin" kelimesi kalır.
- Coin alınınca coin metni K7 punch 1.1 (0.22 sn), **en fazla 0.25 sn'de bir** (Vakum Sv.5 coin çekerken spam olmasın). P2.
- Skor "tick up" sayma: P2, TMP `SetText("{0:N0}"...)` yerine `SetText("{0}", value)` (GC'siz) ile, saniyede en fazla 15 güncelleme.

---

**Slot şeridi (K19)** — P0-c (Bahar kararı: HUD'da küçük ikonlu gösterge)

- **Amaç:** "Elimde ne var, kaç seviye, kaç boş slot kaldı" sorusunu kart ekranına girmeden cevaplamak. Ortak kurallar (sıra, slot görünümü, seviye hapı, boş slot, Sopa Sv.0, güncelleme animasyonu): **interaction-patterns K19 / K23**. Burada yalnız HUD'a özgü olanlar.
- **Yerleşim:** D bölgesi, sol hizalı, kenar 24.
- **Boyut:** slot 72x72 ref px (~26 pt), ikon 52, kısaltma 32, hap 32 (rakam 28). En geniş hali: 6x72 + 4x8 + 2x24 + 2x(72+8) ≈ 672 ref px (ekranın %62'si). Yükseklik 72.
- **Tür dolu** (3/3): değişiklik yok, boş slot kalmadığı görülür. Ekstra uyarı HUD'da yok, kart ekranında var.
- **Animasyon zamanı:** slot fade/punch `OnUpgradeApplied` anında başlar; kart ekranı o sırada sönmekte olduğu için (kart spec §10) oyuncu animasyonu panel kalkarken görür.
- **Nadir rozeti (Bölünme), P1:** **elmas** zemin 72x72, pembe çerçeve, "BÖ" / ikon. Kullanıldıktan sonra: alfa 0.4 + üstünde çapraz çizgi (renk dışı "harcandı").
- **Evrim rozeti (Kara Delik), P1:** **yıldız** zemin + altın çerçeve, "KD" / ikon, hapsız.
- **Hareket:** şerit **sabittir**. K19 güncelleme animasyonu dışında yalnızca:
  - Yara Öfkesi öfkeliyken o slotun çerçevesi turuncu-kırmızı, 1 Hz yavaş nabız (alfa 0.6↔1) + köşede "!" 28 px (P1). Öfke bitince durur.
  - Zırh Sv.5 İkinci Katman bekleme: Zırh slotunun üstünde radyal karartma (`Image.Filled`, Radial360), 30 sn'de açılır; hazırken yok (P2).
  - Skill bekleme süreleri (Havai Fişek, Sopa vuruşu vb.) **gösterilmez** (gereklilik kuralı: otomatik, oyuncu karar vermiyor; şeridi canlı tutup kalabalık yapar).
- **Etkileşim:** yok, dokunma almaz. Detay kart ekranında.
- **Sakinlik ayarı (P2, deneme):** run'ın ilk 8 sn'si ve son değişiklikten 5 sn sonra şerit alfası 0.7'ye iner; yeni değişiklikte 1.0. Kalabalık şikâyeti sürerse açılır.

### 4.3 Geçici skill kısaltmaları (ikon gelene kadar)

| Skill | Tür | Kısaltma |
|---|---|---|
| Vakum | Aktif | VK |
| Kaygan Zemin | Aktif | KZ |
| Sopa | Aktif | SP |
| Havai Fişek | Aktif | HF |
| Yer Sarsıntısı | Aktif | YS |
| Sindirim Gazı | Aktif | SG |
| Zırh | Aktif | ZR |
| Sindirim | Pasif | SN |
| Geri Dönüşüm | Pasif | GD |
| Hızlı Metabolizma | Pasif | HM |
| Hız | Pasif | HZ |
| Tetikçi | Pasif | TT |
| Can Artışı | Pasif | CA |
| Rejenerasyon | Pasif | RJ |
| Yara Öfkesi | Pasif | YÖ |
| Bölünme | Nadir | BÖ |
| Kara Delik | Evrim | KD |

Tümü benzersiz. Kısaltma `UpgradeData._abbr` alanında tutulur, `SkillSetSetup` yazar (P0-b); ikon gelince yedek kalır. Türü şekil söyler, renk ikincil. SKILLS.md'deki emojiler UI'da kullanılmaz (glif kuralı, interaction-patterns §3).

---

**Boss barı** — P0-d ad · P1 yer değişimi ve giriş animasyonu

- **P0-d:** bar mevcut sahne konumunda kalır, mevcut `SetActive` aç/kapa korunur; üstüne **boss adı** eklenir. Ad boss geliş olayından (P0-b) bir kez yazılır.
- **Görsel:** üstte boss adı 34 ref px kalın beyaz, K5 gölge, sol hizalı, TMP auto-size alt 32; altında bar 24 ref px. Dolgu koyu kırmızı-turuncu, zemin gece mavisi alfa 0.7. Final boss: ad altın.
- **Adlar** (sabit metin): "POLİS TANKI", "GÜÇLÜ TANK", "POLİS HELİKOPTERİ". Kaynak: `BossData`/`EnemyData` görünen ad alanı (P0-b; yoksa eklenir).
- **Yer (P1):** A bölgesi (§3.2 ölçüleri); boss gelince haber bandı 0.3 sn'de söner, boss barı aynı yere gelir. Boss ölünce bar söner, bant 0.35 sn sonra geri gelir.
- **Giriş (P1):** bar yukarıdan 0.3 sn'de kayar (y +60 → 0, ease-out) + alfa 0→1, dolgu 0'dan tam cana 0.5 sn'de dolar. Ölçeksiz.
- **Hasar:** hayalet bar (K22) boss barında da (P1) — boss'a vurduğunu görmenin en net yolu.
- **Ölüm (P1):** bar 0.15 sn beyaz parlar, 0.4 sn söner. Olay bandı §8. P0'da mevcut kapanış.
- **Final boss %25 eşikleri:** bar üstünde 3 ince işaret çizgisi (%25/50/75) (P2).

---

**Boss oku (yeni, K21'in yön varyantı)** — P1

- Boss ekran dışındayken ekran kenarında, boss yönünü gösteren 72x72 ok (koyu kırmızı-turuncu + beyaz kontur), kenardan 32 px içeride, safe area içinde. Ekrana girince 0.2 sn söner.
- Her kare konum hesaplanır (dünya→ekran), string yok. Final boss için kamera ikisini kadraja aldığından nadiren görünür; minibosslar için asıl değer.

---

**Kenar uyarısı / vinyet (K21)** — P0-d

- Tek tam ekran `Image` (kök Canvas, safe area dışına taşar, `raycastTarget=false`), kenarlarda yoğun ortada şeffaf radyal gradyan dokusu (256x512, beyaz, tintlenir). Ayrı alt Canvas'ta (yeniden çizim izolasyonu).
- **Boşta çizilmez:** alfa 0'a indiği karede `Image.enabled = false` (tam ekran overdraw olmasın); ilk vuruşta ya da düşük canda `true`.
- **Girdiler:** can vuruşu = `BlobHealth.OnDamaged(hasar)`; kalkan vuruşu = `OnShieldHit(emilen)` ve BonusShield emme olayı (P0-b). Aynı karede ikisi birden gelirse can vuruşu sayılır (kırmızı).
- **Durumlar** (alfa = katmanların en büyüğü, toplanmaz; ölçeksiz zaman):

| Durum | Ne zaman | Alfa davranışı |
|---|---|---|
| Boşta | Vuruş yok, can > eşik | 0, `Image` kapalı |
| Flaş | Vuruş geldi ve önceki vuruştan beri ≥ `VignetteCalmGap` (1.0 sn) geçti | 0 → tepe `VignetteRise` (0.06 sn), tepe → 0 `VignetteFade` (0.3 sn). Tepe = `0.25 + 0.35 × clamp01(hasar / VignetteDamageRef)` |
| Flaş içinde yeni vuruş | Yükselme fazında (ilk 0.06 sn) | Tepe = iki tepenin büyüğü; yeni flaş yok |
| | Sönme fazında | **Sürekli**'ye geçer |
| Sürekli | Önceki vuruştan beri < 1.0 sn (kuşatılma) | Mevcut alfadan `VignetteSustainAlpha`'ya (0.35) 0.15 sn'de doğrusal (yükselir ya da iner, tepe yapmaz) ve **sabit tutulur**. Her vuruş tutmayı uzatır; son vuruştan `VignetteSustainHold` (0.5 sn) sonra 0.3 sn'de 0'a. Bu durumda flaş yok |
| Düşük can | Can ≤ %30 | Ayrı katman: alfa 0.06↔0.18 sinüs, 1.2 Hz; hasar katmanıyla en büyüğü alınır |

  Sonuç: flaş en sık saniyede 1 kez; 5 polis temas halinde kenar sabit tonda kalır, flaş dizisi yok (erişilebilirlik §2.4).
- **Renk:** can vuruşu (1,0.15,0.15) (blob flaşıyla aynı); kalkan vuruşu kalkan mavisi (0.55,0.82,1), tepe ve sürekli alfa yarıya. Sürekli durumda renk son 0.5 sn'de can vuruşu varsa kırmızı, yoksa mavi; renk geçişi 0.15 sn.
- **Yön (P2):** dokuyu dört kenar parçasına bölüp hasar kaynağı tarafını güçlendirme.
- Ekran sarsıntısı kapalıysa da kenar uyarısı **kalır** (sarsıntı değil, ışık).

---

**Olay bandı (K20; K13 evrim banner'ının genellenmesi)** — P0-d sade, P1 kuyruk kuralları

- Mevcut evrim banner'ı (y anchor 0.72, 1000x140, pop 1.6→1) tek ortak bileşene çevrilir: **başlık** + isteğe bağlı **alt satır**. P0-d olayları: tier yukarı, boss geldi, final boss geldi, evrim (mevcut).
- Başlık 72 ref px kalın (evrimde 92), K5 gölge; **gölge rengi gece mavisi** (0.05,0.05,0.17,0.85) olur, mor-siyah tek istisna kalkar (tutarsızlık §4.10).
- Alt satır 36 ref px normal, beyaz alfa 0.9, başlığın 70 px altında.
- Renk olaya göre (§8), ama anlamı her zaman metin taşır.
- Zamanlama: pop 0.22 sn (ölçek 1.4→1; evrimde 1.6), tutma olaya göre (`BannerHold`, en az 1.5 sn), sönme 0.4 sn. Ölçeksiz.
- **Okunurluk:** band arkasındaki düşman görünür kalmalı: zemin paneli yok, yalnız metin + K5 gölge.
- **Tek nesne, havuz gerekmez:** bir band + kuyruk (en fazla 3 kayıt, sabit dizi). Kurallar §8.2.

---

## 5. Bağlama göre HUD durumları

| Bağlam | Gösterilen | Gizlenen | Değişen | Geçiş |
|---|---|---|---|---|
| Run başı (`OnCharacterSelected`) | B, C, D (başlangıç skill'i şeritte dolu) | Haber bandı (8 sn), boss | Tüm değerler sıfırlanır, şerit yeniden dolar (restart sahneyi yeniden yüklemez) | Anında; şerit başlangıç skill'i 0.3 sn fade |
| Normal oynanış | Hepsi | Boss barı/oku | — | — |
| Boss aktif | Boss barı + adı; P1: A bölgesinde, ok (G) | P1: haber bandı | — | P0 anında; P1 bant 0.3 sn söner, bar 0.3 sn kayar |
| Düşük can | Kenar nabzı, "!" işareti (P0); bar nabzı (P1) | — | — | Eşikte anında |
| Kart ekranı (LevelUp) | HUD arkada kalır, backdrop altında | — | Kart ekranı kendi büyük slot şeridini gösterir. O an gösterilmekte olan band **donar** (süre ilerlemez) ve kart paneli kök Canvas'ta son kardeş olduğu için backdrop'un arkasında kalır; panel kapanınca kaldığı yerden sürer | Kart spec §10 |
| Duraklatma | HUD arkada | Duraklat tuşu (mevcut) | — | Mevcut |
| Evrim anı (~1.8 sn ağır çekim) | Hepsi + "★ KARA DELİK" | — | — | K13 |
| Yaya tekmesi (×0.4) | Hepsi | — | — | Değişmez |
| Bölünme tetiklendi | Hepsi + olay bandı | — | Tier düşer, Bölünme rozeti "harcandı" | §8 |
| Ölüm / kazanma | Oyun sonu paneli üstte | Olay bandı kuyruğu temizlenir, kenar uyarısı söner | — | K9 |
| Lobi / menü | HUD kapalı (mevcut) | — | — | Mevcut |

---

## 6. Bilgi hiyerarşisi

| Öğe | Seviye | Gerekçe | Gizlenirse ne |
|---|---|---|---|
| Can barı | KALIR (zorunlu) | Hayatta kalma | — |
| Kenar uyarısı / düşük can | KALIR | Hayatta kalma | — |
| Duraklat | KALIR | Erişilebilirlik | Uygulama arka plan otomatik pause |
| Tier | KALIR | Ana döngü (ne yenir) | Blob boyu |
| XP / Sv. | KALIR | Kart yakınlığı | — |
| Boss barı | KALIR (boss varken) | Hedef | — |
| Slot şeridi | KALMALI | Kart kararlarının bağlamı | Kart ekranındaki büyük şerit |
| Süre | KALMALI | Boss en geç süresi | — |
| Skor, coin | GİZLENEBİLİR | Run sırasında karar değiştirmez | Oyun sonu ekranı |
| Haber bandı | GİZLENEBİLİR | Atmosfer | — |
| Skill bekleme süreleri | HEP GİZLİ | Otomatik | Efektin kendisi |

Yer darlığında (kısa ekran, uzun boss adı) önce haber bandı, sonra skor/coin küçülür.

---

## 7. Görsel bütçe

| Kısıt | Sınır | Ölçüm | Tahmin | Durum |
|---|---|---|---|---|
| Üst HUD yüksekliği (A-D) | ≤ %22 ekran yüksekliği (≤ 420 ref px, 1920'de) | Safe area üstünden D bölgesinin altına | ~410 | Sınırda: şerit eklenince doğrula |
| Merkez %40 bölge | Kalıcı öğe yok; yalnız olay bandı (geçici) | Ekran görüntüsü | 0 | OK |
| Alt %35 | HUD yok | — | 0 | OK |
| Aynı anda görünen öğe (sakin an) | ≤ 10 grup (süre, duraklat, skor, coin, can, XP, tier, şerit, bant) | Sayım | 9 | OK |
| Aynı anda olay bandı | 1 | — | 1 | OK |
| HUD panel opaklığı | 0.55-0.65 | — | 0.55 | OK |
| Minimum metin | 32 ref px (ikincil 28) | Kod denetimi | ihlaller düzeltiliyor | P0 |
| Minimum ikon | 40 ref px (slot ikonu 52) | — | — | OK |

---

## 8. Geri bildirim ve bildirim sistemi

### 8.1 Olay tablosu

Haptik: `Haptics.Impact(0)` hafif, `(1)` orta, `(2)` güçlü (mevcut API, K16). Her satırdaki haptiğin görsel karşılığı aynı satırda (erişilebilirlik §4).

| Olay | Tetik | Yer | Görsel | Süre | Haptik | Öncelik | Kuyruk |
|---|---|---|---|---|---|---|---|
| **Hasar** | `OnDamaged` | G + C | Kenar kırmızı (K21), hayalet bar (P1), blob flaşı + sarsıntı (mevcut) | K21 | **Orta**: ilk vuruşta, sonra en sık `DamageHapticInterval` (0.5 sn) (şu an yok) | P0-d | Kuyruksuz, anında |
| **Kalkan emdi** | `OnShieldHit` / BonusShield emme olayı, aynı karede `OnDamaged` yok | G (+ C P1) | Kenar mavi yarım (K21); kalkan barı beyaz parlama (P1) | K21 | Hafif (aynı aralık sınırı) | P0-d | Anında |
| **Kalkan kırıldı** | `OnShieldBroken` | C | Kalkan barı shake (K7) + soluk | 0.35 sn | Orta | P1 | Anında |
| **Düşük can** | Can ≤ %30 (ilk geçiş) | G + C | Kenar nabzı, "!" işareti; bar nabzı (P1) | Durum boyunca | Güçlü, bir kez (histerezis %40) | P0-d | Anında |
| **Tier atlama** | `OnBlobTierChanged`, yön yukarı (§4.2) | E | Olay bandı başlık `<sprite yukari> ORTA!` + alt satır (aşağıda). P1: tier göstergesi punch, nokta dolar | 0.22 + 1.6 + 0.4 sn | Orta | P0-d | §8.2 |
| **Tier düşme** | Yön aşağı (Bölünme) | E + C | `<sprite asagi> KÜÇÜK` beyaz | aynı | — (Bölünme haptiği yeter) | P1 | Bölünme bandıyla birleşir |
| **Boss geliyor** | Boss geliş olayı (P0-b), final değil | E + A + G | Bant "POLİS TANKI GELDİ!" kırmızımsı (1,0.42,0.38), alt satır "Silahla indir"; boss barı adıyla açılır; P1: bar girişi, ok | 0.22 + 1.6 + 0.4 | Güçlü | P0-d | §8.2 (en yüksek öncelik) |
| **Final boss** | Boss geliş olayı, final | E + A | "HELİKOPTER GELDİ!" altın, alt satır "Uzan ve yut!" | 0.22 + 2.0 + 0.4 | Güçlü | P0-d | Boss geliyor ile aynı |
| **Boss düştü** | Boss öldü (`max ≤ 0` geçişi) | E + A | "TANK DÜŞTÜ!" altın, alt satır "Sandık bıraktı" | 0.22 + 1.5 + 0.4 | Güçlü (mevcut sarsıntıyla) | P1 | Normal |
| **Seviye atlama** | `OnLevelUp` (kart açılıyor) | C | XP bar parlama, "Sv." punch; kart ekranı açılır | 0.15 sn | Hafif | P1 | Kuyruksuz |
| **Kartsız seviye** | Havuz boş, %30 can | E | "Sv. 14 · +%30 CAN" yeşilimsi (0.35,0.82,0.45) | 0.22 + 1.5 + 0.4 | Hafif | P1 | Normal, birleşir |
| **Evrim** | `OnUpgradeApplied` + `IsEvolution` | E | "★ KARA DELİK" 92 px altın (mevcut K13) | 0.22 + 1.6 + 0.5 (mevcut) | Güçlü (mevcut) | P0-d (ortak banda taşınır) | §8.2 |
| **Bölünme tetiklendi** | `BlobHealth.SaveFromDeath` | E + D | "BÖLÜNDÜN!" pembe (1,0.45,0.65), alt satır "Damlaları topla"; Bölünme rozeti "harcandı" | 0.22 + 1.6 + 0.4 | Güçlü | P1 | Boss geliyor ile eşit |
| **Skill eklendi / seviye** | `OnUpgradeApplied` | D | İlgili slot punch / fade (K19) | 0.22-0.3 sn | — (kart seçimi verir) | P0-c | — |
| **Coin toplandı** | `OnCoinsChanged` artış | B | Coin punch, 0.25 sn sınır | 0.22 sn | — (yeme zaten titrer) | P2 | — |

**Tier alt satırları** (sabit metin; tier'ın açtığı en tanıdık lokma, OYUN.md §4 tablosu):

| Yeni tier | Alt satır |
|---|---|
| UFAK | "Artık yayaları yiyebilirsin" |
| KÜÇÜK | "İri yayalar ve seyyar arabalar menüde" |
| ORTA | "Polisler ve arabalar menüde" |
| BÜYÜK | "Otobüsler menüde" |
| KOCAMAN | "Elit polis ve kafeler menüde" |
| DEV | "Bütün şehir menüde" |

Bunlar oyuncuya "şimdi ne yapabilirim" bilgisini verir (gereklilik kuralını geçer). Metin listesi Bahar'ın tonuna göre değişebilir.

### 8.2 Kuyruk kuralları

**P0-d (sade):**
1. **Tek band:** aynı anda bir olay bandı. Gösterilirken gelen olay kuyruğa girer (en fazla 3 kayıt, sabit dizi, geliş sırası). Kuyruk doluysa en eski tier kaydı düşer (tier adı C bölgesinde zaten yazılı).
2. **Boss önde:** boss / final boss gelişi kuyruğun **başına** girer; gösterilmekte olan band kesilmez, kendi süresini bitirir (en kötü bekleme ~2.2 sn; boss barı adı o arada zaten görünür).
3. **Durum:** `GameState` Playing değilken (kart ekranı, duraklatma) gösterilen band donar, kuyruk bekler, süreler ilerlemez (§5). Kart seçilince state Playing'e döner, sıradaki band (evrim dahil) oynar.
4. **Temizlik:** `OnGameOver`, `OnRunComplete`, `OnCharacterSelected`'da kuyruk ve band sıfırlanır (restart sahne yüklemez, CLAUDE.md tuzağı).
5. **Kuyruksuz:** hasar, kalkan, düşük can hiç kuyruğa girmez.

P0-d'de tek lokmada iki tier atlanırsa iki band sırayla oynar (bilinen, P1 birleştirme çözer).

**P1 (ekler):**
6. **Öncelik:** Boss geliyor = Final boss = Bölünme > Evrim > Tier > Boss düştü > Kartsız seviye. Kuyruk doluysa en düşük öncelikli en eski düşer.
7. **Birleştirme:** aynı türden olay gösterilirken ya da kuyruktayken gelirse yenisi eskisinin yerine geçer (yalnız son tier). Gösterilmekte olan banda birleşirse metin değişir, kalan tutma süresi **en az 1.0 sn**'ye tamamlanır.
8. **Kesme:** yalnız en yüksek öncelik gösterilmekte olanı keser (0.15 sn hızlı sönme). Kesilen band kuyruğun **başına** döner, yeniden gösterilince tutma süresi 1.0 sn.

---

## 9. Platform uyarlaması

| Platform | Safe area | Çözünürlük | Giriş | Not |
|---|---|---|---|---|
| iOS (çentik / Dynamic Island) | `Screen.safeArea`, üst ~47-59 pt, alt ev çubuğu | 1080x1920 ref, genişlik eşli; uzun ekranda ek yükseklik ortaya gelir | Dokunma | Üst bölgeler safe area üstüne pinli (mevcut TopOffset kuralı) |
| Android | `Screen.safeArea` (kesik ekran) | aynı | Dokunma | Haptik süre/genlik (K16) |
| iPad (ileride) | — | 4:3'te genişlik eşli = daha kısa görünür yükseklik; üst HUD oranı büyür | Dokunma | Test edilmedi; P2 |
| Editor | — | — | WASD | Geliştirme |

HUD yeniden konumlandırma: kapsam dışı (Temel seviye).

---

## 10. Erişilebilirlik (HUD özel)

### 10.1 Renk

| Öğe | Renk riski | Çözüm |
|---|---|---|
| Can / kalkan / XP / boss barı | Kırmızı/mavi ayrımı | İkon (kalp, kalkan), konum, boss adı |
| Slot türü | Turuncu/mavi | Daire / kare / elmas / yıldız (K23) |
| Max seviye | Altın | Rakam "5" |
| Kenar uyarısı hasar/kalkan | Kırmızı/mavi | Kalkan barının parlaması vs can barının düşmesi |
| Olay bandı rengi | Renk = tür | Metin her zaman anlatır |

### 10.2 Metin ölçekleme

Ayar yok (Temel). Bunun yerine minimum boyutlar yükseltildi: tier 24→32, coin 28→32, Sv. 30→32. Tüm değişken metinler (boss adı, olay bandı başlığı) TMP auto-size, alt sınır 32 (bant başlığı 56).

### 10.3 Hareket

| Öğe | Şiddet | Sarsıntı ayarı kapalıyken | Not |
|---|---|---|---|
| Kamera sarsıntısı | Orta | Kapalı | Erişilebilirlik §2.5 |
| Can barı nabzı (P1) | Hafif | Kalır | Bilgi taşıyor |
| Kenar uyarısı | Hafif (ışık) | Kalır | Flaş ≤ 1/sn, sürekli hasarda sabit ton (K21) |
| Olay bandı pop | Hafif | Kalır | |
| Slot punch | Hafif | Kalır | |

### 10.4 Altyazı

Ses ve diyalog yok: N/A. Haber bandı metindir, altyazı değil.

### 10.5 Oyuncu ayarları

Yalnız: **Titreşim Açık/Kapalı**, **Ekran Sarsıntısı Açık/Kapalı** (P1, duraklatma menüsünde; erişilebilirlik §6). HUD opaklığı, ölçek, bildirim ayrıntısı: kapsam dışı.

---

## 11. Ayar düğmeleri

| Parametre | Değer | Aralık | Not |
|---|---|---|---|
| `LowHealthFraction` | 0.30 | 0.2-0.4 | Yara Öfkesi eşiği (%40/%50) ile karışmasın diye altında |
| `LowHealthRearm` | 0.40 | > eşik | Haptik histerezisi |
| `DamageHapticInterval` | 0.5 sn | 0.4-0.8 | Kuşatılmada titreşim yağmuru olmasın, yeme haptiğinden ayrışsın |
| `VignetteCalmGap` | 1.0 sn | ≥ 1.0 | Flaş yalnız bu kadar sakin süreden sonra; altında sürekli ton. Fotosensitivite tabanı, düşürülmez |
| `VignetteRise` / `VignetteFade` | 0.06 / 0.3 sn | Fade 0.2-0.5 | |
| `VignettePeakMin/Max` | 0.25 / 0.6 | 0.1-0.6 | |
| `VignetteDamageRef` | 15 | 10-25 | Tepe alfanın tam çıktığı hasar |
| `VignetteSustainAlpha` / `VignetteSustainHold` | 0.35 / 0.5 sn | 0.25-0.4 | Kuşatılma tonu |
| `GhostHold` / `GhostDrain` | 0.4 / 0.3 sn | | P1 |
| `BannerHold` (tier / boss / final / diğer) | 1.6 / 1.6 / 2.0 / 1.5 sn | 1.5-2.5 | Erişilebilirlik §5: ≥ 1.5 sn |
| `BannerMergeMinHold` | 1.0 sn | | P1 birleştirme / kesilen band |
| `BannerQueueSize` | 3 | 2-4 | |
| `SlotSize` | 72 ref px | 64-88 | Kalabalık şikâyeti olursa 64 |
| `CoinPunchInterval` | 0.25 sn | | |

---

## 12. Uygulama notları (ui-programmer için)

- **Havuz / Instantiate yok:** tüm yeni öğeler (slot şeridi 6+2 slot, olay bandı, kenar uyarısı, hayalet bar, boss oku) **Awake/Build'de bir kez** kurulur (K18 kalıbı); run sırasında yalnız `SetActive`, sprite/metin ataması. Restart'ta yeniden kurulmaz, `OnCharacterSelected`'da sıfırlanır.
- **String:** metinler yalnız değer değişince (mevcut kural). Tier adları ve sabit metinler statik dizi; boss adları data'dan bir kez okunur. Sayı için TMP `SetText("{0}", int)` (GC'siz). Seviye rakamı "1".."5" önceden hazır dizi.
- **Canvas yeniden çizimi:** her kare animasyon oynayan öğeler (kenar uyarısı, can nabzı, olay bandı, boss oku) **ayrı alt Canvas**'ta; statik HUD her animasyonda yeniden batch'lenmesin.
- **Per-frame iş:** yalnız animasyon süren öğeler `Update`'te çalışır (aktif zamanlayıcı yoksa erken dönüş — mevcut banner kalıbı). Boss oku her kare `WorldToScreenPoint`, maliyeti tek vektör.
- **Önbellek:** `BlobHealth`, `BlobGrowth`, `UpgradeSystem` `OnCharacterSelected`'da (mevcut). `OnXPChanged` içindeki `FindAnyObjectByType` yedeği kalkmalı.
- **Ölçeksiz zaman:** tüm HUD animasyonları `unscaledDeltaTime` (K2 kuralı); kuyruk bekleme kuralı (§8.2-5) state'e bakar.
- **Gereken veri/olaylar:** P0 için olanlar P0-b'de (§16). Kalkan/can ayrımı için yeni olay yok: `BlobHealth.OnShieldHit` / `OnDamaged`. Skill değişimi `OnUpgradeApplied`. P1/P2 için ayrıca gerekecek: kartsız seviye olayı, Bölünme tetiklendi/kullanıldı, Yara Öfkesi durum değişimi, Zırh İkinci Katman kullanıldı/hazır.
- **Kenar uyarısı:** boşta `Image.enabled = false` (§4.2); düşük can ya da vuruşta açılır.
- **Haptik:** hasar haptiği HUD'un `OnDamaged` dinleyicisinde, `Haptics.Impact(1)` + `DamageHapticInterval`. Tüm `Haptics` çağrıları ileride tek açık/kapalı bayrağına bakar.
- **Kalkan barı** bugün `Instantiate` ile can barından kopyalanıyor (Awake, tek sefer): kurallara uygun, kalabilir.

---

## 13. Sabit metinler (Türkçe)

| Anahtar | Metin |
|---|---|
| hud.level | `Sv. {0}` |
| hud.coin (ikonsuz geçici) | `{0} coin` |
| hud.pause | `II` |
| tier.1 … tier.7 | `MİNİK` · `UFAK` · `KÜÇÜK` · `ORTA` · `BÜYÜK` · `KOCAMAN` · `DEV` |
| hud.lowHealth | `!` |
| banner.tierUp | `<sprite name="yukari"> {TIER}!` |
| banner.tierDown | `<sprite name="asagi"> {TIER}` (P1) |
| banner.tierUp.sub.2 … .7 | §8.1 tablosu |
| boss.name.1 / .2 / .final | `POLİS TANKI` · `GÜÇLÜ TANK` · `POLİS HELİKOPTERİ` |
| banner.bossIn | `{BOSS} GELDİ!` |
| banner.bossIn.sub | `Silahla indir` |
| banner.finalIn | `HELİKOPTER GELDİ!` |
| banner.finalIn.sub | `Uzan ve yut!` |
| banner.bossDown | `{BOSS} DÜŞTÜ!` |
| banner.bossDown.sub | `Sandık bıraktı` |
| banner.levelNoCard | `Sv. {0} · +%30 CAN` |
| banner.split | `BÖLÜNDÜN!` |
| banner.split.sub | `Damlaları topla` |
| banner.evolution | `★ {ADI}` (mevcut) |
| skill.abbr.* | §4.3 |

---

## 14. Asset ihtiyacı

| Asset | Adet | Boyut / format | Kullanım | Öncelik |
|---|---|---|---|---|
| Skill ikonları | 16 + 1 evrim (Kara Delik) = **17** | Kaynak 256x256 PNG, şeffaf; tek `SpriteAtlas` (tek draw call); kullanım 52 (HUD), 96 (kart şeridi), 140 (kart) | Slot şeridi, kart, olay bandı | P1 (o zamana kadar §4.3 kısaltmaları) |
| Tür şekilleri | 4: daire, yuvarlak kare (mevcut 9-slice), elmas, yıldız | 128x128 PNG, beyaz (tintlenir) | K23, slot zemini, tür etiketi, düşük can "!" dairesi | P0-a (daire + elmas yeni; yıldız P1) |
| TMP Sprite Asset `UI_Glyphs` | 4: `ok`, `yukari`, `asagi`, `uyari` (P1: `yenilik`) | 64x64 PNG beyaz, tek atlas, tint açık | `<sprite name>` ile rozet oku, tier bandı, son slot uyarısı (glif kuralı, interaction-patterns §3) | P0-a |
| HUD ikonları | kalp, kalkan, coin | 96x96 PNG beyaz/renkli | Can, kalkan, coin | P1 |
| Kenar uyarısı dokusu | 1 | 256x512 tek kanal radyal gradyan, beyaz | K21 | P0 (koddan da üretilebilir, ama runtime'da shader'dan materyal üretme yok; doku dosya olarak) |
| Boss oku | 1 | 96x96 PNG | Boss oku | P1 |

**Stil notu (karar art-director/Bahar'da):** düz renkli, 2-3 renkli, kalın gece mavisi kontur (~8 px / 256), oyunun palet dokusu renkleri; 48 px'te siluet testi (tek bakışta tanınmalı). İkon kendi içinde tür rengi taşımaz (tür çerçeveden gelir). Üretim yolu önerisi: karakter portrelerindeki gibi Blender'dan render (`build_blobs.py` → `Icon_*.png` örneği; ör. `build_skill_icons.py`), ya da 2B çizim. İkonlar `Art/City` altına **girmez** (palet ön işlemcisi Point filtre zorlar); `Art/UI/Skills/` önerilir.

---

## 15. Kabul ölçütleri

Her paket kendi maddeleriyle ayrı test edilir (kart ekranı maddeleri kart spec §14).

**Her paket için ortak**
- [ ] Run sırasında HUD kaynaklı GC tahsisi yok (Profiler, 60 sn oynanış, boss + tier + hasar dahil); runtime'da HUD Instantiate/Destroy yok.
- [ ] Restart sonrası paketin eklediği her durum temiz (şerit, kuyruk, kenar uyarısı, "!" işareti, boss adı).

**P0-a**
- [ ] HUD'da "Lv." yok, "Sv. N" 32 ref px.
- [ ] Tier her değerde Türkçe adla, 32 ref px; İngilizce enum adı hiçbir yerde yok.
- [ ] Coin 32 ref px, 7 haneli değerde kutudan taşmıyor.
- [ ] Duraklat dokunma alanı ≥ 120x120 ref px; cihazda 20 başparmak denemesinde ıska yok; duraklat alanında başlayıp dışarı kayan sürükleme duraklatmıyor.
- [ ] Oyun sonu butonları ilk 0.4 sn dokunmaya kapalı; sürüklerken ölünce parmağı kaldırmak "Tekrar Oyna"ya basmıyor.
- [ ] `UI_Glyphs` sprite'ları ve şekil sprite'ları projede; test metninde `<sprite name="ok">` doğru görünüyor.

**P0-b** (görünür değişiklik yok; log / DevCheats ile)
- [ ] Sıralı aktif/pasif listeler alım sırasını veriyor; başlangıç skill'i ilk; Sopa kartı alınmadan listede değil.
- [ ] Her skill'in `_abbr`'ı §4.3 ile birebir; Sopa'da temel silah bayrağı açık, diğerlerinde kapalı.
- [ ] Boss geliş olayı her boss'ta bir kez (N hilesi ve aşama geçişi dahil), doğru ad ve final bayrağıyla.
- [ ] Geri Dönüşüm geçici kalkanı hasar emince olay atılıyor.

**P0-c**
- [ ] Slot şeridi: başlangıç skill'i run başında görünür; kart seçilince doğru slotta, doğru seviyede; boş slotlar boş; 3/3 dolu tür görünür.
- [ ] Sopa Sv.0 iken şeritte değil; kartı alınınca aktif slotta.
- [ ] Üst HUD (A-D) ≤ 420 ref px; alt %35'te HUD yok; merkezde kalıcı öğe yok; şerit dokunma almıyor.
- [ ] Hiçbir metin < 32 ref px (seviye hapı rakamı 28 istisnası).

**P0-d**
- [ ] Her can vuruşunda haptik (ilk vuruş anında, sonra en sık 0.5 sn) ve kenar uyarısı.
- [ ] 5 polis aynı anda temas: kenar sabit tonda kalıyor, tekrarlayan flaş yok (kare kare kayıt); tek vuruşlar arası flaş en sık 1/sn.
- [ ] Boştayken kenar uyarısı çizilmiyor (Frame Debugger'da draw yok).
- [ ] Kalkan (Zırh ya da Geri Dönüşüm geçici kalkanı) emince kenar mavi, can barı oynamıyor; kalkan + can aynı vuruşta kırmızı.
- [ ] Can %30 altında "!" işareti görünür; ilk inişte tek güçlü titreşim; %40 üstüne çıkmadan tekrar titremiyor.
- [ ] Tier bandı ve diğer bandlar ≥ 1.5 sn tam okunur; band arkasındaki düşman seçilebiliyor.
- [ ] Boss gelişi bandı kuyruğun başına giriyor; boss barında ad var.
- [ ] Kart ekranı açıkken band donuyor; kapanınca kaldığı yerden sürüyor.
- [ ] Kenar uyarısı/nabız oynarken statik HUD Canvas'ı yeniden batch'lenmiyor (Frame Debugger).
- [ ] Titreşim kapalıyken hasar, düşük can, tier, boss ekrandan anlaşılıyor.

**P1 (sonra)**
- [ ] Tek lokmada iki tier atlanınca tek band (son tier); boss gelişi gösterilmekte olan bandı kesiyor, kesilen band geri geliyor.
- [ ] Boss barı A bölgesinde, kayarak giriyor, ölünce sönüyor.

---

## 16. Uygulama paketleri (P0-a…d)

P0 dört pakete bölünür; her paket ayrı uygulanır, ayrı build'de test edilir (§15). HUD ve kart ekranının P0 işlerinin tek listesi burasıdır; kart ekranına düşen ayrıntı kart spec §18.

| Paket | İçerik | Bağımlılık |
|---|---|---|
| **P0-a** Metin / terim / boyutlar | HUD: "Lv."→"Sv. N" 32; tier Türkçe adları 32 (`SlowdownRank` dizisi); coin 32 + kutu genişliği; duraklat 88 görsel / 120 dokunma. Kart: tür etiketi 32, rozet hapı 220x64 / 34, ad auto-size 44-58 (kutu 620), açıklama auto-size 32-36 + sarma, kart 920x340 ve yeni y konumları (kart spec §5.1; şerit yeri boş kalır). **Oyun sonu (`GameOverScreen`) butonlarına 0.4 sn giriş koruması** (kart `InputDelay` kalıbı, `interactable=false`; sahibi bu paket). Asset: `UI_Glyphs` TMP Sprite Asset + daire/elmas şekil sprite'ları (§14) | Yok |
| **P0-b** Veri ve olaylar | (1) `UpgradeSystem`: alım sırasına göre sıralı aktif/pasif listeleri (`IReadOnlyList<UpgradeData>`; run başında temizlenir, başlangıç skill'i ilk, Sopa ancak seviyesi > 0 olunca girer) + mevcut `ActiveSlots`/`PassiveSlots`. (2) `UpgradeData._abbr` (2 harf, §4.3), `SkillSetSetup` yazar (Kara Delik dahil). (3) `UpgradeData` temel silah bayrağı (Sopa), `SkillSetSetup` yazar; `UpgradePanel`'deki `Effect is WeaponUpgradeEffect` tahmini kalkar. (4) Boss geliş olayı `GameEvents`'te: görünen ad + final mi, spawn anında bir kez (N hilesi ve aşama geçişi dahil); `BossData`/`EnemyData` görünen ad alanı yoksa eklenir. Boss gitti için mevcut `OnBossHealthChanged(max ≤ 0)` kalır. (5) `BlobHealth`: BonusShield emince ayrı olay (emilen miktar); `OnShieldHit` Zırh'a ait kalır. Kalkan/can ayrımı için yeni olay yok: mevcut `OnShieldHit` / `OnDamaged` | Yok (a ile paralel) |
| **P0-c** Slot şeridi + kart rozetleri + seçim animasyonu | K19 bileşeni (HUD 72 px D bölgesi; kart 96 px + sayaç); K23 şekiller (slot, kart tür etiketi); rozet `Sv. a → b` / `Sv. a → MAX` / `Sv. 0 → 1`; slot notları (son slot, Sopa, nadir, evrim); YENİ ve Sv. rozeti kontrast düzeltmesi; K24 zaman çizelgesi + seçimde orta haptik (kart spec §10) | P0-a (glif asset, şekil sprite'ları, kart düzeni), P0-b (1)-(3) |
| **P0-d** Geri bildirim | K21 kenar uyarısı (durum makinesi, boşta kapalı); hasar haptiği; düşük can ("!" işareti + kenar nabzı + tek haptik + histerezis); K20 olay bandı sade (tier yukarı, boss/final geldi, evrim ortak banda; §8.2 P0 kuralları); boss barına ad | P0-a (`yukari` sprite'ı, daire sprite'ı), P0-b (4)-(5). Kenar uyarısı, hasar haptiği ve düşük can P0-b'den önce başlayabilir (yalnız BonusShield girdisi ve boss bandı/adı bekler) |

**Sıra:** P0-a ve P0-b paralel → P0-c ve P0-d (birbirinden bağımsız, herhangi sırayla).

**P1 sonrası (özet):** boss barı A bölgesine taşıma + kayarak giriş; düşük canda bar nabzı; band birleştirme/kesme/öncelik (§8.2 P1); tier noktaları, punch, düşme bandı; hayalet bar; kalkan barı düzeltmeleri; kalp/kalkan/coin ikonları; boss oku; XP parlaması; kartsız seviye ve boss düştü bandları; Bölünme/Kara Delik rozetleri; öfke işareti; titreşim/sarsıntı ayarı; skill ikonları.

---

## 17. Açık sorular

| Soru | Sahibi | Çözüm |
|---|---|---|
| Boss barı haber bandının yerine mi geçsin (P1)? | Bahar | — |
| Slot şeridi 72 px ile ilk cihaz testinde kalabalık mı? (Gerekirse 64 + sakinlik soluklaşması) | Bahar + playtest | — |
| Tier alt satırlarının tonu (§8.1) | Bahar | — |
| Bölünme tetiklenince oyun 0.5 sn ağır çekime girsin mi (okunurluk)? Oynanış kararı | Bahar + game-designer | — |
