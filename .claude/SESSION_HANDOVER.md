# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-02
**Active branch:** night/2026-07-02 (Pawn-UI series #11–#15 — series COMPLETE on this branch)
**Current issue:** #15 [Pawn UI 5/5] Selected-pawn HUD panel — CODE-COMPLETE. `ready-for-agent`
removed; awaiting human panel-authoring + SerializeField wiring + Rider/Unity verification only.
**State:** No issue is in progress. The Pawn-UI series (#11–#15) is fully code-complete. Next session:
run the step-2 picker. If nothing else is `ready-for-agent`, emit `NIGHT_RUNNER_NO_TASKS` and stop.

## What the last chunk did

Completed all C# for **#15** in one chunk (commit `e6bd3ce`):

- `Assets/Code/Runtime/UI/PawnHudView.cs` (+`.meta`) — NEW, `Code.Runtime.UI`. Selected-pawn HUD.
  Subscribes `HexSelectionHandler.OnPawnSelected` (OnEnable/OnDisable); on switch `Unbind()` prev
  then `Bind()` next; unbinds in `OnDestroy` too. Shows icon+name, health/mana fill bars (reuses
  `PawnResourceView`) + numeric `current / max` text, and 4 secondary stats (healthRegen, manaRegen,
  movementSpeed, range). Live: pools via `Resource.OnCurrentChanged` (also forwards max changes),
  stats via `Stat.OnTotalChanged` (#11). `Paint()` on bind for initial values. Toggles a **child**
  `_root` for visibility (component must sit on an always-active object — documented in header).
  Reads `IPawn` only → UI → Pawns layering holds.
- `Assets/Code/Runtime/Pawns/Pawn.cs` — smallest identity addition: `IPawn` gains read-only
  `Sprite Icon` + `string DisplayName`; `Pawn` exposes `_icon` and new `_displayName` (cached from
  `config.name` at spawn). Additive two-way door.
- `Assets/Code/Tests/EditMode/Pawns/HexSelectionPolicyTests.cs` — `StubPawn` implements the two new
  `IPawn` members (throwing) so the EditMode assembly still compiles.

## Next step

Run the step-2 picker:
`gh issue list --state open --label ready-for-agent --json number,title --jq 'sort_by(.number)'`.
The Pawn-UI series is drained; if the list is empty and nothing is in progress, print
`NIGHT_RUNNER_NO_TASKS` and STOP (no commits).

## Prior chunks still pending human verification

- **This branch (`night/2026-07-02`):** #11 (Statistics seam), #12 (Selection), #13 (Inventory-trigger),
  #14 (Grid status bar — needs prefab wiring), #15 (HUD — needs panel authoring + wiring).
- **`night/2026-07-01`:** tooltip-redesign slices #3–#10, all code-complete, `ready-for-agent`
  removed, awaiting Rider/Unity verification.

## Blockers

None.

## To verify in Rider / Unity (#15)

1. Project compiles (new `IPawn.Icon`/`DisplayName`; `StubPawn` satisfies `IPawn`).
2. `HexSelectionPolicyTests` / `ChainResolverTests` / `PawnStatsTests` / `Resource` tests green
   (only additive interface members; no existing API changed).
3. Do the WIRE IN UNITY steps (see issue #15 comment): author the HUD panel, put `PawnHudView` on an
   **always-active** parent, assign a **child** `_root` + all refs.
4. Play: select pawns (both teams) → HUD swaps; numbers live in combat; chaining a LifeMax/range item
   while a pawn is selected updates max live; a 0-mana pawn shows `0 / 0` (no NaN); despawning the
   selected pawn logs no leak/NRE.
