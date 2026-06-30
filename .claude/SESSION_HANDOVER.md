# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-06-30 (night run)
**Active branch:** night/2026-06-30
**Current issue:** #4 — [Tooltip 2/8] Delivery verb-led sentence (DeliverySentence)
**State:** Slice 2 implemented & committed (f1dca42). `ready-for-agent` removed (scope fully
covered); left open for human verification. Issue #3 (slice 1) is also still awaiting a human
TMP-atlas check.

## What I did

- New `Assets/Code/Runtime/UI/Inventory/DeliverySentence.cs` — pure static
  `Build(DeliveryPattern, Affinity, Anchor, int shapeSize)` → one verb-led sentence over all three
  delivery axes. Verb by affinity+pattern (Hostile Strikes/Pierces/Cleaves/Blasts; Friendly
  Buffs/Heals); singular subject for `Single`, plural for area; location from anchor with Aoe radius
  woven in. Collapses: `Affinity.Self` → "Buffs self"; `Single+Origin` → "{Hurts|Buffs} self".
  `[Flags]` masks describe the most-expansive flag (Aoe ▸ Line ▸ Cleave ▸ Single).
- `ItemTooltipController.cs` — three `AxesLine(...)` call sites now call `DeliverySentence.Build`.
  Deleted `AxesLine` / `DeliveryWord` / `AffinityWord` / `AnchorWord` (replaced by a pointer comment).
- New EditMode `Tests/EditMode/UI/DeliverySentenceTests.cs` — 4 spec examples verbatim + per-axis
  coverage + both self-collapses + flags priority + None. Each pins a distinct exact string.
- `.meta` committed for both new files. Test asmdef already references `UI` + `Data` (no asmdef edit).

## Next step

Issue #4 is code-complete pending human verification. Next session should take the next
lowest-numbered open `ready-for-agent` issue — **#5** ([Tooltip 3/8] Positional delta model) is the
next independent pick (slices 1/2/3 are all independent; 3 reframes the `BuildTooltip` spine and is
the base for 4/5/6/8). Issues #3 and #4 are both left labeled-cleared (human verifies before closing).

## Blockers

None for the night queue. Two human-only checks outstanding: #3 (TMP atlas glyphs) and #4 (run the
EditMode suite + sanity-hover).

## To verify in Rider / Unity

1. Run the EditMode suite: `DeliverySentenceTests` green; `ChainResolverTests` still green (CLAUDE.md
   lock); `TypeGlyphsTests` (slice 1) still green.
2. Sanity-hover a weapon, a payload weapon, and a standalone weapon — the delivery line should now read
   as a sentence (e.g. "Strikes a single enemy at the target"), not the old `· hits · on` robot output.
3. (Carried from slice 1) TMP atlas: confirm the type glyphs render; flip `TypeGlyphs.UseAsciiFallback`
   if any show as boxes.
