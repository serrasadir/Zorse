---
name: denge
description: Left No Crumbs denge kontrolü — coin ekonomisi, Market fiyatları, XP/seviye eğrisi, skill kazanımı ve düşman ödülleri. "Denge", "ekonomi", "fiyat", "coin kazanımı", "skill oranı", "çok kolay/zor" denince ya da bu değerlerden biri değişmeden önce kullan.
---

# Denge kontrolü

Yöntem her oyunda aynı: **hedef yaz → değerleri modelle → gerçek run'ı ölç → hedefle karşılaştır → değeri değiştir.** Hedef değişmez, değer değişir (hedefi sadece Serra değiştirir).

## 1. Değerleri oku (tahmin etme)

```bash
python3 -I .claude/skills/denge/denge_tablosu.py            # Editor kaydı
python3 -I .claude/skills/denge/denge_tablosu.py <analytics.json>
```

Script değerleri sahneden (`GameScene.unity`, sahne kodun varsayılanını ezer), asset'lerden ve kod sabitlerinden okur. Yeni bir değer eklenince script'e de ekle.

| Değer | Nerede |
|---|---|
| Market fiyat/etki | GameScene → `MetaProgression` (Unity açıkken `Temp/menu.request`: `set MetaProgression _alan değer` + `save`) |
| Market sınırı | `MetaProgression.MaxStatLevel` |
| Polis coin'i, elit coin aralığı | `EnemyBase.PoliceCoin`, `EnemyBase.Die` |
| Sandık coin'i | `Prefabs/Chest.prefab` `_coinAmount` |
| Boss bonus coin | `ScriptableObjects/Enemies/BossData_Miniboss.asset` |
| XP eğrisi | GameScene → `BlobGrowth` (`_baseXPThreshold`, `_xpGrowthPerLevel`, `_xpQuadraticPerLevel`) |
| Skill ağırlık/max/seviye başı | `Data/Upgrades/Upgrade_*.asset` |
| Düşman can/skor/ödül | `ScriptableObjects/Enemies/EnemyData_*.asset` |
| Tasarımdaki sayılar | `OYUN.md` §9, `SKILLS.md` |

## 2. Hedefleri oku

`OYUN.md` → "Denge hedefleri" tablosu. Hedef yoksa önce hedef öner, Serra onaylasın; hedefsiz değer "iyi/kötü" diye yargılanmaz.

## 3. Ölç

Her run `analytics.json`'a yazılır (`RunAnalytics` → `SaveSystem`). Telefonda DEV paneli → COIN / KAYIT: son 10 ölçülen run'ın ortalaması. **Gerçek run verisi yoksa karşılaştırma bölümü "DEĞERLENDİRİLMEDİ" yazar** — tahmini sayıyı ölçülmüş gibi sunma. Model hesabı yapacaksan varsayımları (dakikada polis, run süresi) açıkça yaz. Editor'daki hile tuşlu kısa run'lar ölçüm değildir.

## 4. Kontrol listesi

**Ekonomi:** her coin kaynağının payı (tek kaynak %55'i geçmesin); kasaya giren/run; Market'in her kalemi kaç run; hiç almaya değmeyen kalem (aynı paraya başka kalem her açıdan daha iyi mi?); sınırsız biriken stat; yuvarlama yüzünden kaybolan bonus; yerde kaybolan coin oranı.

**İlerleme:** seviye başı XP ve toplam; boss'ların geldiği seviyede blob'un kütlesi (kütle = XP); run başına kart sayısı (seviye + sandık); evrimlere (Kara Delik: Vakum 8 + Sindirim 8) ölüm ve kazanma run'ında ulaşılabiliyor mu; ölü bölge (uzun süre kart yok) ve ani sıçrama.

**Savaş:** hasar/sn (silah, Havai Fişek, skill seviyelerine göre); polis/elit/boss öldürme süresi (`SKILLS.md` §5 süreleriyle tutuyor mu); her şeyi ezen tek skill var mı; ölümsüz kılan kombinasyon (Zırh + Can Emme).

## 5. Değiştir

- Bir seferde az değer değiştir; her değişikliğin hangi hedefe hizmet ettiğini yaz.
- `OYUN.md` / `SKILLS.md`'yi aynı commit'te güncelle (eski hali sil).
- Kayıtlı oyuncu verisini bozma: `MetaProgressionSaveData` alan adları değişmez.
- Sonuç tablosunu Serra'ya düz Türkçe, sayılarla ver; önce karar gerektirenler.
