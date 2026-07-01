# Session Handover

> Rewritten (overwritten) by the night runner after every task chunk. This is the single
> source of **mid-task state** between fresh sessions within one night. GitHub issues track
> *what* to do and *what's done*; this file tracks *where the last session left off*.

**Last updated:** 2026-07-01 (night run)
**Active branch:** night/2026-07-01
**Current issue:** #7 — [Tooltip 5/8] Symmetric two-state (**code-complete this night**, label removed)
**State:** idle — no issue in progress. #7 done; next session picks the next `ready-for-agent`.

## What I did (#7)

- New `Assets/Code/Runtime/UI/Inventory/TwoStateBlock.cs` (+ `.meta`) — pure builder.
  `TwoStateBlock.Build(item, primaryActive)` → `TwoStateView { Active, Other }` of
  `ItemStateView { Kind, Label, Lines }`. Attachment: `Chained` (from `PositionalDelta.Describe`)
  ⇄ `Unchained` (loose `IAttachmentItem.affixes`); Weapon: `Driving` (base attack) ⇄ `Payload`
  (own child delivery via `PayloadBehavior`, defaults mirroring `AppendPayloadOutput`). Caller
  passes `primaryActive: isChained` (attachment) / `!isPayload` (weapon).
- `ItemTooltipController` rewired to show **both** states always (active bold, other dim; emphasis
  the only marker): `AppendAttachmentIdentity` now delegates to `TwoStateBlock` + a new shared
  `AppendState` renderer (empty state → dim `—`). The chained weapon branch and
  `AppendStandaloneWeapon` append the weapon's dim other-role line ("as payload" / "as driving
  weapon"); existing rich active blocks untouched. Old `ChainedDescription` deleted (folded into
  `AppendState`).
- New `Assets/Code/Tests/EditMode/UI/TwoStateBlockTests.cs` (+ `.meta`) — red-green over the
  Active/Other arrangement + deterministic content lines. All new `.cs` shipped with `.meta`.

## Next step

Nothing in progress. Next session takes the lowest-numbered open `ready-for-agent` issue —
**#8** ([Tooltip 6/8] Alt = math expansion + breadcrumb; delete `BuildChainSentence` & inverted
arrows). It builds on this slice: Alt will extend `TwoStateBlock`/`PositionalDelta` to render
`before → after` equations (the `detailed` flag is deliberately **not** wired into `TwoStateBlock`
yet), add a `Breadcrumb.Build(topology, item)` real-connection-order path, and delete the
backwards `BuildChainSentence` (`ItemTooltipController.cs`) and its inverted arrows.

## Blockers

None. No design fork hit; no `needs-design` filed.

## To verify in Rider / Unity (I cannot compile or run the Test Runner)

1. **Compiles** — `TwoStateBlock` + `ItemTooltipController` edits unverified against the compiler.
2. EditMode suite green: **`TwoStateBlockTests`** (new), plus the untouched **`ChainResolverTests`**
   lock, `PositionalDeltaTests`, `AttachmentDeltaTests`, `DeliverySentenceTests`, `TypeGlyphsTests`.
3. Hover a **chained attachment** → `chained:` bold, `unchained:` dim (or `unchained: —` with no
   loose affix). Hover it **standalone** → emphasis flips.
4. Hover a **driving weapon** (chain) → Attack block + dim `as payload: …`. Hover a **payload
   weapon** → Payload block + dim `as driving weapon: …`. Hover a **loose weapon** → base attack +
   dim `as payload: …`.
5. `·`, `—` render in the tooltip TMP font (both already in use pre-change).
6. **Note (carried from prior slices):** slices 1 & 2 (TypeGlyphs, DeliverySentence) on this branch
   are still pending TMP-atlas / hover verification (issues #3, #4) — the glyphs must render for the
   piece list + these attachment/weapon lines to read correctly.
