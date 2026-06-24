---
tags:
  - ADR
  - Combat
  - Movement
  - Range
status: Accepted
date: 2026-06-21
---

# ADR-0001 — Range as a pawn stat, monotone-closing movement, and a fixed combat tick

**Status:** Accepted (2026-06-21)
**Lifecycle:** Implemented (movement on `CombatClock`, Candidate #5 + #7)
**Amended-by:** [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]] §2 (2026-06-23) —
Reach is one uniform pawn stat: §2b (per-pattern range behaviour) **withdrawn**, §3 (close-to-minimum
across weapons) **amended** to close-to-Reach. The Decisions below are left as-decided-then; ADR-0004 is
the authority on Reach now.
**Supersedes / refines:** the "under consideration" notes in [[Pawn#Combat Structure]] and the
"Movement → central planner" item in [[KNOWN_ISSUES]].
**Companion:** [[Architecture Review]] Candidate #5 (re-scoped by this ADR).

This record bundles several tightly-coupled decisions because they were resolved together in one
design session and only make sense as a set. Each is stated with its rationale. The session was a
deliberate design grilling whose explicit goal was to decide a solid range/movement model *before*
committing to the Candidate #5 "central per-tick movement planner" rework — i.e. to find out whether
the heavy rework was even necessary. It was not, in its heavy form; see Decision 8.

---

## Context

- Combat borrows a **time-based auto-battler** resolution model from Backpack Battles (weapons fire on
  `Timer`s), but the game also wants **deliberate hex positioning** (Into the Breach / TFT lineage).
  These pull in opposite directions: turn-based repositioning vs. real-time firing.
- As built, **range is `Payload.Range` (an int) and movement is dumb gap-closing**: a pawn walks toward
  the nearest enemy until that enemy is within `ResolveMaxRange` (the **max** range across the pawn's
  chains), then fires. Positioning carried no strategic weight at resolution time, and `ResolveMaxRange`
  taking the max gave multi-weapon pawns an incoherent movement intent (a melee weapon on a long-range
  pawn never fires). This contradicted [[Pawn]]'s "kite off the *smallest* range" note — code and doc
  disagreed on a core rule.
- Movement ran on **per-pawn async `Timer`s**, so each pawn decided against stale world state; the
  `CombatCoordinator` carried `_reservedHexes`/`_claimedHexes` reservation sets and an on-arrival
  re-check guard purely to compensate. This stale-state class already caused one combat-breaking bug
  (2026-06-12).

---

## Decisions

### 1. Two combat time domains — Accepted
Combat is split into **Placement (turn-based)** and **Resolution (real-time)**, each its own time
domain. *Why:* this is the only structure that lets deliberate positioning and real-time chain firing
coexist without fighting for the player's attention. The strategic weight of positioning lives in
Placement (the three simultaneous reads — Aura, Terrain↔Aura affinity, weapon range); Resolution
mostly executes what Placement set up.

### 2. Range is a **pawn stat**, not a weapon stat — Accepted
A pawn has a single **range** stat that acts as the **ceiling for range-scaling deliveries**. *Why:*
this is a wand-builder (Noita lineage) — the pawn is the caster, items are spell components — so "reach"
naturally belongs to the combatant, not the component. It gives one coherent range per pawn (kills the
multi-weapon incoherence), and makes range a **roster-pace archetype identity** (sniper vs. brawler
pawns to recruit), consistent with the Pacing Layers table.

This decision forces three commitments, accepted on purpose:
- **2a. Weapons lose their range identity.** Weapons vary by payload, shape, and economy — not reach.
- **2b. Delivery patterns split by range behaviour.** *Range-scaling* deliveries (Projectile, Beam,
  Arc) reach out to the pawn's range; *range-fixed* deliveries (Adjacent, Dash) are intrinsically
  range-1 regardless of the pawn's range. A Converter that turns Adjacent→Projectile therefore
  *unlocks the pawn's range for that weapon* — an intended build moment.
  > _Withdrawn by ADR-0004 §2 — see the **Amended-by** header._
- **2c. Range is capped and priced.** Range does **not** scale infinitely and is **not** a freely
  Amplifier-pumpable output stat (that would be monotonic dominance — "+range" is otherwise always
  correct: hit first, get hit last, win the closing race). It is pumpable only via **passive item
  stats that consume inventory-grid space**, so buying range costs chain-building real estate — a real
  opportunity cost. The exact pricing is owned by the future **balancing/value table** (see Open
  Questions); this ADR only fixes that range is the kind of stat that table must price as expensive +
  hard-capped.

> Reverses [[Weapon]]'s "Range under Output Economy" framing (range was listed beside Damage as a
> pumpable output stat). Range is now a pawn stat with a fixed/capped value, reshaped by Converters,
> not pumped by Amplifiers.

### 3. Movement rule — monotone closing to minimum effective reach — Accepted
During Resolution a pawn closes to the **minimum effective reach across its active weapons** (so *all*
of them can fire), with the pawn's range stat as the ceiling for range-scaling deliveries. Movement is
**monotone closing only — no kiting/retreat in v1.**
> _Amended by ADR-0004 §2 — see the **Amended-by** header: Reach is uniform, so a pawn closes to the
> pawn's Reach; the "across active weapons" clause is dropped._

*Why:* closing-to-minimum guarantees no weapon is
dead weight and gives one unambiguous movement target per pawn; the player authors the engagement
profile (sniper vs. brawler) purely through the weapon mix. Monotone closing eliminates the
mutual-kite **oscillation deadlock** class entirely (two ranged units can't back-pedal forever), so no
stalemate-breaker is needed yet.

> Resolves the code/doc conflict: the doc's "smallest range" intuition was right for *movement*; the
> code's `ResolveMaxRange` max was the bug.

### 4. Contested-hex arbitration — closest-to-target wins — Accepted
When two pawns want the same hex on the same tick, the pawn **nearest its approach-target** takes it;
ties broken by a **stable pawn id** for determinism. *Why:* reads as "the more committed pawn gets
priority," and being deterministic it replaces the nondeterministic reservation bookkeeping.

### 5. Blockage handling — blocked pawn idles — Accepted
If a pawn cannot reach its min-range (allies/terrain block the only path) it **idles and re-evaluates
next tick**. Allies are **passable-but-costly traversal** so a pawn routes *around* when a longer path
exists; the destination hex must be **empty**. No swap/shuffle logic in v1. *Why:* in a game whose
thesis is that positioning weight lives in deliberate placement, a unit walled off by your own bad
placement is **legitimate, legible punishment** (ITB makes bad placement hurt) — not a bug to engineer
away. Combat still terminates via the wipe resolver even if a unit idles.

> **Owed debt:** a blocked/idle unit must *visibly read as blocked* (telegraphing), or it looks broken.

### 6. Combat runs on a **fixed tick**, not the frame rate — Accepted
Resolution advances on a discrete **`CombatClock`** decoupled from `Time.deltaTime`, using an
accumulator that runs **0..N ticks per frame** (frame-rate independent). The target semantics are
**read-then-write within a tick**: gather every pawn's decision against the frozen snapshot, then apply
them — so a death and the re-targets that react to it all resolve on the *same* tick, simultaneously,
not at random frame offsets. *Why:* a "build it, then watch it resolve" game must be **deterministic
and fair** (same setup → same outcome regardless of hardware/frame rate), and a fixed tick makes the
simulation a pure `(snapshot, tick) → snapshot` function that is **unit-testable** (advance N ticks,
assert state). It also lets the **view interpolate between ticks**, retiring the snap-then-lerp smell
in `Pawn.Update`.

### 7. Per-pawn speed is a move-readiness accumulator — Accepted
Movement speed is expressed as readiness accrued per tick (`dt × speed`), not as a `Timer` duration. A
pawn commits a step (carrying surplus) once it has banked the next hex's terrain cost. *Why:* this is
what lets movement live on the shared `CombatClock` with no per-pawn timer and no reservation sets.

### 8. Candidate #5 is re-scoped small; the timer migration is split out — Accepted
The grilling's payoff: the heavy "central planner" was **not necessary**. With monotone closing
(Decision 3), single-unit A* against a snapshot, and closest-wins arbitration (Decision 4), the planner
collapses to a **small pure step-rule** in a per-tick `foreach`.
- **Candidate #5 (this rework):** introduce the project-side `CombatClock` heartbeat and put **movement**
  on it (readiness accumulator + snapshot step-rule), **deleting** `_reservedHexes`, the
  reservation use of `_claimedHexes`, and the on-arrival re-check guard. **Attacks stay on the Utility
  `Timer` for now** — movement (5a) needs no `Timer` at all.
- **Candidate #7 (new, queued):** migrate **attack firing + reactor events** off the Utility `Timer`
  onto `CombatClock`, achieving full read-then-write simultaneity (Decision 6). `Timer.Tick(interval)`
  is already delta-parameterised, so the migration is cheap, and it **subsumes/cleans up** whatever
  interim timer logic #5 introduces.

*Why the split:* keeps one-candidate-per-session context budget; movement-vs-movement is fully synced
immediately, and the death→retarget reaction still synchronises (reactions run on the tick) even while
the killing attack is still a frame-based timer. Only attack-vs-attack firing stays unsynced until #7.

> The Utility `Timer`/`TimerTicker` is a **git submodule**, and its player-loop driver ticks *all*
> timers (UI included). Do **not** globally re-route it to a fixed tick (sweeps up UI timers + edits a
> separate repo). The `CombatClock` is **project-side**; combat subscribes to it.

---

## Deferred (designed, not yet built)

- **Cooperative single-hop sidestep (movement v2).** When an ally blocks a pawn's only path, the
  blocked pawn may ask the ally to sidestep. Safe spec, written down so a future session doesn't
  rediscover the trap: **single-hop only** (no cascade/conga-line — that *is* the heavy planner), and
  the ally sidesteps **only to a hex equally valid on all three placement reads** (range *and* Aura
  *and* terrain affinity — a sidestep that keeps firing range but drops the ally off its lava/aura tile
  is a silent combat downgrade the player didn't author). If no such hex exists, fall back to Decision
  5 (idle). Additive on top of v1; not required for it.
- **Kite/retreat as an opt-in movement-strategy item** (the "small layer of control via items" idea).
  The moment retreat is allowed, mutual-kite oscillation returns, so it must ship **paired with a
  stalemate breaker** (fatigue, or a slow global advance pressure). Not in v1.

## Open questions (own design sessions)

- **Movement-strategy items** — the player-facing control layer (kite / hold / charge / focus-target).
  Rides on top of this model; needs its own design pass.
- **Balancing / value table** — stat → value → item grid-size / rarity, and the pawn-stat tradeoff
  curve (e.g. higher range ⇒ less damage). Decision 2c depends on this table existing to price range as
  expensive + capped.

## Consequences

- **Positive:** deterministic, testable combat; one coherent range/movement intent per pawn; range gains
  a real opportunity cost; the reservation/stale-state bug class is deleted; the lerp polish lands for
  free; Candidate #5 drops from "risky planner" to "small pure step-rule."
- **Negative / debts:** weapons permanently lose range as an identity axis (2a); blocked units can idle
  and **must be telegraphed** (Decision 5); attack-vs-attack firing stays unsynced until Candidate #7;
  Decision 2c is blocked on the balancing table for final tuning.
