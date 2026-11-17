# Etkileşim Kalıpları Kütüphanesi — Left No Crumbs

Durum: koddan çıkarıldı (Assets/_Project/Scripts/UI + Core + Entities). Uydurma kalıp yok; değerler koddaki sabitlerdir.
Kapsam: dikey ekran, dokunmatik, UGUI + TMP. Sonraki adım: oyun içi UI (HUD, skill/slot göstergesi, kart ekranı, geri bildirimler) yeniden tasarımı.

Kısaltmalar: `HUD` = HUDController.cs, `UP` = UpgradePanel.cs, `GO` = GameOverScreen.cs, `MK` = MarketPanel.cs, `LB` = LobbyPanel.cs, `PM` = PauseMenu.cs, `NT` = NewsTicker.cs, `TS` = TitleScreen.cs, `SA` = SafeAreaHandler.cs. Hepsi `Assets/_Project/Scripts/UI/` altında.

**Erişilebilirlik seviyesi:** **Temel** (`design/accessibility-requirements.md`): metin ≥ 32 ref px, dokunma hedefi ≥ 120 ref px, anlam tek başına renkle taşınmaz, titreşim/sarsıntı kapatılabilir. Koddaki sapmalar "Sorun" satırlarında.

K1-K18 koddaki kalıplardır; K19-K25 oyun içi UI tasarımıyla eklenen, henüz kodda olmayan kalıplardır (§1, K18'den sonra).

---

## 1. Kalıplar

### K1. Tam ekran panel (kapsama + safe area)
- **Ne zaman:** Oyunu bölen veya ekranı devralan her şey (kart seçimi, duraklatma, oyun sonu, lobi, Market, giriş).
- **Davranış:** Canvas: ScaleWithScreenSize, referans 1080x1920, match=0 (genişlik) (GameScene.unity ~53512). `SafeAreaHandler` sadece anchorMin/Max'i `Screen.safeArea`'ya çeker, değişince kare başı karşılaştırır (SA:16-36). İki varyant:
  - **A (kök Canvas'a taşı):** `CoverFullScreen` paneli `rootCanvas`'a taşır, tüm ekranı kaplar, `SetAsLastSibling`. İçerik ortada. Kullanan: UP, GO, PM (UP:222, GO:190, PM:84).
  - **B (safe area içinde + taşan arka plan):** panel safe area'da kalır, `ScreenBackdrop` -400/-600 px taşırır; başlık bloğu üste, alt düğme alta `PinToEdge` ile yapışır (LB:194-221, MK:255-267, 307). Kullanan: LB, MK. TS ise safe area'yı kurulumda bir kez elle okur (TS:121-126).
- **Görsel:** zemin gece mavisi, alfa 0.7 (LB/MK) / 0.72 (UP) / 0.85 (GO).
- **Mobil:** orta yerleşim 1080x1920'ye göre çizilmiş; PinToEdge `±960` sabitine dayanır (1920 yükseklik varsayımı).
- **Sorun:** (1) İki varyant bir arada. (2) UP kartları ve başlığı pinlenmiyor (sabit y 640/540/280/-70/-420), çok uzun/kısa ekranda test edilmedi. (3) TS safe area'yı canlı izlemiyor (SA izliyor). (4) Backdrop alfası 0.7/0.72/0.85 arasında gerekçesiz değişiyor.

### K2. Oyun durdurma (GameState + timeScale)
- **Ne zaman:** Duraklat, seviye atlama, oyun sonu, menü.
- **Davranış:** `GameManager` state + `Time.timeScale=0`: `PauseGame` (GameManager.cs:240), `BeginLevelUp` (294; `OnGamePaused` atmaz, böylece pause paneli kart ekranının üstüne açılmaz), `TriggerGameOver` (301), `TriggerRunComplete` (311). Panel animasyonları `Time.unscaledTime/unscaledDeltaTime` kullanır (UP:115, GO:90, HUD:126, MK:364). Uygulama arka plana gidince otomatik pause (OYUN.md §10).
- **Sorun:** `NewsTicker` oyun zamanıyla (`Time.deltaTime`, NT:97) çalışır: bilerek (pause'da durur, başlık 30 sn oyun süresi). Tutarlı ama kural belgelenmeli: **UI animasyonu = unscaled, oyun içeriği (bant) = scaled.**

### K3. Kart seçimi (seviye atlama, 3 kart)
- **Ne zaman:** Seviye atlayınca, oyun durur, 3 skill'den 1 seçilir.
- **Davranış (UP):** Kartlar sırayla belirir: kart başına gecikme `Stagger=0.07 s`, her biri `PopTime=0.22 s` boyunca alfa 0→1 ve ölçek 0.9→1 (ease-out kare) (UP:34-35, 123-133). `InputDelay=0.4 s` boyunca butonlar `interactable=false` (UP:105, 117-119); son kart 0.36 s'de tamamlanır, yani kartlar tam görününce giriş açılır. Dokunma anında panel gizlenir, `RaiseUpgradeSelected` atılır; sırada başka level-up varsa yeni kartlar aynı çağrıda açılır (UP:141-148). `OnGameOver`'da panel kapanır.
- **Görsel:** başlık "SEVİYE ATLADIN!" 96 pt altın + gölge, alt başlık "Bir güç seç" 40 pt normal. Kart 920x310, merkezden y = 280 - i*350 (40 px boşluk), yuvarlak 9-slice, renk: normal (0.05,0.05,0.17,0.88), evrim (0.24,0.17,0.03,0.92), nadir (0.26,0.06,0.16,0.92) (UP:21-24, 191). Ad 58 pt kalın sol, açıklama 36 pt, sağ üstte rozet (K4). Basılı renk (0.8,0.8,0.9). `disabledColor=white`, giriş gecikmesinde soluk görünmez (UP:199).
- **Mobil:** tüm kart tek dokunma hedefi (920x310 px = çok büyük, Fitts için iyi). 0.4 s koruma sürükleyen parmağın yanlış seçimini önler.
- **Sorun:** (1) Ad ve açıklama `NoWrap`/`Overflow` + sabit kutu (ad 720x80, açıklama 840x170); uzun ad sağ üst rozetle çakışabilir, uzun açıklama kart dışına taşar; `enableAutoSizing=false` (UP:205-209, StyleText:243-255). (2) Skill ikonu yok (OYUN.md §11 "Eksik: skill ikonları"). (3) Kartta mevcut skill durumu/slot doluluğu yok: oyuncu 3+3 slotun dolu olduğunu göremez. (4) Karşılaştırma yok (sayısal fark, "şu an -> sonraki"). (5) "Sv." (kart) ile HUD "Lv." tutarsız (bkz. §5). (6) 0.4 s sonrası ayrıca bir "seçim onayı" feedback'i yok; panel anında kaybolur.

### K4. Rozet ve etiket dili
- **Ne zaman:** Kartın durumunu tek bakışta söylemek (skill kartı, Market satırı, lobi kilidi, rekor).
- **Davranış / görsel (UP:99-105, 212-217):** Rozet 200x60, sağ üstte, 30 pt kalın beyaz, yuvarlak:
  - `YENİ` yeşil (0.35,0.75,0.4); sahip olunan mavi (0.25,0.55,1); `EVRİM` altın (1,0.82,0.25) üstüne koyu mürekkep (0.12,0.08,0.02).
  - **Sahip olunan rozet metni:** kazanılan seviye görünür: `Sv. 2 → 3`; max'a çıkışta `Sv. 4 → MAX`; Sopa Sv.0 kartında `Sv. 0 → 1` (P0-c, `seviye-atlama-kartlari.md` §6). Kodda bugün `Sv. x/5` yazıyor. Ok işareti TMP sprite (§3 glif kuralı).
  - Tür etiketi rozetin altında 26 pt: `AKTİF` turuncu (1,0.6,0.3), `PASİF` açık mavi (0.55,0.8,1), `NADİR` pembe (1,0.45,0.65); evrimde boş. Nadir kartın zemini de pembeye çalar.
  - Sopa (temel silah) Sv.0'da bile "YENİ" gösterilmez (UP:101).
  - Rozet metni = rozet rengi **birlikte** (renk tek başına taşımıyor): iyi.
- Aynı dil başka yerlerde: altın "YENİ REKOR!" rozeti 420x84 (GO:153), Market "Sv. x/5" altın metin (MK:350), lobi kilit rozeti altın (LB:250), yeşil "AÇIK/ALINDI/MAX" hapı (MK:134,177).
- **Sorun:** (1) Tür etiketi 26 pt ~ 9-10 pt cihaz boyutu (1080 ref -> ~390 pt genişlik, çarpan ~0.36): okunurluk sınırında (varsayım: Temel seviyede ihlal). (2) Mavi "Sv." rozeti Market'te altın, kartta mavi: aynı kavram iki renk. (3) Etiket renkleri (turuncu/mavi/pembe) yalnızca etiket metniyle anlam taşıyor, iyi; ama kart zemin farkı (nadir/evrim) renge dayanıyor, ikincil.

### K5. Gölgeli başlık
- **Ne zaman:** Ekran başlıkları, logo, banner (şehir/oyun üstünde okunurluk).
- **Davranış:** aynı metin iki kez: arkada koyu gölge (0.05,0.05,0.17,0.85), `y - fontSize*0.06` aşağıda; önde renkli (UP:281, MK:445, LB:305, TS:191). Outline/materyal kopyası yok, bilerek (sıfır maliyet). Banner farklı: gölge ofseti sabit -6 px (HUD:117).
- **Sorun:** `Shadowed` yardımcısı 4 dosyada kopyalanmış (UP, MK, LB, TS + HUD banner'ı ayrı).

### K6. Fiyat hapı + satın alma satırı (Market)
- **Ne zaman:** Coin'le karakter/kalıcı güç satın alma (lobiden Market).
- **Davranış (MK:87-113):** Satırın tamamı buton (karakter kartı 900x260, güç satırı 900x152). Sağda hap 200x84. Durumlar (MK:125-194):
  - yetiyor: hap altın + koyu mürekkep fiyat; yetmiyor: hap gri (0.32,0.32,0.36,0.9) + soluk (alfa 0.45) fiyat; alındı: hap yeşil (0.35,0.82,0.45) "AÇIK"/"ALINDI"/"MAX".
  - Run ile açılan karakter (Dalcık): hapta "x/y run" + 18 px altın ilerleme çubuğu; dokununca çubuk 1.08 zıplar (satılmaz).
  - Başarılı: satır zıplar (1.06) + coin rozeti zıplar (1.1). Yetersiz: coin rozeti **sallanır** ve kırmızıya (1,0.42,0.38) döner (K7).
  - Satın alma **tek dokunuş**, onay yok.
- **Mobil:** satır büyük; ama geri bildirim (sallanma) ekranın üstündeki coin rozetinde, parmağın olduğu yerde değil.
- **Sorun:** (1) Yetersiz-coin geri bildirimi satırdan uzak (rozet y=650, satırlar aşağıda), parmak altında görsel yanıt yok. (2) Soluk fiyat metni ~3:1 kontrast (tahmin; gri hap üzerinde alfa 0.45 beyaz), büyük metin için sınırda. (3) Tek dokunuşla pahalı satın alma onaysız; yanlış dokunma geri alınamaz. (4) Kilitli lobi kartına dokunmak hiçbir şey yapmıyor (LB:105), Market'teki gibi sallanma yok: aynı "yapamazsın" durumu iki farklı davranış.

### K7. UI "punch" ve "shake" mikro-animasyonları
- **Ne zaman:** Bir UI öğesine "bu etkileşim oldu / olmadı" demek.
- **Davranış (MK:361-386, unscaled, coroutine):** `Punch`: 0.22 s, `sin(t/d*pi)` ile ölçek 1 -> peak -> 1 (satır 1.06, rozet 1.1, ilerleme 1.08). `Shake`: 0.35 s, yatay `sin(t*60) * 14 px * (1-t/d)`, süre boyunca metin kırmızı.
- **Sorun:** Sadece Market'te; HUD'da (coin kazanımı, seviye) kullanılmıyor. Coroutine tabanlı: üst üste çağrılırsa ölçek yarışabilir (bitişte 1'e sıfırlanır; kabul edilebilir).

### K8. Sayan sayılar (oyun sonu)
- **Ne zaman:** Run sonu özeti.
- **Davranış (GO:28-29, 88-108):** Panel 0.28 s alfa 0→1 + ölçek 0.92→1 (ease-out), ardından 0.9 s boyunca skor, toplanan coin ve kazanılan coin 0'dan hedefe ease-out kare ile sayar (`N0`, kazanılan "+" önekli). Hedef: kazanılan = toplanan x (kazan 1.0 / ölüm 0.5). Süre/rekor sabit. Bittiğinde `_shownAt=-1`.
- **Sorun:** Sayma **atlanamaz** (ekrana dokununca bitmiyor); butonlar saymayla birlikte aktif. **HUD sayıları sayılmıyor**: skor/coin anında yazılıyor (HUD:240-250), yani sayan sayı kalıbı yalnız oyun sonunda var.

### K9. Oyun sonu ekranı
- **Ne zaman:** Ölüm (GAME OVER) ya da final boss yutuldu (KAZANDIN!).
- **Davranış / görsel (GO):** Başlık 112 pt (kaybetme (1,0.42,0.38), kazanma altın), alt satır 38 pt, rekor kırıldıysa altın "YENİ REKOR!" hapı, kart 760x600 (yarı saydam, 5 satır, ince ayırıcı çizgiler alfa 0.08; coin satırları altın). Butonlar: **Tekrar Oyna** 700x140 / 58 pt kırmızı (0.545,0,0) y=-420; **Ana Menü** 700x116 / 46 pt ikincil (0.16,0.16,0.3) y=-590 (GO:177-186).
- **Sorun:** (1) **Giriş gecikmesi yok** (kart seçiminde 0.4 s var): oyuncu sürüklerken ölürse parmak "Tekrar Oyna"ya denk gelebilir; ölüm anı ile panel arasında geçiş/öldüm hissi yok (timeScale 0 + anında panel). (2) Buton y konumları sabit (pin yok, ortalanmış): alt kısa ekranda test edilmedi. (3) Yalnız ölümde "Polis seni yakaladı" tek ölüm sebebi; ölüm sebebi çeşitlenirse metin değişmeli.

### K10. Duraklatma menüsü
- **Ne zaman:** Üst satırdaki duraklat tuşu ya da uygulama arka plana gidince.
- **Davranış / görsel (PM):** Tuş 72x72, sol üst, süre kutusunun sağında (x=24+190+12, y=-104), HUD panel rengi (0.05,0.05,0.17,0.55), 34 pt (PM:33-50). Panel: Devam (sahne butonu) + Yeniden Başla (y=-200) + Ana Menü (y=-340), kopya buton, yükseklik 110, 46 pt, ikincil renk (0.16,0.16,0.3) (PM:53-72). Tuş sadece oyun sırasında görünür; panel açılınca tuş gizlenir (PM:116-135). Ana Menü run'ı terk eder (coin kasaya girmez).
- **Sorun:** (1) **72 px tuş ~ 4-5 mm** (1080 px ~ 65-70 mm genişlik varsayımıyla): Temel seviye (>= 7 mm) altında; ekranın üstünde, oyuncu baş parmağı uzaklığında. (2) Yeniden Başla ve Ana Menü **onaysız ve geri alınamaz**, ve (3) birbirine 140 px bitişik, Devam'a yakın; ana menü coin kaybettirir. (4) Panelde animasyon yok, diğer ekranlarla tutarsız.

### K11. HUD barları (can / kalkan / XP / boss)
- **Ne zaman:** Oyun içi sürekli durum.
- **Davranış / görsel (HUD):**
  - **Can:** `Slider`; değer anında güncellenir (hasar için geciken/"hayalet" bar yok). Genişlik max cana göre uzar: 100 can = sahne genişliği, en çok 460 px, pivot sağ kenar, sola doğru `1-exp(-6*dt)` ile yumuşak (HUD:211-238). Hasar geri bildirimi bardan değil dünyadan gelir (K16).
  - **Kalkan:** can barının kopyası, hemen altında, %45 kalınlık (min 6), 3 px boşluk, dolgu (0.55,0.82,1), zemin (0.08,0.1,0.2,0.7); max<=0 iken gizli (HUD:141-182).
  - **XP:** `Slider`, her yemede değer anında (HUD:272-279); solunda "Lv.N" 30 pt, üstünde tier adı 24 pt.
  - **Boss:** `OnBossHealthChanged(max<=0)` = boss yok; container aç/kapa, slider değeri anında (HUD:185-197).
- **Sorun:** (1) Barların sahnede elle yerleştirilmiş, kod sadece yazıları ve kalkan barını kuruyor (HUD:304-336): bar boyutu/konumu bu belgeden okunamaz, sahneden doğrulanmalı. (2) Boss barında ad/ikon/segment yok; belirme/kaybolma animasyonu yok (SetActive). (3) Bar değerleri sayı olarak gösterilmiyor (can 37/100 okunmuyor). (4) XP barı dolunca seviye atlama hissi sadece kart ekranı; barda doluş vurgusu yok. (5) Kalkan: 6-px'e inebilir ince bar, anlık değişim. (6) Renk (can/XP/boss) ve konum ayrımı sahneye bağlı; renk körlüğü modu yok.

### K12. HUD bilgi paneli (yarı saydam kutu)
- **Ne zaman:** Süre, skor, coin gibi okunur değerler.
- **Davranış / görsel (HUD:304-336):** Panel rengi (0.05,0.05,0.17,0.55), `raycastTarget=false`. Sol üst süre 190x72 / 50 pt `MM:SS`; sağ üst skor+coin 240x104 (skor 44 pt beyaz `N0`, coin 28 pt altın `"N0 coin"`). Kenar 24 px, üstten 104 px (haber bandının altı: bant y -30, 58 yüksek). Metin güncelleme sadece değer değişince (zamanlayıcı saniyede bir) (HUD:252-264).
- **Sorun:** (1) Çentik/üst bandı kaplar: bant + panel + pause + bar bir arada dikey boşluk tüketiyor. (2) Skor/coin 240 px sabit kutu: büyük sayılarda ("1.234.567 coin") taşar (NoWrap/Overflow). (3) Sayı değişim animasyonu yok.

### K13. Evrim banner'ı ("pop" ile gelen metin)
- **Ne zaman:** Evrim skill'i alınınca (`OnUpgradeApplied`, `IsEvolution`).
- **Davranış (HUD:85-139):** "★ ADI" (tr-TR büyük harf), 92 pt altın + gölge, ekranın üst ortasında (y anchor 0.72, 1000x140). Ölçek 1.6->1 `0.22 s` (pop), 1.6 s bekler, 0.5 s söner; ölçeksiz zaman. Tıklanamaz.
- **Görsel:** altın (1,0.82,0.25), gölge (0.12,0.02,0.2,0.9) (mor tonlu, diğer gölgeler gece mavisi).
- **Sorun:** Tek kullanımlı; seviye atlama, tier atlama, boss gelişi gibi olaylar için ortak "banner" yok. Gölge rengi diğer gölgelerden farklı.

### K14. Haber bandı
- **Ne zaman:** Oyun boyunca ince üst bant, kişilik/atmosfer ve tier atlamasına tepki.
- **Davranış (NT):** İlk haber 8 s sonra belirir (0.35 s fade-in), sonra bant run sonuna kadar kalır. Başlık 30 s'de bir 0.35 s söner/belirir; tier atlayınca 2.5 s içinde o tier'dan haber öne alınır. Uzun başlık: 1.5 s bekle -> 110 px/s kay -> 2 s sonda bekle -> başa (NT:20-29, 93-122, 137-180). Karıştırılmış torba, tekrar yok. Oyun saniyesi (pause'da durur).
- **Sorun:** Bilgi taşımaz (atmosfer); en üst ekranı 58 px + yanındaki panellerle meşgul eder. Okuma için kayan metin hareket azaltma tercihi için aday. Bant dokunmayı etkilemez (doğrulanmadı).

### K15. Butonlar, sekmeler, kilit
- **Birincil (CTA):** Crimson (0.545,0,0), beyaz kalın metin: OYNA 640x180 / 96 pt (TS:136-144), Tekrar Oyna 700x140. OYNA "nefes alır": ölçek `1 + sin(t*3.2)*0.035`, unscaled (TS:102-107).
- **Altın aksiyon:** MARKET 640x120 altın zemin + koyu mürekkep 50 pt (LB:170-178).
- **İkincil:** (0.16,0.16,0.3) / Kart-hap zemini (0.05,0.05,0.17,0.82).
- **Geri:** 120x100 "<" 56 pt, sol üst (-420, 760) (LB:181-187, MK:274-276).
- **Sekmeler (MK:158-167, 278-289):** iki sekme 444x96; aktif = altın zemin + koyu yazı, pasif = kart rengi + beyaz.
- **Basılı durum:** yalnız renk tonu: (0.75,0.75,0.85) LB/MK, (0.8,0.8,0.9) UP. Ölçek/ses/haptik yok.
- **Kilitli kart (LB:96-116):** kart alfası 0.55 (CanvasGroup), `interactable=false`, altın rozet "KİLİTLİ · koşul"; açılma koşulu rozetle ve metinle (renkle değil) okunuyor: iyi.
- **Sorun:** basılı ton iki değerde; kilitli dokunmada geri bildirim yok; geri tuşu 120x100 px (~7-8 mm, sınırda ok).

### K16. Dünyada geri bildirim: hasar, kamera sarsıntısı, haptik
- **Ne zaman:** Hasar, yeme, kalkan, boss, kara delik.
- **Blob hasarı (`DamageFlash.cs`, `CameraController.cs`):** blob renderer'ı (1,0.15,0.15)'e 0.15 s'de doğrusal söner (MaterialPropertyBlock) (DamageFlash:10-11, 58-74). Kamera sarsıntısı genlik 0.12 dünya birimi, 0.15 s, `_heightMultiplier` ile ölçeklenir (zoomla aynı hissetmek için) (CameraController:34-35, 127-130, 184-205); güçlü sarsıntı süren zayıfı ezer. **Ekran kırmızı kenar/overlay yok**: OYUN.md'deki "kırmızı parlama" blob'un kendi rengi.
- **Düşman vuruşu:** kırmızı (1,0.25,0.22) 0.16 s (EnemyBase:50-51, 255-270). **Kalkan:** vuruşta parlama, `5/s` ile söner (ArmorShield:263).
- **Büyük olaylar:** kamera sarsıntısı boss ölümü (0.22/0.35, 0.15/0.3, 0.35/0.5), kara delik (0.3/0.5); `PunchZoom` (yaya tekmesi) (CameraController:165).
- **Haptik (`Haptics.cs`):** `Impact(0)` hafif (her yeme; min 0.08 s aralık), `1` orta (Large/Giant), `2` güçlü (Dev, boss, kara delik, skill). iOS `UIImpactFeedbackGenerator`; Android 15 ms + 15*s ms, genlik 50 + 80*s (Haptics:33-48, 72-73).
- **Sorun:** (1) **Hasar alınca haptik yok** (kod taramasında BlobHealth'te `Haptics` çağrısı bulunamadı) ve **yemede var**: olumlu olay titreşiyor, olumsuz titremiyor. (2) Hasar geri bildirimi yalnız blob renginde ve kamera sarsıntısında; blob ekranın ortasında olsa da ekran kenarında yön/şiddet bilgisi yok. (3) Düşük canda uyarı yok. (4) Sarsıntı/haptik kapatma ayarı yok; Ayarlar ekranı yok. (5) **Ses yok** (OYUN.md §10): tüm geri bildirim görsel/haptik, sessiz cihazda tek kanal.

### K17. Giriş ekranı (canlı şehir)
- **Ne zaman:** Açılış ve "Ana Menü".
- **Davranış:** Arkada canlı şehir; kamera `CinematicZoom=0.35` ile Topik'e yaklaşır. Logo "LEFT NO" 120 pt beyaz + "CRUMBS" 200 pt altın + tagline 46 pt, OYNA altta y=300 (TS:21, 128-148). OYNA -> lobi, lobiden karakter seçimi -> run (karta dokunmak oyunu hemen başlatır, onay yok).
- **Sorun:** Run başında geri sayım/hazırlık yok; karta dokunduğu anda başlar.

### K18. Kurulum kalıbı (teknik, ui-programmer için)
- Tüm paneller **çalışma zamanında bir kez kodla kurulur** (`Build()`, `_built`); sahnedeki eski yazı/butonlar gizlenir, yalnız `rounded` sprite ve buton referansları yeniden kullanılır (UP:158, GO:127, MK:245, LB:134, HUD:310). `Place/NewRect/Box/Text/Label/Shadowed/StyleText` yardımcıları **5 dosyada kopya**.
- Olay veriyolu `GameEvents` (statik); paneller `OnEnable/OnDisable`'da abone olur.
- **Sorun:** sabitlerin (renk/boyut) tek yeri yok; yeni UI için "UITheme" yokluğu en büyük teknik borç (çözüm tasarımı bu belgenin işi değil).

### Yeni kalıplar (K19-K25, oyun içi UI tasarımı; kodda henüz yok)

Her kalıbın tanımı tek yerdedir: K19 ve K23 burada (iki spec ortak kullanır); diğerleri yalnız bir ekranda kullanıldığı için tanımı o spec'te, burada özet + yer. Paket adları (P0-a…d) `hud.md` §16.

### K19. Slot şeridi (ortak bileşen) — P0-c
- **Ne zaman:** sahip olunan skill'ler, seviyeleri ve boş slotlar. HUD (slot 72 ref px, `hud.md` §4.2) ve kart ekranı (slot 96 ref px + sayaç, `seviye-atlama-kartlari.md` §5.1). Tek bileşen, `slotSize` parametresi.
- **Kurulum:** `Build()`'de bir kez 3 aktif + 3 pasif slot + nadir + evrim rozeti (K18). Run'da yalnız sprite/metin ataması ve `SetActive`; Instantiate/Destroy yok.
- **Sıra:** 3 aktif · 24 boşluk · 3 pasif · 24 boşluk · nadir · evrim (sahip değilse yer kaplamaz); slot aralığı 8. Grup içinde **alım sırası**: ilk alınan solda, karakterin başlangıç skill'i ilk. Kaynak: `UpgradeSystem` sıralı aktif/pasif listeleri (P0-b).
- **Slot:** K23 şekilli zemin (gece mavisi alfa 0.55) + 4 px tür rengi çerçeve (alfa 0.8). İçerik: ikon (P1; HUD 52, kart 72) ya da `UpgradeData` kısaltması (P0-b), kalın beyaz, HUD 32 / kart 40 ref px.
- **Seviye hapı:** sağ alt, slotun %44'ü (HUD 32, kart 42), gece mavisi alfa 0.9, rakam kalın beyaz HUD 28 / kart 34 (28 = ikincil istisna, bilgi kartta da var). **Max:** altın hap + koyu mürekkep "5" + altın çerçeve. Hapta "MAX" değil "5": 32 px hapa "MAX" 28 px'te sığmaz; kart rozeti eylemi söyler ("→ MAX"), hap durumu (renk + rakam birlikte). Nadir ve evrim rozetinde hap yok (tek seviye).
- **Boş slot:** yalnız çerçeve, alfa 0.25. Doluluğu sayı yazmadan gösterir.
- **Sopa Sv.0:** listede yok, slot göstermez; kartı alınınca (ya da başlangıç skill'i ise) aktif listeye girer. Kaynak: `UpgradeData` temel silah bayrağı (P0-b).
- **Güncelleme:** yalnız `OnUpgradeApplied(data, level)` ile. Yeni skill: slot 0.3 sn boştan doluya fade. Seviye: o slot K7 punch 1.15 (0.22 sn). Ölçeksiz zaman. **Önizleme yok:** şerit seçimden önce değişmez, hep uygulanmış durumu gösterir.
- **Restart:** `OnCharacterSelected`'da tüm slotlar boşa döner; başlangıç skill'i `GrantStartingUpgrade`'in attığı `OnUpgradeApplied` ile dolar.
- **Etkileşim:** yok (`raycastTarget=false`).
- **Nadir/evrim rozetleri, öfke nabzı, İkinci Katman dolumu:** P1/P2 (`hud.md` §4.2).

### K20. Olay bandı — P0-d (sade), P1 (kuyruk kuralları)
K13 evrim banner'ının ortak bileşene genellenmesi: tek band, başlık + isteğe bağlı alt satır, pop 0.22 sn + tutma + 0.4 sn sönme. Tanım ve kuyruk kuralları: `hud.md` §4.2 "Olay bandı" ve §8.2.

### K21. Kenar uyarısı — P0-d
Tam ekran kenar gradyanı (hasar kırmızı, kalkan mavi, düşük can nabzı). Tek vuruşta kısa flaş, sürekli hasarda sabit ton (flaş dizisi yok), boştayken çizilmez. Tanım (durum makinesi): `hud.md` §4.2 "Kenar uyarısı".

### K22. Hayalet bar — P1
Hasarda ana dolgunun arkasında kalan beyaz parça, gecikmeyle iner. Tanım: `hud.md` §4.2.

### K23. Tür şekli kodlaması — P0-c
- **Ne zaman:** skill türü görünen her yerde (kart tür etiketi, kart ikonu çerçevesi, slot zemini).
- **Şekiller:** Aktif = **daire**, Pasif = **yuvarlak kare** (mevcut 9-slice), Nadir = **elmas**, Evrim = **yıldız** (P1). Şekil bir `Image` sprite'ıdır, metin glifi değildir.
- **Tür etiketi:** 32x32 şekil Image + 8 boşluk + TMP 32 kalın `AKTİF` / `PASİF` / `NADİR` (tür renginde). Evrim kartında etiket yok.
- **Renk ikincil:** aktif turuncu (1,0.6,0.3), pasif açık mavi (0.55,0.8,1), nadir pembe (1,0.45,0.65), evrim altın. Gri tonlamada şekil + metin ayırt eder.

### K24. Seçim onay animasyonu — P0-c
Seçilen kart punch + altın çerçeve, diğerleri söner; olay animasyondan sonra atılır. Zaman çizelgesinin tek yeri: `seviye-atlama-kartlari.md` §10.

### K25. Seviye noktaları — P1
Kartta 5 nokta: dolu = sahip olunan, kontur + nabız = kazanılacak, kontur = boş. Tanım: `seviye-atlama-kartlari.md` §5.3, §12.

---

## 2. Eksik kalıplar (oyun içi UI işi için gerekecek, projede yok)

Sadece liste. 1, 4, 5, 6, 7 ve 11 için kalıplar K19-K25'te; 12'nin oyun sonu giriş koruması P0-a (`hud.md` §16).

1. **Kalıcı skill/slot göstergesi (3 aktif + 3 pasif):** oyun sırasında hangi skill'in kaç seviyede olduğunu ve boş slotu görmek için (2026-11-04'te HUD sadeleştirmesiyle rozetler kaldırıldı, OYUN.md §10 "Skill rozeti yok").
2. **Skill ikonu seti:** kart, slot göstergesi ve banner'da tek bakışta tanınma için (OYUN.md §11 "Eksik").
3. **Bekleme süresi / hazır göstergesi:** otomatik skill'lerin ne zaman ateşleneceği/etkin olduğu okunmuyor.
4. **Olay bildirimi / toast kuyruğu:** tier atlama, boss geliş/ölüm, evrim gibi olaylar için ortak, kuyruklu bildirim (şu an yalnız tek kullanımlık evrim banner'ı K13).
5. **Ekran kenarı uyarısı / yön göstergesi:** ekran dışından gelen boss, yoğun polis, hasar yönü için.
6. **Düşük can uyarısı:** can azaldığında hem görsel (kenar, bar nabzı) hem haptik; şu an hiç yok.
7. **Bar geri bildirim kalıpları:** hasar "hayalet" bar parçası, XP dolma vurgusu, boss barı belirme/ad, kalkan kırılma.
8. **Tier atlama geri bildirimi:** blob büyümesi dünyada ve tier metninde yalnız metin değişiyor (HUD:266-270); büyük bir kilometre taşı için geri bildirim yok.
9. **Seviye atlama öncesi anı (level-up teaser):** XP dolunca oyun aniden donup kart ekranı geliyor; geçiş/ön bildirim yok.
10. **Sayı değişim animasyonu (HUD):** skor/coin/XP için "tick up" (GO'daki sayma HUD'a taşınmıyor).
11. **Skill kartında karşılaştırma ve slot bağlamı:** "şu an -> sonraki" fark, hangi slota gireceği/doluysa ne olacağı.
12. **Onay/koruma kalıbı:** yıkıcı eylemler için (Yeniden Başla, Ana Menü, pahalı satın alma) ve oyun sonu butonlarına giriş gecikmesi.
13. **Ayarlar ekranı:** titreşim/sarsıntı kapatma, hareket azaltma, metin/UI ölçeği, renk körü modu, ses (ses gelince). Temel erişilebilirlik varsayımının önkoşulu.
14. **Durum bildirimi ikonografisi:** aktif buff/debuff süreleri (kalkan, vakum vb.) için küçük ikonlu süre göstergesi.
15. **Ortak tema/token katmanı ve ortak banner/panel bileşenleri:** teknik, K18.

---

## 3. Tasarım dili (koddaki gerçek değerlerle)

**Renk (hepsi dosyalarda tekrarlanıyor, RGBA):**

| Rol | Değer | Kullanım |
|---|---|---|
| Zemin / panel | (0.05, 0.05, 0.17) gece mavisi; alfa 0.55 (HUD kutusu), 0.7-0.85 (ekran zemini), 0.82-0.88 (kart) | tüm paneller |
| Altın (vurgu, coin, başlık) | (1, 0.82, 0.25) | coin, evrim, başlıklar, aktif sekme |
| Koyu mürekkep (altın üstü yazı) | (0.12, 0.08, 0.02) | altın hap/rozet metni |
| Birincil CTA | Crimson (0.545, 0, 0) | OYNA, Tekrar Oyna |
| İkincil buton | (0.16, 0.16, 0.3) | Ana Menü, pause butonları |
| Olumsuz / uyarı | (1, 0.42, 0.38) | GAME OVER başlığı, yetersiz coin |
| Olumlu / sahip | yeşil (0.35, 0.75-0.82, 0.4-0.45) | YENİ, AÇIK/ALINDI/MAX |
| Skill rozeti (sahip olunan) | mavi (0.25, 0.55, 1) | Sv. a → b |
| Tür etiketleri | AKTİF (1,0.6,0.3) · PASİF (0.55,0.8,1) · NADİR (1,0.45,0.65) | kart |
| Kalkan | (0.55, 0.82, 1) | kalkan barı |
| Metin ikincil | beyaz alfa 0.75-0.85; soluk 0.45 | açıklama/ipucu |
| Hasar flaşı | (1, 0.15, 0.15) blob, (1, 0.25, 0.22) düşman | dünya |

**Yazı:** TextMeshPro, varsayılan LiberationSans SDF (Türkçe karakterler dinamik fallback atlasından; CLAUDE.md). Hemen her şey **kalın**, ikincil açıklamalar normal. Boyut basamakları (referans px): 24-26 (etiket/tier), 28-30 (ince bilgi), 34-38 (açıklama/hap), 40-50 (satır/süre), 58 (kart adı/CTA), 92-112 (ekran başlığı), 120-200 (logo). Otomatik boyutlandırma kapalı, `NoWrap + Overflow` varsayılan; sadece çok satırlı açıklamalar `Normal` sarar.

**Glif kuralı (yeni UI metinleri):** TMP metninde yalnız ASCII, Türkçe harfler, `·` ve `★` kullanılır (UI'da kullanılıp çalıştığı doğrulanmış olanlar: `★` evrim kartı/bandı, `·` lobi ve Market). Emoji yok. Diğer işaretler TMP Sprite Asset `UI_Glyphs` üzerinden `<sprite name="...">` ile: `ok` (→), `yukari` (▲), `asagi` (▼), `uyari` (üçgen içinde !); P1'de `yenilik` (kilometre taşı işareti, ★'dan ayrı şekil). Sprite'lar beyaz, tint açık (metin rengini alır). Tür şekilleri metin değil Image (K23). Asset: `hud.md` §14.

**Köşe / yüzey:** tek "yuvarlak" 9-slice sprite (`UISprite`), kartlar/haplar/butonlar aynı; kenar çizgisi yok, derinlik yarı saydam gece mavisi kutu + gölgeli başlıkla. Düz/flat, bloom yok (mobil). Kart boşluğu ~40 px, kenar boşluğu 24 px, güvenli bölge kenar boşluğu çentik için panel içinde.

**Animasyon karakteri:** kısa, "pop" tabanlı; ease-out kare. Süreler: kart belirme 0.22 s (+0.07 s sıralama), panel girişi 0.28 s, sayma 0.9 s, punch 0.22 s, shake 0.35 s, banner pop 0.22 s + 1.6 s bekle + 0.5 s söner, OYNA nefesi sin 3.2 rad/s (ölçek +-3.5%), bar uzama `exp(-6 dt)`, haber geçişi 0.35 s. Başlangıç ölçekleri: 0.9 (kart), 0.92 (panel), 1.6 (banner). Tümü ölçeksiz zaman (haber bandı hariç). Süreler 0.2-0.4 s arası, "enerjik ama kısa" karakter; sönme/fade 0.35-0.5 s.

**Geri bildirim dili:** dünyada renk flaşı + kamera sarsıntısı + haptik; UI'da zıplama/sallanma. Ses yok.

---

## 4. Çapraz tutarsızlıklar (özet)

1. "Sv." (skill kartı, Market) / "Lv." (HUD seviye): Türkçe tutarsızlık (HUD:284).
2. Tier adı `BlobTier.ToString().ToUpper()` (HUD:269): enum İngilizce adı (Tiny/Little/Colossal...) Türkçe arayüzde; evrim banner'ı tr-TR kültürü kullanıyor, tier kullanmıyor.
3. Yeni-Sv rozeti rengi: kartta mavi, Market'te altın.
4. "Yapamazsın" geri bildirimi: Market sallanır, lobi kilidi sessiz.
5. Giriş koruması: kart seçiminde 0.4 s, oyun sonu/pause butonlarında yok.
6. HUD sayıları anlık, oyun sonu sayıları animasyonlu.
7. Backdrop alfası 0.7 / 0.72 / 0.85; Subtle alfası 0.75 / 0.78 / 0.8 / 0.85.
8. Dokunma hedefi: pause 72 px, geri 120x100, kartlar 900+ px.
9. Olumlu olay haptik, hasar haptik yok; ses hiç yok.
10. Banner gölgesi mor-siyah (0.12,0.02,0.2), diğer gölgeler gece mavisi.

## 5. Doğrulanması gereken (okunmadı / varsayım)

- Barların sahne boyutu ve konumu (Slider'lar sahnede); HUD'un SafeArea altında olduğu varsayıldı.
- Dokunma mm hesapları 1080 px ~ 65-70 mm cihaz genişliği varsayımıyla (tipik telefon); gerçek cihazda ölçülmeli.
- Klavye/gamepad UI gezintisi: UGUI `EventSystem` navigasyonu doğrulanmadı; PC portu (OYUN.md §12) için ayrıca ele alınmalı.
