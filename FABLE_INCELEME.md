# Blob.io / Zorse — Proje Durum Raporu (Fable İnceleme)

> Bu doküman, Claude (Fable 5) tarafından yapılan tam proje incelemesinin çıktısıdır.
> Tarih: 2026-07-08 · İnceleme kapsamı: tüm C# scriptler, sahne, ScriptableObject asset'leri, ProjectSettings, paketler.

**TL;DR:** Projenin iskeleti sağlam (temiz namespace ayrımı, import cycle yok, event bus disiplinli) ama **core loop'un yarısı kopuk**: düşman öldürmek hiçbir ödül vermiyor, pool sistemi düşman ve consumable'lar için fiilen çalışmıyor ve 500 düşman hedefi mevcut mimariyle ulaşılamaz durumda. Ayrıca **acil bir senkron sorunu var**: lokal repo `origin/main`'in 2 commit gerisinde — Sero'nun tüm karakter/silah sistemi ve mobil perf düzeltmeleri remote'ta duruyor.

---

## 0. Önce Bunu Yap: `git pull`

Remote'ta bekleyen 2 commit var:
- `fe484ac` — Dev A Sprint 1'in tamamı: `CharacterData`, `WeaponBase` + 3 silah (Cannon/MetalBall/Pistol), projectile sistemi, `LobbyPanel`
- `5f18675` — Mobil perf: düşman AI throttle (0.15s aralık + stagger, `sqrMagnitude`'a geçiş), projectile'lar physics dışına alınmış

Bu commit'ler iki büyük eksiği (oyuncunun saldırı sistemi olmaması, düşman AI'ının throttle'sız olması) zaten çözüyor. Working tree'de sadece TextMesh Pro'nun otomatik değişen font asset'i var; pull büyük ihtimalle sorunsuz geçer. Raporun geri kalanı **pull sonrası hâlâ geçerli olan** bulguları içeriyor (remote kodu ayrıca incelendi).

---

## 1. Boş Kalan Kritik Kısımlar

### 1.1 Düşman ölümü tamamen ödülsüz (core loop kopuk)
`EnemyBase.Die()` sadece `gameObject.SetActive(false)` yapıyor — remote'ta da böyle. Ne XP, ne coin, ne skor, ne event. `EnemyData.ScoreValue` alanı **hiçbir yerden okunmuyor** (ölü veri; `EnemyData_Police.asset`'te zaten 0). Bullet-heaven türünde "düşman öldür → ödül topla" döngüsü oyunun kalbi; şu an silahla düşman öldürmenin oyuncuya hiçbir getirisi yok. Sprint 2'deki A7 (coin drop) bunu kısmen kapatacak ama `OnEnemyDied` event'i + XP orb drop'u da tasarlanmalı.

### 1.2 Pool sistemi düşman ve consumable'lar için fiilen devre dışı
CLAUDE.md'deki "Instantiate/Destroy runtime'da yasak" kuralı şu an ihlal ediliyor çünkü:
- Düşmanlar pool'a **hiç geri dönmüyor**: `Die()` deaktive ediyor, `EnemySpawner.CleanupInactive()` (EnemySpawner.cs:105) listeden siliyor ama `Return()` çağırmıyor. Kuyruk boşalınca her spawn `Instantiate` oluyor (ObjectPool.cs:34).
- Consumable'larda aynı bug: `BlobConsumption.Consume()` (BlobConsumption.cs:43) deaktive ediyor, kimse iade etmiyor → 2 saniyede bir refill sürekli yeni instance yaratıyor.
- Sadece `CarSpawner` doğru yapıyor (CarSpawner.cs:61) — pattern belli, iki yere kopyalanması unutulmuş.

Ek pool zafiyetleri: state reset hook'u yok (geri dönüştürülen düşman eski canını taşıyabilir), double-return koruması yok, max cap yok. Pooled NavMeshAgent yeniden konumlandırılırken `agent.Warp()` çağrılmadığı için mesh dışına ışınlanan düşman sessizce donuyor (EnemyBase.cs:63'teki `isOnNavMesh` guard'ı hatayı yutuyor).

### 1.3 500 düşman hedefi mevcut mimariyle imkânsız
- `EnemySpawner._maxActiveEnemies` 30'a hard-cap'li (EnemySpawner.cs:14 ve :102).
- Düşman başına bir `NavMeshAgent` var. Remote'taki throttle iyileştirmesi 30 düşman için yeterli ama 500'e çıkmaz — NavMeshAgent-per-enemy bu ölçekte yanlış araç. Vampire Survivors tarzı kalabalık için basit steering (blob'a doğru yürü + komşudan ayrıl) veya flow-field gerekir; NavMesh sadece elit/boss gibi az sayıda akıllı birime kalmalı.
- **Bu, "daha iyi kurgulanabilecek" değil, "erken karar verilmesi gereken" bir konu** — düşman sayısı arttıkça geriye dönük değiştirmesi pahalılaşır.

### 1.4 İçerik neredeyse yok
Envanter: **1 düşman** (Police), **1 consumable** (Trash), **1 dalga** (60. saniye, max 5 düşman), **1 araç**, 7 upgrade. GDD'nin MVP tanımı bile 3 düşman tipi + 8 upgrade + tier-5 boss istiyor. Kod iskeletine kıyasla içerik üretimi (SO asset'leri + prefab'lar) ciddi geride — bu Hüma'nın art pipeline'ıyla da kesişen bir darboğaz.

### 1.5 Upgrade seviyelendirme kozmetik
- Her efekt seviyeden bağımsız aynı `PerLevelValue`'yu ekliyor; `EffectValue`, `EffectDuration`, `Cooldown` alanları (UpgradeData.cs:23-25) hiç kullanılmıyor. Seviye eğrisi (örn. azalan getiri) yok.
- `CurrentLevel` **paylaşılan SO asset'i üzerinde** tutuluyor (UpgradeData.cs:32) — editor'da domain reload kapalıysa veya iki sistem instance'ı olursa state sızar. Runtime upgrade state'i ayrı bir wrapper class'ta tutulmalı.
- `OnUpgradeSelected`'da level, effect null olsa bile artıyor (UpgradeSystem.cs:56-57); blob referansı bulunamazsa erken return → `ResumeGame` çağrılmaz → oyun `timeScale=0`'da **kilitli kalır**.

### 1.6 GameState akışında ölü/çakışan yollar
- `GameManager.TriggerLevelUp` (GameManager.cs:69) hiç kullanılmıyor; gerçek akış `UpgradeSystem` → `PauseGame` üzerinden gidiyor, yani level-up sırasında state `LevelUp` değil `Paused`. İleride pause menüsü eklendiğinde `OnGamePaused` dinleyen menü, upgrade seçimi sırasında da açılacak.
- `BlobHealth.Die` hem `TriggerGameOver` hem `RaiseGameOver` çağırıyor — çift event riski.
- `OnApplicationPause` otomatik duraklatıyor ama geri döndürmüyor.

### 1.7 Hazard mekaniği yarım
`IsHazard && RequiredTier > CurrentTier` kombinasyonunda hiçbir dal çalışmıyor (BlobConsumption.cs:24-27); `HazardAmount` alanı hiç okunmuyor, hasar `MassValue * 0.5f` hardcoded. Tasarımcının doldurduğu değer boşa gidiyor.

### 1.8 Hiç dokunulmamış sistemler (kodda izi bile yok)
Ses (AudioManager yok), kayıt sistemi (tek `PlayerPrefs` int'i: highscore — `Save()` bile çağrılmıyor, mobilde crash'te kaybolur), pause menüsü, sahne yönetimi, coin/para birimi, damage number'lar, meta-progression. Bunlar roadmap'te zaten planlı; sadece "planlı ≠ başlanmış" olduğu netleştiriliyor.

---

## 2. Daha İyi Kurgulanabilecek Noktalar

**a) Vizyon dokümanları birbiriyle çelişiyor — en önemli kurgusal sorun bu.**
Kökteki `GDD.md`: 5–10 dk run, tier-5 tavanı, extract modeli, perf hedefi "80 consumable + 20 enemy". `CLAUDE.md` (PDF GDD v1.0'dan): 20–30 dk oturum, 25. dakika Kıyamet Bossu, **500 düşman @30fps**. Bu iki vizyon farklı oyunlar; düşman mimarisi, wave scaling, batarya hedefi ve içerik miktarı hangisine göre kurulacak? Ekipçe tek karara bağlanıp kaybeden doküman güncellenmeli. (500 düşman hedefi düşerse §1.3'ün aciliyeti de düşer.)

**b) `FindWithTag("Blob")` / `FindAnyObjectByType` dağınıklığı.**
Blob referansı 4+ yerde tag ile aranıyor (EnemyBase, EnemySpawner, ConsumableSpawner, CarController); `BlobConsumption.Consume` **her yemede** `FindAnyObjectByType<ScoreSystem>` (BlobConsumption.cs:36), `HUDController` **her XP değişiminde** `FindAnyObjectByType<BlobGrowth>` (HUDController.cs:90) çağırıyor — kendi CLAUDE.md kuralına aykırı. Basit bir `GameContext`/servis sınıfı (blob, score, growth referanslarını bir kez çözen) hepsini toplar.

**c) Magnet/Vacuum sorguları throttle'sız ve maskesiz.**
Her ikisi de her frame `OverlapSphereNonAlloc` çalıştırıyor, **LayerMask'siz** (düşman/araba/blob dahil her collider taranıyor) ve her hit'e `GetComponent<IConsumable>` atıyor (MagnetComponent.cs:18-22, VacuumComponent.cs:28-33). Consumable tier layer'ları (9-13) zaten var — maske bedava. CLAUDE.md'deki 0.15s throttle pattern'i burada da uygulanmalı. Ayrıca transform ile hızlı çekilen item trigger frame'ini atlayıp hiç yenmeyebilir (tünelleme).

**d) State machine her geçişte `new` allocation yapıyor** (EnemyBase.cs:40, ChaseState.cs:17-19 vb.). Attack range sınırında salınan düşmanlar sürekli GC üretir. State'ler stateless'a yakın — paylaşılan statik instance'lar veya enum-based FSM yeterli.

**e) Her yemede `Handheld.Vibrate()`** (BlobConsumption.cs:45) — vakumla 20 item çekildiğinde cihaz kesintisiz titrer; batarya hedefini de yer. Cooldown'lu, tier'a göre kademeli haptic'e geçilmeli.

**f) `GameManager` her frame event raise ediyor** (GameManager.cs:44) ve `WaveController` her frame tüm dalga listesini tarıyor. Saniyede 1 tick yeterli.

**g) ProjectSettings store'a hazır değil:** `productName: "My project"`, bundle id hâlâ URP template default'u, `AndroidTargetSdkVersion: 0` (Play Store reddeder), ekran yönü AutoRotation (portrait HUD'a rağmen). 5 dakikalık iş ama unutulursa build gününde patlar.

**h) Sahne çakışması riski gerçek:** Tüm manager'lar + UI tek `GameScene.unity` içinde ve 3 kişi çalışıyor (GDD kendisi de bu riski listeliyor). Sahnede ayrıca kopya joystick alt-objeleri (`Background`/`Handle` ×2) duruyor. Öneri: manager'ları tek bir "Systems" prefab'ına, UI panellerini ayrı prefab'lara taşıyın; `.gitattributes`'a UnityYAMLMerge (smart merge) ekleyin.

**i) UI ufak tefekleri:** `UpgradePanel.Start` buton dizisini null-check'siz dereference ediyor; `VirtualJoystick` pointerId takip etmediği için ikinci parmak joystick'i kaçırabilir; `TierText`/skor string'leri her event'te alloc (düşük öncelik).

---

## 3. İhtiyaç Duyulacak Skill'ler ve Tool'lar

### Ekipte geliştirilmesi gereken beceriler
| Beceri | Neden | Ne zaman |
|---|---|---|
| **Cihaz üstü profiling** (Unity Profiler + Memory Profiler + Frame Debugger, gerçek Android/iOS cihazda) | 500-düşman/batarya hedefleri editörde ölçülemez; §1.3 kararı veriye dayanmalı | Hemen — Sprint 2'de bir "perf günü" |
| **Kalabalık simülasyonu teknikleri** (basit steering, spatial hashing; gerekirse Jobs + Burst) | NavMeshAgent 500 birime ölçeklenmez | 500 hedefi onaylanırsa Sprint 3 öncesi |
| **Unity YAML merge / sahne iş bölümü** (UnityYAMLMerge, prefab workflow) | 3 kişilik ekip, tek sahne | Hemen |
| **URP mobil optimizasyonu** (SRP Batcher, draw call, GPU instancing) | Art asset'leri gelmeye başlayınca | Hüma'nın asset'leriyle birlikte |
| **SO tabanlı balance workflow** (belki CSV/Sheets → SO importer) | İçerik sayısı 1'den 30'a çıkarken elle asset düzenlemek yavaş | Sprint 3+ |

### Eklenmesi önerilen paket/tool'lar (öncelik sırasıyla)
1. **Şimdi:** `.gitattributes` + UnityYAMLMerge; Git LFS (art gelmeden önce); basit bir JSON tabanlı save sistemi (PlayerPrefs'ten önce, meta-progression Sprint 4'te buna oturacak)
2. **Yakında:** **PrimeTween** (alloc-free tween — DOTween'e mobilde tercih; PunchScale'in Update lerp'iyle çakışması gibi sorunları da çözer); temel **AudioManager** (FMOD'a gerek yok); **Unity Cloud Diagnostics veya Sentry** (crash raporu); **GameAnalytics veya Unity Analytics** (soft-launch öncesi retention verisi şart)
3. **Faz 11/15 geldiğinde:** Unity IAP, LevelPlay/AdMob, Remote Config (balance'ı store update'siz ayarlamak için), Addressables (içerik büyürse)
4. **Opsiyonel ama değerli:** GameCI/GitHub Actions ile otomatik build (3 kişilik ekipte "benim makinede çalışıyor" sorununu keser). Adaptive Performance paketi zaten kurulu — Samsung cihazlarda batarya hedefi için aktive etmeye değer.

---

## 4. Önerilen Öncelik Sırası

1. `git pull` + doküman çelişkisini ekipçe karara bağla (500 düşman mı, 20 mi?)
2. Pool return bug'larını düzelt (düşman + consumable) — 2 satırlık fix'ler ama Instantiate yasağını geri getirir
3. Düşman ölüm ödülü (`OnEnemyDied` event + ScoreValue kullanımı) — Sprint 2 A7 ile birleşir
4. UpgradeSystem'deki soft-lock (blob bulunamazsa resume edilmiyor) + SO'da runtime state sorunu
5. Magnet/Vacuum'a LayerMask + throttle; `FindAnyObjectByType` cache'leri
6. ProjectSettings kimlik/SDK ayarları (5 dk)
7. İçerik üretimi: 2 düşman tipi + 3-4 consumable + 3-4 dalga asset'i — kod değil, veri işi; paralelde yürüyebilir
