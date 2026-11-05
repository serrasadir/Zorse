# Left No Crumbs (Zorse Studio) — CLAUDE.md

Claude Code bu dosyayı her oturumda otomatik okur. **Burada sadece çalışma kuralları ve teknik notlar var.**

## Önce oku

| Dosya | İçerik |
|---|---|
| `OYUN.md` | Oyunun **güncel hali** (döngü, sistemler, değerler). Tasarımla ilgili tek kaynak |
| `SKILLS.md` | Skill, karakter, düşman/boss sayıları |
| `ASSET_RULES.md` | 3B asset üretim kuralları (Blender script'leri) |
| `DISCOVERY_STRATEGY.md` | Pazarlama / tanınma planı |
| GitHub issues | **Bekleyen işler** (`gh issue list`) |

**Belge kuralları:**
- Bir şey değişince `OYUN.md` / `SKILLS.md` güncellenir: eski hali silinir, yenisi yazılır. Üstü çizili satır, tarihli "eskiden şöyleydi" notu yok — geçmiş git commit'lerinde.
- Bekleyen iş için yeni md açılmaz, issue açılır. İş bitince issue kapanır, sonucu `OYUN.md`'ye işlenir.
- Bu dosyaya tarihçe yazılmaz; sadece kalıcı kural / tuzak eklenir.
- Kodda "neden böyle" yorumu kısa tutulur.

## Çalışma şekli

- Claude tüm C# kodunu yazar; Serra sadece Unity Editor adımlarını uygular. Editor adımı gerekiyorsa **önce bir `Tools → Blob.io → ...` menüsü yazmayı** düşün (elle sürükleme yerine) ve net adım ver.
- **Unity açıkken** `ProjectSettings/*.asset` ya da açık sahneyi (`GameScene.unity`) dışarıdan düzenleme — Unity üstüne yazar. Bunlar editör menüsüyle (SerializedObject) değiştirilir. ScriptableObject asset'leri ve kapalı prefab'lar YAML'dan düzenlenebilir.
- Unity açık mı: `pgrep -x Unity`. Derleme hataları: `~/Library/Logs/Unity/Editor.log` (`error CS`).
- **Unity dışında derleme:** Unity'nin ürettiği `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` ile `dotnet build`, ya da bu csproj'lardan üretilmiş bir response dosyasıyla Roslyn `csc.dll`. Her kod değişikliğinden sonra derle.
- Performans açısından pahalı bir istek gelirse kullanıcıya söyle.
- Commit/push sadece istenince. TMP `LiberationSans SDF - Fallback.asset` dinamik atlas — commit'leme.
- Oturum sonu: commit + push, `OYUN.md`/`SKILLS.md` güncel mi, test edilen issue'ları kapat, yeni bilinen işler için issue aç.

## Performans kuralları (mobil hedef)

- `FindAnyObjectByType`, `GetComponent` Update/FixedUpdate/OnCollision içinde yok → Awake/Start/olay anında cache'le.
- Fizik sorguları (Overlap/Raycast/SphereCast) her kare değil, 0.1–0.4 sn throttle + `Random.Range` stagger. Her zaman LayerMask ver; NonAlloc kullan.
- Mesafe karşılaştırmada `sqrMagnitude`.
- Her kare string üretme; değer değişince güncelle.
- **Runtime'da Instantiate/Destroy yok** — her spawn havuzdan (`PoolManager.Instance.CreatePool(prefab, n)` / `GetPool<T>(prefab).Get(...)` / `pool.Return(x)`). Prefab başına tek seferlik runtime kurulum (bileşen ekleme vb.) kabul.
- NavMeshAgent pahalı: sadece elit/boss; sürü polisi ve yayalar steering.

```csharp
private float _timer; private const float Interval = 0.15f;
void Start() => _timer = Random.Range(0f, Interval);
void Update() { _timer -= Time.deltaTime; if (_timer > 0f) return; _timer = Interval; /* pahalı sorgu */ }
```

## Unity 6 notları

- `rigidbody.linearDamping` / `angularDamping` / `linearVelocity` (drag/velocity değil); `FindAnyObjectByType` (FindObjectOfType değil); `GetInstanceID` yerine referans anahtar.
- New Input System — `UnityEngine.Input` kullanılamaz.
- Unity filename = classname. `ObjectPool<T>` sıradan generic sınıf (`ObjectPool.cs`), `PoolManager` ayrı MonoBehaviour.
- Enter Play Mode'da domain reload açık (statikler sıfırlanır).

## Mimari

**Namespace'ler:** `BlobSurvivor.Core` (GameManager, GameEvents, kamera, Haptics, DevCheats) · `.Entities` (Blob, Consumables, Enemies, Pedestrians, Vehicles, Weapons, Coins, Loot) · `.Systems` (Pool, Score, Wave, Upgrade, Meta, Spawning, Steering) · `.UI` · `.Data` (ScriptableObject'ler) · `.Input` · `.EditorTools` (Editor/).

**Katmanlar:**

| Index | Layer | Not |
|---|---|---|
| 8 | Blob | Bir trigger (yeme, r 0.6) + bir katı (r 0.5) SphereCollider |
| 9–13 | ConsumableTiny, Small, Medium, Large, Giant | `TierLayers.For(tier)`; yayalar da bu katmanlarda. Tier yetmezse collider katı, yetince trigger. Maske için `TierLayers.ConsumableMask`, elle `1 << 9..13` yazma |
| 14 | Enemy | |
| 15 | Hazard | Arabalar |
| 16 | Environment | Bina engel kutuları (`EnvironmentObstacle` child), harita duvarı. Mermi/görüş/kamera saydamlığı buna bakar |
| 17 | Ground | |
| 18 | Projectile | |
| 19 | PropObstacle | Sokak eşyası yürüyüş engeli; hiçbir şeyle çarpışmaz, sadece yaya/sürü polisi yürüyüş sorguları görür |
| 20 | ConsumableColossal | Dev (`BlobTier.Colossal`) — sonradan eklendi, 14 Enemy dolu olduğu için ayrı |
| 21 | ConsumableLittle | `BlobTier.Little` (Tiny ile Small arası, sonradan eklendi). Tier sayısal değeri katman sırası değil; hız/yüz dizisi için `SlowdownRank`/`LegacyIndex` |

**Olay veriyolu:** `GameEvents` (statik). Önemlileri: `OnBlobTierChanged` + `CurrentBlobTier`, `OnLevelUp`, `OnUpgradeChoicesReady/Selected/Applied`, `OnHealthChanged`, `OnSurvivalTimeUpdated` (0.1 sn), `OnCharacterSelected` (= run başı/restart), `OnGameOver` (ölüm), `OnRunComplete` (final boss yutuldu), `OnBossHealthChanged` (max ≤ 0 = boss yok).

**GameState:** Menu / Playing / Paused / LevelUp / GameOver / RunComplete.

## Tuzaklar (bilinmesi gerekenler)

- **Restart sahneyi yeniden yüklemez.** Run'a ait her durum `GameManager.ClearPreviousRunState()` ya da `OnCharacterSelected` dinleyicisinde sıfırlanmalı; skill bileşenleri orada silinir (`RemoveComponent<T>`). Yeni run-scoped şey eklerken buraya ekle.
- **Havuz:** `Get()` `OnEnable`'ı `SetData()`'dan önce tetikler — veriye bağlı kurulumu `SetData`'da ya da ilk Update'te yap. `ObjectPool.Return` pasif nesneyi tekrar kuyruğa almaz (çift tetiklenme koruması).
- **İki NavMesh var** (Humanoid + geniş "Tank" ajanı). Her NavMesh sorgusunda filtre ver: `SpawnPositionUtility.HumanoidFilter` — filtresiz `NavMesh.AllAreas` "hangi ajan tipi" uyarısı basar. Sokak eşyaları sadece Humanoid NavMesh'te engel (`m_AffectedAgents = [0]`), tank onları ezer.
- **Blender çıktısı ön yüzü ters çeviriyor:** `H_`/`V_`/`B_` modellere 180° düzeltme (`CityKitPipeline.ApplyModelTransform`). Prefab model kuralları (ölçek, yön, collider) orada; `Şehir Görsel Düzeltmeleri` tekrar çalışınca ezilmez.
- **Şehir objeleri static batching'de** — runtime'da taşınmaz (collider kayar, görsel kalır). Yenen şehir objesi gizlenir, `CityLevel` run sonunda geri açar.
- **`Art/City` altına giren dokular** palet ayarına zorlanır (Point, Clamp, mipmap yok: `CityKitPipeline` ön işlemcisi). Tekrar eden desen dokuları `Art/City/Textures/`'a konur (Repeat + mipmap).
- **Saydam/özel materyaller** build'de ayıklanmasın diye prefab'da referanslı ya da `Resources/` altında olmalı; runtime'da shader'dan materyal üretme.
- Final boss'un collider'ı havada (`EnemyBase.AirborneHeight`/`AimPoint`); kök yerde NavMesh'te.
- UnityEvent (OnClick) slotuna Project penceresinden script değil, **Hierarchy'den GameObject** sürüklenir.
- Proje `~/Developer/zorse`'ta (iCloud dışı). `../zorse-ios-build` ve `~/Developer/zorse-blender` (model kaynakları, `build_*.py`; ayrı private repo `serrasadir/zorse-blender`, `.blend`/render'lar git'e girmez) yan yana durmalı. Script değişince o repo da commit'lenir.
- graphify repo kökünde çalıştırılır (`graphify-out/` gitignore'da).

## Editör araçları (`Tools → Blob.io → ...`)

| Menü | Ne yapar |
|---|---|
| Şehir Kur (Level01) / Şehir Kur (Büyük) | Blender kitinden şehri kurar (`CityKitPipeline`, `CityExpansion`) |
| Şehir Görsel Düzeltmeleri | Model yönü/ölçeği, bina kısaltma, collider ayak izleri, NavMesh engelleri, PropObstacle katmanı, ghost materyaller, **iki NavMesh'i bake eder**. Tekrar çalıştırmak güvenli; sonra Cmd+S |
| Polis Modellerini Uygula · Miniboss Tank Modelini Uygula · Final Boss Helikopterini Uygula · Coin Modelini Uygula | Prefab modellerini kurar |
| Tank NavMesh'ini Kur | "Tank" ajan tipi + ikinci NavMeshSurface |
| Yaya Varyantlarını Bağla | PedestrianData varyant listeleri + spawner havuzu |
| Sandık Modelini Uygula | `Art/City/C_Chest.fbx` (Blender: `build_chest.py`) → Chest.prefab modeli |
| **Şehir Kur (v2)** | Yeni şehir: `Art/City/Kit2/city_v2.json` (Blender: `build_kit2.py --export` binalar, `build_props2.py --export` eşya + blok zeminleri, `build_city2.py --export` mahalle yerleşimi). Yollar/kaldırım/yaya/araba/NavMesh büyük şehirle aynı. Sonra Şehir Görsel Düzeltmeleri + Cmd+S |
| Şehir Kur (Kit2 Deneme) | Büyük şehir + bina noktalarına şehir kiti v2 binaları (`Art/City/Kit2`, Blender: `build_kit2.py --export`). Performans denemesi; eski şehre dönmek için Şehir Kur (Büyük). Sonra Şehir Görsel Düzeltmeleri |
| Karakter Görünümlerini Uygula | `Art/City/Characters/CH_*.fbx` (Blender: `build_blobs.py`, karakter ve tier başına `CH_<Karakter>_T1..T5`) → `Prefabs/Blob/BlobVisual_*` (+ `BlobTierMeshes`) + `CharacterData._blobVisualPrefab`. Runtime'da `BlobVisual` takar, gidiş yönüne çevirir; boş = varsayılan küre |
| Yerdeki Yiyecekleri Kur | `Art/City/Litter/J_*.fbx` (Blender: `build_litter.py`) → prefab + ConsumableData, ConsumableSpawner havuzunu bunlarla değiştirir. GameScene açıkken; sonra Cmd+S |
| Kalp Prefab'ını Kur · Haber Bandını Kur · Duraklatma Menüsünü Kur | İlgili sahne/prefab kurulumu |
| Görsel Ayarları Uygula | Post-processing, ışık, ortam |
| Helikopter Mermi Roketini Kur | Final boss mermisi: alan saldırısının roket şablonundan küçük `HeliRocketBullet` (alev titrer, değince patlar, alan hasarı) → `EnemyData_FinalBoss._projectilePrefab`. Elit polis `EnemyBullet`'ta kalır |
| Tier'ları Boya Göre Kur | Tüketilebilir katman adları + Little/Colossal çarpışması, sahnedeki tier eşikleri, tüm yenebilirlerin tier'ı boydan (`TierRules`: blob çapının %5 fazlasına kadar yenir). Polis/elit hariç. Şehir kurulumu da aynı kuralı kullanır. Sonra Cmd+S |
| Şehir Kıyısını Kur | Şehrin dışı: `CityShore` kökü (W_SeaWall sur parçaları + köşede W_WallTower, Blender: `build_seawall.py`), alçakta su (`WaterScroll` kaydırır), gri Ground plane'in renderer'ını kapatır (collider kalır), Ground'u ve kıyıyı NavMesh'ten çıkarır (`NavMeshModifier`; sonra Görsel Düzeltmeler bake eder), kaldırım karosunu zemine uygular. Dokular: `build_textures.py`. Sonra Cmd+S |
| Yeme Efektini Kur | `Resources/FX/EatCrumbs` (kırıntı parçacığı + materyali) ve her yenebilirin/arabanın kırıntı renkleri (modelin palet renkleri). Şehir kurulumu renkleri kendisi de yazar. Sonra Cmd+S |
| Yenebilir Boyut Raporu | Yenebilir her şeyin oyundaki boyu + kurala göre tier'ı → `Temp/tier_sizes.txt` |
| (Claude) `Temp/menu.request` · `Temp/snapshot.request` | `MenuRequest`: satır başına menü yolu yazılır, sırayla çalışır → `Temp/menu.done`. `SceneSnapshot`: "x z mesafe [yaw] [eat t]" → sahnenin oyun açısıyla `Temp/snapshot.png` (görsel kontrol; `eat t` en yakın Medium+ objenin yeme kırıntılarını t sn ilerletip çeker) |
| Blob Çevresini Raporla | Play'de blob çevresini `Temp/blob_probe.txt`'ye yazar (`Temp/blob_probe.request` ile de tetiklenir) |
| TestFlight Xcode Projesi Üret | Release iOS build, TestFlight için (build no +1) |
| iOS Xcode Projesi Üret | Geliştirme build'i, cihaza doğrudan kurmak için |

**Performans göstergesi** (`PerfOverlay`, telefonda da): 3 parmakla dokun (Editor'da F3) → FPS, en kötü kare, +500 kütle düğmesi. **Yayın öncesi `PerfOverlay.Enabled = false`.**

**Editor test tuşları** (`DevCheats`, sadece Editor): **M** +500 kütle (XP'siz) · **B** final boss −%25 can · **H** can doldur · **N** sıradaki boss aşamasını hemen çıkar (aktif boss kalkar).

## iOS / TestFlight

1. Sürüm Player Settings'te elle: `0.1.<build>` (canlıya çıkana kadar major 0). File → Save Project.
2. `Tools → Blob.io → TestFlight Xcode Projesi Üret` → log `[TestFlightBuildTool] Succeeded ... version=... build=...` ve `[ShaderStripGuard] OK`. Guard build'i durdurursa (gri ekran riski) Unity'yi yeniden başlatıp tekrar al.
3. Arşiv:
   `cd ../zorse-ios-build && xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release -destination generic/platform=iOS -archivePath ~/Library/Developer/Xcode/Archives/<tarih>/LeftNoCrumbs-<sürüm>-<build>.xcarchive -derivedDataPath ~/Library/Developer/Xcode/DerivedData/zorse-dd -allowProvisioningUpdates archive`
4. Yükleme: `xcodebuild -exportArchive -archivePath <arşiv> -exportOptionsPlist ExportOptions.plist -exportPath <dizin> -allowProvisioningUpdates` — plist: `method=app-store-connect`, `destination=upload`, `teamID=66ZS86R987`, `signingStyle=automatic`, `manageAppVersionAndBuildNumber=false`. `UnityRuntime.framework` dSYM uyarısı zararsız.
- Bundle id `com.zorsestudio.leftnocrumbs`, Team `66ZS86R987` (sadece `Unity-iPhone` hedefine). Cihaza doğrudan: `xcrun devicectl device install app --device <id> <app>`.
