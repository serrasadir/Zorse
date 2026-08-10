# Blob.io — Sprint Roadmap

> **Kaynak dokümanı güncellendi:** Bu roadmap artık GDD v1.0 PDF'i değil, **`GDD_v2.md`**'yi (Temmuz 2026, tek kaynak doküman) takip eder. Sprint 1-2, v1.0 vizyonuna göre planlanmıştı; Sprint 3'ten itibaren GDD v2.0'ın kendi fazlandırması (**Faz 0 — Onarım Paketi** → **Prototip**, bkz. GDD_v2.md §14) esas alınır. Çelişki çıkarsa GDD_v2.md kazanır.
>
> Dev A (Sero/Serra) ve Dev B (Bahar) paylaşımlı sprint planı, Zorse Studio Blob.io projesi için.

---

## Sprint 1 — ✅ Tamamlandı (Karakter + Silah + Skill Leveling MVP)

- CharacterData + 3 karakter (Topik/Mıknato/Mermo), WeaponBase + 3 silah, karakter aktivasyon, Lobby UI
- UpgradeData level desteği, level-aware UpgradeSystem, Vakum, Kalkan, HUD skill rozetleri
- Tracking: GitHub issue #1

## Sprint 2 — ✅ Tamamlandı (Combat Progression + Rewards)

- A5 Düşman ölçekleme, A6 Elit düşman, A7 Coin drop, B8 Elit sandık drop, B9 Coin HUD + session-end
- **B6 (Dash) durumu düzeltmesi:** issue #15 kapatılmıştı ama kod incelemesinde `DashComponent`/`DashEffect`/`Upgrade_Dash.asset` repoda **yok**. Issue yeniden açıldı ve Sprint 3 kapsamına alındı (bkz. aşağı).
- Ek olarak bu oturumda: Magnet sistemi kaldırıldı, Vakum'a konsolide edildi (GDD Karar 9'un kod karşılığı, planlanmadan önce yapıldı).
- Tracking: GitHub issue #11

---

## Sprint 3 — Faz 0 Onarım + Yeme/Hazard Çekirdeği (1 hafta)

**Amaç:** GDD_v2.md §14 "Faz 0 — Onarım Paketi" maddelerini kapatmak ve Karar 5 (yeme birincil) + Karar 6 (hazard tam commit) mekaniklerini devreye almak. Bu iki karar oyunun artık resmi ana fiili; şu ana kadar hiç kod karşılığı yok.

**Kod incelemesinde doğrulanan somut buglar** (bu sprintin gerekçesi):
- `EnemyBase.Die()` ve `BlobConsumption.Consume()` sadece `SetActive(false)` çağırıyor, `ObjectPool<T>.Return()` çağırmıyor → pool Queue'su hiç dolmuyor, her spawn'da yeni `Instantiate` tetikleniyor (CLAUDE.md'nin "Instantiate/Destroy runtime'da yasak" kuralının sessizce ihlali, sızıntı).
- `BlobConsumption.OnTriggerEnter`'da hazard mantığı ters: `else if (!consumable.Data.IsHazard)` → hasar sadece hazard **olmayan** objelerde veriliyor, tam tersi olmalı. Ayrıca `HazardAmount` alanı hiç okunmuyor, hardcoded `MassValue * 0.5f` kullanılıyor.
- `UpgradeSystem.OnUpgradeSelected`'da `_blobRoot == null` ise fonksiyon `ResumeGame()` çağırmadan `return` ediyor → level-up ekranı `Time.timeScale=0`'da kilitli kalabilir (soft-lock).
- `GameManager.ApplyCharacter` restart'ta eski silahı temizlemeden yeni `Instantiate` yapıyor, pasifleri stack'liyor.
- `EnemySpawner`, spawn pozisyonunu `NavMeshAgent.Warp()` yerine doğrudan `transform.SetPositionAndRotation` ile veriyor (agent iç state'i ile senkron değil).

**Dosya sahipliği (çakışma önleme):** Dev A sadece `Entities/Enemies/*`, `Core/GameManager.cs`, yeni `Systems/Steering/*` dosyalarına dokunur. Dev B sadece `Entities/Blob/*`, `Entities/Consumables/*`, `Systems/Upgrade/*` dosyalarına dokunur. Ortak sınır **API sözleşmesiyle** çözülür (A12 → B13), dosya kesişimi yok.

### Dev A — Enemy/Pool/Core Mimarisi

| Kod | Görev | Dosyalar | Bağımlılık |
|-----|-------|----------|-------------|
| **A9** | Enemy pool return fix — `EnemyBase`'e `OnDeath` event eklenir, `EnemySpawner` bu event'i dinleyip `pool.Return()` çağırır (CoinSpawner'daki mevcut doğru pattern tekrar kullanılır); spawn'da `agent.Warp(pos)` kullanılır | `EnemyBase.cs`, `EnemySpawner.cs` | — |
| **A10** | Restart flow fix — `GameManager.ApplyCharacter` tekrar çağrılınca önceki silah instance'ı yok edilir/reuse edilir, pasifler idempotent uygulanır (stack'lenmez) | `GameManager.cs` | — |
| **A11** | Sürü düşman steering spike (Karar 2 ön şartı, GDD §7 Mimari) — basit steering (seek + separation, spatial hash grid), sadece normal Police enemy tipine opsiyonel mod olarak eklenir; Elit/Boss NavMesh'te kalmaya devam eder. **Bu bir spike** — tam performans doğrulaması Sprint 4'te | yeni `Systems/Steering/SwarmSteering.cs`, `EnemyBase.cs` | A9 |
| **A12** | Yutulabilirlik API'si (Karar 5) — `EnemyBase.TryConsumeByBlob(BlobTier blobTier)`: tier eşiği tutuyorsa (sürü→Tier3+, elit→Tier5) `true` döner + mass/coin/xp bilgisi verir + `Die()` tetikler; tutmuyorsa `false` | `EnemyBase.cs` | A9 |

### Dev B — Blob/Consumption/Upgrade Mimarisi

| Kod | Görev | Dosyalar | Bağımlılık |
|-----|-------|----------|-------------|
| **B12** | UpgradeSystem soft-lock fix + `CurrentLevel`'ın SO'dan runtime'a taşınması — `Dictionary<UpgradeData,int>` `UpgradeSystem` içinde tutulur, asset'in kendisi mutasyona uğramaz; `_blobRoot == null` durumunda da `ResumeGame()` her zaman çağrılır | `UpgradeSystem.cs`, `UpgradeData.cs` | — |
| **B13** | Hazard dalının canlandırılması + yeme-birincil entegrasyonu — `BlobConsumption`'daki ters mantık düzeltilir (`IsHazard` ise `HazardAmount` hasarı), Enemy layer için A12'nin `TryConsumeByBlob()` API'si çağrılır | `BlobConsumption.cs` | **A12** (API sözleşmesi önceden anlaşılır, dosya çakışmaz) |
| **B14** | Consumable pool return fix — `BlobConsumption.Consume()` ve `ConsumableBase` normal yeme akışında da `pool.Return()` eksik (aynı A9 bug'ı, consumable tarafında); `ConsumeAndSplit` zaten doğru pattern'i kullanıyor, referans alınır | `ConsumableBase.cs`, `ConsumableSpawner.cs`, `BlobConsumption.cs` (B13 ile aynı dosya, sıralı yapılır) | B13 |
| **B15** | B6 Dash yeniden implementasyonu — issue #15 yanlış kapatılmıştı, kod yok; kısa süreli hız burst'ü + cooldown, level başına cooldown↓/süre↑ | yeni `DashComponent.cs` (Entities/Blob), `DashEffect.cs` (Systems/Upgrade/Effects), `Upgrade_Dash.asset` | — |

**Kritik senkron:** A12 → B13 (yutulabilirlik API'si önce tanımlanmalı, iki dev de imzayı görüp anlaşmalı). B13 → B14 (aynı dosyada sıralı, aynı dev).

**Tahmini süre:** ~5-6 iş günü/dev.

---

## Sprint 4 — Prototip Devamı: Boss, Evrim, Analytics (1 hafta)

**Amaç:** GDD_v2.md'nin "Prototip" fazını ilerletmek — 12-15 dakikalık run yapısı, 4./8. dk miniboss + 12. dk final boss (Karar 1, 8), skill evrim sistemi, ve mobil-önce kararının bedeli olan analytics/save iskeleti (GDD §13).

**Dosya sahipliği:** Dev A → `Entities/Enemies/*` (boss dahil), `Core/GameManager.cs`, `Systems/Steering/*`. Dev B → `Systems/Upgrade/*`, `UI/HUDController.cs`, yeni `Systems/Meta/*` (save/analytics).

### Dev A — Boss & Run Yapısı

| Kod | Görev | Dosyalar | Bağımlılık |
|-----|-------|----------|-------------|
| **A14** | Sürü steering'in prod entegrasyonu — A11 spike'ının tamamlanması, 150-200 eşzamanlı düşman hedefiyle performans doğrulaması (mobil 30 FPS) | `Systems/Steering/SwarmSteering.cs`, `EnemyBase.cs` | A11 (Sprint 3) |
| **A15** | Miniboss sistemi — 4. ve 8. dakikada spawn, aynı tasarım artan stat, alan saldırısı; `BossData` ScriptableObject | yeni `Data/BossData.cs`, `Entities/Enemies/MinibossController.cs`, `WaveController.cs`'e tetikleyici | — |
| **A16** | Final Boss (12. dk) — tek faz geçişi, son fazda Tier5 blob'a `TryConsumeByBlob` ile yutulabilir hale gelir | yeni `Entities/Enemies/FinalBossController.cs` | A12 (Sprint 3), A15 |
| **A17** | Run süresi/sonu yapısı (Karar 1) — 12-15 dk run timer, final boss ölümü/yutulması ya da oyuncu ölümüyle run kapanır; `GameManager`'a run-end state | `GameManager.cs` | A16 |

### Dev B — Skill Evrimi & Altyapı

| Kod | Görev | Dosyalar | Bağımlılık |
|-----|-------|----------|-------------|
| **B16** | Yeme-temalı 3 yeni skill (GDD §5 🆕) — Sindirim (mass kazancı +%), Yutuş (yemede HP yenile), Yırtıcı Çene (yutma eşiği kolaylaşır — `TryConsumeByBlob` toleransını etkiler) | yeni `SindirimEffect.cs`, `YutusEffect.cs`, `YirticiCeneEffect.cs`, ilgili `Upgrade_*.asset`'ler | A12 (Sprint 3) |
| **B17** | Skill Evrim (Evolution) sistemi — issue #16 (B7) carryover; `SkillEvolutionData` SO (input skills, output skill), `EvolutionSystem`; lansman evrimleri: **Kara Delik** (Attract max + Sindirim max), **Avcı Formu** (Yırtıcı Çene max + Silah Gücü max) | yeni `Systems/Upgrade/EvolutionSystem.cs`, `Data/SkillEvolutionData.cs` | B16 |
| **B18** | Boss health bar UI — miniboss/final boss can barı, `GameEvents.OnBossHealthChanged` dinler (event'i A15/A16 raise eder, dosya çakışmaz) | `HUDController.cs` | A15, A16 |
| **B19** | Analytics + save iskeleti (GDD §13, "ilk günden zorunlu") — JSON tabanlı basit save (run sayısı, ölüm dakikası/nedeni, kart seçim oranları); üçüncü parti servis entegrasyonu değil, sadece yerel log altyapısı | yeni `Systems/Meta/RunAnalytics.cs`, `Systems/Meta/SaveSystem.cs` | — |

**Kritik senkron:** A12 → B16 (yutma toleransı API'sinin genişletilebilir olması gerekir) · A15/A16 → B18 (boss health event kontratı) · B16 → B17 (evrim, önce bileşen skiller'i ister).

**Not — ertelenen açık kararlar:** GDD §16'daki "Score Multiplier kart mı/meta mı" ve "nihai oyun adı" kararları henüz verilmedi; bu yüzden Sprint 4'e implementasyon işi olarak alınmadı. Karar verildiğinde bir sonraki sprint'e eklenir.

**Not — kod/doküman çelişkisi (aksiyon beklemiyor, takip için):** GDD_v2.md §5 "Kalkan iptal edildi (Zırh ile aynı iş)" diyor ama kod hâlâ çalışan bir Kalkan/Shield sistemi içeriyor (B4, `ShieldEffect.cs`, `BlobHealth.CurrentShield`). Kaldırmak canlı bir sistemi bozar; kullanıcı onayı olmadan dokunulmadı. İleride netleştirilmeli.

---

## Sprint 3 + 4 Özet Tablosu

| Kod | Görev | Dev | Sprint | Bağımlılık |
|-----|-------|-----|--------|-------------|
| A9 | Enemy pool return + Warp fix | A | 3 | — |
| A10 | Restart flow fix (silah/pasif stack) | A | 3 | — |
| A11 | Sürü steering spike | A | 3 | A9 |
| A12 | Yutulabilirlik API'si | A | 3 | A9 |
| B12 | UpgradeSystem soft-lock + CurrentLevel runtime | B | 3 | — |
| B13 | Hazard fix + yeme entegrasyonu | B | 3 | A12 |
| B14 | Consumable pool return fix | B | 3 | B13 |
| B15 | B6 Dash yeniden implementasyon | B | 3 | — |
| A14 | Steering prod entegrasyonu | A | 4 | A11 |
| A15 | Miniboss (4./8. dk) | A | 4 | — |
| A16 | Final Boss (12. dk) + yutulma | A | 4 | A12, A15 |
| A17 | Run süresi/sonu yapısı | A | 4 | A16 |
| B16 | 3 yeme-temalı skill | B | 4 | A12 |
| B17 | Skill Evrim sistemi | B | 4 | B16 |
| B18 | Boss health bar UI | B | 4 | A15, A16 |
| B19 | Analytics + save iskeleti | B | 4 | — |

---

## Sonraki Sprintler (Öngörü)

**Sprint 5:** MVP dikey dilim tamamlama — Market ekranı, meta kredi aktarımı, 6 kalemlik kalıcı stat mağazası (GDD §8).
**Sprint 6:** Grimoire tracking hook'ları (UI'sız log altyapısı) + NG+ zorluk taslağı — post-launch'a hazırlık, lansman kapsamı dışı ama erken enstrümantasyon.
**Sprint 7+:** Soft launch hazırlığı — GameAnalytics/Unity Analytics entegrasyonu, size-flip pazarlama klibi teknik ihtiyaçları, D1/D7 metrik toplama.

Bu roadmap Sprint 3 kapanışında güncellenir.
