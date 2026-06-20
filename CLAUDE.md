# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A Unity **auto-battler** game. Unity **6000.3.9f1** (Unity 6), Universal Render Pipeline, new Input System, 2D tilemap (hex grid). The user develops via the Rider IDE integration. Terminal is Windows PowerShell — use the PowerShell tool, not Bash/WSL.

## Building & Running

There is no CLI build/run workflow. Code is compiled and the game is run from the **Unity Editor** (Rider drives the same editor). Open the project in Unity and use Play mode. Rider compiles on save and surfaces errors in the IDE.

## Tests

Tests use the **Unity Test Framework** (NUnit) plus **FluentAssertions**, and run through the editor's **Test Runner** window (`Window > General > Test Runner`), not from the command line. The EditMode test assembly is `GlyphsHero.Tests.EditMode` (`Assets/Code/Tests/EditMode/`). To run a single test, use the Test Runner tree or Rider's gutter run icons; there is no per-test CLI invocation configured.

NUnit and FluentAssertions DLLs are vendored via NuGet (`Assets/NuGet.config`, `Assets/packages.config`, `Assets/Packages/`) and referenced directly by the test asmdef's `precompiledReferences`.

`ChainResolverTests` is the behavioural lock on the most complex/bug-prone code (`ChainResolver`); keep it green when touching chain resolution.

## Assembly Architecture

Code is split into Unity assembly definitions (`.asmdef`). Dependencies flow one direction — **`Data` is the dependency-free domain bottom; `GameLoop` is the top integrator.** Respect this layering when adding references (asmdefs reference each other by GUID).

- **`Data`** (`Assets/Code/Data/`) — ScriptableObject configs (`ItemConfig` + subtypes, `PawnConfig`, `EncounterConfig`, `TerrainCostConfig`) and all enums. Pure data; references only Utility. Authored assets, not runtime behaviour.
- **`Utility`** (`Assets/Submodules/Utility/`) — **git submodule** (`github.com:lolesch/Utility.git`). Shared helpers: `Hex`/hex math, `Timer` (player-loop driven), shape inspectors, serialized-interface support, extensions. Don't treat edits here as project-local — they belong to a separate repo.
- **`Statistics`** (`.../Modules/Statistics/`) — stat/modifier system. `Stat` wraps a `MutableFloat`; `Resource` (health/resource pools) builds on it. `PawnStats` is the per-pawn stat block built from a `PawnConfig`.
- **`Container`** (`.../Modules/Inventory/`) — the Tetris-grid inventory and the **chain system** (see below). Runtime item types (`WeaponItem`, `AmplifierItem`, etc.) wrap their `ItemConfig`. `ItemFactory.Create` maps a config to its runtime item.
- **`Grids`** (`.../Modules/HexGrid/`) — hex grid controller, `HexPathfinder` (A*), terrain.
- **`Pawns`** (`.../Pawns/`) — `Pawn : IPawn` (units), `PawnFactory`, `PawnRegistry`. `PawnRegistry` is the source of truth for who's on the board and fires `OnPawnRegistered`/`OnPawnUnregistered`.
- **`GameLoop`** (`.../Core/`) — phase state machine and combat. Integrates everything above.
- **`UI`** (`.../UI/`) — views (inventory, roster, combat damage numbers, tooltips). Presentation only.

## Core Runtime Flows

**Game phase loop** — `GamePhaseController` (MonoBehaviour) owns a state machine cycling `Placement → Combat → Loot → Placement`. Each phase is an `IGamePhase` (`PlacementPhase`, `CombatPhase`, `LootPhase`) with `Enter()`/`Exit()`; the controller only coordinates transitions and wires dependencies in `Awake`. `PlayerData` (stash, current encounter) is constructed here.

**Combat** — `CombatCoordinator` is the scene-level authority for combat. It owns one `PawnCombatController` per pawn, per-unit targeting (`TargetSelector`), hex reservation (claimed vs. reserved hexes to avoid collisions), and movement timers. Pawns self-register through `PawnRegistry`; the coordinator reacts to registration events. Core per-unit decision is `EvaluateUnit`: pick a target in range or path one step toward the nearest enemy via `HexPathfinder`. Events flow over `CombatEventBus`.

**Chain system** (the conceptual heart, and the trickiest code) — items are placed on a pawn's `TetrisContainer` grid. Adjacent items connect through directional `ChainConnector`s. `ChainResolver.ResolveTopology` walks these connections (BFS) to build `ItemChain`s: each chain has a **root** (resolved by walking upstream to the furthest trigger — a `Shifter`/`Reactor` — else the weapon itself), a **weapon**, and ordered **modifiers**. Connection legality is enforced in `IsValidConnection`. A chain is only kept if it contains a weapon. Combat weapon range, damage, etc. are derived from a pawn's resolved chains. When changing inventory adjacency, connector, or item-type rules, update `ChainResolver` and its tests together.

## Conventions

- Namespaces mirror folders: `Code.Data.*`, `Code.Runtime.Core(.Combat)`, `Code.Runtime.Modules.{Inventory,Statistics,HexGrid}`, `Code.Runtime.Pawns`, `Code.Runtime.UI.*`, `Submodules.Utility.*`.
- Systems are programmed against **interfaces** (`IPawn`, `ITetrisContainer`, `ICombatCoordinator`, `IGamePhase`, `IItem`/`ITetrisItem`); prefer the interface over the concrete type at call sites and in tests (fakes live in `Tests/EditMode/Inventory/Fakes/`).
- Inspector attributes come from **NaughtyAttributes** (`[ReadOnly]`, `[Min]`, `[ContextMenu]`) and Utility's custom drawers (`[PreviewIcon]`, shape/interface drawers). Config-driven design: tunable values live on ScriptableObjects in `Data`, not hardcoded.
- `.meta` files are part of the repo — never create/move/delete Unity assets without their `.meta`.

## Working sessions

- **Context budget:** keep a session under **200k tokens of context**, and avoid exceeding ~100k by much when it can be helped. Prefer wrapping up and starting a fresh session (leaning on memory + this file) over letting one session balloon — long sessions get slower and lose focus.

## Agent skills

### Issue tracker

Issues, PRDs, and triage live in GitHub Issues at `lolesch/glyphshero` (via the `gh` CLI). External PRs are **not** a triage surface. See `Docs/agents/issue-tracker.md`.

### Triage labels

Canonical label vocabulary, unmapped: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `Docs/agents/triage-labels.md`.

### Domain docs

Single-context repo. `CLAUDE.md` is the current architecture reference; `CONTEXT.md` / `Docs/adr/` are created lazily as terms and decisions get resolved. See `Docs/agents/domain.md`.
