# Blob.io — Independent Investment Board Evaluation

> Independent game investment board · July 2026
> Documents reviewed (in order): `README.md`, `CLAUDE.md`, `GDD.md`, `SPRINT_ROADMAP.md`, `FABLE_INCELEME.md`, `ADVISORY_BOARD.md`
> Mandate: evaluate the **product and game design only**. Technical findings in the documents (code quality, bugs, optimization) were disregarded except where they reveal product-level facts (content volume, internal contradictions). Question to answer: **"Is this game fundamentally worth building?"**
> Constraint context: 3-person indie team, limited budget, PC + Mobile planned.

---

## Part I — Individual Board Member Opinions

---

### 1. Successful Indie Game Director

**First impression:** The pitch line works. "Eat everything, grow, get slower, get hunted" is communicable in one breath — that's rarer than people think. But then I read four documents that describe two different games, and my enthusiasm cooled.

**Hook:** Real. The size-flip moment — the thing that hunted you becomes food — is a genuine emotional beat that Vampire Survivors does not have. **Originality:** The genre shell is derivative by design (VS-like), and the docs admit it. The growth/slowdown twist is the only original element, and it's a good one — but it is *one* element. **Core loop:** Sound on paper; the four nested loops are textbook and correctly structured. **Long-term fun:** Depends entirely on whether eating stays the star. The skill list (regen, max HP, magnet, shield, score multiplier) is the most generic part of the entire GDD — I could paste it into any VS-like unchanged. **Biggest strength:** A one-sentence fantasy with a visible, physical progression system. **Biggest weakness:** The documents don't agree on what game this is — 20–30 min vs 5–10 min runs, mobile-first vs PC-first, pixel art vs 2.5D. A team that hasn't decided what it's making cannot be evaluated as if it had. **Biggest unknown:** Is the eat-grow-slow tension actually fun for 12+ minutes? No playtest data exists in any document. **Biggest risk:** Shipping a competent VS clone where the growth mechanic is cosmetic.

**Verdict:** The idea clears my bar. The project definition does not — yet.

---

### 2. Senior Gameplay Designer

**First impression:** Strong spine, soft muscles. **Hook:** The `speed = 1/√tier` trade-off is the best single design decision in these documents — it converts progression into tension, which is the exact thing this genre chronically lacks. Genuinely good.

**Core loop:** Here is my problem. The game has **two competing verbs**: *eating* (the differentiator) and *auto-firing weapons* (the genre convention). The GDD's content — three weapon-holding characters, weapon upgrades, a weapon-unlock purchase — invests heavily in the verb the game *shares with every competitor*, while the verb it *owns* gets a growth formula and little else. The skill roster contains not a single eating-themed skill beyond pull-radius utilities. If a designer removed the growth system from this GDD, 80% of the document would still function. That is the tell. **Long-term fun potential:** Moderate. Build variety comes from a generic skill list; the evolution system is mentioned but undesigned (the GDD's evolution row is literally blank). **Biggest strength:** Risk/reward is structural, not bolted on. **Biggest weakness:** The differentiator is under-designed relative to the conventions. **Biggest unknown:** Hazard rule (bigger things hurt you) — present in data, absent in design detail. It's the natural completion of the fantasy and it's ambiguous whether the team knows that. **Biggest risk:** The fun test hasn't been run. Everything else is downstream of it.

**Fundamentally worth building?** Yes — *if* the eating verb is the design's center of gravity. As documented today, it isn't.

---

### 3. Cross-platform Game Designer (PC + Mobile)

**First impression:** The one-input constraint is the smartest cross-platform decision here; it means the *game design* ports cleanly. The *business design* does not.

**Assessment:** Session length is the fault line. The docs waver between 20–30 minute sessions (PC-comfortable, mobile-hostile) and 5–10 minute runs (mobile-native). You cannot tune one pacing curve for both. VS gets away with 30-minute runs on mobile because it arrived with a massive PC-earned audience; this game will not have that luxury. The monetization docs make the split worse: cosmetic-F2P on mobile and $4.99 premium on PC are two different economies, two different UX layers, two different update cadences — for three people.

**Does this game truly benefit from releasing on both PC and Mobile?**
Eventually, yes — the design (one input, short sessions, safe-area awareness) is unusually port-friendly, and the genre has proven demand on both.

**Should it initially focus on a single platform?**
**Unambiguously yes.** Ship one platform, validate, port with revenue and data. The documents themselves cannot agree on which platform is first (CLAUDE.md says mobile-first; the GDD timeline ships Steam Early Access *before* mobile). That contradiction alone tells me the team hasn't made the decision, and dual-platform ambition without a decision is how three-person teams die. My read of the docs' own logic points PC-first; but the decision matters more than the direction.

**Biggest risk:** Building two economies before validating one game.

---

### 4. F2P Economy & Monetization Expert

**First impression:** I'll be blunt: the mobile monetization section is a paragraph wearing a strategy's clothes.

**Monetization potential:** On paper: cosmetic-only IAP + optional rewarded ads + zero pay-to-win. Admirable values. As a *business*: cosmetic-only monetization works when you have (a) enormous organic scale, (b) strong character/identity attachment, or (c) social visibility of cosmetics. A single-player blob has none of the three by default. Rewarded ads on re-rolls is the one genuinely good mechanic here — it monetizes the exact moment of highest player desire. But one good ad placement is not an economy. There is no LTV model, no UA budget, no publisher strategy, no ARPDAU hypothesis anywhere in these documents. Mobile F2P without paid acquisition is a lottery ticket, and this team has no stated marketing spend. **PC premium at $4.99:** Realistic, honest, and modest — it caps the upside but matches the product. **The Market/meta economy:** Structurally fine (VS-proven), though the currency proliferation (six numeric systems, flagged in the team's own advisory doc) tells me economy design is being accreted, not planned.

**Biggest weakness:** The revenue plan assumes distribution that nothing in the documents earns. **Monetization confidence: low.** The game can make money; the *plan* as written will not.

---

### 5. LiveOps & Retention Expert

**First impression:** The retention *skeleton* is genre-proven and correctly assembled. The retention *flesh* doesn't exist yet.

**Retention potential:** The loops are right: death preserves 50% of credits (excellent — converts failure into progress), meta unlocks gate future variety, Grimoire feeds collection psychology, NG+ extends endgame. This is a faithful transcription of the VS retention stack, and that stack works. My concern is arithmetic: the documents' own status review says current content is *one enemy, one consumable, one wave*. The GDD's retention features (weather, NG+, Grimoire, 4 maps, evolutions) are all unbuilt. Retention is content-hungry, and this team has one artist. D1 retention will come from the core loop; D7 from build variety; D30 from content volume — and the third is where a three-person team will starve. **Biggest strength:** "Death is progress" is correctly designed. **Biggest unknown:** Content production velocity — no document states how fast the team can add an enemy type or a map. **Biggest risk:** A great first hour and an empty second week.

---

### 6. User Acquisition & Growth Lead

**First impression:** There is exactly one marketable asset in this entire GDD, and the documents don't seem to know it.

**Virality potential:** Moderate-to-low by default, with one exception: the size-flip clip. *Tiny blob flees cop → cut → giant blob eats cop* is a perfect 10-second short — legible without sound, satisfying without context, endlessly remixable with different objects. Hole.io built its entire UA on exactly this asset shape. Nothing else in the GDD is clippable: upgrade cards, weather modifiers, and meta shops do not travel. **Market position / discoverability:** On Steam, the VS-like tag ecosystem gives decent organic discoverability — but it's 2026 and the tag is saturated; a growth gimmick must be visible in the *capsule art and first trailer second*, not in a feature list. On mobile, organic discoverability is effectively zero without spend. **Biggest weakness:** No community plan, no wishlisting strategy, no content-creator angle anywhere in the documents. For an indie in this genre, marketing *is* development. **Biggest unknown:** Whether the art direction (unresolved between pixel art and 2.5D) can make the blob *charismatic*. Virality needs a face.

---

### 7. Venture Capital Partner (Games)

**First impression:** I've seen this deck a hundred times: proven genre + one twist + underdefined business. The twist is better than average. The definition is worse than average.

**What I like:** Genre with demonstrated exits at indie scale; a differentiator that is *mechanical*, not cosmetic; a team that has already built a working core loop (real de-risking — most pitches at this stage are paper); honest internal documentation that names its own weaknesses (rare, and I weight it positively). **What I don't like:** The project cannot state its own session length, platform order, or art direction — three documents, three answers. No fun-validation data. No distribution plan. The mobile F2P ambition is unfunded and unstaffed. Comparable-title math: a competent VS-like on Steam without marketing does low four figures in units; with a Next Fest hit and streamer pickup, low-to-mid five figures. The upside case requires the growth hook to carry a trailer — plausible, unproven.

**Biggest risk:** Identity drift — the team builds the generic 80% first (it's easier) and the differentiating 20% never gets its depth.

**Decision: Keep watching.**
Concretely: I re-enter this conversation when the team shows (1) one locked product definition (session length, platform, art) and (2) a 10-minute prototype where testers restart unprompted. If both arrive, this converts to a small pre-seed — the working core and honest docs earn that. Today, it's a pass on the check but not on the team.

---

### 8. Experienced Indie Game Developer

**First impression:** As someone who has shipped with a three-person team: the GDD is a four-person-year document attached to a three-person team with no stated funding runway.

**Scope reality:** 3 characters, 4+ bosses with unique patterns, 2+ maps, weather system, NG+, Grimoire, evolution system, two platform economies. The team's own status review says content today is one enemy and one consumable. That gap is not a red flag about talent — the built systems show discipline — it's a red flag about *scope honesty*. The GDD's own 18-month timeline is optimistic even before marketing time is counted, and marketing will eat 20% of someone's week whether planned or not. **Biggest strength:** The core is *built and playable*. Most indie pitches can't say that; it moves this project from "idea" to "early prototype" on the risk curve. **Biggest weakness:** No document addresses the team's runway, availability (full-time? nights?), or content production rate — the three numbers that actually determine whether this ships. **Biggest unknown:** Same. I cannot assess feasibility without them, so I log it as a risk, as instructed. **Worth building?** Yes, at half the documented scope. At full scope, this is a project that gets to 70% and stalls — I have watched that movie.

---

### 9. Product Manager

**First impression:** This is not one product spec. It is three drafts of a product interleaved.

**Documented contradictions (product-level, not technical):** Session length (20–30 min vs 5–10 min), platform priority (mobile-first vs Steam-first timeline), art direction (16-bit pixel art vs 2.5D), engine note aside. The GDD's own character meta-progression table lists four characters — Mira, Dorian, Lyra, Bastian — who do not exist anywhere else in the project; that is template residue in a v1.0 document, and it tells me the GDD was not fully proofread against itself. **What's good:** An MVP definition exists (`GDD.md`) and it is *correctly shaped* — "playable + finishable once + replayable" is the right acceptance test. The open-questions section with a decision log convention is genuinely good practice. The advisory document shows the team can absorb hard feedback. **Biggest weakness:** No single source of truth. A stranger reading these documents cannot answer "how long is a run?" — and if the docs can't answer it, neither can the build. **Biggest risk:** Every sprint executed before the contradictions are resolved has a meaningful probability of being rework. **Worth building?** The product inside the contradiction is worth building. The contradiction itself must be resolved *first* — that's a week of decisions, not a year of work, which is why I don't treat it as disqualifying.

---

### 10. Behavioral Psychologist (Player Motivation)

**First impression:** This design accidentally or deliberately stacks three of the strongest motivation primitives in games, and I want to be precise about why that matters.

**Motivational analysis:** (1) **Visible competence growth** — the player's power is rendered as literal physical size. This is the most legible progress feedback that exists; no bar, no number, the *avatar itself* is the progress bar. Katamari and Agar.io proved the compulsion. (2) **Competence-threat oscillation** — the slowdown mechanic re-introduces vulnerability at the exact moment of mastery. This prevents the boredom plateau that kills VS-likes in the late run. It is also the design's biggest psychological risk: if mis-tuned, the player experiences their reward *as punishment*. The documents show no awareness of how narrow that tuning window is. (3) **Loss-aversion buffering** — keeping 50% of credits on death reframes failure as partial success, which is the single most important retention decision in the roguelite genre. Correctly made here. **Hook (psychological):** The predator-prey inversion. The moment a former threat becomes food is a *status reversal* — one of the most reliably pleasurable events across all media. The game should be organized around producing that moment repeatedly; the documents under-weight it. **Biggest weakness:** The generic skill list offers *statistical* choices, not *identity* choices — players bond with builds that express a fantasy, not builds that add 10 HP. **Biggest unknown:** Whether the moment-to-moment eating feedback (the 30-second loop) can carry sessions before the skill system deepens. **Worth building?** From a motivation standpoint, yes — emphatically. The primitives are right. The execution risk is entirely in tuning.

---

## Part II — Internal Board Discussion (Summary)

The board converged quickly on three points and argued about two.

**Consensus:**
1. The **hook is real and rare** — a mechanical differentiator with a built-in viral asset (the size-flip). Nobody on the board called the core idea weak.
2. The **project definition is not investable in its current state** — the session-length/platform/art contradictions mean the team is, formally, pitching multiple games at once. This was the single most-cited weakness across all ten members.
3. The **generic layer outweighs the original layer in the documents** — the skill list, weapon systems, and meta shop are competent genre transcription; the eating fantasy that justifies the game's existence receives the least design attention of any system.

**Disagreements:**
- The **Gameplay Designer and Psychologist** argued the fun risk is low ("the primitives are proven") while the **Indie Director and VC** insisted no fun claim survives contact with playtesters and demanded prototype data. The board sided with demanding data — proven primitives lower the risk, they do not retire it.
- The **F2P Expert** argued the mobile plan is bad enough to cap the company's upside; the **Cross-platform Designer** countered that the mobile plan simply isn't *for now*, and judged the game on its port-friendliness. Final report scores monetization on the plan as written (low), while noting the structural potential (higher).

---

## Part III — Final Board Report

### Executive Summary

Blob.io is a **good idea wrapped in an unresolved product definition**. The core fantasy — devour, grow, slow, get hunted, then devour your hunters — is a genuine mechanical differentiator in a proven, still-commercial genre, and it carries a built-in marketing asset (the predator-prey flip) that most indie games would kill for. The team has a working core loop, which places it ahead of the vast majority of pitches at this stage.

Against that: the project's own documents disagree on session length, platform priority, and art direction; the differentiating mechanic is the *least designed* system in the GDD while the generic genre-standard systems are the most designed; the mobile monetization plan does not survive contact with F2P economics; and no playtest evidence exists for the central fun hypothesis. The documented scope is roughly double what a three-person team can credibly ship.

**Is this game fundamentally worth building? Yes** — the board is unanimous that the core concept clears the bar. But it is worth building *only* as the game the documents haven't quite committed to yet: a single-platform, tightly scoped run-based game where eating is the star. The board's confidence is in the idea and the demonstrated ability to build; its skepticism is reserved for the current definition and the business plan.

### Scores

| Dimension | Score | Note |
|---|---|---|
| **Overall** | **6 / 10** | Good concept, working core, unresolved definition |
| **Originality** | **6 / 10** | One real mechanical twist on a deliberately derivative shell; the twist is under-designed |
| **Fun Potential** | **7 / 10** | Stacked proven motivation primitives; unvalidated by any playtest; narrow tuning window on the slowdown |
| **Commercial Potential** | **5 / 10** | Saturated genre, no distribution plan; ceiling depends entirely on the hook surviving into the trailer |
| **Monetization Confidence** | **4 / 10** | PC premium is realistic but modest; mobile F2P plan as written is not credible for this team size |
| **Investment Readiness** | **3 / 10** | Contradictory product definition, no validation data, no runway/marketing plan in any document |

### The Three Questions

**1. Would this stand out in today's indie market?**
**Conditionally — in motion, yes; at rest, no.** In a trailer or a 10-second clip, a blob that physically outgrows its predators is instantly distinguishable from every other VS-like — that's rare and valuable. In a storefront screenshot or a feature-list description, this reads as "another survivor game," and the genre tag is saturated in 2026. The game stands out exactly to the degree that its marketing leads with the size-flip and its design makes eating (not weapons) the star. If the generic 80% of the GDD becomes the game, it will not stand out at all.

**2. Would you personally invest your own money into this game?**
**Not today.** The board's VC decision stands for the group: **Keep watching.** The concept and the working core earn a seat at the table; the unresolved product identity and absent validation data mean a check written today is a bet on a coin that hasn't been flipped. The re-entry conditions are cheap and fast: one locked product definition and one prototype session where strangers restart unprompted. If those arrive — and the board believes this team can produce them within weeks, not months — several members indicated they would support a small pre-seed. That is a genuinely positive signal by this board's standards; most projects evaluated at this stage receive an unconditional pass.

**3. If you had to spend the next 3–5 years building only one game, would you choose this one?**
**No — and the team shouldn't want it to be.** This is a 12–18 month product with a 1–2 year post-launch content tail, not a half-decade flagship. Its genre window is open *now*; a five-year build ships into a market that has moved on. That is not a criticism — correctly scoped, it is this game's greatest commercial virtue: it can be finished, shipped, and either grown (the VS path: launch small, expand for years on revenue) or concluded, by a small team, without betting the studio's whole decade. The honest version of a "yes" here: this is the right *first* game for a three-person studio — a scoped, provable bet that builds audience and capability — not the right *only* game. A team that would need five years to ship this GDD is a team building the wrong version of it.

---

*Evaluation complete. Per the engagement terms, this report contains no redesign, no new mechanics, and no improvement plan — advisory follow-up available on request as a separate exercise.*
