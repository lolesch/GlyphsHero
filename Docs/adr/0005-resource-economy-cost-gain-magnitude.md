---
tags:
  - ADR
  - Combat
  - Inventory
  - Resources
  - Items
status: Accepted
date: 2026-06-24
---

# ADR-0005 — The resource economy decomposes into Cost, Gain-on-hit, and Magnitude

**Status:** Accepted (2026-06-24)
**Lifecycle:** Implemented 2026-06-24 (both phases, test-first)
**Companion:** ADR-0004 (attack model — item roles, the delivery decomposition).
**Refines:** **ADR-0004 §1** — the Weapon "owns the stat economy" and the Converter's deferred
"resource type" axis. This ADR says *what that economy is* and *which part the Converter reclassifies*.
**Context:** picking up ADR-0004's deferred **resource-type Converter axis**, the first cut welded a
single resource *pool* onto the weapon governing **both** what it spends and what it recovers. That
makes the most iconic build — **cost mana, leech health** — structurally impossible, and it contradicts
how every reference game (Noita, PoE/Diablo, TFT, Brotato/Backpack Hero) models the economy: *what you
spend* and *what you recover* are independent, and recovery is a modifier layer that rides the hit, not
a base weapon scalar. The code already half-says this — see Decision 1's "structural tell."

---

## The mismodel this corrects

The economy was modelled three inconsistent ways at once:

- **Cost** as `WeaponConfig.ResourceCost` — a single untyped float assumed to be mana
  (`WeaponStats.ResourceCost`) — *and also* as the typed pair `WeaponInputStat.{LifeCost, ManaCost}`,
  of which the resolver maps only `ManaCost` and **silently drops `LifeCost`** (enshrined in the
  `UnbackedInputStat_IsIgnored` test). So one half of the code says "two cost pools exist," the other
  collapses them into one untyped mana number.
- **Gain** as `WeaponConfig.ResourceGenOnHit` (untyped float, assumed mana) + the
  `WeaponOutputStat.ResourceGenOnHit` magnitude.

**The structural tell:** in `PawnCombatController.Fire`, **cost is paid once per activation**
(`costResource.ReduceCurrent` at the top) while **gain is applied once per target hit**
(`genResource.IncreaseCurrent` inside the occupancy loop). They already have *different event
semantics* — an activation gate vs. an on-hit reward — yet were welded into one struct as twins.

---

## Decisions

### 1. The weapon economy is three independent axes, not one welded pair

| Axis | What it is | When it resolves | Who owns it |
|---|---|---|---|
| **Cost** | the activation price — a **pool** + a magnitude | **once per fire** (the `CanFire` gate, then spend) | the **Weapon / Trigger** (the fuel to fire) |
| **Gain-on-hit** | recovery the caster receives — a **pool** + an amount | **per hit**, riding the damage event (per covered-hex occupant) | an **on-hit effect** in the Propagation/Payload-effect layer (Decision 3) |
| **Magnitude** | how much of either | resolution time | the existing **Amplifier** (scales) / **Shifter** (trades) layer — unchanged |

Cost's pool and Gain's pool are **independent**. A mana-cost weapon can leech health; a blood weapon can
cost health and restore mana. The two were only ever coupled by the accident of sharing one field.

### 2. The Converter's "Resource axis" reclassifies the **Cost pool** — and only that

This is the concrete fill-in of ADR-0004 §1's deferred "resource type" Converter axis. A
`ConverterAxis.Resource` Converter changes **which pool the weapon spends** (Mana → Health = blood
magic) — one pool, one reclassification, "change the *kind*, never the *amount*," consistent with every
other Converter axis. The **Gain pool is not this axis** — it is a property of the on-hit effect
(Decision 3), reclassified (if ever) by a Converter on *that* effect. Keeping the Converter pointed at a
single, unambiguous pool is what keeps "reclassify the resource" a clean type-change rather than a
vector edit.

### 3. Gain-on-hit is an on-hit **effect**, not a weapon field

Recovery rides the damage event, is **per-target**, and should be **grantable and stackable by
modifiers** — exactly like a PoE leech support, TFT omnivamp, or a Brotato lifesteal stat. Its home is
the existing `PayloadEffect` family (`StatusPayloadEffect`, `PositionPayloadEffect`,
`TerrainPayloadEffect`), as a new **`ResourcePayloadEffect`** carrying its own **pool** (`ResourceType`)
and an amount that is **either a fraction of damage dealt (leech) or a flat per-hit amount**. **Leech =
% of damage is the headline form** — it composes with damage Amplifiers for free (boosting damage boosts
sustain), which is why reference games favour it. `ResourceGenOnHit` leaves the weapon
(`WeaponConfig` / `WeaponStats` / `WeaponOutputStat`); the per-hit `genResource.IncreaseCurrent` in
`Fire`/`FirePayloads` becomes "apply each `ResourcePayloadEffect` over the covered occupants."

Making gain an effect is the **load-bearing** decision: it is what *physically decouples* spend-pool
from recover-pool. The pool split in Decision 1 falls out of it rather than needing a second pool field
bolted to the weapon.

### 4. Cost is one pool + one magnitude — the half-built typed-cost pair is retired

The `WeaponInputStat.{LifeCost, ManaCost}` split (a second, never-finished cost model where `LifeCost`
was silently dropped) is retired in favour of a single **Cost** magnitude plus a **`CostResource`**
pool selector. The pool decides which resource the cost draws from; the Converter reclassifies the pool;
the Shifter still trades the magnitude. **Dual-pool cost** (spending mana *and* life at once) is
deliberately **not** v1 — it is rare, and it muddies the Converter's "reclassify the pool" story
(Decision 2). Cost is a scalar over one pool, not a vector.

### 5. After Gain leaves, the Amplifier's output surface is just Damage — and that is enough

With Gain gone (Decision 3), the weapon's only output **magnitude** is **Damage**, so
`WeaponOutputStat` collapses toward `{ Damage }` and the Amplifier becomes the Damage-magnitude item.
This is deliberate, not a loss of relevance: because **leech is a fraction of damage** (Decision 3), a
**Damage Amplifier *is* a sustain Amplifier** — gain scales *through* damage with no separate
"amplify gain" stat. (This is a second reason to prefer leech-%-of-damage over flat-per-hit: **flat gain
is un-amplifiable** once `ResourceGenOnHit` leaves the output surface, so the flat form stays a
degenerate authored constant while the %-form composes with damage magnitude.)

**Explicit no — effect magnitude is not the Amplifier's job.** Scaling the *effects* themselves
(leech %, status potency/duration, `Aoe` radius) is its **own axis**, distinct from "weapon output."
Folding it into the Amplifier would re-blur the one-item-one-axis discipline ADR-0004 established. It is
deferred until the Phase-2 effect system exists. Future *weapon-output* additions (crit chance /
multiplier) are blocked on the same damage-type/crit system as the damage-type Converter axis.

---

## Considered and rejected

- **One pool governs both cost and gain** (the first cut). Makes mana-cost lifesteal impossible and
  collapses two axes whose event semantics already diverge (once-per-fire vs. per-hit). Rejected — it
  was the bug that triggered this ADR.
- **Gain as a weapon scalar with its *own* pool field** (a typed twin to cost). Technically allows
  cost ≠ gain pools, but keeps recovery welded to the weapon base instead of the modifier layer where
  every reference game puts it — it can't be granted or stacked by a support, and it ignores the
  existing effect system. Rejected in favour of gain-as-effect (Decision 3).
- **Flat-per-hit as the primary gain form.** Kept as a supported option, but **leech = % of damage** is
  primary because it composes with damage magnitude and matches the reference idiom.
- **Dual-pool / vector cost** (Decision 4) — deferred, not rejected outright.

## Deferred (designed, not built)

- **Reclassifying the *gain* pool** via a Converter — needs gain-as-effect to exist first, then a
  Converter that targets the effect.
- **Dual-pool / vector costs.**
- **Resource pools beyond Mana/Health** (rage, energy, charges…). `ResourceType` is the seam.
- **Gain triggers other than on-hit** (on-kill, on-crit) — those are Trigger/Reactor territory, not the
  gain effect.
- **Effect-magnitude axis** (Decision 5) — scaling leech %, status potency/duration, `Aoe` radius. A
  distinct axis from the Amplifier's weapon-output magnitude; waits on the Phase-2 effect system. Future
  weapon-output stats (crit) are blocked on a crit/damage-type system.
- The mana-hardcoded **payload conditions** (`ConditionType.ResourceFull/Below/Above` read
  `_pawn.Stats.mana`) — **superseded by [[0006-payload-propagation-cost-economy|ADR-0006]]**, which
  dissolves the whole `ConditionType` draft rather than generalising it (the watch predicates move to the
  Reactor/Trigger axis, the payload gate becomes economic). This Phase also **orphaned**
  `ConditionType.ResourceGenBelow/Above` — they gated on the now-removed `ResourceGenOnHit` weapon stat;
  they are dissolved with the rest in ADR-0006, and a future gain-magnitude gate belongs to the
  **effect-magnitude axis** above.

## Consequences

- **Positive:** mana-cost / health-leech (and the inverse) is now expressible — the two pools are
  independent; leech composes with damage Amplifiers for free (no separate "amplify gain" stat); the
  half-built typed-cost pair (`LifeCost` silently dropped) is gone; gain lands in the existing
  `PayloadEffect` family where supports can grant/stack it.
- **Negative / debts:** the Amplifier's output surface narrows to Damage (Decision 5, accepted); a
  separate **effect-magnitude axis** (scale leech %, status, `Aoe`) is now owed (Deferred); flat-per-hit
  gain is un-amplifiable, so the `%`-of-damage form is the intended one.

**Seams (where this lives in code):** `ConverterAxis.Resource` + `WeaponStats.CostResource`
(cost-pool reclassification, in `WeaponStatResolver`); `ResourcePayloadEffect.ComputeGain`
(`PayloadEffect.cs`, the pure on-hit gain seam); `PawnCombatController.ResolveChainResources` /
`ExecuteEffect` (cost spend + per-hit gain dispatch). Build detail (file-by-file deltas, test counts,
asset migrations) is in the 2026-06-24 commits — `git log --grep ADR-0005`.
