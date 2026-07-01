
# TODO

- [x] resource regen on pawns. Done (2026-06-22): `Resource.Regenerate(rate, dt)` driven by `CombatCoordinator` on the `CombatClock` (combat-only); enemies full-heal at combat start. Mana regen left unwired on purpose (attack-cost economy).
- [ ] converter should only show cenvertable types, not the input enum and actually apply the change
- [x] Enemies should not be draggable
	- [ ] enemies inventory should not be interactable ( inspect, but no add/remove/drag )
- pawn movement
	- [x] terrain heuristic
	- [x] direction heuristic (cross-product tiebreaker)
	- movement speed 
		- attack readyness
		- targetable during movement
			- draw position outline tile for feedback
- define weapon attacks
	- define status effects
	- define payload attacks
		- implement status effects
		- implement terrain changes

~~List Terrain tile hierarchy, how they order~~
- ~~does sand goes over or under grass? and so on~~

- implement player roster
- extend PlayerData and save 
	- [ ] current roster
	- [x] current map
	- [ ] serialize/deserialize to file

- move pathFinder into submodule, extract all 'non-submodule' logic and pass in its calculations upfront.
# Bugs
- [x] ~~**`Self` delivery no longer self-damages — likely revert (2026-06-23, ADR-0003).**~~ **Fixed (2026-06-23).** The delivery-pattern split made damage uniform hex-occupancy filtered to hostiles. `DeliveryPattern.Self` covers the caster's own hex, but an enemy never stands there, so a `Self` payload hit no one — silently killing the **deliberate self-hurt build-around** (a player intentionally taking damage to drive `OnSelfHit`/threshold builds). Fix: `Self` is now resolved as a *self-affinity* delivery via the new pure `DeliveryResolver.SelfHexes` (the self-affinity subset of the footprint, currently the origin hex). `PawnCombatController.ResolveTargets` runs a two-pass occupancy — hostile-affinity hexes filtered to the enemy team, self-affinity hexes filtered to the caster's own team — so a `Self` weapon/payload hits the firing pawn while `Line`/`Cleave`/`Aoe` never touch allies. This is the friendly/self side of the axis the aura work will also need. Verified by `DeliveryResolverTests.SelfHexes_*` (red-green, mutation-proven on the affinity mask). **Follow-up DONE (2026-06-24, ADR-0004 §3):** the self/hostile split is now a standalone **`Affinity`** enum (hostile/friendly/self); `Self` removed from the `DeliveryPattern` enum; the pure `DeliveryAffinity` helper anchors self-deliveries on the caster (v1) and `DeliveryResolver` is pure geometry again. Verified 101/101 green (mutation-proven via `DeliveryAffinityTests`).
- [x] ~~currently combat is broken, pawns do not move to each other.~~ Fixed (2026-06-12): root cause was `Timer` firing `OnRewind` synchronously in `Start()`, so movement resolved instantly + recursively and corrupted the reservation sets. `Timer` now only schedules (fires `OnComplete` on natural elapse; `Stop()` is a silent cancel); movement subscribes to `OnComplete`; `CombatCoordinator` re-checks range before committing a buffered step. Pawn transform now follows `HexPosition` each frame (view-sync).
- cross container drag swaps items:
	the returning item is placed at the outgoing item with its origin cell, not relative to the cell the outgoing item was placed on top of the returning. make it relative to the dropped cell might feel better. -> or just highlight the required slots in the origin inventory, to show the collisions.
- [x] Same-container drag should attempt swap first to match cross-container; fallback to force-pickup only if returning item does not fit at source. **Fixed (2026-06-21, Candidate 3):** same- and cross-container drops share `ITetrisContainer.TrySwapInto`, which returns the displaced item to the freed source cell and only force-picks-up when it won't fit. Verified by `TetrisContainerTests.TrySwapInto_SameContainer_ReturnsDisplacedToFreedSourceCell` (+ mutation-tested).
- [ ] implement required slots highlights again -> use backpack battles as reference.
- adding and removing max resource mods changes the current value. 
	- change implementation or reset on battle start?
		- implement resource gen first and think of giving a bonus while not in combat
- [x] Weapons fire twice when connected to amplifiers from both sides. **Fixed at the source (2026-06-20, Candidate 2):** `ChainResolver` is now weapon-centric and runs one BFS per firing source over all connectors, so `[ampA][weapon][ampB]` is a single firing carrying both amps — no duplicate `(root, weapon)` chains. `ChainCollapser` (the Candidate-1 stopgap) is deleted. Verified by `ChainResolverTests.AmplifierOnBothSides_ProducesOneChainWithBothAmplifiers`.
- [x] **Equidistant reactors drop a firing (found 2026-06-19).** **Fixed (2026-06-20, Candidate 2):** the weapon now owns a deduped list of reactor firing sources (0 → timer, ≥1 → fire per reactor event, timer suppressed); equidistant/parallel reactors all fire. Verified by `EquidistantReactors_OnDifferentEvents_BothFire` (+ mutation-tested). See `Architecture Review.md` §2.

---

# Pending Implementation
## Game Loop

- [x] Pawns should start with a default weapon
- [x] On player death → trigger Game Over. Done (2026-06-14): `CombatCoordinator` raises `OnCombatEnded` on team wipe → `CombatPhase` routes victory→Loot / defeat→`GamePhase.GameOver` (restart button reloads scene). Wipe rule extracted to pure `CombatOutcomeResolver` (+ red-green test). This closes the Placement→Combat→Loot→Placement loop from gameplay.
- [ ] Hex placement phase — not yet wired in scene

## Combat

- [ ] `PawnEffect` firing — system exists but not triggered
- [x] Unit movement design — range-closing behavior, hex occupancy rules (one unit per hex). **Done 2026-06-21 (Candidate 5):** monotone closing to min active-weapon reach on a fixed `CombatClock`; one-unit-per-hex enforced by the snapshot step-rule + closest-wins arbitration (`MovementResolver`). See ADR-0001 / KNOWN_ISSUES tech-debt entry.

## Item Chain

- [ ] Converters + typed signal propagation — the type-reclassifier across axes. **Delivery pattern, affinity, anchor done (ADR-0004 §1); resource type done (ADR-0005 §2, `ConverterAxis.Resource`).** Still deferred: **damage type**, **target strategy**, optionally **trigger event** — blocked on those underlying systems becoming data-driven (`ConverterAxis.cs`). *Not* cadence/frequency, *not* the Shifter's target-selection role.
- [ ] Reactor ally/nearby events — `OnAllyAttacks`, `OnAllyKills`, `OnNearbyEnemyDies` require coordinator access to other pawns

---

## Code Smells / Tech Debt

- [x] **Movement → fixed combat tick.** **Implemented 2026-06-21 (Candidate 5) — see [[0001-range-movement-and-combat-tick|ADR-0001]] + Architecture Review §5.** A project-side **`CombatClock`** (frame-rate-independent fixed-tick accumulator) drives movement: each tick every seeking pawn accrues **move-readiness** (`movementSpeed × tickInterval`) and proposes one A* step against a **frozen snapshot**, then the pure **`MovementResolver`** applies them read-then-write with **closest-to-target-wins** contested-hex arbitration (ties by stable registration id). Range is now a **pawn stat** (`PawnStat.Range` / `PawnConfig.baseRange`); movement is **monotone closing** to the **minimum active-weapon reach**. This **deleted** `_reservedHexes`, the reservation use of `_claimedHexes`, the per-pawn movement `Timer`s, and the on-arrival re-check guard — dissolving the whole stale-decision bug class. Pure seams locked by `CombatClockTests` + `MovementResolverTests`. **Still open:** (a) ~~view-interpolate lerp polish~~ done (2026-06-22); (b) ~~the delivery-pattern split (Decision 2b)~~ shipped 2026-06-23 (ADR-0003), then **Decision 2b withdrawn** by **ADR-0004 §2** — Reach is one uniform pawn stat; ~~`ResolveMinReach`~~ is now `CombatCoordinator.ResolveReach` = `max(1, round(pawn.range))`, no per-weapon `ShapeSize` placeholder — **done**; (c) range pricing/cap, blocked on the balancing table (Decision 2c). **Attack-firing migration is split out → ADR-0001 §8 / Architecture Review §7 (Candidate 7).**
- [ ] `TetrisContainer` with `null` `IPawnStats` — mild smell; replace with `NullPawnStats` null-object pattern when a third statless container type appears
- [x] `ItemTooltipController` calls `ChainResolver.ResolveTopology` on every hover. **Done** — this was the deferred half of Candidate 2 ("resolve once / push topology"): `ITetrisContainer` now owns a lazily-resolved, dirty-flagged `Topology`; `ItemTooltipController` reads `container.Topology` (no per-hover re-resolve). See `Architecture Review.md` §2 (Candidate 2-A).

---

## Deferred (Out of Scope — Revisit Later)

- [ ] Splitter / Merger — branching and merging chains, highest complexity debt
- [ ] Counter-based conditions (every N hits/kills) — a watch-condition on the **Reactor** (the Payload gate is purely economic since ADR-0006), not a standalone root trigger. Explore in the deferred Trigger-condition ADR.

---

## Design Goals

- No combinatorial explosion
- High systemic depth
- Strong player readability
- Emergent gameplay through interaction