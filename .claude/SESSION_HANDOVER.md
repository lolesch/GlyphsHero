# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-02
**Active branch:** night/2026-07-01
**Current issue:** #10 [Tooltip 8/8] Drag-to-compare — IN PROGRESS (slice 8a done, 8b remains)
**State:** #10 still `ready-for-agent`; resume it next with the slot-hover wiring (8b).

## What the last chunk did

Completed **slice 8a of #10** — the pure `CompareBlock` builder + red-green tests
(commits `9a332b2`, then `4596ea2` removing a stray temp file).

- New `Assets/Code/Runtime/UI/Inventory/CompareBlock.cs` (+`.meta`): pure
  `CompareBlock.Build(held, slotItem)` → `CompareView { HeldName, SlotName, Rows }`.
  Reduces each item to ordered **keyed standalone stats** and aligns by key:
  - Weapon → `dmg` (F1), `rate` (interval `1/AttackSpeed` `0.00s`), `cost` (`F1 [pool]`).
  - Amp/Shifter/Reactor/Converter → own identity modifier(s), keyed by stat/axis; reactor
    also emits a `fires` row (+ additive input line when meaningful).
  - Matched keys → two-sided `CompareRow`; mismatched types → one side `null` (presenter dashes).
- `PositionalDelta.IsMeaningful` / `ConverterTarget` promoted `private`→`internal` for reuse
  (same UI assembly). No behaviour change to PositionalDelta.
- New `Assets/Code/Tests/EditMode/UI/CompareBlockTests.cs` (+`.meta`): matched / mismatched-type /
  reactor / names / null-slot cases; each documents its red-green mutation.
- `ChainResolverTests` untouched.

## Next step (resume #10 — slice 8b: the wiring)

Wire the builder into the drag/hover UI (this is Unity glue, play-mode verified):
1. Detect **holding an item over an occupied slot** during a drag. Look at `SlotView`
   (pointer-enter / hover hooks) and `InventoryDragController` (the carried item + which slot is
   under the cursor). The drop-tooltip path from #9 (`InventoryDragController.DropAt` →
   `ISlotView.ShowTooltip()`) is the nearest precedent for how hover/drag routes to the tooltip.
2. Add a compare render path to `ItemTooltipController` — e.g. `RequestCompare(held, slotItem, …)`
   that formats `CompareBlock.Build(held, slotItem)` into the panel (header `HeldName vs SlotName`;
   each row `label held vs slot`, missing side → `—`). Reuse the existing panel/position/delay
   plumbing; use the same 0.4s show semantics as the rest of the tooltip.
3. **Empty slot → no compare** (Acceptance): only invoke for an occupied target slot; an empty
   slot falls back to the normal held-item standalone read (or nothing).
4. Clear the compare when the drag leaves the slot / ends.

## Prior chunks still pending human verification (this branch)

Issues #3–#9 (tooltip-redesign slices 1–7) are all code-complete on `night/2026-07-01`,
`ready-for-agent` removed, awaiting Rider/Unity verification.

## Blockers

None.

## To verify in Rider / Unity (for slice 8a)

- Test Runner → EditMode → `CompareBlockTests` all green; `ChainResolverTests` still green.
- No Play-mode behaviour yet (wiring is 8b).

Note: branch is `night/2026-07-01` (created before the 2026-07-01→02 date rollover mid-run);
continuing on it rather than forking a `night/2026-07-02` mid-slice.
