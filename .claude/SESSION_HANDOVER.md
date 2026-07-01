# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-02
**Active branch:** night/2026-07-02 (Pawn-UI series #11–#15; stay on this branch for the whole series)
**Current issue:** #13 [Pawn UI 3/5] Inventory-trigger — CODE-COMPLETE. `ready-for-agent` removed;
awaiting Rider/Unity verification only.
**State:** No issue is in progress. Next session: run the step-2 picker for the lowest-numbered
`ready-for-agent` issue (**#14** is next), or emit `NIGHT_RUNNER_NO_TASKS` if the queue is drained.

## What the last chunk did

Completed all of **#13** in one chunk (commit `46d93fb`) — a one-line event swap:

- `Assets/Code/Runtime/UI/Inventory/PawnInventoryView.cs` — `OnEnable`/`OnDisable` now subscribe
  `Show` to `HexSelectionHandler.OnPawnSelected` instead of `OnPawnHovered`. No other behavior change
  (`_view.RefreshView(pawn.Inventory)` unchanged). Also refreshed the stale class doc-comment
  ("hovered" → "selected").
- Robustness bonus: `OnPawnSelected` never fires null (fires on change; `ResolveSelection` returns
  `clicked ?? current`, so empty-clicks don't fire), so `Show(pawn).Inventory` is strictly safer than
  the old hover path (which could fire null on hover-off).
- No tests (one-line swap, no pure seam — per issue spec). No asmdef/interface changes.

## Next step

Pick **#14** [Pawn UI 4/5] Grid status bar: health+mana bars following every pawn — via the step-2
picker. Read spec `Docs/superpowers/specs/2026-07-01-pawn-ui.md` (§Decision 2: world-space canvas child
on `Pawn.prefab`; new `PawnStatusBar` in UI asmdef self-binding from parent `Pawn.Stats` in `Start()`;
respect **UI → Pawns** layering — `Pawn`/`PawnFactory` must NOT reference UI; reuse the fixed
`PawnResourceView`). Note: this issue needs a human to author the prefab child + wiring; the runner
writes C# + a WIRE IN UNITY recipe only. Dependency order: Statistics(#11 ✓) → Selection(#12 ✓) →
Inventory-trigger(#13 ✓) → Grid-bar(#14) → HUD(#15).

## Prior chunks still pending human verification

- **This branch (`night/2026-07-02`):** #11 (Statistics seam), #12 (Selection), #13 (Inventory-trigger).
- **`night/2026-07-01`:** tooltip-redesign slices #3–#10, all code-complete, `ready-for-agent`
  removed, awaiting Rider/Unity verification.

## Blockers

None.

## To verify in Rider / Unity (#13)

1. Hovering a pawn no longer changes the inventory panel.
2. Clicking a pawn shows that pawn's inventory; clicking another switches it.
3. `ChainResolverTests` / `PawnStatsTests` / `HexSelectionPolicyTests` still green (no API changed).
