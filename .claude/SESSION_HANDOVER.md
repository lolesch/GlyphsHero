# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-01 (night run)
**Active branch:** night/2026-07-01
**Current issue:** #8 — [Tooltip 6/8] Alt = math expansion + breadcrumb (**IN PROGRESS** — breadcrumb
done (599a06e), axis from→to done (this chunk); reactor-eq + weapon-base→final half remains;
`ready-for-agent` deliberately kept)
**State:** issue #8 in progress — next session continues #8 (the numeric-equation half).

## What I did (#8, this chunk)

Did the **axis from → to** slice of the Alt math expansion — handover item #1 (converter piece
delta), the part with the cleanest pure-testable seam:

- New pure `PositionalDelta.AxisDeltas(PieceDelta, detailed)` — the categorical (non-numeric) axis
  reclassifications a piece makes (Delivery / Affinity / Anchor / cost-pool), read from the piece's
  before/with snapshots. Without Alt each line names only the result (`→ Aoe`); **with Alt** it
  expands to the full move (`Single → Aoe`), spec §3 Converter row. Uncolored semantic strings —
  color stays the presenter's (direction-only) job.
- `ItemTooltipController.PieceDeltaText` — replaced the four inline axis `if`-lines with
  `parts.AddRange(PositionalDelta.AxisDeltas(p, detailed))`. Numeric dmg/rate/cost parts (with their
  green/red coloring via `Stat`) are unchanged.
- New `Assets/Code/Tests/EditMode/UI/AxisDeltaTests.cs` (+ `.meta`) — red-green: the plain/detailed
  pair per axis pins the Alt flag (ignore-flag fails detailed; always-from→to fails plain), plus the
  additive "no axis change → empty" case. Shipped with `.meta`.

## Next step (finish #8 — the numeric-equation half)

Spec `Docs/superpowers/specs/2026-06-30-tooltip-redesign.md` §2–§3. Still to do (both entangle with
the green/red **color** that lives in the controller's `Stat`, so decide the color-vs-structure split
deliberately — don't rush at end-of-budget):

1. **Reactor equation** `[base 3] ×120% = 3.6` (spec §2.1 / §3 Reactor "Alt adds `[base] ×120% =
   result`"). Today `PieceDeltaText` returns *early* for a reactor with only the firing condition —
   it drops the reactor's numeric input delta entirely. The base/result live in `PieceDelta.Before/
   With` on the stat the reactor's `inputMod` targets (AttackSpeed or ResourceCost, per
   `WeaponStatResolver.ApplyInput`); the modifier string (`×120%`) is `reactor.inputMod.modifier`.
   Needs a WeaponInputStat → WeaponStats-field read to pick the right before/after.
2. **Weapon terminal** `base 12 → final 18` under Alt (spec §2.2). `AppendWeaponTerminal` shows only
   final `totals` today. base = `chain.Weapon.Damage`, final = `PositionalDelta.Totals(chain).Damage`.
   `Stat(before, after, detailed)` almost fits but colors the *non-detailed* total green/red vs base
   (terminal should stay plain without Alt) — so add a `Stat`-variant or a small pure equation helper
   rather than reuse `Stat` directly.

Prefer extending the pure builders (`PositionalDelta`) over inline controller formatting, as before.

## Blockers

None. No design fork hit; no `needs-design` filed. `#8` stays `ready-for-agent` (in-scope work left).

## To verify in Rider / Unity (I cannot compile or run the Test Runner)

1. **Compiles** — `PositionalDelta.AxisDeltas` (uses `where T : struct, Enum` + `EqualityComparer<T>`)
   and the `PieceDeltaText` rewire.
2. EditMode suite green: **`AxisDeltaTests`** (new), plus untouched **`ChainResolverTests`** lock,
   `PositionalDeltaTests`, `AttachmentDeltaTests`, `TwoStateBlockTests`, `BreadcrumbTests`,
   `DeliverySentenceTests`, `TypeGlyphsTests`.
3. Hover a chain with a **converter** and **hold Alt** → its piece-list row reads the full move
   (`Single → Aoe`, `pool Mana → Health`); release Alt → collapses to `→ Aoe` / `pool → Health`.
   (Same behaviour for Affinity/Anchor converters.)
4. **Note (carried from prior slices):** slices 1 & 2 (TypeGlyphs, DeliverySentence) on this branch
   are still pending TMP-atlas / hover verification (issues #3, #4) — glyphs must render for the piece
   list + breadcrumb to read correctly. `→` already renders (used pre-change).
