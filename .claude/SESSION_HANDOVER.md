# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-02
**Active branch:** night/2026-07-01
**Current issue:** #10 [Tooltip 8/8] Drag-to-compare — CODE-COMPLETE (slices 8a + 8b done).
`ready-for-agent` removed; awaiting Rider/Unity play-mode verification only.
**State:** No issue is in progress. Next session: run the step-2 picker for the lowest-numbered
`ready-for-agent` issue (or emit `NIGHT_RUNNER_NO_TASKS` if the queue is drained).

## What the last chunk did

Completed **slice 8b of #10** — the drag-to-compare wiring (commit `a84983c`). This closes #10's
agent work; slice 8a (the pure `CompareBlock` builder + tests) landed earlier in `9a332b2`.

- `InventoryDragController` now exposes `ITetrisItem HeldItem => _heldItem` (added to
  `IInventoryDragController`) so a hovered slot can tell whether a drag is in progress.
- `ItemTooltipController` gained a `RequestCompare(held, slotItem, container, x, onRight)` path
  (added to `IItemTooltipController`). Both `RequestShow` and `RequestCompare` now funnel through a
  private `BeginShow(..., compareHeld)`; the show coroutine `ShowAfterDelay` took a `compareHeld`
  param and branches: when non-null it renders `BuildCompare(CompareBlock.Build(held, slot))` with a
  neutral (`LightGray`) frame, no chain topology, no Alt detail. Dedup is keyed on `(item, compareHeld)`
  so switching either side re-renders; `ExecuteHide` clears the new `_compareHeld`/`_pendingCompareHeld`.
- New `BuildCompare(CompareView)` presenter: `Held vs Slot` header + one `label · held vs slot` row
  each; a missing side prints a dim em-dash. Same 0.4s delay / panel / positioning as a normal hover.
- `SlotView.RequestTooltip` routes: occupied slot **and** an item is held → `RequestCompare`;
  otherwise the existing `RequestShow`. Empty slot still returns early (no tooltip, no compare —
  matches the Acceptance "empty slot → no comparison").
- No new assets, no `.meta` changes. `ChainResolverTests` / `CompareBlockTests` untouched.

## Next step

#10 is off the night queue. Pick the next lowest-numbered `ready-for-agent` issue via the step-2
picker. Tooltip-redesign slices #3–#10 are then all code-complete on this branch, pending human
verification (see below).

## Prior chunks still pending human verification (this branch)

Issues #3–#10 (tooltip-redesign slices 1–8) are all code-complete on `night/2026-07-01`,
`ready-for-agent` removed, awaiting Rider/Unity verification.

## Blockers

None.

## To verify in Rider / Unity (slice 8b — play-mode)

1. **Occupied-slot compare:** pick up an item (click/drag), hover an *occupied* slot → after ~0.4s a
   `Held vs Slot` tooltip appears with aligned rows (e.g. `dmg  8 vs 12`, `rate  1.00s vs 0.40s`).
2. **Mismatched types** (weapon held over an amp, etc.) → the non-shared rows show a dim `—` on the
   absent side.
3. **Empty slot → no compare:** hovering an empty slot while holding shows nothing.
4. **Leave / drop:** moving off the slot clears the compare (pointer-exit); dropping the item shows
   the placed item's normal tooltip (slice-7 drop-shows-tooltip still works, held→null re-renders).
5. **Known minor edge (not fixed — cosmetic):** cancelling a drag with **Esc** while the cursor stays
   over the same occupied slot leaves the compare visible until the next pointer move (Esc/Cancel has
   no tooltip ref; pointer-exit and drop are the clearing signals). Confirm whether this bothers you;
   if so it's a tiny follow-up (route Cancel→Hide).
6. `CompareBlockTests` + `ChainResolverTests` still green in EditMode.

Note: branch is `night/2026-07-01` (created before the 2026-07-01→02 date rollover mid-run);
continued on it rather than forking a `night/2026-07-02` mid-issue.
