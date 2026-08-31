# Blob.io (Zorse Studio) — CLAUDE.md

Bu dosya, Claude Code'un projeyi sıfırdan anlayabilmesi için yazılmıştır. Her yeni sohbette otomatik okunur.

**Slogan:** _"Hayatta kal. Seviyeleri atla. Karanlığı yut."_

## Proje Özeti

**Blob.io** — Zorse Studio tarafından geliştirilen, top-down 2.5D Unity **roguelite / bullet-heaven / survival** oyunu. Oyuncu bir blob'u kontrol eder, kendinden küçük consumable itemleri yiyerek büyür; belirli bir süreden sonra polis/düşman dalgalarıyla saldırıya uğrar. Karakter büyüdükçe XP toplar, yetenek kazanır ve tüketebileceği consumable boyutu artar. 5-10-20-30. dakikalarda güçlü bosslar (SWAT arabası, helikopter, drone) spawn olur.

- **Referanslar:** Vampire Survivors + Katamari Damacy + Hole.io
- **Hedef platform:** Mobil (iOS/Android) ana hedef, PC ikincil
- **Oturum süresi:** 20–30 dakika
- **İş modeli:** Mobil ücretsiz oyna + kozmetik mağaza + reklam; PC ücretli
- GDD: `/Users/baharyavuz/Downloads/Zorse GDD v.1 (1).pdf` (v1.0, Haziran 2025)
- Unity 6, URP (Universal Render Pipeline)
- New Input System (`UnityEngine.InputSystem`)
- AI Navigation paketi (NavMeshAgent)

> **Not:** GDD'de motor olarak Godot 4.x geçiyor ama proje Unity 6'da geliştiriliyor. Tasarım hedeflerini takip et, teknik seçimleri Unity üzerinden yap.

---

## GDD Vizyonu (Özet)

### Karakterler (Başlangıç)
Her karakter bir "top" formunda; ellerinde silahları var.

| Karakter | Pasif | Başlangıç Silahı | Kilit |
|----------|-------|------------------|-------|
| **Topik** | +20% hareket hızı | Top | Başlangıçta açık |
| **Mıknato** | +10% çekim gücü | Metal bilye | Market'te 500 kredi |
| **Mermo** | Büyük consumable'ları mermiyle parçalara ayırır | Pistol | 3 farklı haritada oturum tamamla |

### Skill Seti (Oturum İçi Güçlenmeler)
1'den 8'e kadar seviye alır. Kategoriler:
- **Savunma:** Kalkan, Rejenerasyon (+0.5 HP/sn, seviye başına +0.5)
- **Saldırı:** Silah (karaktere göre değişir — Top, Bilye, Pistol)
- **Pasif:** Maksimum Can (base 100, seviye başına +10)
- **Hareket:** Hızlanma (Bot)
- **Destek:** Vakum (10 birim yarıçap, seviye başına +5%), Mıknatıs, Score Multiplier

Seviye atlandığında oyun durur → 3 kart sunulur → 1 seç. Aynı skill tekrar seçilirse seviye yükselir. **Yeniden Çek** butonu: oturum başına 1 ücretsiz, sonrası 50 altın.

### Düşman Sistemi
| Tip | Davranış | XP |
|-----|----------|-----|
| Normal polis | Kalabalık koşar | 1–5 |
| Elit polis | Yavaş, güçlü, özel saldırı deseni | 20–50 |
| Minyatür boss | Her 5 dakikada bir | 100–200 |
| Kıyamet Bossu | 25. dakikada; faz geçişleri | 500 |

**Ölçekleme:** 0–5dk temel sürü → 5–10dk elit + hasar %20↑ → 10–15dk minyatür boss + 2x yoğunluk → 15–20dk çoklu elit + hız → 20–25dk hasar x3 → 25dk+ Kıyamet dalgası.

**Ölüm ödülleri:** Düşmandan coin; **elit** düşmandan sandık (skill + yüksek coin).

### Hava Durumu Efektleri (Runtime Modifier)
- **Ay Tutulması:** XP +50%, vampirler +30% güçlü
- **Kızıl Yağmur:** Market çarpanı x2, düşmanlar yavaşlar
- **Sis:** Görüş daralır, düşmanlar görünmez spawn
- **Şimşek Fırtınası:** Oyuncu aura hasarı, mekanik düşmanlar devre dışı

### Meta Progression — "Market"
Oturumlardan toplanan kredi kalıcı kaynak. Ölümde %50 korunur.

| Harcama | Maliyet | Etki |
|---------|---------|------|
| Yeni Karakter | 500 | Kalıcı unlock |
| Karakter Pasif +1 | 200–800 | Kademe kalıcı pasif |
| Yeni Harita | 300–700 | Harita havuzuna ekle |
| Başlangıç Silahı Kilidi | 400 | Oturuma o silahla başla |
| XP Çarpanı +10% | 1000 | Tüm oturumlarda |

**Grimoire (Kodeks):** Tüm düşman/silah/harita için stat + lore. %100 doluluk → kozmetik ödül.

**NG+ zorluk:** Standart → Kızıl Ay → Kan Krizi → Apokalips.

### Haritalar
Sonsuz kaydırmalı (Vampire Survivors modeli). Toplanabilir: Coin, Kalp, Altın Kasa. Mağara/ahır gibi yapılar easter egg/rozet barındırabilir.
- Modern Şehir (başlangıç)
- Medieval
- (Diğerleri sonra)

### Sanat Yönü
Referanslar: Vampire Survivors + Castlevania. Gotik palet.

| Kullanım | Renk | Hex |
|----------|------|-----|
| Oyuncu | Kızıl | `#8B0000` |
| UI | Kemik beyazı | `#F5F0E0` |
| Arka plan | Gece mavisi | `#0D0D2B` |
| Sürü düşman | Bataklık yeşili | `#3D5C3A` |
| Elit düşman | Mor gecesi | `#4A0E6B` |
| XP gem | Kehribar | `#FFC300` |

**Erişilebilirlik:** Renk körü dostu — güçlenme kartları renk + sembol (yıldız/elmas/daire).

### Performans Hedefleri
- **Mobil:** 30 FPS @ 720p, 500 düşman aynı anda
- **PC:** 60 FPS @ 1080p, 1000+ düşman
- **Batarya:** 30 dk oturum → max %15 tüketim

## Workflow

- Assistant tüm C# scriptleri yazar
- Kullanıcı sadece Unity Editor'da yapılması gereken adımları uygular (Inspector ayarları, prefab bağlama, NavMesh bake, vb.)
- Kullanıcıya her zaman net Editor adımları verilmeli

---

## Performans Kuralları (Mobil Hedef)

Bu oyun mobilde de çalışacak. Her yeni özellik veya kullanıcı isteği için performans değerlendirmesi yapılmalı. Pahalı bir istek gelirse kullanıcıya söyle.

### Kesinlikle Yapılmayacaklar
- `FindAnyObjectByType<T>()` Update/FixedUpdate/OnCollision içinde → `Start()`/`Awake()`'te cache'le
- Her frame physics sorgusu (OverlapSphere, Raycast) → throttle et (0.1–0.2s aralık)
- Her frame string allocation (`text = value.ToString()`) → sadece değer değişince güncelle
- `GetComponent<T>()` Update içinde → cache'le

### Throttle Pattern (AI sorgular için standart)
```csharp
private float _updateTimer;
private const float UpdateInterval = 0.15f;

// Start'ta stagger ekle — tüm objeler aynı anda çalışmasın
_updateTimer = Random.Range(0f, UpdateInterval);

// Update'te
_updateTimer -= Time.deltaTime;
if (_updateTimer <= 0f)
{
    _updateTimer = UpdateInterval;
    // pahalı sorgu buraya
}
```

### Mesafe Karşılaştırması
```csharp
// YANLIŞ — sqrt hesabı pahalı
float dist = Vector3.Distance(a, b);
if (dist < radius) ...

// DOĞRU — sqrMagnitude kullan
if ((a - b).sqrMagnitude < radius * radius) ...
```

### NavMesh / AI Maliyeti
- Her NavMeshAgent her frame pathfinding yapar → pahalı
- Blob'a uzak düşmanlar için `navAgent.updateInterval` artır
- Aynı anda max 8-10 aktif NavMeshAgent önerilir mobilde

### Object Pool Zorunluluğu
- Tüm spawn olan objeler (consumable, düşman, araba, yaya) pool'dan gelmeli
- `Instantiate`/`Destroy` runtime'da yasak

---

## Unity 6 API Notları

Unity 6'da bazı API'ler değişti — eski versiyonlarla karıştırma:

```csharp
// DOGRU (Unity 6)
_rigidbody.linearDamping = 5f;
_rigidbody.angularDamping = 10f;
FindAnyObjectByType<T>();

// YANLIS (deprecated)
_rigidbody.drag = 5f;
_rigidbody.angularDrag = 10f;
FindObjectOfType<T>();
```

---

## Namespace Yapısı

```
BlobSurvivor.Core        → GameManager, GameEvents, CameraController
BlobSurvivor.Entities    → Blob/*, Consumables/*, Enemies/*, Pedestrians/* (M24)
BlobSurvivor.Systems     → Pool/*, Score/*, Wave/*, Upgrade/*
BlobSurvivor.UI          → HUDController, UpgradePanel, GameOverScreen, SafeAreaHandler
BlobSurvivor.Data        → ScriptableObject'ler (ConsumableData, EnemyData, vb.)
BlobSurvivor.Input       → InputManager, VirtualJoystick
```

---

## Layer Sistemi

| Layer | Index | Açıklama |
|-------|-------|----------|
| Blob | 8 | Oyuncu blob'u |
| ConsumableTier1 | 9 | Tiny tier consumable |
| ConsumableTier2 | 10 | Small tier consumable |
| ConsumableTier3 | 11 | Medium tier consumable |
| ConsumableTier4 | 12 | Large tier consumable |
| ConsumableTier5 | 13 | Giant tier consumable |
| Enemy | 14 | Düşmanlar |
| Hazard | 15 | Zararlı objeler |
| Environment | 16 | Çevre objeleri |
| Ground | 17 | Zemin |
| Projectile | 18 | Silah mermileri (WeaponBase/Projectile); Collision Matrix'te Enemy + ConsumableTier1-5 ile çarpışacak şekilde ayarlı |

**ConsumableBase layer hesabı:** `8 + (int)_data.RequiredTier` → Tiny(1)→9, Small(2)→10, ...

**Collision Matrix:** Blob layer'ı consumable layer'larıyla çarpışır. Her tier için ayrı layer var çünkü blob sadece kendi tier'ından küçük şeyleri yiyebilir.

---

## Blob Büyüme Formülü

```csharp
// Smooth sürekli büyüme — tak diye seviye atlamaz
float scale = baseScale * Mathf.Pow(1f + currentMass * growthFactor, growthExponent);

// Varsayılan değerler
float baseScale = 0.5f;
float growthFactor = 0.5f;
float growthExponent = 0.4f;
```

**Tier eşikleri (test değerleri):**
- Tiny: 0 mass
- Small: 10 mass
- Medium: 30 mass
- Large: 60 mass
- Giant: 100 mass

**XP & Level sistemi (BlobGrowth içinde):** Mass kazanmak XP olarak da sayılır. Her level'da eşik büyür:

```csharp
// AddMass çağrıldıkça AddXP da çalışır
xpThreshold = baseXPThreshold + currentLevel * xpGrowthPerLevel;
// Varsayılan: baseXPThreshold=20, xpGrowthPerLevel=15
```

XP eşiği geçilince `GameEvents.RaiseLevelUp(level)` ateşlenir → `UpgradeSystem` 3 seçenek sunar.

---

## BlobTier Enum

```csharp
// GameEvents.cs içinde
public enum BlobTier { Tiny = 1, Small = 2, Medium = 3, Large = 4, Giant = 5 }
```

---

## Object Pool Yapısı

`ObjectPool<T>` — MonoBehaviour **değil**, sıradan generic class. Unity'de filename=classname zorunluluğu nedeniyle iki ayrı dosyada:

- `ObjectPool.cs` → `ObjectPool<T> where T : MonoBehaviour`
- `PoolManager.cs` → Singleton MonoBehaviour, `Dictionary<Object, object>` key olarak prefab referansı kullanır (GetInstanceID deprecated olduğu için)

```csharp
// Kullanım
PoolManager.Instance.CreatePool(prefabBase, initialSize);
PoolManager.Instance.GetPool<T>(prefab).Get(position, rotation);
pool.Return(enemy);
```

---

## GameEvents — Event Bus

`GameEvents.cs` static event bus. Tüm oyun geneli eventler burada:

```csharp
GameEvents.OnBlobSizeChanged       // float mass
GameEvents.OnBlobTierChanged       // BlobTier tier
GameEvents.CurrentBlobTier         // M25: statik property (event değil) — RaiseBlobTierChanged içinde güncellenir, spawn/pool-reuse anında Find'a gerek kalmadan senkron okunur
GameEvents.OnScoreChanged          // int score
GameEvents.OnXPChanged             // int xp
GameEvents.OnLevelUp               // int level — UpgradeSystem dinler
GameEvents.OnGameOver
GameEvents.OnGamePaused
GameEvents.OnGameResumed
GameEvents.OnUpgradeChoicesReady   // UpgradeData[] — UpgradePanel dinler
GameEvents.OnUpgradeSelected       // UpgradeData — UpgradeSystem dinler, efekti uygular
GameEvents.OnHealthChanged         // float current, float max
GameEvents.OnSurvivalTimeUpdated   // float seconds — WaveController dinler
GameEvents.OnConsumedCountChanged  // int count
GameEvents.OnCharacterSelected     // CharacterData — GameManager.StartGame(data)'da ateşlenir
GameEvents.OnCoinsChanged          // int total — HUD/GameOver coin UI dinler
GameEvents.OnBossHealthChanged     // float current, float max — A15; max<=0 = aktif boss yok (B18 HUDController._bossHealthBar bunu dinler)
GameEvents.OnFinalBossConsumed     // A16; final boss yenebilir fazda yutulunca ateşlenir — A17 GameManager bunu dinleyip run'ı kapatır
GameEvents.OnRunComplete           // A17: RunEndReason (FinalBossConsumed/TimeSurvived) — run GameOver'dan (ölüm) ayrı, "başarıyla" kapanınca
```

Raise metodları: `GameEvents.RaiseBlobSizeChanged(mass)` vs.

---

## Yazılmış Scriptler

### Core
| Script | Açıklama |
|--------|----------|
| `GameManager.cs` | Singleton; GameState (Menu/Playing/Paused/LevelUp/GameOver/RunComplete — A17); `Start()` artık `StartGame()` çağırmaz — Lobby, karakter seçilene kadar açık kalır; `StartGame(CharacterData)` overload'ı pasifi uygular + başlangıç silahını spawnlar; A17: `OnFinalBossConsumed`'ı dinler + `_runTimeoutSeconds` (varsayılan 900s/15dk) güvenlik eşiği — ikisi de `TriggerRunComplete(RunEndReason)` çağırıp `GameEvents.RaiseRunComplete` ateşler (normal ölüm/`TriggerGameOver`'dan bilinçli olarak ayrı); M22: `ClearPreviousRunState()` artık `BlobGrowth.ResetGrowth()`/`BlobHealth.ResetHealth()` de çağırıyor, yeni `ApplyMetaProgression(blob, character)` `MetaProgression.Instance`'ın bonuslarını (Hız/MaxCan/MassKazanımı/CoinKazanımı/XPÇarpanı) karakterin kendi pasifiyle birleştirip uyguluyor — hız artık tek yerden (switch'teki `MoveSpeed` case'i boşaltıldı, overwrite çakışmasın diye) |
| `GameEvents.cs` | Static event bus + BlobTier enum; `RunEndReason` enum (A17, `GameManager.cs`'te tanımlı) |
| `CameraController.cs` | Blob'u smooth takip eder, sabit yükseklik (tier değişince zoom YOK — kullanıcı istemedi) |

### Data (ScriptableObject)
| Script | Alanlar |
|--------|---------|
| `ConsumableData.cs` | displayName, prefab, scoreValue, massValue, objectSize, requiredTier, isHazard, hazardAmount |
| `EnemyData.cs` | displayName, prefab, maxHealth, damage, moveSpeed, attackRange, attackCooldown, spawnTier, scoreValue; A6 için `IsElite`, elit attack hit count/interval; A15 için `PreventConsumption` (true ise `EnemyBase.TryConsumeByBlob` her tier'da `false` döner — miniboss/final boss yutulamaz) |
| `UpgradeData.cs` | id, displayName, description, icon, category (`UpgradeCategory`: Mobility/Defense/Magnetic/Weapon/**Feeding** — B16'da sona eklendi), weight, effectValue, effectDuration, cooldown, maxLevel, perLevelValue; B17: `IsEvolution` (true ise `UpgradePanel` kartı ★+altın renkle işaretler) |
| `WaveData.cs` | timeThreshold, enemyTypes (EnemySpawnEntry[]), spawnRate, maxActiveCount, waveName |
| `CharacterData.cs` | displayName, icon, description, startingWeaponPrefab, passiveType (MoveSpeed/MagnetPull/ConsumableSplit), passiveValue; #48: `_requiresMarketUnlock` — true ise `LobbyPanel` bu karakteri `MetaProgression.MiknatoUnlocked` satın alınana kadar kilitli gösterir (`Char_Miknato.asset`'te `1`, diğer ikisinde varsayılan `0`) |
| `BossData.cs` | A15: displayName, stages (`BossStage[]` — her biri `EnemyData` + `SpawnTime` saniye + `BonusCoinMin/Max`); "aynı tasarım artan stat" bu dizi üzerinden veri-odaklı sağlanır, ayrı bir boss davranış script'i yok |
| `SkillEvolutionData.cs` | B17: `InputA`/`InputB` (iki `UpgradeData`, ikisi de max level'a ulaşmalı) + `Output` (evrim çıktısı `UpgradeData`) — `EvolutionSystem` bunu okur |
| `PedestrianData.cs` | M24: `ConsumableData`'yı miras alır (RequiredTier hem yeme hem kaçış eşiği, ayrı field yok) + moveSpeed, fleeSpeed, wanderRadius, fleeTriggerDistance, bobHeight, bobSpeed |

### Entities / Blob
| Script | Açıklama |
|--------|----------|
| `BlobController.cs` | Rigidbody hareketi; `linearDamping`/`angularDamping`; tier'a göre hız: `1f / Sqrt((float)tier)` |
| `BlobGrowth.cs` | Smooth scale formülü; tier hesabı; `PunchScale()` yeme feedback'i; B16: `_massGainMultiplier` (Sindirim skill'i) — `AddMass()`'ta hem mass hem XP'ye uygulanır (mass=XP birleşik, Karar 7); M22: ayrı `_xpBonusMultiplier` (meta "XP Çarpanı", mass'ı etkilemeden sadece `AddXP`'yi çarpar) + `ApplyMetaBonuses(massGainBonus, xpBonus)` (overwrite, `GameManager` her run başında çağırır); `ResetGrowth()` (restart fix, 2026-08-22) mass/XP/level/tier'ı ve iki çarpanı da sıfırlar |
| `BlobConsumption.cs` | OnTriggerEnter → IConsumable check → tier karşılaştır → Consume(); mobil haptic; B16: enemy yutma dalında `ConsumptionBonusComponent` varsa efektif tier'ı yükseltir (Yırtıcı Çene), her yemede `ApplyHealOnConsume()` çağırır (Yutuş); B17: `ConsumeDirect()` public wrapper — `BlackHoleComponent`'in temassız otomatik yutması için |
| `BlobHealth.cs` | TakeDamage(amount, DamageType); armor; regen; OnDeath → GameOver; M22: `ResetHealth()` — `_maxHealth`'i `Awake()`'te cache'lenen orijinal değere döndürür, can'ı doldurur, zırh/regen'i sıfırlar (restart fix — bkz. Önemli Kararlar) |
| `ConsumptionBonusComponent.cs` | B16 (Yırtıcı Çene skill'i): `TierBonus` int — `BlobConsumption` bunu enemy yutma tier eşiğine ekler, normal consumable yemeyi etkilemez |
| `HealOnConsumeComponent.cs` | B16 (Yutuş skill'i): `HealAmount` — `BlobConsumption` her yemede bu kadar `BlobHealth.Heal()` çağırır |
| `BlackHoleComponent.cs` | B17 evrimi "Kara Delik" (Vakum+Sindirim max): throttle'lı (0.1s) `OverlapSphereNonAlloc` ile küçük bir iç yarıçaptaki tier1/2 consumable'ları `BlobConsumption.ConsumeDirect()` ile temassız yutar |
| `AvciFormuComponent.cs` | B17 evrimi "Avcı Formu" (Yırtıcı Çene+Silah Gücü max): `EnemyBase.OnAnyEnemyDamaged` statik event'ini dinler, vurulan sürü düşmanını `MarkTemporarilyConsumable()` ile geçici tier-şartsız yutulabilir işaretler |

### Entities / Consumables
| Script | Açıklama |
|--------|----------|
| `IConsumable.cs` | Interface: Data, RequiredTier, OnConsumed() |
| `ConsumableBase.cs` | IConsumable impl; `OnEnable` artık `protected virtual` (`AssignLayer()` çağırır) — M24: `PedestrianController` override edip `base.OnEnable()` çağırıyor; `ReturnToOwner()` da `protected virtual` — varsayılan `ConsumableSpawner.Instance.ReturnToPool(this)`, `PedestrianController` kendi spawner'ına döner (bkz. aşağıda). M25: `Awake()`'te `Collider` cache'lenir; `SetData()`/`OnEnable()` ikisi de `UpdateTriggerState()` çağırır — Blob'un tier'ı yetmediği objelerde `collider.isTrigger=false` yapılıp Blob'un solid collider'ıyla fiziksel çarpma sağlanır, yeterli tier'a ulaşınca tekrar trigger'a döner (Hazard'lar bu davranışın dışında, hep trigger kalır) — `GameEvents.OnBlobTierChanged` event'ine abone olunur (`OnDisable`'da unsubscribe) |
| `ConsumableSpawner.cs` | Pool'dan spawn; başlangıçta 40, max 80; 2s'de bir refill; tier değişince bonus; Singleton (`Instance`); `ConsumeAndSplit()` — Pistol'ün büyük consumable'ı parçalaması için; 2026-08-31: `TryGetSpawnPosition()` artık `SpawnPositionUtility` ile NavMesh'e snap ediyor — bina gibi engellerin içine spawn olmayı engeller, bulamazsa o deneme atlanır |

### Entities / Pedestrians (M24)
| Script | Açıklama |
|--------|----------|
| `PedestrianController.cs` | `ConsumableBase`'i miras alır (IConsumable/layer/pool-return hazır gelir) — üzerine wander (rastgele waypoint, `PatrolState` deseni) + kaçış (`_blobGrowth.CurrentTier >= RequiredTier` VE blob `FleeTriggerDistance` içindeyse) + animasyonsuz sinüs-bob (`Cannon` mermisinin yay hareketiyle aynı teknik) ekler. `SwarmSteering` **kullanmaz** — o `EnemyBase`'e sıkı bağlı (dev-a dosyasına dokunmadan genelleştirilemezdi) ve yayaların ayrıca birbirinden kaçınmaya ihtiyacı yok. AI throttle 0.4s (staggered); 2026-08-31: `PickNewWanderTarget()` `SpawnPositionUtility` ile NavMesh'e snap ediyor, `WanderTargetMaxAttempts=5` deneme + hepsi başarısızsa `_spawnCenter`'a dönüş var (ilk versiyonda bulunamayınca `_wanderTarget` hiç güncellenmiyordu, yaya kalıcı donuyordu — `Move()` erken dönüyor ama `ApplyBob()` koşulsuz çalıştığı için sadece animasyon sürüyordu). **Bu tek başına yetmedi** (Serra Play Mode'da yine donma gördü) çünkü WanderRadius (8) blok aralığından (12) küçük — bina yoğun bölgede bir yaya her yönden kuşatılabiliyor, o zaman şans değil geometri gereği HİÇBİR rastgele hedef bulunamıyor. Asıl çözüm: `EnemyBase.ComputeAvoidance()` ile birebir aynı teknik eklendi — throttle'lı (aynı 0.4s tick) `Physics.Raycast` (Environment layer, `_avoidanceLookahead=1.2f`), yol üstünde engel varsa `Move()`'daki hareket yönüne `_avoidanceWeight=2f` ağırlıkla bir sapma karıştırılıyor. Artık hedefin kusursuz/tamamen ulaşılabilir olmasına gerek yok, yaya engelin etrafından sıyrılıp hareketine devam ediyor. **Bu da tam çözmedi** (Serra: "bir yere denk gelince farklı yöne gitmeyi denemiyorlar bile") — geçici bir tanı (`DiagnoseStuck()`, `#if UNITY_EDITOR`) eklenip Serra'nın paylaştığı loglarla **asıl kök sebep bulundu ve tanı kodu kaldırıldı:** `PickNewWanderTarget()`'ın `TryFindNavMeshPosition`'a `yOverride` olarak sabit `0f` vermesi — yaya spawn Y'si + bob nedeniyle hep ~0.75-0.85'te olduğundan, `_wanderTarget.y=0` sahte bir ~0.8 birimlik fark yaratıyordu. `Move()`'daki "hedefe ulaştım mı" kontrolü bu farkı Y **düzleştirmeden** (3D) ölçtüğü için eşiği (0.04) hep aşıyordu — "ulaştım" hiç tetiklenmiyordu; X/Z'de gerçekten hedefe varan yaya (log kanıtı: `seekDist` her satırda `pos.y`'ye birebir eşitti) hiç yeni hedef seçmeden kalıcı duruyordu. Bina/consumable ile ilgisi yoktu (Serra'nın ilk şüphesi), tamamen kendi eklediğim bug'dı. İki parçalı fix: (1) `PickNewWanderTarget()` artık `_spawnCenter.y` kullanıyor, `0f` değil; (2) `Move()`'da `direction.y = 0f` "ulaştım mı" kontrolünden ÖNCEye taşındı (kök düzeltme — artık `_wanderTarget`'ın Y'si ne olursa olsun kontrol doğru çalışır) |
| `PedestrianSpawner.cs` | `ConsumableSpawner`'ın pool/spawn-radius deseniyle aynı; `EnemySpawner`'ın canlı sayacına bağımlı olmadan (cross-file coupling riski) kendi sabit tavanıyla çalışır; 2026-08-31: `_maxActive` sahnede `25`'e ayarlı (GDD Karar 2 revizesi — bkz. Önemli Kararlar), `TryGetSpawnPosition()` artık `SpawnPositionUtility` ile NavMesh'e snap ediyor |

### Entities / Enemies
| Script | Açıklama |
|--------|----------|
| `EnemyBase.cs` | NavMeshAgent; state machine; PerformAttack() → BlobHealth.TakeDamage(); AI throttle (`AIUpdateInterval=0.15s`, randomize stagger) — `CanSeeBlob()` sqrMagnitude ile sadece throttle tick'te hesaplanıp cache'lenir, state'lere `aiTick` bool geçilir; B17: `TakeDamage()` her hasarda statik `OnAnyEnemyDamaged` event'i ateşler (`AvciFormuComponent` dinler), `MarkTemporarilyConsumable(duration)` + `TryConsumeByBlob`'daki `tierBypassed` kontrolü (sadece non-elite) Avcı Formu'nun tier-şartsız yutmasını sağlar |
| `EnemySpawner.cs` | Y=0.65f'te spawn; weighted random enemy seçimi; 2026-08-31: `_maxActiveEnemies` sabit `15` (eski tier-bazlı `40*tier+10, max 200` ölçekleme kaldırıldı — GDD Karar 2 revizesi, bkz. Önemli Kararlar), `_maxActiveElites` `8`→`4`; `TryGetSpawnPosition()` artık `SpawnPositionUtility` üzerinden NavMesh'e snap ediyor, bulamazsa (`false`) o spawn denemesi atlanır — eskiden bulamayınca off-mesh ham adaya (potansiyel olarak bir binanın içine) düşüyordu |
| `BossSpawner.cs` | A15: `BossData.Stages`'i sırayla `GameEvents.OnSurvivalTimeUpdated`'a göre zamanlı spawnlar (EnemySpawner'ın ağırlıklı havuzundan bağımsız, tekil spawn); aynı anda tek boss; ölünce bonus coin (`CoinSpawner`) + `EnemyBase.CurrentHealth/MaxHealth` polling ile `GameEvents.OnBossHealthChanged` yayınlar; restart'ta `OnCharacterSelected` ile `_nextStageIndex` sıfırlanır. A16: final boss da aynı `BossData_Miniboss.asset`'in 3. stage'i olarak eklendi (SpawnTime 720) — ayrı bir spawner gerekmedi |
| `FinalBossController.cs` | A16 (Karar 1+8): `EnemyBase`'e `[RequireComponent]`; `Update()`'te HP oranı `_edibleHealthFraction`'ın altına inince `EnemyBase.SetConsumableOverride(true)` çağırır (yenebilir faz); `EnemyBase.OnDeath`'i dinleyip `GameEvents.RaiseFinalBossConsumed()` ateşler |
| `IEnemyState.cs` | Enter, Update, Exit |
| `PatrolState.cs` | 3s'de bir random waypoint; blob görünce ChaseState |
| `ChaseState.cs` | Blob'a koş; `SetDestination()` (NavMesh pathfinding, pahalı) sadece `aiTick=true` iken çağrılır — her frame değil; attack range'e girince AttackState (sqrMagnitude); göremezse PatrolState |
| `AttackState.cs` | Dur, cooldown'da PerformAttack(); uzaklaşınca ChaseState (sqrMagnitude) |
| `WaveController.cs` | OnSurvivalTimeUpdated dinler; en yüksek geçilen threshold'u aktif dalga yapar; A5 dakika bazlı zorluk çarpanlarını tutar (`DamageMultiplier`, `SpeedMultiplier`, `SpawnDensityMultiplier`) |

### Entities / Coins
| Script | Açıklama |
|--------|----------|
| `CoinPickup.cs` | Blob trigger'a girince `ScoreSystem.AddCoin(amount)` çağırır; collect sonrası spawner/pool'a dönüş event'i verir |
| `CoinSpawner.cs` | Coin prefab pool'unu yönetir, düşman ölümünde coin'i pool'dan spawn eder |

### Entities / Loot
| Script | Açıklama |
|--------|----------|
| `ChestPickup.cs` | Elit sandık pickup'ı; blob dokununca coin verir ve `GameEvents.RaiseLevelUp(currentLevel)` ile skill seçim akışını tetikler; kendi etrafında döner |
| `ChestSpawner.cs` | Chest prefab pool'unu yönetir; elit düşman ölümünde sandığı pool'dan spawn eder |

### Entities / Weapons
| Script | Açıklama |
|--------|----------|
| `WeaponBase.cs` | Abstract; `OverlapSphereNonAlloc` ile Enemy layer'da en yakın hedefi bulur (arama sadece fireRate cooldown'ında — throttle otomatik); pooled `Projectile` spawn eder; `IncreaseDamage()` — WeaponUpgradeEffect kullanır |
| `Projectile.cs` | Pooled mermi; **Rigidbody/Collider YOK** — her frame `Physics.SphereCast` ile Enemy+ConsumableTier1-5 mask'inde manuel sweep hit-check (mobilde fizik motoruna bağımlı olmadan, tünelleme riski de düşük); hit olmazsa/`OnHitOther` false dönerse mermi kalan mesafeyi o frame tamamlar (pas geçer); `OnHitEnemy`/`OnHitOther` (bool döner: true=dur, false=geç) virtual hook'ları alt sınıflar için |
| `CannonWeapon.cs` / `CannonProjectile.cs` (Topik) | Sinüs eğrili "arcing" mermi; çarpışınca küçük AoE (OverlapSphere → çevredeki düşmanlara da hasar) |
| `MetalBallWeapon.cs` / `HomingProjectile.cs` (Mıknato) | Yavaş, `Vector3.Slerp` ile hedefe bükülen homing mermi |
| `PistolWeapon.cs` / `PistolProjectile.cs` (Mermo) | Düz hızlı mermi; `RequiredTier > Small` olan consumable'a vurursa `ConsumableSpawner.ConsumeAndSplit()` ile parçalara ayırır |

### Systems
| Script | Açıklama |
|--------|----------|
| `ObjectPool.cs` | Generic pool, Get/Return/CreateInstance |
| `PoolManager.cs` | Singleton; tüm pool'ları yönetir |
| `ScoreSystem.cs` | AddScore, multiplier, PlayerPrefs highscore, ResetScore; A7/B9 için session coin sayısı, `AddCoin(int)`, `OnCoinsChanged`, `PreviousHighScore`, `HasNewHighScore`; M22: `SetCoinGainMultiplier()` (meta "Coin Kazanımı" bonusu) — `AddCoin()`'e uygulanır, `ResetScore()`'da 1f'e döner |
| `Spawning/SpawnPositionUtility.cs` | 2026-08-31: `TryFindNavMeshPosition(candidate, yOverride, sampleRadius, out result)` — `EnemySpawner`/`ConsumableSpawner`/`PedestrianSpawner`/`PedestrianController`'ın ortak ihtiyacı (greybox/gerçek bina engellerinin içine spawn/wander etmemek) için tek yerde `NavMesh.SamplePosition` sarmalayıcısı; bulamazsa `false` döner, çağıran taraf o denemeyi atlar |

### Systems / Meta (B19/M20)
| Script | Açıklama |
|--------|----------|
| `SaveSystem.cs` | Singleton; `Application.persistentDataPath/analytics.json`'a `JsonUtility` ile yazar/okur; `RunRecord`/`AnalyticsSaveData` bu dosyada tanımlı; `MaxStoredRuns=100` (aşınca en eski kayıtlar budanır). 3. parti servis değil, sadece yerel iskele (Sprint 7+'ta GameAnalytics/Unity Analytics eklenebilir) |
| `RunAnalytics.cs` | Singleton; `OnUpgradeSelected`'i dinleyip seçilen skill id'lerini biriktirir, `OnGameOver`/`OnRunComplete`'te (`RunEndReason.ToString()`) `SaveSystem.SaveRun()`'ı tetikler; `OnCharacterSelected`'ta restart için sıfırlanır |
| `MetaProgression.cs` | M20 (Sprint 5, GDD §8): Singleton; `Application.persistentDataPath/meta_progression.json`'a ayrı bir dosyada (SaveSystem'in analytics log'undan bağımsız) kalıcı kredi + satın alınan stat seviyelerini yazar/okur. `OnGameOver`/`OnRunComplete`'i dinleyip session coin'ini krediye çevirir (ölüm %50, tamamlanan run %100 — `ScoreSystem.ResetScore()` bir sonraki `StartGame()`'e kadar çağrılmadığı için bu event'ler ateşlendiğinde `Coins` hâlâ doğru değerde, `RunAnalytics`'in kullandığı aynı garanti). `MetaStatType` enum'u + `GetCost`/`IsPurchased`/`TryPurchase` API'si Market UI'nin (M21) ve run başı stat uygulamasının (M22) tüketeceği kontrat. Kademeli statlar (Hız/MaxHP/Mass/Coin kazancı) sınırsız tekrar satın alınabilir — GDD'de üst sınır belirtilmemiş, bilinçli varsayım. |

### Systems / Upgrade
| Script | Açıklama |
|--------|----------|
| `UpgradeEffect.cs` | Abstract ScriptableObject; `Apply(blobRoot, data)` |
| `UpgradeSystem.cs` | OnLevelUp dinler; weight bazlı 3 seçenek sunar; OnUpgradeSelected dinler, efekti uygular, oyunu resume eder; B17: `_dynamicUpgrades` listesi + `RegisterDynamicUpgrade()` — `EvolutionSystem`'in havuza runtime'da skill eklemesi için (sahne dizisi `_allUpgrades` sabit olduğundan ayrı bir liste gerekti) |
| `EvolutionSystem.cs` | B17: `OnUpgradeSelected`'i dinler, her `SkillEvolutionData` için `InputA`/`InputB` max level'a ulaştı mı kontrol eder (`UpgradeSystem.GetLevel` üzerinden); ulaştıysa `Output`'u `UpgradeSystem.RegisterDynamicUpgrade()` ile havuza ekler (bir kere, `_unlocked` HashSet ile korunur) |
| `SpeedBoostEffect.cs` | BlobController.SetSpeedMultiplier artırır |
| `DamageReductionEffect.cs` | BlobHealth armor multiplier düşürür (daha az hasar) |
| `RegenBoostEffect.cs` | BlobHealth regen rate artırır |
| `HealthBoostEffect.cs` | BlobHealth max health artırır |
| `ScoreMultiplierEffect.cs` | ScoreSystem multiplier artırır |
| `VacuumEffect.cs` | Blob'a VacuumComponent ekler/radius artırır; sadece yenebilir tier consumable'ları çeken Vakum skill'i |
| `WeaponUpgradeEffect.cs` | `blobRoot.GetComponentInChildren<WeaponBase>()` ile aktif silahı bulur, `IncreaseDamage(PerLevelValue)` çağırır — GDD'deki "Saldırı: Silah" kategorisi için (A1-A4/B1-B5 backlog'unda yoktu, sonradan eklendi). **İleride düşünülecek:** mermi hızı (`Projectile._speed`) ya da yön/mermi sayısı (multi-shot) artırma gibi ek boyutlar eklenebilir — şu an `UpgradeData`'da tek bir `PerLevelValue` alanı olduğu için sadece damage'a bağlandı; ikinci bir stat eklenecekse `UpgradeData`'ya yeni bir alan (örn. `_secondaryPerLevelValue`) gerekir |
| `SindirimEffect.cs` | B16: `BlobGrowth.IncreaseMassGainMultiplier(PerLevelValue)` çağırır — yenen her şeyden kazanılan mass'ı (=XP) çarpar |
| `YutusEffect.cs` | B16: Blob'a `HealOnConsumeComponent` ekler/`IncreaseHealAmount(PerLevelValue)` çağırır — her yemede küçük heal |
| `YirticiCeneEffect.cs` | B16: Blob'a `ConsumptionBonusComponent` ekler/`IncreaseTierBonus` çağırır — düşman yutma tier eşiğini gevşetir |
| `KaraDelikEffect.cs` | B17 evrim çıktısı: Blob'a `BlackHoleComponent` ekler (Vakum+Sindirim max → temassız otomatik yutma) |
| `AvciFormuEffect.cs` | B17 evrim çıktısı: Blob'a `AvciFormuComponent` ekler (Yırtıcı Çene+Silah Gücü max → vurulan sürü düşmanı geçici tier-şartsız yutulabilir) |
| `VacuumComponent.cs` (Entities/Blob) | OverlapSphere ile yakındaki IConsumable'ları bulur, transform'u blob'a doğru taşır (consumable'larda Rigidbody YOK, force çalışmaz — `Vector3.MoveTowards` kullanılır); `BlobGrowth.CurrentTier` altındaki/aynı tier consumable'ları çeker. Not: CLAUDE.md önceden ayrı bir `MagnetComponent.cs`/`MagnetEffect.cs`'den bahsediyordu ama kod tabanında hiç yok — `MagnetPull` karakter pasifi de (`GameManager.ApplyCharacter`) fiilen bu component'i kullanıyor, ayrı bir Magnet script'i yok (2026-08-18'de fark edilip düzeltildi) |

### UI
| Script | Açıklama |
|--------|----------|
| `HUDController.cs` | Health/XP bar, skor, coin, timer, tier, level text; aktif skill rozetleri ve karakter ikonu; GameEvents dinler; B18: `_bossHealthContainer`/`_bossHealthBar` — `OnBossHealthChanged`'da `max<=0` iken container gizli, aktif boss varken görünür |
| `UpgradePanel.cs` | OnUpgradeChoicesReady'de 3 buton gösterir; tıklayınca OnUpgradeSelected raise eder; B17: `UpgradeData.IsEvolution` true ise kart adını ★ ile işaretler + altın renk (`new Color(1f, 0.82f, 0.25f, 1f)`, coin/yeni-rekor rengiyle tutarlı) |
| `GameOverScreen.cs` | OnGameOver'da skor/highscore, coin, süre, önceki rekor ve yeni rekor banner'ı gösterir; restart → GameManager.StartGame() |
| `LobbyPanel.cs` | Oyun başında görünür, HUD'u gizler (`_hud.SetActive(false)`); 3 karakter butonu (icon + isim); tıklayınca paneli kapatır, HUD'u açar, `GameManager.Instance.StartGame(data)` çağırır; M23: `_marketButton` → `OpenMarket()` (kendi panelini gizler, `MarketPanel.Show()` çağırır); public `Show()` — `MarketPanel`'in "geri" butonu Inspector'dan buna bağlanacak (döngüsel script referansı yok, iki OnClick listener); #48: `RefreshCharacterButtons()` (`Start()` ve `Show()`'da çağrılır — Market'ten dönüşte kilit durumu güncel kalsın diye) `CharacterData.RequiresMarketUnlock` true olan karakterleri `MetaProgression.MiknatoUnlocked` satın alınana kadar buton `interactable=false` + isim yanında "(Kilitli)" ile gösterir |
| `MarketPanel.cs` | M21 (GDD §8): 6 sabit kalemlik Market — `LobbyPanel`/`UpgradePanel` ile aynı index-aligned serialized array deseni (`_statTypes`/`_buttons`/`_labelTexts`, dinamik prefab yok); `MetaProgression.Instance`'tan `GetCost`/`IsPurchased`/`Credit` okuyup buton `interactable`/etiket metnini `Refresh()`'te günceller, tıklayınca `TryPurchase()` + tekrar `Refresh()`; `Show()`/`Hide()` public — `LobbyPanel.OpenMarket()` çağırır, "geri" butonu Inspector'dan `MarketPanel.Hide` + `LobbyPanel.Show`'a bağlanacak |
| `SafeAreaHandler.cs` | RectTransform'u Screen.safeArea'ya göre ayarlar (notch desteği) |

### Input
| Script | Açıklama |
|--------|----------|
| `InputManager.cs` | New InputSystem; InputAction Dpad composite binding; WASD + arrow keys; VirtualJoystick fallback |
| `VirtualJoystick.cs` | Dynamic floating joystick; ekranın sol yarısında tetiklenir; CanvasGroup show/hide |

### Editor Araçları (oyuna gömülmez, `Assets/_Project/Editor/`)
| Script | Açıklama |
|--------|----------|
| `GreyboxCityGenerator.cs` | 2026-08-31 (#44/#50 perf testi için): `Tools → Blob.io → Greybox Şehir Oluştur` — 6×6 grid'de rastgele boyutlu Cube'ları "bina" olarak yerleştirir (`Environment` layer + `NavigationStatic`), Blob'un spawn noktası (0,1,0) etrafında `BlobKeepClearRadius=8` boş bırakılır. NavMesh'i kendisi bake ETMEZ — proje `Unity.AI.Navigation` paketini kullanıyor, `Ground` GameObject'indeki `NavMeshSurface` component'inin Inspector'daki "Bake" butonuna elle basılması gerekiyor. `Tools → Blob.io → Greybox Şehiri Sil` ile kaldırılır. Gerçek şehir sanatı (Hüma/Bahar ile konuşulacak, bkz. Önemli Kararlar) gelene kadar geçici bir test aracı. |

---

## Bilinen Eksikler / TODO

- **Karakter iconları eksik:** `Char_Topik`, `Char_Miknato`, `Char_Mermo` (`Assets/_Project/Data/Characters/`) asset'lerinde `Icon` alanı boş — henüz görsel hazır değil. LobbyPanel'deki buton icon'ları bu yüzden şu an boş görünüyor. Icon'lar hazır olunca 3 asset'e de atanmalı.
- ~~Skill Evolution eksik~~ **(2026-08-20 tamamen çözüldü):** `SkillEvolutionData`/`EvolutionSystem` + Editor kurulumu (Kara Delik/Avcı Formu asset'leri + sahne wiring) tamamlandı, bkz. aşağıdaki not.
- ~~A17/B16/B17/B18/B19 Editor kurulumu + Play mode testi bekliyor~~ **(2026-08-20 tamamen çözüldü — GitHub issue #31–#35 ve tracking issue #27 kapatıldı):** Kod tarafı 2026-08-18'de tamamlanmıştı; Editor kurulumu Serra tarafından yapıldı: (1) B16 — `Upgrade_Sindirim/Yutus/YirticiCene.asset` + `FX_*` oluşturuldu, `UpgradeSystem._allUpgrades`/`HUDController._allUpgrades`'e eklendi; (2) B17 — `Upgrade_KaraDelik/AvciFormu.asset` (`IsEvolution=true`) + `FX_*` + `Evolution_Karadelik/AvciFormu.asset` (SkillEvolutionData: Kara Delik=Vacuum+Sindirim→KaraDelik, Avcı Formu=YirticiCene+Weapon→AvciFormu) oluşturuldu, sahneye `EvolutionSystem` GameObject'i `_evolutions` dizisiyle eklendi; (3) B18 — HUD'a `HealthBar` deseni kopyalanarak `BossHealthBar` Slider hiyerarşisi eklendi, `HUDController._bossHealthContainer`/`_bossHealthBar`'a bağlandı; (4) B19 — sahneye `MetaSystems` GameObject'i `SaveSystem`+`RunAnalytics` component'leriyle eklendi; (5) A17 zaten ek Editor adımı gerektirmiyordu. Doğrulama: guid eşleştirmeleriyle tüm sahne/asset referansları (evrim InputA/InputB/Output, `_allUpgrades` dizileri, boss health/meta wiring) kontrol edildi; Serra'nın bu oturumda 3 kez girdiği Play Mode'da (`~/Library/Logs/Unity/Editor.log`) hiçbir compile hatası ya da `BlobSurvivor` namespace'inden exception görülmedi. `Upgrade_AvciFormu.asset`'te başta boş kalan `Id`/`Display Name` de dolduruldu. Kalan tek bilinçli boşluk: tüm upgrade asset'lerinde `Icon` hâlâ boş (yukarıdaki karakter icon'u eksikliğiyle aynı, ayrı bir sanat TODO'su).
- ~~A11/A14 sürü steering'in Editor kurulumu eksik~~ **(2026-08-12 çözüldü):** A15 sırasında fark edildi — kod tarafı A14'te tamamlanmış görünüyordu ama sahnede gerçekten `SwarmSteering` component'li bir GameObject **hiç yoktu** (yani sürü düşmanları o ana kadar separation/engel kaçınma olmadan, sadece seek ile hareket ediyordu — sessiz bir eksiklik, hata vermiyordu). `GameScene.unity`'ye elle (Unity kapalıyken, text-serialized YAML üzerinden — bkz. A15 notu) `SwarmSteering` GameObject'i eklendi ve `SceneRoots.m_Roots`'a kaydedildi. **Unity'de açılıp Hierarchy'de doğrulandı (2026-08-12) — `SwarmSteering` doğru göründü.**
- **A14 FPS/Profiler doğrulaması yapılmadı (GitHub #44):** Kod tarafı (throttle, komşu sayısı sınırı, elit tavanı) tamamlandı ama gerçek FPS ölçümü Unity Editor/cihaz gerektiriyor, buradan yapılamadı. Test için: Window → Analysis → Profiler (CPU), ya da Game view Stats overlay; **hedef artık "30 FPS @ 720p, 15 eşzamanlı polis + 25 yaya"** (2026-08-31'de revize edildi, bkz. Önemli Kararlar — eski 150-200 hedefi geçersiz). **2026-08-31: Serra macOS standalone Development Build + Autoconnect Profiler ile teste başladı** — Build Settings'te sadece `SampleScene.unity` vardı, `GameScene.unity` eksikti, düzeltilmesi gerekti.
~~M21/M23 (Sprint 5) Editor kurulumu bekliyor — Market UI~~ **(2026-08-22 tamamlandı):** Serra Canvas hiyerarşisini (kredi text, 6 buton+label, Geri butonu) kurdu, `MarketPanel`/`LobbyPanel` alanlarını doldurdu. **Kurulum sırasında iki Editor hatası yapılıp düzeltildi (ikisi de not almaya değer, muhtemelen tekrar karşılaşılır):** (1) UnityEvent (OnClick) object slotuna Project penceresinden **script dosyasını** sürüklemek (`MarketPanel.cs`) fonksiyon listesini "MonoScript" kategorisine düşürüyor (`Hide()`/`Show()` gibi component metodları hiç görünmüyor) — doğrusu Hierarchy'den GameObject'i (component'in üzerinde durduğu obje) sürüklemek. (2) Geri butonunun iki OnClick satırından ikisi de yanlışlıkla `MarketPanel`'e bağlanmıştı (`MarketPanel.Hide` + `MarketPanel.Show`) — ikinci satır `LobbyPanel.Show` olmalıydı; tıklama kayıtlanıyordu ama Hide+Show birbirini iptal ettiği için hiçbir şey olmuyormuş gibi görünüyordu. Ayrıca `PedestrianSpawner` (`_maxActive` 20→12, `_initialSpawnCount` 12→5) ve `PedestrianData._objectSize` (0.5→1) ayarlandı. Derleme + fonksiyonel test Serra tarafından yapıldı, sorun bulunmadı. ~~Bilinçli olarak hâlâ yapılmadı: Mıknato'nun Lobby'de Market'ten alınana kadar kilitli görünmesi~~ **(2026-08-31 çözüldü — GitHub #48, bkz. aşağıdaki Önemli Kararlar notu).**
~~M24 (Sprint 5) Editor kurulumu bekliyor~~ **(2026-08-22 tamamlandı):** `Pedestrian.prefab` (capsule + `Mat_Pedestrian`, Is Trigger sonradan düzeltildi — ilk halinde solid'ti, bkz. yukarıdaki sohbet), `PedestrianData.asset` (`Required Tier = Small`, `Spawn Y Offset = 0.75` — ilk 0.15 kalmıştı, yaya zemine gömülüyordu, düzeltildi), sahnede `PedestrianSpawner` (`_maxActive=12`, `_initialSpawnCount=5` — ilk 20/12 fazla yoğundu, azaltıldı). Play mode'da test edildi: wander/kaçış/yeme, fiziksel çarpma (M25) çalışıyor.
~~B13 yeme mekaniği için Collision Matrix doğrulaması gerekiyor~~ **(2026-08-31 çözüldü — GitHub #46 kapatıldı):** `ProjectSettings/DynamicsManager.asset`'teki `m_LayerCollisionMatrix` hex verisi Unity açmadan decode edildi (32×32-bit mask). Blob(8)×Enemy(14) biti işaretli — yutma tetiklenmesi fizik tarafında sorunsuz. Decode yöntemi, Projectile(18) satırının CLAUDE.md'de zaten dokümante edilen davranışıyla (Enemy+ConsumableTier1-5 ile çarpışır, Blob ile çarpışmaz) birebir eşleşerek çapraz doğrulandı.
~~Restart'ta run-scoped upgrade efektleri tam sıfırlanmıyor (A10'un bilinçli kapsam dışı bıraktığı)~~ **(2026-08-22, M22 sırasında büyük ölçüde çözüldü):** Yeniden denetlendiğinde üçün ikisi zaten sorunsuzdu — `WeaponBase.IncreaseDamage` (Silah Gücü) `ClearPreviousRunState`'in silahı `Destroy` edip sıfırdan `Instantiate` etmesiyle örtük olarak sıfırlanıyor (yeni instance = prefab varsayılanı); `ScoreSystem`'in `ScoreMultiplier`'ı da `StartGame()`'in her çağırdığı `ResetScore()` içinde zaten `1f`'e dönüyordu. Gerçek eksik `BlobHealth`'teydi (Zırh/`DamageReductionEffect`, Rejenerasyon/`RegenBoostEffect`, Max Can/`HealthBoostEffect`) — `BlobHealth.ResetHealth()` eklenip `GameManager.ClearPreviousRunState()`'e bağlandı (bkz. aşağıdaki M22 notu).
~~`BlobGrowth.CurrentMass`/`CurrentTier` restart'ta hiç sıfırlanmıyor~~ **(2026-08-22 düzeltildi):** `BlobGrowth.ResetGrowth()` eklendi (mass/XP/level/tier'ı Tiny'ye sıfırlar, scale'i anında `_baseScale`'e döndürür, tier gerçekten değiştiyse `GameEvents.RaiseBlobTierChanged` ateşler — bu da M25'in `GameEvents.CurrentBlobTier` cache'ini ve tüm aktif consumable'ların `isTrigger` durumunu doğru senkronlar). `GameManager.ClearPreviousRunState()`'e (zaten restart'ta silah/Vakum temizleyen metod) bir satır eklendi: `blob.GetComponent<BlobGrowth>()?.ResetGrowth();`. **Kasıtlı olarak dokunulmadı:** `_massGainMultiplier` (Sindirim skill'i) — bu, yukarıdaki "run-scoped upgrade efektleri sıfırlanmıyor" maddesinin kapsamında, ayrı bir iş. **Unity açıkken yazıldı — derleme + gerçek restart testi (blob görsel olarak küçülüyor mu, tekrar Tiny objelerle etkileşiyor mu) doğrulanmadı.**
- ~~A15 Miniboss görsel placeholder kullanıyor~~ **(2026-08-12 çözüldü):** Serra Editor'da `Enemy_Miniboss.prefab` + `Mat_Enemy_Miniboss.mat` oluşturdu, iki stage asset'inin `_prefab` alanı buna çevrildi. Artık elit polisten görsel olarak ayrışıyor.
- ~~A15 Play mode fonksiyonel testi yapılmadı~~ / ~~A16 Final Boss prefab'ı eksik~~ / ~~A16 kod tarafı test edilmedi~~ **(2026-08-16 çözüldü):** Serra Editor'da `Enemy_FinalBoss.prefab`'ı `Enemy_Miniboss.prefab`'tan duplicate edip materyalini farklılaştırdı, `FinalBossController` component'ini ekledi, `EnemyData_FinalBoss.asset`'e atadı. Play mode'da geçici olarak hızlandırılmış spawn süreleriyle (5/10/15sn) uçtan uca test edildi: miniboss stage1→stage2→final boss sırayla doğru HP'yle spawnladı (`[BossSpawner] Spawn: ... CurrentHealth=X, MaxHealth=X` logları — bkz. aşağıdaki HP bug fix), final boss silahla öldürülemedi (HP 1'de kilitlendi), HP %25 altına inince yenebilir faza geçti (`[FinalBossController] Yenebilir faza geçti` logu), Tier5 blob temasla yutabildi (`OnFinalBossConsumed` ateşlendi, bonus coin düştü). Test sonrası `BossData_Miniboss.asset`'in spawn süreleri prodüksiyon değerlerine (240/480/720) geri alındı; `BossSpawner`/`FinalBossController`'daki `#if UNITY_EDITOR` Debug.Log'ları (GameManager.ChangeState'teki pattern'le tutarlı, build'e girmiyor) kalıcı olarak bırakıldı.
- **`Enemy_Miniboss.prefab`'ın `EnemyBase._data` alanı yanlış referans taşıyordu (kozmetik, `SetData()` HP fix'inden sonra zaten zararsızdı) — Serra Editor'da düzeltti (2026-08-16):** `EnemyData_Miniboss_Stage1`'e çekildi, `Enemy_FinalBoss.prefab`'ınki de `EnemyData_FinalBoss`'a ayarlandı.

---

## Önemli Kararlar / Geçmiş Düzeltmeler

- **A9 Enemy pool return + Warp fix tamamlandı:** `EnemyBase`'e `OnDeath` event'i eklendi, `Die()` artık `SetActive(false)` çağırmıyor — dönüş sorumluluğu `EnemySpawner.HandleEnemyDeath`'te (CoinSpawner pattern'i). Ek olarak `_isDead` guard'ı eklendi (aynı frame'de çift ölüm → pool Queue'suna çift ekleme riskine karşı). Spawn'da `EnemyBase.WarpTo()` → `NavMeshAgent.Warp()` kullanılıyor.
- **A10 Restart flow fix tamamlandı:** `GameManager.ApplyCharacter` artık restart'ta eski silahı (`WeaponBase` child) ve eski `VacuumComponent`'i `Destroy` edip sıfırdan kuruyor (stack'lenme yok). Ayrıca daha köklü bir bug bulundu ve düzeltildi: `GameOverScreen.Restart()` parametresiz `StartGame()` çağırıyordu, bu da her zaman `_defaultCharacter`'ı uyguluyor, oyuncunun lobide seçtiği karakteri kayboluyordu — `GameManager._lastCharacter` eklendi.
- **A11 Sürü steering spike tamamlandı:** `SwarmSteering.cs` (spatial-hash, `Systems/Steering/`) eklendi. `EnemyBase.UsesSteering` (`!Data.IsElite`) elit olmayan düşmanlarda `NavMeshAgent`'ı devre dışı bırakıp seek+separation ile hareket ettiriyor; elit/boss NavMeshAgent'ta kalıyor. `PatrolState`/`ChaseState`/`AttackState` değişmedi (zaten `SetDestination`/`StopMoving` soyutlaması üzerinden çalışıyorlardı). **Bilinen sınırlama:** NavMesh'e bağlı olmadığı için engel kaçınması yok — Sprint 4 A14'te ele alınacak. **Editor kurulumu gerekiyor** (yukarıya bkz.).
- **A12 Yutulabilirlik API'si tamamlandı:** `EnemyBase.TryConsumeByBlob(BlobTier blobTier, out float massReward)` — sürü Tier3+, elit Tier5'te `true` döner + `Die()`'ı tetikler. `EnemyData`'ya `_massReward` alanı eklendi (Police=3, ElitePolis=20). **B13 için API sözleşmesi budur** — `BlobConsumption.cs` bu imzayı çağırıp dönen `massReward`'ı `BlobGrowth.AddMass()`'a geçmeli.
- **B12 UpgradeSystem soft-lock + level refactor tamamlandı:** `OnUpgradeSelected` artık `_blobRoot == null` olsa bile `ResumeGame()` çağırıyor (soft-lock fix). `UpgradeData.CurrentLevel` kaldırıldı — seviye artık `UpgradeSystem._levels` (`Dictionary<UpgradeData,int>`) içinde runtime'da tutuluyor, `UpgradeSystem.GetLevel(data)` ile okunuyor. `HUDController` bu API'yi kullanacak şekilde güncellendi.
- **B13 Hazard fix + yeme entegrasyonu tamamlandı:** `BlobConsumption`'daki ters hazard mantığı düzeltildi (artık `IsHazard` ise `HazardAmount` hasarı veriyor, hardcoded `MassValue*0.5` kaldırıldı). Enemy layer'a `A12`'nin `TryConsumeByBlob` API'si bağlandı — Tier eşiği tutan düşmanlar artık gerçekten temasla yutulabiliyor.
- **A14 Sürü steering prod hardening tamamlandı:** Separation + engel kaçınma sorguları artık `aiTick`'te (0.15s throttle) hesaplanıp cache'leniyor, her frame değil (önceden `GetSeparationVector` her frame çağrılıyordu — 150-200 düşmanda gereksiz maliyet). Rotasyon `Quaternion.Slerp` ile yumuşatıldı (separation kaynaklı titreşim azaltıldı). `SwarmSteering.GetSeparationVector`'a komşu sayısı üst sınırı eklendi (`_maxNeighborsPerQuery=12`, yoğun kümelenmede worst-case'i sınırlar). Basit engel kaçınma eklendi: throttle'lı `Physics.Raycast` (Environment layer, `_avoidanceLookahead=1.2f`) ile duvara sıkışma önlenir — haritada henüz Environment collider'ı olmayabilir, o durumda no-op (güvenli). `EnemySpawner`'ın tavanı NavMesh-pathfinding maliyetine göre konmuş eski `30`'dan GDD Karar 2 hedefine (`40*tier+10`, max `200`) yükseltildi; elit/boss için ayrı ve düşük bir eşzamanlılık tavanı (`_maxActiveElites=8`) eklendi çünkü onlar hâlâ NavMeshAgent kullanıyor. **FPS doğrulaması yapılmadı** (yukarıya bkz.).
- **A15 Miniboss sistemi tamamlandı (kod tarafı):** GDD Karar 8 + §7 — "aynı tasarım, artan stat" ifadesi mevcut `EnemyBase`/`EnemyData`/`AttackState` makinesiyle bire bir karşılandığı için **ayrı bir MinibossController/davranış sınıfı yazılmadı** — miniboss, `IsElite=true` işaretli 2 adet `EnemyData` ("stage") + yeni `BossData`/`BossSpawner` ikilisiyle tamamen veri-odaklı kuruldu. `IsElite` zaten NavMesh hareketi, çoklu-vuruş burst saldırı ("alan saldırısı" burada büyük `AttackRange` + burst olarak modellendi) ve ölümde garanti sandık+coin sağlıyordu. Eklenen tek yeni davranış: `EnemyData.PreventConsumption` (miniboss hiçbir tier'da yutulamaz — `EnemyBase.TryConsumeByBlob`'a guard eklendi) ve `EnemyBase.CurrentHealth`/`MaxHealth` public getter'ları (B18 boss health bar kontratı için — `GameEvents.OnBossHealthChanged` de bu amaçla eklendi, `max<=0` = aktif boss yok). `BossSpawner` (`ChestSpawner`/`CoinSpawner` singleton+pool pattern'i, `EnemySpawner`'ın spawn-pozisyonu mantığı) `GameEvents.OnSurvivalTimeUpdated`'ı bağımsız dinler, `WaveController.cs`'e dokunulmadı. **Veri/sahne kurulumu da tamamlandı (2026-08-12):** Unity Editor kapalıyken (Force Text serialization doğrulanarak) elle YAML üretildi — `EnemyData_Miniboss_Stage1/2.asset` (4./8. dk stat eğrisi, `_prefab` şimdilik placeholder olarak `Enemy_ElitePolice.prefab`'ı işaret ediyor), `BossData_Miniboss.asset` (SpawnTime 240/480, BonusCoin 15-25/25-40), ve `GameScene.unity`'ye `BossSpawner` GameObject'i (+ `_bossData` referansı) eklendi, `SceneRoots.m_Roots`'a kaydedildi. Bu sırada A11/A14'ün de aynı şekilde eksik kaldığı fark edildi ve `SwarmSteering` GameObject'i de aynı yöntemle sahneye eklendi (bkz. yukarıdaki not). **Unity'de açıldı, Hierarchy'de her iki GameObject de doğru göründü (2026-08-12).** İlk açılışta tek derleme hatası çıktı: `BossSpawner.cs`'de `using BlobSurvivor.Entities.Coins;` eksikti (`CoinSpawner.Instance` çağrısı için) — eklendi, düzeldi. Play mode'da miniboss'un fiilen 4./8. dk'da spawnladığı henüz test edilmedi (bkz. Bilinen Eksikler'deki doğrulama listesi).
- **A15/A16 EnemyBase.SetData HP bug'ı düzeltildi:** Serra, `Enemy_Miniboss.prefab`'ın `EnemyBase._data` alanının hâlâ `EnemyData_ElitePolis`'i işaret ettiğini fark etti (muhtemelen prefab elit polis'ten kopyalanırken güncellenmemiş kalıntı). Bu, kod tarafında gizli bir bug'ı açığa çıkardı: `BossSpawner.SpawnStage()`, `pool.Get()` → `SetActive(true)` → `OnEnable()` (prefab'da serialize edilmiş, muhtemelen yanlış `_data`'ya göre `_currentHealth` set eder) sırasını takiben `boss.SetData(data)` çağırıyordu, ama `SetData()` `_currentHealth`'i **yeniden set etmiyordu** — yani her boss (miniboss stage1/2, ve yazılacak final boss) spawn anında prefab'da o an ne yazıyorsa onun `MaxHealth`'iyle başlıyordu, `SetData`'nın verdiği doğru stage HP'siyle değil (gösterilen `MaxHealth` `_data`'yı canlı okuduğu için doğru görünüyordu, ama gerçek `CurrentHealth` yanlıştı). Düzeltme: `EnemyBase.SetData()` artık `_currentHealth = data.MaxHealth;` da yapıyor. Bundan sonra prefab'daki `_data` alanı fonksiyonel olarak önemsiz (spawn anında her zaman ezilir) ama Inspector netliği için yine de doğru referansa çekilmesi öneriliyor (bkz. Bilinen Eksikler).
- **A16 Final Boss (kod tarafı) tamamlandı:** GDD Karar 1+8, §4 — "vurarak son faza getirirsin, yutarak bitirirsin." A15'in aksine bu kez gerçek davranış kodu gerekti (`FinalBossController.cs`, yeni), çünkü final boss'un kendine özgü bir kısıtı var: **silahla asla ölmeyecek**, sadece yutularak. Bunun için `EnemyData.RequiresConsumptionToDie` eklendi — `EnemyBase.TakeDamage()`'da HP 0'ın altına inince bu flag'li düşmanlarda `Die()` çağrılmıyor, HP 1'de kilitleniyor. `FinalBossController.Update()` HP oranını izliyor, `_edibleHealthFraction` (varsayılan %25) altına inince `EnemyBase.SetConsumableOverride(true)` çağırıyor — bu, A15'te eklenen `PreventConsumption` asset-seviyeli (paylaşımlı ScriptableObject) kilidini **instance-seviyesinde** geçici olarak açan yeni bir alan (`EnemyBase._consumableOverride`); asset'in kendisi mutasyona uğratılmıyor çünkü aynı `EnemyData` başka pool instance'larında da paylaşılıyor olabilir. `TryConsumeByBlob` artık `PreventConsumption && !_consumableOverride` kontrolü yapıyor. Ölüm sadece bu yolla mümkün olduğu için `EnemyBase.OnDeath` ateşlendiğinde `FinalBossController` bunu doğrudan `GameEvents.RaiseFinalBossConsumed()`'a bağlıyor — A17 run-sonu yapısı bunu dinleyip run'ı "tamamlandı/zafer" olarak kapatacak (A16 kapsamı sadece sinyali sağlamak, run'ı kapatma mantığı A17'nin işi). **Spawn tarafı için ayrı bir spawner yazılmadı** — final boss, A15'in `BossSpawner`'ının zaten generic olan `BossData.Stages` dizisine 3. stage olarak eklendi (`BossData_Miniboss.asset`, SpawnTime=720/12.dk) çünkü "aynı anda tek boss" kısıtı ve zamanlı spawn mantığı zaten oradaydı; `BossSpawner.Instance` singleton olduğu için ikinci bir spawner instance'ı çakışma riski taşırdı. **Prefab bilinçli olarak boş bırakıldı** (bkz. Bilinen Eksikler) — final boss miniboss'la aynı prefab'ı paylaşamaz, çünkü `FinalBossController` sadece final boss'un prefab'ında olmalı (aksi halde miniboss da yanlışlıkla yenebilir faza girer).
- **A17 Run süresi/sonu yapısı (kod tarafı) tamamlandı:** GDD_v2.md Karar 1 — run artık sadece ölümle (GameOver) değil, iki "başarılı" yolla da kapanabiliyor: final boss yutulması (`GameEvents.OnFinalBossConsumed`, A16) ya da `_runTimeoutSeconds` (varsayılan 900s/15dk) güvenlik eşiğine ulaşılması. Yeni `GameState.RunComplete` + `RunEndReason` enum (`FinalBossConsumed`/`TimeSurvived`) `GameManager.cs`'te; `GameManager.TriggerRunComplete(reason)` normal `TriggerGameOver()`'dan bilinçli olarak ayrı tutuldu çünkü B18/Sprint 5'in ayrı bir "run tamamlandı" ekranı göstermesi planlanıyor — bu issue sadece state/event kontratını kuruyor, UI yok. `WaveController.cs`'e dokunulmadı (dev-a domaini, issue'nun kendi notu); "kıyamet yoğunluğu ayarlanabilir eşik" isteği `GameManager._runTimeoutSeconds` Inspector alanı olarak karşılandı.
- **B18 Boss health bar UI (kod tarafı) tamamlandı:** `GameEvents.OnBossHealthChanged` zaten A15'te vardı, bu issue sadece `HUDController` tarafını ekledi — `_bossHealthContainer`/`_bossHealthBar` alanları, `max<=0` iken container gizli/aktif boss varken görünür sözleşmesiyle. Editor'da Slider hiyerarşisi kurulması gerekiyor (bkz. Bilinen Eksikler).
- **B16 3 yeme-temalı skill (kod tarafı) tamamlandı:** GDD_v2.md §5, Karar 5. Sindirim (`BlobGrowth._massGainMultiplier`, `AddMass()`'ta hem mass hem XP'ye uygulanıyor çünkü mass=XP birleşik/Karar 7), Yutuş (yeni `HealOnConsumeComponent`, `BlobConsumption`'ın hem consumable hem enemy yeme dallarında `ApplyHealOnConsume()` çağrılıyor), Yırtıcı Çene (yeni `ConsumptionBonusComponent`, `BlobConsumption`'ın enemy yeme dalında efektif tier'a eklenip `TryConsumeByBlob`'a geçiriliyor — normal consumable yemeyi etkilemiyor). Üçü de `BlobConsumption.cs`'i (Entities/Blob, paylaşılan dosya) değiştirdi ama `EnemyBase.cs`'e dokunmadı — Yırtıcı Çene'nin tier gevşetmesi tamamen çağıran tarafta yapıldı, `TryConsumeByBlob`'un imzası değişmedi. `UpgradeCategory`'ye `Feeding` sona eklendi (mevcut asset'lerin int değerleri kaymasın diye) — şu an sadece metadata, UI kategoriye göre render etmiyor.
- **B17 Skill Evrim sistemi (kod tarafı) tamamlandı:** GDD_v2.md §5, önceki B7/#16'nın devamı. `SkillEvolutionData` (Data/) + `EvolutionSystem` (Systems/Upgrade/) yeni. `UpgradeSystem.cs`'e küçük bir genişletme noktası eklendi (`_dynamicUpgrades` listesi + `RegisterDynamicUpgrade()`) çünkü `_allUpgrades` sahnede sabit dizildiği için evrim çıktılarının havuza runtime'da eklenebileceği ayrı bir yer gerekiyordu. İki lansman evrimi: **Kara Delik** (Vakum+Sindirim max → yeni `BlackHoleComponent`, throttle'lı `OverlapSphereNonAlloc` ile küçük iç yarıçaptaki tier1/2 consumable'ları `BlobConsumption.ConsumeDirect()` — yeni public wrapper — ile temassız yutar) ve **Avcı Formu** (Yırtıcı Çene+Silah Gücü max → yeni `AvciFormuComponent`, `EnemyBase`'e eklenen statik `OnAnyEnemyDamaged` event'ini dinleyip vurulan sürü düşmanını `EnemyBase.MarkTemporarilyConsumable()` ile geçici tier-şartsız yutulabilir işaretler; `TryConsumeByBlob`'daki `tierBypassed` kontrolü sadece non-elite düşmanlarda geçerli, GDD'nin "sürü düşmanları" kısıtına uygun). Görsel işaret: `UpgradeData.IsEvolution` flag'i + `UpgradePanel`'de ★/altın renk — ayrı bir ekran yazılmadı (issue'nun notuna uygun).
- **B19 Analytics + save iskeleti (kod tarafı) tamamlandı:** GDD_v2.md §13. `Systems/Meta/SaveSystem.cs` (JSON, `Application.persistentDataPath/analytics.json`, `JsonUtility`, `MaxStoredRuns=100`) + `RunAnalytics.cs` (level-up seçimlerini ve run bitiş sebebini toplar, `OnGameOver`/`OnRunComplete`'te `SaveSystem.SaveRun()`'ı tetikler) yeni. Bilinçli olarak 3. parti servis (GameAnalytics/Unity Analytics) entegre edilmedi — issue'nun kapsamı sadece yerel iskele, servis entegrasyonu Sprint 7+ işi.
- **2026-08-20 — Cannon mermisi (Topik) düşmanın üzerinden uçup gitme bug'ı düzeltildi:** Serra'nın fark ettiği "mermiler polisin arkasından uçuyor gidiyor" şikayeti araştırıldı. Kök sebep: `CannonProjectile` her frame önce `base.Update()`'in `SphereCast`'iyle çarpışma kontrolü yapıyor, SONRA mermiyi sinüs eğrisiyle (`_arcHeight=1.2`) yukarı/aşağı taşıyordu — yani dikey hareket fizik sorgusundan tamamen habersizdi ve sorgu her zaman bir önceki frame'in yüksekliğinde kalıyordu. Yayın tepe noktasına yakın frame'lerde mermi, `Enemy_Police` collider'ının (height=2) tepesinin epey üzerinde bir Y'de "uçuyordu", `SphereCast` orada hiçbir şey bulamıyordu. Düzeltme: `Projectile.cs`'e `GetExtraMotion()` virtual hook'u (varsayılan `Vector3.zero`, davranışsız) ve `NextLifetimeFraction` property'si eklendi; `Update()` artık `Direction * hız` ile `GetExtraMotion()`'ı TEK bir `SphereCast` sorgusunda birleştiriyor (frameMotion), yani dikey hareket artık sorgunun bir parçası. `CannonProjectile` eski post-hoc Y patch'ini kaldırıp `GetExtraMotion()`'ı override ederek bu frame'in `currentY`→`nextY` farkını döndürüyor. `PistolProjectile`/`HomingProjectile` etkilenmedi (`GetExtraMotion()` override etmiyorlar, fast-path aynı kalıyor). **Ek olarak Editor'da önerilen (henüz uygulanmadı):** `Cannon.prefab`'ın `Arc Height`'i `1.2`'den ~`0.4`'e düşürülmeli — düşman boyuna göre hâlâ gereğinden yüksek, kod düzeltmesiyle birlikte bile yayın tepesi düşmanın üstünden geçebilir. **Play Mode'da gerçek test edilmedi, Serra ile bir sonraki oturuma bırakıldı** (A14 FPS/Profiler testiyle birlikte, bkz. Bilinen Eksikler).
- **2026-08-20 — M20 (Sprint 5) Meta kredi/kalıcı stat data katmanı tamamlandı:** Yeni `Systems/Meta/MetaProgression.cs` — bkz. yukarıdaki script tablosu. Bu oturumda Unity Editor kapalıydı (A15/A16 döneminde kurulan hâlâ geçerli konvansiyon): script `.meta`'sı elle üretildi (guid standart 32-hex format), sahne tarafında da yeni bir GameObject yerine mevcut `MetaSystems` GameObject'ine (SaveSystem/RunAnalytics'in olduğu) üçüncü component olarak elle eklendi — `SceneRoots.m_Roots` değişikliği gerekmedi çünkü GameObject zaten vardı, sadece component eklendi (BossSpawner/SwarmSteering/EvolutionSystem'de kullanılan "yeni GameObject ekleme" yönteminden daha düşük riskli bir alt küme). **Unity açılıp derleme hatasız reload olduğu doğrulanmadı — ilk açılışta kontrol edilmeli.** M21 (Market UI) ve M22 (run başı stat uygulaması) bu sınıfın `Credit`/`Get*Bonus`/`TryPurchase` API'sini tüketecek.
- **2026-08-21 — M24 (Sprint 5) Yaya (sivil NPC) sistemi tamamlandı — GDD_v2.md Karar 14:** Kullanıcı sorusu ("oyunda yayalar da olacaktı, GDD'de bir şey var mı") üzerine araştırıldı — GDD v1/v2'de hiç spec edilmemiş, sadece CLAUDE.md'nin performans kuralları bölümünde ("consumable, düşman, araba, yaya pool'dan gelmeli") tek satırlık bir referans varmış. Serra'yla tasarım netleştirildi: blob küçükken normal yürür, yutma eşiğine (blob tier ≥ `RequiredTier`) yakınsa kaçar, yutulabilir; **bilinçli olarak animasyonsuz** (Animator/SkinnedMeshRenderer maliyeti yok — `Enemy_Police` de zaten animasyonsuz olduğu kontrol edildi, o yüzden ek bir maliyet sınıfı açmıyor), sadece Cannon mermisinin yay hareketiyle aynı teknikle (sinüs eğrisi) basit bir yukarı-aşağı bob. Mimari: `PedestrianController`, `SwarmSteering`'i (o `EnemyBase`'e sıkı bağlı, `Register(EnemyBase)`) kullanmak yerine `ConsumableBase`'i miras alıyor — bu, `IConsumable`/layer-atama/pool-return altyapısını bedavaya getirdi VE **hiç yeni Collision Matrix girdisi gerektirmedi** (yayalar mevcut ConsumableTier1-5 layer'larını otomatik kullanıyor). Bunu yaparken gerçek bir entegrasyon bug'ı bulundu ve düzeltildi: `BlobConsumption.Consume()` pool-return'ü hep `ConsumableSpawner.Instance`'a hardcoded yönlendiriyordu — `PedestrianController` ayrı bir `PedestrianSpawner` havuzundan geldiği için bu, yenilen yayaların hiçbir pool'a dönmeden ekranda "hayalet" gibi aktif kalmasına yol açardı. Düzeltme: `ConsumableBase.ReturnToOwner()` (`protected virtual`, varsayılan `ConsumableSpawner`'a döner) eklendi, `PedestrianController` override edip kendi `PedestrianSpawner`'ına dönüyor, `BlobConsumption` artık `pooled.ReturnToOwner()` çağırıyor (spawner tipine bakmıyor). Ayrıca bir sıralama bug'ı da (kod yazılırken, commit edilmeden) yakalandı: pool'dan `Get()` çağrısı `OnEnable()`'ı `SetData()`'dan ÖNCE tetikliyor — `PickNewWanderTarget()` ilk aktivasyonda `PedData` (data cast'i) hâlâ null'ken çağrılırsa NRE atardı; düzeltme `OnEnable()`'dan çıkarılıp `Update()`'in ilk tick'ine (`PedData != null` garantili) taşındı. `PedestrianSpawner`, `EnemySpawner`'ın (dev-a dosyası) canlı sayacını sorgulamak yerine kendi sabit tavanıyla (`_maxActive=20`) çalışıyor — "aynı bütçeyi paylaşır" ilkesi cross-file coupling riski almadan, baştan küçük tutulan bir payla karşılandı. GDD_v2.md'ye Karar 14 olarak işlendi (§0 tablo + §7 altına yeni "Sivil NPC'ler" alt bölümü). **Unity kapalıyken yazıldı — derleme doğrulaması bekliyor** (M20 ile aynı durum).
- **2026-08-22 — M25 (Sprint 5) Blob boyu yetmeyen consumable'lara fiziksel çarpma tamamlandı:** Serra'nın gözlemi ("yayaların içinden geçebiliyorum") aslında doğru davranıştı (trigger collider, blob tier yetmediği için tepki yoktu) ama sonrasında "boyum yetmeyen consumable'lara çarpmam lazım, en performanslı nasıl yapılır" isteği geldi. Araştırırken `Blob` GameObject'inin zaten **iki** `SphereCollider`'ı olduğu bulundu (`GameScene.unity`'de doğrulandı): biri trigger (yeme algılama, `BlobConsumption.OnTriggerEnter`), biri solid (`IsTrigger=0`, muhtemelen zemin/duvar için) — yani fiziksel çarpma altyapısı zaten hazırdı, sadece consumable tarafının tier'a göre trigger↔solid geçiş yapması gerekiyordu. Çözüm bilinçli olarak **per-frame değil event-driven**: `GameEvents`'e `CurrentBlobTier` statik property'si eklendi (`RaiseBlobTierChanged` içinde güncellenir), `ConsumableBase`'e `Collider` cache (`Awake()`) + `UpdateTriggerState(BlobTier)` eklendi — bu metod sadece `GameEvents.OnBlobTierChanged` ateşlendiğinde (bir koşuda birkaç kez) VE `SetData()`/`OnEnable()`'da (spawn/pool-reuse anında, senkron `GameEvents.CurrentBlobTier` okuyarak, Find'a gerek kalmadan) çalışıyor — per-frame maliyeti sıfır. `RequiredTier <= blobTier` ise trigger (yenebilir/geçilebilir), değilse solid (Blob'un solid collider'ı fiziksel olarak engeller). **Hazard'lar bu davranışın dışında tutuldu** (`isHazard || RequiredTier <= blobTier`) çünkü `BlobConsumption`'daki "yeterince büyük değilsen hasar al" dalı (`else if IsHazard`) trigger'a bağlı — solid yapılsaydı hasar hiç tetiklenmeyip sessizce sadece fiziksel engele dönüşürdü. `PedestrianController` `ConsumableBase`'i miras aldığı için bu davranışı otomatik aldı, ayrı kod gerekmedi. **Editor tarafında hiçbir şey gerekmiyor** — Blob'un solid collider'ı zaten sahnede var, Collision Matrix zaten Blob×ConsumableTier1-5 açık (aksi halde eski trigger sistemi de çalışmazdı). Bu çalışma sırasında **ilgisiz ama gerçek bir eksik** fark edildi (henüz düzeltilmedi, bkz. Bilinen Eksikler): `BlobGrowth.CurrentMass`/`CurrentTier` restart'ta hiç sıfırlanmıyor — A10'un dokunduğu restart temizliği listesinde (BlobHealth/WeaponBase/ScoreSystem) `BlobGrowth` hiç yoktu.
- **2026-08-22 — M22 (Sprint 5) kalıcı statların run başında uygulanması tamamlandı:** GDD §8. `GameManager.ApplyMetaProgression(blob, character)` (yeni, `ClearPreviousRunState`'ten SONRA çağrılır) `MetaProgression.Instance`'ın 5 bonusunu topluyor ve karakterin kendi `MoveSpeed` pasifiyle **tek seferde birleştirip** `BlobController.SetSpeedMultiplier`'a veriyor — `switch(character.PassiveType)`'daki eski `MoveSpeed` case'i bilinçli olarak boşaltıldı çünkü orada ayrıca `SetSpeedMultiplier` çağrılsaydı meta bonusunu ezip kaybederdi. `BlobGrowth.ApplyMetaBonuses(massGainBonus, xpBonus)` de aynı şekilde overwrite (increment değil) — bunun yan etkisi olarak Sindirim skill'inin restart'ta hiç sıfırlanmayan birikimi de (daha önce bilinçli kapsam dışı bırakılmıştı) artık her run başında temizleniyor. Meta "XP Çarpanı" ile "Mass Kazanımı" bilinçli olarak ayrı iki alan (`_massGainMultiplier` vs yeni `_xpBonusMultiplier`) — ikisi de GDD'de ayrı Market kalemi, XP Çarpanı mass/boyutu etkilemeden sadece leveling hızını artırıyor. **Bu işi doğru yapabilmek için `BlobHealth.ResetHealth()` eklemek gerekti** (yukarıdaki restart-fix notuyla aynı gün) — Max Can meta bonusu `IncreaseMaxHealth()` ile ekleniyor (additive), restart'ta `_maxHealth` sıfırlanmasaydı her restart'ta üst üste binip katlanırdı. `ScoreSystem.SetCoinGainMultiplier()` de aynı mantıkla `ResetScore()`'da (zaten her `StartGame()`'de çağrılıyordu) 1f'e dönüyor, katlanma riski yok. **Editor tarafında hiçbir şey gerekmiyor** — hepsi mevcut component'lerin yeni metodları, yeni SerializeField/scene wiring yok. **Unity açıkken yazıldı — derleme doğrulaması bekliyor.**
- **2026-08-22 — M21 (Market UI script) + M23 (Lobby↔Market butonu, kod) tamamlandı — Editor kurulumu bekliyor:** B16/B17'nin aksine bu ikisi kalıcı Canvas/UI hiyerarşisi gerektiriyor, script tek başına yeterli değil (bkz. Bilinen Eksikler). `MarketPanel.cs` (yeni, `UI/`) `LobbyPanel`/`UpgradePanel`'deki index-aligned serialized array deseniyle 6 sabit Market kalemini yönetiyor. `LobbyPanel`'e Market'e giriş butonu eklendi. **Bilinçli tasarım kararı:** MarketPanel'in "geri" butonu C# tarafında `LobbyPanel`'e referans TUTMUYOR — döngüsel script bağımlılığından kaçınmak için `LobbyPanel.Show()` (yeni, public) + `MarketPanel.Hide()` ikisi de Inspector'da Button'ın OnClick listesine ayrı ayrı eklenecek iki persistent listener olarak tasarlandı (Unity'nin bu tip basit iki-panel-arası-geçiş için zaten idiomatic yolu). **Bilinçli olarak yapılmadı:** Mıknato'nun Market'te satın alınana kadar Lobby'de gizli/pasif kalması (GDD'nin "Market'te 500 kredi" kilidi) — şu an `LobbyPanel` üç karakteri de koşulsuz gösteriyor, satın alma Market ekranında bir şey değiştirse de Lobby'de görünürlüğü etkilemiyor. Bunu düzgün yapmak `CharacterData`'ya yeni bir alan eklemek ya da `LobbyPanel`'e hangi index'in Mıknato olduğunu bilecek bir referans eklemek gerektiriyordu — roadmap'in M23 tanımında yoktu, sessizce eklemek yerine açık bırakıldı; istenirse ayrı bir küçük iş olarak yapılabilir.
- **2026-08-31 — Sprint 5 sonrası açık issue triyajı: #46 (Collision Matrix) doğrulandı + kapatıldı, #48 (Mıknato Lobby kilidi) implement edildi, #45 (Cannon arc height) değeri düşürüldü:** `CharacterData.cs`'e yeni `_requiresMarketUnlock` bool alanı eklendi (`Char_Miknato.asset`'te `1`), `LobbyPanel.cs`'e `RefreshCharacterButtons()` eklendi (`Start()` ve `Show()`'da çağrılır — Market'ten dönüşte kilit durumu güncel kalsın diye) — kilitliyken buton `interactable=false` + isim yanında "(Kilitli)". `Cannon.prefab`'ın `_arcHeight`'i `1.2`'den `0.4`'e düşürüldü (#45'in Editor önerisiydi, artık uygulandı). #46 için `ProjectSettings/DynamicsManager.asset`'teki collision matrix'i Unity açmadan hex-decode ederek Blob(8)×Enemy(14) bitinin işaretli olduğu doğrulandı (bkz. yukarıdaki Collision Matrix notu). #47 (karakter ikonları) sanat asseti eksikliği nedeniyle koddan çözülemiyor, açık kaldı — tasarımcı arkadaşın işi, developer scope'unda değil. #44 (FPS/Profiler) gerçek Editor/cihaz oturumu gerektirdiği için açık kaldı. **2026-08-31 — issue #49 ("durduk yere öldüm") silindi:** repro bilgisi hiç netleşmedi, tekrar yaşanırsa o zaman konuşulacak; kalıcı bir TODO olarak izlenmeye değer bulunmadı. **Unity açık/kapalı bilinmeden yazıldı — LobbyPanel/CharacterData derleme + Play Mode doğrulaması (kilitli buton görünümü, Market'ten satın alınca kilidin açılması) Serra tarafından yapılmalı.**
- **2026-08-31 — GDD Karar 2 revize edildi: eşzamanlı düşman hedefi 150-200'den 15 polis + 25 yayaya düşürüldü (Serra kararı, #44 FPS testi sırasında):** Serra'nın gerekçesi: gerçek bir şehirde aynı anda 150-200 polis koşuşturması gerçekçi değil, düşük ama sürekli akan (ölünce yenisi gelen) bir popülasyon hem daha inandırıcı hem optimize etmesi daha kolay. `EnemySpawner.cs`'teki tier-bazlı ölçekleme (`40*(int)tier+10, max 200`, `GameEvents.OnBlobTierChanged` dinleyen `OnTierChanged`) tamamen kaldırıldı — artık `_maxActiveEnemies` sabit `15`; zamana bağlı yoğunluk artışı (10dk+ ×2) hâlâ `WaveController.SpawnDensityMultiplier` üzerinden `GetEffectiveMaxActive()`'da uygulanıyor, ayrı bir mekanizma gerekmedi. `_maxActiveElites` de `8`'den `4`'e indirildi (200'e göre orantılanmış eski değer 15'lik popülasyonda nüfusun yarısından fazlasının elit olabileceği anlamına geliyordu — perf tavanı olarak hâlâ geçerliydi ama oyun hissi için düşürüldü, Serra'dan onay istenmedi, küçük/geri alınabilir bir karar). `PedestrianSpawner`'ın sahnedeki `_maxActive`'i `12`'den `25`'e çıkarıldı (ayrı bir istek — yaya sayısının azaltılması M24'te fazla temkinli yapılmıştı). `GDD_v2.md`'nin Karar Günlüğü (§0 #2), §7 Mimari ve §15 Performans Hedefleri tabloları güncellendi (eski değer üstü çizili bırakıldı, projenin "revize" konvansiyonu — bkz. Dash/Kalkan kararı). Profiler testi için geçici olarak `Wave_Police`/`Wave_PoliceElite`'in `_spawnRate`'i `0.05`'e düşürülmüştü (200'e hızlı ulaşmak için) — hedef 15/25'e inince gerek kalmadı, prod değerlerine (3 / 2.5) geri alındı. **Unity açık/kapalı bilinmeden yazıldı — derleme + Play Mode doğrulaması (yeni build alınıp 15/25 tavanının gözlemlenmesi) Serra tarafından yapılacak.** **⚠️ 2026-08-31 ek not: 15/25 sayıları henüz kesin değil — Hüma ve Bahar ile konuşulacak, o görüşmeden sonra değişebilir.**
- **2026-08-31 — Spawn'da engel çakışması önlendi (SpawnPositionUtility) + geçici greybox şehir aracı eklendi:** Serra'nın gözlemi: gerçek şehir sanatı gelince (henüz yok, greybox — bkz. yukarıdaki not) consumable/polis/yaya bir binanın içine spawn olabilir ya da yaya bir binaya doğru yürüyebilir; "sıkışmamalı" testi baştan doğru kurulsun istedi. `EnemySpawner` zaten `NavMesh.SamplePosition` kullanıyordu ama bulamayınca ham (potansiyel off-mesh) adaya düşüyordu — üçü de (`EnemySpawner`, `ConsumableSpawner`, `PedestrianSpawner`) artık yeni `Systems/Spawning/SpawnPositionUtility.TryFindNavMeshPosition()`'ı kullanıyor: bulamazsa `false` döner, o spawn denemesi sessizce atlanır (bir dahaki tick'te yeniden denenir) — üç yerde aynı mantığı kopyalamak yerine ortak bir static utility'ye çıkarıldı. `PedestrianController.PickNewWanderTarget()` da aynı utility'yle güncellendi — ama bu sadece hedef seçimini düzeltiyor, yayanın hedefe giderken yol üstündeki bir binaya çarpmasını engelleyen sürekli bir kaçınma değil (`SwarmSteering`'in düşmanlar için yaptığı raycast tabanlı kaçınmadan farklı, kapsam dışı bırakıldı — yayalar arka plan dekoru, öncelik değil). Ayrıca `Assets/_Project/Editor/GreyboxCityGenerator.cs` eklendi (bkz. yukarıdaki Editor Araçları tablosu) — gerçek sanat gelene kadar NavMesh/steering'i kaba küp binalarla test etmek için. **Unity açık/kapalı bilinmeden yazıldı — derleme + Editor menü aracının fiilen çalışması (Tools menüsünde görünmesi, bina üretmesi, NavMesh bake sonrası spawnların binaların içine düşmemesi) Serra tarafından doğrulanacak.**
- **B14 Consumable pool return fix tamamlandı:** `ConsumableSpawner.ReturnToPool()` eklendi (`ConsumeAndSplit`'teki pattern genelleştirildi), `BlobConsumption.Consume()` artık `SetActive(false)` yerine bunu çağırıyor — normal yeme akışında da pool sızıntısı vardı (A9'un consumable karşılığı).
- **B15 Dash (Hızlanma) yeniden implementasyonu tamamlandı (sonradan kaldırıldı — bkz. aşağıdaki 2026-08-11 kararı):** GDD'de "Hızlanma (**Bot**)" olarak geçiyor — oyuncu tetiklemez, otomatik/periyodik hız patlaması (tek input kuralına uyar). `DashComponent.cs` (Entities/Blob) + `DashEffect.cs` (Systems/Upgrade/Effects) + `Upgrade_Dash.asset` eklendi. Script `.meta` dosyaları Unity açık olmadığı için elle üretildi (guid'ler standart format, Unity sonraki reimport'ta olduğu gibi kabul eder).
- **2026-08-11 — Dash ve Kalkan skill'leri kaldırıldı (Serra kararı):** Dash, kalıcı Hız skill'iyle; Kalkan, Zırh (`DamageReductionEffect`) ile fonksiyon olarak fazla örtüşüyordu. Silinenler: `DashComponent.cs`, `DashEffect.cs`, `Upgrade_Dash.asset`, `ShieldEffect.cs`, `Upgrade_Shield.asset`, `FX_Shield.asset`. `BlobHealth`'ten `CurrentShield`/`MaxShield`/`AddMaxShield` ve shield-absorbs-first hasar mantığı çıkarıldı; `GameEvents.OnShieldChanged`/`RaiseShieldChanged` kaldırıldı; `HUDController`'dan shield bar (ve artık kullanılmayan `CreateSliderFill` helper'ı) kaldırıldı. Sahnede `UpgradeSystem._allUpgrades` ve `HUDController._allUpgrades` dizilerinden Kalkan referansı çıkarıldı (Dash zaten hiç eklenmemişti). GDD_v2.md §16/§5'teki ilgili maddeler bir sonraki GDD güncellemesinde ✅/kaldırıldı olarak işaretlenmeli.
- **A5 Düşman ölçekleme tamamlandı:** `WaveController` süreye göre hasar, spawn yoğunluğu ve hız çarpanlarını tutuyor; `EnemySpawner/EnemyBase` spawn sırasında bu çarpanlarla çalışmalı.
- **A6 Elit düşman tamamlandı:** `EnemyData` içinde `IsElite`, attack hit count/interval alanları var; `EnemyData_ElitePolis.asset`, `Enemy_ElitePolice.prefab`, `Mat_Enemy_Elite.mat`, `Wave_PoliceElite.asset` yerelde mevcut.
- **A7 Coin drop tamamlandı:** `EnemyBase.Die()` normal düşmanda 1 coin, elit düşmanda 5-10 coin spawn ediyor; `CoinSpawner`/`CoinPickup` pool ve pickup akışını yönetiyor; `ScoreSystem.AddCoin` ve `GameEvents.OnCoinsChanged` eklendi.
- **B8 Elit sandık drop tamamlandı:** Elit düşman ölünce `ChestSpawner` sandık spawn ediyor; `ChestPickup` blob temasında coin ekliyor ve level-up/skill seçim akışını tetikliyor.
- **B9 Coin HUD + GameOver özet tamamlandı:** `HUDController` coin sayacını canlı güncelliyor; `GameOverScreen` coin, süre, önceki rekor ve yeni rekor banner'ı gösteriyor.
- **ObjectPool MonoBehaviour sorunu:** Unity filename=classname zorunluluğu. ObjectPool generic class, PoolManager ayrı MonoBehaviour dosyası.
- **GetInstanceID deprecated:** Dictionary key olarak prefab referansı kullanılıyor (`Dictionary<Object, object>`).
- **Input System çakışması:** Proje New Input System kullanıyor. `UnityEngine.Input` class'ı kullanılamaz. InputManager `InputAction` ile yazıldı.
- **ConsumableBase layer hatası:** `8 + ((int)tier - 1)` Tiny'yi layer 8'e (Blob layer!) koyuyordu. Düzeltme: `8 + (int)_data.RequiredTier`.
- **WASD çalışmıyordu (eski çözüm, artık geçersiz):** GameManager `Start()`'ta Menu state'te kalıyordu ve BlobController hareket ettirmiyordu. O zamanki düzeltme `Start()`'ın direkt `StartGame()` çağırmasıydı — bu artık **Lobby akışı** ile değişti (bkz. A4, issue #5): `Start()` artık `StartGame()` çağırmıyor, oyun `Menu` state'inde kalıyor; `LobbyPanel` bir karakter seçilene kadar açık kalır, seçim `GameManager.StartGame(CharacterData)`'ı tetikler ve state `Playing`'e geçer.
- **Kamera uzaklaşıyordu:** Tier değişince zoom vardı. Kullanıcı istemedi, kaldırıldı.
- **Smooth büyüme:** Başta tier atladıkça scale sıçrıyordu. `Pow` formülüyle smooth hale getirildi.
- **NavMesh spawn hatası:** Enemyler Y=0'da spawn oluyordu, NavMesh'e uzak kalıyordu. `EnemySpawnY = 0.65f` + `NavMesh.SamplePosition` ile düzeltildi.
- **Mıknatıs işe yaramıyordu:** `MagnetComponent` `Rigidbody.AddForce` kullanıyordu ama consumable prefab'larında Rigidbody yok (sadece trigger Collider). Düzeltme: `Transform.MoveTowards` ile direkt pozisyon taşıma.
- **Polis oyunun başında spawn oluyordu:** `Wave_Police.asset`'te `TimeThreshold = 0` idi. Editor'dan 60'a çekildi (oyunun 60. saniyesinde başlıyor).
- **2026-08-11 — Elit polis 1. dakikada normal polisin yerine geçiyordu (bug fix):** `Wave_Police` ve `Wave_PoliceElite`'in ikisinin de `TimeThreshold`'u 60'tı. `WaveController.CheckWaveProgression()` dizide en yüksek index'ten aşağı tarayıp eşiği geçen ilk dalgayı `CurrentWave` yapıp `break` ediyor (`WaveController.cs`) — sahnedeki `_waves` sırası `[Wave_Police, Wave_PoliceElite]` olduğu için 60. saniyede direkt `Wave_PoliceElite` aktif oluyor, `Wave_Police` hiç aktive olmuyordu (yalnızca `CurrentWave`'den spawn eden `EnemySpawner` yüzünden 1. dakikadan itibaren sadece elit polis spawn oluyordu, normal polis hiç çıkmıyordu). `Wave_PoliceElite.asset`'in `TimeThreshold`'u GDD'deki 5 dakikaya (`300`) çekildi.

---

## Yapılacaklar (Sıradaki Fazlar)

Fazlar sırayla yapılacak. Her faz tamamlanınca burası güncellenmeli.

---

### ✅ Phase 1 — Core Altyapı (TAMAMLANDI)
- GameManager, GameEvents, CameraController
- Object Pool sistemi (ObjectPool + PoolManager)
- ScoreSystem

### ✅ Phase 2 — Blob (TAMAMLANDI)
- BlobController (hareket)
- BlobGrowth (smooth büyüme, tier sistemi)
- BlobConsumption (yeme, trigger)
- BlobHealth (hasar, ölüm, regen)

### ✅ Phase 3 — Consumables (TAMAMLANDI)
- IConsumable interface
- ConsumableBase
- ConsumableSpawner (pool'dan spawn, tier bazlı)
- ConsumableData ScriptableObject

### ✅ Phase 4 — Düşman Sistemi (TAMAMLANDI)
- EnemyBase + NavMeshAgent
- State machine: PatrolState → ChaseState → AttackState
- EnemyData ScriptableObject
- WaveData ScriptableObject + WaveController
- EnemySpawner (NavMesh'te geçerli noktada spawn)

---

### ✅ Phase 5 — Upgrade Sistemi (TAMAMLANDI)
- UpgradeEffect (abstract SO) + UpgradeSystem (weight bazlı 3 seçenek, OnLevelUp/OnUpgradeSelected)
- 6 concrete efekt: Speed, DamageReduction, Regen, HealthBoost, ScoreMultiplier, Magnet
- MagnetComponent (yeni Blob bileşeni, consumable'ları çeker)
- BlobGrowth'a XP/Level sistemi eklendi (mass kazanmak = XP)

### ✅ Phase 6 — HUD & UI (TAMAMLANDI)
- HUDController (health/XP bar, skor, timer, tier, level)
- UpgradePanel (3 buton, seçim → GameEvents.RaiseUpgradeSelected)
- GameOverScreen (skor/highscore, restart)
- SafeAreaHandler (notch desteği)
- Canvas yapısı: `Canvas → SafeArea → HUD / UpgradePanel / GameOverScreen`

---

### 🔲 Phase 7 — Harita & Bölgeler (SIRADA)

**Amaç:** Oyun dünyasını bölgelere ayır, her bölgenin kendine özgü consumable ve düşman seti olsun.

**Scriptler:**
- `Assets/_Project/Scripts/Systems/Map/MapRegion.cs`
  - Bölge tanımı: isim, sınırlar, consumable listesi, arka plan rengi
- `Assets/_Project/Scripts/Systems/Map/MapManager.cs`
  - Blob hangi bölgedeyse ona göre ConsumableSpawner'a filtre uygular
- Sınır sistemi: blob haritadan çıkamasın (Rigidbody constraint veya invisible wall)

**Planlanan bölgeler:**
- Şehir Merkezi (başlangıç bölgesi)
- Park
- Liman
- Endüstri Bölgesi

---

### 🔲 Phase 8 — Karakter & Silah Sistemi

**Amaç:** GDD'deki 3 başlangıç karakterini (Topik / Mıknato / Mermo) implement et. Her karakterin kendine özgü silahı ve pasifi olsun.

- `CharacterData` ScriptableObject: pasif tanımı, başlangıç silahı, sprite/model
- `WeaponBase` abstract + concrete silahlar (Top, MetalBilye, Pistol)
- Otomatik saldırı (bullet-heaven mantığı — oyuncu ateşlemez, silah kendi cooldown'ında ateşler)
- Karakter seçim ekranı (lobi)

### 🔲 Phase 9 — Skill Sistemi Genişletme

Sprint 1 B1-B5 kapsamının çoğu tamamlandı: UpgradeData level alanları, level-aware UpgradeSystem, Vakum, HUD skill rozetleri ve karakter ikonu mevcut. GDD'ye göre hâlâ evrim ve bazı ek skill davranışları gerekli. **Not:** Dash ve Kalkan skill'leri 2026-08-11'de kaldırıldı (Hız/Zırh ile örtüştükleri için — bkz. Önemli Kararlar).

- `UpgradeData` level sistemi mevcut: `MaxLevel`, `PerLevelValue`, runtime `CurrentLevel`
- Vakum skill'i mevcut
- HUD aktif skill rozeti ve karakter ikonu mevcut
- ✅ **B16 3 yeme-temalı skill tamamlandı (kod 2026-08-18, Editor 2026-08-20):** Sindirim, Yutuş, Yırtıcı Çene — bkz. Önemli Kararlar. GitHub #32 kapatıldı.
- ✅ **B17 Skill Evrim sistemi tamamlandı (kod 2026-08-18, Editor 2026-08-20):** `SkillEvolutionData`, `EvolutionSystem`, Kara Delik + Avcı Formu — bkz. Önemli Kararlar. GitHub #33 kapatıldı.
- **Yeniden Çek** butonu (oturum başına 1 ücretsiz, sonrası 50 altın)
- Skill kartlarında renk + sembol (renk körü desteği)

### 🟡 Phase 10 — Boss Dalgaları (GDD v2 Karar 1/8'e göre güncellendi: 25dk/5-10-15-20dk değil, 12-15dk run + 4./8.dk miniboss + 12.dk final boss)

- ✅ **A15 Miniboss (4./8. dk) tamamlandı** (kod + veri asset'leri + sahne kurulumu — Unity'de açılıp görsel doğrulama bekliyor, bkz. Bilinen Eksikler): `BossData` ScriptableObject, `BossSpawner`, `EnemyData.PreventConsumption`, `GameEvents.OnBossHealthChanged`. Ayrı bir boss davranış sınıfı yok — mevcut `EnemyBase`/`EnemyData` (`IsElite`) veri-odaklı olarak yeniden kullanıldı.
- ✅ **A16 Final Boss (12. dk) tamamlandı — kod + Editor kurulumu + Play mode testi geçti (2026-08-16):** `FinalBossController` (yeni), `EnemyData.RequiresConsumptionToDie`, `EnemyBase.SetConsumableOverride`, `GameEvents.OnFinalBossConsumed`. Spawn için A15'in `BossSpawner`/`BossData` altyapısı yeniden kullanıldı (3. stage, 12. dk). Bu sırada `EnemyBase.SetData()`'da miniboss'u da etkileyen bir HP başlatma bug'ı bulunup düzeltildi (bkz. Önemli Kararlar).
- ✅ **A17 Run süresi/sonu yapısı tamamlandı (2026-08-18):** `GameState.RunComplete`, `RunEndReason`, `GameManager._runTimeoutSeconds` — bkz. Önemli Kararlar. GitHub #31 kapatıldı.
- ✅ **B18 Boss health bar UI tamamlandı (kod 2026-08-18, Editor 2026-08-20):** `HUDController._bossHealthContainer`/`_bossHealthBar` — bkz. Önemli Kararlar. GitHub #34 kapatıldı.

### 🔲 Phase 11 — Meta Progression (Market + Grimoire)

- `MetaProgressionData` (PlayerPrefs veya JSON): kalıcı kredi, açılmış karakter/harita/silah, kalıcı pasif kademeleri
- **Market ekranı:** Karakter/harita/silah/XP çarpanı satın alma
- **Grimoire:** İlk karşılaşılan düşman/silah/harita loglanır; %100 doluluk → kozmetik ödül
- Coin drop sistemi oturum içi olarak mevcut: düşman ölünce coin spawn, blob toplayınca `ScoreSystem.Coins` artar
- Elit sandık drop mevcut: elit ölünce chest spawn, pickup coin + level-up seçim akışını tetikler
- Kalıcı/meta coin aktarımı hâlâ Sprint 3+ işi
- **NG+ zorluk seviyeleri:** Standart, Kızıl Ay, Kan Krizi, Apokalips
- ✅ **B19 Analytics + save iskeleti tamamlandı (kod 2026-08-18, Editor 2026-08-20):** `SaveSystem`/`RunAnalytics` (Systems/Meta/) — yerel JSON, 3. parti servis değil (Sprint 7+ işi). Sahnede `MetaSystems` GameObject'i üzerinde. Bkz. Önemli Kararlar. GitHub #35 kapatıldı.

### 🔲 Phase 12 — Hava Durumu Sistemi

- `WeatherData` ScriptableObject (efekt tipi, süre, görsel filtre)
- Runtime modifier: XP çarpanı, düşman hızı, görüş mesafesi, aura hasarı
- Rastgele/timeline bazlı tetiklenme

### 🔲 Phase 13 — Harita & Bölgeler

**Amaç:** Modern Şehir + Medieval haritaları. Sonsuz kaydırmalı world (Vampire Survivors modeli).

- `MapRegion.cs`: sınır, consumable listesi, düşman havuzu, palet
- `MapManager.cs`: aktif harita filtresi, spawn kuralları
- Toplanabilirler: Coin, Kalp (can), Altın Kasa (bonus güçlenme)
- Easter egg/rozet konumları (mağara, ahır vb.)

### 🔲 Phase 14 — Cila & Juice

- Parçacık efektleri (yeme, ölüm, tier atlama, kan sıçraması, ruh ışıkları)
- `AudioManager` — synthwave + gotik orkestra; 20+ dk tempo artışı
- `CameraShake`
- Skor combo sistemi
- Blob gözleri / yüz animasyonu
- Renk paletini uygula (`#8B0000`, `#0D0D2B`, vb.)

### 🔲 Phase 15 — Monetizasyon & Kozmetik (Mobil)

- Kozmetik mağaza: karakter renk paleti, silah kaplaması, UI teması
- İsteğe bağlı reklam: reklam izle → Yeniden Çek hakkı veya bonus XP
- IAP entegrasyonu (Unity IAP)
- **Kural:** Hiçbir "pay-to-win" mekanik yok — sadece kozmetik.
