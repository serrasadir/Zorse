# Blob.io — Sprint Roadmap (GDD Bölüm 1-4)

Bu doküman GDD v1.0'ın **1-4. bölümlerini** kapsayan implementasyon planıdır. Zorse Studio Blob.io projesi için, Dev A (Sero) ve Dev B (Bahar) paylaşımlı sprint planı.

**Kaynak:** `/Users/baharyavuz/Downloads/Zorse GDD v.1 (1).pdf` (v1.0, Haziran 2025)

---

## Bölüm 1 — Oyun Vizyonu

**Tasarım dokümanı** — kod implementasyonu gerektirmez. CLAUDE.md'de özetlendi.

- Tür: Roguelite / Bullet Heaven / Survival
- Platform: Mobil (ana), PC (ikincil)
- Oturum: 20-30 dk
- Referanslar: Vampire Survivors + Katamari Damacy + Hole.io

**Durum:** ✅ CLAUDE.md'de dokümante

---

## Bölüm 2 — Gameplay Loop

### Sprint 1'de Tamamlanan
- ✅ Lobby → gameplay flow (A4)
- ✅ Consumable spawn + yeme + büyüme (Phase 2-3)
- ✅ XP toplama + level up + 3 kart seçim (Phase 5)
- ✅ Düşman spawn + saldırı + game over (Phase 4)
- ✅ Mobil portrait HUD (Sprint 1 kapanışı)

### Sprint 2'de Yapılacak
- 🔲 **A5** — Düşman ölçekleme (0-5-10-15-20-25 dk hasar/hız/yoğunluk artışı)
- 🔲 **A6** — Elit düşman davranışı (özel saldırı deseni, yüksek HP)
- 🔲 **A7** — Coin drop sistemi (düşman öldüğünde coin, magnet mesafesinde toplanır)
- 🔲 **B8** — Elit düşman sandık drop (skill + yüksek coin)

### Sprint 3'e Devir
- 🔲 5/10/15/20 dk minyatür boss dalgaları (SWAT arabası, drone, helikopter)
- 🔲 25. dakika Kıyamet Bossu (faz geçişleri, combo saldırılar)
- 🔲 Boss health bar UI
- 🔲 Session-end reward calculation + Market ekranına geçiş

---

## Bölüm 3 — Karakterler

### Sprint 1'de Tamamlanan
- ✅ CharacterData ScriptableObject (A1)
- ✅ 3 karakter: Topik, Mıknato, Mermo (A1)
- ✅ WeaponBase + 3 silah: Top, Metal Bilye, Pistol (A2)
- ✅ Karakter aktivasyon + `StartGame(CharacterData)` (A3)
- ✅ Karakter pasif runtime uygulama (A3)
- ✅ Minimal Lobby UI (A4)

### Sprint 2'de Yapılacak
- 🔲 **A8** — Karakter kilit sistemi
  - Karakter unlock flag'i (PlayerPrefs)
  - Mıknato: Market'te 500 kredi ile açılır
  - Mermo: 3 farklı haritada oturum tamamlama şartı
  - Lobby'de kilitli karakter grey out + kilit ikonu

### Sprint 3+'a Devir
- 🔲 Karakter pasif kademe sistemi (Meta - 5 kalıcı seviye)
- 🔲 Karakter skin sistemi (kozmetik)
- 🔲 Karakter Grimoire lore metinleri

---

## Bölüm 4 — Silah & Güçlendirme Sistemi

### Sprint 1'de Tamamlanan
- ✅ UpgradeData leveling (MaxLevel + PerLevelValue + CurrentLevel) — B1
- ✅ UpgradeSystem level-aware seçim (max'a ulaşan havuzdan çıkar) — B2
- ✅ Vakum skill (VacuumComponent + tier filtresi) — B3
- ✅ HUD skill badges (active upgrade icons + level) — B5
- ✅ 6 base skill: Speed, Armor, Regen, MaxHealth, Score, Magnet
- ✅ Weapon upgrade sistemi (Sero'nun bonus işi)
- ❌ Kalkan — iptal edildi (Zırh ile aynı iş)

### Sprint 2'de Yapılacak
- 🔲 **B6** — Hızlanma (Bot/Dash) skill
  - Kısa süreli hız burst'ü
  - Cooldown mekanik
  - Level başına cooldown azalır / süre artar
- 🔲 **B7** — Skill Evrim (Evolution) sistemi
  - 2 skill max level'a ulaşınca birleşip yeni skill oluştur (örn. Magnet + Vacuum → SuperMagnet)
  - `SkillEvolutionData` ScriptableObject (input skills, output skill)
  - Evrim tetikleme UI'ı
- 🔲 **B9** — Coin HUD + session-end reward özet
  - HUD'da coin sayacı
  - Session sonunda toplam coin + kırdığı highscore vs.

### Sprint 3+'a Devir
- 🔲 Karakter özel silah evrimleri (her karakter için özgün)
- 🔲 Silah kaplama (kozmetik)

---

## Sprint 2 Özet Tablosu

| Kod | Görev | Dev | Bağımlılık |
|-----|-------|-----|-------------|
| A5 | Düşman ölçekleme | A | — |
| A6 | Elit düşman davranışı | A | A5 |
| A7 | Coin drop sistemi | A | — |
| B6 | Hızlanma skill | B | — |
| B7 | Skill Evrim sistemi | B | B1-B2 |
| B8 | Elit sandık drop | B | A6 (elit tanımı), A7 (coin) |
| B9 | Coin HUD + session-end | B | A7 |

**Kritik senkron:** A6 → B8 (elit tanımı gerekir), A7 → B8 + B9 (coin sistemi gerekir).

**Tahmini süre:** Her issue 0.5-1 gün → toplam ~5-6 iş günü/dev.

---

## Sonraki Sprintler (Öngörü)

**Sprint 3:** Boss dalgaları + session-end + Market ekranı taslağı
**Sprint 4:** Meta progression (kalıcı kredi, karakter unlock, XP çarpanı)
**Sprint 5:** Grimoire (kodeks) + NG+ zorluk seviyeleri
**Sprint 6:** Bölüm 5-6 (haritalar, hava durumu)

Bu roadmap Sprint 2 kapanışında güncellenir.
