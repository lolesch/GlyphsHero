---
tags:
  - Combat
  - Weapon
  - HexGrid
  - Targeting
  - Delivery
  - Propagation
---

# Attack Delivery

Every weapon attack resolves across independent axes. Each axis is reclassified by a different [[Item Chaining|chain item]] without affecting the others (see [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]]).

|Axis|Question|Chain Modifier|
|---|---|---|
|**[[Attack Delivery#Target Selection\|Target Selection]]**|Who/where does the weapon aim at?|[[Converter]]|
|**[[Attack Delivery#Delivery\|Delivery]]**|Which hexes does it cover?|[[Converter]]|
|**[[Attack Delivery#Propagation\|Propagation]]**|What spawns on impact?|[[Payload]]|

Reading an attack as a sentence: _"Strike the nearest enemy → along a line → exploding on impact"_

The **Weapon** sets the *base* of each axis; a **Converter** reclassifies the *kind* (target strategy, delivery pattern, damage type). Magnitude is the **Amplifier**'s job, economy trades the **Shifter**'s, firing cadence the **Trigger** axis ([[Reactor]]/timer) — none of those live here.

---

# Target Selection

Defines which unit/hex the weapon aims at — the **aim anchor**. Set per weapon in config. Predictable by design — enemies follow the same rules, so the player can learn and counter patterns via positioning.

| Strategy                          | Description                                                                             |
| --------------------------------- | --------------------------------------------------------------------------------------- |
| `Nearest`                         | Closest valid target by hex distance                                                    |
| `LowestHP`                        | Target most likely to be finished off — maximizes kills                                 |
| `HighestHP`                       | Focus threat — countered by spreading HP across the team                                |
| `RandomWithinShape`               | Fires into a defined hex shape with no lock-on — spread damage, hard to predict exactly |
| `Self` / origin                   | Anchors the delivery on the firing pawn (drives self-damage, nova-around-me, Return)     |
| `MostBuffed/Debuffed`             |                                                                                         |
| `Specific tag` <br>(e.g. burning) |                                                                                         |

**Chain modification:** a [[Converter]] reclassifies the active strategy (e.g. `Nearest`→`LowestHP`, or restricting `Nearest` to **burning** targets) — *not* the [[Shifter]] (that mis-assignment is corrected in ADR-0004). One active strategy per weapon at resolution time; the Converter **replaces, never stacks**.

---

# Delivery

Defines which hexes the attack covers. Decomposes into four independent things (ADR-0004 §3); **recursive** — every delivery node (incl. a payload's child) has its own four.

## Pattern (geometry)

A stackable `[Flags]` mask; covered hexes = the **union** of each set flag, resolved by `DeliveryResolver`, oriented by the anchor and scaled by **Reach** (no separate shape-size knob — ADR-0003).

| Pattern    | Covers                                                          | Notes                                                       |
| ---------- | -------------------------------------------------------------- | ---------------------------------------------------------- |
| `Single`   | the anchor's hex                                                | **Anchor-locked** — ignores intervening pawns              |
| `Line`     | the ray from origin to anchor                                   | "Projectile vs Beam" = `Line` ∓ the Pierce layer, not two patterns |
| `Cleave`   | the anchor + its two same-ring neighbours (3 hexes)            | no angle math; half of the old `Adjacent`; replaces Cone   |
| `Aoe`      | a disk of `ShapeSize` radius around the anchor                  | **payload-only** — no weapon paints a disk                 |

Canonical names are `Single`/`Line`/`Cleave`/`Aoe` — *not* "Bolt", "Projectile", or "Beam". **Chain modification:** a [[Converter]] reclassifies the pattern (e.g. `Line`→`Cleave`).

## Layers (deferred — homes fixed)

Orthogonal resolution flags: **Pierce** (a `Line` hits all pawns in path, not just the first — lives here because it is about *hitting*, not *on-hit*), **LoS/obstacle** (truncate at the first blocking terrain), **Homing** (re-resolve the anchor at impact). All deferred; see ADR-0004.

## Affinity

Whose occupancy counts: **hostile** (default) / **friendly** / **self**. Its own axis — the friendly/self side the aura/buff work extends.

## Anchor

What the geometry centres on: the **target** (default) or **self/origin** (a Target-Selection choice). Anchor-self + `Aoe` + hostile = a damage nova; + self affinity = self-damage; + friendly = heal-around-me.

---

# Propagation

What spawns at the point of impact. Not inherent to the weapon — added via [[Payload]] as **child delivery nodes**, each recursing through the full Delivery model above. A weapon with no payload simply hits.

**Propagation is not a behavior enum** (ADR-0004 §4). The only primitive is **child count** — 1, or **N parallel** (which requires a [[Splitter & Merger|Splitter]]). The old "behaviors" are just child configurations:

| Old behavior | Now |
|---|---|
| `Split`   | child count = N parallel — needs a [[Splitter & Merger|Splitter]] |
| `Chain`   | nested payloads, detonating in sequence — **no** exclude-already-hit (each still hits everyone in its footprint) |
| `Explode` | a child with the `Aoe` pattern |
| `Return`  | a child with anchor = origin |
| `Fork`    | **removed** — redundant with Split |
| `Pierce`  | **moved** to a Delivery Layer |

Each child inherits the originating target selection and delivery unless a subsequent modifier changes it.

---

# Phase Ownership

|Axis|Placement Phase|Resolution Phase|
|---|---|---|
|Target Selection|Read enemy strategies, counter via positioning|Locked — fires per config|
|Delivery|Assess coverage of weapon patterns|Locked — fires per config|
|Propagation|Predict impact spread|Executes per payload chain|

Positioning decisions during the placement phase are direct answers to the target selection and delivery patterns of both sides. This is where the tactical puzzle lives.

---

# Design References

- **Path of Exile** — the additive-modifier model: attack expressions compose through stacked upgrades, not fixed weapon properties. Our recursive child-delivery collapse (ADR-0004 §4) is the same spirit — Split/Chain/Explode/Return are child *configurations*, not a fixed behavior enum.
- **Into the Breach** — fully telegraphed, deterministic targeting on both sides generates deep positional counterplay without randomness. (ITB's flat pattern list — Projectile/Beam/Arc/Dash/Adjacent — is *not* our model: we decompose into Pattern × Layers × Affinity × Anchor, ADR-0004.)
- **Backpack Battles** — fatigue as a stalemate escape valve; async resolution with no player input during the resolution phase.

---

Notes / open threads:

1. **`RandomWithinShape`** — the shape it fires into isn't defined here. It will live in `WeaponConfig`, authored like aura **shapes** (distinct from delivery **patterns**).
2. **`Dash`** is **not a delivery** — it's a movement action (ADR-0004 §2), parked in the movement-strategy-item space, not this axis.
3. **LoS / terrain obstacles** are a deferred Delivery Layer (ADR-0004 §3, §6). *When picking this up, ask the user for the RedBlob Hex reference.*