
# Core Loop
the minimal loop would be:
- spawn phase
- [x] spawn pawns on predefined locations 
	- [x] predefined items in inventory.
	- [x] adjust item positioning
	- [x] implement payload effects (geometry/propagation done, ADR-0004/0006; status/terrain payload effects still unwired — see `Status Effects.md`)
- combat phase
	- [x] start combat
	- [x] inventories fire their weapon chains
		- [x] target evaluation

## Extended Loop
- deployment phase
	- [~] store units in a roster — `RosterView`/`PawnCardView` scaffolded, drag-to-deploy not wired
	- [ ] deploy pawns from roster to grid
- reward phase
	- [x] earn item rewards


- [ ] re-implement patch version number

- [~] unit placement — see roster scaffolding above
  - [~] Unit bank — `RosterView` exists, unfinished
- [ ] unit synergy effects — `PawnEffect` is data-only, not integrated into combat resolution
  - [ ] apply effect
- [x] unit inventory