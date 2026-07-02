# Glyph Design Grammar --- Export Sheet

> [!warning] Mostly superseded
> This is the v3/v4-era "frame shape = component class" direction. [[Sigil_Design_Handoff|Sigil_Design_Handoff.md]]'s
> own iteration history records this idea dying by v4 ("still too iconic") — the shipped baseline
> (v7) uses no outer shapes at all; trigger identity is a *line character*, not a frame. §1
> (frame shapes), §2 (core symbols), §4 (color = damage type), §5 (symbol alignment) and §7
> (example chain) are historical only — the taxonomy in §1 also predates
> [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]] ("Activator"/"Condition" don't
> exist as component classes; "Payload" is a weapon *mode*, not a class). §3 (connector geometry)
> is superseded as a *port-typing* spec, but the underlying need — visualizing where/how items
> connect — **is built**: the inventory grid's pip system (`SlotView.SetPipState`).
>
> Still current and worth keeping in mind: **§6** (tile size = power tier), **§8** (design
> consistency rules), **§9** (expansion strategy). Defer to the Handoff doc for the live visual
> system.

This document defines a consistent visual language for glyph items used
in a grid-based tactical game.

A glyph is composed of four orthogonal layers:

Glyph = Frame Shape + Core Symbol + Connector Geometry + Color

Each layer always represents the same type of gameplay information to
keep the icon language readable and scalable.

------------------------------------------------------------------------

## 1. Frame Shape --- Component Class

  Frame Shape   Meaning     Gameplay Role
  ------------- ----------- --------------------------------------
  Circle        Activator   Self‑contained trigger condition
  Diamond       Reactor     External trigger listener
  Square        Weapon      Base ability emitter
  Triangle      Amplifier   Numeric scaling modifier
  Hexagon       Converter   Signal / damage type transformation
  Star          Payload     Additional effect attached to weapon
  Octagon       Condition   Gate for downstream chain effects

Rule: Frame shape **never encodes effect type or damage** --- only the
component class.

------------------------------------------------------------------------

## 2. Core Symbol --- Effect Identity

The pictogram inside the frame describes the specific mechanical effect.

  Symbol Concept   Meaning
  ---------------- ---------------------------
  Flame            Fire damage
  Snowflake        Ice damage
  Droplet          Poison / damage over time
  Arrow            Projectile attack
  Burst            Critical strike
  Cross            Healing
  Shield           Defense
  Lightning bolt   Shock / chain damage

Rules: - Must remain readable at **24px or smaller** - Prefer silhouette
icons over detailed drawings - One effect per glyph

------------------------------------------------------------------------

## 3. Connector Geometry --- Chain Logic

Edges define how glyphs connect within the inventory grid.

  Connector Type   Visual   Meaning
  ---------------- -------- ---------------------------------
  Solid tab        ▣        Structural adjacency connection
  Arrow port       ▶        Directional flow
  Double port      ◇◇       Branching output
  Circular port    ○        Typed signal transmission

Rules: - Ports always appear on the tile edges - Matching port types
connect - Flow arrows always point **away from the source weapon**

------------------------------------------------------------------------

## 4. Color --- Signal / Damage Type

Color represents the type of damage or magical signal traveling through
the chain.

  Color    Signal Type
  -------- --------------------
  Red      Fire
  Blue     Ice
  Green    Poison
  Yellow   Lightning
  Purple   Arcane
  White    Neutral / physical

Rules: - Color applies to the **core symbol and connector glow** - Frame
shapes remain neutral to preserve readability - Converters change
downstream signal color

------------------------------------------------------------------------

## 5. Optional Channel --- Symbol Alignment

Icon position can encode secondary context.

  Placement   Meaning
  ----------- ----------------------
  Top         Offensive modifier
  Bottom      Defensive modifier
  Left        Resource interaction
  Right       Targeting change

Use sparingly to avoid visual overload.

------------------------------------------------------------------------

## 6. Optional Channel --- Tile Size (Power Tier)

  Tile Size   Meaning
  ----------- -----------------
  1×1         Minor glyph
  2×2         Major glyph
  3×3         Legendary glyph

Larger glyphs justify stronger effects but consume more inventory space.

------------------------------------------------------------------------

## 7. Example Chain

Activator → Weapon → Amplifier → Converter → Payload

Visual grammar example:

Circle (timer icon) → Square (arrow icon) → Triangle (flame icon) →
Hexagon (snowflake icon) → Star (poison icon)

Interpretation:

"When timer triggers → fire projectile → increase fire damage → convert
to ice → apply poison effect."

------------------------------------------------------------------------

## 8. Design Consistency Rules

1.  Each visual channel answers exactly one question.
2.  Avoid reusing color or shape for multiple meanings.
3.  Glyphs must remain identifiable at small sizes.
4.  Icons should be readable in monochrome (accessibility).
5.  Keep the symbol vocabulary limited and reusable.

------------------------------------------------------------------------

## 9. Expansion Strategy

New glyphs should be created by combining existing layers rather than
inventing new visual rules.

Example expansions: - New damage types (new colors) - New payload
effects (new symbols) - New connection logic (new connector geometry)

This keeps the glyph language scalable to 100+ items without losing
clarity.


If you want, the next extremely useful step would be creating:

1️⃣ **A 40–60 glyph “starter vocabulary”**  
(Activator, Amplifier, Payload etc.)

2️⃣ **A visual consistency grid for icons**  
(the secret trick AAA UI teams use)

3️⃣ **A glyph combinatorics system**  
so you can generate **hundreds of items without manually designing each one**

That step is where systems like yours become **really powerful**.

https://tyranny.fandom.com/wiki/Core_Sigils