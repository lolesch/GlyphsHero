# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-01 (night run)
**Active branch:** night/2026-07-01
**Current issue:** #8 — [Tooltip 6/8] Alt = math expansion + breadcrumb (**IN PROGRESS** — breadcrumb
half done this chunk; equation/base-value half remains; `ready-for-agent` deliberately kept)
**State:** issue #8 in progress — next session continues #8 (the equation/base-value half).

## What I did (#8, commit 599a06e)

Did the **breadcrumb** half of slice 6 — the part with a clean pure-testable seam:

- New `Assets/Code/Runtime/UI/Inventory/Breadcrumb.cs` (+ `.meta`) — pure builder.
  `Breadcrumb.Build(chain|topology, item)` renders the chain in **real connection order**
  (`OrderedItems` = root → weapon), hovered item bracketed:
  `Reactor → Amp → [Iron Amp] → Crossblades`. Reserves `Breadcrumb.Branch = "⑂"` for future
  branch rendering (unused). Topology overload resolves the item's chain; empty for loose items.
- Deleted `BuildChainSentence` (`ItemTooltipController.cs`) and its `↓` inverted-arrow diagram
  (it walked outward from the hovered item, reading backwards to the grid). The Alt (`detailed`)
  branch now appends `Breadcrumb.Build(chain, item)` — one line, real order.
- New `Assets/Code/Tests/EditMode/UI/BreadcrumbTests.cs` (+ `.meta`) — red-green: ordering (a
  reversed walk fails), bracket placement, separator, empty-chain, topology resolution. All new
  `.cs` shipped with `.meta`.

## Next step (finish #8 — the equation/base-value half)

Spec `Docs/superpowers/specs/2026-06-30-tooltip-redesign.md` §2–§3. Numeric before→after already
works under Alt for damage/cost/rate (existing `Stat(before, after, detailed)`). Still to do:

1. **Converter piece delta** under Alt: show `Single → Aoe` (from → to), not just `→ Aoe`
   (`ItemTooltipController.PieceDeltaText` + `AppendChainOutput`; the "from" axis is on `p.Before`).
2. **Reactor** equation `[base 3] ×120% = 3.6` (spec §2.1) — a reactor's `inputMod` applied to the
   base, rendered as an equation under Alt.
3. **Weapon terminal** `base 12 → final 18` under Alt (`AppendWeaponTerminal` shows only `totals`
   today; add the weapon-base → final equation when `detailed`). Weapon base = `chain.Weapon.Damage`
   etc.; final = `PositionalDelta.Totals(chain)`.

The `detailed` flag is deliberately **not** wired into `TwoStateBlock`/`PositionalDelta.Describe`
yet — that's where the equation expansion would live if factored into the pure builders (cleaner &
testable) rather than added inline in the controller. Prefer extending the pure builders.

## Blockers

None. No design fork hit; no `needs-design` filed. `#8` remains `ready-for-agent` (in-scope work left).

## To verify in Rider / Unity (I cannot compile or run the Test Runner)

1. **Compiles** — `Breadcrumb` + the `ItemTooltipController` deletion/wiring unverified vs the compiler.
2. EditMode suite green: **`BreadcrumbTests`** (new), plus untouched **`ChainResolverTests`** lock,
   `TwoStateBlockTests`, `PositionalDeltaTests`, `AttachmentDeltaTests`, `DeliverySentenceTests`,
   `TypeGlyphsTests`.
3. Hover a chained piece and **hold Alt** → a single-line breadcrumb appears in grid order
   (root → weapon), the hovered item bracketed `[…]`. **No `↓` arrows anywhere.** Release Alt → gone.
4. `→` and `⑂` render in the tooltip TMP font (`→` already in use pre-change; `⑂` is only reserved,
   not yet drawn).
5. **Note (carried from prior slices):** slices 1 & 2 (TypeGlyphs, DeliverySentence) on this branch
   are still pending TMP-atlas / hover verification (issues #3, #4) — glyphs must render for the
   piece list + breadcrumb to read correctly.
