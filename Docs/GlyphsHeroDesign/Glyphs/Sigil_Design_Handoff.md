# Sigil Language — Design Handoff
*Item Chain Visual System — Session Summary*

---

## Context

This document covers the visual language design for item chain components in a tactical puzzle RPG. Each unit has a 2D grid inventory where items connect via adjacency. The sigil system visualizes what each item does at a glance — both standalone and as a composite rune when multiple components are present.

The reference for composite quality is **Tyranny's sigil stacking system**: four clearly distinguishable layers, readable as a whole or layer-by-layer. Noita wand building is the cautionary tale — powerful but too illegible.

---

## Component Taxonomy (Design Context)

> [!info] Superseded naming
> This table predates [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]]. "Activator" is
> now **Shifter** (trades input stat for output stat, not a free-standing trigger), and Converter
> reclassifies Delivery/Affinity/Anchor/Resource — not damage or target (those are deferred). The visual
> decisions below are still current; treat this table as historical context only, and defer to
> [[Item Chaining]] for the live taxonomy.

| Component | Role |
|---|---|
| **Activator** | Self-contained trigger — fires the next action node on its own condition |
| **Reactor** | External event listener — fires when something outside happens |
| **Weapon** | Action node — dual mode: root (own timer) or payload (conditional) |
| **Amplifier** | Modifies nearest upstream action node — numbers/stats |
| **Converter** | Changes output type of nearest upstream action node — damage/target/delivery |

**Flow rules:** triggers precede what they fire · modifiers follow what they modify.

---

## Visual System — Locked Decisions

> [!warning] Unbuilt
> No composite sigil renderer exists yet — items still show a single static `Sprite Icon` per
> config, and the reading order below (trigger → weapon → amplifiers → payload) is currently
> implemented as *text* in the tooltip (`ItemTooltipController`, `TypeGlyphs`, `ChainComponentColors`),
> not as the visual system this doc describes. See `Ideation.md` for an implementation suggestion.
> One adjacent piece **is** already built, though not from this doc: the inventory grid's pip
> system (`SlotView.SetPipState`, `PipState.DeadEnd/Dash/Arrow/RootDash`) renders each item's
> connector position + direction on the grid — the connector-port visualization the old
> `glyph_design_grammar.md` called for (§3) is effectively shipped, just not under that name.

### Four Layers, No Overlap

1. **Trigger** — the connection line entering the weapon
2. **Weapon** — the center mark
3. **Amplifiers** — orbit around the weapon center
4. **Payload** — adjacent and offset, connected by thin dashed line

### Layer 1: Triggers — Line Character IS the Trigger

No outer shape. No dot. No separate icon. The line that connects into the weapon center is the trigger. Its character (shape, rhythm, direction) encodes the trigger identity.

| Trigger | Line character |
|---|---|
| Default timer | Straight vertical line with evenly spaced horizontal ticks |
| Activator — resource full | Spiral coiling inward, tightening, then straightening into weapon |
| Activator — HP below X | Staircase stepping down, with a dashed threshold notch at the crossing point |
| Reactor — ally attacks | Two curves from different angles merging into one line entering weapon |
| Reactor — on hit | Sharp zigzag arriving from outside (sudden, external) |
| Reactor — nearby kill | Loose wave decelerating as it approaches the weapon |

**Design notes:**
- Line enters weapon from any edge — angle and direction are free, creating variant identity
- No dot at the endpoint — the line terminates at the weapon shape itself
- The line character should be readable even at small inventory grid size

### Layer 2: Weapons — Abstract Center Marks

Invented shapes with strong individual silhouettes. No real-world derivation, no combat metaphors. Just marks that read distinctly at small size.

**Root mode** — solid fill + damage type color
**Payload mode** — outline only, reduced opacity (same shape)

Color encodes damage type:
- Physical = gray (#808080)
- Fire = orange-red (#c86020)
- Ice = blue (#3888b0)
- Poison = green (#589830)
- Converter changes this color

Current mark vocabulary (to be expanded):
- W1: asymmetric hook (L-shape with dot terminal)
- W2: forked Y with thick horizontal base
- W3: lens / eye (two arcs, center dot)
- W4: chevron with stacked internal bars
- W5: off-axis crossed arms with capped end

**Design notes:**
- Solid fill vs. outline alone is sufficient to communicate root vs. payload — no additional color change needed
- Each weapon item has a fixed mark identity — same mark appears in root and payload mode

### Layer 3: Amplifiers — Orbital Shapes

Sit adjacent to / surrounding the weapon center. Each has a distinct shape family — not all circular.

| Amplifier | Shape | Logic |
|---|---|---|
| + Damage | Bold downward wedge, heavy top bar | Mass, weight |
| – Cooldown | 270° sweep arc with arrowhead | Motion through time |
| + Range | Short solid base → long dashed extension → arrowhead | Reach |
| + Proc chance | Branching fork, three tips with dots | Splitting probability |
| + Resource | Closed loop with interior plus | Cycling generation |

**Design notes:**
- In composites, amplifiers orbit tight around the weapon mark
- Multiple amplifiers stack around the weapon, each occupying a different orbital position
- Shape families are intentionally varied (wedge, arc, line, fork, loop) for distinctness

### Layer 4: Converters — Three Visual Dimensions

> [!warning] Superseded axis bindings
> This table predates [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]]'s Converter
> axes. The shipped `ConverterConfig` (`ConverterAxis`) reclassifies **Delivery / Affinity / Anchor
> / Resource** — not damage type or target strategy, which remain deferred (no data system exists
> for either; see `KNOWN_ISSUES.md` → Item Chain). The three-dimension *idea* (color/size/stroke)
> is still sound, but needs rebinding to real axes, e.g. size ↔ `DeliveryPattern` (Single→Cleave→Aoe
> scaling), some marker ↔ `Affinity`/`Anchor`. Damage-type color has no backing enum at all today —
> see `Ideation.md` for the open question that raises.

Converters modify the nearest upstream action node's output type. They don't add a new shape — they change a property of the existing weapon mark.

| Conversion type | Visual dimension |
|---|---|
| Damage type *(deferred — no damage-type system in code)* | **Color** — weapon mark shifts to damage type color |
| Target type *(deferred — no target-strategy system in code)* | **Size** — same weapon mark shape, scaled up for AoE |
| Delivery mode *(shipped as `ConverterAxis.Delivery`)* | **Stroke thickness** — thin = instant · thick/heavy = accumulated burst |

**Design notes:**
- These three dimensions don't conflict — a single converter can only change one dimension
- Multiple converters on the same weapon compound: fire + AoE = large orange mark
- Delivery thickness is the most subtle — may need contrast boosting at small sizes

---

## Composite Rune Reading Order

1. **Trigger line** — what fires this?
2. **Weapon center mark** — what does it do? (color = damage type, size = target type, thickness = delivery)
3. **Orbital amplifiers** — how are the numbers scaled?
4. **Payload offset** — is there a conditional secondary effect?

At a glance: shape character of the line + weapon silhouette tells you 80% of the story. The rest is detail.

---

## What's Not Resolved

- **Payload condition visualization** — how is the payload's condition shown within the payload mark? Current approach: the payload is just the weapon mark outline; its condition is implied by context. May need a small marker.
- **Multiple amplifier positioning** — when 2-3 amplifiers orbit one weapon, where exactly do they sit? Needs a consistent layout rule (e.g. top/left/right reserved positions).
- **Converter indicator** — should the converter item itself have any visual mark, or is it purely implicit (weapon mark changes, no converter symbol visible in composite)?
- **Bidirectional chains** — if two weapons are connected, how does the composite show both with their respective trigger lines? Not yet designed. **Partially resolved in code since this doc was written:** weapon→payload chaining is enabled in `ChainResolver` (a Reactor acts as the chain boundary between two weapons), with a positional tiebreak for directionality as an interim measure pending [[0007|ADR-0007]] (design-only, age-stamp origin deferred). The *visual* question — how the composite shows a weapon-as-payload downstream of a weapon-as-root — is still open.
- **Scale testing** — all concepts at ~80-120px. Needs testing at actual inventory grid cell size (likely much smaller).

---

## Reference Files

- `sigil_concept_v7.html` — latest iteration, all components + composites
- `sigil_concept_v4.html` — zone map diagram showing layer structure
- `archive/sigil_concept_v1.html` – `v3.html` — earlier iterations, superseded, kept for history only (see Iteration History below)
- [[Item Chaining]] — full system design doc (chain grammar, component rules, flow rules); the live taxonomy of record
- `glyph_design_grammar.md` — mostly superseded by this doc (see its own status banner); §6 (tile size = power tier), §8 (consistency rules), §9 (expansion strategy) are still current and worth keeping in mind

---

## Iteration History (condensed)

- **v1** — first composite attempt, good density but no system behind shapes
- **v2** — Tyranny-inspired spatial zones, better structure
- **v3** — outer shape as category, per-stat sub-symbols; composites too cluttered
- **v4** — outer shape = trigger silhouette, condition notch on weapon; still too iconic
- **v5** — rune-like triggers, expressive weapon shapes, varied amplifiers; condition zone too separate
- **v6** — trigger as connection line introduced; weapons still too derived; composites started recovering
- **v7** — trigger = pure line character, no dot, no outer shape; invented weapon marks; tight composites; **current baseline**
