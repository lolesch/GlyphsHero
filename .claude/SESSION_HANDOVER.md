# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-01 (night run)
**Active branch:** night/2026-07-01
**Current issue:** #5 — [Tooltip 3/8] Positional delta model (**code-complete this night**, label removed)
**State:** idle — no issue in progress. #5 done; next session picks the next `ready-for-agent`.

## What I did (#5)

- New `Assets/Code/Runtime/UI/Inventory/PositionalDelta.cs` — pure, Unity-free builder:
  - `Totals(chain)` → the chain's final resolved `WeaponStats` (the weapon's terminal readout).
  - `Pieces(chain)` → ordered `PieceDelta` list (root → modifiers apply order), each carrying the
    resolved snapshot `Before`/`With` that piece. **Weapons excluded** (driving weapon = terminal
    readout; payload weapons aren't stat contributors — summarised separately).
- `ItemTooltipController` — reframed the weapon-hover path: a driving weapon now shows terminal totals
  (dmg · rate · cost + pool + delivery sentence) followed by a glyph-tagged **piece list** (one line
  per contributing piece, `glyph + name + directional delta` via the existing `Stat()` helper).
  Non-weapon pieces still route to the old `AppendChainOutput` (slice 4 / issue #6 refines those).
- Trimmed `AppendPayloadOutput`: header is just `Payload` — dropped the root name and the
  "(#n in propagation)" slot text (spec §1). Its own delivery + cost-to-pool lines are unchanged.
- New `Assets/Code/Tests/EditMode/UI/PositionalDeltaTests.cs` — red-green over `PositionalDelta`
  (weapon excluded from pieces, per-piece before/with damage, reactor-root ordering, payload
  exclusion, no-weapon empties). All new `.cs` shipped with `.meta`.

## Next step

Nothing in progress. Next session takes the lowest-numbered open `ready-for-agent` issue —
**#6** ([Tooltip 4/8] per-attachment delta views) builds directly on this slice's
`PositionalDelta.Pieces` + the `PieceLine`/`PieceDeltaText` helpers.

## Blockers

None. No design fork hit; no `needs-design` filed.

## To verify in Rider / Unity (I cannot compile or run the Test Runner)

1. **Compiles** — the controller edits and `PositionalDelta` are unverified against the compiler.
2. Run EditMode suite: `PositionalDeltaTests` green; **`ChainResolverTests` still green** (CLAUDE.md
   lock — untouched, but confirm). Also `TypeGlyphsTests` / `DeliverySentenceTests` still green.
3. Hover a **driving weapon** in a chain: it should read as *terminal totals* (dmg · rate · cost
   [pool] · delivery sentence) followed by a **piece list** (`glyph name  +N dmg`, coloured by
   direction), plus the payload summary line when payloads exist.
4. Hover a **payload weapon**: header is just `Payload` — no root name, no "(#n in propagation)".
5. Sanity: hovering an amp/reactor still shows its own per-piece view (unchanged old path).
6. **Note:** slices 1 & 2 (TypeGlyphs, DeliverySentence) live on this branch but are still pending
   your TMP-atlas / hover verification (issues #3, #4) — the piece list depends on the glyphs rendering.
