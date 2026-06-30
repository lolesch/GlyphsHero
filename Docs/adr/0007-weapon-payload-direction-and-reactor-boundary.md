---
tags:
  - ADR
  - Inventory
  - Combat
  - Items
status: Accepted
date: 2026-06-30
---

# ADR-0007 — A weapon chains into a weapon as a payload; the reactor is the only firing boundary, and payload direction is a derived, age-stamped origin

**Status:** Accepted (2026-06-30)
**Lifecycle:** **Partially implemented.** Decision 1 (weapon→weapon chaining; reactor-as-boundary)
landed 2026-06-30 in `ChainResolver` (red-green, two mutations proven, 128/128 EditMode green) — with
an **interim positional tiebreak** standing in for Decision 3's age stamp. Decisions 2–7 (the
age-stamped origin model and its UI) are **design-only — not built**.
**Refines:** ADR-0006 (the payload cost economy — this ADR makes its payloads *reachable*; the economy
assumed weapon-payloads the resolver was silently walling out).
**Companion:** ADR-0004 §4 (a payload is a child-delivery node), ADR-0001 (CombatClock determinism).
**Context:** ADR-0006 designed a fail-forward cost economy over a weapon's **payloads** — downstream
weapons carried as chain modifiers. But the weapon-centric resolver rework (commit `ff496a2`) had added
a wall in `GatherModifiers` (`if (next is IWeaponItem && next != weapon) continue;`) that made *every*
weapon its own isolated firing, so no weapon was ever another's payload and the entire ADR-0006 path was
dead code in play. Lifting that wall immediately raises the question the old model never had to answer:
when weapon B connects to weapon A with no trigger between them, **which is the root and which is the
payload** — and connectors between two weapons are mutually-facing, so the topology carries no direction.

---

## Context

Two forces are in tension. First, **ADR-0006 needs weapon-payloads to exist at all**; the reactor-rework
wall has to come down or the cost economy is untestable. Second, once it comes down, a bare weapon pair
has **no intrinsic direction** — A's connector points at B and B's points back at A (a connection
requires the matching reverse), so nothing in the grid says who fires whom. The naive fixes are all bad:
grid-position ordering spends the player's scarce Tetris space and flips the root when a weapon is merely
nudged; a stored per-weapon "mode" desyncs from the live topology; and any rule that re-derives *and*
mutates inside the resolver corrupts the tooltip preview, the stash, and combat replay (the resolver is
pure and runs constantly). The decision is to keep the resolver pure and derive mode every frame, and to
store exactly **one** piece of player intent — an age stamp that says which weapon "owns" the firing.

## Decisions

### 1. A weapon chains into a weapon as a payload; the reactor is the only firing boundary — Accepted

A weapon reached downstream of another weapon **with no reactor on the path between them** is that
firing's **payload** (a child delivery — ADR-0004 §4 / ADR-0006), not a separate firing. A reactor
between two weapons keeps them **independent** firings (each fires on its event). Shifters do **not**
divide weapons — only reactors do. *Why:* this is the boundary the existing model already implied —
reactors are firing sources, shifters are stat-shaping walls — generalised from "weapons never connect"
to "weapons connect, and a reactor is what separates two firings." It makes ADR-0006's economy reachable
without reintroducing the double-fire the rework removed. *(Implemented: the wall now fires only for a
reactor-driven neighbour; a reactor-driven weapon claims its downstream timer-weapons as payloads.)*

### 2. Mode is derived every resolve, never stored; only an age stamp is stored — Accepted

A weapon's mode (root vs. payload) is **recomputed from topology on every `OnContentsChanged`**, not
held as a field. The single stored datum is a per-weapon **age stamp**. *Why:* `ChainResolver` is pure
and runs for combat, the tooltip preview, *and* the stash. A stored mode would desync from the live
graph on the first missed update; deriving it can't drift. Storing only the stamp keeps the mutable
surface to one integer.

### 3. A trigger-less island roots at its **oldest** weapon (age stamp), not grid position — Accepted

Among connected weapons with no reactor, the root is the one with the **oldest** stamp; everyone
downstream is a payload. Two islands merging → the older stamp wins. Removing a root → each resulting
(possibly split) island re-roots at *its* oldest remaining weapon (fork propagation falls out). *Why:*
age is **stable under movement** — rearranging tiles to fit the grid never changes who fires whom —
whereas grid position both wastes the player's spatial budget and flips the root on a nudge. The stamp
is assigned at creation and changed **only** by Decisions 5–6, never by repositioning. *(Interim: the
shipped code uses a positional tiebreak until the stamp field exists.)*

### 4. Default state is **weapon (root)**; payload is conferred by an upstream origin — Accepted

A weapon is a root unless something upstream claims it. A lone weapon is its own root; a **solo island
cannot be toggled** (there is no second weapon to hand the root to). *Why:* matches the reactive "drop
it and it picks its state" feel — you never author payload-ness directly, it is the consequence of
sitting downstream of an origin.

### 5. The player re-roots by **toggle = swap stamps** with the current root — Accepted

A toggle on a weapon makes it the island's root by **swapping its stamp with the current root's**, so
the whole downstream re-derives from one action. Toggle is **disabled under a reactor** (the trigger
locks the root) and on a solo island (Decision 4). *Why:* selecting the root must define the entire
downstream in one gesture — never a per-payload chore — and it must cost no grid space.

### 6. Re-rooting **persists**: a root→payload transition refreshes the demoted weapon's stamp to newest — at the mutation site, not in the resolver — Accepted

When a weapon transitions **root → payload** as the result of a *real inventory change* (an add / move /
connect), its stamp is **refreshed to newest**. *Why:* this makes a re-root sticky — when a reactor (or a
toggle) demotes the old root, pushing it to "newest" means that after the reactor is removed the weapon
that *was* driven is now the oldest and **stays** root, instead of snapping back. **Critically, the
refresh fires from the content-change event, not from inside `Resolve()`**: the resolver also runs for
the tooltip preview and the stash, and mutating a stamp during a pure derivation would let a hover or a
stash rearrange silently reorder a chain and would break CombatClock replay determinism. The cost: a
nudge that transiently re-resolves can refresh the "wrong" weapon — recovered by one re-toggle (Decision 5).

### 7. Stamps choose the **root only**; payload firing order stays topological — Accepted

The stamp selects which weapon is the root. The **order** of payloads within a firing remains the BFS
topology order (`chain.Modifiers`), independent of stamps. *Why:* ADR-0006's fail-forward propagation
prunes in propagation order, which must be deterministic and stable; if stamp churn (Decision 6) drove
payload order, the pruning point would wobble. Decoupling the two keeps the economy deterministic.

## Worked example

Two weapons, created in order: **A** (stamp 1), **B** (stamp 2). A counter assigns ever-larger stamps;
"oldest" = smallest.

1. **Place A, then B adjacent, no reactor.** B is downstream of A → payload candidate (D1). Island
   oldest = A(1) → **A root, B payload** (D3, D4); modes derived, nothing stored but the stamps (D2).
   B went solo-root → payload, so its stamp refreshes to newest = 3 (D6); A is still oldest, no visible
   change. One payload, order trivial (D7).
2. **Player toggles B to root.** Swap stamps with the root A: **B→1, A→3** (D5). Re-resolve: oldest =
   B(1) → **B root, A payload**. A just went root→payload → refresh A to newest = 4 (D6). State: B(1), A(4).
3. **Attach a reactor R to A.** R drives A → **A is the reactor-root, locked** (D1, D5); B is now
   downstream → **B becomes A's payload**. B went root→payload → refresh B to newest = 5 (D6). State:
   A(4), B(5). B cannot be toggled while R is present (D5).
4. **Remove R.** Both timer again. Oldest = A(4) < B(5) → **A root, B payload** — A **persists** as the
   root the reactor conferred, even though B was the player's earlier toggle, because the persist refresh
   (D6) pushed B behind. Payload order is still topological, not stamp-driven (D7).

Every Decision 1–7 is exercised and none contradicts another: the resolver stayed pure (D2, D6), the
root moved only on creation/toggle/transition (D3, D5, D6), and payload sequencing never depended on the
stamp (D7).

## Considered and rejected

- **Grid-position tiebreak** (shipped as interim only) — spends the player's scarce Tetris space and
  flips the root when a weapon is nudged to fit. Acceptable as a deterministic fallback, not as the rule.
- **Typed input/output connectors** — give a weapon an explicit "out" side feeding another's "in". Most
  direction-true, but an authoring change to `ChainConnector` and every weapon asset plus new placement
  legality — far heavier than one age stamp for the same agency.
- **Cross-container re-add to change mode** (pull to stash, re-drop) — a workaround, not a mechanic.
- **Storing the mode on the item** — desyncs from the live graph; the first missed update is a silent bug.
- **Refreshing stamps inside `Resolve()`** — the same purity break: tooltip/stash previews would mutate
  order; resolution would stop being idempotent; replay determinism (ADR-0001) at risk.
- **Re-stamp on every move (recency)** — moving an item to make it fit should never change its firing role.

## Deferred (designed, not built)

- **The age-stamp slice** — the stored stamp field, creation assignment, swap-on-toggle, the
  transition-refresh in the container's change handler, and flipping the resolver tiebreak from position
  to stamp. (Decisions 3, 5, 6.)
- **The visibility slice** — red root / violet payload outlines, a "locked" overlay on weapons under a
  reactor, and the toggle gesture (disabled for solo islands and under a reactor). The telegraph debt
  ADR-0006 already owed, now also covering "which weapon is the origin."
- **True Splitter forks** — rooting a *middle* weapon (or dropping a reactor on one) currently flattens
  both downstream arms into one BFS-linear payload lineage. Real two-arm fork funding is ADR-0006
  Decision 3 (the sibling highest-subtree-first logic already exists in `PropagationCostResolver`; only
  the linear `PayloadCostTree.BuildLineage` needs to become a branching builder).
- **Stash parity** — the stash should show the same root/payload colouring (resolution is free there;
  only `PawnCombatController` firing is pawn-only), so the player can pre-arrange chains in the stash.

## Open questions

- **Reactor-on-payload was decided as persist (D6).** The alternative ("revert to the stamped order when
  the reactor leaves") is recorded as rejected; revisit only if persist proves surprising in playtest.
- **Toggle stamp arithmetic** — swap-with-root vs. "set to min−1". Both realise Decision 5; pick at
  implementation.
- **Cycle ordering** — a closed loop of multi-connector weapons roots at its oldest (D3) and the
  visited-set prevents hangs, but the payload sequence around the loop is BFS-arbitrary. Define or
  disallow loops if they become real.

## Consequences

- **Positive:** ADR-0006's cost economy becomes reachable and playtestable; weapon→weapon direction is
  intentional and **spatially free** (rearranging to fit never changes firing roles); reactor-rooting is
  sticky; the resolver stays pure, so tooltip/stash previews and combat replay are unaffected.
- **Negative / debts:** the persist rule needs **transition-detection plumbing** (diff each weapon's mode
  against the previous resolve) living at the content-change site, outside the resolver — a new seam to
  get right. Until the stamp slice lands, the shipped resolver roots trigger-less pairs by the **interim
  positional tiebreak**, which carries exactly the nudge-flip wart this ADR exists to remove. Forks
  flatten to linear until the Splitter builder exists. UI telegraph (which weapon is root) is owed before
  the model is legible to players.
