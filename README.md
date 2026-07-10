# Blob.io

**Zorse Studio** · Roguelite / Bullet-Heaven / Survival · Unity 6 (URP) · Mobile-first (iOS/Android), PC secondary

> _"Survive. Level up. Devour the dark."_

This document is a complete introduction for someone seeing the game for the first time: what it is, who it's for, why it will make money, how the gameplay works, and where the project stands today. The **vision of the finished game** and the **current development status** are given separately and honestly.

> **Codename / naming note:** The project codename is _Zorse_; the working title is _Blob.io_. The final commercial name is not yet locked (see §18). Although GDD v1.0 lists Godot as the engine, the project is built on **Unity 6** — design goals come from the GDD, technical decisions from Unity.

---

## 1. One-Sentence Pitch

**Blob.io** is a one-handed roguelite bullet-heaven with 20–30 minute sessions: you control a blob, devour everything smaller than you to grow, get slower and stronger as you grow — then police waves and giant bosses arrive. **There is no attack button; movement is the only strategy.**

The formula: **Vampire Survivors'** upgrade chaos and meta-progression × the **Katamari Damacy / Hole.io** "devour everything" growth satisfaction — in a funny, gothic theme.

---

## 2. Vision — Why This Game?

### Core differentiator
The bullet-heaven genre (Vampire Survivors and its successors) is a proven, still-growing market. But nearly every title is built on the same core: a fixed-size character, auto-firing weapons. **Blob.io injects two proven mechanics into that genre:**

1. **Growth curve (Agar.io / Katamari):** The character physically grows throughout the run. Everything you eat makes you bigger, and the bigger you get, the bigger the things you can eat. This adds a concrete, visual sense of progression the genre lacks.
2. **Risk = reward tension:** Growing is powerful, but it has a cost — `speed = 1/√tier`. The more massive you get, the slower you move and the harder it is to escape. Power is never "comfort." This creates a constant decision pressure most games in the genre don't have.

### Three design pillars
1. **Smooth growth feel** — tier-ups aren't sudden jumps but a fluid curve (`Pow`-based scale formula).
2. **Grow bigger, move slower** — a constant trade-off between power and mobility.
3. **Every run is different** — upgrade combinations + meta unlocks guarantee replayability.

### The "zero attack button" philosophy
The player never fires. Weapons run automatically, bullet-heaven style. The only input is movement → **playable one-handed, on the bus, standing in line.** This is a critical accessibility and retention lever for the mobile target audience.

### Anti-vision (what this game is NOT)
- Not story-driven (environmental storytelling only).
- Not PvP.
- Not realistic — stylized, funny, absurd-gothic tone.
- Not an endless runner — every run closes with a boss or the apocalypse wave.

---

## 3. Target Audience & Market

| Segment | Profile | Why Blob.io? |
|---|---|---|
| **Core gamer** | 18–35, indie enthusiast | Build crafting, roguelite depth |
| **Casual mobile** | Likes short sessions | 20–30 min sessions, one-handed controls |
| **Vampire Survivors fan** | Already plays the genre | Familiar loop + new growth mechanic |
| **Retro / low-poly lover** | Seeks gothic-pixel aesthetics | Funny art direction, Castlevania texture |

**Market opportunity:** Bullet-heaven went mainstream with the Vampire Survivors breakout and is especially strong on mobile. It has a low barrier to entry (single input), high retention (short sessions + meta-progression), and a structure that fits ads/IAP naturally. Blob.io aims to be **instantly distinguishable — visually and mechanically — through its growth mechanic** in this increasingly crowded market. That's the hook that makes a storefront browser think "this one is different."

---

## 4. Core Gameplay Loop

The game consists of four nested loops:

### 30-second loop (moment-to-moment)
`Move → spot a consumable → check tier → eat → grow smoothly → flee danger`
Instant gratification: squash animation, sound, score pop-up, haptic feedback.

### 2–5 minute loop (mid-term)
`Accumulate mass → tier up → level up → pick 1 of 3 upgrade cards → eat bigger things with new power → boss spawns → devour boss → next tier`

### Run loop (20–30 min / one session)
`Pick character & map → journey through tiers 1→5 → enemy waves → boss waves → apocalypse wave at minute 25 → die or finish → score + credits → Market`

### Meta loop (between sessions — the motivation engine)
`Spend credits → permanent stat + unlock new character/map/weapon → the next run is more varied → "one more run"`

### Session structure (step by step)
1. Pick a character + map in the lobby.
2. Start in the arena — consumables spawn, you grow by eating.
3. Skill set opens — strategic choices.
4. Enemies spawn and chase you.
5. Kill enemies → coins drop; **elite** enemies drop a **chest** (skill + high coin).
6. On level-up the game pauses → 3 random upgrades → pick 1. Picking the same skill again levels it up.
7. At minute 25 the **Apocalypse Boss** activates.
8. The session ends → rewards are calculated → transferred to the Market.
9. Buy permanent unlocks from the meta shop, start again.

**Fail loop:** Death ends the session, but 50% of collected credits are permanently kept. **There is no "zero loss" — trying is always worth it.**

---

## 5. Core Mechanics

### Growth & Tier system
The blob grows continuously and smoothly with the `mass` it collects:

```
scale = baseScale × (1 + mass × growthFactor) ^ growthExponent
speed = 1 / √tier          // grow bigger, move slower
```

5 tiers: **Tiny → Small → Medium → Large → Giant.** Each tier unlocks the consumable size the blob can eat. The blob can only eat objects in a tier smaller than itself; colliding with something bigger is a hazard. This creates a natural "clear the small stuff first, then climb up" priority flow.

### Consumables & eating
The map fills with continuously spawning consumables (trash, items, progressively bigger objects). Eating is trigger-based: touch → compare tier → devour → mass + XP + score + haptic feedback.

### Skill & Upgrade system (levels 1–8 + evolution)
On level-up the game pauses and offers **3 cards**; you pick 1. Picking the same skill again levels it up to 8. A **Re-roll** button: 1 free per session, then 50 gold.

| Category | Skills |
|---|---|
| Defense | Shield, Regeneration (+0.5 HP/s, +0.5 per level) |
| Offense | Weapon (by character: Cannon / Ball / Pistol) |
| Passive | Max Health (base 100, +10 per level) |
| Mobility | Dash (Bot) |
| Support | Vacuum (10-unit radius, +5% per level), Magnet, Score Multiplier |

**Evolution:** When two skills reach max level they merge into a new power (e.g. Magnet + Vacuum → SuperMagnet). This feeds build depth and the "what am I building toward this run?" decision.

**Accessibility:** Cards are color-blind friendly — color + symbol (star/diamond/circle).

### Enemy system & scaling
| Type | Behavior | XP |
|---|---|---|
| Normal police | Runs in crowds | 1–5 |
| Elite police | Slow, strong, special attack pattern | 20–50 |
| Miniboss | Every 5 minutes | 100–200 |
| Apocalypse Boss | At minute 25, phase transitions + combos | 500 |

**Time-based scaling:** 0–5 min basic swarm → 5–10 min elites + 20% damage↑ → 10–15 min miniboss + 2× density → 15–20 min multiple elites + speed → 20–25 min all types + 3× damage → 25 min+ apocalypse wave. Pressure grows exponentially over time; the "epic finale" feeling comes from here.

### Weather effects (runtime modifiers)
Random/timed in-session modifiers make each run different:
- **Lunar Eclipse:** XP +50%, vampires +30% stronger
- **Crimson Rain:** Market multiplier ×2, enemies slow down
- **Fog:** Vision narrows, enemies spawn invisible
- **Lightning Storm:** Player aura damage, mechanical enemies disabled

---

## 6. Characters & Weapons

Each character is a "ball" form holding a weapon. Weapons fire automatically (bullet-heaven).

| Character | Passive | Starting Weapon | Unlock |
|---|---|---|---|
| **Topik** | +20% move speed | Cannon | Available at start |
| **Mıknato** | +10% pull force | Metal ball | 500 credits in the Market |
| **Mermo** | Splits large consumables into pieces with bullets | Pistol | Complete a session on 3 different maps |

**Character meta-upgrades:** Each character has 5 permanent tiers bought with Market credits (speed, growth area, armor, special passives — e.g. "knock enemies back," "half-vampire form"). These affect all sessions.

---

## 7. Meta Progression — Market, Grimoire, NG+

### Market (persistent economy)
Credits collected in sessions are a persistent resource; 50% is kept on death.

| Purchase | Cost | Effect |
|---|---|---|
| New Character | 500 | Permanent unlock |
| Character Passive +1 | 200–800 | Tiered permanent passive |
| New Map | 300–700 | Add to map pool |
| Starting Weapon Unlock | 400 | Start with that weapon |
| XP Multiplier +10% | 1000 | Applies to all sessions |

### Grimoire (Codex)
Stats + lore text for every enemy/weapon/map encountered. Added automatically on first encounter. **100% completion → special cosmetic reward.** A collection drive and exploration motivator.

### NG+ difficulty tiers
Standard → **Crimson Moon** (finish 1 map: enemy speed +20%, XP +30%) → **Blood Crisis** (5 maps: boss HP ×2, rewards ×2) → **Apocalypse** (all maps: endless extra difficulty). A long-term replayability layer.

---

## 8. Maps & World

- All maps are **infinite-scrolling** (Vampire Survivors model).
- Collectibles: **Coin**, **Heart** (health), **Golden Chest** (bonus upgrade).
- Structures like caves / barns hold easter eggs and badges.
- Planned maps: **Modern City** (starting) → **Medieval** → (Park, Harbor, Industrial later).

Each map carries its own consumable set, enemy pool, and palette; storytelling is done through environment design rather than cutscenes.

---

## 9. Art & Audio Direction

**Style:** 16-bit pixel art, dark gothic palette. Reference: Vampire Survivors' visual language + Castlevania sprite aesthetics. Character sprites 32×32, 4–8 frames per animation. Scrolling parallax layers. Effects: blood splatter, soul lights, flying bones.

| Usage | Color | Hex |
|---|---|---|
| Player | Crimson | `#8B0000` |
| UI | Bone white | `#F5F0E0` |
| Background | Night blue | `#0D0D2B` |
| Swarm enemy | Swamp green | `#3D5C3A` |
| Elite enemy | Night purple | `#4A0E6B` |
| XP gem | Amber | `#FFC300` |

**Audio:** Synthwave + gothic orchestra. Each map has its own theme; tempo ramps up past minute 20. SFX: enemy death sound, level-up jingle, boss entrance.

---

## 10. Controls & Accessibility

| Platform | Movement | Pause | Upgrade Selection |
|---|---|---|---|
| Mobile | Virtual joystick (left) | Top menu button | Screen tap |
| PC | WASD / Arrow keys | ESC | Mouse / 1-2-3 |
| Gamepad | Left stick | Start | A/B/X |

**Accessibility principles:** One-handed play · color-blind friendly UI (color + symbol) · notch/safe-area support · local session save.

---

## 11. Business Model & Monetization

**Core principle: Zero pay-to-win. All mechanical content is free; money buys only cosmetics and convenience.**

### Mobile (iOS/Android) — Free-to-play
- Market credits are earnable in-game (no paywall).
- **Cosmetic shop:** character color palettes, weapon skins, UI themes.
- **Optional ads:** watch → a Re-roll or bonus XP (rewarded, never forced).
- IAP: cosmetics only.

### PC (Steam) — Paid
- Price: **$4.99**.
- DLC: _"Apocalypse Expansion"_ — 2 maps, 1 character, 10 weapons — $2.99.

This dual model delivers a broad audience + ad/cosmetic revenue on mobile, and premium revenue on PC; balance is never broken by payment.

---

## 12. Competitive Analysis & References

| Game | Lesson learned |
|---|---|
| **Vampire Survivors** (poncle) | Core loop, auto-attack, upgrade & meta system |
| **Brotato** (Blobfish) | Character variety, build depth |
| **20 Minutes Till Dawn** (flanne) | Weapon feel, visual feedback |
| **Hades** (Supergiant) | Roguelite meta, narrative integration |
| **Castlevania: SotN** (Konami) | Gothic aesthetics, enemy variety |
| **Agar.io / Katamari / Hole.io** | Growth curve, "devour everything" fantasy |

**Positioning:** Blob.io takes the proven bullet-heaven loop and adds a **physical growth + risk/reward slowdown** mechanic no competitor has. Familiar but not a clone.

---

## 13. Investment Thesis — Why It Sells

1. **Proven, growing genre.** Bullet-heaven has repeatedly succeeded commercially on mobile and Steam. Demand exists and the market isn't saturated.
2. **Instant distinctiveness.** The growth mechanic differentiates the game in the first second on a storefront — escaping the genre's biggest trap, "they all look the same."
3. **Low entry, high retention.** Single input = broad accessibility. Short sessions + meta-progression = strong D1/D7 retention potential.
4. **Natural monetization fit.** Rewarded ads (Re-roll), cosmetic IAP, and PC premium — multi-channel revenue without pay-to-win.
5. **Scalable content.** Character/map/weapon/upgrade systems are ScriptableObject-based; content grows without code changes → low-cost DLC and seasonal content.
6. **Lean team, working core.** A small indie team has produced a playable vertical slice; capital goes toward content and polish, and the core risk (is the gameplay fun?) is largely behind us.

---

## 14. Technical Foundation

- **Engine:** Unity 6, URP (Universal Render Pipeline)
- **Input:** New Input System (`UnityEngine.InputSystem`) — virtual joystick + WASD/gamepad
- **AI:** NavMeshAgent (for elites/bosses); a switch to steering is planned for crowds
- **Architecture:** Clean namespace separation (`Core / Entities / Systems / UI / Data / Input`), a static event bus (`GameEvents`), a generic object pool system, ScriptableObject-based data (Consumable/Enemy/Upgrade/Wave/Character)
- **Performance targets:** Mobile 30 FPS @ 720p (500 enemies) · PC 60 FPS @ 1080p (1000+ enemies) · ≤15% battery over a 30-min session

The architecture is disciplined: no import cycles, loosely coupled systems via the event bus, and mobile performance rules (throttle pattern, `sqrMagnitude`, mandatory object pooling) documented in the project docs.

---

## 15. Development Status (Honest Assessment)

The status below is based on the latest full review of all scripts, the scene, and assets (`FABLE_INCELEME.md`). **We do not hide the gap between the vision and today.**

### ✅ Working core
- Movement, smooth growth, tier system, eating/consumables, blob health/death/regen
- Consumable spawning (pool-based), enemy spawning + state machine (patrol/chase/attack) + NavMesh
- Upgrade system (weight-based 3-card selection, levels 1–8, Vacuum skill)
- 3 characters + 3 weapons + projectile system, minimal lobby
- HUD (health/XP bar, score, timer, tier, level, skill badges), Game Over, safe-area
- Wave system (time-threshold based)

### 🔧 Known critical gaps (priority work)
- **Enemy death is rewardless** — no coin/XP/score drop yet; the reward half of the bullet-heaven loop must be closed (planned for Sprint 2).
- **Pool return gaps** — enemies and consumables aren't returned to the pool (a few-line fix; restores the "no Instantiate" rule).
- **500-enemy scale** — NavMeshAgent-per-enemy won't scale that high; a steering/flow-field decision for crowds must be made early.
- **Content volume** — the inventory is currently 1 enemy, 1 consumable, 1 wave, 1 vehicle. The code skeleton is ahead of content; asset production is the bottleneck.
- **Vision conflict** — two internal docs target different session length/scale (20–30 min & 500 enemies vs. a compact 5–10 min run). The team must settle on one (see §18).

> **Assessment:** The project is at the _solid skeleton + working core_ stage — not yet a _polished vertical slice_. The biggest risk (is the gameplay fun?) is working; the remaining work is largely **content production, closing the reward loop, and scale decisions** — i.e. work where capital and time convert efficiently, with low discovery risk.

---

## 16. Roadmap

**Completed:** Core infrastructure · Blob · Consumables · Enemy system · Upgrade system · HUD/UI · Character & weapon foundation

**Upcoming phases:**
- **Sprint 2:** Enemy scaling, elite behavior, coin drops, chest drops, Dash skill, Skill Evolution system, coin HUD
- **Phase — Maps & Regions:** Infinite scroll, region filters, collectibles
- **Phase — Boss waves:** 5/10/15/20 min minibosses + minute-25 Apocalypse Boss + boss HP bar
- **Phase — Meta Progression:** Market screen, Grimoire, NG+ difficulties, persistent economy
- **Phase — Weather system**
- **Phase — Polish & Juice:** particles, AudioManager, camera shake, combos
- **Phase — Monetization:** cosmetic shop, rewarded ads, Unity IAP

**GDD draft timeline:** Pre-Alpha (Months 1–3) → Alpha (4–6) → Closed Beta (7–9) → Open Beta + Steam Next Fest (10–12) → PC Early Access (Month 13) → 1.0 + Mobile + DLC (Months 15–18).

---

## 17. Team

**Zorse Studio** — a 1–3 person indie team.

| Person | Role |
|---|---|
| Hüma | Game Artist, UI/UX Designer |
| Serra (Sero) | Game Developer, Game Designer |
| Bahar | Game Developer, Game Designer |

---

## 18. Risks & Open Questions

### Risks and mitigations
| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Scope creep | High | Delay | Stay true to MVP, defer extras to DLC |
| Mobile performance (500 enemies) | Medium–High | Poor UX | Early device profiling, enemy limit/steering decision |
| Market competition (crowded genre) | High | Low sales | Strong differentiation via growth mechanic, community-first |
| Team erosion (small team) | Medium | Process stalls | Open records, freelance network |
| Unity scene merge conflicts | Medium | Lost work | Split into prefabs, branch-based scene ownership |

### Open questions awaiting decision
- Final game name (Zorse / Blob.io / ?)
- **Session structure: 20–30 min & 500 enemies, or a compact 5–10 min run?** (the conflict between the two internal docs — the single most critical decision, as it defines architecture and content volume)
- Re-roll economy, meta currency name, region transitions (free/locked)
- Boss distribution: at every tier, only the final one, or both?

---

_Blob.io — Zorse Studio · Based on GDD v1.0 (June 2025), synchronized with current development status._
