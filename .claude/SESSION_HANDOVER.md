# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-01 (night run)
**Active branch:** night/2026-07-01
**Current issue:** #6 — [Tooltip 4/8] Per-attachment delta views (**code-complete this night**, label removed)
**State:** idle — no issue in progress. #6 done; next session picks the next `ready-for-agent`.

## What I did (#6)

- New `PositionalDelta.Describe(ITetrisItem)` — pure, intrinsic builder for each attachment's §3
  **active-delta** content (amplifier output mod; reactor firing-condition **+ input mod**; shifter
  input↔output move; converter `→ target`). **Additive** lines via `IsMeaningful` (flat/percent-add
  ~0 dropped; percent-mult/overwrite always shown). The reactor **input delta** is the concrete §3
  gap the old per-type switch dropped.
- New `PositionalDelta.FiringCondition(ReactorType)` — the reactor firing-condition map, moved out of
  `ItemTooltipController.ReactorWhen` so the attachment view, terminal-rate line, and piece list share
  one map. The 3 controller call sites now delegate; private `ReactorWhen` deleted.
- `ItemTooltipController.ChainedDescription` rewired onto `Describe` (joins additive lines onto one
  `chained:` line). Two-state bold/dim framing in `AppendAttachmentIdentity` untouched (slice 5).
- New `Assets/Code/Tests/EditMode/UI/AttachmentDeltaTests.cs` (+ `.meta`) — red-green over
  `Describe`/`FiringCondition`. All new/changed `.cs` shipped with `.meta`.

## Next step

Nothing in progress. Next session takes the lowest-numbered open `ready-for-agent` issue —
**#7** ([Tooltip 5/8] Symmetric two-state: both states shown, active emphasized). It builds on this
slice's `PositionalDelta.Describe` + the existing `AppendAttachmentIdentity` bold/dim structure, and
extends the two-state to weapons (driving/payload). Note the deliberate boundary left here:
`AppendChainOutput` (weapon-level marginal block still shown on attachment hover) is a candidate for
the slice-5 two-state consolidation.

## Blockers

None. No design fork hit; no `needs-design` filed.

## To verify in Rider / Unity (I cannot compile or run the Test Runner)

1. **Compiles** — `PositionalDelta` additions + controller edits unverified against the compiler.
2. EditMode suite green: **`AttachmentDeltaTests`** (new), **`PositionalDeltaTests`** (slice 3), and
   the **`ChainResolverTests`** lock (untouched — confirm). `DeliverySentenceTests`/`TypeGlyphsTests`
   still green.
3. Hover a **chained reactor**: `chained:` line now reads `fires <when>   ·   <input mod>` — the input
   mod was previously missing.
4. Hover a chained **amp / shifter / converter**: amp `Damage +N`, shifter `↔` move, converter
   `→ <target>` — now additive (a no-op mod prints no line).
5. `·`, `↔`, `→` render in the tooltip TMP font (all already in use pre-change).
6. **Note (carried from prior slices):** slices 1 & 2 (TypeGlyphs, DeliverySentence) on this branch
   are still pending TMP-atlas / hover verification (issues #3, #4) — the glyphs must render for the
   piece list + these attachment lines to read correctly.
