# Combat Legibility — Implementation Plan

**Date:** 2026-08-27
**Governing decision:** [ADR-0012](../../adr/0012-core-loop-hands-off-and-itemized-enemies.md) — §2 is the *legibility contract* this plan discharges; §5 (enemy inspect) covers Slice E. Read it before starting.
**Nature:** Presentation, plus one new additive combat event. Two-way door — no ADR. Each slice ends with a line in [Slice ledger](#slice-ledger).
**How to run this:** one slice per session. Pick the first unchecked box in [Progress](#progress), do only that slice, commit, tick the box, append its ledger line, stop.

## Progress

- [ ] **A** — `AttackReport` event + post-combat recap
- [ ] **B** — Damage numbers, attributed by source
- [ ] **C** — Per-pawn attack indicator
- [ ] **D** — Covered-hex flash on the grid
- [ ] **E** — Enemy inspect during Placement

`A` is the foundation for `B`/`C`/`D`. `E` is independent — it can run any time, and pairs naturally with `D`.

---

## Before you start (applies to every slice)

**Read:** [`CONTEXT.md`](../../../CONTEXT.md) for *covered hexes*, *delivery*, *chain*, *telegraph*. [`CLAUDE.md`](../../../CLAUDE.md) for asmdef layering (**`Data` is the bottom, `UI` is presentation-only — `Core.Combat` never references `UI`**) and the red-green rule.

**Current state — what a grep won't tell you:**

- `ICombatEventBus` (`Assets/Code/Runtime/Core/Combat/CombatEventBus.cs`) has three events — `OnUnitAttacked` / `OnUnitHit` / `OnUnitDefeated` — all consumed by the reactor wiring in `PawnCombatController` (~L175–219). Extend it **additively**: a new event, existing signatures untouched.
- `PawnCombatController.Fire()` (~L225) and `FirePayloads()` (~L286) already compute `covered` (from `DeliveryResolver`), the hit `targets`, `stats.Damage`, and `result.TotalSpent`. None of it is published — Slice A publishes it.
- `DamageNumberView.Show()` (`Assets/Code/Runtime/UI/Combat/DamageNumberView.cs`) is `private` and fired only by a `[ContextMenu]` test — nothing spawns it during combat.
- No telegraph code exists; ADR-0002's telegraph is design-only. Slices D and E write the first "grid highlight from a delivery" code.
- `CombatClock` is the tick source (ADR-0001 §6). Movement and attack firing both run on it (arch-review #7).
- The pawn's resolved chains live at `Pawn.Inventory.Topology.Chains`, rebuilt by `RebuildAttacks` (`Assets/Code/Runtime/Pawns/Pawn.cs` ~L114–150).

**Reuse:**

- `DeliveryResolver.CoveredHexes(origin, anchor, pattern, shapeSize = 0)` — pure. `Assets/Code/Runtime/Core/Combat/DeliveryResolver.cs`.
- `DeliverySentence` — rules-text, already tested. `Assets/Code/Runtime/UI/Inventory/DeliverySentence.cs`.
- `ChainComponentColors` — role → colour. `Assets/Code/Runtime/UI/Inventory/ChainComponentColors.cs`.
- `PawnStatusBar` — the world-space prefab-child widget pattern. `Assets/Code/Runtime/UI/PawnStatusBar.cs`.
- One-way Core → UI handoff: `GamePhaseController.StashBound` / `LootOfferBound` (static `event` + cached value) — `Assets/Code/Runtime/Core/GamePhaseController.cs` ~L47–52.
- EditMode fakes: `Assets/Code/Tests/EditMode/Inventory/Fakes/`.

**Finishing any slice:**

- Red-green every testable seam — prove the test fails under a mutation before it passes (CLAUDE.md rule). Keep `ChainResolverTests` green.
- Write **C# only**. For prefab / `SerializeField` / scene work, write a `WIRE IN UNITY` note listing exactly what to author; a human does the Unity-side wiring and the Play-mode check (same split as `2026-07-01-pawn-ui.md`).
- Commit. Tick the Progress box. Append to [Slice ledger](#slice-ledger): assumptions / decisions taken / gaps left open.

---

## Slice A — `AttackReport` event + post-combat recap

**Serves** ADR-0012 §2 — the recap ("hands the player the theory of what to change"). Highest learning value; most testable.

**Work:**

1. `CombatEventBus.cs` — add `event Action<AttackReport> OnAttackResolved` and `PublishAttackResolved(in AttackReport)` to interface + impl. Leave the three existing events as they are.
2. Define `AttackReport` (readonly struct, namespace `Code.Runtime.Core.Combat`): `IPawn Attacker`, `IItemChain Chain`, `IWeaponItem Weapon`, `bool IsPayload`, `DeliveryPattern Pattern`, `IReadOnlyList<Hex> CoveredHexes`, `IReadOnlyList<AttackHit> Hits`, `float Spent`. `AttackHit` = `IPawn Victim`, `float Amount`.
3. `PawnCombatController.Fire()` (~L253) and `FirePayloads()` (~L309) — after damage is dealt, assemble the report from the in-scope values and call `PublishAttackResolved`. Root fire: `IsPayload = false`, `Spent = result.TotalSpent`. Payload node: `IsPayload = true`, `Spent = 0` (marginal-spend attribution is out of scope — note it in the ledger).
4. New `CombatRecap` recorder (namespace `Code.Runtime.Core.Combat`). Subscribes to `OnAttackResolved` + `OnUnitDefeated`. Accumulates **per attacker** (fire count, total damage, distinct victims, kills) and **per chain** (fired at least once — yes/no). Emits an immutable `CombatRecapReport` when combat ends.
5. `CombatPhase.cs` owns the recorder lifecycle (create in `Enter`, dispose in `Exit`) and surfaces the report to UI via the `StashBound` pattern — a static `event Action<CombatRecapReport>` + cached value on `GamePhaseController` (or on `CombatPhase` itself).
6. `WIRE IN UNITY`: a recap panel under `Assets/Code/Runtime/UI/Combat/`, shown on entry to the Loot screen, rendering the report as lines. Phrase per-chain and "never fired" lines through `DeliverySentence` where it fits.

**Red-green:** `CombatRecapTests` in `Assets/Code/Tests/EditMode/Combat/`. Drive the recorder with a hand-built sequence of `AttackReport` + defeat events through a fake `ICombatEventBus`; assert per-attacker damage totals, victim counts, and a "chain X never fired" flag. Mutation that must go red: recorder ignores `IsPayload` reports, or drops the never-fired check — the matching asserts fail, the rest stay green.

**Done when:** the recap suite is green and mutation-proven; a Play-mode combat in the existing scene produces a `CombatRecapReport` whose per-pawn damage totals match what you see on-screen; the Loot screen shows the panel. Commit + ledger.

**Depends on:** nothing.

---

## Slice B — Damage numbers, attributed by source

**Serves** ADR-0012 §2 — damage numbers attributed to the chain that caused them.

**Work:**

1. `DamageNumberView.cs` — make `Show(float amount, Color color)` public; apply `color` to the text.
2. New spawner MonoBehaviour in `UI/Combat/`, subscribing to `OnAttackResolved` (surface the bus to UI the same way Slice A surfaced the recap). Per `AttackHit`: instantiate a `DamageNumberView` prefab at the victim's world position; pass the amount and a colour derived from the attacker (per-pawn tint) or the chain's role colour via `ChainComponentColors`.
3. Spawner is UI-side, subscribing to a Core event; Core does not reference it.

**Red-green:** extract the colour choice as a pure function `AttackReport → Color`; test that two different attackers (or chains) map to two different colours — confirm the two outputs actually differ, not just that the function returns *a* colour. Spawn + animation are Play-mode-verified.

**Done when:** the colour-mapping test is green; in Play mode a fight shows floating numbers at victims, and the player pawn's numbers are visibly distinct from an enemy's. Commit + ledger.

**Depends on:** Slice A.

---

## Slice C — Per-pawn attack indicator

**Serves** ADR-0012 §2 — a per-pawn indicator on the `CombatClock` cadence that flashes on trigger.

**Work:**

1. A world-space widget, prefab child on `Pawn.prefab` — follow `PawnStatusBar` (self-binds from the parent `Pawn` in `Start()`, respects UI → Pawns layering).
2. Read the pawn's resolved chains (`Pawn.Inventory.Topology.Chains`) for how many weapon icons to show and their sprites.
3. Cooldown fill: `fraction = f(elapsed, interval)` against the weapon's fire timer / `CombatClock` cadence — extract as a pure helper (0 at fire, → 1 at ready, clamped).
4. Flash the icon on this pawn's `OnAttackResolved`.

**Red-green:** the cooldown-fraction helper. Mutation: replace `elapsed / interval` with a constant — the boundary asserts (0 and 1) go red.

**Done when:** helper test green and mutation-proven; Play mode shows rings filling and flashing in time with fires. Commit + ledger.

**Depends on:** Slice A.

---

## Slice D — Covered-hex flash on the grid

**Serves** ADR-0012 §2 — the covered hexes of each attack shown on the grid as it fires (the post-hoc half of the ADR-0002 telegraph).

**Work:**

1. On `OnAttackResolved`, tint `report.CoveredHexes` on the hex grid with a short fade.
2. **Decide and log (two-way door):** whether `HexSelectionHandler`'s existing hover/selection tilemap highlight (`Assets/Code/Runtime/Core/HexSelectionHandler.cs`) can be driven for this, or a separate combat-highlight layer is cleaner. Record the choice in the ledger.
3. Colour by attacker side, or by `report.Pattern`.

**Red-green:** none directly — pure view over data Slice A already tests. If a "hexes to highlight" helper emerges, test it.

**Done when:** Play mode — a `Line` fire visibly lights the row it rakes; an `Aoe` payload lights its disk. Commit + ledger.

**Depends on:** Slice A. Shares the grid-highlight helper with Slice E — whichever runs first builds it.

---

## Slice E — Enemy inspect during Placement

**Serves** ADR-0012 §5 — the player reads every enemy threat by inspection, during Placement, before committing.

**Work:**

1. On hover of an enemy pawn during `PlacementPhase` (via `HexSelectionHandler.OnPawnHovered`): for each of the enemy's resolved chains, compute `DeliveryResolver.CoveredHexes(enemyHex, sampleAnchor, pattern, shapeSize)` with a sample anchor (toward the nearest player pawn is a reasonable default) and highlight the hexes — shared helper with Slice D.
2. Surface the enemy's `DeliverySentence` text in a card/panel — `PawnCardView` (`Assets/Code/Runtime/UI/Roster/PawnCardView.cs`) is the layout reference.
3. Runs in Placement only, not Combat.

**Red-green:** if the sample-anchor choice becomes non-trivial logic, cover it with a pure test. The `DeliverySentence` path is already tested.

**Done when:** Play mode — hovering an enemy in Placement shows its rules-text and lights the hexes its attack would cover; a `Line` enemy and a `Single` enemy visibly differ. Commit + ledger.

**Depends on:** nothing (uses `DeliveryResolver` directly).

---

## Not in this plan

Deferred by ADR-0012, do not start here: the full one-tick-ahead telegraph (ADR-0002 proper), the Sigil renderer (`Docs/GlyphsHeroDesign/Glyphs/Sigil_Design_Handoff.md`), the loot economy, the item value table. Build the cheap versions above first — they prove the direction.

---

## Slice ledger

<!-- One entry per completed slice: assumptions (two-way doors, review/veto) / decisions taken / gaps left open. -->
