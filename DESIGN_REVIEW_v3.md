# Blob.io — Senior Design Team Review (v3 Pass)

> Prepared by a senior game design review team · August 2026
> Baseline document: `GDD_v2.md` (July 2026, single source of truth). `GDD.md`, `ADVISORY_BOARD.md`, `BOARD_EVALUATION.md`, `FABLE_INCELEME.md` used only as history — GDD v2 already resolved the contradictions those documents raised (session length, platform, art pipeline, currency count, hazard commitment, boss roster). This review does not re-litigate those; it audits what GDD v2 actually shipped as decisions and takes the design one more pass toward launch-ready.
> Codebase spot-checked directly (not just docs) where it changes a finding: current `Upgrade_*.asset` inventory, `EnemyData_*.asset` inventory, and the existence of an undocumented `Entities/Vehicles/` system.
> Output: this review, plus `GDD_v3.md` — the clean replacement document.

**Team:** Lead Game Designer · Systems Designer · Progression & Economy Designer · Level & Content Designer · UX/Mobile Designer · Product & Retention Designer · Creative Director.

---

## Phase 1 — Understanding the Game

**Player fantasy.** You are small, edible, and afraid. Everything is a threat or a snack, and the line between the two is legible only by size. Growth is not abstract power — it's the literal thing the camera sees. The fantasy resolves in a single sentence the team has already written correctly: *I started tiny and afraid of everything. By the end I was eating the things that used to chase me.*

**The 30-second experience.** The repeating decision is not "what do I press" (there's nothing to press — movement is the only input) but "where do I put my body." Every few seconds the player re-answers: *is this bigger or smaller than me, and is it worth the detour?* That's the whole game, and it's enough, the same way Threes/2048's repeating decision was enough — because the decision compounds visibly.

**Run arc.** Minute 0 is prey. Minutes 3–8 are the transition — the player is simultaneously growing bolder (can now eat swarms) and the world is growing more hostile (elites, density, damage multipliers) — this tension, not raw difficulty, is the arc's engine. Minutes 8–12 should be the payoff window: elites become food, the map that was a minefield an hour ago is a buffet. Minute 12+ is the final confrontation, and its resolution — eating the thing that was, one map away, the scariest object on screen — is the emotional cadence of the whole run compressed into ten seconds.

**Power curve.** It isn't a curve, it's a *relationship reversal*, and that's the differentiator. In most bullet-heavens the player gets stronger relative to a static threat scale. Here the threat scale is denominated in the player's own body — a police officer never changes, but whether it's dangerous is entirely a function of what the player has eaten. This is the single mechanical idea worth protecting above all others.

**Replay motivation.** Three layers, correctly separated in GDD v2: (1) within-run build variance (which cards you saw), (2) meta unlocks (new character = new relationship to the same fantasy, not new content volume), (3) mastery of the size-flip timing (can I get big enough, fast enough, to flip the elite encounter before it flips me). Layer 3 is underused in the current document — see Phase 5 and Phase 9.

**Market identity.** A ten-second clip: tiny red blob fleeing three cops down an alley, hard cut, giant red blob swallowing a cop whole mid-stride. No UI, no dialogue, understood muted, on a phone, in a scroll feed, in under two seconds of attention. GDD v2 already names this as the primary marketing asset (§13). Correct call — protect it structurally (see Phase 14).

No redesign needed here. This is what GDD v2 is already building. The review below is about whether every system currently *serves* that arc or quietly competes with it.

---

## Phase 2 — Design Review by Section

Legend: 🟢 KEEP · 🟡 CLARIFY · 🟠 IMPROVE · 🔴 RECONSIDER

### §3 Core Gameplay Loop
🟢 **KEEP.** The four nested loops (30s / 2–4min / run / meta) are correctly scoped and don't overlap in responsibility. The "no reward-less death" rule (§7) closing the old board's open wound is good discipline — keep enforcing it as new content is added; it should be a checklist item on every future enemy/hazard PR.

### §4 Growth, Tier & Eating
🟢 **KEEP the formula and the hazard-commit rule.** `speed = 1/√tier` and "bigger than you = hurts you" are the two load-bearing mechanics of the whole design; both are exactly right and both are now implemented in code (Sprint 3). Nothing to change.

🟠 **IMPROVE — tier-to-threat pacing needs an explicit target, not just a threshold table.** The tier thresholds (§4) tell us *what mass* unlocks what, but nothing in the document says *when in the 12–15 minute run* a player should reasonably cross them. Elites are introduced at minute 3–5 (§7 escalation table) but are only edible at Tier 5 (mass 100) — the top tier. Without a stated pacing target, tuning (Consumable mass values, enemy mass rewards) has no target to hit, and playtesting can't tell "too slow" from "as designed." See Phase 4 for the proposed curve. Cost: **LOW** — this is a tuning target, not new systems.

### §5 Skills & Upgrades
🟢 **KEEP** the decision to add eating-themed skills (Sindirim/Yutuş/Yırtıcı Çene) — this is the single most important fix since v2, because it's the fix that actually makes the differentiator show up in the build layer, not just the base mechanic. Previously the entire skill list could have belonged to any VS-like; now three of eleven don't.

🔴 **RECONSIDER — do not cut Shield.** GDD v2 §5 marks Shield as cancelled ("aynı iş" as Armor). We disagree, and the cost of disagreeing is zero: `ShieldEffect.cs`, `BlobHealth.CurrentShield/MaxShield`, and the HUD shield bar are already fully built and shipped (confirmed in code — `Upgrade_Shield.asset` still exists in `Data/Upgrades/`). Mechanically the two are not redundant: Armor is invisible, smooth, percentage mitigation; Shield is a *visible, depleting buffer* — in a swarm-dense hazard game, a readable "how much punishment can I still absorb" number is a distinct and valuable piece of information Armor can't give. Keeping it also restores an 11-skill pool after Score Multiplier is moved out (below), without adding a single hour of new work. **Cost: negative** — reversing this decision *saves* the cleanup work of deleting a working system. Confidence: **HIGH**.

🟡 **CLARIFY — Dash vs. permanent Speed is a real overlap, and the fix is cheap.** Both currently just raise average velocity over a window; if Dash is only "the same stat, but pulsed," it's not a second decision, it's the same decision paid for twice in review-doc language and once in dev time. Concrete default: **Dash's active window grants partial hazard-contact damage reduction** (proposal: −50%, not full i-frames — reward the dodge, don't trivialize the rule), making Speed the *economy* tool (offsets the tier speed-tax at all times) and Dash the *emergency* tool (a panic button that's automatic, matching the one-input rule, but reads as "the game just saved me" rather than "I passively walk faster sometimes"). This is a small `BlobHealth`/`DashComponent` interaction, not a new system. **Cost: LOW.** Confidence: **HIGH** — resolves the open decision in GDD v2 §16 without deleting shipped code.

🟠 **IMPROVE — Score Multiplier: move to the Market, out of the run-time card pool.** The old board already flagged this ("a stat, not a decision — the card players sigh at") and GDD v2 left it as an open decision. Resolve it now: Score is explicitly vanity-only (§8), so a card that increases a vanity number competes for a level-up slot against cards that change how you play. Move it to a flat Market permanent stat (small, cheap, same category as +5% speed etc.). This also cleanly restores the skill pool to **11 run-time skills** once Shield is kept. **Cost: LOW** (data move, no new code). Confidence: **HIGH**.

🟠 **IMPROVE — evolution thresholds are probably unreachable in one run.** Both launch evolutions require **two** skills at **max level (8)** simultaneously. Given XP thresholds grow with level (`20 + level×15`) and mass is the only XP source, reaching level 8 on *one* skill already consumes a large share of a run's total level-ups; reaching it on *two specific* skills at once, inside 12–15 minutes, is an optimistic ask with no supporting math in the document. Proposed default: **max level on the anchor skill + level 4–5 on the pairing skill** (not max/max). This keeps evolutions rare and build-defining without making them theoretical. **Cost: LOW** (a threshold change in `SkillEvolutionData`, not a system change). Confidence: **MEDIUM** — should be confirmed against real level-up counts once B16/B19 analytics land; flag as a Design Risk (Phase 15) to validate in the prototype gate, not ship un-tested.

### §6 Characters
🟢 **KEEP all three as designed.** See Phase 6 for the full pass — all three passives now have genuine verb-level identity (not just stat deltas), which was the old board's sharpest criticism and it's fixed. Mıknato in particular — a homing weapon *and* a boosted-pull passive — is a rare case of a character whose weapon and passive tell the same mechanical story. Don't touch it.

### §7 Enemy System
🟡 **CLARIFY — the launch roster is thinner than the escalation table implies, and there's an undocumented asset that could close the gap for free.** Confirmed in the project: only two `EnemyData` assets exist (`Police`, `ElitePolis`); the miniboss/final boss reuse the same designs at escalated stats (by design, and correctly scoped — see Phase 7/15). Separately, the codebase contains a fully separate, unmentioned system: `Entities/Vehicles/CarController.cs` + `CarSpawner.cs` + `Data/CarData.cs` — not listed in CLAUDE.md's script inventory, not present in GDD v2's enemy or hazard tables at all. Default resolution: formally classify cars as a **Traffic Hazard** class (not an enemy — no AI, no XP/coin reward, pure environmental hazard using the existing hazard-damage pipeline), which gives the launch map a third *readable* danger type for zero new content, since it's already built. If engineering confirms the system is stale/abandoned instead, the alternative is formally removing it — either way, the doc gap should close this sprint. Confidence: **MEDIUM** (contingent on an engineering check of current functionality — this is the one item in this review that needs a fact the design team can't confirm alone).

### §8 Economy & Meta Progression
🟢 **KEEP** the two-currency collapse (coin + vanity score) — still correct, still the right call.

🟠 **IMPROVE — launch meta progression is entirely "content progression" or "flat stat," nothing in between.** See Phase 9 for detail: add one cheap "possibility progression" signal at launch (a discovery counter fed by the Grimoire tracking hooks that are *already logging* per CLAUDE.md — just needs a read-only number in the Market UI, no new tracking work).

### §9 Map
🟡 **CLARIFY — the single-map decision is right, but the doc doesn't yet describe how the same map reads differently by tier.** See Phase 8 for a concrete, art-cheap proposal (spawn-table layering, not new geometry).

### §10 Art & Audio
🟢 **KEEP** as written — 2.5D stylized 3D, gothic palette, Unity Audio + mixer. No notes; this section is already tightly scoped to the team's actual pipeline.

### §11–13 Controls, Monetization, Go-to-Market
🟢 **KEEP.** One-input rule, zero pay-to-win, rewarded-ad-on-reroll, self-publish + organic plan are all sound and none compete with the core fantasy. The mobile-first decision (a deliberate reversal of the prior board's PC-first recommendation) is outside pure game-design scope to re-litigate — see Phase 17 for the one dissenting note the team wants on record.

---

## Phase 3 — Core Loop Stress Test

- **Is eating satisfying enough to carry the game?** Mechanically yes — smooth-scale growth, squash feedback, haptic, and (as of Sprint 3) a real hazard branch mean every eating decision now has stakes and a payoff. The open risk is tuning, not design: "Yeme feedback kalitesi" is already listed in GDD v2 §17 as non-negotiable — keep it there.
- **Does automatic combat support eating or compete with it?** It supports it, and the document undersells *why*: only eating grants mass (=XP); weapon kills grant coin/score but not growth. This is the actual mechanism that keeps guns from becoming the dominant strategy, and it currently lives only in scattered script comments (`EnemyBase.TryConsumeByBlob`) rather than being stated as a design rule anywhere in the GDD. **Fix: state it explicitly as a Combat Philosophy principle in v3** (done — see new §9 in GDD v3). Zero implementation cost, pure documentation, prevents future skill/weapon additions from accidentally breaking the balance.
- **Is grow→slow-down actually interesting, or does it read as punishment?** Interesting, *if and only if* the world visibly gets safer as you slow down (hazard rule flips from threat to food at the same rate you lose mobility). That trade is now implemented. The risk is tuning window, not design — flagged as a Design Risk in Phase 15.
- **Does the hazard system create readable decisions?** Yes, with the caveat that readability depends entirely on the outline/icon treatment shipping as designed (§11 accessibility rule) — this is now a production dependency, not a design one.
- **Is there real eat-now/escape-now tension?** Yes, but it's currently only tested against consumables and swarm enemies. The elite-eat-timing gap noted in Phase 2 (§4) is the one place this tension currently has no floor under it — fixed by the pacing target in Phase 4.
- **Does becoming able to eat enemies deliver the status-reversal moment?** Structurally yes, and it happens in escalating beats: swarm at Tier 3, elite at Tier 5, boss at the end — a three-step crescendo the document has but doesn't call out as a deliberate rhythm. Naming it in v3 (see Design Pillars) makes it a checklist item for every future enemy type: *what tier eats this, and does that tier arrive with room to enjoy it before the run ends?*
- **Does the final boss complete the fantasy?** Conceptually yes ("eaten in the final phase"), but the document doesn't say *how* a Tier-5 blob visually/mechanically swallows something boss-scale without either looking absurd or requiring new large-scale animation work. See Phase 7 for a concrete, zero-new-asset proposal (reuse `ConsumeAndSplit`).

---

## Phase 4 — Run Pacing

GDD v2 compressed the curve correctly to 12–15 minutes but the escalation table (§7) is stated purely in *enemy pressure* terms, not paired with a *tier* target. Proposed pacing (design targets for tuning, not hardcoded — matches the document's own convention that balance numbers live in ScriptableObjects):

| Time | Tier target | Threat state | Emotional beat |
|---|---|---|---|
| 0–1 min | Tiny → early Small | Consumables only | Learning, no danger yet |
| 1–3 min | Small | Basic swarm appears | First fear — everything is bigger |
| 3–5 min | reaching Medium | Elites join; 4-min miniboss | **First reversal**: swarm becomes edible right as the world gets scarier — the two should cross close together, not with a gap |
| 5–8 min | Medium → Large | Density ×2; 8-min miniboss (escalated) | Building confidence, build identity emerges |
| 8–10 min | Large → Giant | Multi-elite, speed bonus | **Second reversal window**: player should reach Giant with 2–4 minutes still on the clock — enough runway to actually enjoy eating elites before the finale, not just unlock it in the last ten seconds |
| 10–12 min | Giant | Chaos peak, damage ×2–3 | Controlled overwhelm |
| 12–15 min | Giant | Final boss | Climax — vulnerable-to-victor arc closes with the boss eaten, not just killed |

The one number worth locking now: **Giant tier (mass 100) should be reachable around minute 8–10**, not later. This single target should drive Consumable/enemy mass-reward tuning going forward — it's the difference between "I got big at the very end" (no payoff, just a stat) and "I got to *play* being big" (the actual fantasy).

---

## Phase 5 — Skill & Build Design

- **Do skills create builds or just bigger numbers?** Mixed, and improving. Eating skills (Sindirim/Yutuş/Yırtıcı Çene) plus Attract plus a defense pair now support a genuine "glass cannon eater" vs. "tanky grinder" spread. Weapon Damage and the movement pair are still closer to generic stat sliders — acceptable, because in this genre not every card needs to be an identity card; 3–4 identity-defining cards out of 11 is a healthy ratio (VS ships plenty of flat stat cards too).
- **Are eating skills important enough?** Now yes, structurally — see above. Recommend HUD/card art give them the most distinctive iconography of the set (they're the brand).
- **Do weapon upgrades pull toward generic bullet-heaven play?** No — see Phase 3's finding that only eating grants mass. This is the load-bearing balance rule; state it explicitly (done in GDD v3) so it survives future content additions.
- **Is Attract too automating?** No — `VacuumComponent` only pulls things at-or-below current tier (already edible, non-decisions), so it removes tedium, not tension. Correct as built.
- **Reachable evolutions?** Currently optimistic — see Phase 2 fix (max/mid instead of max/max).
- **Dominant or dead choices?** None obviously dominant given the mass-only-from-eating rule; the one at-risk-of-boring card is Score Multiplier, resolved by moving it to Market (Phase 2).

---

## Phase 6 — Characters

| | Fantasy | Verb identity | Note |
|---|---|---|---|
| **Topik** | The generalist who never fully loses mobility | +20% speed fights the tier speed-tax directly; cannon AoE clears space to approach food safely | 🟢 Clear, keep — market this explicitly as "still nimble even huge" in FTUE/store copy |
| **Mıknato** | The collector — food comes to you | Attract passive + slow homing weapon both express "I don't chase, things come to me" | 🟢 Best character/weapon synergy in the roster — a model for future characters |
| **Mermo** | The breaker — early access to what shouldn't be edible yet | `ConsumeAndSplit` turns big objects into eatable fragments before natural tier growth allows it | 🟢 Best exemplar of the core differentiator; the passive literally *is* the game's thesis on a per-character basis |

No changes recommended. This is the strongest section of the current GDD — all three passives changed the *verb*, not just a stat, which directly answers the single sharpest criticism the prior advisory board raised.

---

## Phase 7 — Enemies & Bosses

| Type | Purpose |
|---|---|
| Swarm (Police) | Forces constant movement decisions; teaches hazard rule early; first thing to flip to food (Tier 3) |
| Elite | Punishes greedy/careless eating; creates a "danger pocket" inside an otherwise-safe crowd; second, later flip (Tier 5) |
| Traffic (proposed, see Phase 2) | Creates safe-street/unsafe-street geography independent of enemy waves — a hazard, not a combatant |
| Miniboss (4/8 min, same design, escalated) | Rhythm marker — teaches "this fight is bigger, prepare" without new content cost |
| Final boss (12 min) | Delivers the thesis: the biggest, scariest thing in the game becomes food |

🟠 **IMPROVE — the miniboss reuse should still get a spectacle beat.** Reusing one enemy design twice at escalating stats is correct scope discipline (protect it — don't build two unique minibosses). But a same-model re-fight risks reading as "the same enemy, more HP" rather than "an event." Cheap fix: reuse the existing Elite material/tint for a distinct miniboss palette, add a name-banner/screen-flash on spawn (pure UI/data, zero new geometry). **Cost: LOW.**

🟠 **IMPROVE — define the boss-eaten moment mechanically, using a system that already exists.** Recommend the final boss's death sequence reuse `ConsumableSpawner.ConsumeAndSplit` (already built for Mermo) — on defeat, the boss breaks into a handful of giant-tier "chunks" that the Tier-5 player then physically eats in sequence, rather than a single instantaneous "swallow the whole boss" animation that would need bespoke large-scale rigging. This delivers the fantasy (you are visibly consuming the thing that hunted you) using a pattern the codebase already has proven and paid for. **Cost: LOW** — reuses shipped code, no new animation pipeline.

---

## Phase 8 — Map & Consumable Ecology

The single-map decision is correct for a 3-person team and should not be revisited. What's missing is the answer to the Phase 8 test question: *can the same street feel different Tiny vs. Giant?*

🟠 **IMPROVE — layer the spawn table by player tier instead of by fixed zone.** Concretely: the same street location hosts different active spawn entries depending on the local player's current tier — a Tiny-tier pass through an alley spawns trash/small hazards; a Giant-tier pass through the *same* alley (on a later loop, since the map scrolls/loops) spawns cars-as-food-adjacent-scenery and swarm enemies as ambient (now trivial) background. This requires no new art or geography — it's a data change to which spawn table is active, keyed on player tier rather than a new map region. It directly answers the design brief's own test question at near-zero cost, and it's the single cheapest available lever the project has to make the growth fantasy *feel* spatial rather than purely numeric. **Cost: LOW–MEDIUM** (spawner logic change; zero new art).

---

## Phase 9 — Meta Progression

Launch scope (2 character unlocks + 4–6 flat stats) is appropriately small for the team size — don't expand it. The one gap: everything is either pure "content progression" (a new character) or pure "power progression" (+5% stat), with nothing in between to produce the "one more run" pull the old board's LiveOps review specifically warned would starve a small team by week 2.

🟠 **IMPROVE — surface the Grimoire tracking data that's already being logged (CLAUDE.md confirms first-encounter hooks exist with no UI) as a simple discovery counter on the Market screen** ("X/Y şey keşfedildi"). This is a "possibility progression" signal — collection psychology — built entirely from data the game is *already collecting* for the post-launch Grimoire UI. **Cost: LOW** (a read of existing logs into one text field, no new tracking, no new art).

---

## Phase 10 — Mobile UX

No structural issues found; the HUD layout (§11) is already single-hand, safe-area-aware, with color+symbol accessibility on cards. One process note, not a design one: the restart flow (§3 step 8) is already "death → reward → Market → play again" with no forced menu, which is the correct shape — protect it as new meta-progression UI (Market screen) is built; the temptation to insert a confirmation dialog or an extra tab should be resisted by default.

---

## Phase 11 — Retention Without Dark Patterns

The rewarded-ad-on-reroll placement remains the strongest monetization idea in the document — it monetizes desire at its peak, not frustration. Nothing to add beyond Phase 9's discovery-counter proposal, which is itself a retention lever, not a monetization one.

---

## Phase 12 — First 10 Minutes / FTUE

| Beat | Target minute |
|---|---|
| First eat | 0:00–0:05 |
| First "too big to eat" object seen | 0:10–0:30 |
| First hazard damage | 0:30–1:00 (should be survivable, low stakes — a teaching hit, not a punishing one) |
| First upgrade card | ~1:00 |
| First enemy (swarm) | ~1:00 |
| First forced escape/detour | 1:00–3:00 |
| First enemy becomes edible (swarm, Tier 3) | ~3:00–4:00 |
| First major power spike (Tier 4→5 crossing) | ~8:00 |
| First elite eaten | 8:00–10:00 |
| Death or final-boss victory | 12:00–15:00 |
| Meta progression screen | immediately post-run |
| Replay temptation | within the same minute — no menu between "run ended" and "run again" |

No tutorial walls needed anywhere in this sequence — every beat above is teachable by the world itself (an object either eats you or you eat it, and the game shows you which before it happens via the hazard outline).

---

## Phase 13 — "Would This Actually Be Fun?"

**Run 1:** Learn the hazard outline the hard way once, get a card, discover eating a swarm cop is genuinely satisfying, die around minute 6–8 to an elite, keep half the coin.
**Run 3:** Recognize card names, start favoring an eating-build or a defense-build, reach Tier 5 with a couple minutes to spare, actually taste the elite-eating payoff, maybe reach the boss.
**Run 10:** Chasing a specific evolution, has unlocked Mıknato or is close to it, knows the map's rough danger zones by feel, is optimizing *when* to commit to eating vs. detouring — this is mastery of timing, the layer-3 replay motivation from Phase 1 that the document under-serves today.

**Three biggest risks to long-term fun**, in order:
1. **Tuning window on the slowdown mechanic** (the psychologist's risk from the earlier board round, still valid, still unresolved by any playtest data in the documents) — if being big feels like being punished rather than powerful-but-vulnerable, the whole pillar inverts. No design fix substitutes for a playtest here.
2. **Evolution unreachability** (Phase 2) — if nobody sees an evolution in a normal run, the build-depth ceiling the game is counting on for Run 10+ players quietly doesn't exist.
3. **Elite-eating arriving too late** (Phase 4) — if Giant tier is reached at minute 13 instead of minute 9, the second status-reversal beat gets cut, and the run becomes "survive," not "become the predator."

All three are tuning/validation risks, not missing systems — which is a good place for the project to be.

---

## Phase 14 — Viral / Watchability Test

The size-flip clip (tiny-flees → giant-eats) is already correctly identified as the primary asset (§13). Two cheap amplifiers worth adding to the production list, neither of which is a gameplay change:
- **A brief hitch/slow-mo on the first swallow of each new "class" of prey** (first swarm eaten, first elite eaten, boss eaten) — a few frames of time-dilation and a camera nudge, reusing the existing squash/haptic feedback system rather than building new VFX.
- **Make sure the hazard outline (already planned for accessibility) reads on a phone screen at thumbnail size** — it's doing double duty as a clip readability feature, not just an accessibility one.

Nothing else in the GDD needs to change for virality — the mechanic already produces the moment; the job is just not to bury it.

---

## Phase 15 — Scope Control

**MUST SHIP**
- Everything already in GDD v2's MVP definition (§14), unchanged.
- The explicit Combat Philosophy rule (eating grants mass, weapons don't) — documentation only, zero cost.
- Shield restored, Score Multiplier moved to Market, evolution thresholds softened (max/mid) — all zero-to-low cost corrections to already-built systems.

**NICE TO HAVE**
- Tier-layered spawn tables for the single map (Phase 8).
- Traffic-as-hazard formalization for Cars, if engineering confirms the system is live.
- Miniboss spectacle beat (tint + banner).
- Boss-eaten-as-chunks using `ConsumeAndSplit`.
- Grimoire discovery counter on the Market screen.

**POST-LAUNCH** (unchanged from GDD v2, still correct)
- Weather, NG+, Grimoire UI, per-character meta trees, second map, evolution pool expansion, PC/Steam port.

Nothing in this review adds a new system. Every IMPROVE item above either reuses an already-built system (`ConsumeAndSplit`, hazard pipeline, Grimoire logs, HUD shield bar) or is a data/threshold change. That was a deliberate constraint on this pass, not an accident — the team already has the right amount of scope; it needs sharpening, not addition.

---

## Phase 16 — Resolved Ambiguities

| # | Ambiguity | Why it matters | Resolution | Confidence |
|---|---|---|---|---|
| 1 | Score Multiplier: run card or meta stat? | Open in GDD v2 §16; blocks final skill pool count | **Meta Market stat**, removed from run-time cards | HIGH |
| 2 | Shield vs. Armor redundancy | GDD v2 marked Shield cancelled; code disagrees | **Keep both** — distinct roles (buffer vs. mitigation), zero cost to keep | HIGH |
| 3 | Dash vs. Speed overlap | Listed as open in GDD v2 §16 | **Dash = emergency hazard-damage reduction burst; Speed = constant mobility.** Not merged, roles split | HIGH |
| 4 | Evolution thresholds (max/max) | No stated math supports reachability in one run | **Max on anchor skill + mid-level (4–5) on pairing skill** | MEDIUM — validate against real level-up telemetry once B19 analytics ship |
| 5 | Undocumented Car/Vehicle system | Exists in code, absent from GDD and CLAUDE.md | **Classify as Traffic Hazard** (environmental, not enemy) if engineering confirms it's live; otherwise formally remove | MEDIUM — needs one engineering confirmation |
| 6 | Mermo unlock: does a run ending in death count as "completed"? | §6 says "3 sessions completed," undefined what counts | **Any run reaching the end-of-run reward screen counts**, death or boss-clear alike | HIGH |
| 7 | Tier-to-time pacing target | Escalation table has no tier benchmarks | **Giant (Tier 5) reachable by minute 8–10** — see Phase 4 | MEDIUM — a tuning target, confirm via playtest |
| 8 | Final game name | Genuinely a brand/product-owner call | **Not resolved here** — carried forward as open | — |
| 9 | Second soft-launch market | Needs real UA/market data outside design scope | **Not resolved here** — carried forward, with a soft design-adjacent note: prioritize a market with strong short-form-video culture, since the go-to-market plan is built entirely around a clip, not a storefront listing | — |

---

## Phase 17 — Team Debate

Two disagreements surfaced, both resolved by the Lead using the stated priority order (fantasy > fun > distinctiveness > clarity > replayability > mobile usability > production feasibility > monetization):

**Systems Designer vs. Progression Designer, on Shield.** The Progression Designer initially wanted Shield gone, on pure economy-cleanliness grounds ("fewer defensive nouns is easier to balance"). The Systems Designer countered that a *shipped, working, already-drawn-on-the-HUD* system has already paid its clarity cost, and the fun/legibility case (a visible depleting buffer in a dense-swarm game) is real. Lead sided with keeping it: production feasibility favors reuse over deletion, and fun/clarity slightly favor having it. Recorded as a reversal of one GDD v2 micro-decision, not a criticism of the process that made it.

**UX Designer vs. Product Designer, on the mobile-first platform call.** This surfaced again because it's the one place GDD v2 explicitly overrode the prior advisory board's recommendation (PC-first). The Product & Retention Designer flagged that the underlying risk the old board named — F2P without a UA budget or publisher gets close to zero organic visibility — is still true and isn't addressed by anything new in this pass. The UX Designer countered that this is a business/distribution question, not a game-design one, and the *design* (one input, short sessions, safe-area) is equally valid either way. The Lead agrees this sits outside this review's mandate (game design, not go-to-market strategy) and declines to relitigate a decision that was made deliberately and on the record — but flags it for the product owner as a standing risk worth revisiting once soft-launch data (§13 metric gates) exists, not before.

No other disagreements reached the Lead — the team converged quickly, which is itself a signal that GDD v2 left the project in reasonably good shape; this pass is a sharpening exercise, not a course correction.

---

## Phase 18 — Final Design Verdict

| Area | Assessment |
|---|---|
| Core concept | Strong, unchanged, still the reason to build this game |
| Differentiation | Now genuinely present at the build/skill layer (eating skills, character passives), not just the base mechanic — this is the single biggest improvement since the last review round |
| Moment-to-moment gameplay | Sound; the eat/hazard/attract loop is a real decision loop, not busywork |
| Progression | Correctly two-currency, correctly scoped meta; the discovery-counter gap was the only real hole |
| Build depth | Better than the last pass, still thin on evolutions — fixed by softened thresholds |
| Replayability | Good floor (build variance, unlocks), weak ceiling (mastery/timing layer under-supported) — acceptable for a launch-scoped indie title |
| Mobile suitability | Excellent — one input, safe-area, low session length, all correctly prioritized throughout |
| Production scope | Right-sized; every recommendation in this review reuses existing systems specifically to keep it that way |
| Marketability | Strong — one genuinely rare, clippable hook, correctly identified and already central to the go-to-market plan |
| Overall design cohesion | High — GDD v2 already did the hard work of resolving contradictions; this pass closes remaining small gaps rather than finding new large ones |

**What is already genuinely strong:** the growth-speed trade-off, the character roster (all three now express the fantasy through their weapon *and* passive, not just stats), and the fact that only eating grants growth — a rule that quietly prevents the whole game from drifting into a generic bullet-heaven, and that deserves to be written down as a stated principle rather than left as an implicit consequence of `TryConsumeByBlob`.

**Biggest current design weakness:** the gap between when threats are introduced and when they become edible has no stated target — elites in particular risk staying purely dangerous for most of the run if Tier 5 arrives too late, which would quietly remove the second status-reversal beat the whole design is building toward.

**Single improvement with the largest fun payoff:** locking a tier-pacing target (Giant reachable by minute 8–10) and tuning content toward it. Everything else in this review is polish; this one is the difference between the player *unlocking* the power fantasy and actually *living inside it* for a few minutes before the run ends.

**What should absolutely not be changed:** the growth-speed trade-off, the eating-grants-mass rule, the one-input constraint, the 50%-credits-on-death rule, and the single-map/tight-boss-roster launch scope. Every one of these was hard-won in the last review round and every one is still correct.

---

*Review complete. See `GDD_v3.md` for the incorporated, implementable design document.*
