---
tags:
  - Architecture
  - TechDebt
aliases:
  - Deepening Candidates
---

# Architecture Review — 2026-06-19

Deepening candidates from `/improve-codebase-architecture`, viewed through the deep-module lens
(*module, interface, implementation, seam, depth, leverage, locality*). Each turns a **shallow**
module — interface nearly as wide as its implementation, or no interface at all — into a **deep** one.

**Plan:** tackle in the recommended order below, **one per session** to stay within context budget.
Each candidate: grill/brainstorm the deepened module's design → implement test-first (red-green) →
verify, keeping `ChainResolverTests` green.

> Companion ledger: [[KNOWN_ISSUES]]. The separate `MutableInt` infinite-recursion bug is tracked as
> its own task, not in this list.

---

## Status

- **Done:** Candidate 1 — *Resolve weapon stats to a value* (2026-06-19).
- **Done (B):** Candidate 2-B — *firing-source rework* (2026-06-20). Weapon-centric resolver shipped:
  fixes the equidistant-reactor drop, dissolves the amp double-fire at the source, deletes
  `ChainCollapser`. All 3 game-design decisions answered (see §2). 43/43 EditMode tests green,
  mutation-verified.
- **Done (A):** Candidate 2-A — *topology ownership* (2026-06-20). The container now owns the resolved
  topology: `TetrisContainer.Topology` is a lazily-cached `ChainTopology`, resolved once and invalidated
  (dirty flag) on every `Add`/`Remove`. All five consumers read `container.Topology` instead of
  re-resolving; the tooltip no longer re-resolves per hover, and `ChainOverlayView` reads the
  container directly (the `InventoryView → UpdateTopology` push is deleted). 47/47 EditMode green,
  mutation-verified (see §2 outcome).
- **Done:** Candidate 3 — *move placement & swap rules into the container* (2026-06-21). The cross-container
  swap routing left the drag MonoBehaviour: `ITetrisContainer.TrySwapInto(anchor, ref incoming, source,
  sourceAnchor)` is now the single rule for **both** same- and cross-container drops — place the held item,
  return a single displaced item to the freed source cell, force-pickup it only when it won't fit there.
  `DropCrossContainerSwap` deleted; `DropAt` is one call; the controller is input-only for drops. This also
  fixes KNOWN_ISSUES line 35 (same-container now attempts a full swap first instead of always force-picking-up)
  and hardens the source-return to empty space only (no silent item loss via a nested swap). **Deferred:**
  KNOWN_ISSUES line 34 (return the displaced item *relative to the dropped cell*) — only affects multi-cell
  displaced items, marked speculative; `TrySwapInto` is the one place to add it later. 52/52 EditMode green,
  mutation-verified.
- **Done:** Candidate 4 — *reconnect the orphaned chain-state seam* (2026-06-21). Decision: **reconnect**, not
  delete — the "unchained = passive pawn stat" mechanic is authored on `AttachmentItemConfig.pawnStatMod` and
  already surfaced in the tooltip (bold when loose, greyed when chained), so the seam was a half-wired feature,
  not dead code. `ChainStateController` was already correct but never constructed; it's now built in
  `Pawn.SpawnPawn` (after the starter weapon, so it lives across all phases — placement included) fed by the
  container's owned topology (#2-A). A loose attachment now applies its `pawnStatMod` to `PawnStats`, losing it
  once chained into a weapon. +6 `ChainStateControllerTests` (new `FakeStateContainer`/`FakeAttachment`/
  `RecordingStats` fakes that isolate the controller's diff logic from the resolver). **58/58 EditMode green,
  mutation-verified** (disabling the bootstrap apply failed exactly the 4 bootstrap-dependent tests; green on
  revert). The confusing `IAttachmentItem.OnChained`/`OnUnchained` naming (OnChained *removes* the passive) was
  left as-is — a possible small follow-up rename.
- **Done:** Candidate 5 — *movement on a fixed combat tick* (2026-06-21). A design grilling
  ([[0001-range-movement-and-combat-tick|ADR-0001]]) settled the range/movement model **before** coding
  and re-scoped #5 from a heavy planner to a small pure step-rule: range is a pawn stat, movement is
  monotone closing, and a project-side `CombatClock` fixed tick drives a move-readiness step-rule
  (deleting the reservation sets + re-check guard).
- **Done:** Candidate 7 — *combat clock / timer unification* (2026-06-22). Attack firing + reactor
  events migrated onto `CombatClock` alongside movement, read-then-write within a tick — fully
  deterministic combat. Shipped with a playtest re-entrancy crash fix. See §7.
- Candidate 6: not started.

Mark each candidate `done` / `doing` / `next` in its heading as you go.

---

## 1 · Resolve weapon stats to a value — stop mutating the weapon  `done`
**Strength:** Strong (top recommendation) · **dependency:** in-process (pure)

**Files**
- `Assets/Code/Runtime/Modules/Inventory/ItemChain.cs:29-70`
- `Assets/Code/Runtime/Core/Combat/PawnCombatController.cs:75-128`
- `Assets/Code/Runtime/UI/Inventory/ItemTooltipController.cs:249-317`
- `Assets/Code/Runtime/Modules/Inventory/WeaponUtils.cs:9-20`

**Problem.** Effective weapon stats (damage, attack speed, cost, gen) are produced by **mutating the
weapon's live `MutableFloat`s** from three sites — `ItemChain.ApplyChainModifiers` (amplifiers),
`PawnCombatController` (shifters/reactors), and `ItemTooltipController` (apply-then-revert for preview).
The rule has **already drifted**: combat writes the shifter's `inputMod` to the *output* stat
(`PawnCombatController.cs:100`), while the tooltip writes `outputMod` (`ItemTooltipController.cs:310`).
Same rule, two implementations, different numbers.

**Solution.** Resolve a chain to a computed-stats **value** that combat and the tooltip both *read*;
the weapon is never mutated to answer "what are my stats?"

**Wins.** locality: modifiers applied in one module · drift bug becomes impossible · leverage: combat +
tooltip read one value · chain stats become unit-testable for the first time · dissolves the known
"fires twice with amplifiers on both sides" bug · delete apply/revert bookkeeping.

**Outcome (2026-06-19).**
- New pure module: `WeaponStats` (value) + `WeaponStatResolver` (`Resolve(weapon, contributors)` core
  and `Resolve(IItemChain)` adapter that folds in the trigger root) + `ChainCollapser` (merges the
  duplicate `(root, weapon)` chains that caused the double-fire). 12 EditMode tests in
  `WeaponStatResolverTests.cs` — `ChainResolverTests` left untouched and green.
- `ItemChain` is now pure data — `ApplyChainModifiers`/`RemoveChainModifiers` deleted.
- `PawnCombatController` collapses chains, resolves stats once per firing, and reads the value; no
  weapon mutation and no stat-revert cleanup. `ItemTooltipController` diffs two resolver snapshots
  instead of apply-then-revert. `WeaponUtils` (the last by-enum live-stat accessor) deleted.
- **Accepted behaviour changes (both fix the drift):** combat's shifter *output* stat now uses
  `outputMod` (was `inputMod`); the tooltip preview now includes a reactor root's `inputMod`. Also
  fixes latent cross-chain contamination — firings no longer share a mutated weapon instance.
- **Deferred (Candidate 2 / open questions):** ChainResolver still emits one chain per root connector
  (collapse compensates downstream); independent multi-reactor firings and the payload-mode toggle
  remain open.

---

## 2 · Own the chain topology — resolve once, push it  `done`
**Strength:** Strong · **dependency:** in-process · *composes with #1* · **spec'd 2026-06-19** ·
**2-B shipped 2026-06-20**

> This section is a **cold-start handoff**: a context-cleared session should be able to design and
> implement Candidate 2 from this alone (plus the Candidate 1 outcome above). It bundles two things
> that share a root cause — topology *ownership* (the original Candidate 2) and a topology *bug* found
> while shipping Candidate 1 (equidistant reactors) — because the firing-source rework fixes both.

**Files**
- `Assets/Code/Runtime/Modules/Inventory/ChainResolver.cs` — `ResolveRoots` (116-160, the furthest-trigger logic to rework), `ResolveTopology` outer loop (57-109, one chain per root connector), trigger-wall rule (line 87), `IsTrigger` (286), `ChainTopology` (7-43)
- `Assets/Code/Runtime/Core/Combat/PawnCombatController.cs` — `RebuildChains`/`BuildTimedChain`/`BuildReactor` (the firing dispatch that consumes the topology)
- `Assets/Code/Runtime/Modules/Inventory/ChainCollapser.cs` — **delete in this candidate** (a #1 stopgap; see below)
- `Assets/Code/Runtime/UI/Inventory/InventoryView.cs:129`, `ItemTooltipController.cs:106`, `ChainOverlayView.cs:44-126` (re-derives connectors itself)

**Problem A — topology ownership.** Three consumers each call `ResolveTopology` (the tooltip on every
hover — see [[KNOWN_ISSUES]] line 67), and the overlay re-derives connectors independently — two
sources of truth for "what is connected." No module owns the resolved result.

**Problem B — firing-source bug (found 2026-06-19).** `ResolveRoots` keeps only the **single
furthest-upstream trigger** as a weapon's root and dedups via `assignedRoots`. Two reactors
**equidistant** from a weapon — `[reactorA][weapon][reactorB]` (both adjacent, depth 1) — tie at the
same BFS depth; one wins (tie broken by queue dequeue order → **nondeterministic**) and the **other is
silently dropped**, so only one reactor ever fires. The furthest-trigger rule exists to collapse
*series* triggers (`[reactorA][reactorB][weapon]`) into one root, but it wrongly also collapses
*parallel* triggers on different sides.

**Solution — weapon-centric trigger list (user's design, 2026-06-19).** Flip root-propagation: the
**weapon owns a list of firing triggers** (the reactors connected to it).
- **0 reactors** → fire on the weapon's repetitive timer (`1/AttackSpeed`), as today.
- **≥1 reactors** → suppress the timer; subscribe to each reactor's event and fire on each occurrence.

Then resolve once on `OnContentsChanged` and let the container own + push the topology (the original
Problem A); consumers read it — none re-resolve, none re-derive.

**Composes with #1 (reuse, don't rebuild).** `WeaponStatResolver` (the Candidate-1 value) is
**firing-model-agnostic** — it already turns `(weapon, contributors)` into a `WeaponStats` value. This
rework only changes how *firings* and their *contributor sets* are enumerated; the stat math is done.

**Dissolves the double-fire natively → delete `ChainCollapser`.** A weapon-sourced (timer) firing
gathers modifiers by BFS in *both* directions, so `[ampA][weapon][ampB]` is **one** firing carrying
both amps — no duplicate to collapse. `ChainCollapser` (and its `Collapse_*` tests in
`WeaponStatResolverTests.cs`) was a Candidate-1 stopgap for the root model's one-chain-per-connector
duplication; Candidate 2 removes the *cause* and deletes it. This is the proper fix for
[[KNOWN_ISSUES]] line 40 ("weapons fire twice with amplifiers from both sides") — Candidate 1 only
*mitigated* it in combat.

**Design decisions (answered 2026-06-20, implemented):**
1. **Series reactors / multiple reactors → all fire.** Every reactor connected to the weapon is a
   firing source. `[reactor][reactor]` adjacency stays invalid grammar; where multiple reactors reach
   a weapon, all are added to the weapon's trigger list. **Dedup by event:** the list holds unique
   reactor events (`ReactorType`), so two reactors on the *same* event fire the weapon once.
2. **Per-firing stat scoping — confirmed.** Each reactor firing walks from its source toward the
   weapon, stopping at the weapon's far-side trigger wall (existing `ChainResolver` non-trigger→trigger
   rule). `[reactorA][shifterA][weapon][shifterB][reactorB]` → reactorA fires with shifterA only,
   reactorB with shifterB only; an amp past the weapon with no competing trigger rides the firing.
3. **Timer suppression — any reactor suppresses the timer entirely.** 0 reactors → the weapon fires on
   its `1/AttackSpeed` timer; ≥1 reactor → no timer, fire once per reactor event. (The "reactor resets
   the timer / fallback timer" alternative was explicitly rejected — not the original design.)

**Outcome (2026-06-20) — 2-B shipped.**
- `ChainResolver` rewritten weapon-centric: `ResolveRoots`/`IsUpstreamOf` replaced by `FiringSources`
  (reactors that drive a weapon, deduped by `ReactorType`) + `GatherModifiers` (one BFS per source over
  all connectors, with an other-weapon wall + the existing trigger wall). One firing per source — never
  one per connector. `IItemChain` is still the firing abstraction (Root = source, Modifiers =
  contributors + weapon).
- `ChainCollapser` + its `Collapse_*` tests **deleted**; `PawnCombatController.RebuildChains` consumes
  chains directly (its `switch(Root)` dispatches weapon→timer / reactor→event; the dead `IShifterItem`
  case removed — shifters are never sources now).
- `ChainResolverTests` reworked (constraint was lifted for this candidate): the double-fire
  characterization flipped to one chain; added equidistant-reactors, same-event dedup, parallel
  per-side scoping, timer-suppression, and reactor-between-two-weapons tests. `FakeReactor` gained a
  `ReactorType` ctor.
- **Verified via Unity MCP** (`TestRunnerApi` driven through `Unity_RunCommand`, polled over the
  console): 43/43 EditMode green; a "drop one reactor" mutation failed exactly the two multi-source
  tests, then green restored on revert.
- **Deferred → 2-A:** topology ownership (resolve once on `OnContentsChanged`, push the cached topology
  to `InventoryView` / `ItemTooltipController`; overlay stops re-deriving). Signature of
  `ResolveTopology` is unchanged, so those consumers still compile and read correct edges/connectors.

**Outcome (2026-06-20) — 2-A shipped.**
- `ITetrisContainer` gained `ChainTopology Topology { get; }`. `TetrisContainer` owns it: a `_topology`
  cache + `_topologyDirty` flag, computed lazily on first read and invalidated in `Add`/`Remove`
  (alongside the existing `OnContentsChanged` fire). A multi-step mutation (swap = remove + add)
  therefore resolves **once**, and all readers share one instance — the per-hover/per-frame re-resolve
  is gone. `ChainResolver.ResolveTopology`/`Resolve` stay public (the container + the resolver's own
  tests call them).
- **Consumers migrated** to read `container.Topology`: `ItemTooltipController` (kills per-hover
  resolve — [[KNOWN_ISSUES]] line 67), `InventoryView.Refresh`, `PawnCombatController.RebuildChains`,
  `CombatCoordinator.ResolveMaxRange`, and the orphaned `ChainStateController`. `ChainOverlayView` now
  reads `_container.Topology` directly in `OnDrawGizmos`; its `UpdateTopology` push channel + cached
  field are **deleted** (`InventoryView` no longer pushes). The overlay still iterates
  `GetGridConnectors` for *drawing geometry* (dot positions + red arrows on unconnected connectors,
  which topology never enumerates) — that is rendering, not topology, and stays put.
- **New tests:** `TetrisContainerTests.cs` — `Topology_ReflectsCurrentContents`,
  `…ReturnsSameInstance_WhenContentsUnchanged` (the resolve-once lock),
  `…Recomputes_AfterContentsChange`, `…Recomputes_AfterRemoval`. Red-green proven: the same-instance
  test failed under a naive recompute-every-read impl; the three content-reflecting tests failed under
  a dirty-flag-dropped mutation (44/47), exactly and only those three. `ChainResolverTests` untouched.
- **Verified via Unity MCP** (`TestRunnerApi` over `Unity_RunCommand`, polled via console): 47/47
  EditMode green after the migration; mutation reverted, still 47/47.

**⚠ `ChainResolverTests` constraint is LIFTED here.** Candidate 1 had to keep `ChainResolverTests`
green because it never touched topology. Candidate 2 *is* the topology change: the characterization
test `AmplifierOnBothSides_CurrentlyDuplicatesTheWeaponChain` (asserts 2 chains) flips to **one**
chain, and the `[Ignore]`d `AmplifierOnBothSides_ShouldProduceOneChainWithBothAmplifiers` becomes the
active target. Add a test for the equidistant-reactor bug (`[reactorA][weapon][reactorB]` → two
firings). Update `ChainResolverTests` together with the resolver.

**Distinction to preserve.** `IsTrigger` (ChainResolver.cs:286) = shifter *or* reactor, but **only
reactors are event/firing sources**. Shifters are stat-shapers that ride the timer (or the reactor on
their branch); they are "triggers" only for topology/wall purposes. Keep that split.

**Foundation already in place (Candidate 1).** `WeaponStats` + `WeaponStatResolver`
(`Resolve(weapon, contributors)` and `Resolve(IItemChain)`) — pure, reuse. `ItemChain` is pure data.
`PawnCombatController` reads resolved values (no weapon mutation). `WeaponUtils` deleted. Tests in
`WeaponStatResolverTests.cs` — keep the non-`Collapse_*` ones.

**Wins.** resolve once, not per hover · one source of truth for edges · overlay stops re-deriving
connectors · **equidistant/parallel reactors all fire** · double-fire dissolved at the source ·
`ChainCollapser` deleted · leverage: N consumers, one resolve.

---

## 3 · Move placement & swap rules into the container
**Strength:** Strong · **dependency:** in-process

**Files**
- `Assets/Code/Runtime/Modules/Inventory/TetrisContainer.cs`
- `Assets/Code/Runtime/UI/Inventory/InventoryDragController.cs:227-286` (`DropCrossContainerSwap`)
- `Assets/Code/Runtime/UI/Inventory/InventoryView.cs:114-152`

**Problem.** The container's interface *is* its raw `Contents`/`ContentPointer` dictionaries, so
placement and cross-container swap rules live in a drag MonoBehaviour — exactly where the team's
swap bug sits, untestable.

**Solution.** Deepen the container with query + `TryMove`/`TrySwap` behind its interface; the drag
controller becomes input-only (translates pointer events).

**Wins.** swap rules become unit-testable · locality: swap bug has one home · UI stops reaching into
dictionaries · interface shrinks; container absorbs the rules.

---

## 4 · Reconnect (or delete) the orphaned chain-state seam — **done (reconnected, 2026-06-21)**
**Strength:** Worth exploring · **dependency:** in-process

**Files**
- `Assets/Code/Runtime/Modules/Inventory/ChainStateController.cs` (whole file — never constructed)
- `IAttachmentItem.OnChained` / `OnUnchained` (the dangling seam)

**Problem.** The only module that fires attachment items' `OnChained`/`OnUnchained` is never
instantiated — the seam is dead, so attachment effects never fire in combat.

**Solution.** Decide its fate: delete it, or make it the one owner of chain-state events, fed by the
owned topology from #2.

**Wins.** attachment effects actually fire · or: delete a dead module · one owner for chain-state events.

---

## 5 · Movement on a fixed combat tick — **re-scoped & de-risked 2026-06-21** (see [[0001-range-movement-and-combat-tick|ADR-0001]])
**Strength:** Worth exploring · **already on the roadmap** ([[KNOWN_ISSUES]] → "Movement → central planner")

**Files**
- `Assets/Code/Runtime/Core/Combat/CombatCoordinator.cs:18-339` (movement: 109-237)
- `Assets/Code/Runtime/Pawns/Pawn.cs:72-79`

**Problem.** Each pawn's async timer decides against stale world state, so the coordinator carries
`_reservedHexes`/`_claimedHexes` and re-check guards purely to compensate.

**Original framing (heavy).** A planner that maps one world snapshot to all moves per tick.

**Re-scoped (after the 2026-06-21 range/movement design grilling — [[0001-range-movement-and-combat-tick|ADR-0001]]).**
The grilling's payoff is that the *heavy* planner is **not necessary**. With **range as a pawn stat**,
**monotone-closing** movement (close to the minimum active-weapon reach, no kiting), and
**closest-to-target-wins** hex arbitration, the planner collapses to a **small pure step-rule** in a
per-tick `foreach`. This candidate is now:
- Introduce a **project-side `CombatClock`** — a fixed-tick heartbeat (accumulator, 0..N ticks/frame,
  frame-rate independent) firing `OnTick(fixedDelta)`.
- Put **movement** on it: per-pawn **move-readiness accumulator** + a pure snapshot **step-rule**
  (single-unit A*, allies passable-but-costly, destination must be empty). **Delete** `_reservedHexes`,
  the reservation use of `_claimedHexes`, and the on-arrival re-check guard.
- **Attacks stay on the Utility `Timer` for now** — movement (5a) needs no `Timer`. The view interpolates
  between ticks (lands the deferred lerp polish).

**Wins.** step-rule unit-testable against a snapshot · delete reservation bookkeeping · deterministic
combat · dissolves the stale-state guard class · folds in the lerp polish.

**Note.** No longer the riskiest item — the design work above shrank it. The chain-side trio (1–3) was
still sequenced first.

---

## 7 · Combat clock / timer unification — **new, queued (spun out of #5, 2026-06-21)**
**Strength:** Worth exploring · **depends on #5's `CombatClock`**

**Files**
- `Assets/Code/Runtime/Core/Combat/PawnCombatController.cs` (chain firing)
- `Assets/Submodules/Utility/Tools/Timer/` (submodule — do **not** globally re-route)

**Problem.** After #5, **movement** runs on `CombatClock` but **attack firing + reactor events** still
run on the frame-based Utility `Timer`, so attack-vs-attack firing isn't synchronised and combat isn't
yet fully deterministic.

**Solution.** Migrate attack firing + reactor timing onto `CombatClock` with **read-then-write**
within a tick (gather all decisions against the frozen snapshot, then apply) — true simultaneity.
`Timer.Tick(interval)` is already delta-parameterised, so the migration is cheap, and it **subsumes /
cleans up** whatever interim timer logic #5 introduces. Keep the Utility submodule untouched; subscribe
combat to the project-side clock.

**Wins.** fully deterministic combat (`(snapshot, tick) → snapshot`, unit-testable across N ticks) ·
death + reactions resolve on one tick · one combat heartbeat for movement *and* firing.

---

## 6 · One source of truth for the connection grammar
**Strength:** Speculative · **dependency:** local-substitutable

**Files**
- `Assets/Code/Runtime/Modules/Inventory/ChainResolver.cs:272-284` (`IsValidConnection` switch)
- `Assets/Code/Data/Items/ItemConfig.cs:19-47` (config-time geometry)
- [[Item Chaining]] (the grammar table, in prose)

**Problem.** The grammar exists as a runtime switch, a separate geometry check, and a doc table — three
partial encodings, and the author sees no error until resolve time.

**Solution.** Make the grammar table *data* that both authoring validation and the resolver read from
one definition.

**Wins.** grammar has one definition · authoring rejects illegal pairs · rule visible, not buried in a switch.

**Note.** Speculative — for a solo project at this stage the switch is cheap and correct; data-driving
it may be more interface than the variation justifies today (one adapter = hypothetical seam).
