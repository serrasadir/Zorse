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

## Sprint 3 — ✅ Tamamlandı (Faz 0 Onarım + Yeme/Hazard Çekirdeği)

**Amaç:** GDD_v2.md §14 "Faz 0 — Onarım Paketi" maddelerini kapatmak ve Karar 5 (yeme birincil) + Karar 6 (hazard tam commit) mekaniklerini devreye almak.

- **Dev A** (commit `7a6ae3e`): A9 enemy pool return + `NavMeshAgent.Warp()` fix, A10 restart flow fix (silah/pasif stack + kaybolan karakter seçimi bug'ı), A11 sürü steering spike (`SwarmSteering.cs`), A12 yutulabilirlik API'si (`EnemyBase.TryConsumeByBlob`)
- **Dev B** (commit `aa9453b`): B12 UpgradeSystem soft-lock fix + `CurrentLevel` runtime'a taşınması, B13 hazard mantığı düzeltmesi + yeme entegrasyonu, B14 consumable pool return fix, B15 Dash yeniden implementasyonu (otomatik/periyodik — GDD'de "Hızlanma (Bot)")
- Tracking: GitHub issue #19 (+ #20-26, #15) — hepsi kapalı

**Kalan Editor adımları** (kod tamam, sahne kurulumu bekliyor — bkz. CLAUDE.md "Bilinen Eksikler"):
1. Sahneye `SwarmSteering` component'li bir GameObject eklenmeli
2. Physics Collision Matrix'te Blob(8) × Enemy(14) doğrulanmalı (yeme mekaniği için)
3. `Upgrade_Dash.asset`, `UpgradeSystem._allUpgrades` dizisine eklenmeli

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

## Sprint 5 — Market & Meta İlerleme (GDD §8)

**Amaç:** MVP dikey dilimin son parçası — run içi coin'in kalıcı ilerlemeye dönüşmesi. Lansman kapsamı: 1 karakter unlock (Mıknato) + 4 düz kalıcı stat + XP çarpanı, 6 kalemlik Market. Ayrıca GDD_v2.md Karar 14 — yaya (sivil NPC) sistemi.

**Not:** Bahar (Dev B) izinli, bu sprint tek dev (Serra) yürütüyor — A/B dosya-sahipliği ayrımına gerek yok, tek sıralı liste. Kod yine tamamen assistant tarafından yazılıyor, Editor kurulumu Serra'da.

| Kod | Görev | Dosyalar | Bağımlılık |
|-----|-------|----------|-------------|
| **M20** | Meta kredi/kalıcı stat data katmanı — `SaveSystem`'in JSON'ına kalıcı kredi + satın alınan stat seviyeleri eklenir; run bitince kalan coin krediye döner (tamamlanan run'da %100, ölümde %50) | `Systems/Meta/SaveSystem.cs`, yeni `Systems/Meta/MetaProgression.cs` | B19 (Sprint 4) |
| **M21** | Market UI ekranı — 6 kalem: Mıknato unlock (500), +%5 Hız, +10 Max HP, +%5 Mass kazancı, +%5 Coin kazancı (hepsi artan maliyetli), XP Çarpanı +%10 (1000) | yeni `UI/MarketPanel.cs` | M20 |
| **M22** | Kalıcı statların run başında uygulanması — `GameManager.StartGame()`/`ApplyCharacter` akışına meta bonus katmanı | `GameManager.cs` | M20 |
| **M23** | Lobby ↔ Market giriş/çıkış butonu | `LobbyPanel.cs` | M21 |
| **M24** | Yaya (sivil NPC) sistemi — GDD Karar 14: blob küçükken normal yürür (wander), yutma eşiğine yakınsa kaçar (flee), yutulabilir; animasyonsuz sinüs-bob hareketi, sürü düşmanlarıyla aynı ajan bütçesini paylaşır | yeni `Data/PedestrianData.cs`, `Entities/Pedestrians/PedestrianController.cs`, `Entities/Pedestrians/PedestrianSpawner.cs` | — |

**Kritik senkron:** M21/M22 → M20 (data katmanı önce, UI ve runtime uygulama ondan sonra).

---

## Sonraki Sprintler (Öngörü)

**Sprint 6:** Grimoire tracking hook'ları (UI'sız log altyapısı) + NG+ zorluk taslağı — post-launch'a hazırlık, lansman kapsamı dışı ama erken enstrümantasyon.
**Sprint 7+:** Soft launch hazırlığı — GameAnalytics/Unity Analytics entegrasyonu, size-flip pazarlama klibi teknik ihtiyaçları, D1/D7 metrik toplama.

Bu roadmap Sprint 5 kapanışında güncellenir.
