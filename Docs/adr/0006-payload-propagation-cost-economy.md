---
tags:
  - ADR
  - Combat
  - Inventory
  - Resources
  - Items
status: Accepted
date: 2026-06-24
---

# ADR-0006 — Payload propagation is a fail-forward cost economy; the `ConditionType` draft is dissolved

**Status:** Accepted (2026-06-24)
**Lifecycle:** **Implemented.** Slices 1–3 + the consumer wiring landed 2026-06-29/30; all six Decisions
are realised (Decision 6 by the pre-existing reactor-cost pipeline, see below); 126/126 EditMode green.
Remaining items are the body's **Deferred** list (Reactor watch-conditions, Status-as-gate, weapon-economy
pool/`ProcChance`, telegraph, Converter-of-payload-cost, damage-gate) — none are part of this ADR's core.
- **Slice 3 — pure walker (2026-06-29):** `PropagationCostResolver` (+ `CostNode`/`PropagationResult`) in
  `Assets/Code/Runtime/Core/Combat/`, locked by `PropagationCostResolverTests` (red-green, two mutations
  proven: fork-ordering flip and sibling-undo removal each failed exactly their target test).
  **Deviation from Decision 5's literal wording:** the walker does *not* keep a node's modifier on and
  `TryRemoveModifier`-rollback across the whole walk; it threads one `MutableFloat` with add-and-keep down a
  linear lineage and removes modifiers only where state must reset — pruning an unaffordable node, and
  **isolating fork siblings** (each restarts from the fork's running cost `R`). The literal "keep the
  modifier on" leaks sibling A's mods into sibling B at a fork, contradicting Decision 3; the backtracking
  form faithfully realises Decisions 2 and 3 and still routes every number through `MutableFloat`.
- **Slice 1 — dissolve (2026-06-30):** removed `PayloadBehavior.Condition`/`ConditionThreshold`,
  `PawnCombatController.EvaluatePayloadCondition`, and the tooltip/chain-debug condition reads. The
  `ConditionType` enum is kept **parked** (no live uses) for the future Reactor/Trigger-condition ADR.
- **Slice 2 — author cost data (2026-06-30):** `PayloadBehavior` gained `CostValue` (float) + `CostType`
  (`ModifierType`, default `FlatAdd`). Authored as primitives, not a `Modifier`, because the **Data**
  assembly is dependency-free and cannot reference `Modifier`; the runtime builds the `Modifier` at fire
  time. Default `CostValue 0` ⇒ existing payloads are free until authored.
- **Consumer wiring (2026-06-30):** new pure `PayloadCostTree.BuildLineage<T>` (locked by
  `PayloadCostTreeTests`, mutation-proven) maps the chain's payloads into a linear `CostNode` lineage in
  propagation order; `PawnCombatController.Fire` runs the walker once (root gate + fail-forward), spends
  `TotalSpent` from the pool, and `FirePayloads` detonates only the funded nodes. A Health cost pool is
  handed a balance one epsilon under full so blood-magic still can't self-kill (mirrors `Resource.CanSpend`).
  This replaces the old per-payload independent `ResourceCost` deduction; a payload's own `ResourceCost`
  stat is no longer consulted when it acts as a payload.
- **Decision 6 — Reactor event cost factor — ALREADY SATISFIED by the existing pipeline (2026-06-30):**
  no new mechanism was needed. A reactor authors its cost as its `ReactorConfig.inputStatMod` targeting
  `WeaponInputStat.ManaCost` (any `ModifierType`, typically `PercentMult` to tax a frequent trigger);
  `WeaponStatResolver` already folds a reactor-root's `inputMod` into `WeaponStats.ResourceCost`, which is
  the value `Fire` seeds the walker with — i.e. the reactor-scaled **effective base / root gate** of
  Decision 5. So `Fire` passes `reactorMods: null` (passing the mod again would double-count). The walker's
  `reactorMods` parameter stays as the explicit model seam (locked by
  `RootGate_ReactorModRaisesEffectiveBase`) but is unused in this integration because the reactor cost rides
  resolved stats. *Caveat:* a `ReactorConfig` has a single `inputStatMod` slot, so a reactor taxing cost
  cannot also buff another input stat — if a reactor ever needs both, that is a multi-mod generalisation
  beyond ADR-0006, not a gap in the cost model.
- **Needs a play-mode check:** combat firing has no automated coverage (no pawn/registry fakes), so the
  in-combat behaviour — payloads firing per the economy, fail-forward pruning — is verified by the green
  suite + a user play-test, per the established pattern.
**Companion:** ADR-0004 (attack model — §4 makes a Payload a child delivery node), ADR-0005 (the
resource economy — Cost is one pool + magnitude).
**Refines:** ADR-0004 §4 (what gates a child delivery node) and the half-built
`PayloadBehavior.ConditionType` draft.
**Context:** the code carried a `ConditionType` enum on `PayloadBehavior.Condition` that conflated three
unrelated jobs — pawn/enemy *watch* predicates (`ResourceBelow/Above/Full/Depleted`), a *status* check
(`HasStatusEffect`), and chain-stat checks (`DamageBelow/Above`, `ResourceGenBelow/Above`). Only 5 of 9
were wired, mana-hardcoded, and `ResourceGenBelow/Above` referenced the `ResourceGenOnHit` weapon stat
that ADR-0005 had already removed. Meanwhile the gate the design actually named — Payload.md's *"proc
chance and resource cost… chain propagation stops if unmet"* — was unbuilt (`ProcChance` is dropped
silently in `WeaponStatResolver`). The draft was polishing the wrong thing. This ADR replaces it with the
gate the design meant: an **economic propagation gate**, the Noita "pay to enable the combo" fantasy.

---

## The mismodel this corrects

A "payload condition" was modelled as a rich predicate language bolted to the payload. But:

- **Watch predicates** (a pawn's or enemy's resource crossing a threshold, a status being present) are
  *trigger* concerns — the **Trigger / Reactor** axis (ADR-0004 §5), as Reactor.md's whole pattern list
  shows ("opponent reaches X resource," "during state," "before defeat"). The commented-out
  `ReactorConfig.ConditionType` is the tell that they were meant to live there. They are **not** a payload
  property.
- **Status** checks are blocked on a Status-Effects system that does not exist; "burning gates vs.
  burning amplifies" is itself undecided.
- What is *genuinely* a payload property is the **cost to include it in the attack** — every reference
  game (Noita wand mana, PoE link costs) gates extra payloads economically, not by predicate.

So the payload's gate is **economic**, and the predicate enum dissolves.

---

## Decisions

### 1. The payload gate is an economic propagation gate, not a predicate — `ConditionType` is dissolved

`PayloadBehavior.Condition`/`ConditionThreshold` (the predicate draft) is removed. Its three conflated
jobs are routed to where each belongs:

| Old `ConditionType` job | New home |
|---|---|
| pawn/enemy resource *watch* (`ResourceBelow/Above/Full/Depleted`) | the **Reactor / Trigger** axis — a future Trigger-condition ADR (deferred) |
| status presence (`HasStatusEffect`) | the **Status-Effects** design (deferred; gate-vs-amplifier still open) |
| chain-stat checks (`DamageBelow/Above`) | folded into / superseded by the economic gate; revisit if a damage-gate is still wanted |
| gen-magnitude checks (`ResourceGenBelow/Above`) | already orphaned by ADR-0005 (the gen weapon stat is gone) — dissolved here; a future gain-gate belongs to ADR-0005's deferred **effect-magnitude axis** |

The surviving, real payload gate is the **cost** the payload adds to the attack (Decisions 2–5).

### 2. Fail-forward partial propagation

An attack is a **tree of delivery nodes** (root weapon → payload children; a [[Splitter & Merger|Splitter]]
forks siblings — ADR-0004 §4). Resolution walks the tree **depth-first, paying as it goes**:

- At each node: if the pool covers the node's **marginal** cost → **pay and fire**, then recurse into its
  children. If not → **skip the node, prune its whole subtree, spend nothing.**
- **Linear order = propagation order** (the existing chain topology). A node that can't be paid prunes
  everything downstream — those are its descendants and cannot spawn without it. This is the legible
  "the weapon fired, but not the extra bomb."
- A skipped node is **atomic**: it pays nothing, so its budget stays available for whatever follows.

All-or-nothing upfront cost was rejected (Decision-rejected list): it makes every chain a binary
affordability cliff and throws away the Noita texture of a chain fizzling partway. The legibility cost of
fail-forward ("why did the weapon fire but not the bomb?") is a **telegraph** problem, solved by the
ADR-0002 telegraph seam (show fired vs. fizzled nodes), not a model problem.

### 3. A Splitter funds siblings highest-(subtree)-cost-first, off a shared pool

Siblings at a Splitter are **independent**: each is its own sub-walk **seeded at the fork's running cost
`R`** — firing one sibling does **not** inflate another sibling's running cost (they don't compound). They
compete only for the **one shared pool**. Siblings are funded **highest-cost-first**, keyed on each
branch's **whole-subtree potential drain** (the delta its full modifier set would add to `R` if everything
fired — deterministic from topology).

*Why highest-first:* the expensive centerpiece combo gets first claim on the pool; if it can't be paid it
yields (spending nothing) and the cheaper sibling still fires. Lowest-first would systematically starve the
big combos — the opposite of the build fantasy. Highest-first only bites at forks; everywhere else order is
propagation order (Decision 2), so the whole thing is **one rule** (subtree-pruning) with a fork tiebreak.

### 4. One shared pool — the weapon's `CostResource`

The whole tree drains **one pool**: the weapon's `CostResource` (ADR-0005), which a **Converter** may
reclassify (Mana → Health). Per-payload pools were rejected: they turn affordability into a multi-pool
knapsack, and ADR-0005 already deferred dual-pool cost for the same reason. A payload carries **no pool of
its own** — only a cost *modifier* (Decision 5).

### 5. Cost is a `MutableFloat` driven by `Modifier`s — Reactor and Payload are unified as cost modifiers

The per-fire cost **uses** the stat system's `MutableFloat`/`Modifier` (it does **not** re-implement the
math):

- Seed `MutableFloat(weaponBaseCost)`. Apply the **Reactor**'s cost modifier(s) → its `totalValue` is the
  **effective base**, and that is the root gate (`CanFire`: pool ≥ effective base, else nothing fires).
- Walk the tree (Decisions 2–3). At each node: `AddModifier(node.costMod)`, read the new `totalValue`,
  **marginal = new − previous**. Afford it → deduct from the pool, keep the modifier on, fire, recurse.
  Don't → `TryRemoveModifier(node.costMod)`, prune the subtree.

A payload's cost is therefore an authored **`Modifier` carrying a `ModifierType`** — `FlatAdd`,
`PercentAdd`, or `PercentMult` — aggregated in `MutableFloat`'s existing **stage order**
(`(base+Σflat) × (1+Σ%add) × Π(1+%mult)`). Consequences of reusing the pipeline:

- **`PercentAdd`** measures off the effective base and does **not** compound — two `+50%` = `+100%`. This is
  the position-independent default.
- **`PercentMult`** compounds — so **"a payload costs more the deeper it sits"** is an *opt-in property of
  the `PercentMult` type*, not a forced global rule. (A `PercentMult` anywhere also inflates `FlatAdd`
  marginals, because the pipeline multiplies `(base+Σflat)` — faithful to stats, and a high-multiplier
  build legitimately makes everything pricier.)
- The total (if everything fires) is order-independent (sums and products commute); only the **fizzle
  point** depends on traversal order, which is deterministic.

A **Reactor** and a **Payload** are now the *same kind of thing* — cost modifiers — differing only by
**where they sit** (Reactor at the root/trigger, Payload at a tree node) and **what sets their value**
(Decision 6).

### 6. The Reactor's event-factor is a cost multiplier scaled by event frequency

A Reactor that fires the weapon on a combat event applies a **cost multiplier scaled by how common the
event is** — a frequent event carries a larger multiplier, taxing cheap high-frequency triggering. A weapon
on a Reactor still cannot fire without the (scaled) resource to spend. This is the Reactor's balance lever;
it is **not** a threshold/predicate.

---

## Considered and rejected

- **A predicate `ConditionType` enum on the payload** — mislocated. Watch predicates are the Reactor/Trigger
  axis; status is its own system; the real payload gate is economic (Decision 1).
- **All-or-nothing upfront cost** — binary affordability cliffs; discards the partial-propagation texture
  (Decision 2).
- **Per-payload pools** — multi-pool knapsack affordability; ADR-0005 already deferred dual-pool cost
  (Decision 4).
- **Per-node authored flat-only cost** — superseded by reusing `ModifierType` (flat *and* percent), which
  also gives the opt-in compounding for free (Decision 5).
- **Re-implementing / mirroring `MutableFloat`'s aggregation** — *use* it, don't mirror it (Decision 5).
- **Strict-traversal cost aggregation** (every deep node compounds on everything upstream regardless of
  type) — throws away the `ModifierType` distinction; stage order keeps "deeper costs more" an authoring
  choice (Decision 5).
- **Lowest-cost-first Splitter ordering** — starves the centerpiece combo (Decision 3).
- **Proc-chance as a payload propagation gate** — reintroduces RNG into the deterministic combat tick
  (ADR-0001's `CombatClock`), needing a seeded replay-safe RNG; the economic gate already supplies the
  "enable the combo" tension deterministically. Parked, not deleted (see Deferred).

## Deferred (designed, not built)

- **Reactor watch-conditions** — the Trigger-axis condition model ("opponent reaches X resource," "during
  state," "before defeat," counter-based). This is where the old `ResourceBelow/Above/Full/Depleted` go.
  Its own future ADR.
- **Status-as-gate vs. status-as-amplifier** — blocked on the Status-Effects design.
- **The weapon-economy expansion** — per-weapon mana-pool size and recovery rate as wand archetypes (Noita),
  vs. today's pawn-level pool/recovery. The sibling ADR to ADR-0005, and the home for **`ProcChance`** (kept
  vestigial, not deleted).
- **Telegraphing fired-vs-fizzled nodes** — the legibility debt of fail-forward, owed to the ADR-0002
  telegraph seam.
- **A Converter reclassifying a payload's cost-modifier** (its type or, once effect-pools exist, its pool) —
  consistent with ADR-0005's deferred gain-pool reclassification.
- **A damage-gate** (`DamageBelow/Above`) on a payload, if still wanted after the economic gate exists.

## Consequences — planned code deltas (NOT yet implemented)

This ADR is **design only**. Implementation is a sequence of bounded, test-first slices (each red-green,
mutation-proven, `ChainResolverTests` kept green — [[feedback-red-green-testing]]):

1. **Dissolve the draft.** Remove `PayloadBehavior.Condition`/`ConditionThreshold` and the
   `EvaluatePayloadCondition` switch from `PawnCombatController`. Retire the `ConditionType` enum's payload
   uses; `ResourceGenBelow/Above` were already orphaned by ADR-0005. Keep `ProcChance` vestigial.
2. **Author cost modifiers.** `PayloadBehavior` gains a cost `Modifier` (value + `ModifierType`); `Reactor`
   gains an event-frequency cost factor.
3. **Extract a pure propagation-cost walker** — the testable seam (mirroring `DeliveryResolver` /
   `DeliveryAffinity` / `DeliveryAnchor`): given the tree topology, the seeded `MutableFloat`, and a pool
   balance, it returns the set of nodes that fire (fail-forward, Splitter highest-subtree-first). Its tests
   are the red-green lock. `PawnCombatController.Fire`/`FirePayloads` consume it to decide which children
   detonate.
4. **Telegraph** fired-vs-fizzled nodes (later, with the ADR-0002 seam).

Because this is design-not-code, it is **not** a single unattended night chunk — slices 1–3 are the
night-safe units once this ADR is the spec.
