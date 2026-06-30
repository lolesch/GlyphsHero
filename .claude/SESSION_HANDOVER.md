# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-06-30 (night run)
**Active branch:** night/2026-06-30
**Current issue:** #3 — [Tooltip 1/8] Type-glyph vocabulary (TypeGlyphs) + render in item header
**State:** Slice 1 implemented & committed (7d1e6e6). Awaiting human TMP-atlas verification.

## What I did

- New `Assets/Code/Runtime/UI/Inventory/TypeGlyphs.cs` — pure static `For(ITetrisItem, bool isPayload)`
  → glyph. Unicode map (Weapon `⚔` · Payload `◈` · Amplifier `◆` · Reactor `▸` · Shifter `⇄` ·
  Converter `↻`) with a one-edit ASCII fallback behind `public const bool UseAsciiFallback`.
- Rendered the glyph as a leading element in the tooltip header in `ItemTooltipController.BuildTooltip`
  (reuses existing `isPayload`); frame color / direction-color behavior unchanged.
- Added `FakeConverter` to `Tests/EditMode/Inventory/Fakes/ChainFakes.cs`.
- Added EditMode test `Tests/EditMode/UI/TypeGlyphsTests.cs` (six type/role cases + payload-flag guard);
  expectations gate on `UseAsciiFallback`.
- Added `UI` to the EditMode test asmdef references.

## Next step

Issue #3 is code-complete pending human verification. The next session should take the next
lowest-numbered open `ready-for-agent` issue — **#4** ([Tooltip 2/8] DeliverySentence) is the next
independent pick (or #5 PositionalDelta). #3 is left labeled `ready-for-agent` (human verifies the
TMP atlas before closing).

## Blockers

None for the night queue. One human-only check on #3 (TMP font atlas — see below).

## To verify in Rider / Unity

1. **TMP atlas (the open risk):** hover each item type; confirm `⚔ ◈ ◆ ▸ ⇄ ↻` render (not boxes) in
   the tooltip TMP font. If any fail → set `TypeGlyphs.UseAsciiFallback = true` (one edit) and note
   the ASCII set shipped.
2. Run EditMode suite: `TypeGlyphsTests` green; `ChainResolverTests` still green (CLAUDE.md lock).
3. Confirm the test asmdef's new `UI` reference resolves (assembly compiles).
