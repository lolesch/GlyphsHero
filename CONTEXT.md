# GlyphsHero — Domain Language

The canonical glossary for the auto-battler. Definitions are what a term **is**, not how it's coded. Combat vocabulary is the sharp end — an attack is a sentence across independent axes, "delivery" is recursive, and each chain item reclassifies exactly one axis (ADR-0004).

## Chain item roles

Each item touches one axis (ADR-0004 §1):

- **Weapon** — the chain root; the **base** of every axis (base target strategy, delivery pattern, trigger, the stat economy, an optional payload).
- **Amplifier** — **magnitude**: scales output stats up.
- **Shifter** — **economy trade**: moves magnitude between input/output stats. *Nothing else* — it does **not** touch Target Selection.
- **Converter** — **type reclassification on any axis**: damage type, target strategy, delivery pattern, resource type, optionally a trigger event type. Changes the *kind*, never the *amount*.
- **Payload** — **propagation**: spawns a child delivery node on impact.
- **Reactor** — **trigger**: replaces the weapon's timer with a combat event.

## Attack resolution

**Attack**:
One weapon firing, resolved as a sentence across independent axes — Target Selection → Delivery → Propagation. Each axis is reclassified by a different chain item without disturbing the others.

**Aim Anchor** (or **Anchor**):
What an attack's delivery centres on — by default the chosen target (a pawn or hex), but a delivery may anchor on **self/origin** instead. Consumed by Delivery as **geometry that orients the pattern**, never as the list of who gets hit.
_Avoid_: "the target" when you mean the anchor; the locked pawn is only hit because it stands on a covered hex.

**Target Selection**:
The axis that picks the aim anchor (Nearest, LowestHP, RandomWithinShape, self/origin, …). One strategy per weapon at resolution time. Reclassified by a **Converter** (replaces, never stacks) — *not* the Shifter.

**Delivery**:
An attack landing on hexes. Decomposes into four independent things (ADR-0004 §3): **Pattern** × **Layers** × **Affinity** × **Anchor**. **Recursive** — every delivery node (including a payload's child) has its own four.

**Delivery Pattern** (or **Pattern**):
The geometry — *which* hexes, oriented by the anchor and scaled by **Reach**. A **stackable mask** (`DeliveryPattern`, a `[Flags]` enum); covered hexes are the **union** of each set flag's contribution, resolved by `DeliveryResolver`. There is no separate shape-size knob for the size-free patterns (ADR-0003/0004). The v1 set: **Single** (the anchor's hex), **Line** (the ray origin→anchor), **Cleave** (a 3-hex arc, below), and **Aoe** (a disk — payload-only). Names `Single`/`Line`/`Cleave`/`Aoe` are canonical — *not* "Bolt", "Projectile", or "Beam".
_Avoid_: "Targeting" for this (the old `PayloadTargeting` enum was a mis-naming); "shape" (reserve that for authored hex-shapes — aura shapes, `RandomWithinShape`).

**Single**:
The anchor's hex only — **anchor-locked**: it ignores intervening pawns (a Single that stopped at the first body in the way would *be* a `Line`). The locked target is hit because it stands on the covered hex.

**Line**:
The ray of hexes from origin to anchor. The old "Projectile" vs "Beam" split is **not** two patterns — it is `Line` with or without the (deferred) **Pierce** layer.

**Cleave** (aka *Swipe*):
The aim anchor **plus its two same-ring neighbours** — the two of the anchor's six neighbours at the same distance from the firing pawn. Always exactly three hexes; no facing/angle math, diagonal anchors free, narrows with distance. "Hit the target and what flanks it"; effectively half of the old `Adjacent`; replaces the rejected Cone (ADR-0003).

**Delivery Layers**:
Orthogonal resolution flags on a delivery — all **deferred**, homes fixed (ADR-0004 §3): **Pierce** (a `Line` hits all pawns in path, not just the first — about *hitting*, so it lives here, not in Propagation), **LoS/obstacle** (truncate at the first blocking terrain), **Homing**.

**Affinity**:
Whose occupancy a delivery counts — **hostile** (default) / **friendly** / **self**. Its own axis (ADR-0004 §3), promoted out of the old `Self` delivery flag. The friendly/self side the aura/buff work will extend.

**Covered Hexes**:
The hex set a delivery affects. **Damage is resolved by occupancy of this set**, filtered by Affinity — every pawn of the matching team whose `HexPosition` is in it is hit. `Single` is *not* a special case: it covers only the anchor's hex. The lone (deferred) exception is `Homing`. The anchor is an input that shapes this set, not its contents.

**Propagation**:
What an attack spawns **on impact** — child delivery nodes, added by Payloads (ADR-0004 §4). **Not a behavior enum**: the only primitive is **child count** (1, or N via the Splitter item). The old behaviors are child configurations — **Split** = N parallel children (needs a Splitter), **Chain** = nested payloads detonating in sequence (no exclude-already-hit — each still damages everyone in its footprint), **Explode** = an `Aoe`-pattern child, **Return** = an origin-anchored child. **Fork** is removed; **Pierce** moved to a delivery Layer.

**Payload**:
A chained item that adds a **child delivery node** (its own four delivery things) triggered on impact. A nested delivery node, not a free-firing second weapon.

**Homing**:
A delivery whose anchor **re-resolves to the (possibly moved) target at impact** instead of freezing at fire time. The deliberate exception to hex-occupancy resolution. (Deferred.)

## Triggering

**Trigger**:
*When/how often* a weapon fires (was "Delivery Mode" — renamed to end the "delivery" overload, ADR-0004 §5). Owned by the **Weapon** (its timer), the **Reactor** (an event override), and the **Shifter** (attack-speed trades). A **Converter** may reclassify a trigger *event type* (on-hit→on-crit) but never its *frequency*.

## Space & range

**Reach**:
The single, **uniform pawn stat** that is the **acquisition gate** — the distance at which a weapon can acquire a target and the distance a pawn closes to (ADR-0001, ADR-0004 §2). There is no per-weapon reach. **"Melee" vs "ranged" is just Reach = 1 vs Reach > 1** — a pawn archetype, not a weapon or pattern property.
_Avoid_: "range" — that word is overloaded across unrelated axes.

**Shape size**:
An explicit radius for the few size-bearing patterns — currently only the payload-side `Aoe` disk (`PayloadBehavior.ShapeSize`). Most patterns carry no shape-size knob; they scale by engagement distance under the Reach gate (ADR-0003). Distinct from Reach.
_Avoid_: "range" for this.

**Obstacle**:
A blocker, of two distinct kinds (ADR-0004 §6): **pawn occupancy** (dynamic — `Line` stop-at-first vs Pierce; a hard obstacle in pathfinding) and **terrain obstacle** (static — LoS truncation, deferred; impassable terrain). A `Single` ignores pawn occupancy (anchor-locked).

**Telegraph**:
The one-tick-ahead display of a delivery's covered hexes (and of a pawn's planned move). The legibility mechanism that makes hex-occupancy damage fair while sprites interpolate between hex states (see ADR-0002).
