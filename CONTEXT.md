# GlyphsHero — Domain Language

The canonical glossary for the auto-battler. Definitions are what a term **is**, not how it's coded. Combat vocabulary is the sharp end — an attack is a sentence across three independent axes, and "delivery" is recursive.

## Attack resolution

**Attack**:
One weapon firing, resolved as a sentence across three independent axes — Target Selection → Delivery Pattern → Propagation. Each axis is modified by a different chain item without disturbing the others.

**Aim Anchor**:
The point an attack aims at — a pawn (locked) or a bare hex. Produced by Target Selection and consumed by Delivery as **geometry that orients the shape**, never as the list of who gets hit.
_Avoid_: "the target" when you mean the anchor; the locked pawn is only hit because it stands on a covered hex.

**Target Selection**:
The axis that picks the aim anchor (Nearest, LowestHP, RandomWithinShape, …). One strategy per weapon at resolution time. Modified by a Shifter (replaces, never stacks).

**Delivery Pattern** (or **Delivery**):
How an attack travels from its origin to its anchor and which hexes it covers — Projectile, Beam, Arc, Dash, Adjacent, Cone, Homing, … Reclassified by a Converter. **Recursive**: every delivery node has its own pattern.
_Avoid_: "Targeting" for this; the `PayloadTargeting` enum name is a legacy mis-naming of a delivery pattern.

**Covered Hexes**:
The hex set a delivery affects. **Damage is resolved by occupancy of this set** — every hostile pawn whose `HexPosition` is in it takes damage — *except* a `Single`/`Homing` pawn-lock, which hits the anchored pawn directly. The aim anchor is an input that shapes this set, not its contents.

**Propagation**:
What an attack spawns **on impact**: child deliveries (Pierce, Fork, Chain, Split, Explode, Return). Added by Payloads and ordered when several stack. Each child delivery recurses through all three axes, anchored at the parent's impact hex.

**Payload**:
A chained item that adds a **child delivery** (its own pattern + shape) triggered on impact. A nested delivery node, not a free-firing second weapon.

**Homing**:
A delivery whose anchor **re-resolves to the (possibly moved) target at impact** instead of freezing at fire time. The deliberate exception to hex-occupancy resolution.

## Space & range

**Reach**:
The maximum hex distance at which a weapon can **acquire** a target — the Target Selection gate. A pawn/weapon stat (see ADR-0001).
_Avoid_: "range" — that word is overloaded across two unrelated axes.

**Shape size**:
The size of a delivery's covered-hex footprint (an Explode's radius, a Line's length). A parameter of the **pattern**, distinct from Reach.
_Avoid_: "range" for this.

**Telegraph**:
The one-tick-ahead display of a delivery's covered hexes (and of a pawn's planned move). The legibility mechanism that makes hex-occupancy damage fair while sprites interpolate between hex states (see ADR-0002).
