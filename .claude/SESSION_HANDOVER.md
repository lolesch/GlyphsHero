# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-02
**Active branch:** night/2026-07-02 (Pawn-UI series #11–#15; stay on this branch for the whole series)
**Current issue:** #14 [Pawn UI 4/5] Grid status bar — CODE-COMPLETE. `ready-for-agent` removed;
awaiting human prefab-wiring + Rider/Unity verification only.
**State:** No issue is in progress. Next session: run the step-2 picker for the lowest-numbered
`ready-for-agent` issue (**#15** is the only one left), or emit `NIGHT_RUNNER_NO_TASKS` if drained.

## What the last chunk did

Completed all C# for **#14** in one chunk (commit `954b617`):

- `Assets/Code/Runtime/UI/PawnResourceView.cs` — added **initial paint** (`SetPawn` now calls a new
  private `Paint()` after binding, so the bar shows current fill immediately instead of waiting for the
  first `OnCurrentChanged`), and an **`OnDestroy`** that unsubscribes `OnCurrentChanged` (leak fix; the
  rebind-unsubscribe path was already present). `Paint()` reads the #11 NaN-safe `resource.Percentage`
  and null-guards `bar`/`resource`.
- `Assets/Code/Runtime/UI/PawnStatusBar.cs` (+ `.meta`) — NEW, `Code.Runtime.UI`. World-space canvas
  child of `Pawn.prefab`; `Start()` does `GetComponentInParent<Pawn>()` then binds two serialized
  `PawnResourceView` children (`healthBar`, `manaBar`) to `pawn.Stats.health`/`.mana` via `SetPawn`.
  Defensive null logs. Reads a `Pawn` only — UI → Pawns layering respected (UI asmdef already refs
  Pawns GUID `fc25358…`).
- No tests (MonoBehaviours, no pure seam; NaN case already locked by #11's Resource test).

## Next step

Pick **#15** [Pawn UI 5/5] Selected-pawn HUD panel — via the step-2 picker. Read spec
`Docs/superpowers/specs/2026-07-01-pawn-ui.md` (§Decision 3: separate panel, shares only the selection
source `HexSelectionHandler.OnPawnSelected` from #12; shows icon+name, health+mana bars WITH numeric
current/max text, and secondary stats healthRegen/manaRegen/movementSpeed/range; **fully live** — pools
via `Resource.OnCurrentChanged`, every stat incl. max via `Stat.OnTotalChanged` from #11; unsubscribe
previous pawn on switch + on destroy; hidden until first selection; any pawn incl. enemies).
`ItemTooltipController` is the reference for a lifecycle-managed ref-wired panel. Likely runner writes
C# + a WIRE IN UNITY recipe; human authors the panel + SerializeField wiring.
Dependency order: Statistics(#11 ✓) → Selection(#12 ✓) → Inventory-trigger(#13 ✓) → Grid-bar(#14 ✓) → HUD(#15).

## Prior chunks still pending human verification

- **This branch (`night/2026-07-02`):** #11 (Statistics seam), #12 (Selection), #13 (Inventory-trigger),
  #14 (Grid status bar — needs prefab wiring per its WIRE IN UNITY recipe).
- **`night/2026-07-01`:** tooltip-redesign slices #3–#10, all code-complete, `ready-for-agent`
  removed, awaiting Rider/Unity verification.

## Blockers

None.

## To verify in Rider / Unity (#14)

1. Do the WIRE IN UNITY steps on `Assets/Prefabs/Pawn.prefab` (canvas child + two Image bars + two
   `PawnResourceView` + `PawnStatusBar` with `healthBar`/`manaBar` assigned).
2. Play: every spawned pawn (both teams) shows both bars at correct **initial** fill immediately.
3. A 0-mana pawn shows an empty mana bar, **no NaN**.
4. Bars track damage/regen live during Combat; destroying a pawn logs no leak/NRE.
5. `ChainResolverTests` / `PawnStatsTests` / `Resource` tests still green (no API changed).
