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
Which hexes an attack covers, as a **stackable mask** (`DeliveryPattern`, a `[Flags]` enum) — the covered hexes are the **union** of each set flag's contribution, resolved by `DeliveryResolver`. The size-free patterns derive their footprint from the origin→anchor geometry and scale only by engagement distance under the **Reach** gate; there is no separate shape-size knob for them (ADR-0003). The v1 set: **Single** (the anchor's hex), **Line** (a beam from origin to anchor), **Cleave** (a 3-hex arc, below), **Self** (the origin's hex), and **Aoe** (a disk — payload-only). Reclassified by a Converter; **recursive** — every delivery node has its own pattern.
_Avoid_: "Targeting" for this; the old `PayloadTargeting` enum was a mis-naming, now `DeliveryPattern`.

**Cleave** (aka *Swipe*):
A delivery covering the aim anchor **plus its two same-ring neighbours** — the two of the anchor's six neighbours lying at the same distance from the firing pawn. Always exactly three hexes; needs no facing or angle math, handles diagonal anchors for free, and narrows with distance. The "hit the target and what flanks it" pattern; replaces the rejected Cone (ADR-0003).

**Covered Hexes**:
The hex set a delivery affects. **Damage is resolved by occupancy of this set** — every hostile pawn whose `HexPosition` is in it takes damage. `Single` is *not* a special case: it covers only the anchor's hex, so the locked target is hit because it stands there. The lone (deferred) exception is `Homing`, which re-resolves its anchor to the moved target at impact. The aim anchor is an input that shapes this set, not its contents.

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
An explicit radius for the few size-bearing patterns — currently only the payload-side `Aoe` disk (`PayloadBehavior.ShapeSize`). Most patterns carry no shape-size knob; they scale by engagement distance under the Reach gate (ADR-0003). Distinct from Reach.
_Avoid_: "range" for this.

**Telegraph**:
The one-tick-ahead display of a delivery's covered hexes (and of a pawn's planned move). The legibility mechanism that makes hex-occupancy damage fair while sprites interpolate between hex states (see ADR-0002).
