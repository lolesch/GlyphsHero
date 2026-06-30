# Item Tooltip Redesign — Design Spec

**Date:** 2026-06-30
**Status:** Draft — pending Leonid's review (do **not** file issues or start slices until Approved)
**Scope:** Full rewrite of the item tooltip (`ItemTooltipController`). Presentation only — no
combat/chain/economy rules change. This is a two-way door (UI), so no ADR; decisions are logged in
the [Slice ledger](#slice-ledger) below.
**Supersedes:** the current `ItemTooltipController.BuildTooltip` legacy text format.

---

## Problem

The current tooltip (`Assets/Code/Runtime/UI/Inventory/ItemTooltipController.cs`) grew on legacy
assumptions and reads poorly:

- The delivery axis prints as robot output — `Single · hits enemies · on target` — which a player
  can't parse (`ItemTooltipController.cs:490`).
- The Alt chain diagram (`BuildChainSentence`, `:218`) draws arrows **opposite** to the grid
  connections, so it actively misinforms.
- The "two states" idea (chained vs unchained / driving vs payload) is half-present and
  inconsistent — payload weapons and attachments use different code paths
  (`AppendAttachmentIdentity`, `AppendPayloadOutput`) with no unifying rule.
- There's no single, legible rule for **what hovering an item tells you about the chain**.

The good bones to **keep and reuse**:

- The before/after **diff machinery** — resolving the chain twice (up-to-but-excluding the hovered
  item vs. including it) via `WeaponStatResolver.Resolve(weapon, contributors)`
  (`ItemTooltipController.cs:268-269`). This is exactly how a positional delta is computed.
- `ChainComponentColors.GetColor` (type → color) and the `Const` palette (`Const.cs:12-17`).
- The role-detection helpers (`IsPayload`, `PrimaryChain`, `OrderedItems`, `ComponentLabel`).
- Container-owned, resolve-once topology (`container.Topology`, `:109`) — no per-hover re-resolve.

## Goal

A single, consistent tooltip whose rules are the same for every item type:

> **Hovering a piece shows that piece's own marginal effect at its position in the chain.
> Hovering the weapon shows the whole chain's resolved result. Alt expands the math.**

Non-goals (YAGNI for this pass): chain-impact drag-compare (only item-vs-item compare ships here);
moving the type-color onto the inventory **grid** outline (tooltip frame only for now); branch
rendering beyond a single breadcrumb glyph (branches aren't implemented yet).

---

## Display model (the locked rules)

### 1. Positional delta math

Each item shows its **marginal effect at its spot** in the apply order, where:

- **Apply order = the `ChainResolver` / `WeaponStatResolver` order** (root → weapon; the order
  `OrderedItems` already produces: `[Root, ...Modifiers]`). Not "outward from the weapon."
- **Baseline = the weapon's base stats**, with each contributor before this item already applied.
  A piece's "previous state" = `WeaponStatResolver.Resolve(weapon, ordered.Take(index))`; "with" =
  `…Take(index + 1)`. The **delta is the difference** — exactly the existing diff path.
- **The weapon is the terminal readout**: it shows the chain's **final resolved totals** (not a
  delta) and **enumerates its contributing pieces** as a list (see §"Weapon piece list").
- A **payload weapon** is not a stat contributor to the root; it shows **its own child delivery**
  (from `PayloadBehavior`) + what it adds to the shared cost pool. No root name, no propagation slot.

> Reading rule for the player: "What does this piece do?" → hover the piece. "What's the whole chain
> doing?" → hover the weapon.

### 2. Both states always visible; Alt expands the math (symmetric)

Every item has two states. **Both are always shown without Alt** — the **active** state emphasized
(bold/primary), the **other** state dim — so the player has agency at a glance.

- **Attachment** (amplifier / shifter / reactor / converter): **chained** (live in a chain) vs
  **unchained** (its loose `IAttachmentItem.affixes` affix). In a chain the chained effect is live
  and the affix is suppressed (ADR-0004 item roles); standalone, the affix is live.
- **Weapon**: **driving** (fires the chain — final totals) vs **payload** (carried child delivery).

**Alt never hides or reveals a state.** Alt does exactly three things:

1. Turns each delta into a **before → after equation** (`+6 dmg` → `12 → 18`;
   reactor `×120% mana` → `[base 3] ×120% = 3.6`).
2. Shows the **weapon's base** values (the driving weapon's "base 12 → final 18").
3. Adds the **breadcrumb** (§4).

### 3. Per-type content (grounded in the data model)

| Type | Carries (verified) | Active delta (no Alt) | Alt adds |
|------|--------------------|------------------------|----------|
| **Weapon — driving** | base `Damage/AttackSpeed/ResourceCost/CostResource/Delivery/Affinity/Anchor` + `Payload` | final resolved totals + piece list | base → final equations; breadcrumb; dim "as payload" line |
| **Weapon — payload** | `PayloadBehavior` | own child delivery (dmg, delivery sentence, timing) + cost-to-pool | base; dim "as driving weapon" line |
| **Amplifier** | `outputMod` (`WeaponOutputModifier`, 1 stat) | marginal **output** delta, e.g. `+6 dmg` | `12 → 18`; dim unchained affix |
| **Reactor** | `ReactorType` + `inputMod` (`WeaponInputModifier`) | firing condition ("fires when hit") + **input** delta (e.g. `×120% mana`) | `[base] ×120% = result`; dim affix |
| **Shifter** | `inputMod` + `outputMod` | input↔output stat move | both equations; dim affix |
| **Converter** | `Axis` + `To{Delivery,Affinity,Anchor,Resource}` | "→ Aoe" (converts **to**) | "Single → Aoe" (**from** → to); dim affix |

> Note: reactors **do** carry a numeric `inputMod` today (`ReactorItem.cs:10,16`), so the
> "× x% mana to trigger" example is real. The per-type renderer must be **additive**: a numeric line
> only appears when that data is non-default (so future fields don't force layout churn).

### 4. Breadcrumb (Alt only) — replaces the backwards arrow diagram

A single horizontal path in real connection order, the hovered item bracketed:

```
Reactor → Amp → [Iron Amp] → Crossblades
```

Built from the topology's connection order (not the inverted `BuildChainSentence`). When branches
exist (not yet implemented), a branch is denoted by a glyph (e.g. `⑂`) rather than drawing
side-paths. The old `BuildChainSentence` (`:218`) and its inverted arrows are **deleted**.

---

## Visual vocabulary

### Type glyph (the type channel)

Color is **reserved for direction** (green = increase / good, red = decrease / worse — keep the
existing `Stat()` coloring, `:351`). So **type is conveyed by a leading glyph**, used **both** in an
item's own header **and** in the weapon's piece list (same glyph in both places):

| Type | Glyph (proposed) |
|------|------------------|
| Weapon (driving) | `⚔` |
| Weapon (payload) | `◈` |
| Amplifier | `◆` |
| Reactor | `▸` |
| Shifter | `⇄` |
| Converter | `↻` |

**Implementation risk (slice 1):** verify these glyphs exist in the tooltip's TMP font atlas. If any
don't render, fall back to a safe ASCII set (e.g. `W / P / A / R / S / C`) — the *map* is the
deliverable, not the specific glyphs.

### Color (frame + direction only)

- **Tooltip frame** = item's type color via `ChainComponentColors.GetColor` / `Const` palette
  (`Const.cs:12-17`: WeaponRoot `#B72C10`, Payload `#9B62C8`, Amplifier `#E0AF3E`,
  Converter `#71675B`, Shifter/Activator `#206BB6`, Reactor `#67B7E0`). Unchanged behavior.
- **In text**, color means **direction only** (green up / red down). Never use color for type inside
  the body — that's the glyph's job. (Grid-outline type color is a deferred future increment.)

### Weapon piece list

Under the weapon's final totals, list each contributing piece, one per line, `glyph + name +
its delta` (delta colored by direction). Example:

```
⚔ Crossblades — 18 dmg · 0.40s · 3 Mana (root gate)
   Strikes a single enemy at the target
   ▸ Reactor   fires when hit
   ◆ Iron Amp  +6 dmg
   ◆ Steel Amp +4 dmg
```

### State emphasis (no dedicated marker)

No badge/extra frame — the **active** state is bold/primary, the **other** is dim. Layout implies it
(decision confirmed; the "extra frame next to type color" idea was dropped).

---

## Delivery sentence vocabulary (verb-led, all axes)

Replace `AxesLine` / `DeliveryWord` / `AffinityWord` / `AnchorWord` (`:490-508`) with a verb-led
sentence builder over the three axes (`DeliveryPattern` × `Affinity` × `Anchor`, plus AoE
`ShapeSize`). All axes always present, as a readable sentence:

- `Single · enemies · target` → **"Strikes a single enemy at the target"**
- `Aoe r2 · enemies · target` → **"Blasts enemies within 2 of the target"**
- `Single · friendly · self` → **"Buffs self"** (Affinity Self/Friendly + Anchor Origin collapse)
- `Aoe r1 · friendly · origin` → **"Heals allies within 1 of self"**

This is a **pure, fully unit-testable** function (every axis combination → one string). It's the
ideal red-green slice. Exact verb table is finalized in slice 2; the *direction* (verb-led sentence,
all axes) is locked.

---

## Interaction

- **Show timing:** fresh hover waits `_showDelay` (0.4s, `:24`); once a tooltip is visible, moving to
  another item shows instantly (existing behavior, keep).
- **Drop shows tooltip:** after the player places/drops an item, show **that item's** tooltip in the
  newly-resolved chain context, using the **same 0.4s** delay (not instant — avoids mid-drag
  flicker). Wire into the drag/drop completion path.
- **No-chain items** (loot, stash, sitting alone): show a clean standalone read (weapon → base
  attack; attachment → its loose affix as the active state).
- **Drag-to-compare (item vs item):** while holding an item over an **occupied** slot, show
  held-item-vs-slot-item standalone stats side by side (e.g. `dmg 8 vs 12 · rate 1.0s vs 0.4s`).
  **Chain-impact compare (how the swap changes the whole chain) is deferred** to a future increment.

---

## Reusable building blocks (extract these as pure, testable units)

To keep the MonoBehaviour thin and the logic red-green testable, factor the rendering into pure
static builders the controller composes (mirrors the repo convention: domain logic is unit-tested,
Unity glue is play-tested):

- `TypeGlyphs.For(item)` → glyph string (slice 1).
- `DeliverySentence.Build(delivery, affinity, anchor, shapeSize)` → string (slice 2).
- `PositionalDelta.Describe(chain, item, detailed)` → the per-item delta block (slices 3–4).
- `TwoStateBlock.Build(item, isChained/role, detailed)` → active+other states (slice 5).
- `Breadcrumb.Build(topology, item)` → the Alt path string (slice 6).
- `CompareBlock.Build(held, slotItem)` → the compare body (slice 8).

The controller (`ItemTooltipController`) stays responsible only for: delay/coroutine, panel
position/frame color, Alt-key polling + relayout, and calling the builders.

---

## Slices (each = one `ready-for-agent` GitHub issue)

Each issue body must be **self-contained** against durable context (CLAUDE.md, CONTEXT.md, this
spec) per the night-shift bar, and reference this spec by path. Dependencies noted; the night runner
takes the lowest-numbered unblocked `ready-for-agent` issue.

| # | Slice | Dep | Verification |
|---|-------|-----|--------------|
| 1 | **Type-glyph map** (`TypeGlyphs`) + reuse frame palette; render glyph in item header. Verify TMP font support; ASCII fallback. | — | unit (glyph per type) |
| 2 | **Delivery verb-led sentence** (`DeliverySentence`); delete `AxesLine`/word maps. | — | unit — every axis combo → phrase (red-green) |
| 3 | **Positional delta model**: weapon terminal totals + piece list (glyphs). Reframe `BuildTooltip` spine. | — | unit via `WeaponStatResolver` diffs |
| 4 | **Per-attachment delta views** (amp/reactor/shifter/converter) using §3 table; additive numeric lines. | 3 | unit |
| 5 | **Symmetric two-state** (both shown, active bold) for attachments **and** weapons (driving/payload). | 3,4 | unit |
| 6 | **Alt = math expansion + breadcrumb**; delete `BuildChainSentence` & inverted arrows. | 5 | unit (breadcrumb order, equation format) |
| 7 | **Drop-shows-tooltip** wiring (same 0.4s, new chain context). | 3 | manual/play-mode |
| 8 | **Drag-to-compare (item vs item)**; `CompareBlock` pure builder + slot-hover wiring. | 3 | unit (builder) + manual (wiring) |

Slices **1, 2, 3** are independent and are the best first picks. `ChainResolverTests` must stay green
throughout (CLAUDE.md behavioural lock). Every new pure builder ships with red-green tests (proven
able to fail) per the project's testing rule.

---

## Files

**Modified**
- `Assets/Code/Runtime/UI/Inventory/ItemTooltipController.cs` — the spine rewrite; delete
  `AxesLine`/`DeliveryWord`/`AffinityWord`/`AnchorWord` (slice 2), `BuildChainSentence` (slice 6),
  re-home the diff into `PositionalDelta` (slice 3).

**New (each needs its Unity `.meta` committed alongside the `.cs`)**
- `Assets/Code/Runtime/UI/Inventory/TypeGlyphs.cs` (slice 1)
- `Assets/Code/Runtime/UI/Inventory/DeliverySentence.cs` (slice 2)
- `Assets/Code/Runtime/UI/Inventory/PositionalDelta.cs` (slices 3–4)
- `Assets/Code/Runtime/UI/Inventory/TwoStateBlock.cs` (slice 5)
- `Assets/Code/Runtime/UI/Inventory/Breadcrumb.cs` (slice 6)
- `Assets/Code/Runtime/UI/Inventory/CompareBlock.cs` (slice 8)
- Matching tests under `Assets/Code/Tests/EditMode/Inventory/` (or a new `UI/` test folder).

(Final file split is the implementing agent's call; the builders may co-locate. The constraint is
that the pure logic is unit-testable without driving Unity lifecycle.)

---

## Slice ledger

**Decisions taken (two-way doors — logged, not ADR'd):**
- Math is **positional delta**, apply order = `ChainResolver` order, baseline = weapon base; the
  weapon is the terminal totals + piece list.
- **Both states always shown** (active bold, other dim); **Alt = math expansion + breadcrumb only**,
  symmetric across weapons and attachments.
- **Type = leading glyph** (reused in header + piece list); **color reserved for direction**; type
  color stays on the frame only.
- Delivery = **verb-led sentence, all axes**.
- Payload view = **own delivery + cost-to-pool only** (drop root name + propagation slot).
- Drop-show uses the **same 0.4s** delay; **no dedicated state marker** (layout implies active).
- Drag-compare = **item vs item** this pass.
- Per-type numeric lines are **additive** (appear only when data is non-default), to future-proof the
  layout against new fields (e.g. richer reactor mods).

**Assumptions:**
- The proposed glyphs render in the tooltip TMP font (verified in slice 1; ASCII fallback if not).
- `IAttachmentItem.affixes` is the canonical "unchained state" for all four attachment types.

**Gaps left open (future increments, not this pass):**
- **Chain-impact drag-compare** (how swapping changes the whole chain).
- **Grid-outline type color** (type color on inventory cells, not just the tooltip frame).
- **Branch rendering** beyond the single breadcrumb glyph (depends on branches being implemented).
