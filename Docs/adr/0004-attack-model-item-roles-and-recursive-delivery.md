---
tags:
  - ADR
  - Combat
  - Inventory
  - Delivery
  - Items
status: Accepted
date: 2026-06-23
---

# ADR-0004 — Attack model: item roles, the delivery decomposition, and the recursive-delivery collapse

**Status:** Accepted (2026-06-23)
**Lifecycle:** Implemented 2026-06-24
**Amends:** ADR-0001 §2b (withdrawn), §3 (amended) — see Decision 2; ADR-0003 (`Self` delivery flag
relocated to the new Affinity axis) — see Decision 3.
**Companion:** ADR-0001 (combat tick, Reach as a pawn stat), ADR-0002 (hex-occupancy damage +
telegraph), ADR-0003 (delivery patterns reach-gated + stackable).
**Context:** the attack vocabulary had drifted across ADR-0002/0003 and several reworks. Two flat lists
of "delivery patterns" coexisted (Attack Targeting.md's *Projectile/Beam/Arc/Dash/Adjacent/Homing* vs.
the shipped *Single/Line/Cleave/Self/Aoe* mask), "delivery" named two different axes, target-selection
modification was assigned to two different items, and the `ResolveMinReach` melee/ranged placeholder was
a remnant of a reach model ADR-0003 had already moved away from. This ADR consolidates the model so each
component has exactly one job and the doc matches the code's intended shape.

---

## Decisions

### 1. Each chain item owns one axis — and the Converter is the type-reclassifier

| Item | Job |
|---|---|
| **Weapon** | The **base** of every axis — base target strategy, base delivery pattern, base trigger, the stat economy, an optional payload. The chain root. |
| **Amplifier** | **Magnitude** — scales the upstream weapon's output stats up. No conditions; costs grid space. |
| **Shifter** | **Economy trade** — moves magnitude between input/output stats. *Nothing else.* |
| **Converter** | **Type reclassification on any axis** — damage type, **target strategy** (Nearest→LowestHP), **delivery pattern** (Line→Cleave), resource type, and *optionally* a trigger **event** type. Changes the *kind*, never the *amount*. |
| **Payload** | **Propagation** — spawns a child delivery node on impact (Decision 4). |
| **Reactor** | **Trigger** — replaces the weapon's timer with a combat event (Decision 5). |

*Why:* this gives a clean split — Amplifier = *how much*, Shifter = *trade how much*, Converter =
*what kind*, Payload = *what spawns next*, Reactor = *when*. It **corrects a mis-assignment**:
Attack Targeting.md and CONTEXT.md said the **Shifter** modifies Target Selection, but Converter.md's
own signal list already claims "target type" — so target-selection reclassification is the
**Converter's** job, and the Shifter returns to being purely the economy-trade item. A single weapon's
base config seeds all three attack axes; Converters reclassify them downstream.

### 2. Reach is a single uniform pawn stat — ADR-0001 §2b withdrawn, §3 amended

There is **one Reach**: a pawn stat that is purely the **acquisition gate** (the distance at which a
weapon can pick its target and the distance a pawn closes to). It is **uniform across all of a pawn's
attacks** — there is no per-weapon reach.

- **"Melee" vs "ranged" is a pawn archetype, not a weapon or pattern property** — it is just the name
  for Reach = 1 vs Reach > 1. A brawler (Reach 1) and a sniper (Reach 5) holding the *same* weapon get
  different feel for free; the weapon is a component, the pawn is the caster (the Noita/wand-builder
  framing ADR-0001 §2 committed to).
- This **withdraws ADR-0001 §2b** (delivery patterns split by range behaviour; range-fixed
  Adjacent/Dash stay range-1; a Converter "unlocks range"). ADR-0003 had already made Reach the pure
  acquisition gate with footprints scaling by it; §2b was the last remnant of the old per-pattern model.
- It **amends ADR-0001 §3**: movement is monotone closing to **the pawn's Reach**, not "the minimum
  effective reach *across active weapons*." There is nothing to minimise over.

**Accepted losses (on purpose):**
- The "Converter unlocks range" build moment is gone — Reach is bought by **passive grid-space items**
  (ADR-0001 §2c), and the Converter reshapes *coverage*, not distance. Two clean levers, not one
  conflated one.
- The weapon mix no longer authors *engagement distance* (only coverage/effect). A short-reach weapon
  on a high-Reach pawn fires from far away.

**Dash is not a delivery.** ADR-0001 §2b bundled "Adjacent/Dash" as range-fixed patterns; a dash is a
**movement action**, parked in the movement-strategy-item space (an ADR-0001 open question), not the
delivery axis.

*Code consequence:* `CombatCoordinator.ResolveMinReach` collapses to `max(1, round(pawn.range))`; the
`Payload.ShapeSize ≤ 1` melee/ranged placeholder is deleted.

### 3. Delivery decomposes into Pattern × Layers × Affinity × Anchor

"Delivery" (an attack landing on hexes) is four independent things, not one enum:

- **Pattern** (the geometry — which hexes, oriented by the anchor, scaled by Reach):
  - **`Single`** — the anchor's hex. **Anchor-locked**: it ignores intervening pawns (a Single that
    stopped at the first body in the way would *be* a Line; see Layers). Hits its target because the
    target stands on the covered hex.
  - **`Line`** — the ray from origin to anchor. (The old "Projectile" vs "Beam" distinction is **not**
    two patterns — it is `Line` with or without the Pierce layer.)
  - **`Cleave`** — the anchor + its two same-ring neighbours (3 hexes, no angle math). "Hit the target
    and what flanks it"; effectively half of the old `Adjacent`.
  - **`Aoe`** — a disk of `ShapeSize` radius around the anchor. **Payload-only** — no weapon paints a
    disk.
  - Names **`Single`/`Line`/`Cleave`/`Aoe` are canonical** — we do not use "Bolt", "Projectile", or
    "Beam".
- **Layers** (orthogonal resolution flags — **all deferred**, recorded so their home is fixed):
  - **Pierce** — a `Line` hits *all* pawns in path instead of stopping at the first. Lives here, **not**
    in Propagation (it is about *hitting*, not about *what happens on hit*).
  - **LoS / obstacle** — truncates a ranged delivery at the first blocking **terrain** (distinct from
    pawn occupancy; see Decision 6). Deferred — **when this is revisited, ask the user for the RedBlob
    Hex reference.**
  - **Homing** — re-resolve the anchor to the moved target at impact (the lone occupancy exception).
- **Affinity** (whose occupancy counts): **hostile** (default) / **friendly** / **self**. This is
  ADR-0003's Self/hostile split **promoted to its own axis** and **removed from the `DeliveryPattern`
  enum**. It is the friendly/self side the aura/buff work will extend.
- **Anchor** (what the geometry centres on): the **target** (default) or **self/origin** — a
  target-selection option. Anchor-self + `Aoe` + hostile affinity = a damage nova; anchor-self +
  `Single` + self affinity = a self-damage proc; anchor-self + friendly affinity = a heal-around-me.
  "Everything around me" only makes sense centred on me, so it is an anchor choice, not a pattern.

### 4. Propagation is the recursive-delivery collapse — a Payload spawns child delivery node(s)

Propagation is **not a behavior enum.** A Payload **spawns one or more child delivery nodes**, and every
old "behavior" is just a *configuration of the child* on the Decision-3 axes:

| Old behavior | Now |
|---|---|
| **Fork** | **Removed** — redundant with Split once children carry their own target/geometry. |
| **Split** | child **count = N parallel** (vs 1). Requires the **Splitter/Merger item** as its enabler. |
| **Chain** | **nested payloads**, detonating one after another. **No "exclude already-hit" rule** — chained explosions each still damage everyone inside their footprint. Pure recursion depth + sequential timing. |
| **Explode** | a child with the **`Aoe` pattern**. |
| **Return** | a child with **anchor = origin** (Decision 3). |
| **Pierce** | **moved out** to a delivery Layer (Decision 3). |

The only irreducible propagation primitive is **child count (1 or N)**; everything else reuses Target
Selection / Pattern / Anchor / Affinity. This is the recursive-delivery model (ADR-0002, CONTEXT.md)
fully realised — Propagation needs almost no new vocabulary.

### 5. "Delivery Mode" → "Trigger"

The firing-cadence axis ("when/how often a weapon fires") is renamed **Trigger** to end the "delivery"
overload. It is owned by the **Weapon** (its timer), the **Reactor** (an event override), and the
**Shifter** (attack-speed trades). A **Converter** may reclassify a trigger **event type** (e.g.
on-hit→on-crit) — a type-reclassification consistent with Decision 1 — but it never sets *frequency*.
This borders the Reactor: a Reactor *installs* an event trigger; a Converter *reclassifies* one that
already exists.

### 6. One obstacle model, two kinds, two readers

Blocking splits into two distinct concerns, both consumed by the `DeliveryResolver` and the
`HexPathfinder`:

| Kind | Nature | Affects delivery | Affects movement |
|---|---|---|---|
| **Pawn occupancy** | dynamic | `Line` stop-at-first vs Pierce | hard obstacle in A* (already so) |
| **Terrain obstacle** | static | LoS truncation (deferred) | impassable terrain |

A `Single` is anchor-locked and ignores pawn occupancy; pawn-blocking is a `Line`-family concern only.

---

## Considered and rejected

- **`Fork`** as a propagation behavior — collapses into `Split` plus the child's own targeting.
- **`Bolt`** as a rename of `Single` — clearer to some, but `Single` is kept by preference.
- **`Projectile` / `Beam`** as named patterns — they are `Line` ± the Pierce layer, not separate
  geometries.
- **`Self` as a delivery pattern** — relocated to the Affinity axis + the self/origin Anchor option.
- **Per-weapon Reach / a melee-ranged classification** — Reach is one uniform pawn stat (Decision 2).

## Deferred (designed, not built)

- **LoS / terrain-obstacle layer** — and the `Arc` "ignore obstacles" variant. *Reminder to self: ask
  the user for the RedBlob Hex reference when picking this up.*
- **Pierce** (home fixed: a delivery Layer), **Homing**, **Converter trigger-event reclassification**.
- **Ring / Wall / Nova** geometries — natural extensions once geometry is a pure function; not v1.
- **Recursive payload anchoring at the impact hex** — payloads still anchor on the locked target
  (carried from ADR-0003).

## Consequences — code deltas (implemented 2026-06-24, test-first)

- **Done.** `CombatCoordinator.ResolveMinReach` → `ResolveReach` = `max(1, round(pawn.range))`; the
  `ShapeSize ≤ 1` placeholder and per-chain loop deleted (`_minReach`→`_reach`). Verified 103/103 green.
- **Done.** `Self` removed from the `DeliveryPattern` enum (bit `1 << 3` reserved); new `Affinity` enum
  (`Hostile`/`Friendly`/`Self`) authored on `WeaponConfig`/`PayloadBehavior` and threaded through
  `WeaponItem`/`IWeaponItem`/`WeaponStats`/`WeaponStatResolver`. `DeliveryResolver` is now pure geometry
  (dropped `SelfAffinity`/`SelfHexes`). New pure `DeliveryAffinity` (anchor + caster-side rules, v1:
  Self is self-anchored) drives `PawnCombatController.Fire`/`FirePayloads`/`ResolveTargets`. Red-green via
  `DeliveryAffinityTests` (mutation-proven); obsolete `DeliveryResolverTests.Self*` removed. **101/101 green.**
- Docs synced: CONTEXT.md, Attack Targeting.md, Converter.md, Shifter.md, Payload.md,
  Splitter & Merger.md, Weapon.md, and the ADR-0001/0003 amendment notes.
- `ChainResolverTests` kept green throughout.

### Independent Anchor axis (implemented 2026-06-24, test-first)

- **Done.** Anchor is now its own axis, no longer derived from Affinity. New `Anchor` enum
  (`Target`/`Origin`, default `Target`) authored on `WeaponConfig`/`PayloadBehavior` and threaded through
  `WeaponItem`/`IWeaponItem`/`WeaponStats`/`WeaponStatResolver` (mirroring the Affinity threading). The
  v1 coupling is gone: the anchor logic moved out of `DeliveryAffinity` (slimmed to `TargetsCasterSide`
  only) into a new pure `DeliveryAnchor.Resolve(origin, target, anchor)`; `PawnCombatController.Fire`/
  `FirePayloads` centre geometry on `stats.Anchor`/`behavior.Anchor`. Anchor-Origin now combines with
  **any** affinity — anchor-Origin + Hostile + Aoe = a damage nova, + Friendly = a heal-around-me, +
  Self = the deliberate self-hurt build-around (which now authors `Anchor.Origin` explicitly rather than
  getting it implicitly from `Affinity.Self`). No content authored `Affinity.Self`, so nothing relied on
  the old implicit coupling — clean cut.
- Red-green via new `DeliveryAnchorTests` (mutation-proven: an "always return target" mutation failed
  exactly the two Origin tests, the Target test correctly stayed green); the obsolete anchor cases moved
  out of `DeliveryAffinityTests`. **102/102 green.** `ChainResolverTests` untouched.

### Converter — type reclassification on the WeaponStats axes (implemented 2026-06-24, test-first)

- **Done (the three data-backed axes).** The Converter (ADR-0004 §1) now reclassifies the *kind* on one
  axis, never the amount. New `ConverterAxis` enum (`Delivery`/`Affinity`/`Anchor`); `ConverterConfig`
  drops its placeholder `from`/`to` for `Axis` + `ToDelivery`/`ToAffinity`/`ToAnchor`; `IConverterItem`/
  `ConverterItem` carry them. `WeaponStatResolver` seeds delivery/affinity/anchor from the weapon and a
  chained Converter's matching `To*` value **replaces** the seed (last-wins per axis), in the same
  contributor loop as Amplifier/Shifter. A Converter was already a legal chain modifier
  (`ChainResolver.IsValidConnection`) — only the resolver ignored it. Tooltip updated (was "not yet
  implemented" → shows `axis → value`).
- The other Converter.md axes stay deferred for lack of an underlying data system: **damage type**
  (damage is untyped), **target strategy** (selection isn't data-driven — see the deferred
  "target-selection strategies as data"), **resource type** (cost/gen are mana-only;
  `PawnCombatController.ResolveChainResources` still returns mana/mana with a Converter TODO), and
  **trigger event** reclassification.
- Red-green via 5 new `WeaponStatResolverTests` (mutation-proven: a no-op `ApplyConversion` failed
  exactly those 5, the other 10 stayed green). Three play-test assets authored —
  `Converter_Cleave` (Delivery→Cleave), `Converter_Line` (Delivery→Line), `Converter_AnchorSelf`
  (Anchor→Origin) — and added to `GamePhaseController.itemPool` so they drop into the stash. **107/107
  green.** `ChainResolverTests` untouched.

**Still deferred**: the Delivery **Layers** (Pierce/LoS/Homing), the **remaining Converter axes** (damage
type, target strategy, trigger event — each blocked on its data system), recursive payload
anchoring at the impact hex, and `Friendly`-affinity content. *(The **resource-type** axis is taken up
by **ADR-0005**, which decomposes the economy into Cost / Gain-on-hit / Magnitude and points the
Converter at the **cost pool**.)*
