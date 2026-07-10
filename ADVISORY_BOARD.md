# Blob.io — Advisory Board Report

> Prepared by the Fable 5 advisory board · July 2026
> Sources: GDD v1.0 (June 2025), `README.md`, `CLAUDE.md`, `GDD.md`, `SPRINT_ROADMAP.md`, full codebase review (`FABLE_INCELEME.md`)
> Mandate: maximize this game's chance of commercial success. No invented features — every recommendation solves a real weakness or increases commercial potential.

---

## Board Verdict (60 seconds)

The core fantasy — **eat, grow, slow down, get hunted** — is genuinely differentiated and worth building. The danger is not the idea. The danger is that the GDD describes a game roughly **4x bigger than a 3-person team can ship**, on **two platforms with opposing business models**, with **three unresolved identity contradictions** baked into the project's own documents. Fix the decisions before writing more code. Decisions are free; code is not.

---

## 🔴 Critical — must resolve before further development

### 1. Resolve the session-length / scale contradiction NOW
- **Why it matters:** Two internal docs describe different games (20–30 min & 500 enemies vs. 5–10 min compact runs). This single decision determines enemy architecture, content budget, battery target, boss pacing, and monetization cadence. Every sprint run without deciding builds on sand.
- **What it solves:** Prevents building (and later throwing away) the wrong enemy AI, wave scaling, and content volume.
- **If ignored:** Month 6 arrives with systems tuned for a 30-minute game and content for a 10-minute one. Rework at that stage costs 2–3x.
- **Impact:** Highest in this document.
- **Board recommendation:** **12–15 minute runs, ~150–200 concurrent enemies.** Reasons: (a) mobile session data overwhelmingly favors sub-15-min loops; (b) the 15%-battery-per-30-min target is nearly unachievable at 500 agents; (c) 20–30 minutes of content pacing requires a content volume (bosses every 5 min, escalating waves) that one artist cannot feed. Compress the GDD's minute-based escalation table proportionally — keep the *shape* of the curve, shrink the timeline.

### 2. Close the reward loop before anything else
- **Why it matters:** Right now killing an enemy yields *nothing* — no XP, no coin, no event. In a bullet-heaven, kill→reward is the dopamine engine. The game currently has half a genre.
- **What it solves:** Makes the game testable as a *game*. Every playtest before this fix produces invalid feedback.
- **If ignored:** Movement and growth get polished for months while the actual retention loop remains unvalidated.
- **Impact:** Very high, and cheap — days of work (Sprint 2's A7 plus an `OnEnemyDied` event and XP orb drops).

### 3. Pick ONE launch platform and business model
- **Why it matters:** Mobile F2P and PC premium are not two builds of one game — they are two *businesses*. Mobile F2P requires analytics, LiveOps, A/B testing, and a UA budget or publisher; without paid installs, F2P games get ~zero organic visibility. PC premium requires wishlist marketing, Next Fest, demo culture. The GDD even contradicts itself: CLAUDE.md says mobile-first, the GDD timeline ships Steam Early Access at month 13 *before* mobile.
- **What it solves:** Focuses the team's (tiny) marketing capacity and the feature list. Half the GDD's meta economy only makes sense on one platform or the other.
- **If ignored:** A compromised version ships on both platforms and succeeds on neither. This is the most common indie death.
- **Impact:** Existential.
- **Board recommendation:** **PC/Steam first** — the Vampire Survivors → Brotato → 20MTD path. Faster iteration, Next Fest gives free visibility and playtest data, premium means no LiveOps burden, and a successful Steam launch *funds and de-risks* the mobile port (this is exactly what VS did). Keep the mobile-friendly design constraints (one input, short sessions, safe-area) so the port stays cheap. If a mobile publisher with UA budget appears, revisit.

### 4. Decide the enemy architecture before content scales
- **Why it matters:** NavMeshAgent-per-enemy caps at around 30–50 units on mobile hardware. Whether the target is 200 or 500, per-agent pathfinding is the wrong tool for swarms; it's only right for elites/bosses.
- **What it solves:** Prevents a rewrite after 10+ enemy types are built on the wrong base class.
- **If ignored:** The retrofit touches every enemy prefab, every state machine, every spawner. Cost grows with each sprint of delay.
- **Impact:** High and *time-sensitive* — this is the "decide early" item from the technical review.
- **Board recommendation:** Simple steering (move-toward-player + neighbor separation via spatial hash) for swarms; NavMesh reserved for elites and bosses only. One spike week, now.

### 5. Resolve the art-direction contradiction
- **Why it matters:** The GDD specifies 16-bit pixel art, 32×32 sprites, parallax layers. The actual project is 3D/2.5D Unity URP with NavMesh and physics. These are different production pipelines, and the team has **one artist**. Every asset made before this decision risks being thrown away.
- **What it solves:** Locks the single most expensive production pipeline in the project.
- **If ignored:** The artist produces assets for one pipeline while the code assumes another; the mismatch is discovered during integration.
- **Impact:** High — art is the scarcest resource.
- **Board recommendation:** Stay 2.5D/stylized-3D (matches the codebase; a physically growing blob reads *far* better with real scale than with sprite swaps — the core fantasy is literally about size). Update the GDD's art section to match reality.

---

## 🟠 High Impact — strongly recommended

### 6. Make *eating* the primary verb; weapons are support
- **Why:** The differentiator is the growth fantasy, but the GDD's weapon/skill list is generic bullet-heaven. If weapons do most of the killing, this becomes a VS clone with a size gimmick — reviewers will say exactly that.
- **Solves:** Positioning. The thing only *this game* has must be the thing the player does most.
- **If ignored:** "Competent but derivative" reviews; no word of mouth.
- **How:** Let tier/mass gate enemy consumption (a big blob can *eat* swarm enemies whole — the fantasy payoff), let weapons soften what can't yet be eaten. This also makes the hazard mechanic (#8) meaningful.

### 7. Collapse the currencies
- **Why:** Current count: mass, XP, score, in-run coins, Market credits, and "gold" for re-rolls. Six numeric systems for a 15-minute mobile-friendly game.
- **Solves:** Cognitive load, UI clutter, economy-balancing surface area, and player confusion about "what am I earning?"
- **If ignored:** Every currency needs earn rates, sinks, HUD space, and balance passes — pure cost, no fun.
- **How:** Mass = XP (already true in code — good). **One coin**: dropped in-run, pays for re-rolls in-run, converts to Market credit at session end (50% kept on death, as designed). Score stays as leaderboard vanity only. That's it.

### 8. Commit to the hazard rule or cut it
- **Why:** "Things bigger than you hurt you" is half-implemented and half-designed, yet it's the *natural* completion of the risk/reward pillar — the world itself gets safer as you grow, which makes growth *feel* like power.
- **Solves:** Gives early-game tension (dodge the big stuff) and makes tier-ups emotionally legible without any new content.
- **If ignored:** Dead code, dead data fields, and a flatter early game.
- **Impact:** High fun-per-dev-hour ratio. Keep it, finish it.

### 9. Cut boss variety, keep boss *moments*
- **Why:** SWAT car + helicopter + drone + a phase-based Apocalypse Boss = 4+ unique bosses with distinct attack patterns. For one artist and two programmers, each boss is 2–4 weeks.
- **Solves:** The single biggest content-cost line item.
- **If ignored:** Bosses ship undercooked, and an undercooked boss is worse than no boss.
- **How:** MVP = **one** miniboss (reused at escalating stats at each interval) + **one** final boss with a single phase transition. The *fantasy payoff*: the final boss should be something the player ultimately **eats**. That's the trailer moment.

### 10. Analytics + save system before any public build
- **Why:** The save system is currently one PlayerPrefs int that isn't even flushed. Meta-progression — the retention engine — has nowhere to live. Without analytics, the only questions that matter at soft launch (D1, session length, where players die) are unanswerable.
- **If ignored:** The soft launch happens blind and teaches nothing from the most valuable free data.
- **How:** JSON save file + Unity Analytics or GameAnalytics (free tier). One sprint, total.

### 11. Fix the collaboration pipeline (cheap insurance)
- **Why:** Three people, one Unity scene, no UnityYAMLMerge, no Git LFS before art arrives. The GDD itself lists this as a known risk.
- **If ignored:** A day of work *will* be lost to a scene merge, probably the week before a deadline.
- **How:** `.gitattributes` + smart merge, managers into a "Systems" prefab, UI panels as prefabs, Git LFS now. Half a day.

---

## 🟡 Nice to Have

- **Skill evolutions (2–3 at launch, not a full matrix).** Evolution is VS's strongest retention hook, so it stays — but each evolution is a designed, art-ed, balanced feature. Two great ones beat eight mediocre ones.
- **Re-roll button.** Good monetization surface (rewarded ad on mobile later), trivial cost. Keep.
- **Color-blind card symbols.** Cheap, genuinely good, and a nice press talking point. Keep.
- **Second map at launch.** One *polished* map for MVP; the second map is the first content update or a launch-week bonus.

---

## 🟢 Future Update (after launch — do not build now)

- **Weather system.** Four weathers, each with special rules ("mechanical enemies disabled" implies enemy-type tagging systems) — a whole expansion pretending to be a feature. Great *post-launch content drop*: it reuses existing systems and makes patch notes exciting. Zero of it before launch.
- **NG+ (4 difficulty tiers).** Meaningless until players finish the base game. Post-launch retention content.
- **Grimoire/Codex.** Charming, cheap-ish mechanically, but it demands lore text and art for *every* entity — it scales with content that doesn't exist yet. Ship the tracking hooks (first-encounter flags), build the UI later.
- **Per-character 5-tier meta trees.** The GDD marks this "SONRA DÜŞÜNÜLECEK" and the table lists four characters (Mira/Dorian/Lyra/Bastian) who don't exist in the game — template residue. Launch with 4–6 flat permanent stat upgrades; character trees come post-launch.
- **PC DLC ("Apocalypse Expansion").** Planning DLC before validating the core loop is planning the second floor before the foundation is poured.
- **Cosmetic shop / IAP / ads.** Post-validation. On the Steam-first path, all of this moves to the mobile port phase.

---

## Simplify / Remove / Over-designed — direct answers

### Mechanics to simplify
- **Currencies: 6 → 2** (coin + score). See #7.
- **Pull mechanics:** Magnet skill + Vacuum skill + Mıknato's magnet passive = **three overlapping pull systems**. One "Attract" skill line; Mıknato's passive boosts it; Magnet+Vacuum merging into SuperMagnet already admits they're the same idea.
- **Wave escalation table:** keep the curve shape, compress to the chosen run length.

### Mechanics/features to remove (from launch scope)
- Weather, NG+, Grimoire UI, per-character meta trees, PC DLC plan.
- **"Starting Weapon Unlock" Market item** — characters already define starting weapons; this purchase is redundant *and* muddies character identity.

### Over-designed features
- Boss roster (#9).
- Weather system (above).
- The Market table has five purchase categories before a single player has spent a single credit.

### Complexity without fun
- **Score Multiplier as a pickable skill** — it's a stat, not a decision; it's the card players sigh at. Fold score bonuses into meta upgrades instead.
- **Mermo's passive** (bullets split large consumables into fragments) requires a fragmentation system — spawnable fragment variants for every large consumable, forever. That's a *per-asset tax* on all future content for one character's flavor. Redesign the passive to something systemic (e.g., can eat one tier above at a health cost — pure code, same fantasy of "access to big things early").

### Unnecessary development risk
- Dual-platform simultaneous launch (#3).
- NavMesh-at-scale (#4).
- FMOD/WWise — plain Unity audio + a mixer is enough at this scope; the GDD's audio-middleware line is aspiration, not need.
- Single-scene teamwork (#11).
- Building meta-economy screens before the core loop is proven fun.

---

## The Four Questions

### 1. With only a 6-month budget, what gets cut?
Everything in 🟢 above, plus: the mobile version (port later), the second map, two of the three character *unlock flows* (ship all 3 characters but with simple unlocks — Mermo's "3 different maps" condition requires 3 maps that won't exist), boss roster down to 1+1, evolutions down to 2.

What *remains* is the actual game: eat→grow→slow→survive, 10–12 upgrades, 3 enemy types, one great map, one great final boss the player eventually devours, coin→small meta shop, highscore. That is shippable in 6 months by this team *if* content production starts by month 2.

### 2. What's the smallest version still exciting enough to launch?
The test: **does a stranger feel the hook in 60 seconds?** The hook is the moment you tier up and something that used to chase you becomes *food*.

Smallest exciting version:
- 12-minute run · 1 map
- 1 character at start + 2 unlockable
- 3 enemy types + recurring miniboss + final boss
- 10 upgrades, 2 evolutions
- Hazard rule active
- Coin economy + 6-item Market
- Juice on the eat action (squash, sound, haptic)

Nothing else. Vampire Survivors launched with less and a worse-looking game — its loop was just *complete*.

### 3. What to absolutely protect and never compromise on
1. **The growth-speed trade-off** (`1/√tier`). Playtesters *will* complain about being slow when big. Tune it, never remove it — it's the only tension mechanic competitors don't have.
2. **Smooth growth + eat-feedback juice.** The kinesthetic pleasure of devouring is the retention. Any frame drop or feedback cut here damages the product more than any missing feature.
3. **One-input play.** Every design idea that needs a second button is rejected by default.
4. **The fail loop's generosity** (50% credits kept). "Death is progress" is why session #2 happens.
5. **Zero pay-to-win.** Non-negotiable, and in this genre's community it's also *marketing*.
6. **Frame rate over content.** A bullet-heaven that stutters during the exact moments of peak chaos is refunded.

### 4. Game Director's roadmap: Prototype → MVP → Soft Launch → Global Launch

**Phase 0 — Decisions (1 week, now).**
Lock: run length, platform order, enemy architecture, art pipeline, cut list. Update GDD/CLAUDE.md so there is *one* source of truth. Fix pool-return bugs and the reward loop in the same breath.

**Prototype — "Is it fun?" (weeks 2–8).**
Greybox everything. Steering-swarm spike. Eating-as-primary-verb + hazard rule + kill rewards + 6 upgrades.
**Gate:** 5 strangers play 12 minutes; if ≥3 immediately restart unprompted, proceed. If not, iterate here — this is the cheapest floor to fail on.

**MVP / Vertical Slice (months 3–5).**
The Question-2 feature set with real art on the critical path (blob, 3 enemies, one map's dressing, cards UI). Save system + analytics in. Miniboss + final boss.
**Gate:** internal playtest median session count ≥3 runs.

**Soft Launch = Steam Next Fest demo (months 6–7).**
Free, high-visibility, and yields the two numbers that decide everything: median session length and demo→wishlist conversion. Meanwhile: a trailer centered on one shot — *tiny blob fleeing a cop → giant blob eating the cop.*

**Early Access / Global (months 8–12).**
Launch when wishlists justify it (rule of thumb: 7–10k for a meaningful indie launch). EA roadmap = the cut list, now as *content updates*: map 2, weather, Grimoire, NG+, evolutions 3–8. Each cut feature becomes a patch headline — scope discipline converts directly into a living-game narrative.

**Mobile port (post-1.0, months 12+).**
Funded by Steam revenue, informed by real balance data, pitched to publishers *with* traction. This is when the F2P economy, rewarded ads, and cosmetic shop get built — once, correctly, for the platform they belong to.

---

## One-line summary from the board

**The game inside this GDD is good; the GDD is three games. Ship the one only you can make — the eating one — and sell the other two as updates.**
