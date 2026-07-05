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
BlobSurvivor.Entities    → Blob/*, Consumables/*, Enemies/*
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
```

Raise metodları: `GameEvents.RaiseBlobSizeChanged(mass)` vs.

---

## Yazılmış Scriptler

### Core
| Script | Açıklama |
|--------|----------|
| `GameManager.cs` | Singleton; GameState (Menu/Playing/Paused/LevelUp/GameOver); `Start()` artık `StartGame()` çağırmaz — Lobby, karakter seçilene kadar açık kalır; `StartGame(CharacterData)` overload'ı pasifi uygular + başlangıç silahını spawnlar |
| `GameEvents.cs` | Static event bus + BlobTier enum |
| `CameraController.cs` | Blob'u smooth takip eder, sabit yükseklik (tier değişince zoom YOK — kullanıcı istemedi) |

### Data (ScriptableObject)
| Script | Alanlar |
|--------|---------|
| `ConsumableData.cs` | displayName, prefab, scoreValue, massValue, objectSize, requiredTier, isHazard, hazardAmount |
| `EnemyData.cs` | displayName, prefab, maxHealth, damage, moveSpeed, attackRange, attackCooldown, spawnTier, scoreValue |
| `UpgradeData.cs` | id, displayName, description, icon, category, weight, effectValue, effectDuration, cooldown |
| `WaveData.cs` | timeThreshold, enemyTypes (EnemySpawnEntry[]), spawnRate, maxActiveCount, waveName |
| `CharacterData.cs` | displayName, icon, description, startingWeaponPrefab, passiveType (MoveSpeed/MagnetPull/ConsumableSplit), passiveValue |

### Entities / Blob
| Script | Açıklama |
|--------|----------|
| `BlobController.cs` | Rigidbody hareketi; `linearDamping`/`angularDamping`; tier'a göre hız: `1f / Sqrt((float)tier)` |
| `BlobGrowth.cs` | Smooth scale formülü; tier hesabı; `PunchScale()` yeme feedback'i |
| `BlobConsumption.cs` | OnTriggerEnter → IConsumable check → tier karşılaştır → Consume(); mobil haptic |
| `BlobHealth.cs` | TakeDamage(amount, DamageType); armor; regen; OnDeath → GameOver |

### Entities / Consumables
| Script | Açıklama |
|--------|----------|
| `IConsumable.cs` | Interface: Data, RequiredTier, OnConsumed() |
| `ConsumableBase.cs` | IConsumable impl; OnEnable'da layer set |
| `ConsumableSpawner.cs` | Pool'dan spawn; başlangıçta 40, max 80; 2s'de bir refill; tier değişince bonus; Singleton (`Instance`); `ConsumeAndSplit()` — Pistol'ün büyük consumable'ı parçalaması için |

### Entities / Enemies
| Script | Açıklama |
|--------|----------|
| `EnemyBase.cs` | NavMeshAgent; state machine; PerformAttack() → BlobHealth.TakeDamage(); AI throttle (`AIUpdateInterval=0.15s`, randomize stagger) — `CanSeeBlob()` sqrMagnitude ile sadece throttle tick'te hesaplanıp cache'lenir, state'lere `aiTick` bool geçilir |
| `EnemySpawner.cs` | NavMesh.SamplePosition ile Y=0.65f'te spawn; weighted random enemy seçimi |
| `IEnemyState.cs` | Enter, Update, Exit |
| `PatrolState.cs` | 3s'de bir random waypoint; blob görünce ChaseState |
| `ChaseState.cs` | Blob'a koş; `SetDestination()` (NavMesh pathfinding, pahalı) sadece `aiTick=true` iken çağrılır — her frame değil; attack range'e girince AttackState (sqrMagnitude); göremezse PatrolState |
| `AttackState.cs` | Dur, cooldown'da PerformAttack(); uzaklaşınca ChaseState (sqrMagnitude) |
| `WaveController.cs` | OnSurvivalTimeUpdated dinler; en yüksek geçilen threshold'u aktif dalga yapar |

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
| `ScoreSystem.cs` | AddScore, multiplier, PlayerPrefs highscore, ResetScore |

### Systems / Upgrade
| Script | Açıklama |
|--------|----------|
| `UpgradeEffect.cs` | Abstract ScriptableObject; `Apply(blobRoot, data)` |
| `UpgradeSystem.cs` | OnLevelUp dinler; weight bazlı 3 seçenek sunar; OnUpgradeSelected dinler, efekti uygular, oyunu resume eder |
| `SpeedBoostEffect.cs` | BlobController.SetSpeedMultiplier artırır |
| `DamageReductionEffect.cs` | BlobHealth armor multiplier düşürür (daha az hasar) |
| `RegenBoostEffect.cs` | BlobHealth regen rate artırır |
| `HealthBoostEffect.cs` | BlobHealth max health artırır |
| `ScoreMultiplierEffect.cs` | ScoreSystem multiplier artırır |
| `MagnetEffect.cs` | Blob'a MagnetComponent ekler/radius artırır |
| `WeaponUpgradeEffect.cs` | `blobRoot.GetComponentInChildren<WeaponBase>()` ile aktif silahı bulur, `IncreaseDamage(PerLevelValue)` çağırır — GDD'deki "Saldırı: Silah" kategorisi için (A1-A4/B1-B5 backlog'unda yoktu, sonradan eklendi). **İleride düşünülecek:** mermi hızı (`Projectile._speed`) ya da yön/mermi sayısı (multi-shot) artırma gibi ek boyutlar eklenebilir — şu an `UpgradeData`'da tek bir `PerLevelValue` alanı olduğu için sadece damage'a bağlandı; ikinci bir stat eklenecekse `UpgradeData`'ya yeni bir alan (örn. `_secondaryPerLevelValue`) gerekir |
| `MagnetComponent.cs` (Entities/Blob) | OverlapSphere ile yakındaki IConsumable'ları bulur, transform'u blob'a doğru taşır (consumable'larda Rigidbody YOK, force çalışmaz — `Vector3.MoveTowards` kullanılır) |

### UI
| Script | Açıklama |
|--------|----------|
| `HUDController.cs` | Health/XP bar, skor, timer, tier, level text — GameEvents dinler |
| `UpgradePanel.cs` | OnUpgradeChoicesReady'de 3 buton gösterir; tıklayınca OnUpgradeSelected raise eder |
| `GameOverScreen.cs` | OnGameOver'da skor/highscore gösterir; restart → GameManager.StartGame() |
| `LobbyPanel.cs` | Oyun başında görünür, HUD'u gizler (`_hud.SetActive(false)`); 3 karakter butonu (icon + isim); tıklayınca paneli kapatır, HUD'u açar, `GameManager.Instance.StartGame(data)` çağırır |
| `SafeAreaHandler.cs` | RectTransform'u Screen.safeArea'ya göre ayarlar (notch desteği) |

### Input
| Script | Açıklama |
|--------|----------|
| `InputManager.cs` | New InputSystem; InputAction Dpad composite binding; WASD + arrow keys; VirtualJoystick fallback |
| `VirtualJoystick.cs` | Dynamic floating joystick; ekranın sol yarısında tetiklenir; CanvasGroup show/hide |

---

## Bilinen Eksikler / TODO

- **Karakter iconları eksik:** `Char_Topik`, `Char_Miknato`, `Char_Mermo` (`Assets/_Project/Data/Characters/`) asset'lerinde `Icon` alanı boş — henüz görsel hazır değil. LobbyPanel'deki buton icon'ları bu yüzden şu an boş görünüyor. Icon'lar hazır olunca 3 asset'e de atanmalı.

---

## Önemli Kararlar / Geçmiş Düzeltmeler

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

Mevcut Upgrade sistemi 1 seviye + tek efekt. GDD'ye göre 1–8 seviye + evrim gerekli.

- `UpgradeData`'ya `maxLevel`, `currentLevel`, `levelValues[]` ekle
- Vakum, Kalkan, Score Multiplier, Hızlanma efektlerini ekle (mevcut olmayanlar)
- **Yeniden Çek** butonu (oturum başına 1 ücretsiz, sonrası 50 altın)
- Skill kartlarında renk + sembol (renk körü desteği)

### 🔲 Phase 10 — Boss Dalgaları

- `BossData` ScriptableObject (faz sayısı, faz başına saldırı deseni)
- 5–10–15–20. dakika minyatür bosslar (SWAT arabası, drone, helikopter)
- 25. dakikada Kıyamet Bossu (faz geçişleri, combo saldırılar)
- Boss health bar UI

### 🔲 Phase 11 — Meta Progression (Market + Grimoire)

- `MetaProgressionData` (PlayerPrefs veya JSON): kalıcı kredi, açılmış karakter/harita/silah, kalıcı pasif kademeleri
- **Market ekranı:** Karakter/harita/silah/XP çarpanı satın alma
- **Grimoire:** İlk karşılaşılan düşman/silah/harita loglanır; %100 doluluk → kozmetik ödül
- Coin drop sistemi (düşman öldüğünde) + elit → sandık drop
- **NG+ zorluk seviyeleri:** Standart, Kızıl Ay, Kan Krizi, Apokalips

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
