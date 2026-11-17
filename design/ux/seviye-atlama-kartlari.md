# UX Spec: Seviye Atlama Kartları

> **Durum**: Tasarımda (In Design)
> **Yazan**: Bahar + ux-designer
> **Son güncelleme**: 2026-11-15
> **Yolculuk aşaması**: Run içi, her seviye atlamada ve elit/boss sandığında (~run başına 10-20 kez)
> **Ekran adı**: `UpgradePanel` (kod: `Assets/_Project/Scripts/UI/UpgradePanel.cs`)
> **Platform**: iOS / Android, dikey, dokunmatik, tek el. Editor'da fare.
> **İlgili belgeler**: `SKILLS.md` §1-3 · `OYUN.md` §5, §10 · `design/ux/interaction-patterns.md` K1-A, K3, K4, K5, K7, K15 · `design/ux/hud.md` (K19 slot şeridi, K23 tür şekli) · `design/accessibility-requirements.md`
> **Erişilebilirlik seviyesi**: Temel

**Birim:** ref px (1080x1920, genişlik eşli). **Öncelik:** P0 mutlaka · P1 önemli · P2 cila.

**Kullanılan kalıplar:** K1-A (kök Canvas tam ekran), K2 (oyun durur), K3 (kart seçimi; genişletiliyor), K4 (rozet dili; değişiyor), K5 (gölgeli başlık), K7 (punch). **Yeni** (`interaction-patterns.md`): K19 slot şeridi ve K23 tür şekli kodlaması (tanımları orada), **K24 Seçim onay animasyonu** (tanımı §10), **K25 Seviye noktaları** (tanımı §5.3/§12). P0 paketleri (P0-a…d): `hud.md` §16. Glifler: interaction-patterns §3 glif kuralı.

---

## 1. Amaç ve oyuncu ihtiyacı

Oyuncu run'ının yönünü burada belirler ve **geri alamaz**: skill bırakılamıyor, slot run boyunca kalıcı (3 aktif + 3 pasif). Bugünkü ekran oyuncuya kartın ne olduğunu söylüyor ama **elindekilerle ilişkisini** söylemiyor: hangi slotları dolu, bu kart son boş slotu mu yakacak, bu seviye yeni bir yetenek mi (⭐) yoksa sayı artışı mı, evrime ne kadar yakın. Oyuncu bunları kafasında tutmak zorunda; 20 dakikalık bir run'da tutamıyor.

**Oyuncu hedefi:** 3 karttan hangisinin run'ına en çok katkı vereceğini 2-5 saniyede, slot durumunu ve kartın getirdiğini görerek seçmek.

**Oyun hedefi:** seçimi almak (`RaiseUpgradeSelected`), seçimin anlamını (yeni slot / seviye / ⭐ / evrim) hissettirmek ve oyuncuyu ritmi bozmadan oyuna geri koymak.

---

## 2. Varıştaki oyuncu bağlamı

| Soru | Cevap |
|---|---|
| Ne yapıyordu? | Sürüklüyor, yiyor; çoğu zaman polislerden kaçarken. Parmak hâlâ ekranda olabilir |
| Duygu durumu | Yüksek tempo → aniden donma. Hafif şaşkınlık + ödül beklentisi |
| Bilişsel yük | Yüksek: kaçış rotası, can, yakındaki boss. Ekran açılınca bunları bırakması gerekiyor |
| Bildiği | Hangi skill'leri seçtiğini kabaca; slot sayılarını genelde **bilmiyor** |
| Ne yapmak istiyor | Hızlıca iyi bir kart seçip oyuna dönmek |
| Korktuğu | Yanlış karta yanlışlıkla basmak (sürüklerken), bir slotu boşa harcamak, uzun açıklamayı okuyamamak |

**Duygu hedefi:** "Bir ödül seçiyorum ve ne aldığımı biliyorum": kısa, net, tatmin edici bir tık.

---

## 3. Gezinme konumu

```
Run (GameState.Playing)
  └── Seviye Atlama Kartları (GameState.LevelUp, timeScale 0)
        └── (sırada başka seviye varsa) aynı ekran yeniden dolar
```

**Modal:** Oyunun üstünde tam ekran kaplama, oyun durur (K2). **Kapatılamaz**: bir kart seçmek zorunlu (bugünkü tasarım; "atla" yok, gerekçe: her seçim güçlendirir, kaybı yok). Duraklatma tuşu bu ekranda gizli/etkisiz (`BeginLevelUp` `OnGamePaused` atmaz). Uygulama arka plana gidince ekran olduğu gibi kalır, dönüşte aynı kartlar.

| Giriş | Tetik | Not |
|---|---|---|
| Seviye atlama | `OnUpgradeChoicesReady` (XP eşiği) | En sık |
| Elit / boss sandığı | Sandık açıldı → ekstra kart ekranı | Başlık farkı §6 (açık soru) |
| Ardışık seviye | Önceki seçim sonrası kuyrukta seviye var | Aynı çağrıda yeniden açılır |

---

## 4. Giriş ve çıkış

**Giriş:**

| Tetik | Kaynak | Geçiş | Veri | Not |
|---|---|---|---|---|
| XP eşiği | Playing | Arka plan 0.2 sn kararır + kartlar sırayla (§10) | 1-3 `UpgradeData`, mevcut seviyeler, slot durumu, oyuncu seviyesi | |
| Sandık | Playing | aynı | aynı + kaynak = sandık | |
| Kuyruktaki seviye | Bu ekran | Panel hiç kapanmaz; arka plan aynen kalır, kartlar yeniden sırayla gelir (§10) | aynı | "+1 daha" göstergesi P2 |

**Çıkış:**

| Eylem | Hedef | Geçiş | Veri | Not |
|---|---|---|---|---|
| Karta dokun | Playing (ya da kuyruktaki ekran) | K24: 150 ms onay → olay → (kuyruk yoksa) 150 ms panel sönmesi (§10) | `RaiseUpgradeSelected(data)` 150 ms'de | Tek çıkış |
| Ölüm / oyun sonu olayı | Oyun sonu | Anında kapanır (mevcut `OnGameOver`) | — | Normalde olmaz (oyun durmuş) |

---

## 5. Düzen

### 5.1 Tel kafes (1080 genişlik, 16:9 = en kısa durum)

```
╔════════════════════════════════════════════════════╗  +960
║               SEVİYE ATLADIN!          (96, altın) ║  y 775
║               Sv. 8 · Bir güç seç          (40)    ║  y 685
║   AKTİF 2/3              PASİF 3/3                 ║  y 610 sayaç (32)
║   (VK3)(SP2)(  )   [SN5][HZ1][RJ2]   <BÖ>          ║  y 520 K19, slot 96
║ ┌────────────────────────────────────────────────┐ ║  440
║ │ Vakum                            [Sv. 3 → 4]   │ ║  KART 1 y 270 (920x340)
║ │                                  (o) AKTİF     │ ║
║ │ Menzil 2.8, çekme hızı x1.5                    │ ║
║ └────────────────────────────────────────────────┘ ║  100
║ ┌────────────────────────────────────────────────┐ ║  68
║ │ Kaygan Zemin                         [YENİ]    │ ║  KART 2 y -102
║ │                                  (o) AKTİF     │ ║
║ │ 2 sn iz bırakır, izdekiler %30 yavaş           │ ║
║ │ /!\ Son aktif slot (3/3)                       │ ║  slot notu
║ └────────────────────────────────────────────────┘ ║  -272
║ ┌────────────────────────────────────────────────┐ ║  -304
║ │ Rejenerasyon                     [Sv. 2 → 3]   │ ║  KART 3 y -474
║ │                                  [■] PASİF     │ ║
║ │ 3 sn hasar almazsan 2 katı                     │ ║
║ └────────────────────────────────────────────────┘ ║  -644
║                     +1 daha (P2)                   ║  y -720
╚════════════════════════════════════════════════════╝  -960
```

`(o)` / `[■]` = K23 şekil Image (daire / yuvarlak kare), `/!\` = `uyari` sprite'ı. İkon ve seviye noktaları P1 (aşağıda).

**P0 konumları** (P0-a; panel ortasına göre y, merkez pivot, 1080x1920 ref; mevcut düzen gibi ortaya anchor'lı):

| Öğe | Boyut | y (merkez) | Kapladığı | Not |
|---|---|---|---|---|
| Başlık | 96 px, kutu 1000x125 | 775 | 712 … 838 | Bugün 640 |
| Alt başlık | 40 px, kutu 900x60 | 685 | 655 … 715 | Bugün 540 |
| Slot sayacı | 32 px kalın, iki etiket 300x40 | 610 | 590 … 630 | "AKTİF x/3" aktif grubun sol kenarında (x −432), "PASİF y/3" pasif grubun sol kenarında (x −104), sol hizalı |
| Slot şeridi (K19) | slot 96, genişlik 856 | 520 | 472 … 568 | x −432 … 424: aktif −432…−128, pasif −104…200, nadir 224…320, evrim 328…424 |
| Kart 1 | 920x340 | 270 | 100 … 440 | Şeritle arası 32 |
| Kart 2 | 920x340 | −102 | −272 … 68 | Kart arası 32 |
| Kart 3 | 920x340 | −474 | −644 … −304 | |
| Kuyruk göstergesi (P2) | 36 px | −720 | | |

**Kart içi** (kart merkezine göre): ad (−140, 112) kutu 620x80, sol hizalı; rozet hapı (330, 114) 220x64; tür etiketi (330, 50) kutu 220x40 (32x32 şekil + "AKTİF" 32 kalın); açıklama (0, −40) kutu 840x150, 3 satır; slot notu (0, −140) kutu 840x44. P0'da kart ikonu yok (ad sol kenardan başlar); ikon P1'de gelince ad kutusu sağa kayar ve auto-size 44'e iner.

**Sığma kontrolü:** 16:9 (iPhone SE sınıfı, 1920 ref, üst durum çubuğu ~55 ref): başlık üstü 838 < 905, kart 3 altı −644, alt boşluk 316 ≥ 120. 19.5:9 (~2340 ref, ±1170, safe üst ~130 / alt ~94 ref): içerik ortada kalır, üst ve alt boşluk eşit büyür; başlık safe area'nın 200 ref altında. Kartların orta-alta pinlenmesi (başparmak) P1 yerleşim kuralı.

### 5.2 Bölgeler

| Bölge | İçerik | Boyut | Kaydırma | Taşma |
|---|---|---|---|---|
| Başlık | Başlık + alt başlık (K5) | %100 x ~%15 | Yok | Auto-size (alt 64) |
| Slot şeridi | K19 büyük sürüm + "AKTİF x/3 · PASİF y/3" | %100 x ~%9 | Yok | Sabit 6+2 slot |
| Kartlar | 1-3 kart, dikey yığın | 85% x ~%55 | Yok | Kart metni auto-size (§5.3) |
| Alt | Kuyruk göstergesi | — | Yok | — |

**Yerleşim kuralı (P1):** başlık safe area üstüne, kart yığını safe area altına (alt boşluk ≥ 120) pinlenir; şerit başlığın altına. P0'da §5.1 sabit konumları kullanılır.

### 5.3 Bileşenler

| Bileşen | Tür | Bölge | Amaç | Öncelik | Yeniden kullanım |
|---|---|---|---|---|---|
| Arka plan | Image, gece mavisi alfa 0.75 | Tam ekran | Dünyayı bastırır | — | K1-A (alfa 0.72 → 0.75, tek token önerisi) |
| Başlık | TMP 96 altın + gölge | Başlık | Olayı duyurur | — | K5 |
| Alt başlık | TMP 40 | Başlık | "Sv. N · Bir güç seç" | P0 | K3 |
| Slot şeridi | 6 slot + nadir + evrim, slot 96 px | Şerit | Elindekiler | P0-c | **K19** (HUD ile aynı bileşen) |
| Slot sayacı | TMP 32 kalın | Şerit | "AKTİF 2/3", "PASİF 3/3" | P0-c | Yeni |
| Kart | Button + Image 920x340 | Kartlar | Seçim hedefi | P0-a boyut | K3 |
| Kart ikonu | Image 140x140 + K23 çerçevesi (ikon yokken kısaltma 56 px) | Kart sol | Tanınma | P1 | **K23** |
| Kart adı | TMP 58 kalın, auto-size 44-58, tek satır, kutu 620 | Kart | — | P0-a | K3 |
| Seviye rozeti | Hap 220x64, TMP 34, auto-size 28-34 | Kart sağ üst | YENİ / Sv. a → b / EVRİM | P0-a boyut, P0-c metin | K4 |
| Tür etiketi | Şekil Image 32 + TMP 32 | Rozet altı | AKTİF / PASİF / NADİR | P0-a boyut, P0-c şekil | K23 |
| Seviye noktaları (K25) | 5 nokta 22x22 | Tür etiketi yanında | Mevcut + kazanılacak seviye | P1 | **K25** |
| Kilometre taşı çipi | `yenilik` sprite + TMP 32 altın "YENİ YETENEK" | Tür satırı | Yeni mekanik getiren seviye | P1 | Yeni; işaret ★'dan (evrim) ayrı |
| Açıklama | TMP 36, sarar, auto-size 32-36, en fazla 3 satır | Kart | Sonraki seviye ne verir | P0-a | K3 |
| Slot notu | TMP 32 | Kart altı | Slot etkisi (§6) | P0-c | Yeni |
| Kuyruk göstergesi | TMP 36 alfa 0.85 | Alt | "+1 daha" | P2 | Yeni |

**Açılışta odak:** yok (dokunmatik). Klavye/gamepad (ileride PC): ilk kart.

### 5.4 Bilgi hiyerarşisi

1. **Kartın adı + ikonu** (ne): en büyük, sol üstte; göz Z düzeninde önce buraya.
2. **Rozet** (yeni mi / kaçıncı seviye): sağ üst, ad ile aynı satır.
3. **Tür** (P1: + seviye noktaları + kilometre taşı çipi): rozet altı.
4. **Açıklama** (ayrıntı): yalnız kararsız kalan okur.
5. **Slot notu** (sonuç): yalnız önemliyse görünür (son slot, slot harcamaz).
6. **Slot şeridi** (bağlam): kartların üstünde, karar öncesi bir bakış.

---

## 6. Durumlar ve varyantlar

**Rozet ve slot notu kuralları** (P0-c; K4: kazanılan seviye görünür):

| Kart durumu | Rozet | Tür satırı (K23) | Slot notu |
|---|---|---|---|
| Yeni aktif, boş slot > 1 | `YENİ` yeşil + koyu mürekkep | daire AKTİF | — (gösterilmez) |
| Yeni aktif, **son boş slot** | `YENİ` | daire AKTİF | `<sprite uyari> Son aktif slot (3/3)` turuncu |
| Yeni pasif (aynı kurallar) | `YENİ` | kare PASİF | `<sprite uyari> Son pasif slot (3/3)` |
| Sahip olunan, seviye | `Sv. 2 → 3` mavi | tür (P1: + noktalar) | — |
| Sahip olunan, **max'a çıkıyor** | `Sv. 4 → MAX` altın + koyu mürekkep | tür | — |
| Sahip olunan, kilometre taşı seviyesi (P1) | `Sv. 2 → 3` | + `YENİ YETENEK` çipi | Açıklama yeteneğin adıyla başlar, ad altın |
| **Sopa Sv.0** | `Sv. 0 → 1` mavi | daire AKTİF | `Aktif slot kullanır`. Sopa **son boş aktif slotu** dolduracaksa yerine son slot uyarısı gösterilir (geri alınamaz bilgi kazanır) |
| Nadir (Bölünme) | `YENİ` (tek seviyeli, hep yeni) | elmas NADİR | `Tek kullanımlık · Slot harcamaz` |
| Evrim (Kara Delik) | `EVRİM` altın | (yok) | `Vakum 5 + Sindirim 5 birleşir · Slot harcamaz`. Şeritte iki girdi slotunun altın parlaması P1 |
| Evrime giden skill, max'a çıkıyor ve diğer girdi max | `Sv. 4 → MAX` | | `★ Kara Delik açılır` altın (P2) |

Rozetteki ok `<sprite name="ok">`. "Sopa Sv.0" kararı `UpgradeData` temel silah bayrağından (P0-b), tür sınıfı tahmininden değil.

**Ekran durumları:**

| Durum | Tetik | Görsel | Davranış | Not |
|---|---|---|---|---|
| Açılış | Giriş | Arka plan 0.2 sn, kartlar sırayla (K3) | İlk 0.4 sn dokunma kapalı | Mevcut |
| Dolu (3 kart) | Normal | §5.1 | Hepsi seçilebilir | |
| 2 kart / 1 kart | Havuzda az kart | Kartlar dikey ortalanır, aralık aynı | | Mevcut davranış doğrulanmalı |
| Basılı | Parmak kartta | Kart ölçek 0.97 + basılı ton (0.8,0.8,0.9) | Parmak karttan kayarsa iptal (UGUI varsayılanı) | P1 ölçek |
| Seçildi (K24) | Parmak kaldırıldı | §10 zaman çizelgesi 0-150 ms | Tüm kartlar dokunmaya kapalı | P0-c (bugün anında kayboluyor) |
| Kapanış | 150 ms'de olay atıldı, kuyruk yok | Panel 150 ms alfa 1→0, dokunmayı geçirir | Oyun 150 ms'de akar | §10 |
| Kuyrukta seviye | 150 ms'de olay atıldı, sırada seviye var | Panel ve arka plan açık kalır; alt başlık "Sv. 9 · Bir güç daha seç", kartlar yeniden sırayla, şeritte yeni slot K19 animasyonu | 0.4 sn koruma yeniden | "+1 daha" göstergesi P2 |
| Sandık kaynağı | Sandık | Başlık `SANDIK!` (öneri), alt başlık "Bir güç seç" | Aynı | Açık soru §15 |
| Kartsız seviye | Havuz boş | Bu ekran **açılmaz**; HUD olay bandı "Sv. N · +%30 CAN" | — | hud.md §8 |

---

## 7. Etkileşim haritası

### 7.1 Gezinme

| Giriş | Platform | Eylem | Görsel | Haptik | Not |
|---|---|---|---|---|---|
| Dokun-bırak (kart) | Dokunmatik | Seç | K24 | Orta `Impact(1)` seçimde (P0-c); hafif `Impact(0)` basışta (P1) | Ses yok |
| Basılıyken kaydır-çık | Dokunmatik | İptal | Basılı durum kalkar | — | UGUI |
| Kart dışına dokun | Dokunmatik | Yok | — | — | Yanlış kapanma yok |
| Fare tıklama | Editor | Seç | aynı | — | |
| Ok tuşu + Enter / Space | Klavye (ileride PC) | Kart arası / seç | Odak = basılı ton + altın çerçeve 3 px | — | P2, PC portunda |

### 7.2 Duruma özel kısıtlar

| Durum | Kısıt | Neden |
|---|---|---|
| İlk 0.4 sn | Kartlar `interactable=false`, görünüm değişmez | Sürüklerken açılan ekranda yanlış seçim (mevcut, korunur) |
| Açılışta parmak ekrandaysa | O parmağın bırakılması seçim yapmaz (basış karttan başlamadı; UGUI varsayılanı). Cihazda doğrulanır; tutmazsa: basışı panel açılmadan önce başlamış ilk pointer-up yok sayılır | Sürüklerken açılan ekran |
| K24 süresince (0-150 ms) ve panel sönmesi | Tüm kartlar `interactable=false`; sönmede panel `blocksRaycasts=false` | Çift seçim; oyuna dönen parmak panele takılmasın |
| Duraklatma | Tuş gizli/etkisiz | Mevcut |

---

## 8. Veri ihtiyacı

| Veri | Kaynak | Ne zaman | Sahibi | Biçim | Eksikse |
|---|---|---|---|---|---|
| Kart listesi | `OnUpgradeChoicesReady` | Açılışta | `UpgradeSystem` | `UpgradeData[]` 1-3 | Ekran açılmaz |
| Mevcut seviye | `UpgradeSystem.GetLevel(data)` | Açılışta | `UpgradeSystem` | int 0-5 | 0 |
| Max seviye | `UpgradeData.MaxLevel` | Açılışta | data | int | 5 |
| Tür | `UpgradeData.Kind` + `IsEvolution` | Açılışta | data | enum | — |
| Sonraki seviye açıklaması | `GetDescriptionForLevel(next)` | Açılışta | data | string | Boş |
| **Kilometre taşı seviyesi mi** (P1) | **yeni alan:** `UpgradeData` seviye başına bayrak (`IsMilestone(level)`) + yetenek adı | Açılışta | data (`SkillSetSetup`) | bool + string | Çip gösterilmez |
| **Kısaltma** (P0-b) / ikon (P1) | **yeni alan:** `UpgradeData._abbr` (2 harf, `hud.md` §4.3), `_icon` (Sprite) | Açılışta | data | string / Sprite | Adın ilk 2 harfi |
| **Temel silah** (P0-b) | **yeni alan:** `UpgradeData` bayrağı (Sopa) | Açılışta | data (`SkillSetSetup`) | bool | — |
| **Slot durumu** (P0-b) | **yeni:** `UpgradeSystem` alım sırasına göre aktif/pasif listeleri + mevcut `ActiveSlots` / `PassiveSlots` | Açılışta ve `OnUpgradeApplied`'da | `UpgradeSystem` | 2 sıralı liste + 2 int | Şerit gizlenir |
| Oyuncu seviyesi | `OnLevelUp` son değer | Açılışta | `BlobGrowth` | int | Alt başlık "Bir güç seç" |
| Giriş kaynağı (seviye / sandık) | **yeni:** olay parametresi | Açılışta | `UpgradeSystem` | enum | Seviye varsayılır |
| Kuyruk sayısı | **yeni sorgu** | Açılışta | `UpgradeSystem` | int | Gösterilmez |

Ekran hiçbir sisteme doğrudan yazmaz; yalnız olay atar.

## 9. Atılan olaylar

| Eylem | Olay | Veri | Alıcı | Not |
|---|---|---|---|---|
| Kart seç | `GameEvents.RaiseUpgradeSelected` | `UpgradeData` | `UpgradeSystem` → `OnUpgradeApplied` → HUD şeridi, evrim bandı | Mevcut; analitik zaten seçilen kartları yazıyor |

---

## 10. Geçişler ve animasyonlar

Hepsi ölçeksiz zaman (K2).

| Geçiş | Tetik | Tür | Süre | Easing | Kesilebilir | Öncelik |
|---|---|---|---|---|---|---|
| Arka plan girişi | Açılış | Alfa 0 → 0.75 | 200 ms | ease-out | Hayır | P1 (bugün anında) |
| XP bar parlaması görünür kalır | Açılış | Arka plan kararırken HUD'daki parlama 0.15 sn görülür | 150 ms | — | — | P1 (hud.md) |
| Kart belirme | Açılış | Alfa 0→1, ölçek 0.9→1, sırayla 70 ms | 220 ms/kart | ease-out kare | Hayır | Mevcut |
| Basılı | Parmak bastı | Ölçek 1→0.97 | 80 ms | linear | Evet | P1 |
| Kuyruk yenilemesi (kartlar önce söner) | Sırada seviye | Kartlar 100 ms söner, sonra yeniler gelir | +100 ms | | Hayır | P1 (P0'da kartlar doğrudan yeniden belirir) |

**K24 seçim zaman çizelgesi (P0-c, tek kaynak)** — t = seçilen kartta pointer-up (ms, ölçeksiz):

| t | Olay | Ekranda | Oyun |
|---|---|---|---|
| 0 | Seçim alınır; tüm kartlar `interactable=false`; orta haptik | Seçilen: punch 1→1.05→1 (0-120, sin) + 6 px altın çerçeve. Diğerleri: alfa 1→0.35, ölçek 1→0.95 (0-150, ease-out). Slot şeritleri **değişmez** (önizleme yok) | Durmuş (LevelUp) |
| 150 | `RaiseUpgradeSelected(chosen)` atılır. Panel **kapatılmadan** (SetActive(false) yok) atılır. Senkron zincir: `UpgradeSystem` uygular → `OnUpgradeApplied` → kart ve HUD şeritleri güncellenir, ilgili slotun K19 animasyonu (fade 300 / punch 220 ms) başlar → kuyruk varsa aynı çağrıda `OnUpgradeChoicesReady` → `Show()` | — | Kuyruk yoksa `ResumeGame` → Playing |
| 150 → (kuyruk var) | `Show()` açık paneli yeniden doldurur: seçim görselleri sıfırlanır, alfa 1, arka plan **dokunulmaz** (giriş fade'i yalnız panel kapalıyken oynar) | Alt başlık "Bir güç daha seç"; kartlar sırayla (70 ms aralık, 220 ms pop, 150-510); kart şeridinde yeni slot animasyonu görünür | Durmuş |
| 150-300 (kuyruk yok) | `Show()` çağrılmadıysa panel CanvasGroup alfa 1→0 (ease-in), `blocksRaycasts=false` | HUD şerit animasyonu panel kalkarken görünür; band kuyruğu (evrim dahil) state Playing olduğu için başlar | Akıyor |
| 300 (kuyruk yok) | `SetActive(false)` | — | Akıyor |
| 550 (kuyruk var) | 0.4 sn koruma biter, kartlar dokunulabilir | — | Durmuş |

- **Toplam:** dokunuştan oyuna 150 ms; panel 300 ms'de tamamen kalkar. Kuyrukta panel ve arka plan tek kare bile kapanmaz.
- `OnGameOver` bu süreçte herhangi bir anda gelirse panel anında kapanır (mevcut).
- Uygulama: `SelectUpgrade` bugünkü "önce `Hide()`, sonra olay" sırası yerine: 150 ms bekle → "`Show` çağrıldı" bayrağını sıfırla → olay → bayrak kapalıysa sönmeyi başlat. Kuyruk mantığı `UpgradeSystem`'de değişmez.

---

## 11. Giriş yöntemi kontrol listesi

**Dokunmatik**
- [x] Tüm hedefler ≥ 120 ref px (kart 920x340)
- [x] Sistem kaydırma hareketleriyle çakışma yok (yalnız dokunma)
- [x] Tek el, dikey
- [x] Uzun basma kullanılmıyor (tanımsız değil: basılı tutup bırakmak = seçim)

**Klavye / gamepad:** şu an kapsam dışı (mobil). PC portunda: ↑↓ odak, Enter seç, odak görünür. UGUI `Navigation` dikey açık. (P2)

**Fare (Editor):** tıklama = dokunma. Üzerine gelme durumu tanımsız ve gerekmez.

---

## 12. Erişilebilirlik

**Kontrast:**

| Metin | Zemin | Gerek | Not |
|---|---|---|---|
| Kart adı beyaz 58 kalın | Kart (0.05,0.05,0.17) alfa 0.88 → **0.94** | 3:1 | Şehir kartın arkasından sızmasın diye alfa artar (P1) |
| Açıklama beyaz 36 | aynı | 4.5:1 | Beyaz alfa ≥ 0.9 |
| Tür etiketi 32 renkli | aynı | 4.5:1 | Hesap (kart arkasında beyaz zemin en kötü hali): turuncu ~5.8:1, açık mavi ~6:1, pembe ~5.4:1. Geçer; cihazda ölçülerek doğrulanır |
| Rozet 34 kalın (büyük metin sayılmaz) | Yeşil (0.35,0.75,0.4) | 4.5:1 | Beyaz ~2.3:1 **geçmez** → `YENİ` rozeti **koyu mürekkep** (~7.8:1). P0-c |
| Rozet 34 kalın | Mavi (0.25,0.55,1) | 4.5:1 | Beyaz ~3.3:1 **geçmez** → koyu mürekkep (~5.5:1) ya da mavi (0.15,0.38,0.85)'e koyulaşır, beyaz kalır (~5.5:1). Hangisi: art-director; ikisi de geçer. P0-c |
| Rozet koyu mürekkep | Altın | 4.5:1 | Geçer |
| Slot notu turuncu 32 | Kart | 4.5:1 | ~5.8:1, geçer |

**Renk riski ve çözüm:**

| Öğe | Risk | Çözüm |
|---|---|---|
| Tür (aktif/pasif/nadir) | Turuncu/mavi/pembe | Metin + şekil Image (daire / yuvarlak kare / elmas), ikon çerçevesi aynı şekil (K23) |
| Rozet (yeni/sahip/evrim) | Yeşil/mavi/altın | Metin |
| Nadir/evrim kart zemini | Pembe/altın ton | Rozet + ★ |
| Seviye noktaları | Dolu/boş | Dolu = dolgu, boş = sadece kontur, kazanılacak = kontur + nabız |
| Son slot uyarısı | Turuncu | `uyari` sprite'ı + metin |

**Boyut (P0-a):** tür etiketi 26 → 32, rozet 30 → 34, açıklama auto-size alt sınır 32. Bugün `NoWrap/Overflow` ile taşan metin (K3 sorun 1) auto-size + sarma ile biter.

**Bilişsel yük:** kart başına 4 parça (ad/ikon, rozet, tür, açıklama) + nadiren 5. (slot notu). 3 kart + şerit ≈ 7 akış: sınırda. Hafifletme: açıklama ikincil (küçük, normal kalınlık); slot notu yalnız önemliyse görünür; seviye noktaları sayı okumayı gereksizleştirir.

**Ekran okuyucu:** kapsam dışı (Temel).

---

## 13. Yerelleştirme

Oyun yalnız Türkçe. Yine de: tüm metinler sabit listeden (§16), görselde yazı yok. Türkçe büyük harf `tr-TR` kültürüyle (`i → İ`; mevcut evrim bandı kuralı). En uzun ad: "Hızlı Metabolizma" (17 karakter): 58 px'te ~560 ref px, 720 kutu + rozet 220 ile çakışır → auto-size 44'e iner ya da ad kutusu 620'ye daralır (rozetle çakışmaz).

| Metin | En uzun örnek | Sınır | Taşma |
|---|---|---|---|
| Kart adı | Hızlı Metabolizma | ~18 kr | auto-size 44-58 |
| Açıklama | Yetenek adı + iki cümle | 3 satır | auto-size 32-36, sonra kısaltma SkillSetSetup'ta |
| Slot notu | Vakum 5 + Sindirim 5 birleşir · Slot harcamaz | 1 satır | 2 satıra sarar |
| Rozet | Sv. 4 → MAX | 220 px | auto-size 28-34 |

---

## 14. Kabul ölçütleri

**Performans**
- [ ] Panel ilk karesi tetikten sonraki karede; tüm kartlar ≤ 0.4 sn'de tam görünür.
- [ ] Açılış ve seçimde Instantiate/Destroy yok; kartlar ve şerit `Build()`'de bir kez kurulu (K18).
- [ ] Açılışta tek sefer string üretimi; animasyon karelerinde string yok.

**P0-a: düzen ve metin**
- [ ] §5.1 konumlarıyla 1080x1920 (16:9) ve 1080x2340 (19.5:9) safe area ile kesilme/çakışma yok; şerit yeri (y 472-630) boş ve kartlarla çakışmıyor.
- [ ] "Hızlı Metabolizma" adı rozetle çakışmıyor; en uzun açıklama 3 satırda kart içinde kalıyor.
- [ ] Hiçbir metin 32 ref px altında değil (seviye hapı rakamı istisnası).
- [ ] İlk 0.4 sn dokunma seçim yapmıyor; sürüklerken açılan ekranda parmağı bırakmak seçim yapmıyor (cihaz testi).

**P0-c: şerit, rozet, seçim**
- [ ] Slot şeridi açılışta doğru: alım sırası, seviyeler, boş slotlar, "AKTİF x/3", "PASİF y/3".
- [ ] Son boş slotu dolduracak yeni skill kartında (Sopa Sv.0 dahil) son slot uyarısı var; diğerlerinde yok.
- [ ] Sopa Sv.0 kartında "Sv. 0 → 1" ve (son slot değilse) "Aktif slot kullanır".
- [ ] Sahip olunan skill rozeti "Sv. a → b"; max'a çıkışta "Sv. 4 → MAX"; ok sprite'ı görünüyor (kare kutu yok).
- [ ] YENİ ve Sv. rozet metni ≥ 4.5:1 kontrast.
- [ ] Nadir kartta "Tek kullanımlık · Slot harcamaz".
- [ ] Tür her kartta metin + şekil ile; gri tonlamalı ekran görüntüsünde ayırt edilebiliyor.
- [ ] Seçimde orta haptik; K24 §10 çizelgesiyle: oyun dokunuştan 150 ms sonra akıyor, panel 300 ms'de kalkıyor; bu sürede ikinci dokunuş yok sayılıyor; panel sönerken sürükleme oyunu hareket ettiriyor.
- [ ] Kuyruktaki seviyede panel ve arka plan hiç kapanmıyor (kare kare kayıt); alt başlık yeni seviyeyi yazıyor; kart şeridinde yeni slot animasyonu görünüyor.
- [ ] Restart sonrası şerit yeni run'ı gösteriyor (başlangıç skill'i dahil).

**P1**
- [ ] Kilometre taşı seviyelerinde "YENİ YETENEK" çipi; SKILLS.md tablosundaki ⭐ seviyelerle birebir.
- [ ] Evrim kartında girdi slotları şeritte parlıyor.

---

## 15. Açık sorular

| Soru | Sahibi | Çözüm |
|---|---|---|
| Sandık kart ekranının başlığı ayrı mı ("SANDIK!")? Bugün kod ayırt etmiyor | Bahar | — |
| Kart ekranı "atla" seçeneği olmadan kalsın mı? (Öneri: evet) | Bahar + game-designer | — |
| "Şu an → sonraki" sayısal karşılaştırma (ör. "Menzil 2.0 → 2.8") istensin mi? Her effect'e veri gerekir (P2) | Bahar | — |
| Arka plan alfası 0.75 tek token olsun mu (0.7/0.72/0.85 birleşsin)? | Bahar | — |
| Nadir rengi: belgede pembe (kod), SKILLS.md'de "mor" yazıyor; hangisi? | Bahar | — |

---

## 16. Sabit metinler (Türkçe)

| Anahtar | Metin |
|---|---|
| card.title | `SEVİYE ATLADIN!` |
| card.title.chest | `SANDIK!` (onay bekliyor) |
| card.subtitle | `Sv. {0} · Bir güç seç` |
| card.subtitle.again | `Sv. {0} · Bir güç daha seç` |
| card.subtitle.fallback | `Bir güç seç` |
| card.slots.active | `AKTİF {0}/{1}` |
| card.slots.passive | `PASİF {0}/{1}` |
| card.badge.new | `YENİ` |
| card.badge.level | `Sv. {0} <sprite name="ok"> {1}` |
| card.badge.toMax | `Sv. {0} <sprite name="ok"> MAX` |
| card.badge.evo | `EVRİM` |
| card.kind.active / passive / rare | `AKTİF` · `PASİF` · `NADİR` (şekil ayrı Image, K23) |
| card.milestone (P1) | `<sprite name="yenilik"> YENİ YETENEK` |
| card.note.lastActive | `<sprite name="uyari"> Son aktif slot (3/3)` |
| card.note.lastPassive | `<sprite name="uyari"> Son pasif slot (3/3)` |
| card.note.stickSlot | `Aktif slot kullanır` |
| card.note.rare | `Tek kullanımlık · Slot harcamaz` |
| card.note.evo | `{A} 5 + {B} 5 birleşir · Slot harcamaz` |
| card.note.evoUnlock | `★ {EVRİM} açılır` |
| card.queue | `+{0} daha` |

**Glifler:** metinde yalnız ASCII, Türkçe harfler, `·`, `★`; ok, uyarı ve kilometre taşı işareti `UI_Glyphs` sprite'ları (interaction-patterns §3). Kilometre taşı işareti ★'dan ayrı şekildir (★ = evrim).

---

## 17. Asset ihtiyacı

HUD ile ortak (`hud.md` §14): şekil sprite'ları (daire, yuvarlak kare, elmas; yıldız P1) ve `UI_Glyphs` (P0-a); 17 skill ikonu (P1, 256 PNG, atlas; kartta 140 px, ikon yokken şekilli çerçeve + kısaltma 56 ref px).

## 18. Öncelik özeti (uygulama sırası)

P0 paketleri ve bağımlılıkları `hud.md` §16'da. Bu ekranın payı:
- **P0-a:** kart 920x340 + §5.1 konumları (şerit yeri boş); tür etiketi 32, rozet hapı 220x64 / 34, ad auto-size 44-58 (kutu 620), açıklama auto-size 32-36 + sarma.
- **P0-b:** sıralı slot listeleri, `_abbr`, temel silah bayrağı (§8).
- **P0-c:** K19 şerit (96) + sayaç; K23 tür etiketi şekli; rozet "Sv. a → b" / "→ MAX" / "Sv. 0 → 1" + kontrast; slot notları (son slot, Sopa, nadir, evrim); K24 (§10) + seçim haptiği.
- **P1:** kart ikonları (geçici kısaltma ikonu dahil); seviye noktaları (K25); kilometre taşı çipi (veri alanı gerekir); evrim girdi parlaması; basışta ölçek + hafif haptik; arka plan girişi; safe area pinleme; kart alfası 0.94; kuyrukta kartların önce sönmesi.
- **P2:** "+1 daha"; evrim açılır notu; sayısal karşılaştırma; klavye/gamepad.
