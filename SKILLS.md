# Left No Crumbs: Skill, Karakter ve Boss Rehberi

> Skill, karakter, düşman ve boss'ların **güncel** değerleri. Değerler koddan ve asset'lerden okunur (`Assets/_Project/Data/Upgrades/`, `Data/Characters/`, `ScriptableObjects/Enemies/`); değişince burası da güncellenir, eski hali yazılmaz.
> Skill sayıları `Systems/Upgrade/Effects/*Effect.cs` içindeki seviye tablolarında (`_...ByLevel`, Sv.1 → Sv.5). Kart metinleri, tür, ağırlık ve kart havuzu `Tools → Blob.io → Skill Setini Kur` (`Editor/SkillSetSetup.cs`) ile kurulur; metin değişince menüyü tekrar çalıştır.
> Oyunun genel hali `OYUN.md`'de; açık denge soruları GitHub issue'larında.

## 1. Genel kurallar

**Seviye atlama.** Yediğin her şeyin mass'ı aynı zamanda XP'dir. Bir sonraki level için gereken XP `20 + level × 15 + level² × 2 + max(0, level − 4)² × 12`: 1. level 20 XP, 2. level 37, 3. level 58, … 5. level 112, 8. level 331, 10. level 617, 11. level 802 (Giant bina ~17 XP, Dev bina ~28 XP). Karesel terimler geç oyunda (bina = 25–50 XP) seviye atlamanın çok hızlanmasını önler; son terim ilk 5 seviyeyi değiştirmez.

**Kart ekranı.** Level atlayınca oyun durur ve 3 kart gelir. Biri seçilir.
- **Slot:** Aynı anda en fazla **3 aktif + 3 pasif** skill (`UpgradeSystem._activeSlots` / `_passiveSlots`). Slot run boyunca kalıcı, skill bırakılamaz. Bir türün slotları dolunca o türden yeni skill çıkmaz, sadece elindekilerin seviyeleri gelir. **Nadir** kartlar ve **evrim** kartları slot harcamaz.
- **Seviye:** Her skill **en fazla 5 seviye**; çoğu seviye bir sayı artırır, ⭐ seviyeleri yeni bir yetenek açar. Max'e ulaşan skill havuzdan çıkar.
- Kartlar ağırlıklı rastgele çekilir: her skill ağırlık **10**, nadir Bölünme **3**.
- **Karakterin favori skill'i ×3 ağırlıkla** çekilir (ağırlığı 10 olan skill kart ekranlarının ~%41'i yerine ~%75'inde çıkar). `CharacterData._favoredUpgrade` / `_favoredWeightMultiplier`.
- **Teklif edilecek kart kalmazsa** (her şey max ya da slotlar dolu ve elindekiler max) level atlamak kart açmaz, **canın %30'u dolar** (`UpgradeSystem._allMaxHealFraction`).
- Kartın sağ üstünde **YENİ** (yeşil, ilk kez alıyorsun) ya da **mavi Sv. 2/5** (sahip olduğun skill'in şu anki seviyesi) yazar; altında tür etiketi (**AKTİF** turuncu / **PASİF** mavi / **NADİR** mor); açıklama bu seçimle gelecek seviyeyi anlatır. Sopa herkeste baştan olduğu için Sv.0'dayken de YENİ değil "Sv. 0/5" yazar.
- Tek lokmada birden fazla level atlarsan kart ekranları arka arkaya gelir, seviye kaybolmaz.
- Elit sandığı da bir kart ekranı açar.
- **Garanti kart yok.**
- **Başlangıç skill'i:** `CharacterData._startingUpgrade` + seviye: run o skill o seviyede başlar (slot da kaplar). Bkz. §4.

**Run sonu.** Skiller run'a özeldir. Ölüm ya da restart hepsini sıfırlar (`GameManager.ClearPreviousRunState`).

**Kalıcı bonuslarla etkileşim.** Market'ten alınan kalıcı statlar her run başında uygulanır, skiller bunların üstüne eklenir: Hız en fazla +%25, Max Can +50, Kütle kazancı +%25 (XP'yi de artırır), Coin kazancı +%25 (her biri 5 seviye), XP çarpanı +%25 (tek sefer). Fiyatlar ve denge hedefleri `OYUN.md` §9.

**Blob boyutu referansı** (örneklerde kullanılıyor, sahne değerleri):

| Tier | Gereken mass | Görsel ölçek | Gövde yarıçapı |
|---|---|---|---|
| Tiny | 0 | 0.5 | 0.25 |
| Little | 15 | ~0.76 | ~0.38 |
| Small | 50 | ~1.1 | ~0.55 |
| Medium | 200 | ~2.0 | ~1.0 |
| Large | 650 | ~3.3 | ~1.65 |
| Giant | 1100 | ~4.1 | ~2.05 |
| Dev (`Colossal`) | 1650 | ~5.0 | ~2.5 |

---

## 2. Skiller (16 adet)

**Havuz:** 7 aktif (Vakum, Kaygan Zemin, Sopa, Havai Fişek, Yer Sarsıntısı, Sindirim Gazı, Zırh) · 8 pasif (Sindirim, Geri Dönüşüm, Hızlı Metabolizma, Hız, Tetikçi, Can Artışı, Rejenerasyon, Yara Öfkesi) · 1 nadir (Bölünme). Kategoriler: Sindirim ve Büyüme · Hareket · Saldırı · Savunma · Nadir.

Tablolarda her satır Sv.1 → Sv.5; ⭐ yeni yetenek açan seviye.

### Sindirim ve Büyüme

#### 🍖 Sindirim (pasif)
- **Ne yapar:** Yediğin her şeyden kazandığın mass'ı, dolayısıyla XP'yi artırır.
- **Kod:** `SindirimEffect` → `BlobGrowth._massGainMultiplier`, ⭐'lar `BlobSkills` (`BlobConsumption.SkillMass`).

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Kütle +%10 | +%20 | ⭐ **Ağır Lokma:** kendi tier'ındaki lokmalar +%25 | +%30 | ⭐ **Obur:** her 10. lokma ×3 kütle |

#### 🌀 Vakum (aktif)
- **Ne yapar:** Yiyebileceğin tier'daki objeleri kendine çeker. Coin, yaya, polis, bina ve hazard'ları çekmez (Sv.5'e kadar coin ve kalbi de).
- **Menzil gövde yüzeyinden ölçülür:** blob büyüdükçe çekme alanı da büyür.
- **Kod:** `VacuumEffect` → `VacuumComponent` (0.15 sn'de bir tarama).
- **Görsel:** çekim alanı boyunca zeminde dönen açık mavi kasırga (`Tools → Blob.io → Vakum Efektini Kur`, `Resources/FX/Vacuum`).

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Menzil 1.6 | 2.0 | ⭐ **Geniş Ağız:** yutma alanı ×1.3 | 2.8, çekme hızı ×1.5 | ⭐ Coin ve kalpleri de çeker |

#### ♻️ Geri Dönüşüm (pasif)
- **Ne yapar:** Cansız nesne (çöp kutusu, eşya, araba) yedikçe can yenilenir. Yayalar sayılmaz.
- **Kod:** `RecycleEffect` → `BlobSkills`.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Lokma başına +1 can | +2 | ⭐ **Hurdacı:** araba yutunca +25 can | +3 | ⭐ Can doluyken fazlası 20'lik geçici kalkan olur |

#### ⚡ Hızlı Metabolizma (pasif)
- **Ne yapar:** Büyüdükçe gelen yavaşlamayı azaltır, tamamen kaldırmaz (ilke: büyüdükçe yavaşla). Tier yavaşlama üssünü oranla küçültür (`BlobController.SlowdownReduction`, en fazla %90).
- **Kod:** `MetabolismEffect` → `BlobSkills`.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Yavaşlama −%15 | −%30 | ⭐ Tier atlayınca 3 sn +%30 hız | −%45 | ⭐ −%60, büyük lokmadan sonra 1 sn +%20 hız |

### Hareket

#### 💨 Hız (pasif)
- **Ne yapar:** Hız çarpanını artırır.
- **Formül:** `hız = 5 × çarpan / sıra^üs` (sıra: Tiny 1 … Giant 5, Dev 6). Üs sahnede `BlobController._tierSlowdownExponent`; Hızlı Metabolizma bunu küçültür.
- **Kod:** `SpeedBoostEffect` → `BlobController.SetSpeedMultiplier`, roket `BlobSkills`.
- **Görsel (bütün karakterlerde aynı):** **Sv.1-2** kaykay → **Sv.3-4** alevli kaykay → **Sv.5** alevli kaykay + **popoda roket** (alevi hıza göre uzar). Modeller `zorse-blender/build_speed.py` → `Resources/FX/Speed`; `SpeedVisual`. Topik'in rüzgar izi bundan ayrı, baştan var.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Hız +%20, kaykay | +%30 | ⭐ Alevli kaykay, +%38 | +%45 | ⭐ **Roket:** 2 sn durmadan gidince +%20 |

#### 🧼 Kaygan Zemin (aktif)
- **Ne yapar:** Arkanda turkuaz sabunlu su izi bırakırsın; izdeki düşmanlar yavaşlar.
- **Kod:** `SlipTrailEffect` → `SlipTrail` (0.25 sn'de bir nokta, 24'lük halka; 0.2 sn'de bir tarama). Görsel: havuzlu `FX_Slime` lekeleri (`Resources/FX/Slime`, Blender `build_skill_fx.py`); blob üstündeyken leke gizlenir.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| İz 2 sn, izdekiler −%30 | İz 3 sn | ⭐ Robot köpekler izde kayar (0.5 sn) | −%50 | ⭐ **Asit iz:** yutamadıklarına sn'de 5 hasar |

### Saldırı

#### 🪵 Sopa (aktif)
- **Ne yapar:** Ana silah, bütün karakterlerde baştan var (Sv.0): saniyede **1 vuruş × 10 hasar**, yüzün baktığı yöne **120°'lik yay**, menzildeki yaydaki **herkese** vurur (hafif geri itme, bina arkasındakine vurmaz). Menzil **gövde yüzeyinden 2**. Kimse yoksa sallamaz. Kartı alınca 1 aktif slot kaplar.
- **Kod:** `WeaponUpgradeEffect` (kilometre taşı seviyeleri; +hasar `Upgrade_Weapon.asset` → Per Level Value 5) → `StickWeapon` (`Prefabs/Weapons/StickWeapon.prefab`, `GameManager._stickWeaponPrefab`).
- **Görsel:** ağaç dalı (`G_Stick_1..4` ← `zorse-blender/build_stick.py`), sadece vururken görünür, yay boyunca savrulur, arkasında beyaz hilal izi (`SwingArcFx`). Form: **Sv.0-1** yapraklı dal → **Sv.2-3** çift uçlu topuzlu sopa → **Sv.4** çivili → **Sv.5** altın bantlı, çiçekli.

| Sv. | Kazanılan | Vuruş hasarı | Sn. başı (hedef başına) |
|---|---|---|---|
| 0 | – | 10 | 10 |
| 1 | +5 hasar, +0.5 menzil | 15 | 15 |
| 2 | ⭐ **Yay 180°** | 15 | 15 |
| 3 | ⭐ **Arkaya da vurur**, +5 hasar, +0.5 menzil | 20 | 20 |
| 4 | ⭐ **Vuruş hızı ×1.3** | 20 | 26 |
| 5 | ⭐ **Son form: tam tur (360°)**, hasar ×1.5 | 30 | 39 |

#### 🎆 Havai Fişek (aktif)
- **Ne yapar:** İkinci, otomatik silah. Uzun menzilli, yavaş, tek hedefe ağır roket; **her yöne** ateş eder, hedef önceliği **boss > elit > en yakın**. Havuzdaki tek uzaktan boss indiren silah.
- **Temel:** 2 sn'de 1 atış × 25 hasar, menzil gövde yüzeyinden 8. Roket hızı 10, ömrü 1.6 sn (uçuşu görünsün diye yavaş).
- **Kod:** `FireworkEffect` (`FX_Firework.asset`) → `FireworkWeapon` (`WeaponBase` alt sınıfı). Mermi `Prefabs/Weapons/FireworkRocket.prefab`, efektler `FireworkBurstFx`.
- **Görünür atar:** sol omuzda (sol = saldırı, sağ = kalkan); form **Sv.1-2** tek tüp → **Sv.3** kalın tüp → **Sv.4** trafik konisi nişangahı → **Sv.5** çift tüp, altın bant (`build_firework.py`).

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| 2 sn'de 1 roket, 25 hasar | +8 hasar | ⭐ Atış hızı ×1.3 | ⭐ Boss ve elite ×1.5, +8 hasar | ⭐ **Son form:** her atışta 2 roket |

#### 🌋 Yer Sarsıntısı (aktif) ⚠️ deneme
- **Ne yapar:** **Darbe aldıkça dolar:** her X hasarda (kalkanın yuttuğu dahil) yere vurur; çevredeki düşmanlara alan hasarı + geri itme. Alan gövde yüzeyinden ölçülür, blob büyüdükçe büyür. Duvar arkasındakine vurmaz. İki sarsıntı arası en az 1.5 sn. Tetikçi gereken hasarı azaltır. Boss'lar sersemlemez.
- **Durum:** Skill'in kendisinden emin değiliz; `skill-deneme`'de oynanıp kalsın / değişsin / çıksın kararı verilecek.
- **Kod:** `GroundQuakeEffect` → `GroundQuake` (`BlobHealth.OnDamaged` + `OnShieldHit`). Görsel: beyaz şok dalgası (`SwingArcFx`) + yerden yükselip batan kırık asfalt halkası (`QuakeRingFx`, `Resources/FX/Quake`).

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Her 15 alınan hasarda 15 hasar, alan 2.5 | 22 hasar | ⭐ 0.5 sn sersemletme | Her 12 hasarda, alan 3 | ⭐ **Deprem:** her 3. sarsıntı 3× hasar, 2× alan |

#### ☁️ Sindirim Gazı (aktif)
- **Ne yapar:** Blob çevresinde hep açık zehirli alan; alandaki **tüm** düşmanlara vurur (yiyebildiklerin ve boss'lar dahil). Alan gövde yüzeyinden ölçülür.
- **Kod:** `DigestGasEffect` → `DigestGas` (0.25 sn'de bir). Görsel: alanın kenarında dönen yeşil kenar halkalı gaz bulutları (`Resources/FX/Gas`); içi açık kalır.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Sn'de 4 hasar, alan 1 | Sn'de 6 | ⭐ Gazdakiler %20 yavaşlar | Sn'de 8, alan 1.5 | ⭐ Gazda ölenin kütlesinin yarısı sana |

#### 🎯 Tetikçi (pasif)
- **Ne yapar:** Bekleme süreli aktif skill'ler daha sık çalışır: Sopa ve Havai Fişek daha sık vurur, Yer Sarsıntısı daha az hasarla dolar.
- **Kod:** `TriggerFingerEffect` → `BlobSkills.CooldownScale` / `RepeatEvery` / `KillCooldownRefund`; silahlar `BlobSkills.NextCooldown` ile uygular.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Bekleme −%8 | −%16 | ⭐ Her 5. atış hemen tekrarlanır | −%24 | ⭐ Her öldürmede bekleme −0.2 sn |

### Savunma

#### 🛡️ Zırh (aktif)
- **Ne yapar:** Hasarı önce emen, yenilenen kalkan. Gelen her hasarı (polis, mermi, araba, hazard, ısırık) önce kalkan yer. **Hasar vermez** (diken yok). Kalkanın yuttuğu hasarda kırmızı parlama olmaz, kalkan parlar.
- **Yenilenme:** son hasardan bir süre sonra kalkan dolar; seviye atlayınca hemen dolar.
- **Kod:** `ArmorEffect` (asset `FX_DamageReduction`) → `BlobHealth.SetShield`, görsel `ArmorShield`.
- **HUD:** can barının altında ince mavi kalkan barı.
- **Görsel:** blob'un sağ yanında süzülen sokak eşyası kalkan; form **Sv.1-2** çöp kutusu kapağı → **Sv.3** tabela → **Sv.4** menhol kapağı → **Sv.5** altın menhol kapağı (`build_armor_shields.py`, `Resources/FX/Shield`).

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Kalkan 20, 4 sn sonra 2 sn'de dolar | 35, 3 sn sonra | 50, ⭐ kırılınca 1 sn dokunulmazlık | 65, 1 sn'de dolar | 80, ⭐ **İkinci katman:** kırılınca bir kez anında dolar (30 sn'de bir) |

#### ❤️ Can Artışı (pasif)
- **Ne yapar:** Max canı artırır, eklenen miktar kadar da iyileştirir. ⭐ seviyeleri kalplerden gelen canı artırır (`BlobSkills.HeartMultiplier`).
- **Kod:** `HealthBoostEffect` → `BlobHealth.IncreaseMaxHealth`.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Max can +15 | +30 | ⭐ Kalpler %50 fazla can | +45 | ⭐ +60, kalpler 2 kat |

#### 💚 Rejenerasyon (pasif)
- **Ne yapar:** Kendiliğinden can yeniler.
- **Kod:** `RegenBoostEffect` (asset/id eski adıyla `regen_boost`) → `BlobSkills`.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Sn'de 0.5 can | 1 | ⭐ 3 sn hasar almazsan 2 katı | 1.5 | ⭐ 2, tier atlayınca canın %20'si dolar |

#### 😤 Yara Öfkesi (pasif)
- **Ne yapar:** Can eşiğin altındayken hız ve silah hasarı artar (Sopa, Havai Fişek, Gaz, Sarsıntı; `BlobSkills.WeaponDamageScale`). Risk: canı bilerek düşük tutmaya iter.
- **Kod:** `RageEffect` → `BlobSkills`.

| Sv.1 | Sv.2 | Sv.3 | Sv.4 | Sv.5 |
|---|---|---|---|---|
| Can <%40: hız +%15 | + hasar %15 | ⭐ Eşik %50 | Hız +%25, hasar +%25 | ⭐ Öfkeliyken ısırık hasarı almazsın |

### Nadir

#### 🧬 Bölünme (nadir, tek seviye)
- **Ne yapar:** Ölümcül darbede ölmek yerine bölünürsün: kütlenin %30'u 8 jöle damlası olarak etrafa saçılır, en az bir tier düşersin, canın %30'u dolar, 2 sn dokunulmazsın. Damlalar 10 sn yerde kalır (son 2 sn yanıp söner); üstlerinden geçince paylarını geri alırsın. Run'da bir kez. Slot harcamaz, ağırlık 3.
- **Kod:** `SplitEffect` → `BlobSkills.EnableSplit` / `BlobHealth.SaveFromDeath`, damlalar `SplitDrops` (`Resources/FX/Split`).

---

### 🚓 Polis yutma (skill değil, genel kural)
Robot köpek **Little**, polis **Medium**, elit polis **Giant** blob ister.

**Isırık bedeli:** Polis yutulurken ısırır: tier tam eşikteyse 5, 1 tier üstteyse 2.5, 2+ tier üstteyse 0 hasar (robot köpek 2 / 1 / 0). Isırık normal hasar gibi kırmızı parlama + kamera sarsıntısı + titreşim verir. Yara Öfkesi Sv.5 öfkeliyken ısırığı kaldırır.

**Kaçmazlar:** Düşmanlar blob onları yiyebilecek boyda olsa da kaçmaz, saldırmaya devam eder.

### 🦶 Yaya tekmesi (skill değil, genel kural)
Henüz yiyemediğin (senden büyük) yayaya çarparsan yaya seni tekmeler: geriye yaslanıp öne savrulur; blob **seke seke yuvarlanarak** savrulur (3 sekme, her biri bir öncekinin yarısı), tepesinde **sersemleme yıldızları** döner, tekme anında kısa **ağır çekim** (×0.4) ve **kamera yakınlaşması** olur. Can gitmez. Yaya bir tier büyükse ~5 birim / 2 takla, iki tier büyükse ~7.5 birim / 3 takla. Aynı anda üst üste gelmez (0.6 sn). `BlobController` (değerler dosyanın başında), `BlobVisual.Tumble`, `DizzyStars`, `PedestrianController.PlayKick`, `CameraController.PunchZoom`.

---

## 3. Evrim (1 adet)

Evrim kartı, iki girdi skilinin ikisi de **max seviyeye** ulaşınca havuza girer. Kart ★ ve altın renkle işaretlenir, slot harcamaz. Max seviyesi 1'dir: bir kez seçilince havuzdan çıkar. Ağırlık 10.

### ★ Kara Delik
- **Gereken:** Vakum 5 + Sindirim 5 (toplam 10 seçim)
- **Ne yapar:** Blob'un çevresindeki, **yiyebildiğin tier'daki her şey** temas gerekmeden otomatik yutulur: consumable, yaya, şehir objesi. Hazard'ları yemez.
- **Yarıçap:** gövde yarıçapının 2 katı; blob büyüdükçe büyür. 0.1 sn'de bir tarar.
- **Kod:** `KaraDelikEffect` → `BlackHoleComponent`
- **Evrim anı** (kart seçilince ~1.8 sn): zaman yavaşlar (×0.25), açık mavi kasırga hızlanıp blob'a çöker → indigo şok dalgası, kamera sarsıntısı, güçlü titreşim, blob şişip yutkunur; kasırga siyah-lacivert olarak geri açılır, olay ufku taşarak belirir; zaman ve kamera normale döner. Ekranın üstünde altın **★ KARA DELİK** yazısı.
- **Kalıcı görünüm:** vakum kasırgası siyah + lacivert, 1.6 kat hızlı döner; blob'un altında yutma yarıçapında siyah disk ve kenarında dönen indigo yığılma halkası.

| Blob | Gövde yarıçapı | Kara Delik yarıçapı |
|---|---|---|
| Medium | ~1.0 | ~2.0 |
| Giant | ~2.0 | ~4.0 |

**Not:** Polisleri yutmaz; onlar için temas gerekir.

---

## 4. Karakterler (5 adet)

**Açılma koşulları** (Market → **KARAKTERLER** sekmesi; Topik orada yok, baştan açık):
- **Topik:** baştan açık.
- **Mıknato:** Market'te **500 coin** (2.–3. run).
- **Fişek:** Market'te **1.000 coin** (~5. run).
- **Cano:** Market'te **1.500 coin** (~7. run).
- **Dalcık:** satılmaz, **5 run bitirince** kendiliğinden açılır. Ölümle biten run da, kazanılan run da sayılır. Market'teki kartında ilerleme yazar ("3/5 run"). Sayı kalıcı kayıtta tutulur (`MetaProgression.CompletedRuns`), koşul `Char_Dalcik.asset` → `Required Completed Runs`.
- Kasaya run sonunda toplanan coin girer: kazanınca %100'ü, ölünce %50'si. Satın alınan karakter kalıcıdır (`CharacterData._saveId` kayda yazılır, `_marketPrice` > 0 = Market'te satılır).
- Kilitli karakter Lobby'de seçilemez; isminin altında koşul yazar: "Market'te açılır" ya da "2 run sonra açılır".

**Neden Dalcık run sayısıyla:** coin'le alınan üç karakter "biriktir ve seç" hedefi; Dalcık "oynamaya devam et" hedefi. Başarılı run şartı yeni oyuncu için uzak ve en çok yardıma ihtiyacı olanı (kazanamayanı) cezalandırır; 5 run her oyuncuya sayılabilir, görünür bir hedef verir ve Mıknato ile Fişek'in arasına düşer.

Beş karakter de aynı blob'dur; farkları **görünüm** ve **başlangıç avantajı** (kendi skill'iyle başlar; o skill favori skili, kart ekranında ×3 ağırlık). Başlangıç skill'i slot kaplar. Ana silah hepsinde aynı: sopa.

| | 🟠 Topik | 🧲 Mıknato | 🎆 Fişek | 🌿 Dalcık | ♻️ Cano |
|---|---|---|---|---|---|
| **Kilit** | Baştan açık | Market **500** | Market **1.000** | **5 run bitirince** | Market **1.500** |
| **Başlangıç avantajı** | **Hız Sv.2** (pasif) | **Vakum Sv.2** (aktif) | **Havai Fişek Sv.1** (aktif) | **Sopa Sv.2** (aktif) | **Geri Dönüşüm Sv.2** (pasif) |
| **Favori skill (×3)** | Hız | Vakum | Havai Fişek | Sopa | Geri Dönüşüm |
| **Görünüm** | Turuncu → kırmızı | Mavi, tepede mıknatıs | Korsan bomba: siyah, göz bandı, tepede fitil | Fıstık yeşili, tepede minik dal | Ahududu kırmızısı, tepede kalp |
| **Oyun tarzı** | Baştan hızlı; kaçarak büyür | Toplayıcı; yemi kendine çeker | Uzaktan vurur, boss'u erken indirir | Yakın dövüşçü; sürünün içine dalar | Dayanıklı; yedikçe iyileşir |

### 🟠 Topik
- **Pasif:** Run'a **Hız Sv.2** ile başlar (+%30, kaykay baştan görünür). Hız favori skili (×3). `Char_Topik.asset` → `Starting Upgrade` / `Starting Upgrade Level`. Rüzgar izi (`SpeedTrail`) sadece görsel.
- **Görünüm:** turuncu → kırmızı; hızlı giderken arkasında rüzgar izi.

### 🧲 Mıknato
- **Pasif:** Run'a **Vakum Sv.2** ile başlar (menzil gövde yüzeyinden 2). Vakum favori skili (×3). `Char_Miknato.asset`.
- **Görünüm:** mavi, tepede kırmızı at nalı mıknatıs.
- **Örnek:** Diğerleri Vakum'u kart olarak beklerken Mıknato ilk saniyeden çevresini toplar: erken büyüme, erken level. Kara Delik evrimine (Vakum 5 + Sindirim 5) en doğal giden karakter.

### 🎆 Fişek
- **Pasif:** Run'a **Havai Fişek Sv.1** ile başlar: atar baştan sol omzunda, 2 sn'de bir 25 hasarlık roket. Havai Fişek favori skili (×3). `Char_Fisek.asset`.
- **Görünüm:** korsan bomba: siyah gövde, sol gözde kırmızı göz bandı, tepede metal ağızlık ve yanan fitil (`build_blobs.py`; model adları hâlâ `CH_Mermo_*`). Kalkanı diğerleri gibi sağ yanda.

### 🌿 Dalcık
- **Pasif:** Run'a **Sopa Sv.2** ile başlar: sopa ilk saniyeden 15 hasar, menzil 2.5 ve **180° yayla** vurur. Sopa favori skili (×3); kilometre taşlarına (Sv.3 çift yön, Sv.5 tam tur) en hızlı giden karakter. `Char_Dalcik.asset`.
- **Görünüm:** fıstık yeşili, tepede minicik kısa dal (`build_blobs.py` → `CH_Dalcik_T1..T5`, portre `Icon_Dalcik.png`).

### ♻️ Cano
- **Pasif:** Run'a **Geri Dönüşüm Sv.2** ile başlar: yediği her cansız nesne +2 can. Geri Dönüşüm favori skili (×3); Hurdacı (Sv.3, araba +25 can) ve taşan canın kalkana dönmesi (Sv.5) en hızlı ona gelir. `Char_Cano.asset`.
- **Görünüm:** ahududu kırmızısı, tepede kısa sap ucunda pembe kalp; T1-T3'te ağız köşesinde minik vampir dişi. `build_blobs.py` → `CH_Cano_T1..T5`, portre `Icon_Cano.png`.

---

## 5. Düşmanlar ve Bosslar

### Normal düşmanlar (karşılaştırma için)
| | Robot köpek | Polis | Elit Polis |
|---|---|---|---|
| **Ne zaman** | 1. dk'dan itibaren, **3'lü sürü** | 1. dk'dan itibaren | Blob **Medium** olunca (en geç 4. dk) |
| **Sayı** | Ayrı tavan: 8 + seviye × 1 (en fazla 18) | Polis + elit: 8 + seviye × 2.2 (en fazla 30) | Polis tavanı içinde, en fazla 8 |
| **Can** | 10 | 50 | 250 |
| **Saldırı** | Yakın dövüş, 4 hasar, saniyede 1 | Yakın dövüş, 10 hasar, saniyede 1 | Tabanca: 0.5 sn nişan lazeri → **3'lü seri**, mermi başına 10 hasar, menzil 7.5. Arada bina varsa ateş etmez |
| **Hız** | 3.4 | 2.3 | 2.1 |
| **Görme** | 40 (doğduğu andan koşar) | 15 | 23 |
| **Yutulma** | Little blob | Medium blob | Giant blob |
| **Davranış** | Kaçmaz | Kaçmaz | Kaçmaz |
| **Ödül** | 1.5 mass, 1 coin, %2 kalp | 3 mass, 2 coin, %5 kalp | 20 mass, 5–10 coin, **sandık** (35 coin + ekstra kart), %100 kalp |

### 👹 Miniboss: "Polis Tankı"
Aynı tasarım, iki kez gelir, ikincisi daha güçlü. Elit polisin nişan-kilit-ateş akışını top mermisiyle kullanır; üstünden geçtiği arabaları ve sokak eşyalarını ezer.

| | 1. Miniboss | 2. Miniboss |
|---|---|---|
| **Geliş** | **5. seviye** (en geç 9. dk) | **8. seviye** (en geç 15. dk; ilki hâlâ yaşıyorsa o ölene kadar bekler) |
| **Can** | 600 | 1200 |
| **Saldırı** | Tank topu: 1.1 sn nişan lazeri (son 0.4 sn kilit) → **1 mermi × 25 hasar**, sonra 2.5 sn bekleme. Menzil 8, mermi hızı 9 | 0.9 sn nişan (son 0.35 sn kilit) → **2 mermi × 20 = 40 hasar** (0.45 sn arayla), 2.2 sn bekleme. Menzil 8, mermi hızı 10 |
| **Hız** | 2.9. Ekran dışında ve 25 birimden uzaktaysa **×2** (yetişme), ekrana girince normale döner | 3.2, yetişme aynı |
| **Algı** | Tüm harita: oyuncuyu her yerden bulur | Tüm harita |
| **Yutulma** | **Hiçbir şekilde yutulamaz**, silahla ölür | Aynı |
| **Ödül** | 150 skor + 5–10 coin + sandık (35 coin + ekstra kart) + **15–25 bonus coin** | 250 skor + 5–10 coin + sandık (35 coin + ekstra kart) + **25–40 bonus coin** |

- **Silahla öldürme süresi:** 1. Miniboss: temel silahla ~60 sn, + Havai Fişek Sv.1 ~27 sn, Sopa 5 ile (sn'de 39) ~15 sn. 2. Miniboss: ~120 sn / Havai Fişek Sv.1 ~53 sn / Sopa 5 ile ~31 sn. (Temel silahın menzili yüzeyden 3 olduğu için tanka yaklaşmak gerekir.)
- **Ekranda:** Boss can barı HUD'da görünür.
- **Görsel:** Polis tankı modeli, **2.5 kat büyük** (en dar sokağa sığan en büyük boy) (`CityKitPipeline.MinibossScale`). Ateş edince geri teper.
- **Kaçış:** Kilit anında (lazer kalınlaşınca) yana kaçan oyuncu mermiyi atlatır. Arada bina varsa ateş etmez, yürür. Menzil 8 (tankın merkezinden): sopadan çok uzun; Havai Fişek'le (gövde yüzeyinden 8, yani merkezden 8 + gövde yarıçapı) aşağı yukarı eşit. Blob büyüdükçe Havai Fişek tankın menzilinin hemen dışından vurabilir.
- **Ölçek:** 1. miniboss geldiğinde can ~120-150 iken tek isabet canın ~1/5'i; 5-6 isabet öldürür.

### 🚁 Final Boss: "Polis Helikopteri"
**"Vur, alçalt, büyü, yut."** Helikopter havada uçar; vurdukça alçalır, sen de ona uzanacak kadar büyüyüp yutarsın.

| | Final Boss |
|---|---|
| **Geliş** | **10. seviye** (en geç 21. dk). Bir miniboss yaşıyorsa beklemez, miniboss çekilir |
| **Can** | 9000. **Silahla ölmez** (can 1'in altına inmez) |
| **İrtifa** (kızakların yerden yüksekliği) | Başta **8** → can %75'te **7** → %50'de **6** → %25'te **5**. Yumuşak iner, geri yükselmez |
| **Saldırı** | Mini roket serisi, yukarıdan: 1 sn nişan lazeri (son 0.4 sn kilit) → **6 roket × 6 hasar** (0.12 sn arayla), 2 sn bekleme. Menzil 10 (yatay). Arada bina varsa ateş etmez. Roketler blob'a, yere ya da duvara değince patlar: blob yüzeyi patlamaya **1.2 birim** yakınsa hasar alır (ıskalayıp yanına düşen de vurur). Mermi: `HeliRocketBullet` (Helikopter Mermi Roketini Kur) |
| **Roket (alan)** | Her **7 sn**'de (ilk 6. sn'de) blob'un gideceği yere **kırmızı daire**; halka atar, iç disk dolar, **1.6 sn** sonra roket düşer: **20 hasar**. Can %75'in üstünde 1, %50'ye kadar 2, sonrası **3 roket** (ekler hedefin iki yanına, 0.3 sn arayla). Daire yarıçapı 1.8 + blob yarıçapı × 0.6; daireden çıkan (kenarı sıyıran) kurtulur. Menzil 16 (yatay) |
| **Hız** | 3.5. Ekran dışında ve uzaktaysa ×2 (yetişme) |
| **Algı** | Tüm harita |
| **Yutulma** | Blob'un tepesi helikoptere **değdiği an**, hangi irtifada olursa olsun. Çok büyüyen oyuncu daha erken bitirir |
| **Ödül** | Yutulunca **run "tamamlandı" olarak biter** (ölümden ayrı ekran), 300 mass, 600 skor, 5–10 coin + sandık (35) + **60–100 bonus coin**, coin'in tamamı kasaya (ölümde %50) |

- **Ne kadar büyümek gerekir** (blob tepesi ≈ 1.1 × ölçek): irtifa 5 ≈ **1340 mass** (Giant ile Dev arası; Dev olmak gerekmez), 6 ≈ 1950, 7 ≈ 2700, 8 ≈ 3600.
- **Silahlar yukarıyı da vurur:** Havai Fişek havadaki hedefe otomatik 3D nişan alır; sopa tam tepedeki hedefe de değer. Menzil yatay ölçülür; helikopterin altına yaklaşman yeter.
- **Kamera:** Helikopter gelince hızlıca uzaklaşıp ilk 3 sn ikinizi birden kadraja alır; sonra uzaklık kalır, kamera blob'u ortalar; boss ölünce normale döner.
- **Görsel:** Rotorlar döner, havada süzülür, gittiği yöne eğilir. Son irtifada rotor yavaşlar ("hasarlı"). Burnun altındaki projektör yere sarı ışık düşürür, yavaşça sağa sola tarar.
- **Zaman sınırı yok:** Run sadece ölümle ya da helikopter yutulunca biter.

### Zorluk zaman çizelgesi
Boss'lar süreye değil seviyeye bağlı (1. Miniboss Sv.5, 2. Miniboss Sv.8, Final Boss Sv.10; en geç 9 / 15 / 21. dk).

| Dakika | Olay |
|---|---|
| 0–1 | Sadece yenebilir objeler, yayalar, arabalar |
| 1 | Polisler ve robot köpek sürüleri gelmeye başlar (polis + elit seviyeyle 8 → 30, köpek ayrı 8 → 18) |
| **Medium** (en geç 4) | Elit polis dalgası (aynı anda en fazla 8 elit, polis tavanının içinde) |
| 5 | Düşman hasarı ×1.2 |
| 10 | Düşman spawn'ı ×2 hızlı (tavanlar aynı) |
| 15 | Düşman hızı ×1.15 |
| 20 | Düşman hasarı ×3 |
