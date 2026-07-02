# GlyphsHero — Domain Language

The canonical glossary for the auto-battler. Definitions are what a term **is**, not how it's coded. Combat vocabulary is the sharp end — an attack is a sentence across independent axes, "delivery" is recursive, and each chain item reclassifies exactly one axis (ADR-0004).

> This glossary is the **current truth** of each concept. A `(ADR-NNNN)` tag points at the decision that
> governs it — read the ADR for the *why*, but trust this entry (and the code) for *what is true now*; an
> ADR body is frozen at decision time (see `Docs/adr/README.md`).

## Chain item roles

Each item touches one axis (ADR-0004 §1):

- **Weapon** — the chain root; the **base** of every axis (base target strategy, delivery pattern, trigger, the **Cost** side of the economy, an optional payload). It does **not** own Gain-on-hit — that is an on-hit effect (ADR-0005).
- **Amplifier** — **magnitude**: scales the weapon's output (Damage) up. Gain/leech is **not** a separate output — it scales *through* Damage (ADR-0005 §5). Scaling effects themselves (leech %, status, Aoe) is a deferred *effect-magnitude* axis, not the Amplifier's job.
- **Shifter** — **economy trade**: moves magnitude between input/output stats. *Nothing else* — it does **not** touch Target Selection.
- **Converter** — **type reclassification on any axis**: damage type, target strategy, delivery pattern, **cost pool** (ADR-0005), optionally a trigger event type. Changes the *kind*, never the *amount*.
- **Payload** — **propagation**: spawns a child delivery node on impact.
- **Reactor** — **trigger**: replaces the weapon's timer with a combat event.

## Attack resolution

**Attack** (ADR-0004):
One weapon firing, resolved as a sentence across independent axes — Target Selection → Delivery → Propagation. Each axis is reclassified by a different chain item without disturbing the others.

**Aim Anchor** (or **Anchor**) (ADR-0004 §3):
What an attack's delivery centres on — by default the chosen target (a pawn or hex), but a delivery may anchor on **self/origin** instead. Consumed by Delivery as **geometry that orients the pattern**, never as the list of who gets hit.
_Avoid_: "the target" when you mean the anchor; the locked pawn is only hit because it stands on a covered hex.

**Target Selection** (ADR-0004):
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

**Propagation Cost** (fail-forward):
What gates whether a child delivery node fires (ADR-0006). An attack is a **tree** of delivery nodes; resolution walks it depth-first, **paying as it goes** from the weapon's one `CostResource` pool. A node whose **marginal** cost the pool can't cover is **skipped, pruning its subtree** (it spends nothing) — *fail-forward*: "the weapon fired, but not the extra bomb." Linear order is propagation order; a **Splitter** funds siblings **highest-(subtree)-cost-first** off the shared pool. The per-fire cost **uses** a `MutableFloat` seeded with the weapon's base cost; **Reactors and Payloads are cost `Modifier`s** (FlatAdd/PercentAdd/PercentMult) on it — "deeper costs more" is the opt-in `PercentMult`. There is **no** payload predicate/`ConditionType` (dissolved in ADR-0006); watch-conditions are a Reactor/Trigger concern, status is its own system.
_Avoid_: "payload condition" for a predicate — the gate is economic.

**Payload**:
A chained item that adds a **child delivery node** (its own four delivery things) triggered on impact. A nested delivery node, not a free-firing second weapon. Whether it fires is gated by the **propagation cost** (ADR-0006): its cost `Modifier` must be affordable from the running pool, else it and its subtree fizzle. It carries **no pool and no predicate** — only a cost modifier.

**Homing**:
A delivery whose anchor **re-resolves to the (possibly moved) target at impact** instead of freezing at fire time. The deliberate exception to hex-occupancy resolution. (Deferred.)

## Triggering

**Trigger**:
*When/how often* a weapon fires (was "Delivery Mode" — renamed to end the "delivery" overload, ADR-0004 §5). Owned by the **Weapon** (its timer), the **Reactor** (an event override), and the **Shifter** (attack-speed trades). A **Converter** may reclassify a trigger *event type* (on-hit→on-crit) but never its *frequency*. A **Reactor**'s event-firing carries a **cost multiplier scaled by event frequency** (common event → bigger multiplier; ADR-0006 §6) — its balance lever, not a threshold. *Watch*-conditions ("opponent reaches X resource," "before defeat," counters) are a **deferred** Trigger-axis concern (ADR-0006), the home of the dissolved `ConditionType` watch predicates.

## Resource economy

The economy is **three independent axes** (ADR-0005), not one welded pair. *What a weapon spends* and
*what the caster recovers* are unrelated, and recovery rides the hit.

**Cost**:
The activation price of a fire — a **pool** (`CostResource`: Mana / Health / …) plus a magnitude — paid
**once per fire** as the gate that decides whether the attack happens. Owned by the **Weapon/Trigger**
(the fuel). The **Converter** reclassifies the *pool* (Mana → Health = blood magic); the **Shifter**
trades the magnitude. The magnitude is a **`MutableFloat`** over the weapon base, scaled by **Reactor and
Payload cost `Modifier`s** (ADR-0006) — so the "fire cost" is really the running cost of the whole
propagation tree, drained fail-forward (see **Propagation Cost**).
_Avoid_: assuming Cost is mana — it is whichever pool `CostResource` names.

**Gain-on-hit** (or **Leech / Recovery**):
What the caster **recovers per hit**, riding the damage event over each covered-hex occupant. **An
on-hit effect, not a weapon stat** (ADR-0005) — it lives in the Payload-effect family, is grantable and
stackable like a support, and carries **its own pool**, independent of Cost's. Primary form is **leech =
a fraction of damage dealt** (composes with damage magnitude); flat-per-hit is the degenerate case.
_Avoid_: "resource gen on the weapon" — gain left the weapon; a mana-cost weapon can leech health.

**Resource Pool**:
A pawn-side reservoir an attack spends from or restores to (`ResourceType` — Mana, Health, …). Cost and
Gain each name a pool independently. `Resource.CanSpend` already guards spending Health as a resource.
Both health and mana **regenerate on the combat tick** (ADR-0008) — the attack-cost economy is balanced
around a weapon's *sustained* fire rate (`min(naturalCadence, regen/cost)`), not a one-shot budget that
permanently silences a pawn once its pool empties.

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
