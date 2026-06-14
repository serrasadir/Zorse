# Blob Survivor — CLAUDE.md

Bu dosya, Claude Code'un projeyi sıfırdan anlayabilmesi için yazılmıştır. Her yeni sohbette otomatik okunur.

## Proje Özeti

**Blob Survivor** — top-down 2.5D Unity survival oyunu. Oyuncu bir blob'u kontrol eder, etraftaki objeleri yiyerek büyür, düşmanlardan kaçar/savaşır. Agar.io + Vampire Survivors karışımı.

- GDD: `/Users/serrasadir/Desktop/unity_projects/CLAUDE_CODE_PROMPT.md`
- Unity 6, URP (Universal Render Pipeline)
- New Input System (`UnityEngine.InputSystem`)
- AI Navigation paketi (NavMeshAgent)

## Workflow

- Assistant tüm C# scriptleri yazar
- Kullanıcı sadece Unity Editor'da yapılması gereken adımları uygular (Inspector ayarları, prefab bağlama, NavMesh bake, vb.)
- Kullanıcıya her zaman net Editor adımları verilmeli

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
BlobSurvivor.Systems     → Pool/*, Score/*, Wave/*
BlobSurvivor.UI          → (henüz yazılmadı)
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
GameEvents.OnBlobSizeChanged      // float mass
GameEvents.OnBlobTierChanged      // BlobTier tier
GameEvents.OnScoreChanged         // int score
GameEvents.OnHealthChanged        // float current, float max
GameEvents.OnGameOver
GameEvents.OnGameStarted
GameEvents.OnSurvivalTimeUpdated  // float seconds — WaveController dinler
GameEvents.OnLevelUp
```

Raise metodları: `GameEvents.RaiseBlobSizeChanged(mass)` vs.

---

## Yazılmış Scriptler

### Core
| Script | Açıklama |
|--------|----------|
| `GameManager.cs` | Singleton; GameState (Menu/Playing/Paused/LevelUp/GameOver); `Start()`'ta direkt `StartGame()` çağırır (test için menu atlanır) |
| `GameEvents.cs` | Static event bus + BlobTier enum |
| `CameraController.cs` | Blob'u smooth takip eder, sabit yükseklik (tier değişince zoom YOK — kullanıcı istemedi) |

### Data (ScriptableObject)
| Script | Alanlar |
|--------|---------|
| `ConsumableData.cs` | displayName, prefab, scoreValue, massValue, objectSize, requiredTier, isHazard, hazardAmount |
| `EnemyData.cs` | displayName, prefab, maxHealth, damage, moveSpeed, attackRange, attackCooldown, spawnTier, scoreValue |
| `UpgradeData.cs` | id, displayName, description, icon, category, weight, effectValue, effectDuration, cooldown |
| `WaveData.cs` | timeThreshold, enemyTypes (EnemySpawnEntry[]), spawnRate, maxActiveCount, waveName |

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
| `ConsumableSpawner.cs` | Pool'dan spawn; başlangıçta 40, max 80; 2s'de bir refill; tier değişince bonus |

### Entities / Enemies
| Script | Açıklama |
|--------|----------|
| `EnemyBase.cs` | NavMeshAgent; state machine; PerformAttack() → BlobHealth.TakeDamage() |
| `EnemySpawner.cs` | NavMesh.SamplePosition ile Y=0.65f'te spawn; weighted random enemy seçimi |
| `IEnemyState.cs` | Enter, Update, Exit |
| `PatrolState.cs` | 3s'de bir random waypoint; blob görünce ChaseState |
| `ChaseState.cs` | Blob'a koş; attack range'e girince AttackState; göremezse PatrolState |
| `AttackState.cs` | Dur, cooldown'da PerformAttack(); uzaklaşınca ChaseState |
| `WaveController.cs` | OnSurvivalTimeUpdated dinler; en yüksek geçilen threshold'u aktif dalga yapar |

### Systems
| Script | Açıklama |
|--------|----------|
| `ObjectPool.cs` | Generic pool, Get/Return/CreateInstance |
| `PoolManager.cs` | Singleton; tüm pool'ları yönetir |
| `ScoreSystem.cs` | AddScore, multiplier, PlayerPrefs highscore, ResetScore |

### Input
| Script | Açıklama |
|--------|----------|
| `InputManager.cs` | New InputSystem; InputAction Dpad composite binding; WASD + arrow keys; VirtualJoystick fallback |
| `VirtualJoystick.cs` | Dynamic floating joystick; ekranın sol yarısında tetiklenir; CanvasGroup show/hide |

---

## Önemli Kararlar / Geçmiş Düzeltmeler

- **ObjectPool MonoBehaviour sorunu:** Unity filename=classname zorunluluğu. ObjectPool generic class, PoolManager ayrı MonoBehaviour dosyası.
- **GetInstanceID deprecated:** Dictionary key olarak prefab referansı kullanılıyor (`Dictionary<Object, object>`).
- **Input System çakışması:** Proje New Input System kullanıyor. `UnityEngine.Input` class'ı kullanılamaz. InputManager `InputAction` ile yazıldı.
- **ConsumableBase layer hatası:** `8 + ((int)tier - 1)` Tiny'yi layer 8'e (Blob layer!) koyuyordu. Düzeltme: `8 + (int)_data.RequiredTier`.
- **WASD çalışmıyordu:** GameManager `Start()`'ta Menu state'te kalıyordu ve BlobController hareket ettirmiyordu. Düzeltme: `Start()` direkt `StartGame()` çağırır.
- **Kamera uzaklaşıyordu:** Tier değişince zoom vardı. Kullanıcı istemedi, kaldırıldı.
- **Smooth büyüme:** Başta tier atladıkça scale sıçrıyordu. `Pow` formülüyle smooth hale getirildi.
- **NavMesh spawn hatası:** Enemyler Y=0'da spawn oluyordu, NavMesh'e uzak kalıyordu. `EnemySpawnY = 0.65f` + `NavMesh.SamplePosition` ile düzeltildi.

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

### 🔲 Phase 5 — Upgrade Sistemi (SIRADA)

**Scriptler:**
- `Assets/_Project/Scripts/Systems/Upgrade/UpgradeSystem.cs`
  - `GameEvents.OnLevelUp` dinler
  - 3 rastgele UpgradeData seçer (weight bazlı)
  - `GameEvents.RaiseLevelUp()` → UI panel açılır
  - Seçilen upgrade'i uygular
- `Assets/_Project/Scripts/Systems/Upgrade/UpgradeEffect.cs` (abstract base)
- Concrete efektler (her biri ayrı dosya):
  - `SpeedBoostEffect.cs` — BlobController hızını artırır
  - `DamageReductionEffect.cs` — BlobHealth armor'ını artırır
  - `RegenBoostEffect.cs` — BlobHealth regen hızını artırır
  - `MagnetEffect.cs` — Consumable'ları blob'a çeker (yeni bileşen)
  - `ScoreMultiplierEffect.cs` — ScoreSystem multiplier'ını artırır
  - `HealthBoostEffect.cs` — Max health artırır

**Editor adımları:**
- UpgradeData asset'leri oluştur (her upgrade için bir SO)
- UpgradeSystem GameObject'e ekle, UpgradeData listesini bağla

---

### 🔲 Phase 6 — HUD & UI

**Scriptler:**
- `Assets/_Project/Scripts/UI/HUDController.cs`
  - Health bar (GameEvents.OnHealthChanged dinler)
  - Skor göstergesi (GameEvents.OnScoreChanged dinler)
  - Survival timer
  - Tier göstergesi (Tiny / Small / Medium / Large / Giant)
- `Assets/_Project/Scripts/UI/UpgradePanel.cs`
  - 3 upgrade kartı gösterir
  - Kart seçilince UpgradeSystem'e bildirir, panel kapanır
  - Oyun Paused state'e girer panel açıkken
- `Assets/_Project/Scripts/UI/GameOverScreen.cs`
  - Skor, highscore gösterir
  - Restart butonu → GameManager.StartGame()
- `Assets/_Project/Scripts/UI/SafeAreaHandler.cs`
  - iPhone notch / Android çentik desteği
  - Canvas RectTransform'u safe area'ya göre ayarlar

**Editor adımları:**
- Canvas oluştur (Screen Space - Overlay, UI Scale Mode: Scale With Screen Size 1080x1920)
- VirtualJoystick prefab'ını Canvas'a bağla
- HUD elemanlarını (Slider, Text, vb.) Canvas altına ekle

---

### 🔲 Phase 7 — Harita & Bölgeler

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

### 🔲 Phase 8 — Cila & Juice

- Parçacık efektleri (yeme, ölüm, tier atlama)
- Ses efektleri (AudioManager)
- Ekran sarsıntısı (CameraShake)
- Skor combo sistemi
- Blob'un gözleri / yüz animasyonu
