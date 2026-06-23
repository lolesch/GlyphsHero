
# TODO

- [ ] resource regen on pawns.
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
- [x] ~~**`Self` delivery no longer self-damages — likely revert (2026-06-23, ADR-0003).**~~ **Fixed (2026-06-23).** The delivery-pattern split made damage uniform hex-occupancy filtered to hostiles. `DeliveryPattern.Self` covers the caster's own hex, but an enemy never stands there, so a `Self` payload hit no one — silently killing the **deliberate self-hurt build-around** (a player intentionally taking damage to drive `OnSelfHit`/threshold builds). Fix: `Self` is now resolved as a *self-affinity* delivery via the new pure `DeliveryResolver.SelfHexes` (the self-affinity subset of the footprint, currently the origin hex). `PawnCombatController.ResolveTargets` runs a two-pass occupancy — hostile-affinity hexes filtered to the enemy team, self-affinity hexes filtered to the caster's own team — so a `Self` weapon/payload hits the firing pawn while `Line`/`Cleave`/`Aoe` never touch allies. This is the friendly/self side of the axis the aura work will also need. Verified by `DeliveryResolverTests.SelfHexes_*` (red-green, mutation-proven on the affinity mask).
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

- [ ] Converters + typed signal propagation (damage type, target type, delivery mode, resource type)
- [ ] Reactor ally/nearby events — `OnAllyAttacks`, `OnAllyKills`, `OnNearbyEnemyDies` require coordinator access to other pawns

---

## Code Smells / Tech Debt

- [x] **Movement → fixed combat tick.** **Implemented 2026-06-21 (Candidate 5) — see [[0001-range-movement-and-combat-tick|ADR-0001]] + Architecture Review §5.** A project-side **`CombatClock`** (frame-rate-independent fixed-tick accumulator) drives movement: each tick every seeking pawn accrues **move-readiness** (`movementSpeed × tickInterval`) and proposes one A* step against a **frozen snapshot**, then the pure **`MovementResolver`** applies them read-then-write with **closest-to-target-wins** contested-hex arbitration (ties by stable registration id). Range is now a **pawn stat** (`PawnStat.Range` / `PawnConfig.baseRange`); movement is **monotone closing** to the **minimum active-weapon reach** (`ResolveMinReach`, v1: range-fixed weapon → 1, range-scaling → pawn range). This **deleted** `_reservedHexes`, the reservation use of `_claimedHexes`, the per-pawn movement `Timer`s, and the on-arrival re-check guard — dissolving the whole stale-decision bug class. Pure seams locked by `CombatClockTests` + `MovementResolverTests`. **Still open:** (a) the view-interpolate-between-ticks lerp polish in `Pawn.Update` (still exponential snap-smoothing); (b) the full delivery-pattern split (Decision 2b, Projectile/Beam/Arc vs Adjacent/Dash) — v1 classifies range-fixed by `Payload.Range ≤ 1`; (c) range pricing/cap, blocked on the balancing table (Decision 2c). **Attack-firing migration is split out → ADR-0001 §8 / Architecture Review §7 (Candidate 7).**
- [ ] `TetrisContainer` with `null` `IPawnStats` — mild smell; replace with `NullPawnStats` null-object pattern when a third statless container type appears
- [ ] `ItemTooltipController` calls `ChainResolver.ResolveTopology` on every hover. Acceptable for a dev tool. Upgrade path: cache topology on `OnContentsChanged` in `InventoryView` and pass the cached result to `Show()`. **This is the deferred half of Candidate 2 ("resolve once / push topology"):** the firing-model rework (Candidate 2-B) shipped 2026-06-20; topology *ownership* across the UI consumers (InventoryView, ItemTooltipController) is the remaining 2-A. See `Architecture Review.md` §2.

---

## Deferred (Out of Scope — Revisit Later)

- [ ] Splitter / Merger — branching and merging chains, highest complexity debt
- [ ] Counter-based conditions (every N hits/kills) — valid as a condition type on Reactor or Payload, not as a standalone root trigger. Explore as condition type extension when payload conditions are expanded.

---

## Design Goals

- No combinatorial explosion
- High systemic depth
- Strong player readability
- Emergent gameplay through interaction