---
tags:
  - ADR
  - GameLoop
  - Combat
  - Encounters
status: Accepted
date: 2026-08-26
---

# ADR-0012 — Combat is hands-off, the plan is the game, and enemy threat is itemized and inspectable

**Status:** Accepted (2026-08-26)
**Lifecycle:** Design-only — not implemented
**Refines:** ADR-0001 §1 (the two time domains) — hardens "Resolution mostly executes what Placement set up" into "the player issues no input during Resolution at all."
**Companion:** ADR-0002 (the telegraph is the in-combat legibility contract; the Placement-phase inspect preview reuses the same covered-hex geometry), ADR-0004 §3 (the delivery patterns enemies are authored from), ADR-0008 (within-combat regen — this ADR adds the between-battle restore).
**Context:** The game mixes horde-survivor, Backpack Battles, ARPG and Into-the-Breach elements, and the mix was not cohering: combat auto-resolves but is opaque, loot picks feel weightless, and there was no identified system to build around. A design discussion (2026-08-26) worked the "who plays the fight?" axis — player-controlled tactics (ITB) at one end, spectated build-test (auto-battler) at the other — and found the project sitting in the dead middle, delivering neither payoff. This ADR records the structural commitments that pick a side, so the downstream slices (combat legibility, the loot economy, encounter content) have a fixed frame to build against. It also formally closes the "two combat modes" idea floated in `Docs/GlyphsHeroDesign/Design Docs/Ideation.md` (2026-08-20).

---

## Context

- **The "who plays the fight?" axis has no stable middle.** ITB gives the player perfect-information turn control and makes the grid the whole game; auto-battlers hand the fight to the AI and make the *build* the whole game, with the fight as a legible spectacle you learn from. As built, this project has neither: the player does not command units in combat (no ITB payoff) and the fight is not readable enough to learn from (no auto-battler payoff). "The mix feels off" is that gap.
- **The decision→consequence→next-decision loop is broken at two joints.** Every reference genre closes a tight loop: make a decision, see a legible result, make the next decision. Here the result is opaque (health bars move; nothing says which chain fired or why a build did or didn't work), and the next decision has no stakes (`LootPhase` is scripted pick-N straight to the stash — nothing competes, nothing is given up).
- **The chain system is the one genuinely distinctive asset** (CLAUDE.md calls it "the conceptual heart"; the codebase is organised around it). Whatever identity this ADR picks should make the chain the seat of mastery, not a side dish.
- **The hex grid already has real combat teeth in code** — `Line`/`Cleave`/`Aoe` deliveries (ADR-0004 §3) resolve by hex occupancy, so where a pawn stands and where enemies cluster already change outcomes. With a single player pawn the *offensive* half of that is fully live (a start hex that rakes the enemy group with a `Line`); the *defensive* half — keeping a formation out of the enemy's beams — needs two or more pawns and is deferred (Decision 4).

---

## Decisions

### 1. Combat is hands-off — the player issues no input during Resolution — Accepted

All player agency lives in **Placement** (assemble the pawn's chains from the shared stash, position the pawn) and **Loot** (the economy). **Resolution** takes no commands: it is the playout of the plan authored in Placement.

*Why:* the "who plays the fight?" axis cannot be split — an auto-fight with a few interventions reads as neither clean spectacle nor real control, and any mid-fight command re-imports the attention-split ADR-0001 §1 was written to avoid. Committing to hands-off makes the chain-build the seat of mastery, and turns the existing `CombatClock` work into the foundation rather than something to unwind. It also closes the Ideation.md "Survivor mode" (live chain-editing under pressure): a chain is a *topology*, not a slot loadout — re-authoring it against a combat timer is not a real interaction, and a two-modes split doubles the design surface for the half that doesn't work.

> One-way door. The rest of this ADR assumes it.

### 2. The plan is the seat of mastery, and Resolution owes the player a complete readout — Accepted

The design centre of gravity is the Placement decision: *assemble chains + position against a known enemy*. Every feature is judged by whether it enriches the loop **plan → watch → learn → re-plan** — specifically the moment the player reshapes a chain in response to the last fight. **Making that readout legible is the current development priority**, ahead of the economy and squad expansion.

Because Decision 1 makes *watching* the only way the player learns, Resolution is a **legibility contract**, not a polish target. The player is owed attribution: which chain fired, for how much, and why a build did or did not perform. Minimum bar:

- a per-pawn attack indicator on the `CombatClock` cadence that flashes on trigger, with contributing modifiers marked;
- damage numbers attributed (by colour/source) to the chain that caused them;
- the covered hexes of each attack shown on the grid as it fires (the ADR-0002 telegraph, extended to a post-hoc read);
- a **post-combat recap** generated from the resolved `ItemChain`s (phrased via the existing `DeliverySentence`), e.g. "Caster: 168 dmg / 12 fires (1 crit, +14). Priest healed 36 before it died fire 7."

**Variance is allowed; unexplained variance is not.** This contract is about legibility, not determinism — a roll (crit, a random target pick) is fine *provided the recap shows what rolled*, Backpack-Battles-style. ADR-0001 §6's deterministic tick is about a frame-rate-independent, unit-testable simulation; a seeded roll is part of the snapshot and does not break that. The requirement here is narrower: whatever happens in the fight must be reconstructable afterward.

*Why:* an unreadable fight breaks the loop at the *learn* step, and no economy or content investment downstream can compensate — a player cannot value a loot choice they can't connect to an outcome. This is the current top blocker; naming it a contract makes it a prerequisite for the loot work, not a parallel nicety.

### 3. Enemy threat is authored as itemization and read by inspection — not scripted behaviour or stat inflation — Accepted

Enemy pawns are built with the **same chain system** as the player pawn. An encounter's difficulty and character come from its **enemy loadout composition** (`EncounterConfig.enemies` — the PawnConfigs and their chains), not from bespoke AI or inflated numbers. Every threat is legible by **inspecting an enemy's resolved chains** — card + rules-text + hover-to-visualise the attack's covered hexes on the grid — and inspection is available **during Placement**, before the player commits.

*Why:* it collapses "learn the enemy" and "learn your own builds" into one skill, so the chain system is genuinely *the* system, used on both sides. With a single player pawn (Decision 4), reading the enemy *group* is the primary planning input — the whole encounter puzzle is "given this loadout, where do I start and what do I chain." It delivers ITB's perfect-information planning ("see what the enemy is equipped to do") **without** a combat-AI investment: `Line`/`Cleave` danger is player-facing — the enemy walks up and fires, and whether its beam catches the player pawn is a consequence of the player's start hex and the monotone-closing path (ADR-0001 §3). The hover-visualise preview reuses `DeliveryResolver.CoveredHexes` — the same geometry the ADR-0002 telegraph draws — so player-side and enemy-side legibility are one mechanism.

> Debt: every enemy now needs a hand-authored chain (content cost), and the inspect UI (card / rules-text / hover-viz) moves onto the critical path.

### 4. A run starts with a single player pawn; the squad is a deferred expansion axis — Accepted

The current build fields **one player pawn** against a varying-size enemy group. Additional pawns — and with them squad-level item allocation and defensive formation — are **deferred until hex-grid interaction exists** (pawn effects, terrain manipulation, board-changing payloads).

*Why:* squad decisions are hex-grid-interaction decisions. Until a second pawn can *do* something for the first — an aura, a body-block that matters, a terrain effect — it adds management overhead without a matching decision, and it splits attention away from the legibility work that is the current priority (Decision 2). One pawn against a varying enemy count still exercises the offensive half of the grid: the start hex plus the monotone-closing path (ADR-0001 §3) decide which enemies a `Line` rakes and whether a `Cleave` catches a cluster, so positioning is already a real choice.

**Intended direction (not a commitment):** a growing squad remains the intended long-term progression spine — each added pawn re-opening the allocation puzzle, difficulty riding the pawn count. Revisit when hex-grid interaction lands (see Open questions for the trigger).

Playtest target (tuning, exempt from the acceptance gate): ~7 encounters with varying enemy count.

### 5. No permadeath; the run ends on the first loss; pawns are restored between battles — Accepted (interim rule; the health economy is an open question)

A run continues as long as the player's squad **wins each battle**, and ends on the first battle it loses (the existing `OnPlayerDefeated` → GameOver path). Between battles, every pawn is **restored to combat-ready** — interim rule: **full Health and full resource pools**. Builds (inventory contents) persist across encounters as they do today; only the pools reset.

*Why (for the interim):* it keeps every encounter an **independent, tunable checkpoint** — enemy loadout *N* is balanced against the player's expected power at point *N*, not against a dwindling health bar carried across the run. Committing to an attrition model before the fight even *reads* clearly (Decision 2) would be balancing on sand.

**Explicitly unresolved:** whether full-heal is the permanent rule, and the shape of the **health economy** as a whole — attrition between battles, sustain/lifesteal as a build axis, downed-pawn revive costs once a squad exists. This is a deferred design pass, not a settled decision (see Open questions).

---

## Worked example

A mid-run encounter. The player fields one pawn.

- **Caster** — Reach 3, Health 120. Chain: wand + Converter(Delivery→`Line`) → `Line`, ~14 dmg/fire, hits every enemy between Caster and its target.

Enemy loadout, authored as itemization (Decision 3):

- **3× Grunt** — Reach 1, Health 30. Melee `Single`, ~6 dmg. Approach in a loose group.
- **1× Priest** — Reach 2, Health 45. Friendly-affinity payload that heals the lowest-HP ally each tick. Role: the Grunts don't stay down while the Priest lives.

1. **Placement — inspect (D3).** Hovering the Priest's chain shows a friendly-affinity heal pulse → "kill this first or the Grunts never drop." Hovering a Grunt: Reach-1 `Single`, harmless until adjacent. Hovering Caster's own chain lights the `Line` from a candidate start hex through the enemy cluster.
2. **Placement — plan (D1, D2, D4).** One pawn, one decision that matters: *where to start*. Head-on, Caster's `Line` only catches the nearest Grunt. From the flank, the monotone-closing path (ADR-0001 §3) lines the `Line` up through 2 Grunts **and** the Priest behind them. The player picks the flank hex and confirms. No further input is possible (D1).
3. **Resolution — hands-off but legible (D1, D2).** Caster closes; its indicator flashes each `Line` fire; damage numbers in its colour land on 2 Grunts + the Priest at once (the flank start paying off). A crit rolls on fire 3 — the number shows doubled, and the recap will log it (D2: variance shown, not hidden). The Priest's heal indicator flashes each tick; the player watches the race — is Caster out-damaging the heal?
4. **Recap (D2).** "Caster: 168 dmg / 12 fires (1 crit, +14). Priest healed 36 total, died fire 7 → Grunts dropped within 3 fires after." Lesson: flanking to fold the Priest into the `Line` was the win; head-on loses the damage race.
5. **Next Placement — restore (D5 interim).** Caster starts the next encounter at full 120 regardless of ending this one at 74. The next loadout (5 enemies, or 2 Priests) is tuned to Caster's expected power at that point, not its leftover HP.

Every Decision 1–5 is exercised and none contradicts another: hands-off Resolution (D1) is fair because the fight is legible (D2) and the threat was readable before commit (D3); with one pawn the meaningful choice is the start hex driving offensive `Line` geometry (D4's rationale for why one pawn still works); the crit is surfaced, not hidden (D2); and the lesson transfers to the next fight because the pool resets (D5).

## Considered and rejected

- **Player-controlled turn-based combat (ITB-forward).** Shelves the chain system as the seat of mastery, discards the `CombatClock` auto-resolution investment, and demotes the chain-build to a pre-run loadout. Honest, but a different game.
- **Auto-combat with 1–2 "command interventions" per fight.** Reads as neither a clean spectacle nor real control; mid-fight commands re-import the attention-split ADR-0001 §1 avoided.
- **Survivor-style live chain editing** (Ideation.md 2026-08-20, the second "combat mode"). A chain is a topology; re-authoring it under time pressure is not a real interaction. The two-modes split doubles the design surface for a mode that does not work.
- **Scripted enemy behaviour / stat-inflation difficulty.** Splits the vocabulary in two (player builds vs. enemy rules), makes threats unreadable, and needs bespoke AI per encounter.
- **Requiring combat to be RNG-free forever.** Not required — the contract is legibility (Decision 2). Backpack Battles has variance and stays understandable because its log shows what rolled; the same standard applies here.

### Deferred rather than rejected

- **Starting with two pawns now.** The rationale is sound (the grid's defensive teeth, the recurring allocation decision) but the supporting features — pawn effects, terrain interaction — don't exist yet, so a second pawn would be overhead without a decision. Deferred to squad expansion (Decision 4), not rejected.
- **Inter-battle HP attrition / heal-as-a-spent-resource.** Not rejected — folded into the deferred health-economy pass (Decision 5). The interim rule is full heal.
- **A fixed (non-growing) squad.** The growing squad stays the *intended* spine (Decision 4); only its timing is deferred.

## Deferred (designed, not built)

- **The combat-legibility slice** that Decision 2 commits to: the Sigil renderer (`Docs/GlyphsHeroDesign/.../Sigil_Design_Handoff.md`), the `CombatClock`-cadence attack HUD, source-attributed damage numbers, hover-to-visualise (reuses `DeliveryResolver.CoveredHexes`), and the post-combat recap (phrased via `DeliverySentence`). The current priority; its own issue(s).
- **Squad expansion** (Decision 4): the second pawn onward, squad-level item allocation, defensive formation. Gated on hex-grid interaction (pawn effects, terrain manipulation, board-changing payloads).
- **The health economy** (Decision 5): whether full-heal persists; attrition between battles; sustain/lifesteal as a build axis; downed-pawn revive costs once a squad exists. A design pass with a review trigger.
- **The loot economy** — smaller per-pawn grids, grid-extension tiles, salvage → currency, a flat income floor, and the split between a currency **shop** (fungible ingredients) and a non-fungible **reward track** (relics, unbuyable uniques, grid tiles, new pawns). Its own ADR; `Ideation.md` "Loot Agency" is the seed.
- **The item value table** — a canonical item power score, feeding salvage value, shop price, and encounter-budget authoring. Its own ADR; blocks tuned content, not the loop.
- **The relic system** — right-click-to-apply items that permanently confer a `PawnEffect` (`PawnConfig.pawnEffects` is a stubbed hook; `Ideation.md` has the interaction).

## Open questions

- **When does squad expansion begin?** Decision 4 gates it on "hex-grid interaction exists" — name the concrete bar (which pawn-effect / terrain / board-payload features must ship first for a second pawn to carry a real decision).
- **The health-economy review.** Trigger: after the legibility slice lands and/or when squad expansion begins. Question set: is full-heal (Decision 5) permanent; does attrition between battles become a mechanic; is sustain a build axis; do downed pawns cost something to recover.
- **Playtest calibration** (tuning): ~7 encounters, varying enemy count — does one pawn against a group stay a legible, interesting planning puzzle for that long, or does it flatten before squad expansion is ready.

## Consequences

- **Positive:** one clear seat of mastery (the plan); the `CombatClock` work becomes foundational rather than legacy; enemy authoring stays in-engine and in one vocabulary; starting with one pawn keeps every bit of attention on the legibility priority; each fight is an independent, tunable checkpoint; the "two combat modes" fork is closed.
- **Negative / debts:** combat legibility is promoted from polish to a **blocking dependency** — the Sigil/HUD/recap slice must land before the loot economy can be validated. The vertical slice will **not** demonstrate the full intended loop — the grid's defensive/allocation depth stays unexercised until squad expansion, so early playtest feedback is about the single-pawn puzzle only. The interim full-heal (Decision 5) is a known placeholder; content tuned against it may need rebalancing when the health-economy pass lands. Enemy-as-itemization adds a per-enemy chain-authoring cost and puts the inspect UI on the critical path.
