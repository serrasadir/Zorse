# Blob Survivor — GDD (Vizyon)

> Bu doküman **vizyon belgesi**dir, özellik listesi değil. Phase-spesifik sistem detayları `docs/systems/` altına yazılır. Balance değerleri ScriptableObject'lerde yaşar.

---

## 1. Künye

- **Oyun adı:** _(TBD — Zorse / Blob Survivor / ?)_
- **Kod adı:** Zorse
- **Pitch (1 cümle):** _(TBD)_
- **Tür:** Top-down 2.5D survival roguelite
- **Platform:** _(TBD — mobil öncelikli mi?)_
- **Hedef kitle:** _(TBD)_
- **Run süresi hedefi:** ~5–10 dk (boss/extract ile kapanır)
- **Ekip:**
  - Hüma — Game Artist, UI/UX Designer
  - Serra — Game Developer, Game Designer
  - Bahar — Game Developer, Game Designer
- **Hedef milestone:** _(TBD)_

---

## 2. Vizyon & Pillar'lar

### 3 Pillar
1. **Smooth büyüme hissi** — tier atlama ani değil, akıcı (`Pow` formülü).
2. **Risk/ödül: büyüdükçe yavaşla** — güç ≠ rahatlık (`hız = 1/√tier`).
3. **Her run farklı** — upgrade kombinasyonları + meta unlock'lar replay sağlar.

### USP
Agar.io'nun büyüme curve'ü + Vampire Survivors'ın upgrade chaos'u + gerçek şehir bölgelerinde (Park / Liman / Endüstri) tier-bazlı keşif.

### Referanslar
- **Vampire Survivors:** upgrade kart sistemi, boss/extract pacing, meta unlock
- **Agar.io:** büyüme curve'ü, tier-bazlı yeme mantığı
- **Katamari Damacy:** görece ölçek hissi, "her şeyi yutma" fantezisi

### Game feel (1 cümle)
"Tatlı bir kaos: 'bir run daha' dedirten."

### Anti-vizyon (bu oyun NE değildir)
- Hikaye odaklı **değil** (sadece environmental storytelling).
- PvP **değil**.
- Realistik **değil** — stilize, sevimli/absürt ton.
- Sonsuz run **değil** — her run boss / extract ile kapanır.

---

## 3. Hikaye & Dünya

- **Setting:** _(TBD — gerçek şehir mi, kurgu mu?)_
- **Blob kim:** _(TBD)_
- **Düşmanlar kim:** _(TBD — polis / mutant / robot / ?)_
- **Ton:** _(TBD — komik / sevimli / absürt?)_
- **Anlatım biçimi:** Cutscene yok. Bölge teması + asset diliyle environmental storytelling.

---

## 4. Core Gameplay Loop

### 30 saniyelik loop (moment-to-moment)
Hareket et → consumable gör → tier kontrol → ye → smooth büyü → tehlikeden kaç.
Anlık tatmin: squash anim, ses, skor pop-up, haptic.

### 2–5 dakikalık loop (mid-term)
Mass biriktir → tier atla → level-up → **3 upgrade kartından 1 seç** → yeni güçle daha büyük objeleri yiyebil → tier boss spawn → boss'u ye → bir sonraki tier'a geç.

### Run loop (5–10 dk)
Run başla → tier 1–5 yolculuğu → final boss / extract anı → **öl veya bitir** → skor + altın → meta-progression menüsü → unlock kontrol → tekrar başla.

### Meta loop (run-arası, motivasyon katmanı)
Altın → kalıcı stat upgrade + unlock görevleri → yeni blob karakteri / yeni upgrade kartı / yeni bölge açılır → bir sonraki run daha çeşitli.

> **Tasarım kararı (sonsuz büyüme tartışması):**
> Run içinde tier 5 tavan, **meta düzeyde** yeni bölgeler açılır. Vampire Survivors'ın 30dk extract'i = bizim boss/tier-5 bitirimi. Sonsuz büyüme fantezisi run içi değil, meta düzeyde yaşar.

---

## 5. Unlock & Ödül Sistemi (Goal Injection)

Oyuncuyu geri getiren motor. Her görev **görünür hedef + somut ödül** prensibinde.

### Ödül 3 katmanda
**Katman 1 — Mekanik unlock (oyunu değiştirir)**
Oyuncunun yaptığı şeyi değiştiren ödüller. MVP'de **az sayıda ama anlamlı** olmalı.
- Yeni blob karakterleri (farklı başlangıç stat'ları)
- Yeni upgrade kartları (havuza eklenir)
- Upgrade evolution'ları (2 upgrade kombine olunca özel efekt)
- Yeni bölgeler (Şehir → Park → Liman → Endüstri)

**Katman 2 — Kantitatif ödül (kalıcı stat)**
Her run'da biriken altın → meta-progression menüsünde harcanır. Küçük ama sürekli.
- +%5 baz hız / +1 baz HP / +%5 mass kazancı vb.
- Ölüm = boşa run değil. "Bir run daha" hissinin yarısı bu.

**Katman 3 — Cosmetic & koleksiyon**
Skin'ler, trail efektleri, başarım rozetleri. **Phase 8'e** ertelenir.

### Görev tipi kalıpları (4–5 standart)
1. **Hayatta kal:** "X süre dayan / Tier 5'e ulaş"
2. **Avlanma:** "X tipi düşman/consumable Y kez ye"
3. **Build:** "Tek run'da X upgrade kombine et"
4. **Bölge:** "Şu bölgeyi bitir / şu bölgede zarar almadan run yap"
5. **Gizli:** unlock'ların %10–20'si sürpriz (örn. "şu objeyi kır")

### Şeffaflık kuralı
- Unlock'ların **%80'i görünür** olmalı: ana menüde "Achievements / Unlocks" sekmesi, kilitli olanlar bile koşuluyla listelenir.
- %20 gizli sürpriz olabilir.
- "Ne için çabaladığımı bilmiyorum" hissi = motivasyon ölümü.

### MVP unlock hedefi
- 3 blob karakteri (başlangıç + 2 unlock)
- 8 upgrade kartı (Phase 5 listesi)
- 2 bölge (Şehir Merkezi başlangıç + 1 unlock)
- 3–5 kalıcı stat upgrade slotu

---

## 16. Riskler & Açık Sorular

### Bilinen riskler
- **Unity sahne merge çakışmaları** — 3 kişi aynı sahneye dokunamaz. Mitigation: Canvas'ı küçük prefab'lara böl, sahne sahipliği branş bazlı.
- **Mobil performans** — 80 consumable + 20 enemy + particle'lar 60fps tutar mı? Mitigation: erken cihaz testi, Pool sistemi zaten var.
- **Asset/dev hız dengesizliği** — Hüma'nın art üretimi dev'in önüne geçer mi geride mi kalır? Mitigation: weekly sync.
- **Scope kayması** — meta-progression + 4 bölge + 20 upgrade hızla şişer. Mitigation: MVP tanımına sadık kal.

### Açık sorular (karar bekleyen)
- 🔲 Oyun adı kesin mi (Zorse vs Blob Survivor vs ?)
- 🔲 Platform önceliği: mobil mi PC mi?
- 🔲 Upgrade kart sayısı: 3 mü 4 mü?
- 🔲 Re-roll mekaniği var mı?
- 🔲 Meta-progression para birimi ne (altın / mass / başka)?
- 🔲 Bölgeler arası geçiş: serbest mi kilitli mi?
- 🔲 Boss tasarımı: her tier'da mi, sadece final mi, ikisi mi?
- 🔲 Hikaye tonu / blob kimliği

> Cevaplananlar silinmez, **✅ Karar:** notuyla altına yazılır.

---

## 17. Yol Haritası

### Phase durumu (CLAUDE.md ile senkron)
- ✅ Phase 1 — Core altyapı
- ✅ Phase 2 — Blob
- ✅ Phase 3 — Consumables
- ✅ Phase 4 — Düşman sistemi
- 🟡 Phase 5 — Upgrade sistemi (sırada)
- 🔲 Phase 6 — HUD & UI
- 🔲 Phase 7 — Harita & Bölgeler
- 🔲 Phase 8 — Cila & Juice
- 🔲 Phase 9 (yeni) — Meta-progression & Unlock sistemi

### MVP tanımı
**MVP = oynanabilir + bir kez bitirilebilir + tekrar oynanabilir.**

Dahil:
- Phase 1–6 tamamı
- 1 bölge (Şehir Merkezi)
- 5 tier + tier-5 final boss
- 8 upgrade kartı
- 3 düşman tipi
- Game Over + skor + highscore
- **Minimum meta-progression:** 3 kalıcı stat + 1 unlock'lu karakter

Hariç (post-MVP):
- Phase 7 ek bölgeler
- Phase 8 cila
- Tam unlock ağacı
- Evolution sistemi
- Cosmetic'ler

### Milestone tahminleri
_(TBD — phase başına ~2 hafta varsayımıyla doldurulacak)_
