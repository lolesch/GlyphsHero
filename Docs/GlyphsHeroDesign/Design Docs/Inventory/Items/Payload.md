---
tags:
  - Item
  - Attachment
  - Inventory
---

**Gate:** a payload is gated **economically**, not by a predicate (see [[0006-payload-propagation-cost-economy|ADR-0006]]). It adds a **cost modifier** to the attack; propagation pays as it walks the tree and **fail-forwards** — if the running pool can't cover a payload's marginal cost, that payload *and its subtree* fizzle while the rest of the attack still fires. A payload must enable at least one behavior that cannot exist if both weapons are used independently.
# Definition

> [!tldr]+ Description
> Fires conditionally when [[Item Chaining|Chain Propagation]] reaches it.
> Enhances an attack with behavior that cannot exist if used as a [[Weapon]] -> tradeoff

> [!quote]- Purpose - *Why is this essential?*
> Bridges from attacks into hex grid layer. Customize attacks to maximize synergies. 

> [!check]- Reward - *What is the gain?*
> Adds status effects and terrain impact to attacks. Combinations impossible when both weapons fire independently. 

> [!warning]- Risk - *What are the punishments*
> [[Payload#Affinity Tags|Affinity Tags]] mismatch lowers the impact.
> Out-pricing the pool — the payload (and its subtree) fizzles while the rest of the attack fires (fail-forward, [[0006-payload-propagation-cost-economy|ADR-0006]]).
> Bad resource management 

> [!fail]- Opposition - *What counters this?*
> [[Payload#Affinity Tags|Affinity Tags]] mismatch
> Weapon activation penalties

> [!error]- Polarity - *What increases its weakness?*
> Small pools with shared [[Payload#Affinity Tags|Affinity Tags]] -> more mismatch.
> Deep chains behind `PercentMult` cost modifiers -> the payload costs more the deeper it sits.
> High activation penalties. 

> [!example]- Progress - *What is the goal*
> Shape combat in unexpected ways.
> Combat complexity and specialization

> [!info]- Depth - *Where are the synergies*
> Relies on [[Item Chaining]] for optimal setup. 
> Scripted Synergies with [[Weapon]] are planed.

> [!tip]- Appeal - *Does it help the game*
> Core fantasy the [[Item Chaining]] is built around.
> Bridging from Inventory to Hex Grid -> predictable tactile layer

---

# A payload is a child delivery node

A payload doesn't have its own bespoke axes — it is a **child [[Attack Targeting#Delivery|delivery node]]** that recurses through the full attack model (Target Selection × Delivery Pattern × Layers × Affinity × Anchor), triggered on the parent's impact. See [[0004-attack-model-item-roles-and-recursive-delivery|ADR-0004]] §4.

The old "propagation behaviors" are just child configurations:

| Behavior  | = child configured as                                              |
| --------- | ----------------------------------------------------------------- |
| `Explode` | a child with the `Aoe` pattern                                     |
| `Return`  | a child with anchor = origin                                       |
| `Split`   | child count = N parallel — requires a [[Splitter & Merger\|Splitter]] |
| `Chain`   | nested payloads detonating in sequence (no exclude-already-hit)    |
| `Fork`    | **removed** (redundant with Split)                                 |
| `Pierce`  | **moved** to a Delivery Layer (it's about *hitting*, not on-hit)   |

# Propagation cost (the gate)

A payload's only gate is **cost** ([[0006-payload-propagation-cost-economy|ADR-0006]]). It carries a cost **`Modifier`** (`FlatAdd` / `PercentAdd` / `PercentMult`) on the weapon's per-fire cost — *not* a pool of its own, *not* a predicate. The attack is a **tree**; resolution walks it depth-first paying from the weapon's one `CostResource` pool, and **fail-forwards**: an unaffordable node is skipped and its subtree pruned (spending nothing), so the rest of the attack still fires. A [[Splitter & Merger|Splitter]] funds its branches **highest-cost-first** off the shared pool. `PercentAdd` measures off the effective base (no compounding); `PercentMult` compounds — the deliberate "deeper costs more" lever. The cost **uses** the stat system's `MutableFloat`, so it composes exactly like every other stat.

> Proc-chance is **parked** (kept, not deleted) for the future weapon-economy ADR — not a propagation gate, to keep combat deterministic ([[0001-range-movement-and-combat-tick|ADR-0001]]).

# Effect Axes (what a payload adds beyond geometry)

| Axis                | Dimensions                                                                      |
| ------------------- | ------------------------------------------------------------------------------- |
| **Temporal** (when) | Instant · Delayed · Repeating · Duration                                        |
| **Effect** (what)   | Status effects · Positioning (push/pull/stun) · Terrain changes                 |

---
# Affinity Tags

Every weapon is *good at expressing certain payloads, awkward at others.* Measured by tag overlap — matching tags raise expression strength; mismatches lower it (threshold model, think DIII set bonuses).

Tags serve dual roles: on a **weapon** they declare capability; on a **payload** they declare modifier requirements.

| Tag group | Examples |
|---|---|
| Delivery | `melee` `ranged` `projectile` `beam` |
| Spread | `path` `aoe` `pierce` `chain` |
| Interaction | `hit` `dot` `status` `control` `terrain` |
| Constraint | `los` `random` `entity` |

---
# Design References
- **PoE CoC / CwDT** — linked spell fires conditionally when reached, never freely
- **Noita trigger spells** — payload doesn't become a second free-firing wand

