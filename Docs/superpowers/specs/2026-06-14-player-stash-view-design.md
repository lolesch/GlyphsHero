# Player Stash View — Design Spec

**Date:** 2026-06-14
**Status:** Approved (design), pending implementation
**Approach:** A — Composition (pure renderer + per-source presenters)

## Problem

The scene has two `InventoryView` components. Both currently render the **hovered
pawn's** inventory, because `InventoryView` itself subscribes to
`HexSelectionHandler.OnPawnHovered` and calls `RefreshView(pawn.Inventory)`
(`InventoryView.cs:49,56,65`). One of these panels should instead show the
**player stash** (`PlayerData.Stash`), so the player can drag loot from the stash
into pawn inventories — and, importantly, **fiddle with chains inside the stash
and see the resolved results** before committing items to a pawn.

The root cause is a conflation of responsibilities: `InventoryView` both
*renders an `ITetrisContainer`* and *decides which container to show*. The stash
needs the identical renderer (slots, drag, tooltips, **chain overlay/topology**)
with a different source.

## Goal

Separate the "which container" policy from the renderer so the same rendering
machinery can be driven by different sources, and bind one panel to the stash.

Non-goals (YAGNI): stash-specific features (sorting, sell, capacity UI,
stash↔pawn drag rules). The stash behaves like any other `ITetrisContainer`
panel for now.

## Architecture

### 1. `InventoryView` becomes a pure renderer

`InventoryView` keeps everything about *rendering* an `ITetrisContainer`: slot
rebuild, drag controller registration, tooltips, chain overlay, pip/topology
visuals. It loses all knowledge of pawns and hover.

Changes:
- Remove the `OnPawnHovered` subscribe/unsubscribe in `OnEnable`/`OnDisable`
  (`InventoryView.cs:49,56`).
- Remove `RefreshView(IPawn)` from both the class (`:65`) and the `IInventoryView`
  interface (`:164`). The only public verb becomes `RefreshView(ITetrisContainer)`.
- No other behavior changes. Drag registration on enable (`:47-48`) stays — it is
  a renderer concern.

Resulting `IInventoryView`:

```csharp
public interface IInventoryView
{
    IReadOnlyList<ISlotView> Slots { get; }
    void RefreshView(ITetrisContainer container);
}
```

### 2. Two thin presenter components decide the source

Both live in the **UI** assembly (`Assets/Code/Runtime/UI/Inventory/`) and drive
an `IInventoryView` (assigned in the inspector via Utility's serialized-interface
support; fall back to a concrete `InventoryView` field if the drawer is awkward).
Each exposes a plain binding method so the policy is unit-testable without driving
Unity lifecycle/events.

**`PawnInventoryView`** — restores the current behavior, extracted:

```csharp
[SerializeField] private InventoryView _view;   // or serialized IInventoryView

private void OnEnable()  => HexSelectionHandler.OnPawnHovered += Show;
private void OnDisable() => HexSelectionHandler.OnPawnHovered -= Show;

public void Show(IPawn pawn) => _view.RefreshView(pawn.Inventory);
```

**`PlayerStashView`** — binds the renderer to the stash, delivered by event
(see §3):

```csharp
[SerializeField] private InventoryView _view;

private void OnEnable()
{
    if (GamePhaseController.CurrentStash != null)
        Bind(GamePhaseController.CurrentStash);   // late-enable replay
    GamePhaseController.StashBound += Bind;
}

private void OnDisable() => GamePhaseController.StashBound -= Bind;

public void Bind(ITetrisContainer stash) => _view.RefreshView(stash);
```

### 3. Stash delivery is event-based (layering constraint)

**Constraint:** `UI.asmdef` already references `GameLoop.asmdef` (because
`InventoryView` uses `HexSelectionHandler`). Therefore GameLoop **cannot**
reference UI — having `GamePhaseController` hold a `[SerializeField]
PlayerStashView` would create a circular asmdef dependency and fail to compile.

**Solution:** Core publishes the stash via a static event/property; UI listens.
This mirrors the existing `HexSelectionHandler.OnPawnHovered` pattern and
realizes the `//TODO: make this event based` note at `HexSelectionHandler.cs:84`.

On `GamePhaseController` (Core):

```csharp
public static event Action<ITetrisContainer> StashBound;
public static ITetrisContainer CurrentStash { get; private set; }
```

`PlayerData` is created in `GamePhaseController.Awake` (`:54`). Publish the stash
**after** it exists — in `Start()` (after all `OnEnable`s have run, so active
listeners are already subscribed):

```csharp
CurrentStash = PlayerData.Stash;
StashBound?.Invoke(CurrentStash);
```

`CurrentStash` is cached so a panel enabled *after* the broadcast (e.g. toggled
on later) still binds, via the `OnEnable` replay in `PlayerStashView`. The stash
reference is created once and never reassigned (`PlayerData.Stash` is get-only),
so a single broadcast + cache is sufficient.

Also remove the now-dead `using Code.Runtime.UI.Inventory;` in
`GamePhaseController.cs:8` if it is unused after this change (it must not be used,
or the cycle would already exist).

`ITetrisContainer` lives in `Container.asmdef`, which both GameLoop and UI already
reference — no new asmdef references are introduced anywhere.

## Data flow

```
Hover path:   HexSelectionHandler.OnPawnHovered(pawn)
                -> PawnInventoryView.Show(pawn)
                -> InventoryView.RefreshView(pawn.Inventory)

Stash path:   GamePhaseController.Start()  [PlayerData ready]
                -> StashBound(PlayerData.Stash)  (+ CurrentStash cache)
                -> PlayerStashView.Bind(stash)
                -> InventoryView.RefreshView(stash)
```

Dependency arrows stay one-directional: UI → GameLoop, UI → Container,
UI → Pawns. No GameLoop → UI edge is added.

## Files

**Modified**
- `Assets/Code/Runtime/UI/Inventory/InventoryView.cs` — strip pawn/hover policy;
  drop `RefreshView(IPawn)` from class and `IInventoryView`.
- `Assets/Code/Runtime/Core/GamePhaseController.cs` — add `StashBound` event +
  `CurrentStash`; broadcast in `Start()`; remove dead `using`.

**New (each needs its Unity `.meta`; Unity generates it on import — commit the
`.meta` alongside the `.cs`)**
- `Assets/Code/Runtime/UI/Inventory/PawnInventoryView.cs`
- `Assets/Code/Runtime/UI/Inventory/PlayerStashView.cs`

## Scene wiring (manual, in the Unity editor)

1. **Pawn panel** GameObject: add `PawnInventoryView`; set `_view` to its sibling
   `InventoryView`.
2. **Stash panel** GameObject: add `PlayerStashView`; set `_view` to its sibling
   `InventoryView`.
3. No reference to assign on `GamePhaseController` — delivery is via event.

Verify in Play mode: hovering a pawn updates the pawn panel only; the stash panel
shows `PlayerData.Stash` contents (loot added by `AddItems`/`LootPhase`), and
editing chains in the stash shows resolved pip/overlay state.

## Testing

No new unit tests. The presenters are thin Unity glue: their only logic is
one-line delegations (`Show(pawn) => _view.RefreshView(pawn.Inventory)`,
`Bind(stash) => _view.RefreshView(stash)`), and the behavior that actually
matters — the static-event broadcast/cache and `OnEnable` timing — is
integration that's only observable in Play mode. This matches the repo's
convention: every existing EditMode test covers pure domain logic
(`ChainResolver`, stats, `Timer`); there are no MonoBehaviour/glue tests.

Verification is therefore:
- **Existing EditMode suite stays green** (no domain logic changed —
  `ChainResolver` and inventory tests must be unaffected).
- **Play-mode manual check:** hovering a pawn updates the pawn panel only; the
  stash panel shows `PlayerData.Stash` (loot from `AddItems`/`LootPhase`); editing
  chains in the stash shows resolved pip/overlay state.

(Decision: presenter unit tests were considered and deliberately dropped — they'd
require Unity-`Object` fakes injected by reflection for one-line delegations,
ceremony out of step with the rest of the suite.)

## Risks / notes

- **Event timing:** Unity runs all `OnEnable`s before any `Start`. As long as both
  panels are active at scene load, `PlayerStashView` subscribes before
  `GamePhaseController.Start` broadcasts. The `CurrentStash` cache + `OnEnable`
  replay covers panels enabled later.
- **Static event lifetime:** `StashBound` is static; `PlayerStashView` must
  unsubscribe in `OnDisable` (shown) to avoid leaks across scene reloads (the
  game over flow reloads the scene — `GamePhaseController.cs:77-78`).
- **Serialized-interface vs concrete field:** preferring `IInventoryView` matches
  the "program against interfaces" convention; if the custom drawer proves
  awkward in the inspector, a concrete `InventoryView` field is an acceptable
  fallback since the presenters live in the same assembly.
