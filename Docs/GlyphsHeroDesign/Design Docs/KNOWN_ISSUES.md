
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
- [ ] Unit movement design — range-closing behavior, hex occupancy rules (one unit per hex), not fully designed

## Item Chain

- [ ] Converters + typed signal propagation (damage type, target type, delivery mode, resource type)
- [ ] Reactor ally/nearby events — `OnAllyAttacks`, `OnAllyKills`, `OnNearbyEnemyDies` require coordinator access to other pawns

---

## Code Smells / Tech Debt

- [ ] **Movement → central planner** (with lerp/movement-feel polish). Current per-pawn async-timer movement makes every decision against stale world state — that's why `CombatCoordinator` needs the range re-check guard plus the `_reservedHexes`/`_claimedHexes` bookkeeping. Migrate to a planner that re-plans all pawns against one world snapshot per tick (per-pawn speed via a move-readiness accumulator). Dissolves the whole stale-decision bug class and the reservation hack. Do it together with the lerp polish: the pawn view currently snaps the logical position at step completion and then lerps to catch up — interpolate across the step duration instead.
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