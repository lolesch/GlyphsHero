# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-01 (interactive, finishing a chunk the night runner left mid-slice)
**Active branch:** night/2026-07-01
**Current issue:** #8 — [Tooltip 6/8] Alt = math expansion + breadcrumb (**CODE-COMPLETE** —
`ready-for-agent` removed; awaiting Rider/Unity verification)
**State:** idle — next session should take the lowest-numbered open `ready-for-agent` issue
(#9, per `Docs/agents/night-shift.md`).

## What I did (#8, finishing the numeric-equation half)

A prior unattended session hit its `--max-turns` cap mid-slice and left uncommitted WIP already
implementing both remaining pieces; reviewed it, found the code sound but the reactor-equation half
untested (red-green gap), added the missing tests, and committed:

- `PositionalDelta.ReactorInputEquation(IReactorItem, PieceDelta, bool)` — spec §2/§3 Reactor row:
  the modifier alone without Alt (`ManaCost +5`), or `[base X] modifier = result` with Alt, reading
  whichever `WeaponStats` field the reactor's `inputMod.stat` targets (`AttackSpeed`/`ManaCost`) off
  the piece's own before/with snapshot. `ProcChance` has no backing field (`WeaponStatResolver` drops
  it silently) so it falls back to the label even under Alt. Empty when the modifier is a no-op (same
  threshold as `Describe`).
- `PositionalDelta.BaseFinal(string, string, bool)` — spec §2.2 weapon terminal: `base X → final Y`
  under Alt, plain final value otherwise. Pure equation shape; the caller still owns each stat's own
  formatting/coloring.
- `ItemTooltipController.AppendWeaponTerminal`/`TerminalRate`/`PieceDeltaText` wired to both — the
  weapon terminal now shows `base → final` for dmg and the fire-rate interval under Alt; a reactor
  piece's line now carries its input equation alongside the firing condition.
- `Assets/Code/Tests/EditMode/UI/TerminalEquationTests.cs` (+ `.meta`) — added the missing
  `ReactorInputEquationTests` fixture (AttackSpeed/ManaCost/ProcChance/no-op cases), alongside the
  already-present `BaseFinal` tests.

This closes out the tooltip-redesign spec's Alt math expansion (slice 6) in full — issues #3–#8 are
now all code-complete on this branch.

## Blockers

None. No design fork hit; no `needs-design` filed.

## To verify in Rider / Unity (I cannot compile or run the Test Runner)

1. **Compiles** — `PositionalDelta.ReactorInputEquation`/`BaseFinal` and the three
   `ItemTooltipController` call sites that changed signature (`TerminalRate` now takes
   `chain, baseSpeed, finalSpeed, detailed`).
2. EditMode suite green: **`TerminalEquationTests`** (both fixtures, new `ReactorInputEquationTests`
   included), plus the full existing suite — `ChainResolverTests`, `PositionalDeltaTests`,
   `AttachmentDeltaTests`, `AxisDeltaTests`, `TwoStateBlockTests`, `BreadcrumbTests`,
   `DeliverySentenceTests`, `TypeGlyphsTests`.
3. Hold **Alt** on a driving weapon → dmg and fire-rate interval read `base X → final Y`; release Alt
   → just the final value (unchanged from before this chunk).
4. Hold **Alt** on a reactor piece with a non-zero `inputMod` → its line reads
   `fires <condition>   [base X] <modifier> = <result>`; release Alt → just the modifier label
   (`<stat> <modifier>`); a `ProcChance`-targeting reactor never gets the bracketed equation, Alt or not.
5. **Note (carried from prior slices):** slices 1 & 2 (TypeGlyphs, DeliverySentence) are still pending
   TMP-atlas / hover verification (issues #3, #4) — glyphs must render for the piece list + breadcrumb
   to read correctly.
